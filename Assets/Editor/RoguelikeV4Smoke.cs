using System;
using System.IO;
using System.Reflection;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using Graphaclysm.Runtime.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Graphaclysm.Editor
{
    [InitializeOnLoad]
    public static class RoguelikeV4Smoke
    {
        private const string Key = "GRAPHACLYSM.RoguelikeV4Smoke";
        static RoguelikeV4Smoke()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false); new GameObject("Roguelike V4 fixture").AddComponent<RoguelikeV4Runner>();
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
    public sealed class RoguelikeV4Runner : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string X = "2*cos(3*t)+cos(7*t)", Y = "2*sin(3*t)-sin(7*t)";
        private GraphaclysmModernView view;
        private PrototypeGameFlow flow;
        private string output;
        private int frames, stage, errors;
        private double deadline;
        private void Start()
        {
            output=Path.GetFullPath("../RoguelikeV4Captures"); Directory.CreateDirectory(output);
            deadline=EditorApplication.timeSinceStartup+150;
            UnityEngine.Application.logMessageReceived+=OnLog;
            view=FindFirstObjectByType<GraphaclysmModernView>();
            flow=(PrototypeGameFlow)typeof(GraphaclysmModernView).GetField("flow",Private).GetValue(view);
        }
        private void OnLog(string c,string s,LogType t) { if(t==LogType.Exception||t==LogType.Error||t==LogType.Assert) errors++; }
        private void Update()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException();
                if(++frames<20)return; frames=0;
                switch(stage++)
                {
                    case 0: Capture("01-title-luna");break;
                    case 1: flow.OpenCharacterSelection();flow.TrySelectCharacter(1);Refresh();break;
                    case 2: Capture("02-luna-redesign");break;
                    case 3: StartRun(101,0);break;
                    case 4: Capture("03-dungeon-seed-101");break;
                    case 5: StartRun(202,1);break;
                    case 6: Capture("04-dungeon-seed-202");break;
                    case 7: flow.CurrentRun.TrySelectMapNode(0);Refresh();break;
                    case 8: Capture("05-calculator-opening");break;
                    case 9: Formula(X,Y);break;
                    case 10: Capture("06-built-spirograph");break;
                    case 11:
                        bool moved=flow.CurrentRun.TryMovePlayer(-1.5,0)||flow.CurrentRun.TryMovePlayer(0,1.5)||flow.CurrentRun.TryMovePlayer(1.5,0);
                        if(!moved)throw new InvalidOperationException("No fixture move.");Refresh();break;
                    case 12: Capture("07-moved");break;
                    case 13: if(!flow.CurrentRun.TryUndoMove())throw new InvalidOperationException("Move undo failed.");Refresh();break;
                    case 14: Capture("08-move-undone");break;
                    case 15: Formula(X,"sin(");break;
                    case 16: Capture("09-incomplete-draft");break;
                    case 17: Formula(X,Y);InstallBattle(100);Formula(X,Y);break;
                    case 18: Play("calc.hush");Refresh();break;
                    case 19: Capture("10-card-on-y");break;
                    case 20: flow.CurrentRun.TryUndoLastPlayedCard(out _);Refresh();Invoke("StartCast");view.DiagnosticCastTime=0.43f;break;
                    case 21: Capture("11-spirograph-inner");break;
                    case 22: view.DiagnosticCastTime=0.84f;break;
                    case 23: Capture("12-spirograph-outer");break;
                    case 24: view.DiagnosticCastTime=1.25f;break;
                    case 25: Capture("13-impact");break;
                    case 26: view.DiagnosticCastTime=1.86f;break;
                    case 27:
                        if(flow.CurrentRun.CurrentBattle.Battle.Turn!=2 || flow.CurrentRun.CurrentBattle.Battle.Equation.Calculator.XText!=X) throw new InvalidOperationException("Turn or formula persistence failed.");
                        Capture("14-next-turn");break;
                    case 28: InstallRooms();flow.CurrentRun.TrySelectMapNode(0);Refresh();break;
                    case 29: Expect(RunPhase.Room);Capture("15-event-room");break;
                    case 30: flow.CurrentRun.TryChooseRoomOption(1);Refresh();break;
                    case 31: Expect(RunPhase.DeckRefinement);Capture("16-remove-card");break;
                    case 32: if(!flow.CurrentRun.TryRemoveDeckCard(0))throw new InvalidOperationException("Removal failed.");flow.CurrentRun.TrySelectMapNode(1);Refresh();break;
                    case 33: Expect(RunPhase.Room);Capture("17-rest-room");break;
                    case 34: flow.CurrentRun.TryChooseRoomOption(0);flow.CurrentRun.TrySelectMapNode(2);Refresh();break;
                    case 35: Capture("18-treasure-room");break;
                    case 36: flow.CurrentRun.TryChooseRoomOption(0);Refresh();break;
                    case 37: Expect(RunPhase.RelicReward);Capture("19-treasure-reward");break;
                    case 38: flow.CurrentRun.TrySelectRelicReward(0);flow.CurrentRun.TrySelectMapNode(3);InstallBattle(1);Formula(X,Y);flow.CurrentRun.TryBeginPlot();flow.CurrentRun.ResolvePlot();Refresh();break;
                    case 39: Expect(RunPhase.CardReward);Capture("20-battle-reward");break;
                    case 40: flow.CurrentRun.TrySkipReward();flow.CurrentRun.TrySelectMapNode(4);InstallBattle(1);Formula(X,Y);flow.CurrentRun.TryBeginPlot();flow.CurrentRun.ResolvePlot();Refresh();break;
                    case 41: Expect(RunPhase.Completed);Capture("21-completed");break;
                    case 42: StartRun(303,1);flow.CurrentRun.TrySelectMapNode(0);Refresh();Formula(X,Y);SetSize(1280,720);break;
                    case 43: Capture("22-calculator-720p");break;
                    case 44: Invoke("StartCast");view.DiagnosticCastTime=0.84f;break;
                    case 45: Capture("23-spirograph-720p");break;
                    case 46:
                        File.WriteAllText(Path.Combine(output,"smoke-result.txt"),"Full HD + 720p; 23 captures\nTwo actual procedural seeds, no starter hand, parsed spirograph, independent card axis, move undo, invalid draft, radial cast, retained expression, event, removal, rest, treasure, relic, battle reward, completion.\nRooms/completion/turn states asserted. Fixed battle and room fixtures test integration, not run balance.\nRuntime errors: "+errors);
                        EditorApplication.Exit(errors==0?0:1);break;
                }
            }
            catch(Exception ex) { File.WriteAllText(Path.Combine(output??"Logs","smoke-failure.txt"),ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1); }
        }
        private void StartRun(uint seed,int character)
        {
            view.DiagnosticCastTime=-1;typeof(GraphaclysmModernView).GetField("castActive",Private).SetValue(view,false);
            flow=new PrototypeGameFlow(Graphaclysm.Core.Characters.PrototypeCharacterCatalog.All,seed);
            flow.OpenCharacterSelection();flow.TrySelectCharacter(character);flow.TryStartRun();
            typeof(GraphaclysmModernView).GetField("flow",Private).SetValue(view,flow);Refresh();
        }
        private void InstallBattle(int health)
        {
            var c=flow.CurrentCharacter;
            var b=new BattleSession(new BattleDefinition(c.MaxHealth,c.MaxEnergy,new[]{
                new EnemyDefinition("a","첫 번째 기어",8,0,health,0),new EnemyDefinition("b","두 번째 기어",2,0,health,0)
            },c.Archetype,true),c.MaxHealth,0,0,6);
            var cards=new[]{CalculatorCardCatalog.Find("calc.hush"),CalculatorCardCatalog.Find("calc.echo"),CalculatorCardCatalog.Find("calc.fall"),CalculatorCardCatalog.Find("calc.garden"),CalculatorCardCatalog.Find("calc.rupture")};
            typeof(RunGameSession).GetProperty("CurrentBattle").SetValue(flow.CurrentRun,new BattleGameSession(b,new DeckSession(cards,5,new XorShiftRandom(21))));Refresh();
        }
        private void InstallRooms()
        {
            view.DiagnosticCastTime=-1;typeof(GraphaclysmModernView).GetField("castActive",Private).SetValue(view,false);
            var c=flow.CurrentCharacter;var def=new BattleDefinition(c.MaxHealth,c.MaxEnergy,new[]{new EnemyDefinition("a","Gear",8,0,1,0)},c.Archetype,true);
            var nodes=new[]{
                new RunMapNodeDefinition("event","Event",0,0,RunNodeKind.Event,null,new[]{1},RoomStoryCatalog.Event(4),c.MaxHealth),
                new RunMapNodeDefinition("rest","Rest",1,0,RunNodeKind.Rest,null,new[]{2},RoomStoryCatalog.Rest,c.MaxHealth),
                new RunMapNodeDefinition("treasure","Treasure",2,0,RunNodeKind.Treasure,null,new[]{3},RoomStoryCatalog.Treasure,c.MaxHealth),
                new RunMapNodeDefinition("battle","Battle",3,0,RunNodeKind.Battle,def,new[]{4}),
                new RunMapNodeDefinition("boss","Boss",4,0,RunNodeKind.Boss,def,new int[0])};
            typeof(PrototypeGameFlow).GetProperty("CurrentRun").SetValue(flow,new RunGameSession(new RunMapDefinition(nodes),c.CreateStartingDeckCopy(),CalculatorCardCatalog.All,5,501,PrototypeRelicCatalog.All));Refresh();
        }
        private void Formula(string x,string y)
        { typeof(GraphaclysmModernView).GetField("calculatorX",Private).SetValue(view,x);typeof(GraphaclysmModernView).GetField("calculatorY",Private).SetValue(view,y);Invoke("ApplyCalculator"); }
        private void Play(string id)
        { var run=flow.CurrentRun;for(int i=0;i<run.CurrentBattle.Deck.HandCount;i++)if(run.CurrentBattle.Deck.GetHandCard(i).Id==id){if(!run.TryPlayHandCard(i,out _,out _))throw new InvalidOperationException("Card failed.");return;}throw new InvalidOperationException("Card missing."); }
        private void Refresh()=>Invoke("Refresh");
        private void Invoke(string name)=>typeof(GraphaclysmModernView).GetMethod(name,Private).Invoke(view,null);
        private void Expect(RunPhase phase){if(flow.CurrentRun.Phase!=phase)throw new InvalidOperationException("Expected "+phase);}
        private void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        private static void SetSize(int w,int h)=>typeof(VisualV3SmokeRunner).GetMethod("SetSize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h});
    }
}
