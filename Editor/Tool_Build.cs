#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ROMAi.MCPTools.Editor
{
    [McpPluginToolType]
    public static partial class Tool_Build
    {
        // ──────────────────────────────────────────────────────────────────────
        // build-platform-get
        // ──────────────────────────────────────────────────────────────────────

        public const string BuildPlatformGetToolId = "build-platform-get";

        [McpPluginTool
        (
            BuildPlatformGetToolId,
            Title = "Build / Platform Get",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Returns the currently active BuildTarget, BuildTargetGroup, and standalone subtarget for the Editor.")]
        public static PlatformInfoResponse PlatformGet()
        {
            return MainThread.Instance.Run(() =>
            {
                var target = EditorUserBuildSettings.activeBuildTarget;
                var group = BuildPipeline.GetBuildTargetGroup(target);
                return new PlatformInfoResponse
                {
                    activeBuildTarget = target.ToString(),
                    activeBuildTargetGroup = group.ToString(),
                    standaloneSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget.ToString(),
                    isBuildTargetSupported = BuildPipeline.IsBuildTargetSupported(group, target)
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // build-platform-set
        // ──────────────────────────────────────────────────────────────────────

        public const string BuildPlatformSetToolId = "build-platform-set";

        [McpPluginTool
        (
            BuildPlatformSetToolId,
            Title = "Build / Platform Set",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Switches the Editor's active BuildTarget. Long-running: triggers a domain reload and may take seconds to minutes. Returns the new active platform.")]
        public static PlatformInfoResponse PlatformSet
        (
            [Description("Target platform. Examples: 'StandaloneWindows64', 'Android', 'iOS', 'WebGL'.")]
            BuildTarget target
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var group = BuildPipeline.GetBuildTargetGroup(target);

                // No-op: SwitchActiveBuildTarget returns false both for "already on target" and real failures —
                // we differentiate by checking the active target before calling.
                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    var ok = EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
                    if (!ok && EditorUserBuildSettings.activeBuildTarget != target)
                        throw new Exception($"Failed to switch to '{target}' (group '{group}'). Platform may not be installed for this Unity Editor.");
                }

                var actualTarget = EditorUserBuildSettings.activeBuildTarget;
                var actualGroup = BuildPipeline.GetBuildTargetGroup(actualTarget);
                return new PlatformInfoResponse
                {
                    activeBuildTarget = actualTarget.ToString(),
                    activeBuildTargetGroup = actualGroup.ToString(),
                    standaloneSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget.ToString(),
                    isBuildTargetSupported = BuildPipeline.IsBuildTargetSupported(actualGroup, actualTarget)
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // build-scenes-list
        // ──────────────────────────────────────────────────────────────────────

        public const string BuildScenesListToolId = "build-scenes-list";

        [McpPluginTool
        (
            BuildScenesListToolId,
            Title = "Build / Scenes List",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Returns EditorBuildSettings.scenes — the ordered list of scenes that ship in builds. Each entry has path, enabled flag, and asset GUID.")]
        public static ScenesListResponse ScenesList()
        {
            return MainThread.Instance.Run(() =>
            {
                var scenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
                return new ScenesListResponse
                {
                    scenes = scenes.Select(s => new SceneEntry
                    {
                        path = s.path,
                        enabled = s.enabled,
                        guid = s.guid.ToString()
                    }).ToArray()
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // build-scenes-set
        // ──────────────────────────────────────────────────────────────────────

        public const string BuildScenesSetToolId = "build-scenes-set";

        [McpPluginTool
        (
            BuildScenesSetToolId,
            Title = "Build / Scenes Set",
            ReadOnlyHint = false,
            DestructiveHint = true,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Replaces EditorBuildSettings.scenes with the provided ordered list. Each entry needs a project-relative scene path; 'enabled' defaults to true. Destructive: overwrites the existing list.")]
        public static ScenesListResponse ScenesSet
        (
            [Description("Ordered list of scenes to put into EditorBuildSettings.scenes. Provide path (required) and enabled (default true). 'guid' is ignored on input — re-derived from the path.")]
            SceneEntry[] scenes
        )
        {
            if (scenes == null)
                throw new ArgumentNullException(nameof(scenes));

            return MainThread.Instance.Run(() =>
            {
                var built = new EditorBuildSettingsScene[scenes.Length];
                for (var i = 0; i < scenes.Length; i++)
                {
                    var entry = scenes[i];
                    if (entry == null || string.IsNullOrEmpty(entry.path))
                        throw new ArgumentException($"scenes[{i}] is missing 'path'.", nameof(scenes));
                    built[i] = new EditorBuildSettingsScene(entry.path, entry.enabled);
                }
                EditorBuildSettings.scenes = built;
                return new ScenesListResponse
                {
                    scenes = EditorBuildSettings.scenes.Select(s => new SceneEntry
                    {
                        path = s.path,
                        enabled = s.enabled,
                        guid = s.guid.ToString()
                    }).ToArray()
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // build-settings-get
        // ──────────────────────────────────────────────────────────────────────

        public const string BuildSettingsGetToolId = "build-settings-get";

        [McpPluginTool
        (
            BuildSettingsGetToolId,
            Title = "Build / Settings Get",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Reads a single PlayerSettings property. Supported properties: 'product_name', 'company_name', 'version', 'bundle_id', 'scripting_backend'. The target argument scopes per-platform properties (bundle_id, scripting_backend); for global ones it is ignored.")]
        public static SettingsValueResponse SettingsGet
        (
            [Description("Build target to scope per-platform reads (bundle_id, scripting_backend). For global properties this argument is accepted but unused.")]
            BuildTarget target,
            [Description("Property key. One of: 'product_name', 'company_name', 'version', 'bundle_id', 'scripting_backend'.")]
            string property
        )
        {
            if (string.IsNullOrEmpty(property))
                throw new ArgumentException("'property' is required.", nameof(property));

            return MainThread.Instance.Run(() =>
            {
                var named = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
                var value = property switch
                {
                    "product_name" => PlayerSettings.productName,
                    "company_name" => PlayerSettings.companyName,
                    "version" => PlayerSettings.bundleVersion,
                    "bundle_id" => PlayerSettings.GetApplicationIdentifier(named),
                    "scripting_backend" => PlayerSettings.GetScriptingBackend(named).ToString(),
                    _ => throw new ArgumentException($"Unknown property '{property}'. Supported: product_name, company_name, version, bundle_id, scripting_backend.", nameof(property))
                };
                return new SettingsValueResponse
                {
                    target = target.ToString(),
                    property = property,
                    value = value
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // build-settings-set
        // ──────────────────────────────────────────────────────────────────────

        public const string BuildSettingsSetToolId = "build-settings-set";

        [McpPluginTool
        (
            BuildSettingsSetToolId,
            Title = "Build / Settings Set",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Writes a single PlayerSettings property. Same property keys as build-settings-get. For 'scripting_backend' value must be 'Mono2x' or 'IL2CPP'. Returns the value as written back.")]
        public static SettingsValueResponse SettingsSet
        (
            [Description("Build target to scope per-platform writes. Ignored for global properties.")]
            BuildTarget target,
            [Description("Property key. One of: 'product_name', 'company_name', 'version', 'bundle_id', 'scripting_backend'.")]
            string property,
            [Description("New value as a string. For 'scripting_backend' use 'Mono2x' or 'IL2CPP'.")]
            string value
        )
        {
            if (string.IsNullOrEmpty(property))
                throw new ArgumentException("'property' is required.", nameof(property));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return MainThread.Instance.Run(() =>
            {
                var named = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
                switch (property)
                {
                    case "product_name":
                        PlayerSettings.productName = value;
                        break;
                    case "company_name":
                        PlayerSettings.companyName = value;
                        break;
                    case "version":
                        PlayerSettings.bundleVersion = value;
                        break;
                    case "bundle_id":
                        PlayerSettings.SetApplicationIdentifier(named, value);
                        break;
                    case "scripting_backend":
                        if (!Enum.TryParse<ScriptingImplementation>(value, ignoreCase: true, out var backend))
                            throw new ArgumentException($"Invalid scripting_backend value '{value}'. Use 'Mono2x' or 'IL2CPP'.", nameof(value));
                        PlayerSettings.SetScriptingBackend(named, backend);
                        break;
                    default:
                        throw new ArgumentException($"Unknown property '{property}'. Supported: product_name, company_name, version, bundle_id, scripting_backend.", nameof(property));
                }
                AssetDatabase.SaveAssets();

                var readBack = property switch
                {
                    "product_name" => PlayerSettings.productName,
                    "company_name" => PlayerSettings.companyName,
                    "version" => PlayerSettings.bundleVersion,
                    "bundle_id" => PlayerSettings.GetApplicationIdentifier(named),
                    "scripting_backend" => PlayerSettings.GetScriptingBackend(named).ToString(),
                    _ => value
                };

                return new SettingsValueResponse
                {
                    target = target.ToString(),
                    property = property,
                    value = readBack
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // build-execute
        // ──────────────────────────────────────────────────────────────────────

        public const string BuildExecuteToolId = "build-execute";

        [McpPluginTool
        (
            BuildExecuteToolId,
            Title = "Build / Execute",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = true
        )]
        [Description(@"Runs BuildPipeline.BuildPlayer synchronously. Long-running (minutes). Returns a compact BuildReport summary (success flag, output path, total bytes, duration, error/warning counts). If 'scenes' is null or empty, uses EditorBuildSettings.scenes.")]
        public static BuildExecuteResponse Execute
        (
            [Description("Target platform to build for.")]
            BuildTarget target,
            [Description("Output path. For Standalone: full path including the .exe filename. For Android: full .apk path. Parent folders are created if missing.")]
            string outputPath,
            [Description("Optional ordered list of scene paths (Assets/...). When null/empty, uses EditorBuildSettings.scenes.")]
            string[]? scenes = null,
            [Description("Pass true to set BuildOptions.Development.")]
            bool developmentBuild = false
        )
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("'outputPath' is required.", nameof(outputPath));

            return MainThread.Instance.Run(() =>
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var sceneList = scenes != null && scenes.Length > 0
                    ? scenes
                    : (EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>())
                        .Where(s => s.enabled).Select(s => s.path).ToArray();
                if (sceneList.Length == 0)
                    throw new InvalidOperationException("No scenes provided and EditorBuildSettings has no enabled scenes.");

                var opts = new BuildPlayerOptions
                {
                    scenes = sceneList,
                    locationPathName = outputPath,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                    options = developmentBuild ? BuildOptions.Development : BuildOptions.None
                };

                // Editor-side exceptions (toolchain missing, license invalid, etc.) — log full stack to the
                // Editor console so the user can diagnose, then re-throw so the MCP layer surfaces them
                // as proper errors rather than ambiguous "Failed" payloads. BuildPipeline.BuildPlayer
                // does NOT throw on build failures — it returns a BuildReport with a non-Succeeded result.
                BuildReport report;
                try
                {
                    report = BuildPipeline.BuildPlayer(opts);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    throw;
                }

                var summary = report.summary;
                EditorUtils.RepaintAllEditorWindows();

                return new BuildExecuteResponse
                {
                    result = summary.result.ToString(),
                    outputPath = summary.outputPath,
                    totalSizeBytes = summary.totalSize,
                    totalTimeMs = (long)summary.totalTime.TotalMilliseconds,
                    totalErrors = summary.totalErrors,
                    totalWarnings = summary.totalWarnings,
                    errorMessage = summary.result == BuildResult.Succeeded ? null : $"BuildResult: {summary.result}"
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // DTOs
        // ──────────────────────────────────────────────────────────────────────

        public class PlatformInfoResponse
        {
            public string activeBuildTarget = string.Empty;
            public string activeBuildTargetGroup = string.Empty;
            public string standaloneSubtarget = string.Empty;
            public bool isBuildTargetSupported;
        }

        public class SceneEntry
        {
            public string path = string.Empty;
            public bool enabled = true;
            public string? guid;
        }

        public class ScenesListResponse
        {
            public SceneEntry[] scenes = Array.Empty<SceneEntry>();
        }

        public class SettingsValueResponse
        {
            public string target = string.Empty;
            public string property = string.Empty;
            public string value = string.Empty;
        }

        public class BuildExecuteResponse
        {
            public string result = string.Empty;
            public string outputPath = string.Empty;
            public ulong totalSizeBytes;
            public long totalTimeMs;
            public int totalErrors;
            public int totalWarnings;
            public string? errorMessage;
        }
    }
}
