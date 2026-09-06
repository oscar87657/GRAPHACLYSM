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
    [InitializeOnLoad]
    public static class VisualV3Smoke
    {
        private const string Key = "GRAPHACLYSM.VisualV3Smoke";
        static VisualV3Smoke()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false);
                new GameObject("Visual V3 fixture").AddComponent<VisualV3SmokeRunner>();
            };
        }
        public static void RunBatch()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Isolated batch project only.");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var window = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            window.Show(); window.Focus();
            typeof(PolynomiographProfiling).GetMethod("SetFullHdSize", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { window });
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
    }

    public sealed class VisualV3SmokeRunner : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GraphaclysmModernView view;
        private PrototypeGameFlow flow;
        private string output;
        private int frames, stage, errors;
        private double deadline;
        private void Start()
        {
            output = Path.GetFullPath("../VisualV3Captures"); Directory.CreateDirectory(output);
            deadline = EditorApplication.timeSinceStartup + 120;
            UnityEngine.Application.logMessageReceived += OnLog;
            view = FindFirstObjectByType<GraphaclysmModernView>();
            if (view == null) { Debug.LogError("Modern bootstrap did not start."); EditorApplication.Exit(1); return; }
            flow = (PrototypeGameFlow)typeof(GraphaclysmModernView).GetField("flow", Private).GetValue(view);
        }
        private void OnLog(string condition, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++; }
        private void Update()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Visual fixture timed out.");
                if (++frames < 22) return; frames = 0;
                switch (stage++)
                {
                    case 0: Capture("01-title"); break;
                    case 1: flow.OpenCharacterSelection(); Refresh(); break;
                    case 2: Capture("02-ian"); break;
                    case 3: flow.TrySelectCharacter(1); Refresh(); break;
                    case 4: Capture("03-luna"); break;
                    case 5: flow.TryStartRun(); Refresh(); break;
                    case 6: Capture("04-map"); break;
                    case 7: flow.CurrentRun.TrySelectMapNode(0); Refresh(); break;
                    case 8: Capture("05-live-opening-hand"); break;
                    case 9: Setup(0); break;
                    case 10: Capture("06-battle-fixture"); break;
                    case 11: view.DiagnosticHoveredCard = 2; break;
                    case 12: Capture("07-card-detail"); break;
                    case 13: view.DiagnosticHoveredCard = -1; Play("skill.focus"); Play("skill.echo"); Play("skill.fall"); Refresh(); break;
                    case 14: Capture("08-dual-hit-preview"); break;
                    case 15:
                        if (!flow.CurrentRun.CurrentBattle.Battle.PreviewPlayerHit) throw new InvalidOperationException("Self-hit fixture missed.");
                        Invoke("StartCast"); view.DiagnosticCastTime = 0.38f; break;
                    case 16: Capture("09-ink-inner"); break;
                    case 17: view.DiagnosticCastTime = 0.79f; break;
                    case 18: Capture("10-ink-outer"); break;
                    case 19: view.DiagnosticCastTime = 1.25f; break;
                    case 20: Capture("11-impact"); break;
                    case 21: view.DiagnosticCastTime = 1.86f; break;
                    case 22: Capture("12-next-turn"); break;
                    case 23: Setup(0); Play("skill.focus"); Play("skill.fall"); flow.CurrentRun.TryToggleUltimate(); Refresh(); Invoke("StartCast"); view.DiagnosticCastTime = 0.79f; break;
                    case 24: Capture("13-ian-ultimate"); break;
                    case 25: Setup(1); Play("skill.focus"); Play("skill.fall"); flow.CurrentRun.TryToggleUltimate(); Refresh(); Invoke("StartCast"); view.DiagnosticCastTime = 0.79f; break;
                    case 26: Capture("14-luna-ultimate"); break;
                    case 27: Setup(0, 1); Play("skill.focus"); Play("skill.fall"); flow.CurrentRun.TryBeginPlot(); flow.CurrentRun.ResolvePlot(); view.DiagnosticRewardHover = 1; Refresh(); break;
                    case 28: Expect(RunPhase.CardReward); Capture("15-card-reward"); break;
                    case 29: view.DiagnosticRewardHover = -1; flow.CurrentRun.TrySkipReward(); flow.CurrentRun.TrySelectMapNode(3); InstallBattle(1); Play("skill.focus"); Play("skill.fall"); flow.CurrentRun.TryBeginPlot(); flow.CurrentRun.ResolvePlot(); Refresh(); break;
                    case 30: Expect(RunPhase.RelicReward); Capture("16-relic-reward"); break;
                    case 31: flow.CurrentRun.TrySkipReward(); flow.CurrentRun.TrySelectMapNode(4); InstallBattle(1); Play("skill.focus"); Play("skill.fall"); flow.CurrentRun.TryBeginPlot(); flow.CurrentRun.ResolvePlot(); Refresh(); break;
                    case 32: Expect(RunPhase.Completed); Capture("17-completed"); break;
                    case 33: Setup(1); SetSize(1280, 720); break;
                    case 34: Capture("18-battle-720p"); break;
                    case 35: Play("skill.focus"); Play("skill.echo"); Play("skill.fall"); Refresh(); Invoke("StartCast"); view.DiagnosticCastTime = 0.79f; break;
                    case 36: Capture("20-etching-720p"); break;
                    case 37: view.DiagnosticCastTime = 1.86f; break;
                    case 38: typeof(GraphaclysmModernView).GetField("helpOpen", Private).SetValue(view, true); break;
                    case 39: Capture("19-help-720p"); break;
                    case 40: typeof(GraphaclysmModernView).GetField("helpOpen", Private).SetValue(view, false); Setup(0); SetSize(1920, 1080); break;
                    case 41:
                        typeof(BattleSession).GetProperty("PlayerHealth").SetValue(flow.CurrentRun.CurrentBattle.Battle, 1);
                        Play("skill.focus"); flow.CurrentRun.TryBeginPlot(); flow.CurrentRun.ResolvePlot(); flow.CurrentRun.ResolveEnemyTurn(); Refresh(); break;
                    case 42: Expect(RunPhase.Defeated); Capture("21-defeated"); break;
                    case 43:
                        File.WriteAllText(Path.Combine(output, "smoke-result.txt"), "Full HD + 1280x720\nTitle, both characters, map, actual starting hand, card detail, dual hit, inner/outer etching, impact, next turn, both ultimates, card reward with detail, relic reward, completed, defeated, help, scaled GL etching.\nReward / completed / defeated phases asserted. Fixture uses seeded hands / HP / resonance; not a balance playthrough.\nRuntime errors: " + errors);
                        EditorApplication.Exit(errors == 0 ? 0 : 1); break;
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(output ?? "Logs", "smoke-failure.txt"), ex.ToString());
                Debug.LogException(ex); EditorApplication.Exit(1);
            }
        }
        private void Setup(int character, int health = 100)
        {
            view.DiagnosticCastTime = -1; view.DiagnosticHoveredCard = -1;
            typeof(GraphaclysmModernView).GetField("castActive", Private).SetValue(view, false);
            flow.ReturnToCharacterSelection(); flow.TrySelectCharacter(character); flow.TryStartRun(); flow.CurrentRun.TrySelectMapNode(0);
            InstallBattle(health);
        }
        private void InstallBattle(int health)
        {
            var cards = new[] { SkillCardCatalog.Find("skill.focus"), SkillCardCatalog.Find("skill.echo"),
                SkillCardCatalog.Find("skill.fall"), SkillCardCatalog.Find("skill.garden"), SkillCardCatalog.Find("skill.eclipse") };
            var battle = new BattleSession(new BattleDefinition(flow.CurrentCharacter.MaxHealth, 6, new[] {
                new EnemyDefinition("left", "균열 잔영", 2, health == 1 ? -4 : -1, health, 3),
                new EnemyDefinition("right", "유리 파편", 7, 1, health, 4),
                new EnemyDefinition("core", "침묵의 핵", 5, -1, health, 4, health == 1 ? EnemyBehaviorDefinition.SteadyAttack() : EnemyBehaviorDefinition.ChargeBurst())
            }, flow.CurrentCharacter.Archetype), 32, 0, 0, 6);
            typeof(RunGameSession).GetProperty("CurrentBattle").SetValue(flow.CurrentRun,
                new BattleGameSession(battle, new DeckSession(cards, cards.Length, new XorShiftRandom(123))));
            Refresh();
        }
        private void Play(string id)
        {
            var run = flow.CurrentRun;
            for (int i = 0; i < run.CurrentBattle.Deck.HandCount; i++)
                if (run.CurrentBattle.Deck.GetHandCard(i).Id == id)
                { if (!run.TryPlayHandCard(i, out _, out _)) throw new InvalidOperationException("Card failed: " + id); return; }
            throw new InvalidOperationException("Card absent: " + id);
        }
        private void Refresh() => Invoke("Refresh");
        private void Expect(RunPhase phase) { if (flow.CurrentRun.Phase != phase) throw new InvalidOperationException("Expected phase: " + phase); }
        private void Invoke(string method) => typeof(GraphaclysmModernView).GetMethod(method, Private).Invoke(view, null);
        private void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
        private static void SetSize(int width, int height)
        {
            var assembly = typeof(EditorWindow).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = sizesType.BaseType.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { sizesType.GetProperty("currentGroupType").GetValue(sizes) });
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)size.GetType().GetProperty("width").GetValue(size) == width && (int)size.GetType().GetProperty("height").GetValue(size) == height) { index = i; break; }
            }
            if (index < 0)
            {
                var kind = Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"), 1);
                var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new[] { kind, (object)width, height, "V3 720p" });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); index = count;
            }
            var window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            window.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(window, index);
        }
    }
}
