namespace LTESystemMonitoring.Configurations;

public class MonitoringConfiguration
{
    public string[] MonitoredProcesses { get; set; } = [];

    public int CpuSampleMilliseconds { get; set; } = 500;
}
