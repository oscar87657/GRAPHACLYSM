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
    public static class ExpeditionV7Smoke
    {
        private const string Key = "GRAPHACLYSM.ExpeditionV7Smoke";
        static ExpeditionV7Smoke()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false); new GameObject("Expedition V7 fixture").AddComponent<ExpeditionV7Runner>();
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
    public sealed class ExpeditionV7Runner : MonoBehaviour
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
            output=Path.GetFullPath("../ExpeditionV7Captures"); Directory.CreateDirectory(output);
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
                    case 0: Capture("01-main-card-dictionary");break;
                    case 1: Toggle("codexOpen",true);Codex(0,0);break;
                    case 2: Capture("02-card-dictionary");break;
                    case 3: Codex(3,0);Select(10);break;
                    case 4: Capture("03-origin-card-details");break;
                    case 5: Codex(0,2);break;
                    case 6: Capture("04-dictionary-last-page");break;
                    case 7: Toggle("codexRelics",true);Codex(0,1);break;
                    case 8: Capture("05-new-relics");break;
                    case 9: Toggle("codexOpen",false);StartRun(202,1);break;
                    case 10: Capture("06-floor-one");break;
                    case 11: AdvanceFloor();break;
                    case 12: if(flow.CurrentRun.CurrentFloor!=2)throw new InvalidOperationException("Floor two missing");Capture("07-floor-two");break;
                    case 13: AdvanceFloor();break;
                    case 14: if(flow.CurrentRun.CurrentFloor!=3)throw new InvalidOperationException("Floor three missing");Capture("08-floor-three");break;
                    case 15: StartRun(202,1);flow.CurrentRun.TrySelectMapNode(0);Refresh();break;
                    case 16: Capture("09-starting-hand");break;
                    case 17: InstallBattle(200);Play("frag.echo");Play("frag.home");Play("frag.orbit");Refresh();break;
                    case 18: Capture("10-shifted-weave");break;
                    case 19: view.DiagnosticHoveredCard=0;break;
                    case 20: Capture("11-card-hover");break;
                    case 21: view.DiagnosticHoveredCard=-1;Toggle("showBattleDetails",true);Toggle("showEquation",true);break;
                    case 22: Capture("12-equation-and-origin");break;
                    case 23: Toggle("showBattleDetails",false);Toggle("showEquation",false);Invoke("Condense");break;
                    case 24:
                        if(flow.CurrentRun.CurrentBattle.Battle.PlayerHealth!=34 || flow.CurrentRun.CurrentBattle.Battle.SealedCardCount!=3)throw new InvalidOperationException("Condense cost/seal regression");
                        Capture("13-condense-cost");break;
                    case 25: if(!flow.CurrentRun.TryMovePlayer(-1.5,0))throw new InvalidOperationException("Movement failed");Refresh();break;
                    case 26: Capture("14-movement-dock-and-undo");break;
                    case 27: if(!flow.CurrentRun.TryUndoMove())throw new InvalidOperationException("Undo failed");Refresh();Invoke("StartCast");view.DiagnosticCastTime=.28f;break;
                    case 28: Capture("15-relocated-cast-origin");break;
                    case 29: view.DiagnosticCastTime=.86f;break;
                    case 30: Capture("16-relocated-cast-lines");break;
                    case 31: view.DiagnosticCastTime=1.86f;break;
                    case 32: view.DiagnosticCastTime=-1;flow.CurrentRun.CurrentBattle.Deck.DrawRetained(8);Refresh();break;
                    case 33: Capture("17-eight-cards");break;
                    case 34: SetSize(1280,720);break;
                    case 35: Capture("18-battle-720p");break;
                    case 36: view.DiagnosticHoveredCard=2;break;
                    case 37: Capture("19-hover-720p");break;
                    case 38: view.DiagnosticHoveredCard=-1;Toggle("showSecondaryActions",true);break;
                    case 39: Capture("20-secondary-action");break;
                    case 40: SetSize(1920,1080);InstallRooms();flow.CurrentRun.TrySelectMapNode(0);Refresh();break;
                    case 41: Capture("21-event-room");break;
                    case 42: flow.CurrentRun.TryChooseRoomOption(1);Refresh();break;
                    case 43: Expect(RunPhase.DeckRefinement);Capture("22-deck-removal");break;
                    case 44: flow.ReturnToMainMenu();Refresh();Toggle("codexOpen",true);Toggle("codexRelics",false);Codex(0,0);SetSize(1280,720);break;
                    case 45: Capture("23-dictionary-720p");break;
                    case 46:
                        File.WriteAllText(Path.Combine(output,"smoke-result.txt"),"Full HD + 720p; 23 captures\nMain dictionary, filters and pages; new relics; 3 floor maps; unique card seals; shifted graph geometry and reveal; condense health cost; movement dock/undo; hover/details; 8-card hand; rooms/removal.\nEditor fixtures (including map fast-forward and 8-card refill), not full-run balance.\nRuntime errors: "+errors);
                        EditorApplication.Exit(errors==0?0:1);break;
                }
            }
            catch(Exception ex) { File.WriteAllText(Path.Combine(output??"Logs","smoke-failure.txt"),ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1); }
        }
        private void Codex(int filter,int page)=>typeof(GraphaclysmModernView).GetMethod("ChangeCodex",Private).Invoke(view,new object[]{filter,page});
        private void Select(int index)=>typeof(GraphaclysmModernView).GetMethod("SelectCodex",Private).Invoke(view,new object[]{index});
        private void AdvanceFloor()
        {
            var map=flow.CurrentRun.Map;
            for(int room=0;room<8;room++)
                for(int i=0;i<map.Definition.NodeCount;i++)if(map.IsAvailable(i)){map.TryEnterNode(i);map.TryCompleteActiveNode();break;}
            Refresh();
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
            var cards=new[]{FragmentCardCatalog.Find("frag.echo"),FragmentCardCatalog.Find("frag.orbit"),FragmentCardCatalog.Find("frag.home"),FragmentCardCatalog.Find("frag.surge"),FragmentCardCatalog.Find("frag.expand"),FragmentCardCatalog.Find("frag.echo"),FragmentCardCatalog.Find("frag.right"),FragmentCardCatalog.Find("frag.down"),FragmentCardCatalog.Find("frag.square"),FragmentCardCatalog.Find("frag.mirror"),FragmentCardCatalog.Find("frag.overtone"),FragmentCardCatalog.Find("frag.petal")};
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
