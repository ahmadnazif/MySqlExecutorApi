namespace MySqlExecutorApi.Models;

public class MySqlStatus : MySqlStatusBase
{
    public string? DbIp { get; set; }
    public string? DbName { get; set; }
    public string? DbUserId { get; set; }
}

public class MySqlStatusBase
{
    public string? Uptime { get; set; }
    public DateTime? StartTime { get; set; }
    public string? MySqlVersion { get; set; }
    public int ServerConnectionTimeoutSec { get; set; }
    public int AppConnectionTimeoutSec { get; set; }
    public int DefaultCommandTimeoutSec { get; set; }
    public PostResponse QueryStatus { get; set; }
}
