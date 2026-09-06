using System;
using System.IO;
using System.Reflection;
using Graphaclysm.Application;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using Graphaclysm.Runtime.Presentation;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Graphaclysm.Editor
{
    /// <summary>Opt-in, reproducible Game view profiling; never runs in a player.</summary>
    [InitializeOnLoad]
    public static class PolynomiographProfiling
    {
        private const string PendingKey = "GRAPHACLYSM.Profile.Pending";
        private const string ExitKey = "GRAPHACLYSM.Profile.Exit";
        private const string SizeKey = "GRAPHACLYSM.Profile.PreviousSize";
        private static double deadline;

        static PolynomiographProfiling()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (SessionState.GetBool(PendingKey, false))
            {
                deadline = EditorApplication.timeSinceStartup + 180.0;
                EditorApplication.update += CheckTimeout;
            }
        }

        [MenuItem("GRAPHACLYSM/Diagnostics/Profile Polynomiograph")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before starting the rendering profile.");
                return;
            }

            SessionState.SetBool(PendingKey, true);
            deadline = EditorApplication.timeSinceStartup + 180.0;
            EditorApplication.update -= CheckTimeout;
            EditorApplication.update += CheckTimeout;
            EditorWindow gameView = EditorWindow.GetWindow(
                typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.Show();
            gameView.Focus();
            SetFullHdSize(gameView);
            EditorApplication.isPlaying = true;
        }

        private static void SetFullHdSize(EditorWindow gameView)
        {
            // Unity has no public Game view resolution API; keep this adapter Editor-only.
            var assembly = typeof(EditorWindow).Assembly;
            Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            object sizes = sizesType.BaseType.GetProperty("instance").GetValue(null);
            object groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes);
            object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var selectedSize = gameView.GetType().GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < count; i++)
            {
                object size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                int width = (int)size.GetType().GetProperty("width").GetValue(size);
                int height = (int)size.GetType().GetProperty("height").GetValue(size);
                if (width != 1920 || height != 1080) continue;
                SessionState.SetInt(SizeKey, (int)selectedSize.GetValue(gameView));
                selectedSize.SetValue(gameView, i);
                return;
            }
            throw new InvalidOperationException("Full HD Game view preset was not found.");
        }

        // Invoke with Unity -batchmode -projectPath ... -executeMethod
        // Graphaclysm.Editor.PolynomiographProfiling.RunBatch (no -quit / -nographics).
        public static void RunBatch()
        {
            SessionState.SetBool(ExitKey, true);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Run();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode
                && SessionState.GetBool(PendingKey, false))
            {
                new GameObject("Polynomiograph profiling (Editor only)")
                    .AddComponent<PolynomiographProfileRunner>();
            }
        }

        private static void CheckTimeout()
        {
            if (SessionState.GetBool(PendingKey, false)
                && EditorApplication.timeSinceStartup > deadline)
            {
                Debug.LogError("Polynomiograph profile timed out: Game view did not produce enough frames.");
                Finish(1);
            }
        }

        internal static void Finish(int exitCode)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= CheckTimeout;
            EditorWindow gameView = EditorWindow.GetWindow(
                typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            int previousSize = SessionState.GetInt(SizeKey, -1);
            if (previousSize >= 0)
            {
                gameView.GetType().GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SetValue(gameView, previousSize);
                SessionState.EraseInt(SizeKey);
            }
            if (SessionState.GetBool(ExitKey, false))
            {
                SessionState.SetBool(ExitKey, false);
                EditorApplication.Exit(exitCode);
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
        }
    }

    public sealed class PolynomiographProfileRunner : MonoBehaviour
    {
        private const int WarmupFrames = 45;
        private const int SampleFrames = 120;
        private static readonly BaseFunctionKind[] Functions =
        {
            BaseFunctionKind.PolynomiographPentacle,
            BaseFunctionKind.PolynomiographOctave,
            BaseFunctionKind.PolynomiographDodecagram
        };

        private readonly long[] drawTimes = new long[SampleFrames];
        private readonly long[] frameTimes = new long[SampleFrames];
        private readonly long[] gcBytes = new long[SampleFrames];
        private readonly long[] drawGcBytes = new long[SampleFrames];
        private readonly System.Text.StringBuilder report = new System.Text.StringBuilder(4096);
        private GraphaclysmPrototypeView view;
        private PrototypeGameFlow flow;
        private EquationState equation;
        private ProfilerRecorder drawRecorder;
        private ProfilerRecorder frameRecorder;
        private ProfilerRecorder gcRecorder;
        private int frame;
        private int scenario;
        private bool ready;
        private string outputDirectory;
        private int validationStage;
        private float visualPlotStartedAt;
        private bool previousProfilerEnabled;
        private bool previousProfileEditor;
        private bool changedProfiler;

        private void Start()
        {
            try
            {
                view = UnityEngine.Object.FindFirstObjectByType<GraphaclysmPrototypeView>();
                if (view == null) throw new InvalidOperationException("Prototype view was not bootstrapped.");
                view.ProfileFieldLines = true;
                flow = (PrototypeGameFlow)typeof(GraphaclysmPrototypeView)
                    .GetField("flow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                flow.OpenCharacterSelection();
                if (!flow.TryStartRun() || !flow.CurrentRun.TrySelectMapNode(0))
                    throw new InvalidOperationException("Cannot start the profile encounter.");
                equation = flow.CurrentRun.CurrentBattle.Battle.Equation;
                outputDirectory = Path.GetFullPath("Logs/PolynomiographProfile/" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(Path.Combine(outputDirectory, "environment.txt"),
                    "Unity: " + UnityEngine.Application.unityVersion + "\nGPU: " + SystemInfo.graphicsDeviceName
                    + "\nGraphics API: " + SystemInfo.graphicsDeviceType
                    + "\nWarmup frames: " + WarmupFrames + "\nSample frames: " + SampleFrames
                    + "\nEditor Game view, preview, targetFrameRate=60. Main Thread includes frame pacing."
                    + "\nGC frame counter includes other UI and Editor work; draw GC is scoped to the draw call.\n");
                report.AppendLine("renderer,scenario,segments,width,height,draw_mean_ms,draw_p95_ms,frame_mean_ms,gc_mean_bytes,draw_gc_max_bytes");
                drawRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts,
                    "GRAPHACLYSM.Polynomiograph.Draw", 1);
                frameRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
                gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
                SetScenario();
                ready = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                PolynomiographProfiling.Finish(1);
            }
        }

        private void SetScenario()
        {
            view.UseLegacyFieldLineRendering = scenario < Functions.Length;
            equation.SetBase(Functions[scenario % Functions.Length]);
            // This fixture deliberately installs an equation without changing the starting deck.
            // Refresh through the existing view binding before the measurement warmup.
            typeof(GraphaclysmPrototypeView).GetMethod("EnsureInitialized",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            typeof(GraphaclysmPrototypeView).GetMethod("RefreshCachedText",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            frame = 0;
        }

        private void Update()
        {
            if (!ready) return;
            try
            {
                SampleFrame();
            }
            catch (Exception exception)
            {
                ready = false;
                Debug.LogException(exception);
                PolynomiographProfiling.Finish(1);
            }
        }

        private void SampleFrame()
        {
            if (scenario >= Functions.Length * 2)
            {
                ValidateVisuals();
                return;
            }
            frame++;
            if (frame <= WarmupFrames) return;
            if (Screen.width != 1920 || Screen.height != 1080)
                throw new InvalidOperationException("Profiling requires a 1920x1080 Game view.");
            if (!drawRecorder.Valid || !frameRecorder.Valid || !gcRecorder.Valid || drawRecorder.Count == 0)
                throw new InvalidOperationException("Required profiler counters are unavailable.");
            int sample = frame - WarmupFrames - 1;
            if (sample < SampleFrames)
            {
                drawTimes[sample] = drawRecorder.LastValue;
                frameTimes[sample] = frameRecorder.LastValue;
                gcBytes[sample] = gcRecorder.LastValue;
                drawGcBytes[sample] = view.FieldLineDrawAllocatedBytes;
                return;
            }

            double drawMean = Mean(drawTimes) / 1000000.0;
            double frameMean = Mean(frameTimes) / 1000000.0;
            double gcMean = Mean(gcBytes);
            Array.Sort(drawTimes);
            Array.Sort(drawGcBytes);
            double p95 = drawTimes[(int)(SampleFrames * 0.95) - 1] / 1000000.0;
            string row = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5:F4},{6:F4},{7:F4},{8:F0},{9}",
                view.UseLegacyFieldLineRendering ? "IMGUI" : "GL",
                Functions[scenario % Functions.Length], equation.FieldLineSegmentCount, Screen.width, Screen.height,
                drawMean, p95, frameMean, gcMean, drawGcBytes[SampleFrames - 1]);
            report.AppendLine(row);
            Debug.Log("POLYNOMIOGRAPH_PROFILE " + row);
            if (drawMean <= 0)
            {
                Debug.LogError("No draw marker samples were recorded; rendering profile is invalid.");
                PolynomiographProfiling.Finish(1);
                return;
            }

            CaptureGameView((view.UseLegacyFieldLineRendering ? "IMGUI-" : "GL-")
                + Functions[scenario % Functions.Length]);
            scenario++;
            if (scenario < Functions.Length * 2)
            {
                SetScenario();
                return;
            }

            File.WriteAllText(Path.Combine(outputDirectory, "profile.csv"), report.ToString());
            DisposeRecorders();
            frame = 0;
            previousProfilerEnabled = ProfilerDriver.enabled;
            previousProfileEditor = ProfilerDriver.profileEditor;
            changedProfiler = true;
            ProfilerDriver.profileEditor = true;
            ProfilerDriver.enabled = true;
        }

        private void ValidateVisuals()
        {
            frame++;
            switch (validationStage)
            {
                case 0:
                    if (frame < 60) return;
                    CaptureAllocationParents();
                    ProfilerDriver.enabled = previousProfilerEnabled;
                    ProfilerDriver.profileEditor = previousProfileEditor;
                    changedProfiler = false;
                    equation.SetBase(BaseFunctionKind.PolynomiographDodecagram);
                    if (!equation.TryAddModifier(EquationModifierKind.ShiftUp)
                        || !equation.TryAddModifier(EquationModifierKind.Stretch)
                        || !equation.TryAddModifier(EquationModifierKind.Negate))
                        throw new InvalidOperationException("Cannot prepare transformed contour fixture.");
                    RefreshView();
                    validationStage++;
                    frame = 0;
                    return;
                case 1:
                    if (frame < 4) return;
                    CaptureGameView("GL-Transforms");
                    if (!equation.TryRemoveLastStep())
                        throw new InvalidOperationException("Cannot remove the last contour transform.");
                    RefreshView();
                    validationStage++;
                    frame = 0;
                    return;
                case 2:
                    if (frame < 4) return;
                    CaptureGameView("GL-RemovedTransform");
                    equation.SetBase(BaseFunctionKind.PolynomiographDodecagram);
                    RefreshView();
                    typeof(GraphaclysmPrototypeView).GetMethod("StartPlot",
                        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                    visualPlotStartedAt = Time.unscaledTime;
                    validationStage++;
                    return;
                case 3:
                    if (Time.unscaledTime - visualPlotStartedAt < 0.6f) return;
                    if (flow.CurrentRun.CurrentBattle.Battle.Phase != BattlePhase.Plotting)
                        throw new InvalidOperationException("Plot animation was not observed.");
                    CaptureGameView("GL-PlotHalf");
                    validationStage++;
                    return;
                default:
                    if (Time.unscaledTime - visualPlotStartedAt < 1.4f) return;
                    BattleSession battle = flow.CurrentRun.CurrentBattle.Battle;
                    if (battle.Phase != BattlePhase.PlayerPlanning || battle.Equation.HasBase)
                        throw new InvalidOperationException("Plot did not resolve into a cleared next turn.");
                    CaptureGameView("GL-NextTurn");
                    File.WriteAllText(Path.Combine(outputDirectory, "visual-checks.txt"),
                        "PASS: transformed field, last transform removal, partial plot, cleared next turn.\n"
                        + "Screenshots require visual inspection for color, position, and overlay order.\n");
                    Debug.Log("POLYNOMIOGRAPH_PROFILE_COMPLETE " + outputDirectory);
                    ready = false;
                    PolynomiographProfiling.Finish(0);
                    return;
            }
        }

        private void RefreshView()
        {
            typeof(GraphaclysmPrototypeView).GetMethod("RefreshCachedText",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
        }

        private void CaptureAllocationParents()
        {
            // Diagnose whole-frame GC separately, after timing has finished.
            var totals = new System.Collections.Generic.Dictionary<string, long>();
            int last = ProfilerDriver.lastFrameIndex;
            int first = Math.Max(ProfilerDriver.firstFrameIndex, last - 9);
            int captured = 0;
            for (int index = first; index <= last; index++)
            {
                using (var data = ProfilerDriver.GetRawFrameDataView(index, 0))
                {
                    if (!data.valid) continue;
                    captured++;
                    var parents = new int[data.sampleCount];
                    var ends = new int[data.sampleCount];
                    int depth = 0;
                    for (int i = 0; i < data.sampleCount; i++)
                    {
                        while (depth > 0 && i > ends[depth - 1]) depth--;
                        if (data.GetSampleName(i) == "GC.Alloc")
                        {
                            var path = new System.Text.StringBuilder();
                            for (int p = 0; p < depth; p++)
                                path.Append(data.GetSampleName(parents[p])).Append(" / ");
                            string key = path.ToString();
                            totals.TryGetValue(key, out long bytes);
                            totals[key] = bytes + data.GetSampleMetadataAsLong(i, 0);
                        }
                        int children = data.GetSampleChildrenCountRecursive(i);
                        if (children <= 0) continue;
                        parents[depth] = i;
                        ends[depth] = i + children;
                        depth++;
                    }
                }
            }
            var output = new System.Text.StringBuilder("Main thread allocation parents, frames: ")
                .Append(captured).AppendLine();
            foreach (var entry in totals) output.Append(entry.Value).Append(" bytes: ").AppendLine(entry.Key);
            File.WriteAllText(Path.Combine(outputDirectory, "gc-allocation-parents.txt"), output.ToString());
        }

        private void CaptureGameView(string name)
        {
            EditorWindow gameView = EditorWindow.GetWindow(
                typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            var target = (RenderTexture)typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeView")
                .GetField("m_TargetTexture",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(gameView);
            if (target == null) throw new InvalidOperationException("Game view has no rendered texture.");
            RenderTexture previous = RenderTexture.active;
            Texture2D capture = null;
            try
            {
                RenderTexture.active = target;
                capture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                // The Editor's D3D Game view render target is vertically inverted.
                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    var pixels = capture.GetPixelData<Color32>(0);
                    for (int y = 0; y < target.height / 2; y++)
                    {
                        int opposite = target.height - y - 1;
                        for (int x = 0; x < target.width; x++)
                        {
                            int a = y * target.width + x;
                            int b = opposite * target.width + x;
                            Color32 temporary = pixels[a];
                            pixels[a] = pixels[b];
                            pixels[b] = temporary;
                        }
                    }
                }
                capture.Apply();
                File.WriteAllBytes(Path.Combine(outputDirectory, name + ".png"), capture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (capture != null) Destroy(capture);
            }
        }

        private static double Mean(long[] values)
        {
            double sum = 0;
            for (int i = 0; i < values.Length; i++) sum += values[i];
            return sum / values.Length;
        }

        private void DisposeRecorders()
        {
            drawRecorder.Dispose();
            frameRecorder.Dispose();
            gcRecorder.Dispose();
        }

        private void OnDestroy()
        {
            if (changedProfiler)
            {
                ProfilerDriver.enabled = previousProfilerEnabled;
                ProfilerDriver.profileEditor = previousProfileEditor;
            }
            if (view != null)
            {
                view.ProfileFieldLines = false;
                view.UseLegacyFieldLineRendering = false;
            }
            DisposeRecorders();
        }
    }
}
