using System.Text.RegularExpressions;

namespace MySqlExecutorApi.Helpers;

public partial class SqlCommandTypeDetector
{
    private static readonly Regex ReadCommandRegex = ReadRegex();
    private static readonly Regex WriteCommandRegex = WriteRegex();

    public static SqlCommandType GetCommandType(string sqlCommand)
    {
        if (ReadCommandRegex.IsMatch(sqlCommand))
            return SqlCommandType.Read;

        if (WriteCommandRegex.IsMatch(sqlCommand))
            return SqlCommandType.Write;

        return SqlCommandType.Unknown;
    }

    [GeneratedRegex(@"^\s*(SELECT|SHOW|DESCRIBE|EXPLAIN)\s+", RegexOptions.IgnoreCase, "en-MY")]
    private static partial Regex ReadRegex();

    [GeneratedRegex(@"^\s*(INSERT|UPDATE|DELETE|REPLACE|ALTER|CREATE|DROP|TRUNCATE|LOAD|RENAME|SET)\s+", RegexOptions.IgnoreCase, "en-MY")]
    private static partial Regex WriteRegex();
}
