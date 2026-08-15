# MSSQL MCP Server

[![NuGet version](https://img.shields.io/nuget/v/MssqlMcp.Dnx.svg)](https://www.nuget.org/packages/MssqlMcp.Dnx/)

A [Model Context Protocol](https://modelcontextprotocol.io/) server for SQL Server, built with the official **MCP C# SDK 2.0** (`ModelContextProtocol`) and `Microsoft.Data.SqlClient`, over **stdio**.

## Requirements

- .NET SDK 10 (for building / running from source)
- A SQL Server instance (local or remote)
- Native AOT publish (optional) needs a Visual Studio C++ workload (`link.exe`) — see [Native AOT](#native-aot)

## Build & Run

```powershell
cd MssqlMcp
dotnet build
```

### Run from source

The server connects to SQL Server using one of the following, in priority order:

1. `--conn "<connection string>"` argument
2. `MSSQL_CONN` environment variable
3. Default: `Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;`

The connection string may be split across multiple `args` elements and/or wrapped in single quotes — `ResolveConn` re-joins them with spaces and strips both quote types, so a client can't accidentally quote it. Setting `MSSQL_CONN` keeps the secret out of the process command line.

```powershell
# Windows Auth
dotnet run -- --conn "Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;"

# SQL Auth
dotnet run -- --conn "Server=localhost;Database=master;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

> **Note:** the connection string is owned by the server and is **never** exposed to the LLM as a tool parameter.

## Publish

### Managed single-file (framework‑dependent)

```powershell
cd MssqlMcp
# Publish a single executable that uses the .NET runtime installed on the machine.
# Do **not** set self‑contained; otherwise the optimizer aborts (NETSDK1102).
# This yields a small ~5 MB file.

dotnet publish -c Release -r win-x64 /p:PublishAot=false /p:PublishSingleFile=true --self-contained false -o .\bin\publish-single
```

### Native AOT (fast startup, no runtime install)

```powershell
cd MssqlMcp
# vswhere must be on PATH so the ILCompiler can find link.exe
$env:PATH = "C:\Program Files (x86)\Microsoft Visual Studio\Installer;" + $env:PATH
dotnet publish -c Release -r win-x64 -o .\bin\publish-aot
```

Output: `MssqlMcp.exe` (native) + `Microsoft.Data.SqlClient.SNI.dll` sidecar (cannot be embedded).

### Troubleshooting

- **NETSDK1102** (`Optimizing assemblies for size is not supported`): you
  combined single-file with `--self-contained true` (or left `PublishAot=true`).
  Use the framework-dependent command above, or switch to Native AOT.
- **`vswhere.exe is not recognized` / link fails on AOT**: add the VS
  Installer directory to `PATH` first (see the AOT command).
- **`Keyword not supported` / `index ...` in the connection string**: your MCP
  client is wrapping the value in quotes. Use `args` as two elements with
  **no** extra quotes (see below). A self-signed local SQL Server cert also
  needs `TrustServerCertificate=True`.

## Connect

Register **once** in your MCP client as a stdio server. Prefer passing the
connection string via the server's `env` so it never appears on the process
command line:

```json
{
  "mcpServers": {
    "mssql": {
      "command": "C:\\Development\\mssql-mcp\\MssqlMcp\\bin\\publish-single\\MssqlMcp.exe",
      "env": { "MSSQL_CONN": "Server=localhost;Database=master;User Id=sa;Password=123456;TrustServerCertificate=True" }
    }
  }
}
```


Alternatively, pass it as an arg -- but keep it to one clean array element, no surrounding quotes (a quoted or space-split value is tolerated by the server, but in the config it's a footgun).

> The single-file build needs the .NET 10 runtime; the Native AOT build has no runtime dependency.

## Run with `dnx`

You can run the published NuGet package directly with the .NET 10 `dnx` command. No
local clone or permanent installation is required; `dnx` downloads the package from
NuGet.org and starts the MCP server when your MCP client launches it.

The package is available at [MssqlMcp.Dnx on NuGet.org](https://www.nuget.org/packages/MssqlMcp.Dnx/).

Add this server to your MCP client configuration:

```json
{
  "mcpServers": {
    "mssql": {
      "command": "dnx",
      "args": ["MssqlMcp.Dnx@1.0.4", "--yes"],
      "env": {
        "MSSQL_CONN": "Server=localhost;Database=master;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
      }
    }
  }
}
```

Replace `1.0.4` with the release version you want. The `MSSQL_CONN` environment
variable keeps database credentials out of the command line and is read by the
server at startup. `dnx` is included with the .NET 10 SDK.

## Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| `get_table_list` | Lists all tables in the database (base tables). | — |
| `get_table_columns` | Gets columns of a table (or all tables), filterable by data type, precision, scale. Supports `schema.table` qualifiers. | `tableName`, `schemaName`, `dataType`, `precision`, `scale` (all optional) |
| `execute_get_query_data` | Runs a `SELECT` and returns results as a Markdown table. | `query` (required) |
| `execute_insert_update_data` | Runs `INSERT` / `UPDATE` / `DELETE` and returns the affected row count. | `query` (required) |
| `check_slow_query` | Lists slow queries from `sys.dm_exec_query_stats` (execution time, CPU, logical reads), filtered by minimum average execution time and optional date range. | `minAvgExecutionTimeMs` (default 1000), `startDate`, `endDate` (optional) |

### Security note

All tools execute against the connection string configured server-side at startup. The LLM can only pass `query` / filter parameters — it never sees database credentials.



