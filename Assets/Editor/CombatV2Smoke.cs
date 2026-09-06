using System;
using System.IO;
using System.Reflection;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Runtime.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Graphaclysm.Editor
{
    /// <summary>Explicit batch-only visual fixture; never runs automatically in the user's Editor.</summary>
    [InitializeOnLoad]
    public static class CombatV2Smoke
    {
        private const string Key = "GRAPHACLYSM.CombatV2Smoke";
        static CombatV2Smoke()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                {
                    SessionState.SetBool(Key, false);
                    new GameObject("Combat V2 visual fixture").AddComponent<CombatV2SmokeRunner>();
                }
            };
        }

        public static void RunBatch()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Use the isolated batch verification project.");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.Show(); gameView.Focus();
            typeof(PolynomiographProfiling).GetMethod("SetFullHdSize", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { gameView });
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
    }

    public sealed class CombatV2SmokeRunner : MonoBehaviour
    {
        private GraphaclysmPrototypeView view;
        private PrototypeGameFlow flow;
        private string output;
        private int frame, stage, errors;
        private float frozenProgress = -1;
        private double deadline;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private void Start()
        {
            // Batch copy lives at Logs/CombatV2VerificationProject; export into parent Logs.
            output = Path.GetFullPath("../CombatV2Captures");
            Directory.CreateDirectory(output);
            deadline = EditorApplication.timeSinceStartup + 90;
            UnityEngine.Application.logMessageReceived += OnLog;
            view = FindFirstObjectByType<GraphaclysmPrototypeView>();
            flow = (PrototypeGameFlow)typeof(GraphaclysmPrototypeView).GetField("flow", Private).GetValue(view);
        }

        private void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++;
        }

        private void LateUpdate()
        {
            if (frozenProgress >= 0)
                typeof(GraphaclysmPrototypeView).GetField("plotStartedAt", Private)
                    .SetValue(view, Time.unscaledTime - frozenProgress * 1.15f);
        }

        private void Update()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Combat V2 capture timeout.");
                if (++frame < 18) return;
                frame = 0;
                if (Screen.width != 1920 || Screen.height != 1080) throw new InvalidOperationException("Full HD fixture expected.");
                switch (stage++)
                {
                    case 0: Capture("01-title"); break;
                    case 1: flow.OpenCharacterSelection(); Refresh(); break;
                    case 2: Capture("02-characters"); break;
                    case 3: Setup(0); break;
                    case 4: Capture("03-battle"); break;
                    case 5: Play("input.x"); Play("coef.down"); Play("sigil.ward"); Play("sigil.ember"); Refresh(); break;
                    case 6: Capture("04-dual-hit-preview"); break;
                    case 7:
                        if (!flow.CurrentRun.CurrentBattle.Battle.PreviewPlayerHit) throw new InvalidOperationException("Expected self hit.");
                        flow.CurrentRun.TryToggleUltimate(); Refresh(); break;
                    case 8: Capture("05-ian-ultimate"); break;
                    case 9: Invoke("StartPlot"); frozenProgress = 0.22f; break;
                    case 10: Capture("06-reveal-inner"); break;
                    case 11: frozenProgress = 0.65f; break;
                    case 12: Capture("07-reveal-outer"); break;
                    case 13: frozenProgress = -1; Invoke("ResolvePlot"); break;
                    case 14: Capture("08-after-turn"); break;
                    case 15:
                        if (flow.CurrentRun.CurrentBattle.Battle.Turn != 2) throw new InvalidOperationException("Enemy turn did not advance.");
                        Setup(1); Play("input.x"); flow.CurrentRun.TryToggleUltimate(); Refresh(); break;
                    case 16: Capture("09-luna-ultimate"); break;
                    case 17: Invoke("StartPlot"); frozenProgress = 0.75f; break;
                    case 18: frozenProgress = -1; Invoke("ResolvePlot"); break;
                    case 19: Capture("10-luna-after-turn"); break;
                    case 20: view.DiagnosticHoveredCard = 0; break;
                    case 21: Capture("11-card-tooltip"); break;
                    case 22:
                        view.DiagnosticHoveredCard = -1;
                        var run = flow.CurrentRun;
                        var cards = new[] { SpellCardCatalog.Find("input.x"), SpellCardCatalog.Find("coef.down") };
                        var rewardBattle = new BattleSession(new BattleDefinition(42, 6,
                            new[] { new EnemyDefinition("reward.fixture", "Fixture", 5, -1, 1, 0) }, CombatArchetype.Luna));
                        typeof(RunGameSession).GetProperty("CurrentBattle").SetValue(run,
                            new BattleGameSession(rewardBattle, new DeckSession(cards, 2, new XorShiftRandom(123))));
                        Invoke("EnsureInitialized"); Play("input.x"); Play("coef.down");
                        run.TryBeginPlot(); run.ResolvePlot(); Refresh(); break;
                    case 23: Capture("12-card-reward"); break;
                    case 24:
                        File.WriteAllText(Path.Combine(output, "smoke-result.txt"), "1920x1080 | Ian and Luna | dual hit | radial reveal | enemy turn\nErrors: " + errors);
                        EditorApplication.Exit(errors == 0 ? 0 : 1); break;
                }
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(output ?? "Logs", "smoke-failure.txt"), exception.ToString());
                Debug.LogException(exception); EditorApplication.Exit(1);
            }
        }

        private void Setup(int characterIndex)
        {
            flow.ReturnToCharacterSelection(); flow.TrySelectCharacter(characterIndex); flow.TryStartRun();
            var run = flow.CurrentRun; run.TrySelectMapNode(0);
            var cards = new[] { SpellCardCatalog.Find("input.x"), SpellCardCatalog.Find("coef.down"),
                SpellCardCatalog.Find("sigil.ward"), SpellCardCatalog.Find("sigil.ember"),
                SpellCardCatalog.Find("op.sin"), SpellCardCatalog.Find("coef.half") };
            var battle = new BattleSession(new BattleDefinition(42, 6, new[] {
                new EnemyDefinition("fixture.left", "균열 잔영", 2, -1, 100, 3),
                new EnemyDefinition("fixture.right", "유리 파편", 7, 1, 100, 4),
                new EnemyDefinition("fixture.core", "침묵의 핵", 5, -1, 100, 4,
                    EnemyBehaviorDefinition.ChargeBurst()) }, flow.CurrentCharacter.Archetype), 32, 0, 0, 6);
            var deck = new DeckSession(cards, cards.Length, new XorShiftRandom(123));
            typeof(RunGameSession).GetProperty("CurrentBattle").SetValue(run, new BattleGameSession(battle, deck));
            Invoke("EnsureInitialized"); Refresh();
        }

        private void Play(string id)
        {
            var run = flow.CurrentRun;
            for (int i = 0; i < run.CurrentBattle.Deck.HandCount; i++)
                if (run.CurrentBattle.Deck.GetHandCard(i).Id == id)
                {
                    if (!run.TryPlayHandCard(i, out _, out _)) throw new InvalidOperationException("Fixture card failed.");
                    return;
                }
            throw new InvalidOperationException("Fixture card missing.");
        }

        private void Refresh() => Invoke("RefreshCachedText");
        private void Invoke(string name) => typeof(GraphaclysmPrototypeView).GetMethod(name, Private).Invoke(view, null);
        private void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
    }
}
