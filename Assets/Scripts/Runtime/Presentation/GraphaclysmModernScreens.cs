using Graphaclysm.Application;
using Graphaclysm.Core.Combat;
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
            Label(new Rect(1550, 65, 230, 48), right, ui.Small);
            if (ui.Button(new Rect(1800, 68, 45, 39), "?")) helpOpen = !helpOpen;
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
            if (ui.Button(new Rect(140, 738, 350, 70), "기록 시작", true)) { flow.OpenCharacterSelection(); Refresh(); }
            if (ui.Button(new Rect(140, 827, 350, 52), "카드 사전")) {codexOpen=true;codexRelics=false;ChangeCodex(0,0);}
            if (ui.Button(new Rect(510, 827, 260, 52), "작도 안내")) helpOpen = true;
            Fill(new Rect(127, 972, 351, 44), new Color(Paper.r, Paper.g, Paper.b, 0.86f));
            Label(new Rect(140, 978, 330, 32), "수식으로 엮는 카드 전투", ui.Small);
        }

        private void DrawCharacters()
        {
            bool ian = flow.SelectedCharacterIndex == 0;
            Header("CHOOSE YOUR TRACE", "01 / TRAVELER");
            Label(new Rect(146, 177, 720, 75), "빛을 기억하는 사람", ui.PageTitle);
            Label(new Rect(152, 300, 700, 62), ian ? "이안" : "루나", ui.Heading);
            Label(new Rect(152, 370, 680, 92), ian ? "깨진 유리에도, 지나간 빛은 남아 있어." : "틀린 회전은 없어. 아직 맞물리지 않았을 뿐.", ui.Body);
            Label(new Rect(152, 502, 680, 45), characterStats, ui.Body);
            Line(new Vector2(152, 568), new Vector2(758, 568), Gold);
            Label(new Rect(152, 599, 670, 40), ian ? "흑유리 개방" : "백야의 포옹", ui.Heading);
            Label(new Rect(152, 649, 650, 86), ian ? "공명 6  /  다음 작도 피해 +6\n적중한 적의 이동을 봉쇄한다." : "공명 6  /  자신이 선에 닿는 범위 확대\n자가 적중 시 정화 · 보호막 8 · 회복 5", ui.Body);
            if (ui.Button(new Rect(150, 805, 270, 66), "이안", ian)) { flow.TrySelectCharacter(0); Refresh(); }
            if (ui.Button(new Rect(441, 805, 270, 66), "루나", !ian)) { flow.TrySelectCharacter(1); Refresh(); }
            Diamond(new Vector2(1344, 514), 329, new Color(Gold.r, Gold.g, Gold.b, 0.43f));
            DrawPortrait(ian ? new Rect(990,136,698,840) : new Rect(1020,142,650,780), ian ? ianPortrait : lunaPortrait);
            if (ui.Button(new Rect(143, 966, 220, 51), "돌아가기")) { flow.ReturnToMainMenu(); Refresh(); }
            if (ui.Button(new Rect(1450, 936, 345, 76), "이 여정 시작", true)) { flow.TryStartRun(); Refresh(); }
        }

        private Vector2 MapPoint(RunMapNodeDefinition node)
            => new Vector2(554 + (node.Layer % run.Map.Definition.RoomsPerFloor) * (1170f / Mathf.Max(1, run.Map.Definition.RoomsPerFloor - 1)), 420 + node.Lane * 184);

        private void DrawMap()
        {
            Header("THE CHANGING ATLAS", seedText);
            Label(new Rect(143, 180, 1350, 80), floorTitle, ui.PageTitle);
            Label(new Rect(150, 277, 1410, 44), "보스 포함 층마다 8개 방 · 층 보스 격파 시 회복 8과 유물 · 덱과 유물은 다음 층으로", ui.Body);
            DrawPortrait(new Rect(70, 422, 320, 507), flow.CurrentCharacter.Archetype == CombatArchetype.Ian ? ianPortrait : lunaPortrait);
            Fill(new Rect(91, 947, 387, 92), new Color(Paper.r, Paper.g, Paper.b, 0.92f));
            Label(new Rect(106, 957, 360, 35), flow.CurrentCharacter.DisplayName, ui.Heading);
            Label(new Rect(106, 998, 360, 31), roomResourceText, ui.Small);
            var map = run.Map;
            for (int d = 0; d < map.Definition.RoomsPerFloor; d++)
                Label(new Rect(519 + d * (1170f / Mathf.Max(1, map.Definition.RoomsPerFloor - 1)), 342, 70, 30), DepthLabels[Mathf.Min(d, 7)], ui.Small, true);
            for (int i = 0; i < map.Definition.NodeCount; i++)
            {
                var node = map.Definition.GetNode(i); if(node.Layer / map.Definition.RoomsPerFloor != run.CurrentFloor-1) continue; Vector2 p = MapPoint(node);
                for (int e = 0; e < node.NextNodeCount; e++)
                {
                    int target = node.GetNextNodeIndex(e);
                    if(map.Definition.GetNode(target).Layer / map.Definition.RoomsPerFloor != run.CurrentFloor-1) continue;
                    bool near = map.IsAvailable(i) || (map.IsCompleted(i) && map.IsAvailable(target));
                    Line(p, MapPoint(map.Definition.GetNode(target)), near ? Violet : new Color(Gold.r, Gold.g, Gold.b, 0.30f), near ? 1.7f : 1);
                }
            }
            for (int i = 0; i < map.Definition.NodeCount; i++)
            {
                var node = map.Definition.GetNode(i); if(node.Layer / map.Definition.RoomsPerFloor != run.CurrentFloor-1) continue; Vector2 p = MapPoint(node);
                bool active = map.IsAvailable(i), complete = map.IsCompleted(i);
                Disc(p, 34, active ? Ink : new Color(Paper.r, Paper.g, Paper.b, 0.95f));
                DrawRoomGlyph(p, 26, node.Kind, active ? Paper : complete ? Violet : Muted);
                if (active) Ring(p, 40, Violet, 1.7f);
                Fill(new Rect(p.x - 77, p.y + 48, 154, 67), new Color(Paper.r, Paper.g, Paper.b, 0.9f));
                Label(new Rect(p.x - 75, p.y + 49, 150, 29), complete ? "기록 완료" : RoomLabels[(int)node.Kind], ui.Body, true);
                Label(new Rect(p.x - 75, p.y + 79, 150, 37), node.DisplayName, ui.Small, true);
                if (active && GUI.Button(new Rect(p.x - 76, p.y - 40, 152, 156), GUIContent.none, GUIStyle.none))
                { run.TrySelectMapNode(i); message = ""; GUI.FocusControl(null); Refresh(); break; }
            }
            Label(new Rect(550, 958, 1210, 60), run.RoomResult, ui.Body, true);
        }

        private void DrawRewardsOrResult()
        {
            bool cardReward = run.Phase == RunPhase.CardReward, relicReward = run.Phase == RunPhase.RelicReward;
            Header("AFTER THE LIGHT", deckText);
            if (cardReward || relicReward)
            {
                Label(new Rect(142, 159, 1500, 95), cardReward ? "새로운 선을 기억하다" : "빛이 머문 물건", ui.PageTitle);
                Label(new Rect(150, 270, 1400, 45), cardReward ? "함께할 기술을 하나 고르세요." : "이번 여정에 남을 유물을 하나 고르세요.", ui.Body);
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
                        Label(new Rect(r.x, 816, r.width, 32), RarityNames[(int)card.Rarity], ui.Small, true);
                        if (hover)
                        {
                            Fill(new Rect(127, 395, 243, 380), new Color(Paper.r, Paper.g, Paper.b, 0.93f));
                            Label(new Rect(144, 411, 210, 52), card.DisplayName, ui.Heading);
                            Label(new Rect(144, 480, 210, 281), visual.Details, ui.Small);
                        }
                    }
                    else
                    {
                        var relic = run.RelicRewardOptions[i]; if (relic == null) continue;
                        Fill(r, new Color(1, 1, 1, 0.5f)); Border(r, hover ? Violet : Gold);
                        Diamond(new Vector2(r.center.x, r.y + 134), 61, Gold);
                        Diamond(new Vector2(r.center.x, r.y + 134), 35, Violet, 2);
                        Label(new Rect(r.x + 25, r.y + 234, r.width - 50, 60), relic.DisplayName, ui.Heading, true);
                        Label(new Rect(r.x + 25, r.y + 310, r.width - 50, 79), relic.Description, ui.Body, true);
                    }
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                    { if (cardReward) run.TrySelectReward(i); else run.TrySelectRelicReward(i); Refresh(); break; }
                }
                if (ui.Button(new Rect(799, 971, 322, 55), "지금의 덱으로 계속")) { run.TrySkipReward(); Refresh(); }
                return;
            }
            bool victory = run.Phase == RunPhase.Completed;
            DrawPortrait(new Rect(1110, 210, 500, 760), flow.CurrentCharacter.Archetype == CombatArchetype.Ian ? ianPortrait : lunaPortrait);
            Label(new Rect(145, 264, 1000, 118), victory ? "A Trace Remains" : "Until the Next Light", ui.Display);
            Label(new Rect(150, 423, 840, 55), victory ? "모든 기록을 이었습니다." : "이번 궤적은 여기서 멈춥니다.", ui.Heading);
            Label(new Rect(150, 512, 730, 75), victory ? "흩어진 빛은, 이제 하나의 길이 되었다." : "그리지 못한 선은 다음 만남을 기다린다.", ui.Body);
            if (ui.Button(new Rect(150, 715, 350, 68), "다시 시작", true)) { flow.TryRestartRun(); Refresh(); }
            if (ui.Button(new Rect(150, 804, 350, 55), "다른 여행자 선택")) { flow.ReturnToCharacterSelection(); Refresh(); }
            if (ui.Button(new Rect(150, 878, 350, 55), "처음으로")) { flow.ReturnToMainMenu(); Refresh(); }
        }

        private void DrawHelp()
        {
            Fill(new Rect(0, 0, 1920, 1080), new Color(Ink.r, Ink.g, Ink.b, 0.76f));
            Rect r = new Rect(460, 231, 1000, 618); Fill(r, Paper); Border(r, Gold, 24);
            Label(new Rect(518, 269, 840, 82), "파편을 모아 하나의 궤적으로", ui.Heading);
            Label(new Rect(520, 390, 840, 68), "01   카드의 빈칸은 안쪽의 식 전체입니다. 새 파편이 앞선 궤적을 감쌉니다.", ui.Body);
            Label(new Rect(520, 481, 840, 86), "02   응축은 체력 2 → 4 소모, 기본 1장 보충. 추가 드로우는 최대 1장. 적도 행동합니다.", ui.Body);
            Label(new Rect(520, 586, 840, 79), "03   방출은 모든 파편을 함께 발동합니다. 같은 선이 적에게 피해를, 자신에게 강화를 줍니다.", ui.Body);
            Label(new Rect(520, 705, 840, 55), "방향키 / 왼쪽 이동 패널 · Backspace 취소 · Space 응축 · Enter 방출", ui.Small);
            if (ui.Button(new Rect(1150, 745, 240, 55), "계속   Esc", true)) helpOpen = false;
        }
    }
}
