using System.Text;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    /// <summary>1920x1080 logical canvas with uniform scaling. All mutations go through run commands.</summary>
    public sealed partial class GraphaclysmModernView : MonoBehaviour
    {
        private const float ImpactTime = 1.08f, CastDuration = 1.85f;
        private PrototypeGameFlow flow;
        private RunGameSession run;
        private BattleGameSession game;
        private BattleSession battle;
        private AstralUi ui;
        private AstralSpellRenderer spellRenderer;
        private Texture2D backdrop, ianPortrait, lunaPortrait, disc;
        private SkillVisual[] visuals;
        private string hpText = "", energyText = "", turnText = "", formulaText = "", outcomeText = "", selfStateText = "";
        private string message = "", deckText = "", ultimateText = "", characterStats = "";
        private string seedText = "", roomResourceText = "", deckPageText = "";
        private string[] enemyHealth, enemyDamage, enemyIntent, enemyStatuses, enemyStatusSummary, enemyBadges, cardFailures, hitNumbers;
        private int[] damagePreview;
        private bool[] castHits;
        private float[] cardHover;
        private bool selfPreview, castSelfHit, castActive, impactApplied, helpOpen, showEquation, castUltimate;
        private int hoveredHand = -1, hoveredEnemy = -1;
        private float castStarted;
        private string lastPlotName = "";
        private static readonly string[] AbilityNames = { "보호막", "집중", "재생", "잔불", "약화", "노출", "고정", "경쾌", "회복", "정화" };
        private static readonly string[] RarityNames = { "일반", "고급", "희귀", "전설" };
        private const float FieldUnit = 95;
        private static readonly Rect Field = new Rect(423, 108, 10 * FieldUnit, 8 * FieldUnit);

#if UNITY_EDITOR
        public int DiagnosticHoveredCard { get; set; } = -1;
        public int DiagnosticRewardHover { get; set; } = -1;
        public float DiagnosticCastTime { get; set; } = -1;
#endif
        private float CastElapsed
        {
            get
            {
#if UNITY_EDITOR
                if (DiagnosticCastTime >= 0) return DiagnosticCastTime;
#endif
                return ViewTime - castStarted;
            }
        }

        private sealed class SkillVisual
        {
            public CardDefinition Card;
            public string Cost, Abilities, Details, CompactAbilities, PowerBadge;
            public Vector2[] Glyph;
        }

        private void Awake()
        {
            flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, unchecked((uint)(System.DateTime.UtcNow.Ticks ^ System.Environment.TickCount)));
            backdrop = Resources.Load<Texture2D>("Art/Generated/astral-archive-background-v3");
            ianPortrait = Resources.Load<Texture2D>("Art/Generated/ian-character-portrait-v2");
            lunaPortrait = Resources.Load<Texture2D>("Art/Generated/luna-nocturne-gothic-v6");
            spellRenderer = new AstralSpellRenderer();
            BuildVisuals(); BuildDisc(); InitializeServices(); Refresh();
            UnityEngine.Application.targetFrameRate = 60;
        }

        private void OnDestroy()
        {
            SaveCurrent(true); SavePreferences();
            sound?.Dispose(); spellRenderer?.Dispose();
            if (disc != null) Destroy(disc);
        }

        private void Update()
        {
            if (!ModalOpen) viewTime += Time.unscaledDeltaTime;
            if (!ReferenceEquals(run, flow.CurrentRun) || (run != null && !ReferenceEquals(game, run.CurrentBattle))) Refresh();
            if (ModalOpen) return;
            if (cardHover != null)
                for (int i = 0; i < cardHover.Length; i++)
                    cardHover[i] = preferences.ReduceMotion ? (i == hoveredHand ? 1 : 0)
                        : Mathf.MoveTowards(cardHover[i], i == hoveredHand ? 1 : 0, Time.unscaledDeltaTime * 7);
            if (!castActive) return;
            float elapsed = CastElapsed;
            if (!impactApplied && elapsed >= ImpactTime)
            {
                PlotReport report = run.ResolvePlot();
                sound?.Play(GameCue.Impact);
                impactApplied = true;
                message = report.PlayerHit ? "같은 선 위에서, 힘을 되찾았다." : "새로운 궤적이 새겨졌다.";
                Refresh();
            }
            if (elapsed < CastDuration) return;
            castActive = false;
            if (run.Phase == RunPhase.Battle && battle.Phase == BattlePhase.EnemyTurn)
            { if (run.ResolveEnemyTurn() > 0) sound?.Play(GameCue.Damage); }
            Refresh();
        }

        private void OnGUI()
        {
            if (flow == null) return;
            if (ui == null) ui = new AstralUi(PlayClick);
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            Fill(new Rect(0, 0, Screen.width, Screen.height), Ink);
            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1920 * scale) * 0.5f,
                (Screen.height - 1080 * scale) * 0.5f, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            try
            {
                DrawBackdrop(); HandleKeys();
                GUI.enabled = !ModalOpen;
                if (codexOpen) DrawCodex();
                else if (flow.Phase == GameFlowPhase.MainMenu) DrawTitle();
                else if (flow.Phase == GameFlowPhase.CharacterSelection) DrawCharacters();
                else if (castActive || run.Phase == RunPhase.Battle) DrawBattle();
                else if (run.Phase == RunPhase.MapSelection) DrawMap();
                else if (run.Phase == RunPhase.Room) DrawRoom();
                else if (run.Phase == RunPhase.DeckRefinement) DrawDeckRefinement();
                else DrawRewardsOrResult();
                GUI.enabled = true;
                if (saveFailed && !ModalOpen) Label(new Rect(425, 4, 1080, 38), saveNotice, ui.Small, true);
                DrawSystemOverlay();
            }
            finally { GUI.matrix = saved; GUI.color = Color.white; GUI.enabled = true; }
        }

        private void Refresh()
        {
            if(message != lastFeedback) {lastFeedback=message;feedbackUntil=ViewTime+3;}
            run = flow.CurrentRun;
            RefreshSystemState();
            BattleGameSession next = run?.CurrentBattle;
            if (!ReferenceEquals(next, game))
            {
                game = next; battle = game?.Battle;
                if (battle != null)
                {
                    int count = battle.Enemies.Count;
                    enemyHealth = new string[count]; enemyDamage = new string[count]; enemyIntent = new string[count];
                    enemyStatuses = new string[count]; enemyStatusSummary = new string[count]; enemyBadges = new string[count]; damagePreview = new int[count];
                    castHits = new bool[count]; hitNumbers = new string[count];
                    cardHover = new float[game.Deck.HandCapacity]; cardFailures = new string[game.Deck.HandCapacity];
                    if (battle.Equation.IsCalculator)
                    { calculatorX = battle.Equation.Calculator.XText; calculatorY = battle.Equation.Calculator.YText; calculatorValid = true; calculatorError = ""; }
                }
                hoveredHand = -1; showBattleDetails=false;showSecondaryActions=false;showEquation=false;
            }
            int selected = flow.Phase == GameFlowPhase.CharacterSelection ? flow.SelectedCharacterIndex : 0;
            CharacterDefinition character = flow.CurrentCharacter ?? flow.GetCharacter(Mathf.Max(0, selected));
            characterStats = "체력 " + character.MaxHealth + "     시작 손패 5 · 보존 한도 8";
            if (run == null) return;
            seedText = "SEED  " + run.Seed;
            floorTitle = run.CurrentFloor + "층 / " + run.Map.Definition.FloorCount + "층  ·  " + FloorNames[Mathf.Min(run.CurrentFloor-1,2)];
            roomResourceText = "체력 " + run.PlayerHealth + " / " + run.PlayerMaxHealth + "    ·    공명 " + run.Resonance + " / 6";
            deckPageText = (deckPage + 1) + " / " + ((run.Deck.Count + 11) / 12);
            deckText = "덱 " + run.Deck.Count + "  ·  유물 " + run.Relics.Count;
            hpText = (battle == null ? run.PlayerHealth : battle.PlayerHealth) + " / " + character.MaxHealth;
            if (battle == null) return;
            energyText = battle.Energy + " / " + battle.PlayerMaxEnergy;
            if(battle.UsesFragments)
            {
                quietWeave = "조립 " + battle.PlayedCardCount + " / 8";
                quietCondense = battle.CondenseCount>=2 ? "응축 완료" : "응축 · 체력 −" + battle.CondenseHealthCost;
                condenseHint = battle.CondenseCount>=2 ? "방출 또는 해체로 새로 준비" : "드로우 " + game.CondenseDrawCount + "장 · 적 행동 · " + (2-battle.CondenseCount) + "회 남음";
                weaveStatus = "파편 " + battle.PlayedCardCount + "/8  ·  응축 " + battle.CondenseCount + "/2";
                drawStatus = "턴 드로우 +" + game.NextDrawCount + " · 손패 " + game.Deck.HandCount + "/8";
                burstStatus = "조립 피해 +" + battle.WeaveDamageBonus;
            }
            turnText = "TURN  " + battle.Turn.ToString("00");
            formulaText = battle.Equation.HasBase ? battle.Equation.BuildFormula() : "아직 쓰이지 않은 수식";
            if (battle.Equation.IsCalculator)
                inkText = "잉크 " + battle.Equation.TraceLength.ToString("0.0") + " / 64 · t = 0~2π";
            selfPreview = battle.PreviewPlayerHit;
            int hits = 0;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i];
                enemyHealth[i] = enemy.Health + " / " + enemy.Definition.MaxHealth;
                enemyBadges[i] = (i + 1).ToString("00");
                damagePreview[i] = battle.PreviewDamage(enemy);
                int healthDamage = Mathf.Max(0, damagePreview[i] - enemy.Statuses.Get(CombatStatusKind.Shield));
                enemyDamage[i] = damagePreview[i] > 0 ? (healthDamage > 0 ? "−" + healthDamage : "방어") : "";
                if (damagePreview[i] > 0) hits++;
                if (enemy.Intent.Kind == EnemyIntentKind.Charge) enemyIntent[i] = "힘을 모으는 중";
                else if (enemy.Intent.Kind == EnemyIntentKind.Reposition) enemyIntent[i] = "자리 이동";
                else enemyIntent[i] = "공격 " + Mathf.Max(0, enemy.Intent.Damage - enemy.Statuses.Get(CombatStatusKind.Weaken));
                enemyStatuses[i] = DescribeStatuses(enemy.Statuses, 8, true);
                enemyStatusSummary[i] = DescribeStatuses(enemy.Statuses, 2);
            }
            outcomeText = battle.Equation.HasBase ? "적 " + hits + "명" + (selfPreview ? "  ·  자신 강화" : "") : "첫 카드를 선택하세요";
            if (battle.Equation.IsCalculator && battle.Phase == BattlePhase.PlayerPlanning && !battle.CanPlot)
                outcomeText = battle.Equation.TraceLength > BattleSession.MaximumTraceLength ? "잉크 초과 · 선을 줄이세요" : "필드 안에 선을 그리세요";
            selfStateText = battle.Tactics == null ? "" : DescribeStatuses(battle.Tactics.Statuses);
            ultimateText = (character.Archetype == CombatArchetype.Ian ? "흑유리 개방" : "백야의 포옹");
            if (battle.Tactics != null && battle.Tactics.UltimateArmed) ultimateText += " · 준비됨";
            for (int i = 0; i < game.Deck.HandCount; i++)
                cardFailures[i] = battle.CanPlayCard(game.Deck.GetHandCard(i), out CardPlayFailure failure) ? "" : Failure(failure);
        }

        private static string Failure(CardPlayFailure failure)
        {
            switch (failure)
            {
                case CardPlayFailure.MissingBaseFunction: return "초점이나 월륜으로 먼저 선을 시작하세요.";
                case CardPlayFailure.BaseFunctionAlreadySelected: return "시작 카드는 한 장이면 충분합니다.";
                case CardPlayFailure.NotEnoughEnergy: return "남은 에너지가 부족합니다.";
                case CardPlayFailure.WrongPhase: return "작도가 끝나면 선택할 수 있습니다.";
                default: return "수식에 더 연결할 수 없습니다.";
            }
        }

        private void PlayCard(int index)
        {
            if (castActive || run == null || (battle.Equation.IsCalculator && !calculatorValid)) return;
            if (run.TryPlayHandCard(index, out CardDefinition card, out CardPlayFailure failure))
            {
                sound?.Play(GameCue.Card); fragmentPlacedAt = ViewTime; message = ""; lastPlotName = card.DisplayName; hoveredHand = -1;
            }
            else message = Failure(failure);
            Refresh();
        }

        private void Undo()
        {
            if (castActive || run == null) return;
            if (run.TryUndoLastPlayedCard(out _)) { message = ""; Refresh(); }
        }

        private void Move(double x, double y)
        {
            if (castActive) return;
            message = run.TryMovePlayer(x, y) ? "" : "지금은 그 자리로 이동할 수 없습니다.";
            Refresh();
        }

        private void StartCast()
        {
            if (castActive || (battle.Equation.IsCalculator && !calculatorValid) || !run.TryBeginPlot()) return;
            lastPlotName = battle.PlayedCardCount > 0 ? battle.GetPlayedCard(battle.PlayedCardCount - 1).DisplayName : "작도";
            for (int i = 0; i < castHits.Length; i++) { castHits[i] = damagePreview[i] > 0; hitNumbers[i] = enemyDamage[i]; }
            castUltimate = battle.Tactics.UltimateArmed;
            castSelfHit = selfPreview; castActive = true; impactApplied = false; castStarted = ViewTime;
            hoveredHand = -1; message = "";
            sound?.Play(GameCue.Cast); Refresh();
        }

        private void HandleKeys()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (confirmation != Confirmation.None) confirmation = Confirmation.None;
                else if (settingsOpen) CloseSettings();
                else if (helpOpen) CloseHelp();
                else if (inventoryOpen) inventoryOpen = false;
                else if (codexOpen) codexOpen = false;
                else if (run != null) paused = !paused;
                else if (flow.Phase == GameFlowPhase.CharacterSelection) { flow.ReturnToMainMenu(); Refresh(); }
                else OpenSettings();
                e.Use(); return;
            }
            if (e.type == EventType.KeyDown && confirmation == Confirmation.None && !settingsOpen)
            {
                if (e.keyCode == KeyCode.F1) { if (helpOpen) CloseHelp(); else OpenHelp(); e.Use(); return; }
                if (e.keyCode == KeyCode.D && run != null && !helpOpen)
                { if (inventoryOpen) inventoryOpen = false; else OpenInventory(); e.Use(); return; }
            }
            if (codexOpen || ModalOpen || run == null || run.Phase != RunPhase.Battle || castActive) return;
            string focused = GUI.GetNameOfFocusedControl();
            if (focused == "calculator-x" || focused == "calculator-y") return;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Backspace) { UndoMove(); e.Use(); return; }
            if (e.type == EventType.MouseDown && e.button == 1) { Undo(); e.Use(); return; }
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9)
            {
                int index = e.keyCode - KeyCode.Alpha1;
                if (index < game.Deck.HandCount) PlayCard(index);
                e.Use(); return;
            }
            switch (e.keyCode)
            {
                case KeyCode.LeftArrow: Move(-1.5, 0); break;
                case KeyCode.RightArrow: Move(1.5, 0); break;
                case KeyCode.UpArrow: Move(0, 1.5); break;
                case KeyCode.DownArrow: Move(0, -1.5); break;
                case KeyCode.Return: StartCast(); break;
                case KeyCode.Space: if(battle.UsesFragments) Condense(); break;
                default: return;
            }
            e.Use();
        }

        private void BuildVisuals()
        {
            visuals = new SkillVisual[FragmentCardCatalog.All.Count + CalculatorCardCatalog.All.Count + SkillCardCatalog.All.Count];
            for (int i = 0; i < visuals.Length; i++)
            {
                int old = i - FragmentCardCatalog.All.Count;
                CardDefinition card = i < FragmentCardCatalog.All.Count ? FragmentCardCatalog.All[i] : old < CalculatorCardCatalog.All.Count ? CalculatorCardCatalog.All[old] : SkillCardCatalog.All[old - CalculatorCardCatalog.All.Count];
                var shortText = new StringBuilder(); var details = new StringBuilder();
                if (card.IsFragment) details.Append(FragmentEquation.Rule(card.Fragment)).Append("\n").Append("조립 위력 +").Append(FragmentCardCatalog.Power(card.Fragment)).Append(" · 합계 최대 10\n");
                details.Append(card.Description).Append("\n\n");
                if(!card.IsFragment) details.Append(card.FormulaLabel).Append("\n");
                for (int a = 0; a < card.AbilityCount; a++)
                {
                    CardAbility ability = card.GetAbility(a);
                    if (a > 0) shortText.Append("  ·  ");
                    shortText.Append(AbilityNames[(int)ability.Kind]);
                    if (ability.Kind != CardAbilityKind.Cleanse && ability.Kind != CardAbilityKind.Anchor) shortText.Append(' ').Append(ability.Magnitude);
                    details.Append(ability.Target == AbilityTarget.Enemy ? "적 적중  /  " : "자신 적중  /  ");
                    details.Append(AbilityDetail(ability)).Append('\n');
                }
                if(card.DrawBonus > 0)
                { if(shortText.Length>0) shortText.Append(" · "); shortText.Append("드로우 +").Append(card.DrawBonus);
                  details.Append("다음 보충 +").Append(card.DrawBonus).Append("장 · 파편당 한 번\n응축: 보너스 합계 최대 1장, 나머지는 소멸\n"); }
                if (!card.IsFragment) details.Append("\n선이 닿은 대상에만 발동합니다.");
                visuals[i] = new SkillVisual { Card = card, PowerBadge = card.IsFragment ? (card.DrawBonus>0 ? "뽑기 +"+card.DrawBonus : "위력 +"+FragmentCardCatalog.Power(card.Fragment)) : "", Cost = card.Cost.ToString(), Abilities = shortText.ToString(), CompactAbilities = shortText.ToString().Replace("  ·  ", "\n").Replace(" · ", "\n").Replace("드로우 +", "뽑기").Replace(" ", ""), Details = details.ToString(), Glyph = MakeGlyph(card) };
            }
        }

        private static string AbilityDetail(CardAbility a)
        {
            switch (a.Kind)
            {
                case CardAbilityKind.Cleanse: return a.Target == AbilityTarget.Player ? "해로운 상태 해제" : "남은 강화 해제";
                case CardAbilityKind.Heal: return "체력 " + a.Magnitude + " 회복";
                case CardAbilityKind.Focus: return "다음 작도 피해 +" + a.Magnitude;
                case CardAbilityKind.Anchor: return "이동 봉쇄 · 적 행동 " + a.Duration + "회";
                case CardAbilityKind.Haste: return "다음 이동 무료 · 적 행동 " + a.Duration + "회";
                default: return AbilityNames[(int)a.Kind] + " " + a.Magnitude + " · 적 행동 " + a.Duration + "회";
            }
        }

        private static string DescribeStatuses(CombatStatusState statuses, int limit = 8, bool detail = false)
        {
            var s = new StringBuilder();
            int displayed = 0;
            for (int i = 0; i < CombatStatusState.Capacity; i++)
            {
                int count = statuses.Get((CombatStatusKind)i); if (count == 0) continue;
                if (displayed++ == limit) { s.Append(" …"); break; }
                if (s.Length > 0) s.Append(detail ? "\n" : "  ·  ");
                s.Append(AbilityNames[i]).Append(' ').Append(count);
                if (detail) s.Append("  /  남은 적 행동 ").Append(statuses.Duration((CombatStatusKind)i)).Append("회");
            }
            return s.ToString();
        }

        private SkillVisual Visual(CardDefinition card)
        {
            for (int i = 0; i < visuals.Length; i++) if (visuals[i].Card.Id == card.Id) return visuals[i];
            return null;
        }

        private static Vector2[] MakeGlyph(CardDefinition card)
        {
            if(card.IsFragment) return MakeFragmentEmblem(card.Fragment);
            var points = new Vector2[41];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / 40f, x = t * 2 - 1, y = x;
                if (card.IsBaseFunction && card.BaseFunction == BaseFunctionKind.InputAngle)
                { float a = t * Mathf.PI * 3; points[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.18f + t * 0.72f); continue; }
                switch (card.Modifier)
                {
                    case EquationModifierKind.Square: y = x * x * 1.5f - 0.5f; break;
                    case EquationModifierKind.Cube: y = x * x * x; break;
                    case EquationModifierKind.Sine: y = Mathf.Sin(x * Mathf.PI); break;
                    case EquationModifierKind.Cosine: y = Mathf.Cos(x * Mathf.PI); break;
                    case EquationModifierKind.AbsoluteValue: y = Mathf.Abs(x) * 1.5f - 0.5f; break;
                    case EquationModifierKind.Double: x *= 0.5f; break;
                    case EquationModifierKind.Half: y *= 0.5f; break;
                    case EquationModifierKind.Negate: y = -x; break;
                    case EquationModifierKind.ShiftUp: if (!card.IsBaseFunction) y = x * 0.6f + 0.3f; break;
                    case EquationModifierKind.ShiftDown: y = x * 0.6f - 0.3f; break;
                }
                points[i] = new Vector2(x, y * 0.8f);
            }
            return points;
        }

        private void BuildDisc()
        {
            disc = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(31 - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f))));
            disc.SetPixels(pixels); disc.Apply(false, true);
        }

        private void Disc(Vector2 point, float radius, Color color)
        {
            Color saved = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(point.x - radius, point.y - radius, radius * 2, radius * 2), disc);
            GUI.color = saved;
        }
    }
}
