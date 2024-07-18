namespace MySqlExecutorApi.Models;

public class MySqlStatus : MySqlStatusBase
{
    public string DbIp { get; set; }
    public string DbName { get; set; }
    public string DbUserId { get; set; }
}
