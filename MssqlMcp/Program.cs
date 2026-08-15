using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SseMcps.AI.Tools;
using SseMcps.Helpers;

string ResolveConn(string[] args)
{
    // --conn <connection string> > MSSQL_CONN env > localhost default.
    // A conn string contains spaces; re-join every element after --conn (clients
    // may split it across args) and strip any wrapping quotes.
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == "--conn")
            return string.Join(" ", args[(i + 1)..])
                .Trim('\'', '"');
    return (Environment.GetEnvironmentVariable("MSSQL_CONN")
        ?? "Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;").Trim('\'', '"');
}

ConnectionProvider.Value = ResolveConn(args);

// stderr only (stdout is reserved for MCP JSON); mask the password
var masked = string.Concat(ConnectionProvider.Value.Split(';').Select(k =>
    k.TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase) ? "Password=***;" : k + ";"));
Console.Error.WriteLine($"[mssql] resolved connection string: {masked}");

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // stdio: all logs to stderr, JSON only on stdout
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    // generic WithTools<T> keeps metadata for trim/AOT (WithToolsFromAssembly does not)
    .WithTools<SchemaTool>()
    .WithTools<QueryTool>();

await builder.Build().RunAsync();