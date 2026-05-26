namespace LTESM.DAL.SQLite.Configurations;

public class DatabaseConfiguration
{
    public required string ConnectionString { get; set; } = string.Empty;

    public bool LoggingEnabled { get; set; }
}
