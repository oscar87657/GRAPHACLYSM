using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
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
        public static void RunGrowthBatch()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Batch diagnostics only.");
            SessionState.SetBool("GRAPHACLYSM.Growth36",true);RunBatch();
        }
        public static void RunSuppliesBatch()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Batch diagnostics only.");
            SessionState.SetBool("GRAPHACLYSM.Supplies37",true);RunBatch();
        }
        public static void RunRewardRowBatch()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Batch diagnostics only.");
            SessionState.SetBool("GRAPHACLYSM.RewardRow38",true);RunBatch();
        }
        public static void RunPreparationBatch()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Batch diagnostics only.");
            SessionState.SetBool("GRAPHACLYSM.Preparation39",true);RunBatch();
        }
        public static void RunTowerMapBatch()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Batch diagnostics only.");
            SessionState.SetBool("GRAPHACLYSM.TowerMap42",true);RunBatch();
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

    [DefaultExecutionOrder(10000)]
    public sealed class BasicsV9Runner : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private GraphaclysmModernView view;
        private PrototypeGameFlow flow;
        private string output, dataDirectory;
        private int errors, captures;
        private int previewCaptureTarget = -1;
        private IEnumerator scenario;
        private double deadline;
        private bool growthOnly;
        private bool suppliesOnly;
        private bool rewardRowOnly;
        private bool preparationOnly;
        private bool towerMapOnly;
        private void Start()
        {
            growthOnly=SessionState.GetBool("GRAPHACLYSM.Growth36",false);SessionState.SetBool("GRAPHACLYSM.Growth36",false);
            suppliesOnly=SessionState.GetBool("GRAPHACLYSM.Supplies37",false);SessionState.SetBool("GRAPHACLYSM.Supplies37",false);
            rewardRowOnly=SessionState.GetBool("GRAPHACLYSM.RewardRow38",false);SessionState.SetBool("GRAPHACLYSM.RewardRow38",false);
            preparationOnly=SessionState.GetBool("GRAPHACLYSM.Preparation39",false);SessionState.SetBool("GRAPHACLYSM.Preparation39",false);
            towerMapOnly=SessionState.GetBool("GRAPHACLYSM.TowerMap42",false);SessionState.SetBool("GRAPHACLYSM.TowerMap42",false);
            output = Path.GetFullPath(towerMapOnly?"Logs/TowerMap42Captures":preparationOnly?"Logs/Preparation39Captures":rewardRowOnly?"Logs/RewardRow38Captures":suppliesOnly?"Logs/Supplies37Captures":growthOnly?"Logs/Growth36Captures":"Logs/BasicsV9Captures"); Directory.CreateDirectory(output);
            dataDirectory = Path.Combine(output, "UserData-" + Guid.NewGuid().ToString("N"));
            UnityEngine.Application.logMessageReceived += OnLog;
            view = FindFirstObjectByType<GraphaclysmModernView>(); ConfigureView();
            deadline = EditorApplication.timeSinceStartup + 240;
            scenario = towerMapOnly?TowerMapScenario():preparationOnly?PreparationScenario():rewardRowOnly?RewardRowScenario():suppliesOnly?SuppliesScenario():growthOnly?GrowthScenario():Scenario(); StartCoroutine(Guarded());
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
            File.WriteAllText(Path.Combine(output, "smoke-result.txt"), "Captures: " + captures + "\nRuntime errors: " + errors + (towerMapOnly?
                "\nTowerMap42: all floor joins/top/base at 1080p/720p; monotonic edges, wheel/drag/clamp/modal guard, no navigation mutation, actual first victory/refocus/save-replay, legacy map. Not full-run balance or OS mouse automation.":preparationOnly?
                "\nPreparation39/Legacy41: actual opening victory/instant drafts/replay; four-colour loot, shop purchase/return fixture, expanded 24-node tree with wheel/drag-release checks and squad selection at 1080p/720p. Not full-run balance or OS mouse automation.":rewardRowOnly?
                "\nRewardRow38: no free starter, actual opening win/partial restore, single-row scroll/drag and release suppression, six-card visual fixture, collapse/empty/confirmation at 1080p/720p. Not full-run balance or OS input automation.":suppliesOnly?
                "\nSupplies37 focused fixture: actual opening victory and saved partial loot, starting relic, three-point tree, chest success/failure fixture, 1080p/720p. Separate user data; not full-run balance.":growthOnly?
                "\nGrowth36 focused UI fixture: four trees, overview/720p, nine-point cap, refund/reallocation. Synthetic awards; no player save files used.":
                "\nReal generated battle and local save; permanent tree, one pannable 144-node run constellation per character (240 unique nodes), illustrated relic, cold view recreation, card/move undo, condense, pause during plot, help freeze, settings, inventory, overwrite prompt, 1080p/720p.\nNo player save files used. Not a full-run balance or Profiler test."));
            EditorApplication.Exit(errors == 0 ? 0 : 1);
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++; }
        private IEnumerator TowerMapScenario()
        {
            flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);Set("flow",flow);
            flow.OpenCharacterSelection();flow.TrySelectCharacter(1);Check(flow.TryStartRun(CombatApproach.Tuning),"Tower start");
            var r=flow.CurrentRun;Check(!r.OpeningStepHint.Contains("성장점 +2"),"Obsolete growth hint");Invoke("Refresh");yield return Shot("01-entrance");
            SetSize(1280,720);yield return Shot("02-entrance-720p");SetSize(1920,1080);
            var viewport=new Rect(610,130,800,840);float maximum=Get<float>("towerMapHeight")-viewport.height;
            int cross=0;
            for(int i=0;i<r.Map.Definition.NodeCount;i++)
            {
                var node=r.Map.Definition.GetNode(i);Vector2 p=(Vector2)InvokeResult("MapPoint",node);
                for(int e=0;e<node.NextNodeCount;e++)
                {
                    var next=r.Map.Definition.GetNode(node.GetNextNodeIndex(e));Vector2 q=(Vector2)InvokeResult("MapPoint",next);
                    Check(q.y<p.y,"Tower edge did not ascend");
                    if(next.Layer/r.Map.Definition.RoomsPerFloor!=node.Layer/r.Map.Definition.RoomsPerFloor)cross++;
                }
            }
            Check(cross>=4,"Missing floor joins");
            for(int floor=1;floor<5;floor++)
            {
                float boundary=(r.Map.Definition.LayerCount-floor*r.Map.Definition.RoomsPerFloor)*170+200;
                Set("towerMapScroll",Mathf.Clamp(boundary-450,0,maximum));yield return Shot("join-"+floor+"-"+(floor+1));
            }
            Set("towerMapScroll",0f);yield return Shot("07-crown");SetSize(1280,720);yield return Shot("08-crown-720p");SetSize(1920,1080);
            bool inputChecked=false;view.DiagnosticGuiCheck=()=>
            {
                int revision=r.Revision,completed=r.Map.CompletedNodeCount;Set("towerMapScroll",maximum);
                Vector2 before=(Vector2)InvokeResult("MapPoint",r.Map.Definition.GetNode(0));
                var e=new Event{type=EventType.ScrollWheel,mousePosition=viewport.center,delta=new Vector2(0,-3)};
                Invoke("HandleTowerMapPan",e);float moved=Get<float>("towerMapScroll");
                Check(moved<maximum && e.type==EventType.Used,"Tower wheel");
                Vector2 after=(Vector2)InvokeResult("MapPoint",r.Map.Definition.GetNode(0));
                Check(Mathf.Abs(after.y-before.y-(maximum-moved))<.01f,"Scroll/coordinate mismatch");
                Invoke("HandleTowerMapPan",new Event{type=EventType.MouseDown,button=0,mousePosition=viewport.center});
                Invoke("HandleTowerMapPan",new Event{type=EventType.MouseDrag,button=0,mousePosition=viewport.center+new Vector2(0,100),delta=new Vector2(0,100)});
                e=new Event{type=EventType.MouseUp,button=0,mousePosition=viewport.center+new Vector2(0,100)};Invoke("HandleTowerMapPan",e);
                Check(e.type==EventType.Used && GUIUtility.hotControl==0 && !Get<bool>("towerMapDragging"),"Tower release selected a room");
                e=new Event{type=EventType.ScrollWheel,mousePosition=viewport.center,delta=new Vector2(0,-10000)};Invoke("HandleTowerMapPan",e);Check(Get<float>("towerMapScroll")==0,"Crown clamp");
                GUI.enabled=false;Invoke("HandleTowerMapPan",new Event{type=EventType.ScrollWheel,mousePosition=viewport.center,delta=new Vector2(0,5)});GUI.enabled=true;
                Check(Get<float>("towerMapScroll")==0,"Modal allowed pan");
                Check(r.Revision==revision && r.Map.CompletedNodeCount==completed,"Navigation mutated run");inputChecked=true;
            };
            yield return null;yield return null;Check(inputChecked,"Tower GUI input check not run");
            Check(!r.TrySelectMapNode(r.Map.Definition.NodeCount-1),"Future boss selectable");
            Invoke("FocusTowerCurrent");yield return Shot("09-return-current");
            Check(r.TrySelectMapNode(0),"Enter opening");WinEconomyBattle(r);r.TryLeaveLoot();Invoke("Refresh");
            yield return Shot("10-after-victory-focus");Check(r.Map.CompletedNodeCount==1,"Actual victory missing");
            Check(RunGameSession.TryRestore(r.CaptureSave(),out var restored,out var character),"Tower save replay");
            flow.ReturnToMainMenu();flow.TryContinueRun(restored,character);Invoke("Refresh");yield return Shot("11-restored-current");
            Check(restored.Map.CompletedNodeCount==r.Map.CompletedNodeCount,"Restored progress changed");
            var old=PrototypeRunFactory.Create(302,PrototypeCharacterCatalog.All[1]);flow.ReturnToMainMenu();flow.TryContinueRun(old,PrototypeCharacterCatalog.All[1]);Invoke("Refresh");
            yield return Shot("12-legacy-map");Check(old.Map.Definition.FloorCount==3,"Legacy fixture changed");
        }
        private object InvokeResult(string name,params object[] args)
            => typeof(GraphaclysmModernView).GetMethod(name,Private).Invoke(view,args);
        private IEnumerator PreparationScenario()
        {
            var account=Get<LegacyProgression>("legacy");
            for(uint i=1;i<20;i++)account.AwardRun(i,true,5);
            Set("legacyOpen",true);
            for(int page=0;page<3;page++)
            {
                Set("legacyTreePan",page==0?new Vector2(450,-560):page==1?new Vector2(-390,-190):new Vector2(-520,-140));
                Set("legacySelectedNode",page==0?6:page==1?8:13);yield return Shot("legacy-"+page);
                SetSize(1280,720);yield return Shot("legacy-"+page+"-720p");SetSize(1920,1080);
            }
            Set("legacyTreePan",Vector2.zero);Set("legacySelectedNode",-1);yield return Shot("legacy-expanded-centre");
            Set("legacyTreePan",new Vector2(560,150));Set("legacySelectedNode",18);yield return Shot("legacy-expanded-left");
            SetSize(1280,720);yield return Shot("legacy-expanded-left-720p");SetSize(1920,1080);
            Set("legacyTreePan",new Vector2(-520,120));Set("legacySelectedNode",22);yield return Shot("legacy-expanded-right");
            Set("legacyTreePan",new Vector2(450,-630));Set("legacySelectedNode",21);yield return Shot("legacy-expanded-bottom");
            bool treeInputChecked=false;
            view.DiagnosticGuiCheck=()=>
            {
                Set("legacyTreePan",Vector2.zero);int funds=account.Currency,selection=Get<int>("legacySelectedNode");
                var e=new Event{type=EventType.ScrollWheel,mousePosition=new Vector2(600,500),delta=new Vector2(0,3)};
                Invoke("HandleLegacyTreeNavigation",e);Check(Get<Vector2>("legacyTreePan").y<0 && e.type==EventType.Used,"Tree wheel did not move");
                Invoke("HandleLegacyTreeNavigation",new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(600,500)});
                Invoke("HandleLegacyTreeNavigation",new Event{type=EventType.MouseDrag,button=0,mousePosition=new Vector2(700,550),delta=new Vector2(100,50)});
                e=new Event{type=EventType.MouseUp,button=0,mousePosition=new Vector2(700,550)};Invoke("HandleLegacyTreeNavigation",e);
                Check(e.type==EventType.Used && !Get<bool>("legacyTreeDragging") && GUIUtility.hotControl==0,"Tree drag release not suppressed");
                Check(account.Currency==funds && Get<int>("legacySelectedNode")==selection,"Tree navigation changed account or selection");
                treeInputChecked=true;
            };
            yield return null;yield return null;Check(treeInputChecked,"Tree input check did not run");
            Check(account.TryPurchase(6) && account.TryPurchase(11) && account.TryPurchase(7) && account.TryPurchase(13)
                && account.TryPurchase(4) && account.TryPurchase(12) && account.TryPurchase(8),"Unlock fixture");
            Check(Get<LegacyProgressionStore>("legacyStore").TrySave(account),"Save account");
            Set("legacyOpen",false);
            flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);Set("flow",flow);
            flow.OpenCharacterSelection();flow.TrySelectCharacter(1);Invoke("Refresh");
            Set("squadSelectionOpen",true);yield return Shot("squads-open");SetSize(1280,720);yield return Shot("squads-open-720p");SetSize(1920,1080);
            Set("selectedSquad",ExpeditionSquad.Salvage);flow.PreparationUpgrades=account.Preparation(ExpeditionSquad.Salvage);flow.LegacyBenefits=account.Benefits;
            yield return Shot("squad-salvage-selected");Set("squadSelectionOpen",false);
            Invoke("Refresh");yield return Shot("character-selected-squad");
            Check(flow.TryStartRun(CombatApproach.Tuning),"New run");var r=flow.CurrentRun;Invoke("Refresh");yield return Shot("purchased-starting-relic");
            Check(r.IsStartingRelic,"Paid relic missing");r.TrySelectRelicReward(0);r.TrySelectMapNode(0);WinEconomyBattle(r);Invoke("Refresh");yield return Shot("opening-two-drafts");
            Check(r.GetLoot(1).Kind==LootKind.Cards && r.GetLoot(2).Kind==LootKind.Cards && r.ResearchTickets==0,"Research still offered");
            r.TryClaimLoot(1);Invoke("Refresh");yield return Shot("immediate-draft");SetSize(1280,720);yield return Shot("immediate-draft-720p");SetSize(1920,1080);
            Check(RunGameSession.TryRestore(r.CaptureSave(),out var restored,out var character),"Draft replay");
            flow.ReturnToMainMenu();flow.TryContinueRun(restored,character);r=restored;r.TrySelectReward(0);Invoke("Refresh");yield return Shot("draft-card-disappeared");
            var t=typeof(RunGameSession);t.GetMethod("ResetLoot",Private).Invoke(r,null);
            for(int grade=0;grade<4;grade++)t.GetMethod("AddDraftLoot",Private).Invoke(r,new object[]{grade});
            Invoke("Refresh");yield return Shot("four-draft-colours");SetSize(1280,720);yield return Shot("four-draft-colours-720p");SetSize(1920,1080);
            r.TryLeaveLoot();
            for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.IsAvailable(i) && r.Map.Definition.GetNode(i).Battle==null)
            {r.TrySelectMapNode(i);Invoke("Refresh");yield return Shot("room-no-research-counter");r.TryLeaveRoom();break;}
            int shop=-1;for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.Definition.GetNode(i).Kind==RunNodeKind.Shop){shop=i;break;}
            while(!r.Map.IsAvailable(shop))
            {
                int next=-1;for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.IsAvailable(i)){next=i;break;}
                Check(next>=0,"Shop route");r.Map.TryEnterNode(next);r.Map.TryCompleteActiveNode();
            }
            r.TrySelectMapNode(shop);t.GetField("<Coins>k__BackingField",Private).SetValue(r,100);Invoke("Refresh");
            yield return Shot("shop-draft-stock");SetSize(1280,720);yield return Shot("shop-draft-stock-720p");SetSize(1920,1080);
            Set("inspectedShopItem",12);yield return Shot("shop-draft-chances");Set("inspectedShopItem",-1);
            Check(r.TryBuyShop(12),"Shop draft purchase");Invoke("Refresh");yield return Shot("shop-draft-choice");
            r.TrySkipReward();Invoke("Refresh");yield return Shot("shop-draft-sold-return");Check(r.IsShop && r.ShopSold(12),"Shop return");
        }
        private IEnumerator RewardRowScenario()
        {
            flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);Set("flow",flow);
            flow.OpenCharacterSelection();flow.TrySelectCharacter(1);Check(flow.TryStartRun(CombatApproach.Tuning,true,false,false),"New start");
            var r=flow.CurrentRun;Check(r.Phase==RunPhase.MapSelection && r.Relics.Count==0,"Free starter still granted");
            Invoke("Refresh");yield return Shot("01-no-free-starter");
            r.TrySelectMapNode(0);WinEconomyBattle(r);Invoke("Refresh");yield return Shot("02-opening-row");
            SetSize(1280,720);yield return Shot("03-opening-row-720p");SetSize(1920,1080);
            Check(r.TryClaimLoot(0),"Coins");Invoke("Refresh");yield return Shot("04-coins-disappeared");
            Check(r.PendingLootCount==r.LootCount-1,"Pending count");
            Check(RunGameSession.TryRestore(r.CaptureSave(),out var restored,out var character),"Partial restore");
            flow.ReturnToMainMenu();flow.TryContinueRun(restored,character);r=restored;Invoke("Refresh");yield return Shot("05-restored-row");
            var t=typeof(RunGameSession);t.GetMethod("ResetLoot",Private).Invoke(r,null);var add=t.GetMethod("AddLoot",Private);
            string[] titles={"은화 18","파편 연구권","새로운 파편","유물 발견","상자 열쇠","봉인된 유물 상자"};
            string[] descriptions={"상점에서 카드·유물·정비를 구매합니다.","지도에서 카드 3장 중 하나를 고릅니다.","카드 3장 중 하나 선택. 원하지 않으면 넘길 수 있습니다.","미보유 유물 중 하나를 골라 가져갑니다.","상자 하나를 실패 없이 엽니다.","열쇠 1개: 확정 개방\n직접 열기: 성공률 50%\n실패하면 상자를 잃습니다. 시도는 한 번."};
            for(int i=0;i<6;i++)add.Invoke(r,new object[]{(LootKind)i,i==0?18:1,titles[i],descriptions[i]});
            Invoke("Refresh");yield return Shot("06-six-row-left");
            bool checkedInput=false;
            view.DiagnosticGuiCheck=()=>
            {
                var e=new Event{type=EventType.ScrollWheel,mousePosition=new Vector2(600,500),delta=new Vector2(0,3)};
                Invoke("HandleLootPan",e);Check(Get<float>("lootScroll")>0 && e.type==EventType.Used,"Wheel did not scroll row");
                int before=r.Revision;
                Invoke("HandleLootPan",new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(700,803)});
                Invoke("HandleLootPan",new Event{type=EventType.MouseDrag,button=0,mousePosition=new Vector2(430,803)});
                e=new Event{type=EventType.MouseUp,button=0,mousePosition=new Vector2(430,803)};Invoke("HandleLootPan",e);
                Check(e.type==EventType.Used && !Get<bool>("lootDragging") && GUIUtility.hotControl==0,"Drag release could claim another card");
                Check(r.Revision==before,"Navigation claimed reward");checkedInput=true;
            };
            yield return null;yield return null;Check(checkedInput,"Input check not run");
            SetSize(1280,720);yield return Shot("07-six-row-right-720p");SetSize(1920,1080);
            Check(r.TryClaimLoot(4),"Key reward after pan");Invoke("Refresh");yield return Shot("08-key-disappeared-row-clamped");
            Check(Get<float>("lootScroll")==0,"Shorter row was left off screen");
            r.TryClaimLoot(0);r.TryClaimLoot(1);r.TryClaimLoot(2);r.TrySkipReward();r.TryClaimLoot(3);r.TrySelectRelicReward(0);
            Invoke("Refresh");yield return Shot("09-only-chest-left");
            r.TryOpenLootChest(5,true);Invoke("Refresh");yield return Shot("10-chest-relic-choice");
            r.TrySelectRelicReward(0);Invoke("Refresh");yield return Shot("11-empty-row");Check(r.PendingLootCount==0,"Empty rewards not reached");
            t.GetMethod("PrepareVaultSupplies",Private).Invoke(r,null);Invoke("Refresh");yield return Shot("12-new-vault-row");
            Set("confirmLeaveLoot",true);SetSize(1280,720);yield return Shot("13-leave-confirmation-720p");SetSize(1920,1080);Set("confirmLeaveLoot",false);
            t.GetField("chestWillOpen",Private).SetValue(r,false);r.TryOpenLootChest(0,false);Set("lootNoticeIndex",0);Invoke("Refresh");yield return Shot("14-failed-chest-disappeared");
        }
        private IEnumerator SuppliesScenario()
        {
            for(int a=1;a<=4;a++)
            {
                flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);Set("flow",flow);
                flow.OpenCharacterSelection();flow.TrySelectCharacter(a<3?0:1);
                Check(flow.TryStartRun((CombatApproach)a,true,true),"Supplies start");Invoke("Refresh");
                Check(flow.CurrentRun.IsStartingRelic && flow.CurrentRun.Growth.Points==3,"Three-point preparation missing");
                yield return Shot("starting-relic-"+a);
                SetSize(1280,720);yield return Shot("starting-relic-"+a+"-720p");SetSize(1920,1080);
                flow.CurrentRun.TrySelectRelicReward(0);Invoke("Refresh");
                Set("growthOpen",true);Invoke("EnsureGrowthLayout",flow.CurrentRun.Growth);Invoke("FitGrowthTree");
                yield return Shot("three-point-tree-"+a);Set("growthOpen",false);
                flow.ReturnToMainMenu();Invoke("Refresh");
            }
            flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);Set("flow",flow);
            flow.OpenCharacterSelection();flow.TrySelectCharacter(1);flow.TryStartRun(CombatApproach.Tuning,true,true);
            var r=flow.CurrentRun;r.TrySelectRelicReward(0);r.TrySelectMapNode(0);WinEconomyBattle(r);Invoke("Refresh");
            Check(r.Phase==RunPhase.Loot && r.Coins==0 && r.ResearchTickets==0,"Loot was auto-collected");
            yield return Shot("opening-loot");SetSize(1280,720);yield return Shot("opening-loot-720p");SetSize(1920,1080);
            r.TryClaimLoot(0);r.TryClaimLoot(2);Invoke("Refresh");yield return Shot("loot-card-choice");r.TrySkipReward();Invoke("Refresh");
            var data=r.CaptureSave();Check(RunGameSession.TryRestore(data,out var restored,out var character),"Partial loot restore");
            Check(restored.Coins==r.Coins && restored.ResearchTickets==0 && restored.GetLoot(0).Resolved,"Partial loot was duplicated");
            flow.ReturnToMainMenu();flow.TryContinueRun(restored,character);r=restored;Invoke("Refresh");yield return Shot("partial-loot-restored");
            Set("confirmLeaveLoot",true);yield return Shot("leave-loot-confirmation");r.TryLeaveLoot();Invoke("Refresh");
            yield return Shot("map-keys-and-resources");
            // Explicit UI-only fixture for six visible reward types and both chest outcomes.
            var t=typeof(RunGameSession);t.GetMethod("ResetLoot",Private).Invoke(r,null);
            var add=t.GetMethod("AddLoot",Private);
            foreach(LootKind kind in Enum.GetValues(typeof(LootKind)))
                add.Invoke(r,new object[]{kind,1,kind==LootKind.Chest?"봉인된 유물 상자":kind==LootKind.Key?"상자 열쇠":kind==LootKind.Coins?"은화 18":kind==LootKind.Research?"파편 연구권":kind==LootKind.Cards?"새로운 파편":"유물 발견",
                    kind==LootKind.Chest?"열쇠 1개: 확정 개방\n직접 열기: 성공률 50%\n실패하면 상자를 잃습니다.":"원하는 보상만 선택해서 받으세요."});
            t.GetProperty("Phase").SetValue(r,RunPhase.Loot);Invoke("Refresh");
            yield return Shot("six-loot-cards");SetSize(1280,720);yield return Shot("six-loot-cards-720p");SetSize(1920,1080);
            t.GetField("chestWillOpen",Private).SetValue(r,false);Check(r.TryOpenLootChest(5,false),"Chest attempt");Invoke("Refresh");
            yield return Shot("chest-failed");Check(!r.TryOpenLootChest(5,true),"Failed chest retried");
            t.GetMethod("PrepareVaultSupplies",Private).Invoke(r,null);Invoke("Refresh");yield return Shot("vault-chest");
            Check(r.TryOpenLootChest(0,true),"Key opening");Invoke("Refresh");yield return Shot("chest-relic-choice");
            Check(r.Keys==0,"Key was not consumed");r.TrySelectRelicReward(0);Invoke("Refresh");yield return Shot("chest-claimed");
        }
        private IEnumerator GrowthScenario()
        {
            yield return null;
            for(int a=1;a<=4;a++)
            {
                flow.OpenCharacterSelection();flow.TrySelectCharacter(a<3?0:1);
                Check(flow.TryStartRun((CombatApproach)a, false),"Growth run start");Invoke("Refresh");
                var r=flow.CurrentRun;var g=r.Growth;var layout=new GrowthTreeLayout(g);
                Check(g.HasPointLimit&&g.Points==6,"Budget not enabled");
                for(int i=1;i<g.NodeCount;i++)
                {
                    Check(layout.Positions[i].y<layout.Positions[g.ParentOf(i)].y,"Tree does not ascend");
                    for(int j=0;j<i;j++)Check((layout.Positions[i]-layout.Positions[j]).sqrMagnitude>240*240,"Node overlap");
                }
                Set("growthOpen",true);Invoke("EnsureGrowthLayout",g);Invoke("FitGrowthTree");
                yield return Shot("growth-"+a+"-overview");
                SetSize(1280,720);yield return Shot("growth-"+a+"-overview-720p");SetSize(1920,1080);
                // Explicit visual fixture award; save/budget replay is tested in EditMode.
                g.AddPoints(100);foreach(int n in new[]{1,2,3,4,5,6,7})Check(r.TryPurchaseGrowthNode(n),"Spend nine-point fixture");
                Check(g.Points==0&&g.SpentPoints==9,"Budget exceeded");Invoke("Refresh");Invoke("FocusGrowthNode",g,8);
                SetSize(1280,720);yield return Shot("growth-"+a+"-limit-720p");SetSize(1920,1080);
                Check(r.TryRefundGrowthNode(3),"Refund capstone");Check(r.TryPurchaseGrowthNode(8),"Replace capstone");Invoke("Refresh");Invoke("FitGrowthTree");
                yield return Shot("growth-"+a+"-reallocated");
                Check(r.TryResetGrowth(),"Reset growth");Check(g.Points==9&&g.TotalPointBudget==9,"Reset changed budget");
                Set("growthOpen",false);flow.ReturnToMainMenu();Invoke("Refresh");
            }
        }
        private IEnumerator Scenario()
        {
            yield return Shot("01-title-new");
            var fixtureLegacy = Get<LegacyProgression>("legacy");
            fixtureLegacy.AwardRun(7001, false, 3); fixtureLegacy.AwardRun(7002, false, 3); fixtureLegacy.AwardRun(7003, false, 3);
            Set("legacyOpen", true); yield return Shot("01b-legacy-tree"); Set("legacyOpen", false);
            Get<int[]>("legacyCart")[0] = 3; Get<int[]>("legacyCart")[2] = 2;
            Set("legacyOpen", true); yield return Shot("22-legacy-cart-insufficient");
            SetSize(1280,720); yield return Shot("22-legacy-cart-720p"); SetSize(1920,1080);
            Check(fixtureLegacy.GetRank(0) == 0 && fixtureLegacy.Currency == 12, "Legacy preview charged currency");
            Invoke("ResetLegacyCart"); Set("legacyOpen", false);
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
            view.DiagnosticHoveredCharacter = -2; flow.TrySelectCharacter(1); Invoke("Refresh");
            yield return new WaitForSecondsRealtime(.25f);
            Check(Get<float[]>("characterEyeOpen")[1] > .9f, "Selected character did not keep her eyes open");
            yield return Shot("04c-character-selected-luna");
            flow.TryStartRun(); Invoke("Refresh");
            yield return TacticalThreatScreens();
            ValidateGrowthLayout(new RunGrowthState(CombatArchetype.Ian));
            ValidateGrowthLayout(flow.CurrentRun.Growth);
            yield return Shot("04-map-saved");
            Invoke("EnsureGrowthLayout", flow.CurrentRun.Growth); Invoke("FitGrowthTree");
            Set("growthOpen", true); yield return Shot("04c-run-growth-whole-tree"); Set("growthOpen", false);
            Invoke("FocusGrowthNode", flow.CurrentRun.Growth, flow.CurrentRun.Growth.IndexOf(GrowthTreePaths.RootId));
            Set("growthOpen", true); yield return Shot("04i-growth-distributed-root"); Set("growthOpen", false);
            Set("growthSearch", "원격"); Invoke("NextGrowthSearch", flow.CurrentRun.Growth, 1);
            Set("growthOpen", true); yield return Shot("22-growth-search");
            Check(Get<int>("growthSearchCount") > 0, "Effect search has no results");
            int planRevision = flow.CurrentRun.Revision;
            Invoke("FocusGrowthNode", flow.CurrentRun.Growth, flow.CurrentRun.Growth.IndexOf("common.origin.01"));
            Invoke("OpenGrowthPlan"); yield return Shot("22-growth-plan");
            SetSize(1280,720); yield return Shot("22-growth-plan-720p"); SetSize(1920,1080);
            Check(flow.CurrentRun.Revision == planRevision, "Planning mutated the run");
            Set("growthPlanOpen", false); Set("growthSearch", ""); Set("growthOpen", false);
            int identity = flow.CurrentRun.Growth.IndexOf("luna.orbit.core");
            int triple = flow.CurrentRun.Growth.IndexOf("luna.fulljump.form");
            Check(flow.CurrentRun.TryPurchaseGrowthNode(flow.CurrentRun.Growth.IndexOf(GrowthTreePaths.RootId)), "Root unlock failed");
            Check(flow.CurrentRun.TryPurchaseGrowthNode(flow.CurrentRun.Growth.IndexOf("common.origin.01")), "Origin route unlock failed");
            Check(flow.CurrentRun.TryPurchaseGrowthNode(identity), "Run identity unlock failed"); Invoke("Refresh");
            Invoke("FocusGrowthNode", flow.CurrentRun.Growth, identity);
            Set("growthOpen", true); yield return Shot("04d-run-growth-identity"); Set("growthOpen", false);
            Check(flow.CurrentRun.TryPurchaseGrowthNode(flow.CurrentRun.Growth.IndexOf("common.geometry.02")), "Geometry investment failed");
            Check(flow.CurrentRun.TryPurchaseGrowthNode(flow.CurrentRun.Growth.IndexOf("common.fragments.01")), "Fragment investment failed");
            Check(flow.CurrentRun.TryPurchaseGrowthNode(triple), "Run combat form unlock failed"); Invoke("Refresh");
            Invoke("FocusGrowthNode", flow.CurrentRun.Growth, triple);
            Set("growthOpen", true); yield return Shot("04e-run-growth-combat-form"); Set("growthOpen", false);
            Invoke("FocusGrowthNode", flow.CurrentRun.Growth, flow.CurrentRun.Growth.IndexOf("common.condense.07"));
            Set("growthOpen", true); yield return Shot("04f-run-growth-mixed-rules"); Set("growthOpen", false);
            int beforeHover = flow.CurrentRun.Growth.AcquiredCount;
            int selectedBeforeHover = Get<int>("growthFocusedNode");
            view.DiagnosticGrowthHover = flow.CurrentRun.Growth.IndexOf(GrowthTreePaths.RootId);
            view.DiagnosticGrowthTerm = 3;
            Set("growthOpen", true); yield return Shot("04j-growth-hover-keyword"); Set("growthOpen", false);
            Check(Get<int>("growthFocusedNode") == selectedBeforeHover, "Hover must not replace a clicked node");
            Check(flow.CurrentRun.Growth.AcquiredCount == beforeHover, "Hover must not purchase a node");
            view.DiagnosticGrowthHover = -1; view.DiagnosticGrowthTerm = -1;
            Invoke("FocusGrowthNode", flow.CurrentRun.Growth, flow.CurrentRun.Growth.IndexOf("luna.u.timeless.form"));
            Set("growthOpen", true); yield return Shot("04g-run-growth-turn-change"); Set("growthOpen", false);
            ValidateGrowthInput();
            SetSize(1280, 720); Set("growthOpen", true);
            yield return Shot("04h-run-growth-720p");
            Set("growthOpen", false); SetSize(1920, 1080);
            Check(File.Exists(Path.Combine(dataDirectory, "run.save")), "New run not saved");
            flow.CurrentRun.TrySelectMapNode(0); Invoke("Refresh");
            var previewRun = flow.CurrentRun;
            var phaseField = typeof(RunGameSession).GetField("<Phase>k__BackingField", Private);
            var rewardField = typeof(RunGameSession).GetField("rewardOptions", Private);
            var previewRewards = (CardDefinition[])rewardField.GetValue(previewRun);
            previewRewards[0] = FragmentCardCatalog.Find("frag.west");
            previewRewards[1] = FragmentCardCatalog.Find("frag.ellipse");
            previewRewards[2] = FragmentCardCatalog.Find("frag.sanctuary");
            phaseField.SetValue(previewRun, RunPhase.CardReward); view.DiagnosticRewardHover = 0; Invoke("Refresh");
            yield return Shot("04e-card-reward-layout");
            view.DiagnosticRewardHover = -1; phaseField.SetValue(previewRun, RunPhase.Battle);
            Array.Clear(previewRewards, 0, previewRewards.Length); Invoke("Refresh");
            ValidateHandInput();
            Invoke("PlayCard", 0); Check(TryAnyMove(flow.CurrentRun), "Move failed"); Invoke("Refresh");
            string card = flow.CurrentRun.CurrentBattle.Battle.GetPlayedCard(0).Id;
            double movedX = flow.CurrentRun.CurrentBattle.Battle.Tactics.X;
            double movedY = flow.CurrentRun.CurrentBattle.Battle.Tactics.Y;
            yield return Shot("05-battle-saved");
            view.DiagnosticHoveredCard = 0; yield return Shot("05b-card-book-hover");
            var diagnosticAbility = flow.CurrentRun.CurrentBattle.Deck.GetHandCard(0).GetAbility(0);
            view.DiagnosticKeyword = (int)diagnosticAbility.Kind;
            view.DiagnosticKeywordBody = "현재 카드 효과 · 수치 " + diagnosticAbility.Magnitude
                + " · 남은 적 행동 " + diagnosticAbility.Duration + "회\n설명창 위에 마우스를 두어도 카드와 설명이 유지됩니다.";
            yield return Shot("05c-keyword-tooltip");
            view.DiagnosticHoveredCard = -1; view.DiagnosticKeyword = -1; view.DiagnosticKeywordBody = "";
            bool keywordChecked=false;
            view.DiagnosticGuiCheck=()=> { ValidateKeywordInput();keywordChecked=true; };
            yield return Shot("05d-keyword-click-pinned");
            Check(keywordChecked,"Keyword input checks did not run");
            SetSize(1280,720);yield return Shot("05e-keyword-click-720p");SetSize(1920,1080);
            Invoke("CloseKeyword");
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
            if (flow.CurrentRun.Phase == RunPhase.Battle)
            {
                BattleSession skillBattle = flow.CurrentRun.CurrentBattle.Battle;
                int targetIndex = -1;
                for (int i = 0; i < skillBattle.Enemies.Count; i++)
                    if (skillBattle.Enemies[i].IsAlive) { targetIndex = i; break; }
                Check(targetIndex >= 0, "No living skill target");
                int durabilityBefore = skillBattle.Enemies[targetIndex].Health
                    + skillBattle.Enemies[targetIndex].Statuses.Get(CombatStatusKind.Shield);
                Invoke("HandleKeyEvent", new Event { type = EventType.KeyDown, keyCode = KeyCode.K });
                Check(Get<bool>("combatSkillTargeting"), "Skill button did not enter target selection");
                int beforeCancel = flow.CurrentRun.Revision;
                Invoke("HandleKeyEvent", new Event { type = EventType.MouseDown, button = 1 });
                Check(!Get<bool>("combatSkillTargeting"), "Right click did not cancel skill aim");
                Check(flow.CurrentRun.Revision == beforeCancel, "Cancelling skill changed journal");
                Invoke("HandleKeyEvent", new Event { type = EventType.KeyDown, keyCode = KeyCode.K });
                Invoke("UseCombatSkillOnTarget", -1);
                Check(skillBattle.CombatSkillCooldown == 0, "View used nearest target without selection");
                var skillPrediction = skillBattle.PreviewCombatSkill(targetIndex);
                int predictedHp = 0, predictedShield = 0;
                for (int i = 0; i < skillBattle.Enemies.Count; i++)
                {
                    var contact = skillBattle.PreviewCombatSkillContact(skillPrediction, i);
                    predictedHp += contact.HealthDamage; predictedShield += contact.ShieldDamage;
                }
                previewCaptureTarget = targetIndex;
                yield return Shot("12a-skill-targeting");
                SetSize(1280, 720); yield return Shot("21-skill-preview-720p"); SetSize(1920, 1080);
                previewCaptureTarget = -1;
                Invoke("UseCombatSkillOnTarget", targetIndex);
                Check(skillBattle.LastSkillDamage == predictedHp && skillBattle.LastSkillShieldDamage == predictedShield,
                    "Skill preview differs from displayed damage");
                Check(skillBattle.CombatSkillCooldown == BattleSession.CombatSkillCooldownTurns
                    || skillBattle.LastSkillCooldownReset, "Cooldown skill did not resolve");
                int durabilityAfter = skillBattle.Enemies[targetIndex].Health
                    + skillBattle.Enemies[targetIndex].Statuses.Get(CombatStatusKind.Shield);
                Check(durabilityAfter < durabilityBefore, "Targeted skill did not damage health or shield");
                yield return Shot("12b-path-skill");
                // Explicit presentation fixtures; the separate Core tests verify real health/shield accounting.
                Get<GamePreferences>("preferences").ReduceMotion=false;
                Set("playerImpactAt",Get<float>("viewTime")); Set("playerImpactHealth",true);
                Set("playerImpactText","체력 −7\n보호막 −3");
                yield return Shot("12c-player-damage-feedback");
                Set("playerImpactAt",Get<float>("viewTime")); Set("playerImpactHealth",false);
                Set("playerImpactText","방어 성공\n보호막 −5");
                yield return Shot("12d-player-shield-feedback");
                Get<GamePreferences>("preferences").ReduceMotion=true;
                SetSize(1280,720); Set("playerImpactAt",Get<float>("viewTime"));
                yield return Shot("12e-player-feedback-reduced-motion"); SetSize(1920,1080);
            }
            SetSize(1280, 720); Set("paused", true); yield return Shot("13-pause-720p");
            Invoke("OpenSettings"); yield return Shot("14-settings-720p"); Invoke("CloseSettings");
            Invoke("OpenHelp"); Set("helpPage", 3); yield return Shot("15-guide-720p"); Invoke("CloseHelp");
            Invoke("OpenInventory"); yield return Shot("16-inventory-720p"); Set("inventoryOpen", false);
            Invoke("SaveAndReturn"); yield return Shot("17-title-720p");
            SetSize(1920, 1080);
            var savedBeforePractice = Get<RunGameSession>("savedRun");
            int currencyBeforePractice = Get<LegacyProgression>("legacy").Currency;
            foreach (CombatApproach approach in new[] { CombatApproach.Execution, CombatApproach.Recording, CombatApproach.Tuning })
            {
                Invoke("StartApproach", approach);
                var trial = flow.CurrentRun;
                Check(trial.IsPractice && !trial.CanSave, "Practice was not isolated");
                foreach (string id in new[] { "frag.ellipse", "frag.expand" })
                {
                    for (int i = 0; i < trial.CurrentBattle.Deck.HandCount; i++)
                        if (trial.CurrentBattle.Deck.GetHandCard(i).Id == id) { Invoke("PlayCard", i); break; }
                }
                yield return Shot("22-approach-" + approach + "-before");
                if (approach == CombatApproach.Execution) Invoke("UseCombatSkillOnTarget", 0);
                else
                {
                    Invoke("ToggleCombatSkillTargeting");
                    Check(Get<bool>("combatSkillTargeting"), "Diagram ability did not enter aiming");
                    Invoke("HandleKeyEvent", new Event { type = EventType.MouseDown, button = 1 });
                    Check(!Get<bool>("combatSkillTargeting"), "Diagram aim did not cancel");
                    Check(trial.TryUseDiagramAbility(approach == CombatApproach.Tuning ? 5.25 : 5.5,
                        approach == CombatApproach.Tuning ? -1.5 : .5, approach == CombatApproach.Tuning ? -90 : 0), "Diagram command rejected");
                    Invoke("Refresh");
                }
                yield return Shot("23-approach-" + approach + "-after");
                Invoke("StartCast"); yield return new WaitForSecondsRealtime(2.4f);
                Check(trial.CurrentBattle.Battle.Phase != BattlePhase.Plotting, "Practice cast stuck");
                SetSize(1280, 720); yield return Shot("24-approach-" + approach + "-720p"); SetSize(1920, 1080);
                if (approach == CombatApproach.Recording)
                {
                    for (int i = 0; i < 8 && trial.Phase == RunPhase.Battle; i++)
                    { Check(trial.TryUnravel(), "Practice pass rejected"); trial.ResolveEnemyTurn(); }
                    Check(trial.Phase == RunPhase.Defeated, "Practice terminal fixture did not end");
                    Invoke("Refresh"); yield return Shot("25-approach-result");
                }
                Invoke("SaveAndReturn");
                Check(ReferenceEquals(savedBeforePractice, Get<RunGameSession>("savedRun")), "Practice replaced continue slot");
                Check(Get<LegacyProgression>("legacy").Currency == currencyBeforePractice, "Practice changed legacy currency");
            }
            foreach (CombatApproach approach in new[] { CombatApproach.Execution, CombatApproach.Recording, CombatApproach.Tuning })
            {
                flow.OpenCharacterSelection(); flow.TrySelectCharacter(approach == CombatApproach.Tuning ? 1 : 0);
                Invoke("Refresh"); yield return Shot("26-campaign-choice-" + approach);
                Check(flow.TryStartRun(approach, false), "New campaign choice rejected");
                Invoke("Refresh");
                Check(flow.CurrentRun.TrySelectMapNode(0), "New campaign entry failed");
                Invoke("Refresh");
                foreach (string id in new[] { "frag.ellipse", "frag.expand" })
                    for (int i = 0; i < flow.CurrentRun.CurrentBattle.Deck.HandCount; i++)
                        if (flow.CurrentRun.CurrentBattle.Deck.GetHandCard(i).Id == id) { Invoke("PlayCard", i); break; }
                if (approach != CombatApproach.Execution)
                {
                    var diagramBattle = flow.CurrentRun.CurrentBattle.Battle;
                    Check(flow.CurrentRun.TryUseDiagramAbility(diagramBattle.Equation.Fragments.OriginX,
                        diagramBattle.Equation.Fragments.OriginY, approach == CombatApproach.Tuning ? 30 : 0), "Campaign diagram rejected");
                    Invoke("Refresh");
                }
                yield return Shot("27-campaign-battle-" + approach);
                Invoke("SaveAndReturn"); Invoke("ContinueSavedRun");
                Check(flow.CurrentRun.StartingApproach == approach, "Campaign approach lost on continue");
                Check(flow.CurrentRun.CurrentBattle.Battle.Approach == approach, "Battle approach lost on continue");
                yield return Shot("28-campaign-restored-" + approach);
                Invoke("SaveAndReturn");
            }
            foreach (CombatApproach approach in new[] { CombatApproach.Execution, CombatApproach.Tuning })
            {
                flow.OpenCharacterSelection(); flow.TrySelectCharacter(approach == CombatApproach.Tuning ? 1 : 0);
                Check(flow.TryStartRun(approach, false), "Spatial campaign start failed");
                Check(flow.CurrentRun.TrySelectMapNode(0), "Spatial campaign entry failed"); Invoke("Refresh");
                for (int i = 0; i < flow.CurrentRun.CurrentBattle.Deck.HandCount; i++)
                    if (flow.CurrentRun.CurrentBattle.Deck.GetHandCard(i).Id == "frag.ellipse") { Invoke("PlayCard", i); break; }
                bool pull = approach == CombatApproach.Tuning;
                Invoke("HandleKeyEvent", new Event { type = EventType.KeyDown, keyCode = pull ? KeyCode.J : KeyCode.K });
                Check(Get<bool>("combatSkillTargeting"), "Spatial key did not start aiming");
                Set("spatialPreviewFixture", true);
                Set("spatialFixturePointer", new Vector2(460 + (pull ? 5.5f : 9f) * 100, 88 + (pull ? 5f : 6f) * 100));
                yield return Shot("29-spatial-preview-" + approach);
                SetSize(1280, 720); yield return Shot("30-spatial-preview-720p-" + approach); SetSize(1920, 1080);
                Set("spatialPreviewFixture", false);
                Invoke("HandleKeyEvent", new Event { type = EventType.MouseDown, button = 1 });
                Check(!Get<bool>("combatSkillTargeting"), "Spatial aim cancel failed");
                Invoke("HandleKeyEvent", new Event { type = EventType.KeyDown, keyCode = pull ? KeyCode.J : KeyCode.K });
                if (pull) Invoke("UseLunaPullAt", 5.5, -1.0); else Invoke("UseExecutionAt", 9.0, -2.0);
                Check(!Get<bool>("combatSkillTargeting"), "Spatial command not committed");
                var source = flow.CurrentRun.CurrentBattle.Battle;
                Check(pull ? source.LastPullCount > 0 : source.Tactics.X > source.Enemies[0].X, "Spatial movement did not happen");
                double playerX = source.Tactics.X, enemyX = source.Enemies[0].X;
                yield return Shot("31-spatial-committed-" + approach);
                Invoke("SaveAndReturn"); Invoke("ContinueSavedRun");
                var restored = flow.CurrentRun.CurrentBattle.Battle;
                Check(Math.Abs(restored.Tactics.X - playerX) < .00001 && Math.Abs(restored.Enemies[0].X - enemyX) < .00001, "Spatial save changed positions");
                yield return Shot("32-spatial-restored-" + approach);
                Invoke("SaveAndReturn");
            }
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(1); Invoke("Refresh");
            yield return Shot("33-two-luna-approaches");
            Check(flow.TryStartRun(CombatApproach.Observation, false), "Observation campaign start failed");
            Check(flow.CurrentRun.TrySelectMapNode(0), "Observation entry failed"); Invoke("Refresh");
            var observation = flow.CurrentRun;
            for (int i=0;i<observation.CurrentBattle.Deck.HandCount;i++)
                if (observation.CurrentBattle.Deck.GetHandCard(i).Id == "frag.ellipse") { Invoke("PlayCard", i); break; }
            Invoke("HandleKeyEvent", new Event { type=EventType.KeyDown,keyCode=KeyCode.K });
            Check(Get<bool>("combatSkillTargeting"), "Satellite aim did not start");
            Invoke("HandleKeyEvent",new Event { type=EventType.MouseDown,button=1 });
            Check(!Get<bool>("combatSkillTargeting"), "Satellite aim did not cancel");
            Invoke("ToggleCombatSkillTargeting"); Invoke("PlaceSatelliteAt",6.2,-2.0);
            Check(observation.CurrentBattle.Battle.SatellitePlotHit && !observation.CurrentBattle.Battle.BodyPlotHit,"Satellite did not supply remote self hit");
            yield return Shot("34-satellite-remote-protection");
            SetSize(1280,720); yield return Shot("35-satellite-720p"); SetSize(1920,1080);
            Invoke("SaveAndReturn"); Invoke("ContinueSavedRun");
            Check(flow.CurrentRun.CurrentBattle.Battle.SatellitePlotHit,"Satellite lost on continue");
            Invoke("StartCast"); yield return new WaitForSecondsRealtime(2.4f);
            Check(flow.CurrentRun.CurrentBattle.Battle.HasSatellite,"Satellite lost after plot");
            Invoke("HandleKeyEvent",new Event { type=EventType.KeyDown,keyCode=KeyCode.J });
            Check(flow.CurrentRun.CurrentBattle.Battle.SatelliteOriginSelected,"Satellite origin toggle failed");
            Invoke("PlayCard",0);
            Check(Math.Abs(flow.CurrentRun.CurrentBattle.Battle.Equation.Fragments.OriginX-6.2)<.001,"Remote equation origin not applied");
            yield return Shot("36-satellite-origin");
            Invoke("SaveAndReturn"); Invoke("ContinueSavedRun");
            Check(flow.CurrentRun.StartingApproach==CombatApproach.Observation && flow.CurrentRun.CurrentBattle.Battle.SatelliteOriginSelected,"Observation restore failed");
            yield return Shot("37-satellite-restored");
            Invoke("SaveAndReturn");
            Invoke("StartApproach",CombatApproach.Observation);
            Check(flow.CurrentRun.IsPractice && !flow.CurrentRun.CanSave,"Observation practice not isolated");
            yield return Shot("38-observation-practice"); Invoke("SaveAndReturn");
            foreach(string id in new[] { "ian.inscription.core", "ian.archive.core", "luna.orbit.core", "luna.binary.core" })
            {
                bool luna=id.StartsWith("luna.",StringComparison.Ordinal);
                flow.OpenCharacterSelection(); flow.TrySelectCharacter(luna?1:0);
                Check(flow.TryStartRun(luna?CombatApproach.Tuning:CombatApproach.Execution, false),"Node trial source start failed"); Invoke("Refresh");
                var original=flow.CurrentRun; var originalSave=original.CaptureSave(); originalSave.SavedUtcTicks=0;
                string fingerprint=Convert.ToBase64String(RunSaveStore.Encode(originalSave));
                int currency=Get<LegacyProgression>("legacy").Currency;
                Set("growthOpen",true); Invoke("EnsureGrowthLayout",original.Growth);
                int node=original.Growth.IsSpecialized?0:original.Growth.IndexOf(id); Invoke("FocusGrowthNode",original.Growth,node);
                Vector2 pan=Get<Vector2>("growthPan"); float zoom=Get<float>("growthZoom");
                yield return Shot("39-trial-node-"+id);
                Invoke("OpenNodeComparison",id);
                yield return Shot("40-node-comparison-"+id);
                SetSize(1280,720); yield return Shot("41-node-comparison-720p-"+id); SetSize(1920,1080);
                Invoke("HandleKeyEvent",new Event { type=EventType.KeyDown,keyCode=KeyCode.Escape });
                Check(Get<NodeTrialComparison>("nodeComparison")==null && Get<bool>("growthOpen"),"Comparison did not return to tree");
                Invoke("OpenNodeComparison",id); Invoke("StartSelectedNodeTrial");
                Check(flow.IsNodeTrial && flow.CurrentRun.IsPractice && !Get<bool>("growthOpen"),"Node trial did not start");
                Check(flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount==2,"Node trial hand was not prepared");
                Invoke("StartCast"); yield return new WaitForSecondsRealtime(2.4f);
                Check(flow.CurrentRun.CurrentBattle.Battle.Turn==2,"Node trial cast did not resolve");
                Check((bool)Invoke("SaveCurrent",true),"Suspended run save failed");
                Check(Get<RunSaveStore>("saves").TryLoad(out var stored,out _,out _),"Suspended run was not readable");
                var storedData=stored.CaptureSave(); storedData.SavedUtcTicks=0;
                Check(Convert.ToBase64String(RunSaveStore.Encode(storedData))==fingerprint,"Trial overwrote source save");
                if(id=="luna.binary.core")
                {
                    var trial=flow.CurrentRun;
                    for(int step=0;step<12 && trial.Phase==RunPhase.Battle;step++)
                    { Check(trial.TryUnravel(),"Trial pass failed"); trial.ResolveEnemyTurn(); }
                    Check(trial.Phase==RunPhase.Defeated,"Node trial terminal fixture did not finish");
                    Invoke("Refresh"); yield return Shot("42-node-trial-result");
                    Invoke("RestartNodeTrial"); Check(flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount==2,"Trial restart lost setup");
                    Invoke("StartCast"); Check(Get<bool>("castActive"),"Trial return-during-cast fixture did not start");
                }
                if(id=="luna.binary.core" || id=="ian.archive.core")
                    Invoke("HandleKeyEvent",new Event { type=EventType.KeyDown,keyCode=KeyCode.G });
                else Invoke("SaveAndReturn");
                Check(!Get<bool>("castActive"),"Trial cast continued after returning");
                Check(ReferenceEquals(flow.CurrentRun,original) && !flow.IsNodeTrial,"Original run object was not restored");
                Check(Get<bool>("growthOpen") && Get<int>("growthFocusedNode")==node,"Original tree node was not restored");
                Check(Get<Vector2>("growthPan")==pan && Get<float>("growthZoom")==zoom,"Tree camera was not restored");
                Check(Get<LegacyProgression>("legacy").Currency==currency,"Node trial awarded permanent currency");
                originalSave=original.CaptureSave(); originalSave.SavedUtcTicks=0;
                Check(Convert.ToBase64String(RunSaveStore.Encode(originalSave))==fingerprint,"Trial mutated source journal");
                yield return Shot("43-trial-returned-"+id);
                Set("growthOpen",false); Invoke("SaveAndReturn");
            }
            var openingScreens = OpeningRouteScreens();
            while (openingScreens.MoveNext()) yield return openingScreens.Current;
        }
        private IEnumerator OpeningRouteScreens()
        {
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(1);
            Check(flow.TryStartRun(CombatApproach.Tuning, false), "Opening route start failed");
            var opening = flow.CurrentRun;
            Check(opening.HasOpeningRoute, "New route not enabled");
            Invoke("Refresh"); yield return Shot("44-opening-map");
            opening.TrySelectMapNode(0); Invoke("Refresh");
            yield return Shot("45-opening-intro");
            for (int stage = 0; stage < 2; stage++)
            {
                for (int turn = 0; turn < 24 && opening.Phase == RunPhase.Battle; turn++)
                {
                    var b = opening.CurrentBattle.Battle;
                    EnemyState target = null;
                    foreach (var enemy in b.Enemies) if (enemy.IsAlive) { target = enemy; break; }
                    opening.TryMovePlayerTo(Math.Max(.5, target.X - 3), target.Y);
                    foreach (string id in new[] { "frag.ellipse", "frag.expand" })
                        for (int i = 0; i < opening.CurrentBattle.Deck.HandCount; i++)
                            if (opening.CurrentBattle.Deck.GetHandCard(i).Id == id) { opening.TryPlayHandCard(i, out _, out _); break; }
                    opening.TryUseDiagramAbility(Math.Max(.5, target.X - 3), target.Y);
                    Check(opening.TryBeginPlot(), "Opening plot rejected"); opening.ResolvePlot();
                    if (opening.Phase == RunPhase.Battle) opening.ResolveEnemyTurn();
                }
                Check(opening.Phase == RunPhase.CardReward, "Opening route did not reach reward");
                Invoke("Refresh"); yield return Shot("46-opening-reward-" + stage);
                opening.TrySkipReward();
                if (opening.Phase == RunPhase.RelicReward) opening.TrySkipReward();
                if (stage == 0)
                {
                    for (int i = 0; i < opening.Map.Definition.NodeCount; i++)
                        if (opening.Map.IsAvailable(i) && opening.Map.Definition.GetNode(i).Kind == RunNodeKind.Workshop)
                        { opening.TrySelectMapNode(i); break; }
                    Invoke("Refresh"); yield return Shot("47-opening-preparation");
                    Check(opening.TryChooseRoomOption(0), "Free opening training failed");
                    Invoke("SaveAndReturn"); Invoke("ContinueSavedRun"); opening = flow.CurrentRun;
                    Check(opening.HasOpeningRoute && opening.TrainingPower == 1, "Opening training lost on continue");
                    Invoke("Refresh"); SetSize(1280,720); yield return Shot("48-opening-grown-map-720p"); SetSize(1920,1080);
                }
                for (int i = 0; i < opening.Map.Definition.NodeCount; i++)
                    if (opening.Map.IsAvailable(i) && opening.Map.Definition.GetNode(i).Kind == RunNodeKind.Battle)
                    { opening.TrySelectMapNode(i); break; }
                Invoke("Refresh"); yield return Shot(stage == 0 ? "49-opening-application" : "50-opening-wave");
            }
            WinEconomyBattle(opening);
            Invoke("Refresh"); yield return Shot("51-currency-loot");
            while (opening.Phase == RunPhase.CardReward || opening.Phase == RunPhase.RelicReward) opening.TrySkipReward();
            for (int i=0;i<opening.Map.Definition.NodeCount;i++)
                if(opening.Map.IsAvailable(i) && opening.Map.Definition.GetNode(i).Kind==RunNodeKind.Shop)
                { opening.TrySelectMapNode(i); break; }
            Check(opening.IsShop,"First merchant not reachable"); Invoke("Refresh");
            yield return Shot("52-merchant-stock");
            SetSize(1280,720); yield return Shot("52-merchant-stock-720p"); SetSize(1920,1080);
            Check(opening.HasMarketBalance && opening.ShopOfferCount==13,"Expanded merchant is not active");
            int inspectionCoins=opening.Coins, inspectionCards=opening.Deck.Count;
            Set("inspectedShopItem",6); yield return Shot("52-merchant-card-inspection");
            SetSize(1280,720); yield return Shot("52-merchant-card-inspection-720p"); SetSize(1920,1080);
            Set("inspectedShopItem",10); yield return Shot("52-merchant-relic-inspection");
            Check(opening.Coins==inspectionCoins && opening.Deck.Count==inspectionCards,"Inspection purchased merchandise");
            Set("inspectedShopItem",-1);
            Check(opening.TryBuyShop(0),"Merchant card purchase failed");
            Check(opening.TryBuyShop(4),"Merchant removal selection failed");
            int coinsBeforeCancel=opening.Coins;
            Invoke("Refresh"); yield return Shot("53-merchant-removal");
            opening.TrySkipRefinement(); Check(opening.IsShop && opening.Coins==coinsBeforeCancel,"Removal cancel spent money or left shop");
            Check(opening.TryBuyShop(4),"Merchant removal retry failed");
            Invoke("SaveAndReturn"); Invoke("ContinueSavedRun"); opening=flow.CurrentRun;
            Check(opening.Phase==RunPhase.DeckRefinement,"Pending merchant removal not restored");
            Check(opening.TryRemoveDeckCard(opening.Deck.Count-1),"Merchant removal confirmation failed");
            Check(opening.IsShop && opening.ShopSold(4) && opening.Coins==coinsBeforeCancel-10,"Merchant payment or return mismatch");
            Invoke("Refresh"); yield return Shot("54-merchant-purchased");
            opening.TryLeaveRoom(); Check(opening.TryUseResearch(),"Banked research ticket not usable");
            Invoke("Refresh"); yield return Shot("55-research-reward");
            opening.TrySelectReward(0); Invoke("Refresh"); Set("growthOpen",true);
            Invoke("EnsureGrowthLayout",opening.Growth); Invoke("FocusGrowthNode",opening.Growth,opening.Growth.IndexOf("luna.rotate.m2"));
            yield return Shot("56-working-growth-node");
            Invoke("FocusGrowthNode",opening.Growth,opening.Growth.IndexOf("luna.u.timeless.form"));
            yield return Shot("57-pending-growth-node");
            Set("growthOpen",false);
            Invoke("SaveAndReturn");
            Invoke("StartApproach",CombatApproach.Observation);
            for(int i=0;i<flow.CurrentRun.CurrentBattle.Deck.HandCount;i++)
                if(flow.CurrentRun.CurrentBattle.Deck.GetHandCard(i).Id=="frag.ellipse") { Invoke("PlayCard",i); break; }
            Invoke("ToggleSatelliteTargeting");
            view.DiagnosticHoveredCard=0;
            yield return Shot("58-satellite-clean-aim");
            Check(Get<int>("hoveredHand")==-1 && Get<Rect>("handInspectionRect").width==0,"Card popup obscured satellite aim");
            view.DiagnosticHoveredCard=-1;
            int hand=flow.CurrentRun.CurrentBattle.Deck.HandCount;
            var click=new Event { type=EventType.MouseDown,button=0,mousePosition=new Vector2(1080,688) };
            Set("hoveredHand",0); Set("handInspectionRect",new Rect(1000,600,310,370)); Set("keywordTooltipRect",new Rect(1000,600,420,170));
            Invoke("HandleHandCardInput",click,0,hand);
            Check(click.type==EventType.MouseDown,"Card layer consumed aim click");
            Invoke("HandleBoardInput",click);
            Check(flow.CurrentRun.CurrentBattle.Battle.HasSatellite && Math.Abs(flow.CurrentRun.CurrentBattle.Battle.SatelliteX-6.2)<.001,"Stale card inspection blocked board input");
            Check(flow.CurrentRun.CurrentBattle.Deck.HandCount==hand,"Satellite placement played a card");
            SetSize(1280,720); yield return Shot("59-satellite-separated-720p"); SetSize(1920,1080);
            Invoke("SaveAndReturn");
            Set("codexOpen",true); Set("codexRelics",false); Invoke("ChangeCodex",0,4);
            Check(FragmentCardCatalog.All.Count==105,"Archive must contain 105 playable cards");
            for(int tier=0;tier<4;tier++)
            {
                Set("codexTier",tier); Invoke("ChangeCodex",0,0);
                int expected=0;foreach(var card in FragmentCardCatalog.All) if((int)card.DiagramRarity==tier) expected++;
                Check(Get<int>("codexCount")==expected,"Color filter disagrees with catalog");
                yield return Shot("62-color-filter-"+tier);
            }
            Invoke("ChangeCodex",3,0);
            Check(Get<int>("codexCount")==0 && Get<int>("codexSelected")==-1,"Empty color/role filter was not handled");
            yield return Shot("62-color-filter-empty");
            Set("codexTier",-1); Invoke("ChangeCodex",0,0);
            for(int i=0;i<FragmentCardCatalog.All.Count;i++)
            {
                Invoke("ChangeCodex",0,i/8); Invoke("SelectCodex",i);
                Set("maximumEffectOverflow",0f);
                yield return Shot("60-expanded-card-"+i);
                Check(Get<float>("maximumEffectOverflow")<=1,"Card effect text overflow: "+FragmentCardCatalog.All[i].Id+" / "+Get<float>("maximumEffectOverflow"));
            }
            Set("codexRelics",true); Invoke("ChangeCodex",0,2);
            for(int i=20;i<FragmentRelicCatalog.All.Count;i++)
            { Invoke("ChangeCodex",0,i/8); Invoke("SelectCodex",i); yield return Shot("61-expanded-relic-"+i); }
            Set("codexRelics",false);Set("codexEnemies",true);Invoke("ChangeCodex",0,0);
            for(int i=0;i<EnemyArchive.All.Count;i++)
            { Invoke("ChangeCodex",0,i/8);Invoke("SelectCodex",i);yield return Shot("65-enemy-archive-"+i); }
            SetSize(1280,720);yield return Shot("65-enemy-archive-720p");SetSize(1920,1080);
            Set("codexEnemies",false);
            Set("codexOpen",false); Set("codexRelics",false);
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(1);
            Check(flow.TryStartRun(CombatApproach.Tuning, false),"Status showcase start failed");
            Check(flow.CurrentRun.TrySelectMapNode(0),"Status showcase entry failed");
            var statusBattle=flow.CurrentRun.CurrentBattle.Battle;
            var addStatus=typeof(CombatStatusState).GetMethod("Add",BindingFlags.Instance|BindingFlags.NonPublic);
            var clearStatus=typeof(CombatStatusState).GetMethod("Clear",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int group=0;group<4;group++)
            {
                clearStatus.Invoke(statusBattle.Tactics.Statuses,null);
                clearStatus.Invoke(statusBattle.Enemies[0].Statuses,null);
                for(int i=0;i<4;i++)
                {
                    int index=group==3 ? 12+(i+1)%4 : group*4+i;
                    if(index==9) continue;
                    addStatus.Invoke(statusBattle.Tactics.Statuses,new object[]{(CombatStatusKind)index,3,2});
                    addStatus.Invoke(statusBattle.Enemies[0].Statuses,new object[]{(CombatStatusKind)index,3,2});
                }
                Invoke("Refresh");SetSize(1280,720);yield return Shot("63-status-icons-"+group);SetSize(1920,1080);
            }
            clearStatus.Invoke(statusBattle.Enemies[0].Statuses,null);
            addStatus.Invoke(statusBattle.Enemies[0].Statuses,new object[]{CombatStatusKind.Guidance,1,2});
            addStatus.Invoke(statusBattle.Enemies[0].Statuses,new object[]{CombatStatusKind.Wound,3,2});
            Invoke("Refresh");yield return Shot("64-guidance-wound");
            var originalRun=flow.CurrentRun;
            var runField=typeof(PrototypeGameFlow).GetField("<CurrentRun>k__BackingField",Private);
            try
            {
                for(int boss=17;boss<EnemyArchive.All.Count;boss++)
                {
                    if(boss==20 || boss==21)continue;
                    var definition=new BattleDefinition(999,3,new[]{EnemyArchive.All[boss].Create("boss.fixture",6.6,1.5)},CombatArchetype.Luna,fragments:true);
                    var trial=new RunGameSession(new[]{definition},PrototypeCharacterCatalog.All[1].CreateStartingDeckCopy(),FragmentCardCatalog.All,5,73);
                    runField.SetValue(flow,trial);trial.TrySelectMapNode(0);trial.CurrentBattle.Battle.EnableStatusRules();
                    for(int turn=1;turn<=4;turn++)
                    {Invoke("Refresh");yield return Shot("66-boss-"+boss+"-turn-"+turn);trial.TryUnravel();trial.ResolveEnemyTurn();}
                    trial.CurrentBattle.Battle.Enemies[0].TakeDamage(EnemyArchive.All[boss].Health/2+20);
                    trial.TryUnravel();trial.ResolveEnemyTurn();Invoke("Refresh");
                    Check(trial.CurrentBattle.Battle.Enemies[0].Enraged,"Boss second phase not entered");
                    yield return Shot("67-boss-phase-two-"+boss);
                }
            }
            finally {runField.SetValue(flow,originalRun);Invoke("Refresh");}
            var selectedField=typeof(PrototypeGameFlow).GetField("<SelectedCharacterIndex>k__BackingField",Private);
            int originalSelected=flow.SelectedCharacterIndex;
            try
            {
                for(int a=1;a<=4;a++)
                {
                    var approach=(CombatApproach)a;
                    var trial=SignatureBattleFactory.Create(approach);
                    var b=trial.CurrentBattle.Battle;b.EnableStatusRules();b.EnableApproachUltimates();
                    typeof(TacticalCombatState).GetMethod("GainResonance",Private).Invoke(b.Tactics,new object[]{6});
                    runField.SetValue(flow,trial);selectedField.SetValue(flow,a<3?0:1);
                    trial.TryPlayHandCard(0,out _,out _);
                    if(a==4)trial.TryPlaceSatellite(5.6,-2);
                    Check(trial.TryToggleUltimate(),"Approach ultimate unavailable");Invoke("Refresh");
                    yield return Shot("68-ultimate-"+a+"-preview");
                    Check(trial.TryBeginPlot(),"Approach ultimate cannot plot");trial.ResolvePlot();Invoke("Refresh");
                    yield return Shot("68-ultimate-"+a+"-resolved");
                    if(trial.Phase==RunPhase.Battle)
                    {
                        trial.ResolveEnemyTurn();Invoke("Refresh");
                        SetSize(1280,720);yield return Shot("68-ultimate-"+a+"-next-720p");SetSize(1920,1080);
                        if(a==3)Check(b.InExtraAssembly && b.Turn==1,"Extra assembly incorrectly advanced turn");
                    }
                }
            }
            finally {runField.SetValue(flow,originalRun);selectedField.SetValue(flow,originalSelected);Invoke("Refresh");}
            try
            {
                for(int a=1;a<=4;a++)
                {
                    var source=new PrototypeGameFlow(PrototypeCharacterCatalog.All,73);
                    source.OpenCharacterSelection();source.TrySelectCharacter(a<3?0:1);
                    Check(source.TryStartRun((CombatApproach)a, false),"Specialized tree start failed");
                    var trial=source.CurrentRun;runField.SetValue(flow,trial);selectedField.SetValue(flow,a<3?0:1);Invoke("Refresh");
                    var layout=new GrowthTreeLayout(trial.Growth);
                    for(int i=1;i<trial.Growth.NodeCount;i++)
                    {
                        Check(layout.Positions[i].y<layout.Positions[trial.Growth.ParentOf(i)].y,"Tree edge descends");
                        for(int j=0;j<i;j++)Check((layout.Positions[i]-layout.Positions[j]).sqrMagnitude>240*240,"Tree nodes overlap");
                    }
                    Set("growthOpen",true);Invoke("EnsureGrowthLayout",trial.Growth);Invoke("FocusGrowthNode",trial.Growth,0);
                    yield return Shot("69-tree-"+a+"-root");
                    trial.TryPurchaseGrowthNode(1);trial.TryPurchaseGrowthNode(2);Invoke("Refresh");Invoke("FocusGrowthNode",trial.Growth,3);
                    SetSize(1280,720);yield return Shot("69-tree-"+a+"-choice-720p");SetSize(1920,1080);
                    Invoke("FitGrowthTree");yield return Shot("69-tree-"+a+"-whole");
                }
            }
            finally {Set("growthOpen",false);runField.SetValue(flow,originalRun);selectedField.SetValue(flow,originalSelected);Invoke("Refresh");}
        }

        private static void WinEconomyBattle(RunGameSession run)
        {
            for(int turn=0;turn<24 && run.Phase==RunPhase.Battle;turn++)
            {
                EnemyState target=null;
                foreach(var enemy in run.CurrentBattle.Battle.Enemies) if(enemy.IsAlive) { target=enemy; break; }
                run.TryMovePlayerTo(Math.Max(.5,target.X-3),target.Y);
                foreach(string id in new[] { "frag.ellipse","frag.expand" })
                    for(int i=0;i<run.CurrentBattle.Deck.HandCount;i++)
                        if(run.CurrentBattle.Deck.GetHandCard(i).Id==id) { run.TryPlayHandCard(i,out _,out _); break; }
                run.TryUseDiagramAbility(Math.Max(.5,target.X-3),target.Y);
                Check(run.TryBeginPlot(),"Economy fixture plot rejected"); run.ResolvePlot();
                if(run.Phase==RunPhase.Battle) run.ResolveEnemyTurn();
            }
            Check(run.Phase==(run.HasExpeditionSupplies?RunPhase.Loot:RunPhase.CardReward),"Economy fixture failed to win");
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.25f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png")); captures++;
            yield return new WaitForSecondsRealtime(.15f);
        }
        private void OnGUI()
        {
            // Explicit hover presentation fixture; not evidence of OS pointer input.
            if (previewCaptureTarget < 0 || view == null || Event.current.type != EventType.Repaint) return;
            Matrix4x4 oldMatrix = GUI.matrix;
            int oldHover = Get<int>("hoveredEnemy");
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
                Set("hoveredEnemy", previewCaptureTarget);
                Invoke("DrawCombatSkillPreview");
            }
            finally { Set("hoveredEnemy", oldHover); GUI.matrix = oldMatrix; }
        }
        private static void ValidateGrowthLayout(RunGrowthState growth)
        {
            var layout = new GrowthTreeLayout(growth);
            for (int i = 0; i < growth.NodeCount; i++)
            {
                foreach (int parent in layout.Parents[i])
                    Check(layout.Positions[parent].y > layout.Positions[i].y, "A growth path must point upward: " + growth.GetNode(i).Id);
                for (int j = i + 1; j < growth.NodeCount; j++)
                    Check(Mathf.Abs(layout.Positions[i].x - layout.Positions[j].x) >= 230
                        || Mathf.Abs(layout.Positions[i].y - layout.Positions[j].y) >= 175, "Overlapping growth nodes");
            }
        }
        private IEnumerator TacticalThreatScreens()
        {
            var original=flow.CurrentRun;
            var field=typeof(PrototypeGameFlow).GetField("<CurrentRun>k__BackingField",Private);
            var character=PrototypeCharacterCatalog.All[1];
            try
            {
                for (int variant=0;variant<6;variant++)
                {
                    var definition=variant<3?DungeonGenerator.TacticalEncounter(variant,character,character.MaxHealth)
                        :new BattleDefinition(character.MaxHealth,character.MaxEnergy,new[]{new EnemyDefinition("v27.threat","공격 패턴 시험",6,0,100,6,
                            variant==3?EnemyBehaviorDefinition.RingSentinel():variant==4?EnemyBehaviorDefinition.CrossGunner():EnemyBehaviorDefinition.TwinMortar())},character.Archetype,fragments:true);
                    var trial=new RunGameSession(new[]{definition},character.CreateStartingDeckCopy(),FragmentCardCatalog.All,5,73);
                    field.SetValue(flow,trial);
                    Check(trial.TrySelectMapNode(0),"Tactical fixture entry failed"); Invoke("Refresh");
                    yield return Shot("20-threat-pair-"+variant);
                    if (variant == 0)
                    {
                        trial.TryUnravel(); trial.ResolveEnemyTurn(); Invoke("Refresh");
                        Check(trial.CurrentBattle.Battle.SealActive(trial.CurrentBattle.Battle.Enemies[1]),"Seal fixture inactive");
                        yield return Shot("20-threat-seal-active");
                        SetSize(1280,720); yield return Shot("20-threat-720p"); SetSize(1920,1080);
                    }
                }
            }
            finally { field.SetValue(flow,original); Invoke("Refresh"); }
        }

        private void ValidateKeywordInput()
        {
            Event saved=Event.current;bool enabled=GUI.enabled;
            var register=typeof(GraphaclysmModernView).GetMethod("RegisterKeyword",Private,null,
                new[]{typeof(Rect),typeof(string),typeof(string),typeof(bool),typeof(Rect)},null);
            Rect source=new Rect(650,590,130,32);
            object[] args={source,"연계 읽기","유도는 다음 끌어당김을 늘립니다. 상처는 실제 이동 시 피해를 줍니다. 보호막으로 피해를 막을 수 있습니다.",true,source};
            try
            {
                GUI.enabled=true;Invoke("CloseKeyword");
                Event.current=new Event {type=EventType.MouseMove,mousePosition=source.center};register.Invoke(view,args);
                Check(!Get<bool>("keywordPinned"),"Hover opened click-only keyword");
                Event.current=new Event {type=EventType.MouseDown,button=0,mousePosition=source.center};register.Invoke(view,args);
                Check(Get<bool>("keywordPinned") && Event.current.type==EventType.Used,"Keyword click was not consumed");
                Event.current=new Event {type=EventType.MouseMove,mousePosition=Vector2.zero};Invoke("BeginBattleHoverFrame");
                Check(Get<bool>("keywordPinned"),"Pinned keyword vanished when pointer left");
                Event.current=new Event {type=EventType.Repaint};Invoke("DrawKeywordTooltip");
                object paragraph=Get<object>("tooltipParagraph");
                var hits=(IList)paragraph.GetType().GetField("Hits").GetValue(paragraph);
                Check(hits.Count>0,"Tooltip nested keywords have no hit areas");
                Rect hit=(Rect)hits[0].GetType().GetField("Area").GetValue(hits[0]);
                Event.current=new Event {type=EventType.MouseDown,button=0,mousePosition=hit.center};Invoke("HandleKeywordInput");
                Check(Get<string>("hoveredKeywordTitle")!="연계 읽기" && Event.current.type==EventType.Used,"Nested keyword was not followed safely");
                Event.current=new Event {type=EventType.MouseDown,button=0,mousePosition=Vector2.zero};Invoke("HandleKeywordInput");
                Check(!Get<bool>("keywordPinned") && Event.current.type==EventType.Used,"Outside dismiss leaked a click");
                Event.current=new Event {type=EventType.MouseUp,button=0,mousePosition=Vector2.zero};Invoke("HandleKeywordInput");
                Check(Event.current.type==EventType.Used,"Dismiss release leaked to underlying button");
                GUI.enabled=false;Event.current=new Event {type=EventType.MouseDown,button=0,mousePosition=source.center};register.Invoke(view,args);
                Check(!Get<bool>("keywordPinned"),"Disabled background opened keyword");
                GUI.enabled=true;register.Invoke(view,args);
                Event.current=new Event {type=EventType.KeyDown,keyCode=KeyCode.Escape};Invoke("HandleKeywordInput");
                Check(!Get<bool>("keywordPinned") && Event.current.type==EventType.Used,"Esc did not close keyword before parent modal");
                ValidateKeywordPolicy();
                ValidateCatalogKeywordCoverage();
                ValidateInlineCardKeyword();
                Invoke("CloseKeyword");
                Event.current=new Event {type=EventType.MouseDown,button=0,mousePosition=source.center};register.Invoke(view,args);
            }
            finally {Event.current=saved;GUI.enabled=enabled;}
        }

        private void ValidateCatalogKeywordCoverage()
        {
            var terms=(string[])typeof(GraphaclysmModernView).GetField("GrowthTerms",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            var policy=typeof(GraphaclysmModernView).GetMethod("ShouldExplainKeyword",BindingFlags.Static|BindingFlags.NonPublic);
            Invoke("EnsureEffectStyles");
            var style=Get<GUIStyle>("effectBodyStyle");
            int cards=0,relics=0;
            Event.current=new Event {type=EventType.Repaint};
            foreach(var card in FragmentCardCatalog.All)
            {
                object visual=Invoke("Visual",card);
                var titles=(string[])visual.GetType().GetField("EffectTitles").GetValue(visual);
                var bodies=(string[])visual.GetType().GetField("EffectBodies").GetValue(visual);
                Invoke("DrawReadableEffectRows",new Rect(1200,300,545,600),visual);
                for(int i=0;i<card.AbilityCount;i++)
                {
                    Check(titles[i]=="적 적중" || titles[i]=="자신 적중","Card repeats status in heading: "+card.Id);
                    Check(!bodies[i].Contains("+0.9") && !bodies[i].Contains("피해를") && !bodies[i].Contains("회복합니다"),"Card repeats glossary mechanics: "+card.Id);
                    CheckParagraphKeywords(bodies[i],terms,policy);
                }
                cards++;
            }
            foreach(var relic in FragmentRelicCatalog.All)
            {
                Invoke("DrawExplainedText",new Rect(1210,642,525,110),relic.Description,style,true,false,null,null);
                CheckParagraphKeywords(relic.Description,terms,policy);
                Check(!relic.Description.Contains("+0.9") && !relic.Description.Contains("다음 그래프를 강화")
                    && !relic.Description.Contains("다음 타격 때 추가 피해"),"Relic repeats glossary mechanics: "+relic.Id);
                relics++;
            }
            Check(cards==105 && relics==100,"Incomplete card/relic text audit");
            // The screenshot regression: a tightly sized single line must retain its visible keyword.
            Invoke("DrawExplainedText",new Rect(1200,300,250,style.lineHeight-2),"잔불 3",style,false,false,null,null);
            CheckParagraphKeywords("잔불 3",terms,policy);
        }

        private void CheckParagraphKeywords(string text,string[] terms,MethodInfo policy)
        {
            object found=null;
            foreach(object paragraph in Get<IList>("explainedParagraphs"))
                if((string)paragraph.GetType().GetField("Text").GetValue(paragraph)==text)found=paragraph;
            Check(found!=null,"Unrendered effect paragraph: "+text);
            var hits=(IList)found.GetType().GetField("Hits").GetValue(found);
            for(int t=0;t<terms.Length;t++)
            {
                if(!text.Contains(terms[t]) || !(bool)policy.Invoke(null,new object[]{terms[t],null}))continue;
                bool linked=false;
                foreach(object hit in hits) if((int)hit.GetType().GetField("Term").GetValue(hit)==t){linked=true;break;}
                Check(linked,"Missing visible keyword "+terms[t]+" in "+text);
            }
        }

        private void ValidateKeywordPolicy()
        {
            var terms=(string[])typeof(GraphaclysmModernView).GetField("GrowthTerms",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            var meanings=(string[])typeof(GraphaclysmModernView).GetField("GrowthMeanings",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            var names=(string[])typeof(GraphaclysmModernView).GetField("AbilityNames",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            var policy=typeof(GraphaclysmModernView).GetMethod("ShouldExplainKeyword",BindingFlags.Static|BindingFlags.NonPublic);
            Check(terms.Length==meanings.Length,"Glossary definitions out of sync");
            for(int i=0;i<terms.Length;i++)
            {
                Check(meanings[i].Length<=85,"Glossary meaning is too long: "+terms[i]);
                Check(!(bool)policy.Invoke(null,new object[]{terms[i],terms[i]}),"Self keyword linked: "+terms[i]);
            }
            Check(!(bool)policy.Invoke(null,new object[]{"회복",null}),"Ordinary recovery still linked");
            Check(!(bool)policy.Invoke(null,new object[]{"파열","균열"}),"Same status alias still linked");
            Check(!(bool)policy.Invoke(null,new object[]{"관통","방패 관통"}),"Long status name links itself");
            for(int i=0;i<names.Length;i++)
            {
                if(i==12)continue; // Plain healing is deliberately not a glossary link.
                bool found=false;
                for(int j=0;j<terms.Length;j++)
                    if(names[i].Contains(terms[j]) && (bool)policy.Invoke(null,new object[]{terms[j],null})) {found=true;break;}
                Check(found,"Ability keyword has no linked explanation: "+names[i]);
            }
            Invoke("CloseKeyword");
            Invoke("OpenKeyword",new Rect(700,400,100,30),"파열","파열 · 균열 · 회복 · 잔불",false,new Rect(700,400,100,30));
            Event.current=new Event {type=EventType.Repaint};Invoke("DrawKeywordTooltip");
            object paragraph=Get<object>("tooltipParagraph");
            var hits=(IList)paragraph.GetType().GetField("Hits").GetValue(paragraph);
            Check(hits.Count>0,"Related debuff was removed from explanation");
            foreach(object hit in hits)
                Check(terms[(int)hit.GetType().GetField("Term").GetValue(hit)]=="잔불","Self/ordinary word has an active underline");
            Invoke("CloseKeyword");
        }

        private void ValidateInlineCardKeyword()
        {
            CardDefinition card=null;int abilityIndex=-1;
            foreach(var candidate in FragmentCardCatalog.All)
            {
                for(int i=0;i<candidate.AbilityCount;i++)
                    if(candidate.GetAbility(i).Kind==CardAbilityKind.Weaken) {card=candidate;abilityIndex=i;break;}
                if(card!=null)break;
            }
            Check(card!=null,"Missing weaken card for inline keyword test");
            object visual=Invoke("Visual",card);
            Rect owner=new Rect(630,400,340,420), rows=new Rect(648,663,304,104);
            Event.current=new Event {type=EventType.Repaint};Invoke("DrawReadableCardBadges",rows,visual,owner);
            string expected=((string[])visual.GetType().GetField("EffectBadges").GetValue(visual))[abilityIndex];
            var paragraphs=Get<IList>("explainedParagraphs");Rect keyword=default(Rect);
            var terms=(string[])typeof(GraphaclysmModernView).GetField("GrowthTerms",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            foreach(object paragraph in paragraphs)
            {
                if((string)paragraph.GetType().GetField("Text").GetValue(paragraph)!=expected)continue;
                foreach(object hit in (IList)paragraph.GetType().GetField("Hits").GetValue(paragraph))
                    if(terms[(int)hit.GetType().GetField("Term").GetValue(hit)]=="약화")
                    {keyword=(Rect)hit.GetType().GetField("Area").GetValue(hit);break;}
            }
            Check(keyword.width>0 && keyword.x>rows.x+30,"Inline ability word has no precise underline hit area");
            Invoke("CloseKeyword");
            Event.current=new Event {type=EventType.MouseDown,button=0,mousePosition=new Vector2(rows.x+12,rows.y+abilityIndex*54+12)};
            Invoke("DrawReadableCardBadges",rows,visual,owner);
            Check(!Get<bool>("keywordPinned"),"Whole effect row still opens instead of just keyword");
            Event.current=new Event {type=EventType.MouseDown,button=0,mousePosition=keyword.center};
            Invoke("DrawReadableCardBadges",rows,visual,owner);
            Check(Get<bool>("keywordPinned") && Get<string>("hoveredKeywordTitle")=="약화"
                && Get<bool>("keywordKeepsCard") && Event.current.type==EventType.Used,"Inline weaken word did not open and retain card safely");
        }

        private void ValidateHandInput()
        {
            int count = flow.CurrentRun.CurrentBattle.Deck.HandCount;
            var pick = typeof(GraphaclysmModernView).GetMethod("PickHandCard", Private);
            var rectMethod = typeof(GraphaclysmModernView).GetMethod("HandCardRect", Private);
            Rect front = (Rect)rectMethod.Invoke(view, new object[] { 0, count, true });
            Rect behind = (Rect)rectMethod.Invoke(view, new object[] { 1, count, false });
            Vector2 overlap = new Vector2(front.xMax - 12, Mathf.Max(front.yMin, behind.yMin) + 35);
            Check(front.Contains(overlap), "Missing overlap fixture");
            int selected = (int)pick.Invoke(view, new object[] { overlap, count, 0 });
            Check(selected == 0, "Lifted front card lost input to a card behind it");
            Rect inspection = new Rect(620, 476, 310, 370);
            Set("handInspectionRect", inspection);
            Check((int)pick.Invoke(view, new object[] { inspection.center, count, 0 }) == 0,
                "Inspected card did not own its click");
            string expected = flow.CurrentRun.CurrentBattle.Deck.GetHandCard(selected).Id;
            int before = flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount;
            Invoke("HandleHandCardInput", new Event { type = EventType.MouseDown, button = 0 }, selected, count);
            Check(flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount == before, "Mouse down played prematurely");
            Invoke("HandleHandCardInput", new Event { type = EventType.MouseUp, button = 0 }, selected, count);
            Check(flow.CurrentRun.CurrentBattle.Battle.GetPlayedCard(before).Id == expected, "Click played the wrong card");
            Invoke("Undo");
            Invoke("HandleHandCardInput", new Event { type = EventType.MouseDown, button = 0 }, 0, count);
            Invoke("HandleHandCardInput", new Event { type = EventType.MouseUp, button = 0 }, 1, count);
            Check(flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount == before, "Drag release played another card");
            Set("handInspectionRect", default(Rect));
        }

        private void ValidateGrowthInput()
        {
            Event input = null;
            int acquired = flow.CurrentRun.Growth.AcquiredCount;
            Rect viewport = new Rect(42, 142, 1380, 870);
            Invoke("FocusGrowthNode", flow.CurrentRun.Growth, flow.CurrentRun.Growth.IndexOf("common.condense.07"));
            Vector2 before = Get<Vector2>("growthPan");
            float zoom = Get<float>("growthZoom");
            try
            {
                input = new Event { type = EventType.ScrollWheel, mousePosition = viewport.center, delta = new Vector2(0, -2) };
                Invoke("HandleGrowthCanvasInput", viewport, input);
                Check(Get<Vector2>("growthPan").y < before.y && Get<float>("growthZoom") == zoom, "Wheel must scroll upward without zooming");
                input = new Event { type = EventType.ScrollWheel, mousePosition = viewport.center, delta = new Vector2(0, -2), modifiers = EventModifiers.Control };
                Invoke("HandleGrowthCanvasInput", viewport, input);
                Check(Get<float>("growthZoom") > zoom, "Ctrl-wheel must zoom");
                input = new Event { type = EventType.MouseDown, button = 0, mousePosition = viewport.center };
                Invoke("HandleGrowthCanvasInput", viewport, input);
                before = Get<Vector2>("growthPan");
                input = new Event { type = EventType.MouseDrag, button = 0, mousePosition = viewport.center, delta = new Vector2(30, 20) };
                Invoke("HandleGrowthCanvasInput", viewport, input);
                Check(Get<Vector2>("growthPan").x < before.x, "Drag must pan");
                input = new Event { type = EventType.MouseUp, button = 0, mousePosition = viewport.center };
                Invoke("HandleGrowthCanvasInput", viewport, input);
                Check(!Get<bool>("growthDragging"), "Drag release must clear capture");
                Check(flow.CurrentRun.Growth.AcquiredCount == acquired, "Navigation must never spend growth points");
            }
            finally { input = null; }
        }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static bool TryAnyMove(RunGameSession run)
            => run.TryMovePlayer(-1.5, 0) || run.TryMovePlayer(1.5, 0)
                || run.TryMovePlayer(0, 1.5) || run.TryMovePlayer(0, -1.5);
        private void Set(string name, object value) => typeof(GraphaclysmModernView).GetField(name, Private).SetValue(view, value);
        private T Get<T>(string name) => (T)typeof(GraphaclysmModernView).GetField(name, Private).GetValue(view);
        private object Invoke(string name, params object[] arguments) => typeof(GraphaclysmModernView).GetMethod(name, Private).Invoke(view, arguments);
        private static void SetSize(int w, int h) => typeof(VisualV3SmokeRunner).GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { w, h });
    }
}
