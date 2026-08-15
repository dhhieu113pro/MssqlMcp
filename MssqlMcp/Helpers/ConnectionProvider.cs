namespace SseMcps.Helpers;

// ponytail: process-global singleton for the connection string, owned by the server,
// never exposed to the LLM via tool params. Fine for a single-process local server.
// Per-request/scoped injection only if you later support multiple tenants or rotate creds.
internal static class ConnectionProvider
{
    public static string Value { get; set; } = "";
}
