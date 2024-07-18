using MySqlExecutorApi.Models;

namespace MySqlExecutorApi.Services;

public interface IDbRepo
{
    #region MySql status
    Task<MySqlStatusBase> GetDbStatusAsync(CancellationToken ct);
    #endregion
}
