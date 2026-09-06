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
    public static class QuietV6Smoke
    {
        private const string Key = "GRAPHACLYSM.QuietV6Smoke";
        static QuietV6Smoke()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false); new GameObject("Quiet V6 fixture").AddComponent<QuietV6Runner>();
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
    public sealed class QuietV6Runner : MonoBehaviour
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
            output=Path.GetFullPath("../QuietV6Captures"); Directory.CreateDirectory(output);
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
                    case 0: Capture("01-title-no-characters");break;
                    case 1: flow.OpenCharacterSelection();flow.TrySelectCharacter(0);Refresh();break;
                    case 2: Capture("02-ian-facing-left");break;
                    case 3: flow.TrySelectCharacter(1);Refresh();break;
                    case 4: Capture("03-luna-facing-right");break;
                    case 5: StartRun(202,1);break;
                    case 6: Capture("04-map-new-luna");break;
                    case 7: flow.CurrentRun.TrySelectMapNode(0);Refresh();break;
                    case 8: Capture("05-quiet-opening");break;
                    case 9: InstallBattle(200);Play("frag.echo");Play("frag.orbit");Play("frag.contract");Refresh();break;
                    case 10: Capture("06-quiet-assembled");break;
                    case 11: view.DiagnosticHoveredCard=0;break;
                    case 12: Capture("07-card-inspection");break;
                    case 13: view.DiagnosticHoveredCard=-1;Toggle("showBattleDetails",true);Toggle("showEquation",true);break;
                    case 14: Capture("08-details-expanded");break;
                    case 15: Toggle("showBattleDetails",false);Toggle("showEquation",false);Invoke("Condense");break;
                    case 16:
                        if(flow.CurrentRun.CurrentBattle.Battle.SealedCardCount!=3)throw new InvalidOperationException("Condense regression.");Capture("09-quiet-condensed");break;
                    case 17: Play("frag.echo");Play("frag.petal");Refresh();if(!flow.CurrentRun.TryMovePlayer(-1.5,0))throw new InvalidOperationException("Move failed.");Refresh();break;
                    case 18: Capture("10-move-undo-visible");break;
                    case 19: if(!flow.CurrentRun.TryUndoMove())throw new InvalidOperationException("Undo failed.");Refresh();Invoke("StartCast");view.DiagnosticCastTime=.84f;break;
                    case 20: Capture("11-quiet-release");break;
                    case 21: view.DiagnosticCastTime=1.86f;break;
                    case 22:
                        if(flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount!=0)throw new InvalidOperationException("Release regression.");view.DiagnosticCastTime=-1;Invoke("Condense");Invoke("Condense");break;
                    case 23: if(flow.CurrentRun.CurrentBattle.Deck.HandCount!=8)throw new InvalidOperationException("Hand regression.");Capture("12-eight-card-hand");break;
                    case 24: SetSize(1280,720);break;
                    case 25: Capture("13-quiet-720p");break;
                    case 26: view.DiagnosticHoveredCard=2;break;
                    case 27: Capture("14-inspect-720p");break;
                    case 28: view.DiagnosticHoveredCard=-1;Toggle("showSecondaryActions",true);break;
                    case 29: Capture("15-secondary-actions");break;
                    case 30: Invoke("Unravel");Toggle("showSecondaryActions",false);break;
                    case 31: if(!flow.CurrentRun.CurrentBattle.Battle.CanCondense)throw new InvalidOperationException("Unravel regression.");SetSize(1920,1080);InstallRooms();flow.CurrentRun.TrySelectMapNode(0);Refresh();break;
                    case 32: Capture("16-room-preserved");break;
                    case 33: flow.CurrentRun.TryChooseRoomOption(1);Refresh();break;
                    case 34: Expect(RunPhase.DeckRefinement);Capture("17-deck-preserved");break;
                    case 35:
                        File.WriteAllText(Path.Combine(output,"smoke-result.txt"),"Full HD + 720p; 17 captures\nTitle without characters; Ian left/Luna right; quiet default HUD; card hover and details disclosure; condense/release/move undo; 8-card hand; secondary actions; rooms/removal.\nCore unchanged. Fixture assertions, not full-run balance.\nRuntime errors: "+errors);
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
        private void Toggle(string name,bool value)=>typeof(GraphaclysmModernView).GetField(name,Private).SetValue(view,value);
        private void Refresh()=>Invoke("Refresh");
        private void Invoke(string name)=>typeof(GraphaclysmModernView).GetMethod(name,Private).Invoke(view,null);
        private void Expect(RunPhase phase){if(flow.CurrentRun.Phase!=phase)throw new InvalidOperationException("Expected "+phase);}
        private void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        private static void SetSize(int w,int h)=>typeof(VisualV3SmokeRunner).GetMethod("SetSize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,h});
    }
}
