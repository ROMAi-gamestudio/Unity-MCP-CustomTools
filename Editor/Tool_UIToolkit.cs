#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AIGD;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ROMAi.MCPTools.Editor
{
    [McpPluginToolType]
    public static partial class Tool_UIToolkit
    {
        // ──────────────────────────────────────────────────────────────────────
        // ui-link-stylesheet
        // ──────────────────────────────────────────────────────────────────────

        public const string UILinkStylesheetToolId = "ui-link-stylesheet";

        [McpPluginTool
        (
            UILinkStylesheetToolId,
            Title = "UI / Link Stylesheet",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Adds a <Style src=""...""/> reference to a UXML file's root, linking a USS stylesheet. Idempotent: returns alreadyLinked=true if the same src is already present. Both paths must start with 'Assets/'.")]
        public static LinkStylesheetResponse LinkStylesheet
        (
            [Description("Project-relative path to the .uxml file. Must start with 'Assets/' and end with '.uxml'.")]
            string uxmlPath,
            [Description("Project-relative path to the .uss stylesheet to link. Must start with 'Assets/' and end with '.uss'.")]
            string ussPath
        )
        {
            ValidateAssetPath(uxmlPath, ".uxml", nameof(uxmlPath));
            ValidateAssetPath(ussPath, ".uss", nameof(ussPath));

            return MainThread.Instance.Run(() =>
            {
                if (!File.Exists(uxmlPath))
                    throw new FileNotFoundException($"UXML file not found: '{uxmlPath}'.");
                if (!File.Exists(ussPath))
                    throw new FileNotFoundException($"USS file not found: '{ussPath}'.");

                // Read into memory first so the file handle is closed before doc.Save() reopens for write —
                // XDocument.Load defers handle release to GC, which on Windows can race with Save.
                var sourceText = File.ReadAllText(uxmlPath);
                var doc = XDocument.Parse(sourceText, LoadOptions.PreserveWhitespace);
                var root = doc.Root;
                if (root == null)
                    throw new InvalidOperationException($"UXML file '{uxmlPath}' has no root element.");

                // Existing <Style src="ussPath"/> regardless of namespace prefix.
                // Compare normalized: collapse '\' to '/', remove './' segments, case-insensitive
                // (Windows + Unity asset paths are case-insensitive at the filesystem level).
                var normalizedTarget = NormalizeAssetPath(ussPath);
                var existing = root.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Style"
                        && string.Equals(NormalizeAssetPath((string?)e.Attribute("src")),
                                          normalizedTarget,
                                          StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    return new LinkStylesheetResponse
                    {
                        uxmlPath = uxmlPath,
                        ussPath = ussPath,
                        alreadyLinked = true,
                        modified = false
                    };
                }

                // Insert <Style src="..."/> as the first child of the root, reusing whatever
                // namespace the root already uses (XDocument auto-picks the matching prefix).
                XNamespace uiNs = "UnityEngine.UIElements";
                var styleElem = new XElement(uiNs + "Style", new XAttribute("src", ussPath));
                root.AddFirst(styleElem);
                doc.Save(uxmlPath);
                AssetDatabase.ImportAsset(uxmlPath, ImportAssetOptions.ForceUpdate);
                EditorUtils.RepaintAllEditorWindows();

                return new LinkStylesheetResponse
                {
                    uxmlPath = uxmlPath,
                    ussPath = ussPath,
                    alreadyLinked = false,
                    modified = true
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // ui-document-attach
        // ──────────────────────────────────────────────────────────────────────

        public const string UIDocumentAttachToolId = "ui-document-attach";

        [McpPluginTool
        (
            UIDocumentAttachToolId,
            Title = "UI / Document Attach",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Adds a UIDocument component to the target GameObject (or reuses an existing one) and assigns its VisualTreeAsset and optionally PanelSettings. Both asset paths must start with 'Assets/'.")]
        public static DocumentAttachResponse DocumentAttach
        (
            [Description("Reference to the target GameObject in the open scene or prefab.")]
            GameObjectRef gameObjectRef,
            [Description("Project-relative path to the .uxml VisualTreeAsset to assign.")]
            string uxmlPath,
            [Description("Optional project-relative path to a PanelSettings .asset. If null/empty, panelSettings is left untouched.")]
            string? panelSettingsPath = null
        )
        {
            if (gameObjectRef == null)
                throw new ArgumentNullException(nameof(gameObjectRef));
            if (!gameObjectRef.IsValid(out var goErr))
                throw new ArgumentException(goErr, nameof(gameObjectRef));
            ValidateAssetPath(uxmlPath, ".uxml", nameof(uxmlPath));
            if (!string.IsNullOrEmpty(panelSettingsPath))
                ValidateAssetPath(panelSettingsPath!, ".asset", nameof(panelSettingsPath));

            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var resolveErr);
                if (resolveErr != null)
                    throw new Exception(resolveErr);
                if (go == null)
                    throw new Exception("GameObject not found.");

                var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                if (vta == null)
                    throw new Exception($"VisualTreeAsset not found at '{uxmlPath}'.");

                PanelSettings? panel = null;
                if (!string.IsNullOrEmpty(panelSettingsPath))
                {
                    panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
                    if (panel == null)
                        throw new Exception($"PanelSettings not found at '{panelSettingsPath}'.");
                }

                var doc = go.GetComponent<UIDocument>();
                var added = false;
                if (doc == null)
                {
                    doc = go.AddComponent<UIDocument>();
                    added = true;
                }
                doc.visualTreeAsset = vta;
                if (panel != null)
                    doc.panelSettings = panel;

                EditorUtility.SetDirty(doc);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                return new DocumentAttachResponse
                {
                    gameObject = go.name,
#pragma warning disable CS0618 // GetInstanceID is obsolete since Unity 6.5; keep for back-compat with 2022.3+ until package drops support.
                    instanceId = go.GetInstanceID(),
#pragma warning restore CS0618
                    componentAdded = added,
                    visualTreeAssetPath = uxmlPath,
                    panelSettingsPath = panel != null ? panelSettingsPath : null
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // ui-panel-settings-create
        // ──────────────────────────────────────────────────────────────────────

        public const string UIPanelSettingsCreateToolId = "ui-panel-settings-create";

        [McpPluginTool
        (
            UIPanelSettingsCreateToolId,
            Title = "UI / PanelSettings Create",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [Description(@"Creates a PanelSettings ScriptableObject asset at the given path. Creates intermediate folders if needed. The output path must start with 'Assets/' and end with '.asset'.")]
        public static PanelSettingsCreateResponse PanelSettingsCreate
        (
            [Description("Project-relative path for the new PanelSettings asset. Must start with 'Assets/' and end with '.asset'.")]
            string outputPath,
            [Description("Optional reference resolution width in pixels. 0 = leave Unity default.")]
            int referenceWidth = 0,
            [Description("Optional reference resolution height in pixels. 0 = leave Unity default.")]
            int referenceHeight = 0,
            [Description("Scale mode. Default: ConstantPixelSize.")]
            PanelScaleMode scaleMode = PanelScaleMode.ConstantPixelSize
        )
        {
            ValidateAssetPath(outputPath, ".asset", nameof(outputPath));

            return MainThread.Instance.Run(() =>
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }

                var panel = ScriptableObject.CreateInstance<PanelSettings>();
                if (referenceWidth > 0 && referenceHeight > 0)
                    panel.referenceResolution = new Vector2Int(referenceWidth, referenceHeight);
                panel.scaleMode = scaleMode;

                AssetDatabase.CreateAsset(panel, outputPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorUtils.RepaintAllEditorWindows();

                return new PanelSettingsCreateResponse
                {
                    path = outputPath,
#pragma warning disable CS0618
                    instanceId = panel.GetInstanceID(),
#pragma warning restore CS0618
                    referenceWidth = panel.referenceResolution.x,
                    referenceHeight = panel.referenceResolution.y,
                    scaleMode = panel.scaleMode.ToString()
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────

        private static string NormalizeAssetPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            var parts = path!.Replace('\\', '/').Split('/');
            var collapsed = new List<string>(parts.Length);
            foreach (var p in parts)
            {
                if (p.Length == 0 || p == ".")
                    continue;
                collapsed.Add(p);
            }
            return string.Join("/", collapsed);
        }

        private static void ValidateAssetPath(string path, string requiredExt, string paramName)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException($"'{paramName}' is required.", paramName);
            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException($"'{paramName}' must start with 'Assets/'. Got: '{path}'.", paramName);
            if (!path.EndsWith(requiredExt, StringComparison.Ordinal))
                throw new ArgumentException($"'{paramName}' must end with '{requiredExt}'. Got: '{path}'.", paramName);
            // Reject '..' segments — StartsWith("Assets/") alone would let "Assets/../etc/passwd.uxml" through.
            foreach (var segment in path.Split('/', '\\'))
            {
                if (segment == "..")
                    throw new ArgumentException($"'{paramName}' must not contain '..' segments. Got: '{path}'.", paramName);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Response DTOs
        // ──────────────────────────────────────────────────────────────────────

        public class LinkStylesheetResponse
        {
            public string uxmlPath = string.Empty;
            public string ussPath = string.Empty;
            public bool alreadyLinked;
            public bool modified;
        }

        public class DocumentAttachResponse
        {
            public string gameObject = string.Empty;
            public int instanceId;
            public bool componentAdded;
            public string visualTreeAssetPath = string.Empty;
            public string? panelSettingsPath;
        }

        public class PanelSettingsCreateResponse
        {
            public string path = string.Empty;
            public int instanceId;
            public int referenceWidth;
            public int referenceHeight;
            public string scaleMode = string.Empty;
        }
    }
}
