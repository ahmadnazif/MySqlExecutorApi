namespace MySqlExecutorApi.Models;

public class MySqlTableInfo
{
    public string? TableName { get; set; }
    public List<MySqlTableColumn> Columns { get; set; }
    public List<MySqlTableIndexIndividual> IndexesIndividual { get; set; }
    public List<MySqlTableIndex> Indexes { get; set; }
}

public class MySqlTableColumn
{
    public string? ColumnName { get; set; }
    public string? ColumnType { get; set; }
}

public class MySqlTableIndexIndividual
{
    public string? IndexName { get; set; }
    public string? Column { get; set; }
    public bool IsUnique { get; set; }
    public string? IndexType { get; set; }
}

public class MySqlTableIndex
{
    public string? IndexName { get; set; }
    public Dictionary<string, int> Columns { get; set; }
    public bool IsUnique { get; set; }
    public string? IndexType { get; set; }
}
