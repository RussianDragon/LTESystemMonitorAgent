using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using LTESM.DAL.Abstractions;
using LTESM.DAL.Abstractions.Entities;
using LTESystemMonitoring.Abstractions;
using LTESystemMonitoring.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LTESystemMonitoring;

public class MetricCollectionService(
    ILTEDbContext dbContext,
    IOptions<SystemMonitoringConfiguration> options,
    ILogger<MetricCollectionService> logger) : IMetricCollectionService
{
    public async Task CollectAndSaveAsync(CancellationToken cancellationToken = default)
    {
        var collectedAtUtc = DateTimeOffset.UtcNow;
        var cpuUsagePercent = await GetCpuUsagePercentAsync(options.Value.CpuSampleMilliseconds, cancellationToken);
        var memoryStatus = GetMemoryStatus();
        var runningProcesses = GetRunningProcesses();

        var metric = new Metric
        {
            CollectedAtUtc = collectedAtUtc,
            Hostname = Environment.MachineName,
            WindowsVersion = RuntimeInformation.OSDescription,
            UptimeSeconds = Environment.TickCount64 / 1000,
            CpuUsagePercent = cpuUsagePercent,
            RamUsagePercent = memoryStatus.UsagePercent,
            TotalMemoryBytes = memoryStatus.TotalBytes,
            AvailableMemoryBytes = memoryStatus.AvailableBytes,
            IpAddresses = GetIpAddresses(),
            DiskSpaces = GetDiskSpaces(),
            RunningProcesses = runningProcesses,
            MonitoredProcesses = GetMonitoredProcesses(runningProcesses),
            OutboxMessage = new MetricOutboxMessage
            {
                CreatedAtUtc = collectedAtUtc,
                Status = OutboxMessageStatus.Pending
            }
        };

        dbContext.Metrics.Add(metric);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("System metric snapshot saved. MetricId: {MetricId}.", metric.Id);
    }

    private async Task<double> GetCpuUsagePercentAsync(int sampleMilliseconds, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogWarning("CPU usage collection is implemented only for Windows.");
            return 0;
        }

        var sampleDelay = TimeSpan.FromMilliseconds(Math.Max(sampleMilliseconds, 100));
        var first = CpuTimes.Read();

        await Task.Delay(sampleDelay, cancellationToken);

        var second = CpuTimes.Read();
        var idleDelta = second.Idle - first.Idle;
        var totalDelta = second.Total - first.Total;

        if (totalDelta == 0)
        {
            return 0;
        }

        var usagePercent = (1.0 - idleDelta / (double)totalDelta) * 100;
        return Math.Round(Math.Clamp(usagePercent, 0, 100), 2);
    }

    private MemoryStatus GetMemoryStatus()
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogWarning("RAM usage collection is implemented only for Windows.");
            return new MemoryStatus(0, 0, 0);
        }

        var memoryStatus = new NativeMemoryStatus
        {
            Length = (uint)Marshal.SizeOf<NativeMemoryStatus>()
        };

        if (!GlobalMemoryStatusEx(ref memoryStatus))
        {
            logger.LogWarning("Failed to collect RAM usage. Win32Error: {Win32Error}.", Marshal.GetLastWin32Error());
            return new MemoryStatus(0, 0, 0);
        }

        return new MemoryStatus(
            (long)memoryStatus.TotalPhysical,
            (long)memoryStatus.AvailablePhysical,
            memoryStatus.MemoryLoad);
    }

    private static ICollection<MetricIpAddress> GetIpAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up)
            .Where(networkInterface => networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses
                .Where(address => address.Address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                .Where(address => !IPAddress.IsLoopback(address.Address))
                .Where(address => !address.Address.IsIPv6LinkLocal)
                .Select(address => new MetricIpAddress
                {
                    Address = address.Address.ToString(),
                    AddressFamily = address.Address.AddressFamily.ToString(),
                    NetworkInterfaceName = networkInterface.Name
                }))
            .ToList();
    }

    private static ICollection<MetricDiskSpace> GetDiskSpaces()
    {
        return DriveInfo.GetDrives()
            .Where(drive => drive.IsReady)
            .Select(drive => new MetricDiskSpace
            {
                Name = drive.Name,
                VolumeLabel = drive.VolumeLabel,
                DriveFormat = drive.DriveFormat,
                TotalSpaceBytes = drive.TotalSize,
                FreeSpaceBytes = drive.AvailableFreeSpace
            })
            .ToList();
    }

    private ICollection<MetricProcess> GetRunningProcesses()
    {
        using var processScope = new ProcessScope(Process.GetProcesses());

        return processScope.Processes
            .Select(CreateMetricProcess)
            .Where(process => process is not null)
            .Select(process => process!)
            .ToList();
    }

    private MetricProcess? CreateMetricProcess(Process process)
    {
        try
        {
            return new MetricProcess
            {
                ProcessId = process.Id,
                Name = process.ProcessName,
                StartedAtUtc = TryGetStartedAtUtc(process),
                WorkingSetBytes = TryGetWorkingSetBytes(process)
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            logger.LogDebug(exception, "Failed to read process information.");
            return null;
        }
    }

    private ICollection<MetricMonitoredProcess> GetMonitoredProcesses(ICollection<MetricProcess> runningProcesses)
    {
        return options.Value.MonitoredProcesses
            .Where(processName => !string.IsNullOrWhiteSpace(processName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(processName =>
            {
                var normalizedName = NormalizeProcessName(processName);
                var matchedCount = runningProcesses.Count(process =>
                    string.Equals(NormalizeProcessName(process.Name), normalizedName, StringComparison.OrdinalIgnoreCase));

                return new MetricMonitoredProcess
                {
                    Name = processName,
                    IsRunning = matchedCount > 0,
                    MatchedProcessCount = matchedCount
                };
            })
            .ToList();
    }

    private static DateTimeOffset? TryGetStartedAtUtc(Process process)
    {
        try
        {
            return process.StartTime.ToUniversalTime();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static long? TryGetWorkingSetBytes(Process process)
    {
        try
        {
            return process.WorkingSet64;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static string NormalizeProcessName(string processName)
    {
        return Path.GetFileNameWithoutExtension(processName.Trim());
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out NativeFileTime idleTime, out NativeFileTime kernelTime, out NativeFileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref NativeMemoryStatus memoryStatus);

    private sealed class ProcessScope(Process[] processes) : IDisposable
    {
        public IReadOnlyCollection<Process> Processes { get; } = processes;

        public void Dispose()
        {
            foreach (var process in Processes)
            {
                process.Dispose();
            }
        }
    }

    private readonly record struct MemoryStatus(long TotalBytes, long AvailableBytes, double UsagePercent);

    private readonly record struct CpuTimes(ulong Idle, ulong Kernel, ulong User)
    {
        public ulong Total => Kernel + User;

        public static CpuTimes Read()
        {
            if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            {
                throw new InvalidOperationException($"Failed to collect CPU times. Win32Error: {Marshal.GetLastWin32Error()}.");
            }

            return new CpuTimes(idleTime.ToUInt64(), kernelTime.ToUInt64(), userTime.ToUInt64());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeFileTime
    {
        private readonly uint lowDateTime;
        private readonly uint highDateTime;

        public ulong ToUInt64()
        {
            return ((ulong)highDateTime << 32) | lowDateTime;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
