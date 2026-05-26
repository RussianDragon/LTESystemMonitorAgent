namespace LTESystemOutbox.Configurations;

public class SystemOutboxConfiguration
{
    public required string ApiUrl { get; set; } = string.Empty;

    public int HttpTimeoutSeconds { get; set; } = 10;

    public int BatchSize { get; set; } = 10;
}
