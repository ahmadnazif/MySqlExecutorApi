namespace MySqlExecutorApi.Models;

public class CommandExecutionResponse : PostResponse
{
    public string? CommandText { get; set; }
    public int ConnecionId { get; set; }
    public string? Elapsed { get; set; }
}

public class ReadCommandExecutionResponse : CommandExecutionResponse
{

}

public class WriteCommandExecutionResponse : CommandExecutionResponse
{
    public int RowsAffected { get; set; }
}
