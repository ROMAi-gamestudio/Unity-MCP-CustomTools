# Bootstrap a new Unity project with ROMAi MCP

End-to-end recipe to wire up a fresh Unity project with:

- IvanMurzak `com.ivanmurzak.unity.mcp` (the MCP server)
- 3 official extensions (`animation`, `particlesystem`, `probuilder`)
- ROMAi custom tools (`com.romai.mcp.tools`, this repo) — adds 16 tools across **UI Toolkit / Build / Profiler**

Total time: **~2-3 minutes** after the one-time prerequisites.

---

## 0. Prerequisites (one-time per machine)

- **Unity 6.x** (developed against `6000.4.5f1` LTS) installed via Unity Hub.
- **Node.js** ≥ 18 (for `npx unity-mcp-cli`). Verify: `node --version`.
- **Claude Code** CLI installed and signed in. Verify: `claude --version`.
- **GitHub access** to `ROMAi-gamestudio/Unity-MCP-CustomTools` (this repo is public — no auth needed for installation).

---

## 1. Manual phase — create the project shell (one-time per project, ~3 min)

Things Claude Code can't do for you, because the MCP server isn't running yet:

1. **Unity Hub → New Project** (any template, e.g. Universal 2D).
2. **Install the AI Game Developer Installer**:
   - Download `unity-mcp-installer.unitypackage` from <https://github.com/IvanMurzak/Unity-MCP/releases>.
   - In the new project: `Assets → Import Package → Custom Package…` → select the file → `Import`.
   - Wait for domain reload (1-2 min on first install — Unity pulls NuGet DLLs).
3. **Start the MCP server inside Unity**: `Tools → AI Game Developer → Server → Start` (or whatever entry the installer added; the bottom-right status should turn green).
4. **Wire up Claude Code** — in the project root terminal:
   ```bash
   npx unity-mcp-cli setup-mcp claude-code .
   ```
   This writes `.mcp.json` so Claude Code knows about the `ai-game-developer` MCP server.
5. **Open Claude Code** in the project directory: `claude` (or via IDE extension).

The base `com.ivanmurzak.unity.mcp` package is now installed and the MCP bridge is alive. From here Claude Code can do the rest.

---

## 2. Automated phase — paste this prompt into Claude Code

The prompt below is also available as a standalone file: **[`prompts/new-project-bootstrap.md`](prompts/new-project-bootstrap.md)**.

Two ways to grab it:
- **GitHub web:** open <https://github.com/ROMAi-gamestudio/Unity-MCP-CustomTools/blob/main/prompts/new-project-bootstrap.md> → click **Raw** → `Ctrl+A`, `Ctrl+C`.
- **Terminal:** `curl -sL https://raw.githubusercontent.com/ROMAi-gamestudio/Unity-MCP-CustomTools/main/prompts/new-project-bootstrap.md | clip` (then `Ctrl+V` into Claude Code).

Open a new Claude Code session in the Unity project directory and paste this **whole block** as the first message:

````text
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
````

When Claude Code reports the success summary you're done — 16 ROMAi tools + animation/particlesystem/probuilder tools are all live in this project.

---

## 3. Expected end state

Your project's `Packages/manifest.json` should contain:

```json
{
  "scopedRegistries": [{
    "name": "package.openupm.com",
    "url": "https://package.openupm.com",
    "scopes": ["com.ivanmurzak", "extensions.unity"]
  }],
  "dependencies": {
    "com.ivanmurzak.unity.mcp": "0.71.0",
    "com.ivanmurzak.unity.mcp.animation": "1.1.37",
    "com.ivanmurzak.unity.mcp.particlesystem": "1.0.63",
    "com.ivanmurzak.unity.mcp.probuilder": "1.0.72",
    "com.romai.mcp.tools": "https://github.com/ROMAi-gamestudio/Unity-MCP-CustomTools.git#v1.0.0",
    "...": "your other Unity packages"
  }
}
```

`smoke-greet name="MyGame"` returns:
```
Hello, MyGame! ROMAi MCPTools pipeline is alive.
```

---

## 4. Troubleshooting

| Symptom | Cause / Fix |
|---|---|
| **Burst warning** `Failed to resolve assembly: 'com.IvanMurzak.Unity.MCP.Editor'` | Known cosmetic noise. Ignore. |
| `package-add` for `com.ivanmurzak.unity.mcp.animation` returns "package not found" | `com.ivanmurzak` scope missing from `Packages/manifest.json` scopedRegistries. Add it (see Phase 2 step 2). |
| `package-add` for the Git URL hangs | First-time Git fetch — wait up to 1 min. If it fails, check `git ls-remote https://github.com/ROMAi-gamestudio/Unity-MCP-CustomTools.git` works in your terminal. |
| Tools not registered after install (count < 16) | Asmdef compiled before NuGet DLLs landed. Force recompile: `assets-refresh options=ForceUpdate`, then re-run `tool-list`. Should self-heal. |
| `build-execute` MCP timeout but file appeared on disk | Known caveat — build succeeded but response was lost. Treat the on-disk artifact as ground truth. |

---

## 5. Updating to a newer ROMAi package version

After the package author releases a new tag (e.g. `v1.1.0`):

```text
Bump com.romai.mcp.tools to v1.1.0:
1. Read Packages/manifest.json, change "#v1.0.0" → "#v1.1.0" in the com.romai.mcp.tools URL.
2. Call assets-refresh, wait for domain reload.
3. Verify with package-list filter "romai" — version should be 1.1.0.
4. Run smoke-greet name="ping" to confirm tools still register.
```

(Or do it manually: edit `manifest.json`, save, Unity auto-resolves.)

---

## 6. Releasing a new package version (maintainer notes)

For when you (the package author) edit the tools:

```powershell
cd E:\AIHere\LAB\Unity-Packages\Unity-MCP-CustomTools

# Edit Editor/*.cs, update CHANGELOG.md, bump version in package.json
git commit -am "feat: <what changed>"

$ver = "1.1.0"
git tag "v$ver"
gh auth switch --user ROMAi-gamestudio  # if multiple gh accounts
git push origin main --tags
```

Consumers update by changing `#vTag` in their `manifest.json`.
