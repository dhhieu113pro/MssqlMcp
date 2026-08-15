using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using SseMcps.Helpers;
using System.ComponentModel;
using System.Text;

namespace SseMcps.AI.Tools;

[McpServerToolType]
public class SchemaTool
{
    [McpServerTool, Description("""
 Gets column lists of a given table or all table in a database. Can filter columns by data type, precision, and scale.
 """)]
    public string GetTableColumns(
        string? tableName = null,
        string? schemaName = null,
        string? dataType = null,
        int? precision = null,
        int? scale = null)
    {
        if (!string.IsNullOrEmpty(tableName) && tableName.Contains('.'))
        {
            int lastDot = tableName.LastIndexOf('.');
            schemaName = tableName[..lastDot];
            tableName = tableName[(lastDot + 1)..];
        }

        var _tableQuery = new StringBuilder($$"""
 SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
 FROM INFORMATION_SCHEMA.COLUMNS
 WHERE 1 = 1 {WHERE_CONDITION}
 ORDER BY TABLE_NAME, COLUMN_NAME
 """);
        var command = new SqlCommand();
        var whereCondition = new StringBuilder();
        if (!string.IsNullOrEmpty(tableName))
        {
            whereCondition.Append("AND TABLE_NAME = @tableName ");
            command.Parameters.AddWithValue("@tableName", tableName);
        }
        if (!string.IsNullOrEmpty(schemaName))
        {
            whereCondition.Append("AND TABLE_SCHEMA = @schemaName ");
            command.Parameters.AddWithValue("@schemaName", schemaName);
        }
        if (!string.IsNullOrEmpty(dataType))
        {
            whereCondition.Append("AND DATA_TYPE = @dataType ");
            command.Parameters.AddWithValue("@dataType", dataType);
        }
        if (precision.HasValue)
        {
            whereCondition.Append("AND NUMERIC_PRECISION = @precision ");
            command.Parameters.AddWithValue("@precision", precision.Value);
        }
        if (scale.HasValue)
        {
            whereCondition.Append("AND NUMERIC_SCALE = @scale ");
            command.Parameters.AddWithValue("@scale", scale.Value);
        }
        _tableQuery = _tableQuery.Replace("{WHERE_CONDITION}", whereCondition.ToString());
        command.CommandText = _tableQuery.ToString();
        return DbHelper.Execute(ConnectionProvider.Value, command, (reader) =>
        {
            var results = new StringBuilder();
            results.AppendLine("| Table Name | Column Name | Data Type | Precision | Scale |");
            results.AppendLine("|------------|-------------|-----------|-----------|-------|");
            bool hasRows = false;
            while (reader.Read())
            {
                hasRows = true;
                string table = reader["TABLE_NAME"]?.ToString()?.Replace("|", "\\|") ?? "";
                string column = reader["COLUMN_NAME"]?.ToString()?.Replace("|", "\\|") ?? "";
                string dataTypeValue = reader["DATA_TYPE"]?.ToString()?.Replace("|", "\\|") ?? "";
                string precisionStr = reader["NUMERIC_PRECISION"]?.ToString() ?? "";
                string scaleStr = reader["NUMERIC_SCALE"]?.ToString() ?? "";
                results.AppendLine($"| {table} | {column} | {dataTypeValue} | {precisionStr} | {scaleStr} |");
            }
            return hasRows ? results.ToString() : "No matching columns found.";
        });
    }

    [McpServerTool, Description("Retrieves a list of all table names in the database.")]
    public string GetTableList()
    {
        var _tableListQuery = """
 SELECT TABLE_SCHEMA, TABLE_NAME
 FROM INFORMATION_SCHEMA.TABLES
 WHERE TABLE_TYPE = 'BASE TABLE'
 ORDER BY TABLE_NAME
 """;
        var cmd = new SqlCommand(_tableListQuery);
        return DbHelper.Execute(ConnectionProvider.Value, cmd, (reader) =>
        {
            var results = new StringBuilder();
            results.AppendLine("| Schema | Table Name |");
            results.AppendLine("|--------|------------|");
            bool hasRows = false;
            while (reader.Read())
            {
                hasRows = true;
                string schema = reader["TABLE_SCHEMA"]?.ToString()?.Replace("|", "\\|") ?? "";
                string table = reader["TABLE_NAME"]?.ToString()?.Replace("|", "\\|") ?? "";
                results.AppendLine($"| {schema} | {table} |");
            }
            return hasRows ? results.ToString() : "No tables found in the database.";
        });
    }
}
