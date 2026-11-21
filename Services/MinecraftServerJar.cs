using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace MinecraftConnectTool.Services;

internal sealed record MinecraftServerRelease(string? GameVersion, string? Brand, string? Build)
{
    public string? DisplayBuild => Brand == null ? null : string.Join(" ", new[] { Brand, Build }.OfType<string>());
}

internal static class MinecraftServerJar
{
    internal static MinecraftServerRelease? Read(string jarPath, string directory)
    {
        try
        {
            using var archive = ZipFile.OpenRead(jarPath);
            var manifest = ReadManifest(archive);
            var mainClass = manifest.GetValueOrDefault("Main-Class");
            if (mainClass is not ("io.papermc.paperclip.Main" or "io.papermc.paperclip.Paperclip"
                or "net.minecraft.bundler.Main" or "net.minecraft.server.Main"
                or "org.bukkit.craftbukkit.Main" or "net.fabricmc.loader.impl.launch.server.FabricServerLauncher"
                or "org.quiltmc.loader.impl.launch.server.QuiltServerLauncher"))
                return null;

            var gameVersion = MinecraftVersionMetadata.ReadJarVersion(archive);
            var versions = archive.GetEntry("META-INF/versions.list");
            if (versions is { Length: > 0 and <= 64 * 1024 })
            {
                using var reader = new StreamReader(versions.Open());
                while (reader.ReadLine() is { } line)
                {
                    var fields = line.Split('\t');
                    if (fields.Length != 3 || fields[0].Length != 64 || !fields[0].All(Uri.IsHexDigit))
                        continue;
                    var innerPath = GetCorePath(directory, fields[2]);
                    if (innerPath == null || !File.Exists(innerPath))
                        continue;

                    using var stream = File.OpenRead(innerPath);
                    if (!Convert.ToHexString(SHA256.HashData(stream)).Equals(fields[0], StringComparison.OrdinalIgnoreCase))
                        continue;
                    stream.Position = 0;
                    using var core = new ZipArchive(stream);
                    var coreManifest = ReadManifest(core);
                    gameVersion = MinecraftVersionMetadata.ReadJarVersion(core) ?? gameVersion;
                    return new MinecraftServerRelease(gameVersion, GetBrand(coreManifest), GetBuild(coreManifest));
                }
            }

            return new MinecraftServerRelease(gameVersion, GetBrand(manifest), GetBuild(manifest));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException
            or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string? GetCorePath(string directory, string path)
    {
        var segments = path.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".." || segment.IndexOfAny(['\\', ':']) >= 0))
            return null;
        var core = Path.GetFullPath(Path.Combine(directory, "versions", Path.Combine(segments)));
        return MinecraftModScanner.IsLocalAbsolutePath(core) ? core : null;
    }

    private static Dictionary<string, string> ReadManifest(ZipArchive jar)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (jar.GetEntry("META-INF/MANIFEST.MF") is not { Length: > 0 and <= 64 * 1024 } entry)
            return fields;

        using var reader = new StreamReader(entry.Open());
        string? field = null;
        while (reader.ReadLine() is { Length: > 0 } line)
        {
            if (line[0] == ' ' && field != null)
            {
                fields[field] += line[1..];
                continue;
            }

            var separator = line.IndexOf(": ", StringComparison.Ordinal);
            field = separator > 0 ? line[..separator] : null;
            if (field != null)
                fields[field] = line[(separator + 2)..];
        }
        return fields;
    }

    private static string? GetBrand(Dictionary<string, string> fields) =>
        DisplayValue(fields.GetValueOrDefault("Brand-Name") ?? fields.GetValueOrDefault("Implementation-Title"));

    private static string? GetBuild(Dictionary<string, string> fields) =>
        DisplayValue(fields.GetValueOrDefault("Implementation-Version") ?? fields.GetValueOrDefault("Specification-Version"));

    private static string? DisplayValue(string? value) =>
        value is { Length: > 0 and <= 128 } && !value.Any(char.IsControl) ? value : null;
}
