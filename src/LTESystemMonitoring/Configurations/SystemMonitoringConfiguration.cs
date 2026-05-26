namespace LTESystemMonitoring.Configurations;

public class SystemMonitoringConfiguration
{
    public string[] MonitoredProcesses { get; set; } = [];

    public int CpuSampleMilliseconds { get; set; } = 500;
}
