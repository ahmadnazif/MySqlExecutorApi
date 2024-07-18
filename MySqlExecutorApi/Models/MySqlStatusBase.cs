namespace MySqlExecutorApi.Models;

public class MySqlStatusBase
{
    public string Uptime { get; set; }
    public DateTime StartTime { get; set; }
    public string MySqlVersion { get; set; }
    public int ServerConnectionTimeoutSec { get; set; }
    public int AppConnectionTimeoutSec { get; set; }
    public int DefaultCommandTimeoutSec { get; set; }
}
