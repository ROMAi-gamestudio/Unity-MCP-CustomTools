#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using Unity.Profiling;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ROMAi.MCPTools.Editor
{
    [McpPluginToolType]
    public static partial class Tool_Profiler
    {
        // ──────────────────────────────────────────────────────────────────────
        // profiler-start
        // ──────────────────────────────────────────────────────────────────────

        public const string ProfilerStartToolId = "profiler-start";

        [McpPluginTool
        (
            ProfilerStartToolId,
            Title = "Profiler / Start",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Starts the Unity Profiler (ProfilerDriver.enabled = true). Idempotent: calling twice leaves the profiler enabled. To save the captured data, call profiler-stop with saveCapturePath.")]
        public static ProfilerStatusResponse ProfilerStart
        (
            [Description("If true, profile the Editor itself instead of the Player. Default: false.")]
            bool profileEditor = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
                ProfilerDriver.enabled = true;
                ProfilerDriver.profileEditor = profileEditor;
                return BuildStatus();
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // profiler-stop
        // ──────────────────────────────────────────────────────────────────────

        public const string ProfilerStopToolId = "profiler-stop";

        [McpPluginTool
        (
            ProfilerStopToolId,
            Title = "Profiler / Stop",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Stops the Unity Profiler (ProfilerDriver.enabled = false). If saveCapturePath is provided, also writes the captured data to that file via ProfilerDriver.SaveProfile. Idempotent for the stop action.")]
        public static ProfilerStopResponse ProfilerStop
        (
            [Description("Optional path to write the captured profile data. Unity will append the appropriate extension if missing.")]
            string? saveCapturePath = null
        )
        {
            return MainThread.Instance.Run(() =>
            {
                ProfilerDriver.enabled = false;
                bool saved = false;
                string? saveError = null;
                if (!string.IsNullOrEmpty(saveCapturePath))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(saveCapturePath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        ProfilerDriver.SaveProfile(saveCapturePath);
                        saved = true;
                    }
                    catch (Exception ex)
                    {
                        saveError = ex.Message;
                    }
                }
                var status = BuildStatus();
                return new ProfilerStopResponse
                {
                    enabled = status.enabled,
                    profileEditor = status.profileEditor,
                    deepProfiling = status.deepProfiling,
                    captureSaved = saved,
                    captureSavePath = saved ? saveCapturePath : null,
                    captureSaveError = saveError
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // profiler-status
        // ──────────────────────────────────────────────────────────────────────

        public const string ProfilerStatusToolId = "profiler-status";

        [McpPluginTool
        (
            ProfilerStatusToolId,
            Title = "Profiler / Status",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [Description(@"Returns the current ProfilerDriver state: enabled, profileEditor, deepProfiling.")]
        public static ProfilerStatusResponse ProfilerStatus()
        {
            return MainThread.Instance.Run(BuildStatus);
        }

        private static ProfilerStatusResponse BuildStatus()
        {
            return new ProfilerStatusResponse
            {
                enabled = ProfilerDriver.enabled,
                profileEditor = ProfilerDriver.profileEditor,
                deepProfiling = ProfilerDriver.deepProfiling
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // profiler-get-frame-timing
        // ──────────────────────────────────────────────────────────────────────

        public const string ProfilerGetFrameTimingToolId = "profiler-get-frame-timing";

        [McpPluginTool
        (
            ProfilerGetFrameTimingToolId,
            Title = "Profiler / Get Frame Timing",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [Description(@"Reads the most recent FrameTiming sample via FrameTimingManager. Returns CPU/GPU times in milliseconds. May return zeros if Frame Timing Stats are disabled or no frame has rendered recently.")]
        public static FrameTimingResponse FrameTimingGet()
        {
            return MainThread.Instance.Run(() =>
            {
                FrameTimingManager.CaptureFrameTimings();
                var timings = new FrameTiming[1];
                var count = FrameTimingManager.GetLatestTimings(1, timings);

                if (count == 0)
                {
                    return new FrameTimingResponse
                    {
                        sampleCount = 0,
                        note = "No frame timings available. In Editor, ensure Frame Timing Stats is enabled in Player Settings, and that the Game View has rendered recently."
                    };
                }

                var t = timings[0];
                return new FrameTimingResponse
                {
                    sampleCount = (int)count,
                    cpuFrameTimeMs = t.cpuFrameTime,
                    cpuMainThreadFrameTimeMs = t.cpuMainThreadFrameTime,
                    cpuMainThreadPresentWaitTimeMs = t.cpuMainThreadPresentWaitTime,
                    cpuRenderThreadFrameTimeMs = t.cpuRenderThreadFrameTime,
                    gpuFrameTimeMs = t.gpuFrameTime,
                    heightScale = t.heightScale,
                    widthScale = t.widthScale,
                    syncInterval = (int)t.syncInterval
                };
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // profiler-get-counters
        // ──────────────────────────────────────────────────────────────────────

        public const string ProfilerGetCountersToolId = "profiler-get-counters";

        [McpPluginTool
        (
            ProfilerGetCountersToolId,
            Title = "Profiler / Get Counters",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [Description(@"Reads one or more ProfilerRecorder counters from a single category. 'counters' is REQUIRED (no wildcard) to keep responses small. Examples: category='Render', counters=['Batches Count','Triangles Count','SetPass Calls Count'].

LIMITATION: ProfilerRecorder samples are collected per Unity frame. This call returns the most recent value at invocation time. In idle Editor (no rendering, no Play Mode), values are typically 0 because no frame has run since the recorder was created. To get meaningful data, either: (a) enter Play Mode and call during gameplay, or (b) trigger Editor rendering activity (mouse over Game View) immediately before the call.

For each requested counter the response includes 'valid' (was the recorder accepted by Unity), 'lastValue', and 'unit'. valid=false means the counter name is unknown for the given category.")]
        public static CountersResponse CountersGet
        (
            [Description("ProfilerCategory name. Examples: 'Render', 'Memory', 'Internal', 'Scripts', 'GC', 'Physics', 'Audio'.")]
            string category,
            [Description("Counter names within the category. REQUIRED — must contain at least one entry. No wildcard support; pick the specific counters you want.")]
            string[] counters
        )
        {
            if (string.IsNullOrEmpty(category))
                throw new ArgumentException("'category' is required.", nameof(category));
            if (counters == null || counters.Length == 0)
                throw new ArgumentException("'counters' must contain at least one counter name.", nameof(counters));

            return MainThread.Instance.Run(() =>
            {
                var profilerCategory = new ProfilerCategory(category);
                var recorders = new ProfilerRecorder[counters.Length];

                try
                {
                    for (var i = 0; i < counters.Length; i++)
                        recorders[i] = ProfilerRecorder.StartNew(profilerCategory, counters[i]);

                    // Removed Thread.Sleep here: it would block the main thread without advancing
                    // any frames, so no new samples could land. ProfilerRecorder.LastValueAsDouble
                    // returns whatever sample Unity has already collected. See the tool description
                    // for the workflow when values come back as 0.
                    var readings = new List<CounterReading>(counters.Length);
                    for (var i = 0; i < counters.Length; i++)
                    {
                        var rec = recorders[i];
                        readings.Add(new CounterReading
                        {
                            name = counters[i],
                            valid = rec.Valid,
                            lastValue = rec.Valid ? rec.LastValueAsDouble : 0d,
                            unit = rec.Valid ? rec.UnitType.ToString() : null
                        });
                    }

                    return new CountersResponse
                    {
                        category = category,
                        readings = readings.ToArray()
                    };
                }
                finally
                {
                    for (var i = 0; i < recorders.Length; i++)
                    {
                        if (recorders[i].Valid)
                            recorders[i].Dispose();
                    }
                }
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // DTOs
        // ──────────────────────────────────────────────────────────────────────

        public class ProfilerStatusResponse
        {
            public bool enabled;
            public bool profileEditor;
            public bool deepProfiling;
        }

        public class ProfilerStopResponse
        {
            public bool enabled;
            public bool profileEditor;
            public bool deepProfiling;
            public bool captureSaved;
            public string? captureSavePath;
            public string? captureSaveError;
        }

        public class FrameTimingResponse
        {
            public int sampleCount;
            public double cpuFrameTimeMs;
            public double cpuMainThreadFrameTimeMs;
            public double cpuMainThreadPresentWaitTimeMs;
            public double cpuRenderThreadFrameTimeMs;
            public double gpuFrameTimeMs;
            public float heightScale;
            public float widthScale;
            public int syncInterval;
            public string? note;
        }

        public class CounterReading
        {
            public string name = string.Empty;
            public bool valid;
            public double lastValue;
            public string? unit;
        }

        public class CountersResponse
        {
            public string category = string.Empty;
            public CounterReading[] readings = Array.Empty<CounterReading>();
        }
    }
}
