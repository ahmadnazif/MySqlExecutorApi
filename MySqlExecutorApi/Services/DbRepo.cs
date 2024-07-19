using MySqlConnector;
using MySqlExecutorApi.Models;
using System.Diagnostics;
using System.Text.Json;

namespace MySqlExecutorApi.Services;

public class DbRepo(ILogger<DbRepo> logger, MySqlDataSource db) : IDbRepo
{
    private readonly ILogger<DbRepo> logger = logger;
    private readonly MySqlDataSource db = db;

    #region Helper
    private static object? GetObjectValue(object obj)
    {
        if (obj == DBNull.Value) return null;
        else return obj;
    }

    private static string? GetStringValue(object obj)
    {
        if (obj == DBNull.Value) return null;
        else return obj.ToString();
    }

    private static byte[]? GetByteArrayValue(object obj)
    {
        if (obj == DBNull.Value) return null;
        else return (byte[])obj;
    }

    private static DateTime? GetDateTimeValue(object obj)
    {
        if (obj == DBNull.Value) return null;
        else return Convert.ToDateTime(obj);
    }

    private static double? GetDoubleValue(object obj)
    {
        if (obj == DBNull.Value) return null;
        else
        {
            return obj.ToString() == null ? null : double.Parse(obj.ToString());
        }
    }
    private static int? GetIntValue(object obj)
    {
        if (obj == DBNull.Value) return null;
        else
        {
            return obj.ToString() == null ? null : int.Parse(obj.ToString());
        }
    }

    private static long? GetLongValue(object obj)
    {
        if (obj == DBNull.Value) return null;
        else
        {
            return obj.ToString() == null ? null : long.Parse(obj.ToString());
        }
    }

    /// <summary>
    /// Already handled if value is NULL or empty or whitespace
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static Dictionary<string, int> Deserialize(string value)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value) ? [] : JsonSerializer.Deserialize<Dictionary<string, int>>(value);
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<string, string> DeserializeStringDict(string value)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(value);
        }
        catch
        {
            return [];
        }
    }
    #endregion

    #region Mysql status

    public async Task<MySqlStatusBase> GetDbStatusAsync(CancellationToken ct)
    {
        try
        {
            var sql =
                $"SELECT * FROM performance_schema.global_status WHERE variable_name IN ('Uptime');" +
                $"SELECT * FROM performance_schema.global_variables WHERE variable_name IN ('version', 'connect_timeout');";

            TimeSpan? uptime = null;
            string mysqlVersion = null;
            string serverConnectionTimeout = null;
            int appConnectionTimeout = 0;
            int commandTimeout = 0;

            await using MySqlConnection connection = await db.OpenConnectionAsync(ct);
            await using MySqlCommand cmd = new(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            appConnectionTimeout = connection.ConnectionTimeout;
            commandTimeout = cmd.CommandTimeout;

            while (await reader.ReadAsync(ct))
            {
                var name = GetStringValue(reader[0]);
                var valStr = GetStringValue(reader[1]);

                switch (name)
                {
                    case "Uptime": uptime = TimeSpan.FromSeconds(int.Parse(valStr)); break;
                }
            }

            await reader.NextResultAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var name = GetStringValue(reader[0]);
                var valStr = GetStringValue(reader[1]);

                switch (name)
                {
                    case "version": mysqlVersion = valStr; break;
                    case "connect_timeout": serverConnectionTimeout = valStr; break;
                }
            }

            return new MySqlStatus
            {
                Uptime = uptime.ToString(),
                MySqlVersion = mysqlVersion,
                ServerConnectionTimeoutSec = int.Parse(serverConnectionTimeout),
                AppConnectionTimeoutSec = appConnectionTimeout,
                DefaultCommandTimeoutSec = commandTimeout,
                StartTime = DateTime.Now.Subtract(uptime.Value)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }

    #endregion

    #region Command exe

    public async Task<ReadCommandExecutionResponse> ExecuteReadCommandAsync(string? commandText, CancellationToken ct)
    {
        try
        {
            var type = SqlCommandTypeDetector.GetCommandType(commandText);

            if (type == SqlCommandType.Unknown)
            {
                return new()
                {
                    CommandText = commandText,
                    IsSuccess = false,
                    Message = "Supplied command is not valid"
                };
            }

            if (type != SqlCommandType.Read)
            {
                return new()
                {
                    CommandText = commandText,
                    IsSuccess = false,
                    Message = "Supplied command is not a read command"
                };
            }

            int resultCount = 0;

            await using MySqlConnection connection = await db.OpenConnectionAsync(ct);
            await using MySqlCommand cmd = new(commandText, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            Stopwatch sw = Stopwatch.StartNew();

            while (await reader.ReadAsync(ct))
            {
                resultCount += 1;
            }

            sw.Stop();

            return new()
            {
                CommandText = commandText,
                ConnecionId = connection.ServerThread,
                Elapsed = sw.Elapsed.ToString(),
                ResultCount = resultCount,
                IsSuccess = true,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new()
            {
                CommandText = commandText,
                IsSuccess = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }


    public async Task<WriteCommandExecutionResponse> ExecuteWriteCommandAsync(string? commandText, CancellationToken ct)
    {
        try
        {
            var type = SqlCommandTypeDetector.GetCommandType(commandText);

            if (type == SqlCommandType.Unknown)
            {
                return new()
                {
                    CommandText = commandText,
                    IsSuccess = false,
                    Message = "Supplied command is not valid"
                };
            }

            if (type != SqlCommandType.Write)
            {
                return new()
                {
                    CommandText = commandText,
                    IsSuccess = false,
                    Message = "Supplied command is not a write command"
                };
            }

            int rowAffected = 0;

            await using MySqlConnection connection = await db.OpenConnectionAsync(ct);
            await using MySqlCommand cmd = new(commandText, connection);

            Stopwatch sw = Stopwatch.StartNew();
            rowAffected = await cmd.ExecuteNonQueryAsync(ct);
            sw.Stop();

            return new()
            {
                CommandText = commandText,
                ConnecionId = connection.ServerThread,
                Elapsed = sw.Elapsed.ToString(),
                RowsAffected = rowAffected,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new()
            {
                CommandText = commandText,
                IsSuccess = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }


    #endregion

    #region Typical functions

    public async Task<List<string>> ListAllTablesAsync(CancellationToken ct)
    {
        try
        {
            List<string> data = [];

            string commandText = $"SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()";

            await using MySqlConnection connection = await db.OpenConnectionAsync(ct);
            await using MySqlCommand cmd = new(commandText, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            Stopwatch sw = Stopwatch.StartNew();

            while (await reader.ReadAsync(ct))
            {
                data.Add(GetStringValue(reader[0]));
            }

            sw.Stop();

            return data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return [];
        }
    }

    public async Task<string> GetTableInfoAsync(string tableName, CancellationToken ct)
    {
        try
        {
            string data = null;

            string commandText = $"SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()";

            await using MySqlConnection connection = await db.OpenConnectionAsync(ct);
            await using MySqlCommand cmd = new(commandText, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            Stopwatch sw = Stopwatch.StartNew();

            while (await reader.ReadAsync(ct))
            {
                //data.Add(GetStringValue(reader[0]));
            }

            sw.Stop();

            return data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }


    #endregion
}
