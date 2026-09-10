using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Runtime.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Graphaclysm.Editor
{
    [InitializeOnLoad]
    public static class BasicsV9Smoke
    {
        private const string Key = "GRAPHACLYSM.BasicsV9Smoke";
        static BasicsV9Smoke()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false); new GameObject("Basics V9 fixture").AddComponent<BasicsV9Runner>();
            };
        }
        public static void RunBatch()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Explicit batch diagnostics only.");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var window = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            window.Show(); window.Focus();
            typeof(PolynomiographProfiling).GetMethod("SetFullHdSize", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { window });
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }

        public static void BuildWindowsBatch()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Explicit batch build only.");
            string directory = Path.GetFullPath("../Builds/GraphaclysmV9"); Directory.CreateDirectory(directory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = Path.Combine(directory, "GRAPHACLYSM.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            File.WriteAllText(Path.Combine(directory, "build-result.txt"), report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings);
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }

    public sealed class BasicsV9Runner : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private GraphaclysmModernView view;
        private PrototypeGameFlow flow;
        private string output, dataDirectory;
        private int errors, captures;
        private IEnumerator scenario;
        private double deadline;
        private void Start()
        {
            output = Path.GetFullPath("Logs/BasicsV9Captures"); Directory.CreateDirectory(output);
            dataDirectory = Path.Combine(output, "UserData-" + Guid.NewGuid().ToString("N"));
            UnityEngine.Application.logMessageReceived += OnLog;
            view = FindFirstObjectByType<GraphaclysmModernView>(); ConfigureView();
            deadline = EditorApplication.timeSinceStartup + 180;
            scenario = Scenario(); StartCoroutine(Guarded());
        }
        private void ConfigureView()
        {
            var legacyStore = new LegacyProgressionStore(dataDirectory);
            var legacy = legacyStore.Load();
            Set("legacyStore", legacyStore); Set("legacy", legacy);
            Set("saves", new RunSaveStore(dataDirectory)); Set("preferenceStore", new GamePreferencesStore(dataDirectory));
            var preferences = new GamePreferencesStore(dataDirectory).Load();
            preferences.TutorialCompleted = true; preferences.PauseOnFocusLoss = false;
            Set("preferences", preferences); Invoke("ApplyPreferences", false);
            Set("persistenceEnabled", true);
            flow = Get<PrototypeGameFlow>("flow"); flow.LegacyBenefits = legacy.Benefits; Invoke("ReadContinue");
        }
        private IEnumerator Guarded()
        {
            while (true)
            {
                object next;
                try
                {
                    if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Smoke timeout");
                    if (!scenario.MoveNext()) break;
                    next = scenario.Current;
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(output, "failure.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); yield break;
                }
                yield return next;
            }
            File.WriteAllText(Path.Combine(output, "smoke-result.txt"), "Captures: " + captures + "\nRuntime errors: " + errors +
                "\nReal generated battle and local save; permanent tree, run growth, illustrated relic, cold view recreation, card/move undo, condense, pause during plot, help freeze, settings, inventory, overwrite prompt, 1080p/720p.\nNo player save files used. Not a full-run balance or Profiler test.");
            EditorApplication.Exit(errors == 0 ? 0 : 1);
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++; }
        private IEnumerator Scenario()
        {
            yield return Shot("01-title-new");
            var fixtureLegacy = Get<LegacyProgression>("legacy");
            fixtureLegacy.AwardRun(7001, false, 3); fixtureLegacy.AwardRun(7002, false, 3); fixtureLegacy.AwardRun(7003, false, 3);
            Set("legacyOpen", true); yield return Shot("01b-legacy-tree"); Set("legacyOpen", false);
            Check(fixtureLegacy.TryPurchase(3) && fixtureLegacy.TryPurchase(3) && fixtureLegacy.TryPurchase(3), "Legacy fixture purchase failed");
            flow.LegacyBenefits = fixtureLegacy.Benefits;
            Invoke("OpenSettings"); yield return Shot("02-settings");
            var prefs = Get<GamePreferences>("preferences"); prefs.MasterVolume = 35; prefs.EffectsVolume = 0; prefs.ReduceMotion = true;
            Set("settingsDirty", true); Invoke("CloseSettings");
            Check(new GamePreferencesStore(dataDirectory).Load().MasterVolume == 35, "Settings not persisted");
            Check(Mathf.Abs(AudioListener.volume - .35f) < .01f, "Master volume not applied");
            Invoke("OpenHelp");
            for (int i = 0; i < 5; i++) { Set("helpPage", i); yield return Shot("03-guide-" + (i + 1)); }
            Invoke("CloseHelp");
            flow.OpenCharacterSelection(); Invoke("Refresh"); view.DiagnosticHoveredCharacter = -2;
            yield return Shot("04-character-pair");
            view.DiagnosticHoveredCharacter = 0; yield return Shot("04a-character-hover-ian");
            view.DiagnosticHoveredCharacter = 1; yield return Shot("04b-character-hover-luna");
            view.DiagnosticHoveredCharacter = -1; flow.TrySelectCharacter(1); flow.TryStartRun(); Invoke("Refresh");
            yield return Shot("04-map-saved");
            Set("growthOpen", true); yield return Shot("04c-run-growth"); Set("growthOpen", false);
            Check(flow.CurrentRun.TryPurchaseGrowthNode(0), "Run skill unlock failed"); Invoke("Refresh");
            Check(File.Exists(Path.Combine(dataDirectory, "run.save")), "New run not saved");
            flow.CurrentRun.TrySelectMapNode(0); Invoke("Refresh");
            Invoke("PlayCard", 0); Check(TryAnyMove(flow.CurrentRun), "Move failed"); Invoke("Refresh");
            string card = flow.CurrentRun.CurrentBattle.Battle.GetPlayedCard(0).Id;
            double movedX = flow.CurrentRun.CurrentBattle.Battle.Tactics.X;
            double movedY = flow.CurrentRun.CurrentBattle.Battle.Tactics.Y;
            yield return Shot("05-battle-saved");
            view.DiagnosticHoveredCard = 0; yield return Shot("05b-card-book-hover");
            view.DiagnosticKeyword = (int)flow.CurrentRun.CurrentBattle.Deck.GetHandCard(0).GetAbility(0).Kind;
            yield return Shot("05c-keyword-tooltip");
            view.DiagnosticHoveredCard = -1; view.DiagnosticKeyword = -1;
            Set("paused", true); yield return Shot("06-pause");
            Invoke("OpenInventory"); yield return Shot("07-inventory");
            foreach (var relic in FragmentRelicCatalog.All)
                if (!string.IsNullOrEmpty(relic.ImageResource)) { flow.CurrentRun.Relics.TryAdd(relic); break; }
            Set("inventoryRelics", true); Invoke("SelectInventory", 0); yield return Shot("08-inventory-relics");
            Set("inventoryOpen", false); Invoke("SaveAndReturn");
            yield return Shot("09-title-continue");
            Invoke("RequestNewRun"); Check(Get<object>("confirmation").ToString() == "NewRun", "Missing overwrite confirmation");
            yield return Shot("10-overwrite-confirmation");
            Set("confirmation", Enum.ToObject(typeof(GraphaclysmModernView).GetField("confirmation", Private).FieldType, 0));
            // Recreate the entire view and flow, then read the file as on a new launch.
            Destroy(view.gameObject); yield return null;
            view = new GameObject("Cold resumed application").AddComponent<GraphaclysmModernView>(); ConfigureView();
            Check(Get<GamePreferences>("preferences").MasterVolume == 35 && Get<GamePreferences>("preferences").ReduceMotion, "Cold settings lost");
            Invoke("ContinueSavedRun");
            Check(flow.CurrentRun.CurrentBattle.Battle.GetPlayedCard(0).Id == card, "Cold continue lost the weave");
            Check(flow.CurrentRun.CurrentBattle.Battle.Tactics.X == movedX && flow.CurrentRun.CurrentBattle.Battle.Tactics.Y == movedY, "Cold continue lost movement");
            Check(flow.CurrentRun.TryUndoMove(), "Restored movement cannot be undone");
            Check(flow.CurrentRun.TryUndoLastPlayedCard(out _), "Restored card cannot be undone");
            Invoke("Refresh");
            var store = Get<RunSaveStore>("saves");
            Directory.CreateDirectory(store.FilePath + ".tmp");
            Invoke("PlayCard", 0); Invoke("SaveAndReturn");
            Check(flow.Phase == GameFlowPhase.Run && Get<bool>("saveFailed"), "Failed save left the run or hid the error");
            yield return Shot("10b-save-failure");
            Directory.Delete(store.FilePath + ".tmp");
            Invoke("SaveAndReturn"); Invoke("ContinueSavedRun"); Invoke("Undo");
            Check(store.TryLoad(out var retried, out _, out _) && retried.CurrentBattle.Battle.PlayedCardCount == 0, "Successful retry kept autosave throttled");
            Invoke("PlayCard", 0); Invoke("Condense");
            Check(flow.CurrentRun.CurrentBattle.Battle.SealedCardCount == 1, "Condense did not seal");
            Invoke("SaveAndReturn"); Invoke("ContinueSavedRun");
            Check(!flow.CurrentRun.TryUndoLastPlayedCard(out _), "Continue bypassed seal");
            Invoke("StartCast"); Set("paused", true);
            float frozen = Get<float>("viewTime");
            yield return new WaitForSecondsRealtime(2.1f);
            Check(Get<float>("viewTime") == frozen, "Pause advanced presentation time");
            Check(flow.CurrentRun.CurrentBattle.Battle.Phase == BattlePhase.Plotting, "Paused plot resolved");
            yield return Shot("11-paused-cast");
            Invoke("OpenHelp"); Set("paused", false); yield return new WaitForSecondsRealtime(1.2f);
            Check(flow.CurrentRun.CurrentBattle.Battle.Phase == BattlePhase.Plotting, "Help did not pause");
            Invoke("CloseHelp"); Set("paused", true); Invoke("SaveAndReturn"); Invoke("ContinueSavedRun");
            yield return new WaitForSecondsRealtime(2.3f);
            Check(flow.CurrentRun.Phase != RunPhase.Battle || flow.CurrentRun.CurrentBattle.Battle.Phase == BattlePhase.PlayerPlanning, "Resumed cast stuck");
            yield return Shot("12-resumed-battle");
            SetSize(1280, 720); Set("paused", true); yield return Shot("13-pause-720p");
            Invoke("OpenSettings"); yield return Shot("14-settings-720p"); Invoke("CloseSettings");
            Invoke("OpenHelp"); Set("helpPage", 3); yield return Shot("15-guide-720p"); Invoke("CloseHelp");
            Invoke("OpenInventory"); yield return Shot("16-inventory-720p"); Set("inventoryOpen", false);
            Invoke("SaveAndReturn"); yield return Shot("17-title-720p");
            SetSize(1920, 1080);
        }
        private IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.25f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png")); captures++;
            yield return new WaitForSecondsRealtime(.15f);
        }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static bool TryAnyMove(RunGameSession run)
            => run.TryMovePlayer(-1.5, 0) || run.TryMovePlayer(1.5, 0)
                || run.TryMovePlayer(0, 1.5) || run.TryMovePlayer(0, -1.5);
        private void Set(string name, object value) => typeof(GraphaclysmModernView).GetField(name, Private).SetValue(view, value);
        private T Get<T>(string name) => (T)typeof(GraphaclysmModernView).GetField(name, Private).GetValue(view);
        private void Invoke(string name, params object[] arguments) => typeof(GraphaclysmModernView).GetMethod(name, Private).Invoke(view, arguments);
        private static void SetSize(int w, int h) => typeof(VisualV3SmokeRunner).GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { w, h });
    }
}
