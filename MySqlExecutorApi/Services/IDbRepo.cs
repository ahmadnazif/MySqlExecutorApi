using MySqlExecutorApi.Models;

namespace MySqlExecutorApi.Services;

public interface IDbRepo
{
    #region MySql status
    Task<MySqlStatusBase> GetDbStatusAsync(CancellationToken ct);
    #endregion

    #region Command exe
    Task<ReadCommandExecutionResponse> ExecuteReadCommandAsync(string? commandText, CancellationToken ct);
    Task<WriteCommandExecutionResponse> ExecuteWriteCommandAsync(string? commandText, CancellationToken ct);
    #endregion

    #region Typical in functions
    Task<List<string>> ListAllTablesAsync(CancellationToken ct);
    #endregion
}
