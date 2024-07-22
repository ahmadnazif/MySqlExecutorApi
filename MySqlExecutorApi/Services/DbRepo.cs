using MySqlConnector;
using MySqlExecutorApi.Models;
using System.Diagnostics;
using System.Text.Json;

namespace MySqlExecutorApi.Services;

public class DbRepo(ILogger<DbRepo> logger, MySqlDataSource db, IConfiguration config) : IDbRepo
{
    private readonly ILogger<DbRepo> logger = logger;
    private readonly MySqlDataSource db = db;
    private readonly string dbServer = config["Db:Server"];
    private readonly string dbUsername = config["Db:UserId"];

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
            Stopwatch sw = Stopwatch.StartNew();

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
                var name = GetStringValue(reader[0]); // VARIABLE_NAME
                var valStr = GetStringValue(reader[1]); // VARIABLE_VALUE

                switch (name)
                {
                    case "Uptime": uptime = TimeSpan.FromSeconds(int.Parse(valStr)); break;
                }
            }

            await reader.NextResultAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var name = GetStringValue(reader[0]); // VARIABLE_NAME
                var valStr = GetStringValue(reader[1]); // VARIABLE_VALUE

                switch (name)
                {
                    case "version": mysqlVersion = valStr; break;
                    case "connect_timeout": serverConnectionTimeout = valStr; break;
                }
            }
            sw.Stop();

            return new MySqlStatus
            {
                Uptime = uptime.ToString(),
                MySqlVersion = mysqlVersion,
                ServerConnectionTimeoutSec = int.Parse(serverConnectionTimeout),
                AppConnectionTimeoutSec = appConnectionTimeout,
                DefaultCommandTimeoutSec = commandTimeout,
                StartTime = DateTime.Now.Subtract(uptime.Value),
                QueryStatus = new PostResponse
                {
                    IsSuccess = true,
                    Message = $"Query OK [{sw.Elapsed}]"
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new MySqlStatusBase
            {
                QueryStatus = new PostResponse
                {
                    IsSuccess = false,
                    Message = ex.Message
                }
            };
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

    private async Task<bool> IsTableExistAsync(string tableName, CancellationToken ct)
    {
        try
        {
            bool exist = false;
            string query = $"SELECT COUNT(*) FROM information_schema.TABLES WHERE table_schema = DATABASE() AND table_name = '{tableName}';";

            await using MySqlConnection connection = await db.OpenConnectionAsync(ct);
            await using MySqlCommand command = new(query, connection);
            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                exist = GetIntValue(reader[0]).Value == 1;
            }

            return exist;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return false;
        }
    }

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

    public async Task<MySqlTableInfo> GetTableInfoAsync(string tableName, CancellationToken ct)
    {
        var tableExist = await IsTableExistAsync(tableName, ct);
        if (!tableExist)
            return null;

        // Columns
        // ==========

        var columnsQuery = @"
            SELECT COLUMN_NAME, COLUMN_TYPE
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName";

        List<MySqlTableColumn> columns = [];

        await using var columnsConnection = await db.OpenConnectionAsync(ct);
        await using var columnsCommand = new MySqlCommand(columnsQuery, columnsConnection);
        columnsCommand.Parameters.AddWithValue("@tableName", tableName);

        await using var columnsReader = await columnsCommand.ExecuteReaderAsync(ct);
        while (await columnsReader.ReadAsync(ct))
        {
            columns.Add(new()
            {
                ColumnName = GetStringValue(columnsReader[0]),
                ColumnType = GetStringValue(columnsReader[1])
            });
        }

        //// Individual indexes
        //// ==========

        //List<MySqlTableIndexIndividual> individualIndexes = [];
        //var indQuery = @"
        //    SELECT INDEX_NAME, COLUMN_NAME, NON_UNIQUE, INDEX_TYPE
        //    FROM information_schema.STATISTICS
        //    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName";

        //await using var indConnection = await db.OpenConnectionAsync(ct);
        //await using var indCommand = new MySqlCommand(indQuery, indConnection);
        //indCommand.Parameters.AddWithValue("@tableName", tableName);

        //await using var indReader = await indCommand.ExecuteReaderAsync(ct);
        //while (await indReader.ReadAsync(ct))
        //{
        //    individualIndexes.Add(new()
        //    {
        //        IndexName = GetStringValue(indReader[0]),
        //        Column = GetStringValue(indReader[1]),
        //        IsUnique = GetIntValue(indReader[2]) == 0,
        //        IndexType = GetStringValue(indReader[3])
        //    });
        //}

        // Indexes
        // ==========

        List<MySqlTableIndex> tempIndexes = [];

        var idxQuery = @"
            SELECT INDEX_NAME, COLUMN_NAME, SEQ_IN_INDEX, INDEX_TYPE, NON_UNIQUE 
            FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName
            ORDER BY INDEX_NAME, SEQ_IN_INDEX";

        await using var idxConnection = await db.OpenConnectionAsync(ct);
        await using var idxCommand = new MySqlCommand(idxQuery, idxConnection);
        idxCommand.Parameters.AddWithValue("@tableName", tableName);

        Dictionary<string, Dictionary<string, int>> idxes = [];

        await using var idxReader = await idxCommand.ExecuteReaderAsync(ct);
        while (await idxReader.ReadAsync(ct))
        {
            var indexName = GetStringValue(idxReader[0]);
            var columnName = GetStringValue(idxReader[1]);
            int seqInIndex = GetIntValue(idxReader[2]).Value;
            var idxType = GetStringValue(idxReader[3]);
            var idxUnique = GetIntValue(idxReader[4]).Value == 0;

            if (!idxes.TryGetValue(indexName, out var value))
            {
                value = [];
                idxes[indexName] = value;
            }

            value.Add(columnName, seqInIndex);

            tempIndexes.Add(new MySqlTableIndex
            {
                IndexName = indexName,
                Columns = value.ToDictionary(),
                IndexType = GetStringValue(idxReader[3]),
                IsUnique = GetIntValue(idxReader[4]).Value == 0
            });
        }

        var indexes = tempIndexes
            .GroupBy(index => index.IndexName)
            .Select(group => group.OrderByDescending(index => index.Columns.Count)
            .First()).ToList();

        return new()
        {
            TableName = tableName,
            Columns = columns,
            //IndexesIndividual = individualIndexes,
            Indexes = indexes
        };
    }

    public async Task<string> ShowGrantsAsync(CancellationToken ct)
    {
        try
        {
            string data = null;

            string commandText = $"SHOW GRANTS;";

            await using MySqlConnection connection = await db.OpenConnectionAsync(ct);
            await using MySqlCommand cmd = new(commandText, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            Stopwatch sw = Stopwatch.StartNew();

            while (await reader.ReadAsync(ct))
            {
                data = GetStringValue(reader[0]);
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
