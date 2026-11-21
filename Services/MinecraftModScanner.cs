using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftConnectTool.Services;

internal sealed record ModFile(string FileName, string Md5);
internal sealed record ModManifest(string? GameVersion, ModFile[] Mods, string? Loader,
    string? LoaderVersion, string? FmlVersion, string? McpVersion, string? NeoFormVersion, string? JavaVersion)
{
    [JsonIgnore]
    public MinecraftServerRelease? ServerRelease { get; init; }
}

internal sealed record MinecraftInstance(string Directory, string[] Arguments, string[] ClassPath, string? JavaExecutable,
    MinecraftServerRelease? ServerRelease = null);

internal static class MinecraftModScanner
{
    public static async Task<ModManifest?> ScanRunningInstanceAsync(CancellationToken cancellationToken,
        bool allowServer = false, Action<int, int>? progress = null)
    {
        MinecraftInstance[] instances;
        try
        {
            instances = await Task.Run(() => FindInstances(allowServer, cancellationToken), cancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return null;
        }

        if (instances.Length != 1)
            return null;

        var instance = instances[0];
        return await Task.Run(() => ScanAsync(instance, cancellationToken, progress), cancellationToken)
            .ConfigureAwait(false);
    }

    private static MinecraftInstance[] FindInstances(bool allowServer, CancellationToken cancellationToken)
    {
        try
        {
            var instances = OperatingSystem.IsWindows() ? FindWindowsInstances(allowServer, cancellationToken)
                : OperatingSystem.IsLinux() ? FindUnixInstances(ReadLinuxArguments, allowServer, cancellationToken)
                : OperatingSystem.IsMacOS() ? FindUnixInstances(ReadMacArguments, allowServer, cancellationToken)
                : Enumerable.Empty<MinecraftInstance>();
            var candidates = instances.ToArray();
            var clients = candidates.Where(instance => instance.ServerRelease == null).ToArray();
            return (clients.Length == 0 ? candidates : clients).DistinctBy(instance => instance.Directory,
                OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).ToArray();
        }
        catch (Exception ex) when (ex is ManagementException or COMException or IOException or UnauthorizedAccessException
            or DllNotFoundException or EntryPointNotFoundException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return Array.Empty<MinecraftInstance>();
        }
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<MinecraftInstance> FindWindowsInstances(bool allowServer, CancellationToken cancellationToken)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT ProcessId, CommandLine, ExecutablePath FROM Win32_Process WHERE Name='javaw.exe' OR Name='java.exe'");
        using var results = searcher.Get();
        foreach (ManagementObject process in results)
        {
            using (process)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (process["CommandLine"] is not string commandLine)
                    continue;
                var arguments = SplitWindowsCommandLine(commandLine);
                var executable = process["ExecutablePath"] as string;
                var instance = FindInstance(arguments, executable);
                if (instance == null && allowServer && process["ProcessId"] is uint processId)
                    instance = FindServerInstance(arguments, executable, (int)processId);
                if (instance != null)
                    yield return instance;
            }
        }
    }

    private static IEnumerable<MinecraftInstance> FindUnixInstances(Func<int, string[]> readArguments,
        bool allowServer, CancellationToken cancellationToken)
    {
        foreach (var process in Process.GetProcessesByName("java"))
        {
            using (process)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[]? arguments = null;
                string? executable = null;
                try
                {
                    arguments = readArguments(process.Id);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The process may have exited or belong to a different user.
                }

                try
                {
                    executable = process.MainModule?.FileName;
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                }

                if (arguments == null)
                    continue;
                var instance = FindInstance(arguments, executable);
                if (instance == null && allowServer)
                    instance = FindServerInstance(arguments, executable, process.Id);
                if (instance != null)
                    yield return instance;
            }
        }
    }

    private static string[] ReadLinuxArguments(int pid) => File.ReadAllText($"/proc/{pid}/cmdline").Split('\0');

    private static string[] ReadMacArguments(int pid)
    {
        const int ctlKern = 1;
        const int kernProcArgs2 = 49;
        int[] request = [ctlKern, kernProcArgs2, pid];
        nuint length = 0;
        if (Sysctl(request, (uint)request.Length, null, ref length, IntPtr.Zero, 0) != 0 || length is < 4 or > 1024 * 1024)
            return Array.Empty<string>();

        var data = new byte[(int)length];
        if (Sysctl(request, (uint)request.Length, data, ref length, IntPtr.Zero, 0) != 0 || length > (nuint)data.Length)
            return Array.Empty<string>();

        return ParseMacArguments(data.AsSpan(0, (int)length));
    }

    internal static string[] ParseMacArguments(ReadOnlySpan<byte> data)
    {
        if (data.Length < sizeof(int))
            return Array.Empty<string>();

        var argumentCount = BitConverter.ToInt32(data);
        // The executable path and NUL padding precede argv. Ignore the environment after argv.
        data = data[sizeof(int)..];
        var end = data.IndexOf((byte)0);
        if (end < 0 || argumentCount <= 0 || argumentCount > data.Length)
            return Array.Empty<string>();

        data = data[(end + 1)..].TrimStart((byte)0);
        var arguments = new string[argumentCount];
        for (var i = 0; i < arguments.Length; i++)
        {
            end = data.IndexOf((byte)0);
            if (end < 0)
                return Array.Empty<string>();
            arguments[i] = Encoding.UTF8.GetString(data[..end]);
            data = data[(end + 1)..];
        }
        return arguments;
    }

    private static MinecraftInstance? FindInstance(string[] arguments, string? executable)
    {
        arguments = ExpandArgumentFiles(arguments);
        var version = GetOption(arguments, "--version");
        var gameDirectory = GetOption(arguments, "--gameDir");
        var classPath = new[] { GetOption(arguments, "-cp") ?? GetOption(arguments, "-classpath") ?? GetOption(arguments, "--class-path"),
                GetOption(arguments, "-DlegacyClassPath"), GetOption(arguments, "-p") ?? GetOption(arguments, "--module-path") }
            .OfType<string>().SelectMany(value => value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            .Append(GetOption(arguments, "-jar")).OfType<string>().Where(IsLocalAbsolutePath).ToArray();
        var isMinecraftClient = arguments.Any(argument => argument is "net.minecraft.client.main.Main"
            or "net.fabricmc.loader.impl.launch.knot.KnotClient" or "net.fabricmc.loader.launch.knot.KnotClient"
            or "org.quiltmc.loader.impl.launch.knot.KnotClient")
            || GetOption(arguments, "--launchTarget") is "forgeclient" or "fmlclient" or "neoforgeclient" or "forge_client"
            || GetOption(arguments, "--tweakClass") is "net.minecraftforge.fml.common.launcher.FMLTweaker"
                or "cpw.mods.fml.common.launcher.FMLTweaker";
        var knownGameDirectory = gameDirectory != null && IsLocalAbsolutePath(gameDirectory) && Directory.Exists(gameDirectory)
            ? Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameDirectory)) : null;
        if (isMinecraftClient && knownGameDirectory != null)
            return new MinecraftInstance(knownGameDirectory, arguments, classPath, executable);

        foreach (var jarPath in classPath)
        {
            if (!Path.GetExtension(jarPath).Equals(".jar", StringComparison.OrdinalIgnoreCase)
                || (!Path.GetFileNameWithoutExtension(jarPath).Equals(version, StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(jarPath))) != "versions")
                || !File.Exists(jarPath))
                continue;

            var parent = new FileInfo(jarPath).Directory;
            for (var level = 0; level < 4 && parent != null; level++, parent = parent.Parent)
            {
                var isMinecraftHome = parent.Name.Equals(".minecraft", StringComparison.OrdinalIgnoreCase)
                    || (OperatingSystem.IsMacOS() && parent.Name == "minecraft" && parent.Parent?.Name == "Application Support");
                if (!isMinecraftHome && !Directory.Exists(Path.Combine(parent.FullName, "mods")))
                    continue;
                // A launcher can share the version JAR while using a separate game directory for its mods.
                return new MinecraftInstance(knownGameDirectory ?? parent.FullName, arguments, classPath, executable);
            }
        }
        return null;
    }

    private static MinecraftInstance? FindServerInstance(string[] arguments, string? executable, int processId)
    {
        var jar = GetOption(arguments, "-jar");
        if (jar == null || !Path.GetExtension(jar).Equals(".jar", StringComparison.OrdinalIgnoreCase))
            return null;

        var directory = JavaProcessWorkingDirectory.Read(processId);
        if (directory == null || !IsLocalAbsolutePath(directory) || !Directory.Exists(directory))
            return null;

        var jarPath = Path.GetFullPath(jar, directory);
        if (!IsLocalAbsolutePath(jarPath) || !File.Exists(jarPath))
            return null;

        var release = MinecraftServerJar.Read(jarPath, directory);
        return release == null ? null : new MinecraftInstance(
            Path.TrimEndingDirectorySeparator(directory), arguments, [jarPath], executable, release);
    }

    private static string[] ExpandArgumentFiles(string[] arguments)
    {
        if (!arguments.Skip(1).Any(argument => argument.StartsWith('@')))
            return arguments;

        var expanded = new List<string> { arguments[0] };
        for (var i = 1; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            if (argument is "-jar" or "--disable-@files" || (argument.Length > 0 && !argument.StartsWith('-') && !argument.StartsWith('@')
                && expanded.LastOrDefault() is not ("-cp" or "-classpath" or "--class-path" or "-p" or "--module-path")))
            {
                expanded.AddRange(arguments.Skip(i));
                break;
            }
            if (argument.StartsWith('@') && !argument.StartsWith("@@", StringComparison.Ordinal)
                && IsLocalAbsolutePath(argument[1..]))
            {
                try
                {
                    var file = new FileInfo(argument[1..]);
                    if (file.Exists && file.Length <= 1024 * 1024 && (file.Attributes & FileAttributes.ReparsePoint) == 0)
                    {
                        expanded.AddRange(ParseArgumentFile(File.ReadAllText(file.FullName)));
                        continue;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }
            }
            expanded.Add(argument);
        }
        return expanded.ToArray();
    }

    private static IEnumerable<string> ParseArgumentFile(string content)
    {
        var token = new StringBuilder();
        char quote = '\0';
        for (var i = 0; i < content.Length; i++)
        {
            var current = content[i];
            if (quote == '\0' && current == '#' && token.Length == 0)
            {
                while (i + 1 < content.Length && content[i + 1] is not ('\n' or '\r'))
                    i++;
            }
            else if (quote == '\0' && char.IsWhiteSpace(current))
            {
                if (token.Length == 0)
                    continue;
                yield return token.ToString();
                token.Clear();
            }
            else if (quote == '\0' && current is ('\'' or '"'))
            {
                quote = current;
            }
            else if (current == quote)
            {
                quote = '\0';
            }
            else if (quote != '\0' && current == '\\' && i + 1 < content.Length
                && (content[i + 1] == quote || content[i + 1] == '\\'))
            {
                token.Append(content[++i]);
            }
            else
            {
                token.Append(current);
            }
        }
        if (token.Length > 0 && quote == '\0')
            yield return token.ToString();
    }

    internal static bool IsLocalAbsolutePath(string path)
    {
        if (!Path.IsPathFullyQualified(path))
            return false;
        if (!OperatingSystem.IsWindows())
            return true;
        var root = Path.GetPathRoot(path);
        return root?.Length == 3 && root[1] == ':' && new DriveInfo(root).DriveType != DriveType.Network;
    }

    internal static string? GetOption(string[] arguments, string name)
    {
        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i].Equals(name, StringComparison.Ordinal) && i + 1 < arguments.Length)
                return arguments[i + 1];
            if (arguments[i].StartsWith(name + "=", StringComparison.Ordinal))
                return arguments[i][(name.Length + 1)..];
        }
        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string[] SplitWindowsCommandLine(string commandLine)
    {
        var argv = CommandLineToArgvW(commandLine, out var count);
        if (argv == IntPtr.Zero)
            return Array.Empty<string>();

        try
        {
            var arguments = new string[count];
            for (var i = 0; i < count; i++)
                arguments[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, i * IntPtr.Size)) ?? string.Empty;
            return arguments;
        }
        finally
        {
            LocalFree(argv);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr CommandLineToArgvW(string commandLine, out int argumentCount);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr LocalFree(IntPtr memory);

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "sysctl")]
    private static extern int Sysctl([In] int[] name, uint nameLength, [Out] byte[]? value,
        ref nuint valueLength, IntPtr newValue, nuint newValueLength);

    private static async Task<ModManifest> ScanAsync(MinecraftInstance instance, CancellationToken cancellationToken,
        Action<int, int>? progress)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var versions = MinecraftVersionMetadata.Read(instance, cancellationToken);
        var modsDirectory = Path.Combine(instance.Directory, "mods");
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(modsDirectory);
        }
        catch (IOException ex) when ((ex is FileNotFoundException or DirectoryNotFoundException) && Directory.Exists(instance.Directory))
        {
            progress?.Invoke(0, 0);
            return versions.CreateManifest(Array.Empty<ModFile>()) with { ServerRelease = instance.ServerRelease };
        }
        if ((attributes & FileAttributes.Directory) == 0 || (attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("mods 路径不是普通目录。");

        var paths = new List<string>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(modsDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(path);
            if (!name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                continue;
            if (paths.Count == 1024 || name.Length > 255 || name.Any(c => char.IsControl(c) || c == '\\') || !names.Add(name))
                throw new IOException("模组数量或文件名超出校验支持的范围。");
            paths.Add(path);
        }

        var mods = new ModFile[paths.Count];
        progress?.Invoke(0, paths.Count);
        var updateTimer = Stopwatch.StartNew();
        var progressGate = new object();
        var completed = 0;
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4)
        };
        await Parallel.ForEachAsync(Enumerable.Range(0, paths.Count), options, async (index, token) =>
        {
            var path = paths[index];
            token.ThrowIfCancellationRequested();
            var stamp = ModMd5DiskCache.FileStamp.Read(path);
            var md5 = ModMd5DiskCache.TryRead(path, stamp);
            if (md5 == null)
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                md5 = Convert.ToHexString(await MD5.HashDataAsync(stream, token).ConfigureAwait(false));
                if (stamp != ModMd5DiskCache.FileStamp.Read(path))
                    throw new IOException("扫描时模组文件发生变化。");
                token.ThrowIfCancellationRequested();
                ModMd5DiskCache.Store(path, stamp, md5);
            }
            mods[index] = new ModFile(Path.GetFileName(path), md5);
            if (progress == null)
                return;

            lock (progressGate)
            {
                completed++;
                if (completed == paths.Count || updateTimer.ElapsedMilliseconds >= 125)
                {
                    progress(completed, paths.Count);
                    updateTimer.Restart();
                }
            }
        }).ConfigureAwait(false);

        Array.Sort(mods, (left, right) => StringComparer.Ordinal.Compare(left.FileName, right.FileName));
        return versions.CreateManifest(mods) with { ServerRelease = instance.ServerRelease };
    }
}
