using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using SseMcps.Helpers;
using System.ComponentModel;

namespace SseMcps.AI.Tools;

[McpServerToolType]
public class QueryTool
{
    [McpServerTool, Description("""
 Get data from a given query. Can filter by column values or sort by specific columns. The query must be a valid SQL SELECT statement.
 The object in query must exist in the database.
 The query should get top 10 record or have given limit in the query if you don't want to get all data.
 """)]
    public string ExecuteGetQueryData(string query)
    {
        var command = new SqlCommand(query);
        return DbHelper.ExecuteDataTable(ConnectionProvider.Value, command);
    }

    [McpServerTool, Description("""
 Execute insert, update or delete from a given query. The query must be a valid SQL SELECT statement.
 The object in query must exist in the database. The query parameters must be valid SQL parameters.
 """)]
    public string ExecuteInsertUpdateData(string query)
    {
        var command = new SqlCommand(query);
        return DbHelper.ExecuteInsertUpdate(ConnectionProvider.Value, command);
    }

    [McpServerTool, Description("""
 Identify slow queries by querying sys.dm_exec_query_stats to retrieve metrics like total execution time, CPU time, logical reads, and execution count, focusing on queries exceeding a specified threshold (e.g., average execution time).
 Can filter by date range and minimum average execution time.
 """)]
    public string CheckSlowQuery(DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal minAvgExecutionTimeMs = 1000)
    {
        var command = new SqlCommand("""
 SELECT
 SUBSTRING(t.text, (qs.statement_start_offset/2) + 1,
 ((CASE qs.statement_end_offset
 WHEN -1 THEN DATALENGTH(t.text)
 ELSE qs.statement_end_offset
 END - qs.statement_start_offset)/2) + 1) AS QueryText,
 qs.execution_count AS ExecutionCount,
 (qs.total_elapsed_time / 1000.0) / qs.execution_count AS AvgExecutionTimeMs,
 qs.total_elapsed_time / 1000.0 AS TotalExecutionTimeMs,
 qs.total_logical_reads AS TotalLogicalReads,
 qs.total_worker_time / 1000.0 AS TotalCpuTimeMs,
 qs.last_execution_time AS LastExecutionTime
 FROM sys.dm_exec_query_stats qs
 CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) t
 WHERE qs.execution_count > 0
 AND (qs.total_elapsed_time / 1000.0) / qs.execution_count >= @MinAvgExecutionTimeMs
 AND (@StartDate is null or qs.last_execution_time >= @StartDate )
 AND (@EndDate is null or qs.last_execution_time < @EndDate)
 ORDER BY AvgExecutionTimeMs DESC;
 """);
        command.Parameters.AddWithValue("@MinAvgExecutionTimeMs", minAvgExecutionTimeMs);
        command.Parameters.AddWithValue("@StartDate", startDate.HasValue ? (object)startDate.Value : DBNull.Value);
        command.Parameters.AddWithValue("@EndDate", endDate.HasValue ? (object)endDate.Value : DBNull.Value);
        return DbHelper.ExecuteDataTable(ConnectionProvider.Value, command);
    }
}
