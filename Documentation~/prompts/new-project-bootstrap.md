# New-project bootstrap prompt

Paste **everything between the `===` markers below** as your first message in a fresh Claude Code session opened inside a new Unity project.

Prerequisites: Phase 1 of [BOOTSTRAP.md](../BOOTSTRAP.md) is done — i.e. AI Game Developer Installer is imported, MCP server is running in Unity, `npx unity-mcp-cli setup-mcp claude-code .` has written `.mcp.json`.

---

=== COPY FROM HERE ===

Bootstrap MCP packages for this Unity project. Use the `ai-game-developer` MCP tools (no manual file edits unless I say so). Run sequentially because each `package-add` may trigger a domain reload.

Steps:

1. Verify Unity Editor state with `editor-application-get-state`. Fail fast if `IsPlaying=true` or `IsCompiling=true`.

2. Read `Packages/manifest.json` and confirm `com.ivanmurzak` is in scopedRegistries scopes. If not, add a scopedRegistry entry:
   ```json
   { "name": "package.openupm.com",
     "url": "https://package.openupm.com",
     "scopes": ["com.ivanmurzak", "extensions.unity"] }
   ```

3. Install the IvanMurzak extensions and the ROMAi custom tools — call `package-add` once per packageId, in order, waiting for each domain reload:
   - `com.ivanmurzak.unity.mcp.animation`
   - `com.ivanmurzak.unity.mcp.particlesystem`
   - `com.ivanmurzak.unity.mcp.probuilder`
   - `https://github.com/ROMAi-gamestudio/Unity-MCP-CustomTools.git#v1.0.0`

4. Verify installation with `package-list` (filter `ivanmurzak`, then filter `romai`). All 4 IvanMurzak packages and `com.romai.mcp.tools@1.0.0` (Source: Git) must be present.

5. Verify the 16 ROMAi tools registered: call `tool-list` with regexSearch `^(smoke-greet|ui-(link-stylesheet|document-attach|panel-settings-create)|build-(platform-get|platform-set|scenes-list|scenes-set|settings-get|settings-set|execute)|profiler-(start|stop|status|get-frame-timing|get-counters))$`. Expected count: **16**.

6. Run `smoke-greet name="<this project's product_name>"` and confirm the response ends with `ROMAi MCPTools pipeline is alive.`. Use `build-settings-get target=StandaloneWindows64 property=product_name` to read the project name.

7. Report a one-line success summary. If any step fails, stop and report the failing step with the error verbatim — don't try to "fix it up". Burst warnings about `Failed to resolve assembly: com.IvanMurzak.Unity.MCP.Editor` are known cosmetic noise — ignore them.

After success, save a project-type memory noting this project uses `com.romai.mcp.tools` v1.0.0 via Git URL so future sessions don't re-discover it.

=== COPY UNTIL HERE ===

---

## How to grab this in 2 clicks

**Option 1 — GitHub web (easiest):**

Open <https://github.com/ROMAi-gamestudio/Unity-MCP-CustomTools/blob/main/prompts/new-project-bootstrap.md> → click the **Raw** button (top-right of the file view) → `Ctrl+A`, `Ctrl+C` → paste into Claude Code → trim the markdown header/markers if you want (Claude understands either way).

**Option 2 — terminal:**

```powershell
curl -sL https://raw.githubusercontent.com/ROMAi-gamestudio/Unity-MCP-CustomTools/main/prompts/new-project-bootstrap.md | clip
```

Then `Ctrl+V` into Claude Code.
