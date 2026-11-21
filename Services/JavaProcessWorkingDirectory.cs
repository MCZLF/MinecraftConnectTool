using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace MinecraftConnectTool.Services;

internal static class JavaProcessWorkingDirectory
{
    public static string? Read(int processId)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                return ReadWindows(processId);
            if (OperatingSystem.IsLinux())
                return Directory.ResolveLinkTarget($"/proc/{processId}/cwd", returnFinalTarget: false)?.FullName;
            if (OperatingSystem.IsMacOS())
                return ReadMac(processId);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException)
        {
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadWindows(int processId)
    {
        const uint processQueryAndRead = 0x0400 | 0x0010;
        using var process = OpenProcess(processQueryAndRead, false, processId);
        if (process.IsInvalid)
            return null;

        // Read the target PEB's process parameters, including the 32-bit PEB for WOW64.
        const int processBasicInformation = 0;
        const int processWow64Information = 26;
        var info = new byte[6 * IntPtr.Size];
        var pointerSize = IntPtr.Size;
        IntPtr peb;
        if (pointerSize == 8 && NtQueryInformationProcess(process, processWow64Information, info, IntPtr.Size, out _) == 0
            && ReadPointer(info, 0, IntPtr.Size) is var wow64Peb && wow64Peb != IntPtr.Zero)
        {
            peb = wow64Peb;
            pointerSize = 4;
        }
        else
        {
            if (NtQueryInformationProcess(process, processBasicInformation, info, info.Length, out _) != 0)
                return null;
            peb = ReadPointer(info, IntPtr.Size, IntPtr.Size);
        }

        const int parametersOffset64 = 0x20;
        const int parametersOffset32 = 0x10;
        var parametersOffset = pointerSize == 8 ? parametersOffset64 : parametersOffset32;
        if (peb == IntPtr.Zero || !ReadMemory(process, peb + parametersOffset, pointerSize, out var parametersBuffer))
            return null;
        var parameters = ReadPointer(parametersBuffer, 0, pointerSize);
        const int directoryOffset64 = 0x38;
        const int directoryOffset32 = 0x24;
        var directoryOffset = pointerSize == 8 ? directoryOffset64 : directoryOffset32;
        if (parameters == IntPtr.Zero || !ReadMemory(process, parameters + directoryOffset, 2 * pointerSize, out var directory))
            return null;
        var length = BitConverter.ToUInt16(directory, 0);
        var capacity = BitConverter.ToUInt16(directory, 2);
        var address = ReadPointer(directory, pointerSize, pointerSize);
        if (length == 0 || length > capacity || length % 2 != 0 || address == IntPtr.Zero
            || !ReadMemory(process, address, length, out var path))
            return null;

        return Encoding.Unicode.GetString(path);
    }

    private static IntPtr ReadPointer(byte[] data, int offset, int pointerSize) => pointerSize == 8
        ? new IntPtr(BitConverter.ToInt64(data, offset))
        : (IntPtr)(nuint)BitConverter.ToUInt32(data, offset);

    private static bool ReadMemory(SafeProcessHandle process, IntPtr address, int length, out byte[] buffer)
    {
        buffer = new byte[length];
        return ReadProcessMemory(process, address, buffer, (nuint)length, out var read) && read == (nuint)length;
    }

    [SupportedOSPlatform("macos")]
    private static string? ReadMac(int processId)
    {
        const int pidVnodePathInfo = 9;
        const int vnodeInfoSize = 152;
        const int maxPathLength = 1024;
        // proc_vnodepathinfo contains one vnode_info_path each for cwd and root.
        var data = new byte[2 * (vnodeInfoSize + maxPathLength)];
        if (proc_pidinfo(processId, pidVnodePathInfo, 0, data, data.Length) != data.Length)
            return null;

        var path = data.AsSpan(vnodeInfoSize, maxPathLength);
        var length = path.IndexOf((byte)0);
        return length > 0 ? Encoding.UTF8.GetString(path[..length]) : null;
    }

    [DllImport("kernel32.dll")]
    private static extern SafeProcessHandle OpenProcess(uint access, bool inheritHandle, int processId);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(SafeProcessHandle process, int informationClass,
        [Out] byte[] info, int size, out int returnedSize);

    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(SafeProcessHandle process, IntPtr address, [Out] byte[] buffer,
        nuint size, out nuint bytesRead);

    [DllImport("/usr/lib/libproc.dylib")]
    private static extern int proc_pidinfo(int pid, int flavor, ulong arg, [Out] byte[] buffer, int size);
}
