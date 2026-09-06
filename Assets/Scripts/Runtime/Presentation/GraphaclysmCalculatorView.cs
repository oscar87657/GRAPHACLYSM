using Graphaclysm.Application;
using Graphaclysm.Core.Runs;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private string calculatorX = "t-3", calculatorY = "t-3", calculatorError = "";
        private bool calculatorValid = true;
        private string inkText = "";
        private int deckPage;
        private static readonly string[] Keys = { "7", "8", "9", "+", "sin(", "4", "5", "6", "−", "cos(", "1", "2", "3", "×", "(", "0", ".", "t", "÷", ")" };
        private static readonly string[] DepthLabels = { "01", "02", "03", "04", "05", "06", "07", "08" };
        private static readonly string[] RoomLabels = { "전투", "정예", "보스", "이벤트", "유물", "휴식" };

        private void DrawCalculator()
        {
            Fill(new Rect(91, 243, 339, 392), new Color(Paper.r, Paper.g, Paper.b, 0.95f));
            bool enabled = GUI.enabled; GUI.enabled = enabled && !castActive;
            if (ui.Button(new Rect(100, 250, 155, 33), "x(t)", battle.Equation.SelectedAxis == 0)) { run.TrySelectFormulaAxis(0); GUI.FocusControl(null); }
            if (ui.Button(new Rect(266, 250, 155, 33), "y(t)", battle.Equation.SelectedAxis == 1)) { run.TrySelectFormulaAxis(1); GUI.FocusControl(null); }
            Fill(new Rect(100, 292, 320, 59), Color.white); Border(new Rect(100, 292, 320, 59), Gold);
            GUI.SetNextControlName("calculator-x");
            string nextX = GUI.TextField(new Rect(108, 296, 304, 51), calculatorX, 128, ui.Body);
            if (nextX != calculatorX) { calculatorX = nextX; run.TrySelectFormulaAxis(0); ApplyCalculator(); }
            Fill(new Rect(100, 359, 320, 59), Color.white); Border(new Rect(100, 359, 320, 59), Gold);
            GUI.SetNextControlName("calculator-y");
            string nextY = GUI.TextField(new Rect(108, 363, 304, 51), calculatorY, 128, ui.Body);
            if (nextY != calculatorY) { calculatorY = nextY; run.TrySelectFormulaAxis(1); ApplyCalculator(); }
            string focus = GUI.GetNameOfFocusedControl();
            if (focus == "calculator-x") run.TrySelectFormulaAxis(0); else if (focus == "calculator-y") run.TrySelectFormulaAxis(1);
            for (int i = 0; i < Keys.Length; i++)
                if (ui.Button(new Rect(100 + i % 5 * 65, 430 + i / 5 * 36, 59, 31), Keys[i])) CalculatorKey(Keys[i]);
            if (ui.Button(new Rect(100, 578, 95, 31), "지우기")) CalculatorKey("AC");
            if (ui.Button(new Rect(202, 578, 100, 31), "한 글자")) CalculatorKey("DEL");
            if (ui.Button(new Rect(309, 578, 111, 31), "원래 식"))
            { calculatorX = battle.Equation.Calculator.XText; calculatorY = battle.Equation.Calculator.YText; ApplyCalculator(); }
            Label(new Rect(101, 613, 320, 29), calculatorValid ? inkText : calculatorError, ui.Small);
            GUI.enabled = enabled;
        }
        private void CalculatorKey(string key)
        {
            int axis = battle.Equation.SelectedAxis;
            string current = axis == 0 ? calculatorX : calculatorY;
            if (key == "AC") current = "";
            else if (key == "DEL") { if (current.Length > 0) current = current.Substring(0, current.Length - 1); }
            else if (current.Length + key.Length <= 128) current += key;
            if (axis == 0) calculatorX = current; else calculatorY = current;
            GUI.FocusControl(null); ApplyCalculator();
        }
        private void ApplyCalculator()
        {
            calculatorValid = run.TrySetCalculator(calculatorX, calculatorY, out calculatorError);
            if (calculatorValid) { message = ""; Refresh(); }
        }
        private void UndoMove()
        {
            if (castActive || run == null) return;
            if (run.TryUndoMove()) { message = "이동 전 위치와 비용을 되돌렸습니다."; Refresh(); }
        }

        private void DrawRoom()
        {
            Header("BETWEEN THE TRACES", seedText);
            RoomStory room = run.CurrentRoom;
            Label(new Rect(145, 163, 1380, 80), room.Title, ui.PageTitle);
            Label(new Rect(151, 282, 1080, 163), room.Body, ui.Heading);
            Label(new Rect(151, 473, 1150, 48), roomResourceText, ui.Body);
            RunNodeKind kind = run.Map.Definition.GetNode(run.Map.ActiveNodeIndex).Kind;
            DrawRoomGlyph(new Vector2(1515, 360), 94, kind, Violet);
            float width = 1560f / room.ChoiceCount - 22;
            for (int i = 0; i < room.ChoiceCount; i++)
            {
                RoomChoice choice = room.GetChoice(i); Rect r = new Rect(150 + i * (width + 22), 633, width, 227);
                bool available = run.CanChooseRoomOption(i);
                Fill(r, new Color(1, 1, 1, available ? 0.72f : 0.3f)); Border(r, available ? Gold : Muted);
                Label(new Rect(r.x + 26, r.y + 28, r.width - 52, 74), choice.Title, ui.Heading);
                Label(new Rect(r.x + 26, r.y + 122, r.width - 52, 63), choice.Consequence, ui.Body);
                if (!available) Label(new Rect(r.x + 26, r.yMax - 37, r.width - 52, 26), "현재 상태에서는 선택할 수 없습니다.", ui.Small);
                bool old = GUI.enabled; GUI.enabled = old && available;
                bool picked = GUI.Button(r, GUIContent.none, GUIStyle.none); GUI.enabled = old;
                if (picked) { deckPage = 0; run.TryChooseRoomOption(i); Refresh(); break; }
            }
            Label(new Rect(151, 972, 1380, 48), "선택의 결과는 다음 방까지 이어집니다.", ui.Small);
            if (ui.Button(new Rect(1445, 967, 270, 53), "지나가기")) { run.TryLeaveRoom(); Refresh(); }
        }
        private static void DrawRoomGlyph(Vector2 p, float size, RunNodeKind kind, Color color)
        {
            if (kind == RunNodeKind.Rest)
            { Ring(p, size * 0.65f, color, 1.6f, 0.77f, 0.5f); Line(p + new Vector2(-size, size * 0.9f), p + new Vector2(size, size * 0.9f), color); }
            else if (kind == RunNodeKind.Event)
            { Ring(p, size * 0.75f, color); Line(p, p + Vector2.up * size * 0.65f, color, 2); Line(p, p + new Vector2(size * 0.42f, size * 0.25f), color, 2); }
            else if (kind == RunNodeKind.Treasure)
            { Diamond(p, size, color, 1.5f); Diamond(p, size * 0.55f, color); Line(p - Vector2.right * size, p + Vector2.right * size, color); }
            else
            { Diamond(p, size, color, kind == RunNodeKind.Boss ? 3 : 1.5f); if (kind != RunNodeKind.Battle) Diamond(p, size * 0.6f, color); }
        }
        private void DrawDeckRefinement()
        {
            Header("LEAVE A PAGE BEHIND", deckText);
            Label(new Rect(145, 160, 1300, 80), "어떤 기록을 덜어낼까요", ui.PageTitle);
            Label(new Rect(150, 253, 1300, 45), "선택한 카드 한 장이 이번 여정의 덱에서 사라집니다.", ui.Body);
            for (int slot = 0; slot < 12; slot++)
            {
                int index = deckPage * 12 + slot; if (index >= run.Deck.Count) break;
                var visual = Visual(run.Deck[index]); if (visual == null) continue;
                Rect r = new Rect(284 + slot % 6 * 224, 349 + slot / 6 * 236, 204, 216);
                DrawSkillCard(r, visual, r.Contains(Event.current.mousePosition), false);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && run.TryRemoveDeckCard(index)) { Refresh(); break; }
            }
            if (ui.Button(new Rect(610, 855, 165, 42), "이전", false, deckPage > 0)) { deckPage--; Refresh(); }
            Label(new Rect(822, 855, 270, 42), deckPageText, ui.Body, true);
            if (ui.Button(new Rect(1145, 855, 165, 42), "다음", false, (deckPage + 1) * 12 < run.Deck.Count)) { deckPage++; Refresh(); }
            if (ui.Button(new Rect(795, 966, 330, 57), "그대로 가져가기")) { run.TrySkipRefinement(); Refresh(); }
        }
    }
}
