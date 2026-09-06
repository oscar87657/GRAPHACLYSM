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
    public static class FragmentsV5Smoke
    {
        private const string Key = "GRAPHACLYSM.FragmentsV5Smoke";
        static FragmentsV5Smoke()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false); new GameObject("Fragments V5 fixture").AddComponent<FragmentsV5Runner>();
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
    public sealed class FragmentsV5Runner : MonoBehaviour
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
            output=Path.GetFullPath("../FragmentsV5Captures"); Directory.CreateDirectory(output);
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
                    case 0: StartRun(202,1);break;
                    case 1: Capture("01-generated-dungeon");break;
                    case 2: flow.CurrentRun.TrySelectMapNode(0);Refresh();break;
                    case 3: Capture("02-no-calculator-no-energy");break;
                    case 4: InstallBattle(200);Play("frag.echo");Refresh();break;
                    case 5: Capture("03-first-fragment");break;
                    case 6: Play("frag.orbit");Refresh();break;
                    case 7: Capture("04-wrapped-curve");break;
                    case 8: Play("frag.contract");Refresh();break;
                    case 9: Capture("05-draw-fragment");break;
                    case 10: Invoke("Condense");break;
                    case 11:
                        if(flow.CurrentRun.CurrentBattle.Battle.Turn!=2 || flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount!=3 || flow.CurrentRun.CurrentBattle.Battle.SealedCardCount!=3)throw new InvalidOperationException("Condense lost queue.");
                        Capture("06-retained-and-drawn");break;
                    case 12: Play("frag.echo");Play("frag.petal");Refresh();break;
                    case 13: Capture("07-five-fragment-burst");break;
                    case 14: typeof(GraphaclysmModernView).GetField("showEquation",Private).SetValue(view,true);break;
                    case 15: Capture("08-equation-rules");break;
                    case 16:
                        typeof(GraphaclysmModernView).GetField("showEquation",Private).SetValue(view,false);
                        if(!flow.CurrentRun.TryMovePlayer(-1.5,0))throw new InvalidOperationException("Move failed.");Refresh();break;
                    case 17: Capture("09-free-move");break;
                    case 18: if(!flow.CurrentRun.TryUndoMove())throw new InvalidOperationException("Move undo failed.");Refresh();break;
                    case 19: Capture("10-move-restored");break;
                    case 20: Invoke("StartCast");view.DiagnosticCastTime=.43f;break;
                    case 21: Capture("11-inner-release");break;
                    case 22: view.DiagnosticCastTime=.84f;break;
                    case 23: Capture("12-outer-release");break;
                    case 24: view.DiagnosticCastTime=1.25f;break;
                    case 25: Capture("13-impact");break;
                    case 26: view.DiagnosticCastTime=1.86f;break;
                    case 27:
                        if(flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount!=0 || flow.CurrentRun.CurrentBattle.Deck.ReservedCount!=0)throw new InvalidOperationException("Release did not empty queue.");
                        Capture("14-hand-after-release");break;
                    case 28: view.DiagnosticCastTime=-1;Invoke("Condense");Invoke("Condense");break;
                    case 29: if(flow.CurrentRun.CurrentBattle.Deck.HandCount!=8 || flow.CurrentRun.CurrentBattle.Battle.CanCondense)throw new InvalidOperationException("Hand/condense limit failed.");Capture("15-full-retained-hand");break;
                    case 30: SetSize(1280,720);break;
                    case 31: Capture("16-full-hand-720p");break;
                    case 32: Invoke("Unravel");break;
                    case 33: if(!flow.CurrentRun.CurrentBattle.Battle.CanCondense)throw new InvalidOperationException("Unravel failed.");Capture("17-unravel");break;
                    case 34: SetSize(1920,1080);InstallRooms();flow.CurrentRun.TrySelectMapNode(0);Refresh();break;
                    case 35: Capture("18-event");break;
                    case 36: flow.CurrentRun.TryChooseRoomOption(1);Refresh();break;
                    case 37: Expect(RunPhase.DeckRefinement);Capture("19-fragment-removal");break;
                    case 38: flow.CurrentRun.TryRemoveDeckCard(0);flow.CurrentRun.TrySelectMapNode(1);flow.CurrentRun.TryChooseRoomOption(0);flow.CurrentRun.TrySelectMapNode(2);flow.CurrentRun.TryChooseRoomOption(0);Refresh();break;
                    case 39: Expect(RunPhase.RelicReward);Capture("20-draw-relics");break;
                    case 40: flow.CurrentRun.TrySelectRelicReward(0);flow.CurrentRun.TrySelectMapNode(3);InstallBattle(1);Play("frag.echo");flow.CurrentRun.TryBeginPlot();flow.CurrentRun.ResolvePlot();Refresh();break;
                    case 41: Expect(RunPhase.CardReward);Capture("21-fragment-reward");break;
                    case 42: flow.CurrentRun.TrySkipReward();flow.CurrentRun.TrySelectMapNode(4);InstallBattle(1);Play("frag.echo");flow.CurrentRun.TryBeginPlot();flow.CurrentRun.ResolvePlot();Refresh();break;
                    case 43: Expect(RunPhase.Completed);Capture("22-completed");break;
                    case 44: StartRun(303,0);flow.CurrentRun.TrySelectMapNode(0);InstallBattle(200);Play("frag.echo");Play("frag.orbit");Play("frag.petal");Refresh();SetSize(1280,720);break;
                    case 45: Capture("23-wrapped-720p");break;
                    case 46: Invoke("StartCast");view.DiagnosticCastTime=.84f;break;
                    case 47: Capture("24-release-720p");break;
                    case 48:
                        File.WriteAllText(Path.Combine(output,"smoke-result.txt"),"Full HD + 720p; 24 captures\nNo calculator/energy, whole-curve wrapping, draw fragment, retained hand/queue, sealed undo, burst, movement undo, discharge reset, 8-card hand, 2-condense cap, unravel, rooms, removal, draw relics, rewards, completion.\nFixture integration assertions; not a full-run balance playthrough.\nRuntime errors: "+errors);
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
            view.DiagnosticCastTime=-1;typeof(GraphaclysmModernView).GetField("castActive",Private).SetValue(view,false);
            var c=flow.CurrentCharacter;
            var b=new BattleSession(new BattleDefinition(c.MaxHealth,c.MaxEnergy,new[]{
                new EnemyDefinition("a","첫 번째 기어",7.4,0,health,0),new EnemyDefinition("b","두 번째 기어",2.6,0,health,0)
            },c.Archetype,fragments:true),c.MaxHealth,0,0,6);
            var cards=new[]{FragmentCardCatalog.Find("frag.echo"),FragmentCardCatalog.Find("frag.orbit"),FragmentCardCatalog.Find("frag.contract"),FragmentCardCatalog.Find("frag.petal"),FragmentCardCatalog.Find("frag.expand"),FragmentCardCatalog.Find("frag.echo"),FragmentCardCatalog.Find("frag.right"),FragmentCardCatalog.Find("frag.down"),FragmentCardCatalog.Find("frag.square"),FragmentCardCatalog.Find("frag.mirror"),FragmentCardCatalog.Find("frag.overtone"),FragmentCardCatalog.Find("frag.petal")};
            Array.Reverse(cards); var deck=new DeckSession(cards,8,new KeepOrder(),true,8);
            typeof(RunGameSession).GetProperty("CurrentBattle").SetValue(flow.CurrentRun,new BattleGameSession(b,deck));Refresh();
        }
        private sealed class KeepOrder : IRandomSource { public int Next(int exclusiveMaximum) => exclusiveMaximum-1; }
        private void InstallRooms()
        {
            view.DiagnosticCastTime=-1;typeof(GraphaclysmModernView).GetField("castActive",Private).SetValue(view,false);
            var c=flow.CurrentCharacter;var def=new BattleDefinition(c.MaxHealth,c.MaxEnergy,new[]{new EnemyDefinition("a","Gear",7.4,0,1,0)},c.Archetype,fragments:true);
            var nodes=new[]{
                new RunMapNodeDefinition("event","Event",0,0,RunNodeKind.Event,null,new[]{1},RoomStoryCatalog.Event(4),c.MaxHealth),
                new RunMapNodeDefinition("rest","Rest",1,0,RunNodeKind.Rest,null,new[]{2},RoomStoryCatalog.Rest,c.MaxHealth),
                new RunMapNodeDefinition("treasure","Treasure",2,0,RunNodeKind.Treasure,null,new[]{3},RoomStoryCatalog.Treasure,c.MaxHealth),
                new RunMapNodeDefinition("battle","Battle",3,0,RunNodeKind.Battle,def,new[]{4}),
                new RunMapNodeDefinition("boss","Boss",4,0,RunNodeKind.Boss,def,new int[0])};
            typeof(PrototypeGameFlow).GetProperty("CurrentRun").SetValue(flow,new RunGameSession(new RunMapDefinition(nodes),c.CreateStartingDeckCopy(),FragmentCardCatalog.All,5,501,FragmentRelicCatalog.All));Refresh();
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
