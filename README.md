# ROMAi MCP Custom Tools

Custom Editor tools for the [IvanMurzak Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) server. Adds **16 tools** across three domains that the upstream server does not cover: **UI Toolkit**, **Build**, and **Profiler**.

## Tools

### Smoke (1)
- `smoke-greet` — pipeline liveness check.

### UI Toolkit (3)
- `ui-link-stylesheet` — adds `<Style src="..."/>` to a UXML file (idempotent).
- `ui-document-attach` — attaches a `UIDocument` component with `VisualTreeAsset` and optional `PanelSettings`.
- `ui-panel-settings-create` — creates a `PanelSettings` ScriptableObject asset.

### Build (7)
- `build-platform-get`, `build-platform-set`
- `build-scenes-list`, `build-scenes-set`
- `build-settings-get`, `build-settings-set` (`product_name`, `company_name`, `version`, `bundle_id`, `scripting_backend`)
- `build-execute` — runs `BuildPipeline.BuildPlayer` synchronously, returns compact `BuildReport` summary.

### Profiler (5)
- `profiler-start`, `profiler-stop`, `profiler-status`
- `profiler-get-frame-timing` — reads latest `FrameTiming` sample.
- `profiler-get-counters` — reads `ProfilerRecorder` values for a category.

## Installation

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ivanmurzak.unity.mcp": "0.71.0",
    "com.romai.mcp.tools": "https://github.com/ROMAi-gamestudio/Unity-MCP-CustomTools.git#v1.0.0"
  }
}
```

Pin to a specific tag (`#v1.0.0`) for reproducibility, or use `#main` to follow the latest commit on `main`.

### Requirements

- Unity 6.x (developed against 6000.4.5f1 LTS)
- `com.ivanmurzak.unity.mcp` 0.71.0+
- The `UNITY_MCP_READY` define is set automatically by the IvanMurzak `DependencyResolver` once NuGet DLLs land. Tools compile only after that — protects against compile errors during initial install.

## Verification

After install, in a fresh Claude Code session pointed at the project:

```
> tool-list with regex `^(smoke-greet|ui-|build-|profiler-)`
> smoke-greet name="MyProject"
→ "Hello, MyProject! ROMAi MCPTools pipeline is alive."
```

Expected count: **16** tools.

## Known caveats

- **`build-execute`** is synchronous and long (minutes). MCP timeouts can occur — if the response is lost, check `outputPath` directly on disk to confirm whether the build actually finished.
- **`profiler-get-counters`** in idle Editor returns 0 for most counters because no frame has rendered. Either enter Play Mode first or move the mouse over the Game View to trigger a render before calling.
- **Burst warnings** about `Failed to resolve assembly: 'com.IvanMurzak.Unity.MCP.Editor'` after recompile — known cosmetic noise, ignore.

## License

MIT — see [LICENSE](LICENSE).
