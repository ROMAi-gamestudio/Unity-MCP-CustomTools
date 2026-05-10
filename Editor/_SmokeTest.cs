#nullable enable

using System.ComponentModel;
using com.IvanMurzak.McpPlugin;

namespace ROMAi.MCPTools.Editor
{
    [McpPluginToolType]
    public static partial class Tool_SmokeTest
    {
        [McpPluginTool
        (
            "smoke-greet",
            Title = "Smoke / Greet",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description("Smoke test for ROMAi.MCPTools.Editor pipeline. Returns a greeting string.")]
        public static string Greet
        (
            [Description("Name to greet.")]
            string name
        ) => $"Hello, {name}! ROMAi MCPTools pipeline is alive.";
    }
}
