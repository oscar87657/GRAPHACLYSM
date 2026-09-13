using Graphaclysm.Application;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private void DrawBackdrop()
        {
            Fill(new Rect(0, 0, 1920, 1080), Paper);
            if (backdrop != null) GUI.DrawTexture(new Rect(0, 0, 1920, 1080), backdrop, ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 0, 1920, 1080), new Color(Paper.r, Paper.g, Paper.b, (castActive || (run != null && run.Phase == RunPhase.Battle)) ? 0.91f : flow.Phase == GameFlowPhase.MainMenu ? 0.12f : 0.38f));
            Fill(new Rect(1537, 64, 251, 51), new Color(Paper.r, Paper.g, Paper.b, 0.75f));
            Line(new Vector2(54, 54), new Vector2(1866, 54), new Color(Gold.r, Gold.g, Gold.b, 0.35f));
            Line(new Vector2(54, 1057), new Vector2(1866, 1057), new Color(Gold.r, Gold.g, Gold.b, 0.35f));
            Diamond(new Vector2(54, 54), 4, Gold); Diamond(new Vector2(1866, 1057), 4, Gold);
        }

        private void Header(string title, string right)
        {
            Label(new Rect(90, 65, 900, 48), title, ui.Body);
            Label(new Rect(1500, 65, 200, 48), right, ui.Small);
            if (ui.Button(new Rect(1710, 68, 74, 39), run == null ? "설정" : "메뉴")) { if (run == null) OpenSettings(); else paused = true; }
            if (ui.Button(new Rect(1800, 68, 45, 39), "?")) OpenHelp();
        }

        private static void DrawPortrait(Rect r, Texture2D portrait)
        {
            if (portrait != null) GUI.DrawTexture(r, portrait, ScaleMode.ScaleToFit, true);
        }

        private void DrawTitle()
        {
            Header("A RECORD OF LIGHT AND EQUATIONS", "FIRST ARCHIVE");
            Diamond(new Vector2(1408, 507), 351, new Color(Gold.r, Gold.g, Gold.b, 0.37f));
            Diamond(new Vector2(1408, 507), 372, new Color(Violet.r, Violet.g, Violet.b, 0.17f));
            Ring(new Vector2(1408,507),192,new Color(Violet.r,Violet.g,Violet.b,.27f),1.2f);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;Vector2 p=new Vector2(1408,507)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*192;
                Diamond(p,7,new Color(Gold.r,Gold.g,Gold.b,.55f));
            }
            Label(new Rect(128, 303, 910, 145), "GRAPHACLYSM", ui.Logo);
            Line(new Vector2(137, 468), new Vector2(805, 468), Gold);
            Label(new Rect(137, 490, 790, 70), "The Unwritten Axis", ui.Display);
            Label(new Rect(140, 588, 620, 45), "흩어진 빛을 잇고, 아직 없는 길을 그린다.", ui.Body);
            if (ui.Button(new Rect(140, 650, 350, 64), "이어하기", true, HasContinue)) ContinueSavedRun();
            Label(new Rect(515, 650, 610, 64), savedSummary, ui.Small);
            if (ui.Button(new Rect(140, 738, 350, 70), "새 기록 시작", !HasContinue)) RequestNewRun();
            if (ui.Button(new Rect(140, 827, 350, 52), "카드 사전")) {codexOpen=true;codexRelics=false;ChangeCodex(0,0);}
            if (ui.Button(new Rect(510, 827, 260, 52), "작도 안내")) OpenHelp();
            if (ui.Button(new Rect(790, 827, 300, 52), "영구 기록 · 잔광 " + legacy.Currency)) legacyOpen = true;
            if (ui.Button(new Rect(140, 901, 350, 52), "설정")) OpenSettings();
            if (ui.Button(new Rect(510, 901, 260, 52), "게임 종료")) confirmation = Confirmation.Quit;
            Label(new Rect(510, 971, 1120, 65), saveNotice, ui.Small);
            Fill(new Rect(127, 972, 351, 44), new Color(Paper.r, Paper.g, Paper.b, 0.86f));
            Label(new Rect(140, 978, 330, 32), "수식으로 엮는 카드 전투", ui.Small);
            DrawApproachEntry();
        }

        private void DrawCharacters()
        {
            if(squadSelectionOpen){DrawSquadSelection();return;}
            Vector2 left = new Vector2(610, 477), right = new Vector2(1310, 477);
            float pointerLeft = Vector2.Distance(Event.current.mousePosition, left);
            float pointerRight = Vector2.Distance(Event.current.mousePosition, right);
            hoveredCharacter = pointerLeft <= 260 ? 0 : pointerRight <= 260 ? 1 : -1;
#if UNITY_EDITOR
            if (DiagnosticHoveredCharacter == -2) hoveredCharacter = -1;
            else if (DiagnosticHoveredCharacter >= 0 && DiagnosticHoveredCharacter < 2) hoveredCharacter = DiagnosticHoveredCharacter;
#endif
            int preview = hoveredCharacter >= 0 ? hoveredCharacter : flow.SelectedCharacterIndex;
            bool ian = preview == 0;
            var previewCharacter = flow.GetCharacter(preview);
            string previewStats = "체력 " + previewCharacter.MaxHealth + "     시작 손패 6 · 보존 한도 8";
            Header("CHOOSE YOUR TRACE", "01 / TRAVELER");
            Label(new Rect(500, 118, 920, 54), "두 기록 사이에서 한 사람을 선택하세요", ui.PageTitle, true);
            DrawCharacterMedallion(0, left, ianMedallionClosed, ianMedallionOpen);
            DrawCharacterMedallion(1, right, lunaMedallionClosed, lunaMedallionOpen);

            Fill(new Rect(889, 215, 142, 525), new Color(Paper.r, Paper.g, Paper.b, .88f));
            Diamond(new Vector2(960, 477), 265, new Color(Gold.r, Gold.g, Gold.b, .66f), 2.2f);
            Diamond(new Vector2(960, 477), 224, new Color(Violet.r, Violet.g, Violet.b, .48f), 1.4f);
            Ring(new Vector2(960, 477), 72, new Color(Gold.r, Gold.g, Gold.b, .52f), 1.5f);
            Label(new Rect(890, 440, 140, 74), "THE\nGATE", ui.Small, true);

            if (GUI.Button(new Rect(left.x - 280, left.y - 280, 560, 560), GUIContent.none, GUIStyle.none))
            { flow.TrySelectCharacter(0); Refresh(); }
            if (GUI.Button(new Rect(right.x - 280, right.y - 280, 560, 560), GUIContent.none, GUIStyle.none))
            { flow.TrySelectCharacter(1); Refresh(); }

            Fill(new Rect(350, 770, 1220, 174), new Color(Paper.r, Paper.g, Paper.b, .9f));
            Border(new Rect(350, 770, 1220, 174), flow.SelectedCharacterIndex == preview ? Violet : Gold, 18);
            Label(new Rect(385, 789, 250, 48), ian ? "이안" : "루나", ui.PageTitle);
            Label(new Rect(650, 790, 450, 42), ian ? "흑유리의 기록자" : "천문 도구의 조율자", ui.Heading);
            Label(new Rect(385, 842, 710, 67), ian ? "깨진 유리에도, 지나간 빛은 남아 있어." : "어긋난 회전을, 새로운 궤적으로 잇는다.", ui.Body);
            Label(new Rect(1122, 790, 410, 43), previewStats, ui.Small);
            Label(new Rect(1122, 838, 410, 72), ian ? "흑유리 개방 · 피해 +6 / 이동 봉쇄" : "백야의 포옹 · 정화 / 보호막 / 회복", ui.Small);
            if (ui.Button(new Rect(143, 966, 220, 51), "돌아가기")) { flow.ReturnToMainMenu(); Refresh(); }
            if(ui.Button(new Rect(1420,968,360,62),ExpeditionSquads.Names[(int)selectedSquad]))squadSelectionOpen=true;
            if (flow.SelectedCharacterIndex == 0)
            {
                if (ui.Button(new Rect(500, 968, 420, 62), "집행 이안으로 원정", true)) { flow.TryStartRun(CombatApproach.Execution); Refresh(); }
                if (ui.Button(new Rect(950, 968, 420, 62), "기록 이안으로 원정")) { flow.TryStartRun(CombatApproach.Recording); Refresh(); }
            }
            else
            {
                if (ui.Button(new Rect(500, 968, 420, 62), "조율 루나로 원정", true)) { flow.TryStartRun(CombatApproach.Tuning); Refresh(); }
                if (ui.Button(new Rect(950, 968, 420, 62), "관측 루나로 원정")) { flow.TryStartRun(CombatApproach.Observation); Refresh(); }
            }
        }

        private Texture2D RelicTexture(RelicDefinition relic)
        {
            if (relic == null || relicArt == null) return null;
            for (int i = 0; i < FragmentRelicCatalog.All.Count; i++)
                if (FragmentRelicCatalog.All[i].Id == relic.Id) return relicArt[i];
            return null;
        }

        private void DrawRelicArt(Rect area, RelicDefinition relic)
        {
            if (relic == null) return;
            Texture2D texture = RelicTexture(relic);
            if (texture != null) GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
            else
            {
                DrawRelicEmblem(area,relic);
            }
        }

        // Existing vector vocabulary; no duplicated bitmap assets or per-frame geometry arrays.
        private static void DrawRelicEmblem(Rect area, RelicDefinition relic)
        {
            Vector2 p=area.center; float s=Mathf.Min(area.width,area.height)*.34f;
            Ring(p,s,Gold,1.2f); Ring(p,s*.85f,new Color(Gold.r,Gold.g,Gold.b,.35f),1);
            switch(relic.Effect)
            {
                case RelicEffectKind.Conditional:
                    int points=3+(int)relic.Trigger%7;
                    for(int i=0;i<points;i++)
                    {
                        float angle=(i+(int)relic.Reward*.08f)*Mathf.PI*2/points;
                        Vector2 tip=p+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*s*.75f;
                        Line(p,tip,Violet,2);Diamond(tip,s*.1f,Gold,1);
                    }
                    DrawStatusIcon(p,RelicArchive.RewardStatus(relic.Reward),(int)relic.Reward>=8?Threat:Violet);
                    break;
                case RelicEffectKind.SoloAnchor:
                    Diamond(p+Vector2.up*s*.38f,s*.22f,Violet,3);
                    Line(p+Vector2.up*s*.2f,p-Vector2.up*s*.65f,Violet,4);
                    Line(p-Vector2.right*s*.45f,p+Vector2.right*s*.45f,Gold,3); break;
                case RelicEffectKind.SharedResonance:
                    Ring(p-Vector2.right*s*.3f,s*.4f,Violet,3);
                    Ring(p+Vector2.right*s*.3f,s*.4f,Violet,3);
                    Diamond(p,s*.18f,Gold,2); break;
                case RelicEffectKind.CondenseRupture:
                    Border(new Rect(p.x-s*.42f,p.y-s*.48f,s*.84f,s),Violet,9);
                    Line(p-Vector2.up*s*.7f-Vector2.right*s*.2f,p-Vector2.up*s*.7f+Vector2.right*s*.2f,Gold,4);
                    Diamond(p,s*.25f,Gold,3); break;
                case RelicEffectKind.UnravelShield:
                    Ring(p,s*.45f,Violet,3); Ring(p,s*.22f,Gold,2);
                    Line(p+Vector2.right*s*.45f,p+new Vector2(s*.8f,s*.6f),Violet,3); break;
                default:
                    int variant=(int)relic.Effect%4;
                    if(variant==0) { Diamond(p,s*.62f,Violet,3); Line(p-Vector2.up*s*.75f,p+Vector2.up*s*.75f,Gold,2); }
                    else if(variant==1) { Ring(p,s*.5f,Violet,3); Line(p-Vector2.one*s*.5f,p+Vector2.one*s*.5f,Gold,3); }
                    else if(variant==2) { Diamond(p-Vector2.right*s*.2f,s*.5f,Violet,2); Diamond(p+Vector2.right*s*.2f,s*.5f,Gold,2); }
                    else { Line(p-Vector2.right*s*.6f,p+Vector2.up*s*.6f,Violet,3); Line(p+Vector2.up*s*.6f,p+Vector2.right*s*.6f,Violet,3); Ring(p,s*.2f,Gold,3); }
                    break;
            }
        }

        private void DrawCharacterMedallion(int index, Vector2 center, Texture2D closed, Texture2D open)
        {
            float eyeOpen = characterEyeOpen[index];
            const float radius = 270;
            Rect image = new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2);
            Color saved = GUI.color;
            GUI.color = new Color(1, 1, 1, 1 - eyeOpen);
            if (closed != null) GUI.DrawTexture(image, closed, ScaleMode.StretchToFill, true);
            GUI.color = new Color(1, 1, 1, eyeOpen);
            if (open != null) GUI.DrawTexture(image, open, ScaleMode.StretchToFill, true);
            GUI.color = saved;
            Color ring = new Color(Gold.r, Gold.g, Gold.b, .62f);
            Ring(center, radius + 5, ring, 1.5f);
            Ring(center, radius + 14, new Color(ring.r, ring.g, ring.b, .32f), 1.1f, .83f, index == 0 ? 2.6f : -.5f);
            Rect tag = new Rect(center.x - 100, center.y + radius - 70, 200, 55);
            Fill(tag, new Color(Paper.r, Paper.g, Paper.b, .84f));
            Border(tag, ring, 8);
            Label(new Rect(tag.x + 8, tag.y + 2, tag.width - 16, 29), index == 0 ? "이안" : "루나", ui.Small, true);
            if (flow.SelectedCharacterIndex == index)
                Label(new Rect(tag.x + 8, tag.y + 28, tag.width - 16, 23), "선택된 기록", ui.Small, true);
        }

        private Texture2D towerBackdrop;
        private Vector2 MapPoint(RunMapNodeDefinition node)
            => new Vector2(TowerViewport.center.x+(node.Lane-(run.Map.Definition.LaneCount-1)*.5f)*230,
                TowerViewport.y+TowerLayerY(node.Layer)-towerMapScroll);

        private void DrawMap()
        {
            EnsureTowerMap();HandleTowerMapPan(Event.current);
            if (towerBackdrop == null) towerBackdrop = Resources.Load<Texture2D>("Art/Generated/tower-ascent-v18");
            Fill(new Rect(0,0,1920,1080),Ink);
            DrawContinuousTowerBackdrop();
            Fill(new Rect(64,140,510,820),new Color(.04f,.035f,.07f,.78f));
            Label(new Rect(94,173,440,90),floorTitle,ui.PageTitleLight);
            Label(new Rect(96,283,440,95),string.IsNullOrEmpty(run.OpeningStepHint)
                ? "위로 이어지는 기록의 탑\n빛나는 방을 골라 올라가세요." : run.OpeningStepHint,ui.Light);
            Label(new Rect(96,389,440,95),run.HasRewardPacks?run.MapPreparationText:"숙련 " + run.MasteryRank + " · 그래프 피해 +" + (run.MasteryRank+run.TrainingPower)
                + "\n시작 보호막 +" + (run.MasteryRank+run.PreparedShield)
                + "\n성장점 5점 투자마다 숙련 상승 (최대 5)",ui.SmallLight);
            DrawPortrait(new Rect(140,496,285,330),flow.CurrentCharacter.Archetype == CombatArchetype.Ian ? ianPortrait : lunaPortrait);
            Label(new Rect(96,858,440,64),roomResourceText,ui.SmallLight);
            if (run.HasEconomy)
            {
                Label(new Rect(96,824,440,32),run.HasExpeditionSupplies?run.SupplyInventoryText:"은화 " + run.Coins + " · 연구권 " + run.ResearchTickets,ui.SmallLight);
                if (!run.HasRewardPacks && ui.Button(new Rect(96,933,440,46),"연구권 1장 → 카드 선택",false,run.CanUseResearch))
                { run.TryUseResearch(); Refresh(); return; }
            }
            Label(new Rect(80,45,1200,52),"THE ASCENDING ARCHIVE",ui.HeadingLight);
            Label(new Rect(625,78,810,42),"드래그·휠로 탑 둘러보기 · 빛나는 방을 클릭하여 이동",ui.SmallLight);
            if(ui.Button(new Rect(1480,170,330,50),"현재 위치로",true))FocusTowerCurrent();
            if(ui.Button(new Rect(1480,235,155,46),"꼭대기",true))towerMapScroll=0;
            if(ui.Button(new Rect(1650,235,160,46),"입구",true))towerMapScroll=TowerMaximumScroll;
            if(ui.Button(new Rect(1700,48,130,46),"메뉴",true)) paused=true;
            var map = run.Map;
            for (int i = 0; i < map.Definition.NodeCount; i++)
            {
                var node = map.Definition.GetNode(i); Vector2 p = MapPoint(node);
                for (int e = 0; e < node.NextNodeCount; e++)
                {
                    int target = node.GetNextNodeIndex(e);
                    bool near = map.IsAvailable(i) || (map.IsCompleted(i) && map.IsAvailable(target));
                    DrawTowerLine(p, MapPoint(map.Definition.GetNode(target)), near ? new Color(.84f,.7f,1) : new Color(.65f,.6f,.75f,.44f), near ? 3f : 1.5f);
                }
            }
            DrawTowerFloorBanners();
            for (int i = 0; i < map.Definition.NodeCount; i++)
            {
                var node = map.Definition.GetNode(i); Vector2 p = MapPoint(node);
                if(p.y-40<TowerViewport.yMin || p.y+62>TowerViewport.yMax)continue;
                bool active = map.IsAvailable(i), complete = map.IsCompleted(i);
                Rect hit = new Rect(p.x-72,p.y-36,144,91);
                bool hover = !towerMapDragging && TowerViewport.Contains(Event.current.mousePosition) && hit.Contains(Event.current.mousePosition);
                Disc(p, 30, new Color(.065f,.05f,.11f,.97f));
                Ring(p,32,active ? Gold : complete ? Violet : Muted,active ? 3 : 1);
                DrawRoomGlyph(p, 22, node.Kind, active ? Paper : complete ? Violet : new Color(.65f,.61f,.72f));
                if (active) Ring(p, 38, new Color(.84f,.7f,1), 1.7f);
                Fill(new Rect(p.x-72,p.y+35,144,25),new Color(.04f,.03f,.07f,.94f));
                Label(new Rect(p.x-72,p.y+35,144,25),complete ? "✓ 기록 완료"
                    : towerMapLabels[i],ui.SmallLight,true);
                if (hover)
                {
                    Fill(new Rect(1460,420,350,235),new Color(.05f,.04f,.09f,.95f));
                    Label(new Rect(1480,440,310,60),node.DisplayName,ui.HeadingLight);
                    Label(new Rect(1480,515,310,115),towerMapHints[i],ui.Light);
                    Label(new Rect(1480,666,310,45),active?"선택하여 다음 방으로":complete?"지나온 기록":"연결된 길로 접근",ui.SmallLight);
                }
                if (active && !towerMapDragging && GUI.Button(hit, GUIContent.none, GUIStyle.none))
                { run.TrySelectMapNode(i); message = ""; GUI.FocusControl(null); Refresh(); break; }
            }
            towerMapScroll=GUI.VerticalScrollbar(new Rect(1422,130,20,840),towerMapScroll,TowerViewport.height,0,towerMapHeight);
            Label(new Rect(620, 1015, 800, 42), run.RoomResult, ui.SmallLight, true);
            if (ui.Button(new Rect(1490, 930, 320, 62), "원정 성장   G", true)) growthOpen = true;
        }

        private static string MapRewardHint(RunNodeKind kind)
        {
            switch(kind)
            {
                case RunNodeKind.Battle: return "일반 전투 · 카드 보상";
                case RunNodeKind.Shop: return "은화로 카드·유물·회복·성장 구매";
                case RunNodeKind.Elite: return "강한 적 · 승리하면 유물 선택";
                case RunNodeKind.Boss: return "층 보스 · 유물과 다음 층";
                case RunNodeKind.Workshop: return "원정 피해 강화 또는 카드 제거";
                case RunNodeKind.Observatory: return "체력을 내고 성장점 / 공명";
                case RunNodeKind.Supply: return "다음 전투 보호막 / 회복 / 카드";
                case RunNodeKind.Rest: return "회복 / 공명 / 카드 제거";
                case RunNodeKind.Treasure: return "유물 선택 또는 회복";
                default: return "사건의 선택과 대가";
            }
        }

        private void DrawRewardsOrResult()
        {
            if (run.IsPractice) { DrawApproachResult(); return; }
            if (run.Phase==RunPhase.Loot) { DrawExpeditionLoot(); return; }
            bool cardReward = run.Phase == RunPhase.CardReward, relicReward = run.Phase == RunPhase.RelicReward;
            Header("AFTER THE LIGHT", deckText);
            if (cardReward || relicReward)
            {
                Label(new Rect(142, 159, 1500, 95), run.IsStartingRelic?"여행에 가져갈 첫 유물":cardReward ? "새로운 선을 기억하다" : "빛이 머문 물건", ui.PageTitle);
                Label(new Rect(150, 270, 1400, 45), cardReward ? "지금 덱에 추가할 카드 한 장을 고르세요." : "이번 여정에 남을 유물을 하나 고르세요.", ui.Body);
                if(cardReward && run.ActiveDraftGrade>=0)
                {
                    Label(new Rect(150,314,1570,60),Graphaclysm.Core.Cards.FragmentDraft.Chances(run.ActiveDraftGrade),ui.Small);
                    Fill(new Rect(150,302,1570,3),Rarity((Graphaclysm.Core.Cards.CardRarity)run.ActiveDraftGrade));
                }
                else if (run.HasOpeningRoute)
                    Label(new Rect(150, 324, 1570, 36), run.HasEconomy
                        ? (string.IsNullOrEmpty(run.LastLootText) ? "방 완료" : run.LastLootText)
                            + (run.IsResearchReward || run.IsStartingRelic ? "" : " · 성장점 +" + run.LastExplorationPoints)
                        : "방 완료 · 성장점 +" + run.LastExplorationPoints + " / 사용 가능 " + run.Growth.Points + "점 · 보상 선택 후 지도에서 G로 성장", ui.Small);
                for (int i = 0; i < 3; i++)
                {
                    Rect r = new Rect(411 + i * 383, 377, 330, 419); bool hover = r.Contains(Event.current.mousePosition);
#if UNITY_EDITOR
                    hover |= DiagnosticRewardHover == i;
#endif
                    if (cardReward)
                    {
                        var card = run.RewardOptions[i]; if (card == null) continue;
                        var visual = Visual(card); if (visual == null) continue;
                        DrawSkillCard(r, visual, hover, true);
                        Fill(new Rect(r.x+25,816,r.width-50,4),Rarity(visual.Card.DiagramRarity));
                        if (hover)
                        {
                            Fill(new Rect(95,395,295,526),new Color(Paper.r,Paper.g,Paper.b,.98f));
                            Label(new Rect(111,411,263,52),card.DisplayName,ui.Heading);
                            DrawReadableCardDetails(new Rect(111,478,263,420),visual);
                        }
                    }
                    else
                    {
                        var relic = run.RelicRewardOptions[i]; if (relic == null) continue;
                        Fill(r, new Color(1, 1, 1, 0.5f)); Border(r, hover ? Violet : Gold);
                        DrawRelicArt(new Rect(r.x + 48, r.y + 27, r.width - 96, 202), relic);
                        Label(new Rect(r.x + 25, r.y + 234, r.width - 50, 60), relic.DisplayName, ui.Heading, true);
                        DrawExplainedText(new Rect(r.x + 25, r.y + 310, r.width - 50, 79), relic.Description, ui.Body, true);
                    }
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                    { if (cardReward) run.TrySelectReward(i); else run.TrySelectRelicReward(i); Refresh(); break; }
                }
                if (ui.Button(new Rect(799, 971, 322, 55), run.IsStartingRelic?"유물 없이 출발":cardReward?"카드 받지 않기":"유물 받지 않기")) { run.TrySkipReward(); Refresh(); }
                return;
            }
            bool victory = run.Phase == RunPhase.Completed;
            DrawPortrait(new Rect(1110, 210, 500, 760), flow.CurrentCharacter.Archetype == CombatArchetype.Ian ? ianPortrait : lunaPortrait);
            Label(new Rect(145, 264, 1000, 118), victory ? "A Trace Remains" : "Until the Next Light", ui.Display);
            Label(new Rect(150, 423, 840, 55), victory ? "모든 기록을 이었습니다." : "이번 궤적은 여기서 멈춥니다.", ui.Heading);
            Label(new Rect(150, 512, 730, 75), victory ? "흩어진 빛은, 이제 하나의 길이 되었다." : "그리지 못한 선은 다음 만남을 기다린다.", ui.Body);
            Label(new Rect(150, 612, 730, 42), "영구 기록 잔광 " + legacy.Currency + " · 메인 화면에서 사용할 수 있습니다.", ui.Small);
            if (ui.Button(new Rect(150, 715, 350, 68), "다시 시작", true)) { flow.TryRestartRun(); Refresh(); }
            if (ui.Button(new Rect(150, 804, 350, 55), "다른 여행자 선택")) { flow.ReturnToCharacterSelection(); Refresh(); }
            if (ui.Button(new Rect(150, 878, 350, 55), "처음으로")) { flow.ReturnToMainMenu(); Refresh(); }
        }

        private void DrawHelp()
        {
            ModalPanel(HelpTitles[helpPage]);
            Label(new Rect(485, 302, 936, 405), HelpBodies[helpPage], ui.Body);
            if (ui.Button(new Rect(480, 791, 240, 60), "이전", enabled: helpPage > 0)) helpPage--;
            if (ui.Button(new Rect(748, 791, 290, 60), "안내 닫기   Esc")) CloseHelp();
            if (ui.Button(new Rect(1066, 791, 352, 60), helpPage == HelpTitles.Length - 1 ? "시작하기" : "다음", true))
            { if (helpPage == HelpTitles.Length - 1) CloseHelp(); else helpPage++; }
        }
    }
}
