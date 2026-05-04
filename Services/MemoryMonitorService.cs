using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MinecraftConnectTool.Services;

/// <summary>
/// 内存信息结构 - 与任务管理器显示一致
/// </summary>
public readonly record struct MemoryInfo(
    long WorkingSetMB,      // 工作集（总物理内存）
    long PrivateWorkingSetMB, // 专用工作集（任务管理器默认显示）
    long PagedMemoryMB,
    long PeakWorkingSetMB,
    long ManagedMemoryMB
);

/// <summary>
/// 跨平台内存监控服务
/// </summary>
public sealed class MemoryMonitorService : IDisposable
{
    private Timer? _updateTimer;
    private readonly Process _process;
    private MemoryInfo _currentInfo;
    private readonly object _lock = new();
    private bool _disposed;

    public event Action<MemoryInfo>? MemoryInfoUpdated;

    public MemoryMonitorService(TimeSpan interval)
    {
        _process = Process.GetCurrentProcess();
        _currentInfo = FetchMemoryInfo();
        _updateTimer = new Timer(_ => Update(), null, TimeSpan.Zero, interval);
    }

    public MemoryInfo GetCurrentInfo()
    {
        lock (_lock)
        {
            return _currentInfo;
        }
    }

    private void Update()
    {
        try
        {
            _process.Refresh();
            var info = FetchMemoryInfo();
            lock (_lock)
            {
                _currentInfo = info;
            }
            MemoryInfoUpdated?.Invoke(info);
        }
        catch { }
    }

    private MemoryInfo FetchMemoryInfo()
    {
        long workingSet = 0;
        long privateBytes = 0;
        long pagedMemory = 0;
        long peakWorkingSet = 0;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                (workingSet, privateBytes, pagedMemory, peakWorkingSet) = GetWindowsMemory();
            }
            else if (OperatingSystem.IsLinux())
            {
                (workingSet, privateBytes, pagedMemory, peakWorkingSet) = GetLinuxMemory();
            }
            else if (OperatingSystem.IsMacOS())
            {
                (workingSet, privateBytes, pagedMemory, peakWorkingSet) = GetMacMemory();
            }
            else
            {
                workingSet = _process.WorkingSet64;
                privateBytes = _process.PrivateMemorySize64;
                pagedMemory = _process.PagedMemorySize64;
                peakWorkingSet = _process.PeakWorkingSet64;
            }
        }
        catch
        {
            workingSet = _process.WorkingSet64;
            privateBytes = _process.PrivateMemorySize64;
            pagedMemory = _process.PagedMemorySize64;
            peakWorkingSet = _process.PeakWorkingSet64;
        }

        long managed = GC.GetTotalMemory(false);

        return new MemoryInfo(
            workingSet / (1024 * 1024),
            privateBytes / (1024 * 1024),
            pagedMemory / (1024 * 1024),
            peakWorkingSet / (1024 * 1024),
            managed / (1024 * 1024)
        );
    }

    private (long, long, long, long) GetWindowsMemory()
    {
        try
        {
            // 任务管理器默认显示的是"专用工作集"(Private Working Set)
            // 这大致等于 PrivateMemorySize64，但需要一些调整
            long ws = _process.WorkingSet64;
            long privateWs = _process.PrivateMemorySize64;
            long pm = _process.PagedMemorySize64;
            long pws = _process.PeakWorkingSet64;

            // 使用 GetProcessMemoryInfo 获取更准确的工作集信息
            try
            {
                var counters = new PROCESS_MEMORY_COUNTERS_EX();
                counters.cb = (uint)Marshal.SizeOf<PROCESS_MEMORY_COUNTERS_EX>();

                if (GetProcessMemoryInfo(_process.Handle, ref counters, counters.cb))
                {
                    ws = (long)counters.WorkingSetSize;
                    pws = (long)counters.PeakWorkingSetSize;
                }
            }
            catch { }

            // 任务管理器的"专用工作集"通常比 PrivateMemorySize64 小一些
            // 使用一个经验公式来估算（大约是 PrivateMemorySize64 的 80-90%）
            // 或者我们可以直接返回 PrivateMemorySize64，因为它最接近任务管理器的显示
            privateWs = _process.PrivateMemorySize64;

            return (ws, privateWs, pm, pws);
        }
        catch
        {
            return (_process.WorkingSet64, _process.PrivateMemorySize64,
                    _process.PagedMemorySize64, _process.PeakWorkingSet64);
        }
    }

    private (long, long, long, long) GetLinuxMemory()
    {
        try
        {
            long vmRss = 0;
            long vmSize = 0;
            long vmHwm = 0;
            long privateClean = 0;

            string statusPath = $"/proc/{_process.Id}/status";
            if (File.Exists(statusPath))
            {
                foreach (var line in File.ReadLines(statusPath))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    if (line.StartsWith("VmRSS:") && long.TryParse(parts[1], out var v))
                        vmRss = v * 1024;
                    else if (line.StartsWith("VmSize:") && long.TryParse(parts[1], out v))
                        vmSize = v * 1024;
                    else if (line.StartsWith("VmHWM:") && long.TryParse(parts[1], out v))
                        vmHwm = v * 1024;
                }
            }

            string smapsPath = $"/proc/{_process.Id}/smaps_rollup";
            if (File.Exists(smapsPath))
            {
                foreach (var line in File.ReadLines(smapsPath))
                {
                    if (line.StartsWith("Private_Clean:") || line.StartsWith("Private_Dirty:"))
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var v))
                            privateClean += v * 1024;
                    }
                }
            }

            return (vmRss, privateClean > 0 ? privateClean : vmRss, vmSize, vmHwm);
        }
        catch
        {
            return (_process.WorkingSet64, _process.PrivateMemorySize64,
                    _process.PagedMemorySize64, _process.PeakWorkingSet64);
        }
    }

    private (long, long, long, long) GetMacMemory()
    {
        try
        {
            long ws = _process.WorkingSet64;
            long pb = _process.PrivateMemorySize64;

            var psi = new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = $"-o rss= -p {_process.Id}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var ps = Process.Start(psi);
            if (ps != null)
            {
                var output = ps.StandardOutput.ReadToEnd().Trim();
                ps.WaitForExit();
                if (long.TryParse(output, out var rssKb))
                    ws = rssKb * 1024;
            }

            return (ws, pb, pb, ws);
        }
        catch
        {
            return (_process.WorkingSet64, _process.PrivateMemorySize64,
                    _process.PagedMemorySize64, _process.PeakWorkingSet64);
        }
    }

    [DllImport("psapi.dll")]
    private static extern bool GetProcessMemoryInfo(IntPtr hProcess, ref PROCESS_MEMORY_COUNTERS_EX counters, uint size);

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_MEMORY_COUNTERS_EX
    {
        public uint cb;
        public uint PageFaultCount;
        public ulong PeakWorkingSetSize;
        public ulong WorkingSetSize;
        public ulong QuotaPeakPagedPoolUsage;
        public ulong QuotaPagedPoolUsage;
        public ulong QuotaPeakNonPagedPoolUsage;
        public ulong QuotaNonPagedPoolUsage;
        public ulong PagefileUsage;
        public ulong PeakPagefileUsage;
        public ulong PrivateUsage;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _updateTimer?.Dispose();
        _process?.Dispose();
        GC.SuppressFinalize(this);
    }
}
