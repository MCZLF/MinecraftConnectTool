using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace MinecraftConnectTool.Services;

internal sealed class MinecraftVersionMetadata
{
    private const int MaxMetadataSize = 1024 * 1024;
    private string? _gameVersion;
    private string? _loader;
    private string? _loaderVersion;
    private string? _fmlVersion;
    private string? _mcpVersion;
    private string? _neoFormVersion;
    private string? _javaVersion;

    public ModManifest CreateManifest(ModFile[] mods) =>
        new(_gameVersion, mods, _loader, _loaderVersion, _fmlVersion, _mcpVersion, _neoFormVersion, _javaVersion);

    public static MinecraftVersionMetadata Read(MinecraftInstance instance, CancellationToken cancellationToken)
    {
        var metadata = new MinecraftVersionMetadata();
        var args = instance.Arguments;
        string? Option(string name) => VersionToken(MinecraftModScanner.GetOption(args, name));
        metadata.ReadLaunchVersions(args);
        metadata._gameVersion ??= instance.ServerRelease?.GameVersion;

        var versionJars = instance.ClassPath.Where(jar => Path.GetExtension(jar).Equals(".jar", StringComparison.OrdinalIgnoreCase)
            && Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(jar))) == "versions").ToArray();
        var versionRoots = versionJars.Select(jar => Path.GetDirectoryName(Path.GetDirectoryName(jar))!)
            .Prepend(Path.Combine(instance.Directory, "versions"))
            .Append(Path.GetDirectoryName(instance.Directory) ?? instance.Directory)
            .Append(GetVersionRootFromLibraryPath(args) ?? instance.Directory)
            .Where(root => Path.GetFileName(root) == "versions")
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();

        foreach (var path in instance.ClassPath)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = new FileInfo(path);
            if (file.Directory?.Parent is { } artifact && file.Name.StartsWith(artifact.Name + "-", StringComparison.Ordinal))
                metadata.ReadDependency(artifact.Parent?.FullName.Replace('\\', '/'), artifact.Name, file.Directory.Name);
        }

        var profileId = Option("--version");
        var profile = profileId == null ? null : FindProfile(instance.Directory, versionRoots, profileId);
        if (profile == null && Path.GetFileName(Path.GetDirectoryName(instance.Directory)) == "versions"
            && VersionToken(Path.GetFileName(instance.Directory)) is { } instanceId)
            profile = FindProfile(instance.Directory, versionRoots, instanceId);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (profile != null && visited.Count < 8 && visited.Add(profile))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var json = ReadJsonFile(profile);
            if (json == null)
                break;

            var root = json.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("arguments", out var arguments)
                && arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty("game", out var game)
                && game.ValueKind == JsonValueKind.Array)
            {
                metadata.ReadLaunchVersions(game.EnumerateArray()
                    .Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "").ToArray());
            }
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("libraries", out var libraries)
                && libraries.ValueKind == JsonValueKind.Array)
            {
                foreach (var library in libraries.EnumerateArray())
                {
                    var coordinate = ReadString(library, "name")?.Split(':');
                    if (coordinate is { Length: >= 3 })
                        metadata.ReadDependency(coordinate[0].Replace('.', '/'), coordinate[1], coordinate[2]);
                }
            }

            var parent = VersionToken(ReadString(root, "inheritsFrom")) ?? VersionToken(ReadString(root, "jar"));
            if (parent == null)
                break;
            metadata._gameVersion ??= MinecraftRelease(parent);
            profile = FindProfile(instance.Directory, versionRoots, parent);
        }

        foreach (var path in versionJars)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (metadata._gameVersion != null)
                break;
            metadata._gameVersion = ReadJarVersion(path);
        }
        if (metadata._gameVersion == null && MinecraftRelease(profileId) is { } clientVersion)
            metadata._gameVersion = clientVersion;

        if (metadata._loader == null)
        {
            if (args.Any(arg => arg.Contains(".fabricmc.loader.", StringComparison.Ordinal) && arg.EndsWith(".KnotClient", StringComparison.Ordinal)))
                metadata.SetLoader("fabric", null);
            else if (args.Any(arg => arg == "org.quiltmc.loader.impl.launch.knot.KnotClient"))
                metadata.SetLoader("quilt", null);
            else if (Option("--launchTarget") is "neoforgeclient")
                metadata.SetLoader("neoforge", null);
            else if (Option("--launchTarget") is "forgeclient" or "forge_client" or "fmlclient")
                metadata.SetLoader("forge", null);
            else if (args.Contains("net.minecraft.client.main.Main") && profileId == metadata._gameVersion && profileId != null)
                metadata.SetLoader("vanilla", null);
        }

        metadata._javaVersion = ReadJavaVersion(instance.JavaExecutable ?? args.FirstOrDefault());
        return metadata;
    }

    private void ReadLaunchVersions(string[] arguments)
    {
        string? Option(string name) => VersionToken(MinecraftModScanner.GetOption(arguments, name));
        _gameVersion ??= Option("--fml.mcVersion") ?? Option("--fml.minecraftVersion");
        _fmlVersion ??= Option("--fml.fmlVersion");
        _mcpVersion ??= Option("--fml.mcpVersion") ?? Option("--mcpVersion");
        _neoFormVersion ??= Option("--fml.neoFormVersion");
        var neoForge = Option("--fml.neoForgeVersion") ?? Option("--fml.neoforgeVersion");
        var forge = Option("--fml.forgeVersion");
        if (neoForge != null)
            SetLoader("neoforge", neoForge);
        else if (forge != null)
            SetLoader("forge", forge);
    }

    private void ReadDependency(string? groupPath, string artifact, string version)
    {
        if (groupPath == null || VersionToken(version) == null)
            return;

        bool IsGroup(string group) => groupPath == group || groupPath.EndsWith("/" + group, StringComparison.Ordinal);
        if (artifact == "loader" && IsGroup("net/neoforged/fancymodloader"))
            _fmlVersion ??= version;
        if (artifact == "client" && IsGroup("net/minecraft"))
            _gameVersion ??= MinecraftRelease(version) ?? MinecraftRelease(version.Split('-', 2)[0]);
        if (artifact == "intermediary" && IsGroup("net/fabricmc"))
            _gameVersion ??= MinecraftRelease(version);

        var loader = artifact switch
        {
            "forge" when IsGroup("net/minecraftforge") => "forge",
            "forge" or "neoforge" when IsGroup("net/neoforged") => "neoforge",
            "fabric-loader" when IsGroup("net/fabricmc") => "fabric",
            "quilt-loader" when IsGroup("org/quiltmc") => "quilt",
            _ => null
        };
        if (loader == null)
            return;

        if (artifact == "forge")
        {
            var separator = version.IndexOf('-');
            if (separator > 0 && MinecraftRelease(version[..separator]) is { } minecraft)
            {
                _gameVersion ??= minecraft;
                version = version[(separator + 1)..];
                if (version.EndsWith("-" + minecraft, StringComparison.Ordinal))
                    version = version[..^(minecraft.Length + 1)];
            }
        }
        SetLoader(loader, VersionToken(version));
    }

    private static string? GetVersionRootFromLibraryPath(string[] args)
    {
        var libraries = MinecraftModScanner.GetOption(args, "-DlibraryDirectory");
        if (libraries == null || !MinecraftModScanner.IsLocalAbsolutePath(libraries))
            return null;
        libraries = Path.TrimEndingDirectorySeparator(libraries);
        var parent = Path.GetDirectoryName(libraries);
        return Path.GetFileName(libraries) == "libraries" && parent != null ? Path.Combine(parent, "versions") : null;
    }

    private void SetLoader(string loader, string? version)
    {
        _loader ??= loader;
        if (_loader == loader)
            _loaderVersion ??= version;
    }

    private static string? FindProfile(string gameDirectory, string[] roots, string id)
    {
        var local = Path.Combine(gameDirectory, id + ".json");
        if (File.Exists(local))
            return local;

        return roots.Select(root => Path.Combine(root, id, id + ".json")).FirstOrDefault(File.Exists);
    }

    private static string? ReadJarVersion(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            return ReadJarVersion(archive);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            return null;
        }
    }

    internal static string? ReadJarVersion(ZipArchive archive)
    {
        var entry = archive.GetEntry("version.json");
        if (entry == null || entry.Length is <= 0 or > MaxMetadataSize)
            return null;
        using var stream = entry.Open();
        using var json = JsonDocument.Parse(stream);
        return MinecraftRelease(ReadString(json.RootElement, "id")) ?? MinecraftRelease(ReadString(json.RootElement, "name"));
    }

    private static JsonDocument? ReadJsonFile(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return stream.Length is > 0 and <= MaxMetadataSize ? JsonDocument.Parse(stream) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static string? ReadJavaVersion(string? executable)
    {
        if (executable == null || !MinecraftModScanner.IsLocalAbsolutePath(executable))
            return null;
        try
        {
            var binary = new FileInfo(executable);
            var resolved = binary.ResolveLinkTarget(returnFinalTarget: true) ?? binary;
            var home = Path.GetDirectoryName(Path.GetDirectoryName(resolved.FullName));
            if (home == null)
                return null;
            var release = new FileInfo(Path.Combine(home, "release"));
            if (!release.Exists || release.Length > 64 * 1024)
                return null;
            const string key = "JAVA_VERSION=";
            var line = File.ReadLines(release.FullName).FirstOrDefault(value => value.StartsWith(key, StringComparison.Ordinal));
            return line == null ? null : VersionToken(line[key.Length..].Trim('"'));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value)
            && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? VersionToken(string? value) =>
        value is { Length: > 0 and <= 128 } && value is not ("." or "..")
            && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or '+') ? value : null;

    private static string? MinecraftRelease(string? value)
    {
        value = VersionToken(value);
        if (value == null)
            return null;

        if (value.Length >= 6 && value[2] == 'w' && char.IsAsciiDigit(value[0]) && char.IsAsciiDigit(value[1])
            && char.IsAsciiDigit(value[3]) && char.IsAsciiDigit(value[4]) && char.IsAsciiLetter(value[5]))
            return value;

        var parts = value.Split('-', 2);
        var numbers = parts[0].Split('.');
        var release = numbers.Length is >= 2 and <= 3 && numbers.All(number => number.Length > 0 && number.All(char.IsAsciiDigit));
        if (!release)
            return null;
        if (parts.Length == 1)
            return value;
        var suffix = parts[1];
        return suffix.StartsWith("pre", StringComparison.Ordinal) || suffix.StartsWith("rc", StringComparison.Ordinal)
            || suffix.StartsWith("snapshot", StringComparison.Ordinal) ? value : null;
    }
}
