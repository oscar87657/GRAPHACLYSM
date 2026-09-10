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
            if (ui.Button(new Rect(140, 901, 350, 52), "설정")) OpenSettings();
            if (ui.Button(new Rect(510, 901, 260, 52), "게임 종료")) confirmation = Confirmation.Quit;
            Label(new Rect(510, 971, 1120, 65), saveNotice, ui.Small);
            Fill(new Rect(127, 972, 351, 44), new Color(Paper.r, Paper.g, Paper.b, 0.86f));
            Label(new Rect(140, 978, 330, 32), "수식으로 엮는 카드 전투", ui.Small);
        }

        private void DrawCharacters()
        {
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
            string previewStats = "체력 " + previewCharacter.MaxHealth + "     시작 손패 5 · 보존 한도 8";
            Header("CHOOSE YOUR TRACE", "01 / TRAVELER");
            Label(new Rect(500, 118, 920, 54), "두 기록 사이에서 한 사람을 선택하세요", ui.PageTitle, true);
            DrawCharacterMedallion(0, left, ianMedallionClosed, ianMedallionOpen);
            DrawCharacterMedallion(1, right, lunaMedallionClosed, lunaMedallionOpen);

            Fill(new Rect(889, 215, 142, 525), new Color(Paper.r, Paper.g, Paper.b, .88f));
            Diamond(new Vector2(960, 477), 265, new Color(Gold.r, Gold.g, Gold.b, .66f), 2.2f);
            Diamond(new Vector2(960, 477), 224, new Color(Violet.r, Violet.g, Violet.b, .48f), 1.4f);
            Ring(new Vector2(960, 477), 72, new Color(Gold.r, Gold.g, Gold.b, .52f), 1.5f);
            Label(new Rect(890, 440, 140, 74), "THE\nGATE", ui.Small, true);

            if (GUI.Button(new Rect(left.x - 260, left.y - 260, 520, 520), GUIContent.none, GUIStyle.none))
            { flow.TrySelectCharacter(0); Refresh(); }
            if (GUI.Button(new Rect(right.x - 260, right.y - 260, 520, 520), GUIContent.none, GUIStyle.none))
            { flow.TrySelectCharacter(1); Refresh(); }

            Fill(new Rect(350, 770, 1220, 174), new Color(Paper.r, Paper.g, Paper.b, .9f));
            Border(new Rect(350, 770, 1220, 174), flow.SelectedCharacterIndex == preview ? Violet : Gold, 18);
            Label(new Rect(385, 789, 250, 48), ian ? "이안" : "루나", ui.PageTitle);
            Label(new Rect(650, 790, 450, 42), ian ? "흑유리의 기록자" : "천문 도구의 조율자", ui.Heading);
            Label(new Rect(385, 842, 710, 67), ian ? "깨진 유리에도, 지나간 빛은 남아 있어." : "틀린 회전은 없어. 아직 맞물리지 않았을 뿐.", ui.Body);
            Label(new Rect(1122, 790, 410, 43), previewStats, ui.Small);
            Label(new Rect(1122, 838, 410, 72), ian ? "흑유리 개방 · 피해 +6 / 이동 봉쇄" : "백야의 포옹 · 정화 / 보호막 / 회복", ui.Small);
            if (ui.Button(new Rect(143, 966, 220, 51), "돌아가기")) { flow.ReturnToMainMenu(); Refresh(); }
            if (ui.Button(new Rect(785, 968, 350, 62), (flow.SelectedCharacterIndex == 0 ? "이안" : "루나") + "의 기록 시작", true)) { flow.TryStartRun(); Refresh(); }
        }

        private void DrawCharacterMedallion(int index, Vector2 center, Texture2D closed, Texture2D open)
        {
            float eyeOpen = characterEyeOpen[index];
            const float radius = 238;
            Disc(center, radius + 12, new Color(Ink.r, Ink.g, Ink.b, .92f));
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
            Label(new Rect(center.x - 125, center.y + radius - 24, 250, 42), index == 0 ? "이안" : "루나", ui.Light, true);
            if (flow.SelectedCharacterIndex == index)
                Label(new Rect(center.x - 125, center.y + radius + 13, 250, 31), "선택된 기록", ui.Small, true);
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
            ModalPanel(HelpTitles[helpPage]);
            Label(new Rect(485, 302, 936, 405), HelpBodies[helpPage], ui.Body);
            if (ui.Button(new Rect(480, 791, 240, 60), "이전", enabled: helpPage > 0)) helpPage--;
            if (ui.Button(new Rect(748, 791, 290, 60), "안내 닫기   Esc")) CloseHelp();
            if (ui.Button(new Rect(1066, 791, 352, 60), helpPage == HelpTitles.Length - 1 ? "시작하기" : "다음", true))
            { if (helpPage == HelpTitles.Length - 1) CloseHelp(); else helpPage++; }
        }
    }
}
