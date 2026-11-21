using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MinecraftConnectTool.Services;

internal static class ModMd5DiskCache
{
    private static readonly object Gate = new();
    private static string? _directory;
    private static bool _closed;

    static ModMd5DiskCache() => AppDomain.CurrentDomain.ProcessExit += (_, _) => Cleanup();

    internal readonly record struct FileStamp(long Length, long LastWriteTicks, long CreationTicks)
    {
        public static FileStamp Read(string path)
        {
            var file = new FileInfo(path);
            if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("mods 目录包含指向其他位置的 JAR 文件。");
            return new FileStamp(file.Length, file.LastWriteTimeUtc.Ticks, file.CreationTimeUtc.Ticks);
        }
    }

    public static string? TryRead(string path, FileStamp stamp)
    {
        lock (Gate)
        {
            if (_closed || _directory == null)
                return null;

            try
            {
                var entry = EntryPath(_directory, path);
                if (!File.Exists(entry))
                    return null;
                using var reader = new BinaryReader(File.OpenRead(entry));
                if (reader.BaseStream.Length != 3 * sizeof(long) + 1 + 32)
                    return null;
                var saved = new FileStamp(reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64());
                if (stamp != saved)
                    return null;
                var md5 = reader.ReadString();
                return md5.Length == 32 && md5.All(Uri.IsHexDigit) ? md5 : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
            {
                return null;
            }
        }
    }

    public static void Store(string path, FileStamp stamp, string md5)
    {
        lock (Gate)
        {
            if (_closed)
                return;

            try
            {
                _directory ??= Path.Combine(TempRunLogService.BaseDirectory,
                    $"ModMd5-{Environment.ProcessId}-{Guid.NewGuid():N}");
                Directory.CreateDirectory(_directory);
                using var writer = new BinaryWriter(File.Create(EntryPath(_directory, path)));
                writer.Write(stamp.Length);
                writer.Write(stamp.LastWriteTicks);
                writer.Write(stamp.CreationTicks);
                writer.Write(md5);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    internal static void Cleanup()
    {
        lock (Gate)
        {
            _closed = true;
            if (_directory == null || !Directory.Exists(_directory))
                return;

            try
            {
                foreach (var entry in Directory.EnumerateFiles(_directory, "*.md5"))
                    File.Delete(entry);
                Directory.Delete(_directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static string EntryPath(string directory, string path) =>
        Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path))) + ".md5");
}
