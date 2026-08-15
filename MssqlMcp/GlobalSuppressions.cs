using System.Diagnostics.CodeAnalysis;

// Tool classes are instance types (not static) so the MCP SDK's generic
// WithTools<T>() can keep method metadata for Native AOT. That API refuses
// static type arguments (CS0718), which is why CA1822's "make it static"
// suggestion cannot be applied here.
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance tool types required by AOT-safe WithTools<T>()", Scope = "namespaceanddescendants", Target = "~N:SseMcps.AI.Tools")]
