using System.Text;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Relics;
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
        private Texture2D ianProfileClosed, ianProfileOpen, lunaProfileClosed, lunaProfileOpen;
        private Texture2D ianMedallionClosed, ianMedallionOpen, lunaMedallionClosed, lunaMedallionOpen;
        private Texture2D[] relicArt;
        private Material portraitMaterial;
        private SkillVisual[] visuals;
        private string hpText = "", energyText = "", turnText = "", formulaText = "", outcomeText = "", selfStateText = "";
        private string message = "", deckText = "", ultimateText = "", ultimateName = "", combatSkillText = "", combatSkillDescription = "", ultimateDescription = "";
        private string seedText = "", roomResourceText = "", deckPageText = "";
        private string[] enemyHealth, enemyDamage, enemyIntent, enemyStatuses, enemyStatusSummary, enemyBadges, cardFailures, hitNumbers;
        private int[] damagePreview;
        private bool[] castHits;
        private float[] cardHover;
        private readonly float[] characterEyeOpen = new float[2];
        private int hoveredCharacter = -1;
        private bool selfPreview, castSelfHit, castActive, impactApplied, helpOpen, showEquation, castUltimate;
        private int hoveredHand = -1, hoveredEnemy = -1;
        private bool combatSkillTargeting;
        private Rect handInspectionRect, handHoverBridge;
        private float castStarted;
        private float skillFxUntil;
        private double skillFxOriginX, skillFxOriginY, skillFxEndX, skillFxEndY;
        private bool skillFxReset;
        private int skillFxHits, skillFxLaneCount = 1, skillFxStyle;
        private string skillFxLabel = "";
        private string lastPlotName = "";
        private static readonly string[] AbilityNames = { "보호막", "집중", "재생", "잔불", "약화", "노출", "고정", "경쾌", "가시", "추진", "요새화", "균열", "회복", "정화", "수호", "방패 관통", "약해진 방벽", "찢긴 상처", "유도", "끌어당김" };
        private static int StatusAbilityIndex(int status) => status >= 12 ? status + 2 : status;
        private const float FieldUnit = 100;
        private static readonly Rect Field = new Rect(460, 88, 10 * FieldUnit, 8 * FieldUnit);

#if UNITY_EDITOR
        public System.Action DiagnosticGuiCheck { get; set; }
        public int DiagnosticHoveredCard { get; set; } = -1;
        public int DiagnosticRewardHover { get; set; } = -1;
        public int DiagnosticHoveredCharacter { get; set; } = -1;
        public int DiagnosticKeyword { get; set; } = -1;
        public string DiagnosticKeywordBody { get; set; } = "";
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
            public string[] KeywordNames, KeywordDetails;
            public string[] EffectTitles, EffectBodies, EffectNotes;
            public string[] EffectBadges;
            public string RuleText, OutputText;
            public Vector2[] Glyph;
        }

        private void Awake()
        {
            flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, unchecked((uint)(System.DateTime.UtcNow.Ticks ^ System.Environment.TickCount)));
            backdrop = Resources.Load<Texture2D>("Art/Generated/astral-archive-background-v3");
            ianPortrait = Resources.Load<Texture2D>("Art/Generated/ian-character-portrait-v2");
            lunaPortrait = Resources.Load<Texture2D>("Art/Generated/luna-nocturne-gothic-v6");
            ianProfileClosed = Resources.Load<Texture2D>("Art/Generated/ian-profile-eyes-closed-v11");
            ianProfileOpen = Resources.Load<Texture2D>("Art/Generated/ian-profile-eyes-open-v11");
            lunaProfileClosed = Resources.Load<Texture2D>("Art/Generated/luna-profile-eyes-closed-v11");
            lunaProfileOpen = Resources.Load<Texture2D>("Art/Generated/luna-profile-eyes-open-v11");
            relicArt = new Texture2D[FragmentRelicCatalog.All.Count];
            for (int i = 0; i < relicArt.Length; i++)
            {
                string resource = FragmentRelicCatalog.All[i].ImageResource;
                if (!string.IsNullOrEmpty(resource)) relicArt[i] = Resources.Load<Texture2D>(resource);
            }
            spellRenderer = new AstralSpellRenderer();
            BuildVisuals(); BuildDisc(); BuildPortraitMedallions(); InitializeServices(); Refresh();
            UnityEngine.Application.targetFrameRate = 60;
        }

        private void OnDestroy()
        {
            SaveCurrent(true); SavePreferences(); SaveLegacy();
            sound?.Dispose(); spellRenderer?.Dispose();
            if (disc != null) Destroy(disc);
            if (ianMedallionClosed != null) Destroy(ianMedallionClosed);
            if (ianMedallionOpen != null) Destroy(ianMedallionOpen);
            if (lunaMedallionClosed != null) Destroy(lunaMedallionClosed);
            if (lunaMedallionOpen != null) Destroy(lunaMedallionOpen);
            if (portraitMaterial != null) Destroy(portraitMaterial);
        }

        private void Update()
        {
            if (!ModalOpen) viewTime += Time.unscaledDeltaTime;
            if (!ReferenceEquals(run, flow.CurrentRun) || (run != null && !ReferenceEquals(game, run.CurrentBattle))) Refresh();
            ObservePlayerImpact();
            if (flow.Phase == GameFlowPhase.CharacterSelection)
            {
                for (int i = 0; i < characterEyeOpen.Length; i++)
                {
                    float target = hoveredCharacter == i || flow.SelectedCharacterIndex == i ? 1f : 0f;
                    characterEyeOpen[i] = preferences.ReduceMotion ? target
                        : Mathf.MoveTowards(characterEyeOpen[i], target, Time.unscaledDeltaTime * 5.5f);
                }
            }
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
                if (battle.Approach != CombatApproach.None) message = (battle.LastCannonCut ? "포격 중단 · " : "")
                    + "작도 " + report.HitCount + "명 적중"
                    + (battle.LastRecordingHits > 0 ? " · 기록 " + battle.LastRecordingHits + "명 추가 적중" : "")
                    + (report.Shield > 0 ? " · 보호막 +" + report.Shield : "")
                    + (battle.HasRecording ? " · 다음 방출에 기록 공격" : "");
                if(battle.LastRelicActivations>0) message+=" · 유물 "+battle.LastRelicActivations+"개 발동";
                if(battle.LastUltimateHits>0) message+=" · 궁극 후속 "+battle.LastUltimateHits+"명";
                if(battle.LastGrowthTriggers>0)message+=" · 성좌 연계 "+battle.LastGrowthTriggers+"개";
                if(battle.ExtraAssemblyPending) message="적 행동 전, 남은 손패로 한 번 더 조립하세요. 추가 드로우는 없습니다.";
                else if(battle.LastUltimateActivated) message+=" · "+ultimateName+" 발동";
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
#if UNITY_EDITOR
                if (DiagnosticGuiCheck != null && Event.current.type == EventType.Repaint)
                { var check=DiagnosticGuiCheck;DiagnosticGuiCheck=null;check(); }
#endif
                DrawBackdrop(); HandleKeywordInput(); HandleKeys(); BeginBattleHoverFrame();
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
                DrawKeywordTooltip();
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
                hoveredHand = -1; combatSkillTargeting = false; lunaPullTargeting = false;
                showBattleDetails=false;showSecondaryActions=false;showEquation=false;
            }
            int selected = flow.Phase == GameFlowPhase.CharacterSelection ? flow.SelectedCharacterIndex : 0;
            CharacterDefinition character = flow.CurrentCharacter ?? flow.GetCharacter(Mathf.Max(0, selected));
            if (run == null) return;
            seedText = "SEED  " + run.Seed;
            floorTitle = run.CurrentFloor + "층 / " + run.Map.Definition.FloorCount + "층  ·  " + FloorNames[Mathf.Min(run.CurrentFloor-1,FloorNames.Length-1)];
            roomResourceText = "체력 " + run.PlayerHealth + " / " + run.PlayerMaxHealth + "    ·    공명 " + run.Resonance + " / 6";
            deckPageText = (deckPage + 1) + " / " + ((run.Deck.Count + 11) / 12);
            deckText = "덱 " + run.Deck.Count + "  ·  유물 " + run.Relics.Count;
            hpText = (battle == null ? run.PlayerHealth : battle.PlayerHealth) + " / " + run.PlayerMaxHealth;
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
            turnText = "TURN  " + battle.Turn.ToString("00") + (battle.InExtraAssembly ? " · 두 번째 조립" : "");
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
                damagePreview[i] = battle.PreviewDamage(enemy) + battle.PreviewDeviceDamage(enemy) + battle.PreviewRecordingFollowupDamage(enemy) + battle.PreviewApproachUltimateDamage(enemy);
                int healthDamage = battle.PreviewHealthDamage(enemy);
                enemyDamage[i] = damagePreview[i] > 0 ? (healthDamage > 0 ? "−" + healthDamage : "방어") : "";
                if (damagePreview[i] > 0) hits++;
                if (battle.CannonDisconnected && enemy.Definition.Id == "signature.cannon") enemyIntent[i] = "동력 차단 · 공격 불가";
                else if(enemy.Intent.Kind==EnemyIntentKind.Charge && enemy.Definition.Behavior.Kind==EnemyBehaviorKind.EclipseEmperor)enemyIntent[i]="회복 12 · 수호 1";
                else if(enemy.Intent.Kind==EnemyIntentKind.Charge && (enemy.Definition.Behavior.Kind==EnemyBehaviorKind.GridSentry || enemy.Definition.Behavior.Kind==EnemyBehaviorKind.LaneArtillerist))enemyIntent[i]="보호막 4";
                else if(enemy.Intent.Kind==EnemyIntentKind.Charge && enemy.Definition.Behavior.Kind>=EnemyBehaviorKind.RotatingGun)
                    enemyIntent[i]=enemy.Definition.Behavior.Kind==EnemyBehaviorKind.RepairPriest?"아군 회복 5":enemy.Definition.Behavior.Kind==EnemyBehaviorKind.ArchiveKeeper?"아군 회복 8":enemy.Definition.Behavior.Kind==EnemyBehaviorKind.HexCaller || enemy.Definition.Behavior.Kind==EnemyBehaviorKind.AstralSovereign?"방벽·수호":"보호막 12";
                else if(enemy.FieldZoneCount>0)enemyIntent[i]=enemy.FieldPatternName+" "+Mathf.Max(0,enemy.Intent.Damage-enemy.Statuses.Get(CombatStatusKind.Weaken));
                else if (enemy.IsLineGunner) enemyIntent[i] = "직선 공격 " + Mathf.Max(0, enemy.Intent.Damage - enemy.Statuses.Get(CombatStatusKind.Weaken));
                else if (enemy.IsRingSentinel || enemy.IsCrossGunner || enemy.IsTwinMortar)
                    enemyIntent[i]=(enemy.IsRingSentinel?"고리 공격 ":enemy.IsCrossGunner?"십자 공격 ":"쌍점 폭격 ")
                        +Mathf.Max(0,enemy.Intent.Damage-enemy.Statuses.Get(CombatStatusKind.Weaken));
                else if (battle.SealPending(enemy)) enemyIntent[i] = battle.TraceBreaksSeal(enemy) ? "방출하면 봉인 취소" : "다음 턴 봉인";
                else if (battle.GuardActive(enemy)) enemyIntent[i] = battle.TraceBreaksGuard(enemy) ? "방출하면 연결 해제" : "연결 보호 · 공격 " + Mathf.Max(0, enemy.Intent.Damage - enemy.Statuses.Get(CombatStatusKind.Weaken));
                else if (enemy.Intent.Kind == EnemyIntentKind.Charge) enemyIntent[i] = enemy.SealInterrupted ? "봉인 중단" : "힘을 모으는 중";
                else if (enemy.Intent.Kind == EnemyIntentKind.Reposition) enemyIntent[i] = "자리 이동";
                else enemyIntent[i] = "공격 " + Mathf.Max(0, enemy.Intent.Damage - enemy.Statuses.Get(CombatStatusKind.Weaken));
                if(enemy.IsBoss) enemyIntent[i]=(enemy.Enraged?"Ⅱ · ":"Ⅰ · ")+enemyIntent[i];
                enemyStatuses[i] = DescribeStatuses(enemy.Statuses, 8, true);
                enemyStatusSummary[i] = DescribeStatuses(enemy.Statuses, 2);
            }
            outcomeText = battle.Equation.HasBase ? "적 " + hits + "명" + (selfPreview ? "  ·  자신 강화" : "") : "첫 카드를 선택하세요";
            if (battle.Equation.IsCalculator && battle.Phase == BattlePhase.PlayerPlanning && !battle.CanPlot)
                outcomeText = battle.Equation.TraceLength > BattleSession.MaximumTraceLength ? "잉크 초과 · 선을 줄이세요" : "필드 안에 선을 그리세요";
            selfStateText = battle.Tactics == null ? "" : DescribeStatuses(battle.Tactics.Statuses);
            int ultimateVariant = run.Growth.UltimateVariant;
            ultimateName = character.Archetype == CombatArchetype.Ian
                ? (ultimateVariant == 1 ? "성좌 붕괴" : ultimateVariant == 2 ? "불멸의 기록"
                    : ultimateVariant == 3 ? "흑경 반전" : "흑유리 개방")
                : (ultimateVariant == 1 ? "만월의 포옹" : ultimateVariant == 2 ? "그믐의 칼날"
                    : ultimateVariant == 3 ? "월식 정지" : "백야의 포옹");
            if(battle.HasApproachUltimates) ultimateName=ApproachUltimateNames[(int)battle.Approach];
            ultimateText = ultimateName;
            if (battle.Tactics != null && battle.Tactics.UltimateArmed) ultimateText += " · 준비됨";
            combatSkillText = battle.CombatSkillCooldown > 0 ? "재사용 " + battle.CombatSkillCooldown + "턴" : GrowthSkillName();
            combatSkillDescription = GrowthSkillDescription();
            ultimateDescription = UltimateSkillDescription();
            RefreshApproachUltimateView();
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
            if (castActive || combatSkillTargeting || run == null || (battle.Equation.IsCalculator && !calculatorValid)) return;
            if (run.TryPlayHandCard(index, out CardDefinition card, out CardPlayFailure failure))
            {
                sound?.Play(GameCue.Card); fragmentPlacedAt = ViewTime; message = ""; lastPlotName = card.DisplayName; hoveredHand = -1;
            }
            else message = Failure(failure);
            Refresh();
        }

        private void Undo()
        {
            if (castActive || combatSkillTargeting || run == null) return;
            if (run.TryUndoLastPlayedCard(out _)) { message = ""; Refresh(); }
        }

        private void Move(double x, double y)
        {
            if (castActive || combatSkillTargeting) return;
            message = run.TryMovePlayer(x, y) ? "" : "지금은 그 자리로 이동할 수 없습니다.";
            Refresh();
        }

        private void MoveTo(double x, double y)
        {
            if (castActive || combatSkillTargeting) return;
            message = run.TryMovePlayerTo(x, y) ? "" : "이동 횟수·고정 상태·기둥과 경계를 확인하세요.";
            Refresh();
        }

        private void ToggleCombatSkillTargeting()
        {
            lunaPullTargeting = false;
            if (battle != null && battle.Approach == CombatApproach.Observation)
            { ToggleSatelliteTargeting(); return; }
            skillInputPath = "button";
            if (castActive || run == null || battle == null) return;
            if (combatSkillTargeting)
            {
                combatSkillTargeting = false;
                message = "전투 기술 대상 선택을 취소했습니다.";
                Refresh();
                return;
            }
            if (battle.UsesDiagramAbility && battle.CanUseDiagramAbility)
            {
                combatSkillTargeting = true; diagramAngle = 0; diagramAimValid = false;
                diagramPreviewText = "기준점에서 3 이내의 전장 위치를 고르세요. 우클릭: 취소";
                message = battle.Approach == CombatApproach.Recording ? "기록을 재현할 위치를 선택하세요." : "새 기준점을 선택하세요. Q / E: 30도 회전";
                Refresh(); return;
            }
            if (!battle.CanUseCombatSkill)
            {
                message = battle.CombatSkillCooldown > 0
                    ? "전투 기술 대기시간이 남았습니다."
                    : "지금은 전투 기술을 사용할 수 없습니다.";
                Refresh();
                return;
            }
            combatSkillTargeting = true;
            message = battle.Approach == CombatApproach.Execution
                ? "방향을 클릭하세요 · 3.6 돌진 / 경로의 적 공격 · 우클릭 취소"
                : "전투 기술을 맞힐 적 표식이나 오른쪽 목록을 선택하세요.";
            Refresh();
        }

        private void UseCombatSkillOnTarget(int targetIndex)
        {
            if (battle != null && battle.Approach == CombatApproach.Execution)
            {
                if (targetIndex >= 0 && targetIndex < battle.Enemies.Count)
                    UseExecutionAt(battle.Enemies[targetIndex].X, battle.Enemies[targetIndex].Y);
                return;
            }
            if (battle != null && battle.UsesDiagramAbility) return;
            if (castActive || run == null || battle == null) return;
            if (targetIndex < 0) return;
            BattleSession source = battle;
            CombatSkillPreview prediction = source.PreviewCombatSkill(targetIndex);
            TraceCombatSkill("before", source, prediction);
            if (!prediction.CanUse)
            { message = SkillFailureText(prediction.Failure); Refresh(); return; }
            if (!run.TryUseCombatSkill(targetIndex))
            { message = source.CombatSkillCooldown > 0 ? "전투 기술 대기시간이 남았습니다." : "기술 궤적을 만들 수 없습니다."; Refresh(); return; }
            TraceCombatSkill("after", source, prediction);
            ShowCommittedSkill(source);
        }

        private void ShowCommittedSkill(BattleSession source)
        {
            combatSkillTargeting = false;
            skillFxOriginX = source.LastSkillOriginX; skillFxOriginY = source.LastSkillOriginY;
            skillFxEndX = source.LastSkillEndX; skillFxEndY = source.LastSkillEndY;
            skillFxReset = source.LastSkillCooldownReset; skillFxHits = source.LastSkillHitCount;
            skillFxLaneCount = source.LastSkillLaneCount;
            skillFxStyle = source.LastSkillStyle;
            skillFxLabel = skillFxReset ? "처치 · 즉시 재사용"
                : source.LastSkillDamage > 0 ? "체력 −" + source.LastSkillDamage
                : source.LastSkillShieldDamage > 0 ? "보호막 −" + source.LastSkillShieldDamage
                : "적중 " + skillFxHits;
            skillFxUntil = ViewTime + (preferences.ReduceMotion ? .35f : .9f);
            if (skillFxReset) message = "처치 공명 · 전투 기술을 바로 다시 사용할 수 있습니다.";
            else
            {
                message = "궤적 기술 · 적 " + skillFxHits + "명";
                if (source.LastSkillDamage > 0) message += " · 체력 피해 " + source.LastSkillDamage;
                if (source.LastSkillShieldDamage > 0) message += " · 보호막 파괴 " + source.LastSkillShieldDamage;
                if (source.LastSkillDamage == 0 && source.LastSkillShieldDamage == 0) message += " 적중";
            }
            Refresh();
        }

        private void StartCast()
        {
            if (castActive || combatSkillTargeting || (battle.Equation.IsCalculator && !calculatorValid) || !run.TryBeginPlot()) return;
            lastPlotName = battle.PlayedCardCount > 0 ? battle.GetPlayedCard(battle.PlayedCardCount - 1).DisplayName : "작도";
            for (int i = 0; i < castHits.Length; i++) { castHits[i] = damagePreview[i] > 0; hitNumbers[i] = enemyDamage[i]; }
            castUltimate = battle.Tactics.UltimateArmed;
            castSelfHit = selfPreview; castActive = true; impactApplied = false; castStarted = ViewTime;
            hoveredHand = -1; message = "";
            sound?.Play(GameCue.Cast); Refresh();
        }

        private void HandleKeys()
        {
            HandleKeyEvent(Event.current);
        }

        private void HandleKeyEvent(Event e)
        {
            if(nodeComparison != null)
            {
                if(e.type==EventType.KeyDown && (e.keyCode==KeyCode.Escape || e.keyCode==KeyCode.G))
                { nodeComparison=null; e.Use(); }
                return;
            }
            if (growthOpen && growthPlanOpen && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            { growthPlanOpen = false; e.Use(); return; }
            if (growthOpen && GUI.GetNameOfFocusedControl() == "GrowthSearch" && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape) { GUI.FocusControl(null); e.Use(); }
                return;
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (confirmation != Confirmation.None) confirmation = Confirmation.None;
                else if (settingsOpen) CloseSettings();
                else if (helpOpen) CloseHelp();
                else if (inventoryOpen) inventoryOpen = false;
                else if (growthOpen) growthOpen = false;
                else if (legacyOpen)
                {
                    if (legacyCartOwner == legacy && legacy.QuoteRanks(legacyCart) > 0)
                        legacyCartMessage = "구매 목록이 남아 있습니다. 계속 편집하거나 ‘예약 취소 후 닫기’를 선택하세요.";
                    else legacyOpen = false;
                }
                  else if (codexOpen) codexOpen = false;
                  else if(squadSelectionOpen)squadSelectionOpen=false;
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
                if (e.keyCode == KeyCode.G && run != null && !helpOpen)
                { if(flow.IsNodeTrial) ReturnNodeTrial(); else { growthOpen = !growthOpen; paused = false; } e.Use(); return; }
            }
            if (codexOpen || ModalOpen || run == null || run.Phase != RunPhase.Battle || castActive) return;
            string focused = GUI.GetNameOfFocusedControl();
            if (focused == "calculator-x" || focused == "calculator-y") return;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Backspace) { UndoMove(); e.Use(); return; }
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                if (combatSkillTargeting)
                { combatSkillTargeting = false; message = "전투 기술 대상 선택을 취소했습니다."; Refresh(); }
                else Undo();
                e.Use(); return;
            }
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9)
            {
                int index = e.keyCode - KeyCode.Alpha1;
                if (index < game.Deck.HandCount) PlayCard(index);
                e.Use(); return;
            }
            switch (e.keyCode)
            {
                case KeyCode.LeftArrow: Move(-battle.Tactics.CurrentMoveDistance, 0); break;
                case KeyCode.RightArrow: Move(battle.Tactics.CurrentMoveDistance, 0); break;
                case KeyCode.UpArrow: Move(0, battle.Tactics.CurrentMoveDistance); break;
                case KeyCode.DownArrow: Move(0, -battle.Tactics.CurrentMoveDistance); break;
                case KeyCode.Return: StartCast(); break;
                case KeyCode.Space: if(battle.UsesFragments) Condense(); break;
                case KeyCode.K: ToggleCombatSkillTargeting(); skillInputPath = "K"; break;
                case KeyCode.J:
                    if (battle.Approach == CombatApproach.Observation) ToggleSatelliteOrigin(); else ToggleLunaPull(); break;
                default: return;
            }
            e.Use();
        }

        private void BuildVisuals()
        {
            int fragmentCount = FragmentCardCatalog.All.Count + FragmentCardCatalog.Version29.Count + FragmentCardCatalog.Version28.Count + FragmentCardCatalog.Version27.Count;
            visuals = new SkillVisual[fragmentCount + CalculatorCardCatalog.All.Count + SkillCardCatalog.All.Count];
            for (int i = 0; i < visuals.Length; i++)
            {
                int old = i - fragmentCount;
                int modernCount = FragmentCardCatalog.All.Count, marketEnd = modernCount + FragmentCardCatalog.Version29.Count;
                int archiveEnd = marketEnd + FragmentCardCatalog.Version28.Count;
                CardDefinition card = i < modernCount ? FragmentCardCatalog.All[i]
                    : i < marketEnd ? FragmentCardCatalog.Version29[i - modernCount]
                    : i < archiveEnd ? FragmentCardCatalog.Version28[i - marketEnd]
                    : i < fragmentCount ? FragmentCardCatalog.Version27[i - archiveEnd]
                    : old < CalculatorCardCatalog.All.Count ? CalculatorCardCatalog.All[old] : SkillCardCatalog.All[old - CalculatorCardCatalog.All.Count];
                var shortText = new StringBuilder(); var details = new StringBuilder();
                int keywordCount = card.AbilityCount + (card.DrawBonus > 0 ? 1 : 0);
                var keywordNames = new string[keywordCount];
                var keywordDetails = new string[keywordCount];
                var effectTitles = new string[keywordCount];
                var effectBodies = new string[keywordCount];
                var effectNotes = new string[keywordCount];
                var effectBadges = new string[keywordCount];
                if (card.IsFragment) details.Append(FragmentEquation.Rule(card.Fragment)).Append("\n").Append("조립 위력 +").Append(card.WeavePower).Append(" · 합계 최대 10\n");
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
                    keywordNames[a] = AbilityNames[(int)ability.Kind];
                    keywordDetails[a] = (ability.Target == AbilityTarget.Enemy ? "적 적중 시 · " : "자신 적중 시 · ")
                        + AbilityDetail(ability) + "\n" + KeywordDescriptions[(int)ability.Kind];
                    effectTitles[a] = ability.Target == AbilityTarget.Enemy ? "적 적중" : "자신 적중";
                    effectBodies[a] = AbilityNames[(int)ability.Kind]
                        + (ability.Kind==CardAbilityKind.Cleanse || ability.Kind==CardAbilityKind.Anchor ? "" : " " + ability.Magnitude);
                    effectNotes[a] = ability.Kind == CardAbilityKind.Heal || ability.Kind == CardAbilityKind.Cleanse || ability.Kind == CardAbilityKind.Pull
                        ? "적중 시 즉시 적용" : "지속: 적 행동 " + ability.Duration + "회";
                    effectBadges[a] = effectTitles[a] + " · " + effectBodies[a];
                    if(card.IsFragment && ability.Kind==CardAbilityKind.Haste && ReferenceEquals(card,FragmentCardCatalog.Current(card.Id)))
                    {
                        keywordDetails[a]="다음 일반 이동 거리 +0.9 · 이동하면 소모";
                    }
                }
                if(card.DrawBonus > 0)
                { if(shortText.Length>0) shortText.Append(" · "); shortText.Append("드로우 +").Append(card.DrawBonus);
                  details.Append("다음 보충 +").Append(card.DrawBonus).Append("장 · 파편당 한 번\n응축: 보너스 합계 최대 1장, 나머지는 소멸\n"); }
                if (card.DrawBonus > 0)
                {
                    keywordNames[keywordCount - 1] = "드로우 +" + card.DrawBonus;
                    keywordDetails[keywordCount - 1] = "다음 보충에 카드 " + card.DrawBonus + "장을 추가합니다. 응축에서는 파편 보너스 합계가 최대 1장입니다.";
                    effectTitles[keywordCount - 1] = "다음 보충  ·  드로우";
                    effectBodies[keywordCount - 1] = "카드 " + card.DrawBonus + "장 추가";
                    effectNotes[keywordCount - 1] = "파편당 한 번 · 응축 보너스 합계 최대 1장";
                    effectBadges[keywordCount - 1] = "다음 보충  ·  카드 +" + card.DrawBonus + "장";
                }
                if (!card.IsFragment) details.Append("\n선이 닿은 대상에만 발동합니다.");
                visuals[i] = new SkillVisual { Card = card, PowerBadge = card.IsFragment ? (card.DrawBonus>0 ? "뽑기 +"+card.DrawBonus : "위력 +"+card.WeavePower) : "", Cost = card.Cost.ToString(), Abilities = shortText.ToString(), CompactAbilities = shortText.ToString().Replace("  ·  ", "\n").Replace(" · ", "\n").Replace("드로우 +", "뽑기").Replace(" ", ""), Details = details.ToString(), KeywordNames = keywordNames, KeywordDetails = keywordDetails, Glyph = MakeGlyph(card) };
                visuals[i].EffectTitles=effectTitles; visuals[i].EffectBodies=effectBodies; visuals[i].EffectNotes=effectNotes;
                visuals[i].EffectBadges=effectBadges;
                visuals[i].RuleText=card.IsFragment?FragmentEquation.Rule(card.Fragment):card.FormulaLabel;
                visuals[i].OutputText=card.IsFragment?"조립 위력 +"+card.WeavePower+"  /  합계 최대 10":"선에 닿은 대상에게 적용";
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
                case CardAbilityKind.Thorns: return "체력 피해를 받으면 공격자에게 " + a.Magnitude + " 반격";
                case CardAbilityKind.Momentum: return "다음 작도 피해 +" + a.Magnitude + " · 발동 후 소모";
                case CardAbilityKind.Fortify: return "적 행동 직전에 보호막 " + a.Magnitude + " 획득";
                case CardAbilityKind.Rupture: return "다음에 받는 작도 피해 +" + a.Magnitude + " · 발동 후 소모";
                case CardAbilityKind.Ward: return "해로운 상태 " + a.Magnitude + "회 차단 · 막을 때 소모";
                case CardAbilityKind.Pierce: return "다음 작도 피해 " + a.Magnitude + "은 보호막 통과";
                case CardAbilityKind.FrailShield: return "새로 얻는 보호막 −" + a.Magnitude + " · 기존 보호막 유지";
                case CardAbilityKind.Wound: return "이동하면 피해 " + a.Magnitude + " · 행동 단계당 1회";
                case CardAbilityKind.Guidance: return "다음 끌어당김 거리 +" + a.Magnitude + " · 이동 성공 시 소모";
                case CardAbilityKind.Pull: return "적을 내 쪽으로 " + a.Magnitude + " 당김 · 방출당 1회";
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
                s.Append(AbilityNames[StatusAbilityIndex(i)]).Append(' ').Append(count);
                if (detail) s.Append("  /  남은 적 행동 ").Append(statuses.Duration((CombatStatusKind)i)).Append("회");
            }
            return s.ToString();
        }

        private SkillVisual Visual(CardDefinition card)
        {
            for (int i = 0; i < visuals.Length; i++) if (ReferenceEquals(visuals[i].Card, card)) return visuals[i];
            for (int i = 0; i < visuals.Length; i++) if (visuals[i].Card.Id == card.Id && visuals[i].Card.HasMarketBalance == card.HasMarketBalance) return visuals[i];
            return null;
        }

        private Vector2[] MakeGlyph(CardDefinition card)
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

        private void BuildPortraitMedallions()
        {
            Shader shader = Resources.Load<Shader>("PortraitMedallion");
            if (shader == null) return;
            portraitMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            ianMedallionClosed = BakePortraitMedallion(ianProfileClosed);
            ianMedallionOpen = BakePortraitMedallion(ianProfileOpen);
            lunaMedallionClosed = BakePortraitMedallion(lunaProfileClosed);
            lunaMedallionOpen = BakePortraitMedallion(lunaProfileOpen);
        }

        private Texture2D BakePortraitMedallion(Texture2D source)
        {
            if (source == null || portraitMaterial == null) return null;
            const int size = 512;
            RenderTexture target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, target, portraitMaterial);
            RenderTexture.active = target;
            var result = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            result.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
            result.Apply(false, true);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return result;
        }

        private void Disc(Vector2 point, float radius, Color color)
        {
            Color saved = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(point.x - radius, point.y - radius, radius * 2, radius * 2), disc);
            GUI.color = saved;
        }
    }
}
