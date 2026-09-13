using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using Unity.Profiling;
using UnityEngine;

namespace Graphaclysm.Runtime.Presentation
{
    /// <summary>
    /// Temporary IMGUI presentation. All game rules live in Graphaclysm.Core.
    /// This class may be replaced by uGUI without changing combat behavior.
    /// </summary>
    public sealed partial class GraphaclysmPrototypeView : MonoBehaviour
    {
        private const float DomainMin = 0f;
        private const float DomainMax = 10f;
        private const float RangeMin = -4f;
        private const float RangeMax = 4f;
        private const float PlotDuration = 1.15f;
        private const float CardHoverInSpeed = 9f;
        private const float CardHoverOutSpeed = 12f;
        private static readonly ProfilerMarker FieldLinesMarker =
            new ProfilerMarker("GRAPHACLYSM.Polynomiograph.Draw");

        private struct CardLayout
        {
            public Rect Rect;
            public float Angle;
        }

        private BattleSession battle;
        private DeckSession deck;
        private BattleGameSession game;
        private PrototypeGameFlow flow;
        private RunGameSession run;
        private Texture2D circleTexture;
        private Texture2D titleBackground;
        private Texture2D[] characterPortraits;
        private bool presentationAssetsLoaded;
        private GUIStyle titleStyle;
        private GUIStyle formulaStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle cardStyle;
        private GUIStyle handCardStyle;
        private GUIStyle actionStyle;
        private GUIStyle resultStyle;
        private GUIStyle menuTitleStyle;

        private string[] cardLabels;
        private CardLayout[] cardLayouts;
        private float[] cardHoverAmounts;
        private string[] playedCardLabels;
        private string[] enemyStatusLabels;
        private string[] previewLabels;
        private string[] rewardLabels;
        private string[] relicRewardLabels;
        private string[] mapNodeLabels;
        private string[] characterLabels;
        private int[] previewDamage;
        private string statusText;
        private string formulaText;
        private string combatMessage;
        private string characterHudText;
        private float plotStartedAt;
        private int hoveredHandIndex = -1;
        private PolynomiographLineRenderer fieldLineRenderer;

#if UNITY_EDITOR
        // Opt-in A/B profiling only. The legacy renderer is excluded from players.
        public bool ProfileFieldLines { get; set; }
        public int DiagnosticHoveredCard { get; set; } = -1;
        public bool UseLegacyFieldLineRendering { get; set; }
        public long FieldLineDrawAllocatedBytes { get; private set; }
#endif

        private void Awake()
        {
            // Enter Play Mode can keep managed fields when Domain Reload is disabled.
            // A new play session must always own a fresh run root.
            flow = null;
            run = null;
            game = null;
            battle = null;
            deck = null;
            titleBackground = null;
            characterPortraits = null;
            presentationAssetsLoaded = false;
            statusText = null;
            formulaText = null;
            fieldLineRenderer?.Dispose();
            fieldLineRenderer = new PolynomiographLineRenderer();
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            UnityEngine.Application.targetFrameRate = 60;

            if (flow == null)
            {
                flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 20260903u);
                combatMessage = "수식을 선택하고 현실에 작도하세요.";
            }

            LoadPresentationAssets();

            if (!ReferenceEquals(run, flow.CurrentRun))
            {
                run = flow.CurrentRun;
                BindCurrentBattle();
                statusText = null;
                formulaText = null;
            }
            else if (run != null
                && (!ReferenceEquals(game, run.CurrentBattle)
                || (game == null && (battle != null || deck != null)))
            )
            {
                BindCurrentBattle();
            }

            bool cacheWasMissing = characterLabels == null
                || statusText == null
                || formulaText == null
                || (run != null && mapNodeLabels == null)
                || (battle != null
                    && (cardLabels == null
                        || cardLayouts == null
                        || cardHoverAmounts == null
                        || playedCardLabels == null
                        || enemyStatusLabels == null
                        || previewLabels == null
                        || previewDamage == null
                        || rewardLabels == null
                        || relicRewardLabels == null));
            EnsurePresentationBuffers();

            if (circleTexture == null)
            {
                CreateCircleTexture();
            }
            if (combatMessage == null)
            {
                combatMessage = run != null && run.Phase == RunPhase.MapSelection
                    ? "진입할 작도 지점을 선택하세요."
                    : "수식을 선택하고 현실에 작도하세요.";
            }

            if (cacheWasMissing)
            {
                RefreshCachedText();
            }
        }

        private void Update()
        {
            EnsureInitialized();
            UpdateCardHoverAnimation(Time.unscaledDeltaTime);

            if (flow.Phase != GameFlowPhase.Run
                || run == null
                || run.Phase != RunPhase.Battle
                || battle == null
                || battle.Phase != BattlePhase.Plotting)
            {
                return;
            }

            if (Time.unscaledTime - plotStartedAt >= PlotDuration)
            {
                ResolvePlot();
            }
        }

        private void OnDestroy()
        {
            fieldLineRenderer?.Dispose();
            fieldLineRenderer = null;
            if (circleTexture != null)
            {
                Destroy(circleTexture);
                circleTexture = null;
            }

        }

        private void EnsureStyles()
        {
            if (titleStyle != null
                && formulaStyle != null
                && bodyStyle != null
                && smallStyle != null
                && centeredStyle != null
                && cardStyle != null
                && actionStyle != null
                && resultStyle != null
                && menuTitleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 27;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.83f, 0.87f, 1f);

            formulaStyle = new GUIStyle(GUI.skin.label);
            formulaStyle.fontSize = 21;
            formulaStyle.fontStyle = FontStyle.Bold;
            formulaStyle.alignment = TextAnchor.MiddleLeft;
            formulaStyle.normal.textColor = new Color(0.9f, 0.93f, 1f);

            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.fontSize = 16;
            bodyStyle.wordWrap = true;
            tooltipStyle = new GUIStyle(bodyStyle) { padding = new RectOffset(16, 16, 14, 14), fontSize = 15 };
            bodyStyle.normal.textColor = new Color(0.86f, 0.9f, 0.98f);

            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = 12;
            smallStyle.wordWrap = true;
            smallStyle.alignment = TextAnchor.MiddleCenter;
            smallStyle.normal.textColor = new Color(0.73f, 0.8f, 0.91f);

            centeredStyle = new GUIStyle(bodyStyle);
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            centeredStyle.fontStyle = FontStyle.Bold;

            cardStyle = new GUIStyle(GUI.skin.button);
            cardStyle.fontSize = 15;
            cardStyle.fontStyle = FontStyle.Bold;
            cardStyle.alignment = TextAnchor.MiddleCenter;
            cardStyle.wordWrap = true;
            cardStyle.padding = new RectOffset(6, 6, 6, 6);
            handCardStyle = new GUIStyle(cardStyle);
            handCardStyle.normal.background = null;
            handCardStyle.hover.background = null;
            handCardStyle.active.background = null;
            handCardStyle.normal.textColor = new Color(0.91f, 0.94f, 1f);
            handCardStyle.hover.textColor = Color.white;

            actionStyle = new GUIStyle(cardStyle);
            actionStyle.fontSize = 18;

            resultStyle = new GUIStyle(GUI.skin.label);
            resultStyle.fontSize = 34;
            resultStyle.fontStyle = FontStyle.Bold;
            resultStyle.alignment = TextAnchor.MiddleCenter;
            resultStyle.normal.textColor = new Color(0.85f, 0.87f, 1f);

            menuTitleStyle = new GUIStyle(resultStyle);
            menuTitleStyle.fontSize = 58;
        }

        private void OnGUI()
        {
            EnsureInitialized();
            EnsureStyles();

            float width = Screen.width;
            float height = Screen.height;
            DrawRect(new Rect(0f, 0f, width, height), new Color(0.025f, 0.035f, 0.07f));

            if (flow.Phase == GameFlowPhase.MainMenu)
            {
                DrawMainMenu(width, height);
                return;
            }

            if (flow.Phase == GameFlowPhase.CharacterSelection)
            {
                DrawCharacterSelection(width, height);
                return;
            }

            GUI.Label(new Rect(28f, 13f, 330f, 38f), "GRAPHACLYSM", titleStyle);
            GUI.Label(new Rect(360f, 17f, 350f, 26f), characterHudText, smallStyle);

            if (run.Phase == RunPhase.MapSelection)
            {
                DrawMapScreen(width, height);
                return;
            }

            if (battle == null || deck == null)
            {
                DrawRunOverlay(width, height);
                return;
            }

            GUI.Label(new Rect(width - 620f, 16f, 590f, 30f), statusText, centeredStyle);
            GUI.Label(new Rect(30f, 50f, width * 0.5f, 38f), formulaText, formulaStyle);
            DrawPlayedCardChain(new Rect(width * 0.52f, 50f, width * 0.46f, 38f));
            HandleUndoInput();
            DrawTacticalControls(width);

            Rect graphRect = new Rect(32f, 160f, width - 64f, Mathf.Max(200f, height - 510f));
            DrawGraphPanel(graphRect);

            float controlsTop = graphRect.yMax + 12f;
            GUI.Label(new Rect(32f, controlsTop, width - 360f, 25f), combatMessage, bodyStyle);

            bool isPlanning = battle.Phase == BattlePhase.PlayerPlanning;
            GUI.enabled = isPlanning && battle.Equation.HasBase;
            GUI.backgroundColor = new Color(0.12f, 0.75f, 0.88f);
            if (GUI.Button(new Rect(width - 202f, controlsTop - 3f, 170f, 38f), "작도 실행  ▶", actionStyle))
            {
                StartPlot();
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            DrawHand(controlsTop + 68f);
            DrawCardTooltip();
            DrawUltimateTooltip(width);

            if (run.Phase != RunPhase.Battle)
            {
                DrawRunOverlay(width, height);
            }
        }

        private void DrawGraphPanel(Rect rect)
        {
            GUI.DrawTexture(rect, titleBackground, ScaleMode.ScaleAndCrop);
            DrawRect(rect, new Color(0.055f, 0.075f, 0.15f, 0.9f));
            DrawRitualCircle(rect);
            DrawGrid(rect);
            DrawAttackZones(rect);

            if (battle.Equation.HasBase)
            {
                if (battle.Equation.IsFieldFunction)
                {
                    DrawPolynomiographLines(rect);
                }
                else
                {
                    DrawFunction(rect, 1f, new Color(0.2f, 0.75f, 0.85f, 0.32f), 2f);

                    if (battle.Phase == BattlePhase.Plotting)
                    {
                        float progress = Mathf.Clamp01((Time.unscaledTime - plotStartedAt) / PlotDuration);
                        DrawFunction(rect, progress, new Color(0.35f, 0.96f, 1f), 4f);
                    }
                }
            }

            DrawEnemies(rect);
            DrawPlayer(rect);
            if (battle.Phase == BattlePhase.Plotting)
            {
                float progress = Mathf.Clamp01((Time.unscaledTime - plotStartedAt) / PlotDuration);
                GUI.BeginGroup(rect);
                DrawWorldRing(new Rect(0, 0, rect.width, rect.height), 5, 0,
                    System.Math.Sqrt(41) * progress, new Color(0.75f, 0.78f, 1f, (1 - progress) * 0.65f));
                GUI.EndGroup();
            }

            if (!battle.Equation.HasBase)
            {
                GUI.Label(new Rect(rect.x, rect.center.y - 22f, rect.width, 44f),
                    "좌표 또는 각도 조각 → 연산·계수 → 효과 각인 → 작도", centeredStyle);
            }
        }

        private void DrawRitualCircle(Rect rect)
        {
            Vector2 center = GraphToScreen(rect, 5f, 0f);
            float radius = Mathf.Min(rect.height * 0.43f, rect.width * 0.24f);
            bool hasEquation = battle.Equation.HasBase;
            bool isPlotting = battle.Phase == BattlePhase.Plotting;
            int complexity = battle.Equation.ModifierCount;
            float pulse = isPlotting
                ? 0.75f + Mathf.Sin(Time.unscaledTime * 13f) * 0.2f
                : 0.42f;
            float activeAlpha = hasEquation ? 0.25f + complexity * 0.035f : 0.1f;
            Color outer = new Color(0.22f, 0.78f, 0.9f, activeAlpha * pulse);
            Color inner = new Color(0.46f, 0.34f, 0.9f, activeAlpha * 0.72f);
            float clockwise = Time.unscaledTime * (isPlotting ? 24f : 5f);
            float counterClockwise = -Time.unscaledTime * (isPlotting ? 17f : 3f);

            DrawRing(center, radius, 80, outer, hasEquation ? 1.8f : 1f);
            DrawRing(center, radius * 0.82f, 72, inner, 1f);
            DrawDashedRing(center, radius * 0.92f, 28, clockwise, outer, 1.5f);
            DrawDashedRing(center, radius * 0.68f, 20, counterClockwise, inner, 1f);

            int polygonSides = hasEquation ? 4 + complexity : 4;
            DrawRegularPolygon(
                center,
                radius * 0.55f,
                polygonSides,
                counterClockwise * 0.35f,
                inner,
                hasEquation ? 1.4f : 0.8f);

            int runeCount = 8 + complexity * 2;
            float innerRadius = radius * (hasEquation ? 0.61f : 0.72f);
            float outerRadius = radius * 0.79f;
            for (int i = 0; i < runeCount; i++)
            {
                float angle = (clockwise + 360f * i / runeCount) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                DrawLine(
                    center + direction * innerRadius,
                    center + direction * outerRadius,
                    i % 2 == 0 ? outer : inner,
                    hasEquation ? 1.3f : 0.7f);
            }

            DrawRing(center, radius * 0.16f, 40, outer, hasEquation ? 2f : 1f);
            if (hasEquation)
            {
                DrawDashedRing(
                    center,
                    radius * 0.3f,
                    12 + complexity * 2,
                    clockwise * 1.4f,
                    new Color(0.45f, 0.95f, 1f, activeAlpha * 0.9f),
                    1.5f);
            }
        }

        private static void DrawRing(
            Vector2 center,
            float radius,
            int segments,
            Color color,
            float thickness)
        {
            Vector2 previous = center + Vector2.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector2 current = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawLine(previous, current, color, thickness);
                previous = current;
            }
        }

        private static void DrawDashedRing(
            Vector2 center,
            float radius,
            int dashCount,
            float rotationDegrees,
            Color color,
            float thickness)
        {
            float step = Mathf.PI * 2f / dashCount;
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < dashCount; i++)
            {
                float startAngle = rotation + step * i;
                float endAngle = startAngle + step * 0.56f;
                Vector2 start = center
                    + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
                Vector2 end = center
                    + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius;
                DrawLine(start, end, color, thickness);
            }
        }

        private static void DrawRegularPolygon(
            Vector2 center,
            float radius,
            int sideCount,
            float rotationDegrees,
            Color color,
            float thickness)
        {
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            Vector2 first = center + new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * radius;
            Vector2 previous = first;
            for (int i = 1; i < sideCount; i++)
            {
                float angle = rotation + Mathf.PI * 2f * i / sideCount;
                Vector2 current = center
                    + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawLine(previous, current, color, thickness);
                previous = current;
            }

            DrawLine(previous, first, color, thickness);
        }

        private void DrawGrid(Rect rect)
        {
            Color thinGrid = new Color(0.14f, 0.22f, 0.34f);
            Color axis = new Color(0.34f, 0.52f, 0.68f);

            for (int x = 0; x <= 10; x++)
            {
                Vector2 top = GraphToScreen(rect, x, RangeMax);
                Vector2 bottom = GraphToScreen(rect, x, RangeMin);
                DrawLine(top, bottom, x == 5 ? axis : thinGrid, x == 5 ? 2f : 1f);
                GUI.Label(new Rect(bottom.x - 14f, rect.yMax - 20f, 28f, 18f), GridXLabels[x], smallStyle);
            }

            for (int y = -4; y <= 4; y++)
            {
                Vector2 left = GraphToScreen(rect, DomainMin, y);
                Vector2 right = GraphToScreen(rect, DomainMax, y);
                DrawLine(left, right, y == 0 ? axis : thinGrid, y == 0 ? 2f : 1f);
                GUI.Label(new Rect(rect.x + 2f, left.y - 10f, 28f, 18f), GridYLabels[y + 4], smallStyle);
            }
        }

        private void DrawFunction(Rect rect, float normalizedProgress, Color color, float thickness)
        {
            const int segments = GraphSegmentClipper.CurveSegments;
            battle.Equation.Sample(0, out double previousX, out double previousY);
            for (int i = 1; i <= segments; i++)
            {
                battle.Equation.Sample(i / (double)segments, out double currentX, out double currentY);
                double x0 = previousX, y0 = previousY, x1 = currentX, y1 = currentY;
                if (GraphSegmentClipper.ClipToField(ref x0, ref y0, ref x1, ref y1)
                    && GraphSegmentClipper.ClipReveal(normalizedProgress, ref x0, ref y0, ref x1, ref y1))
                    DrawLine(GraphToScreen(rect, (float)x0, (float)y0),
                        GraphToScreen(rect, (float)x1, (float)y1), color, thickness);
                previousX = currentX; previousY = currentY;
            }
        }

        private void DrawPolynomiographLines(Rect rect)
        {
#if UNITY_EDITOR
            long allocatedBefore = ProfileFieldLines ? System.GC.GetAllocatedBytesForCurrentThread() : 0;
            if (UseLegacyFieldLineRendering)
            {
                using (FieldLinesMarker.Auto())
                {
                    DrawPolynomiographLinesImmediate(rect);
                }
                if (ProfileFieldLines && Event.current.type == EventType.Repaint)
                    FieldLineDrawAllocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                return;
            }
#endif
            if (Event.current.type != EventType.Repaint) return;
            using (FieldLinesMarker.Auto())
            {
                bool plotting = battle.Phase == BattlePhase.Plotting;
                float progress = plotting
                    ? Mathf.Clamp01((Time.unscaledTime - plotStartedAt) / PlotDuration)
                    : 1f;
                int visibleSegments = battle.Equation.FieldLineSegmentCount;
                Color color = plotting
                    ? new Color(0.42f, 0.98f, 1f, 0.94f)
                    : new Color(0.28f, 0.8f, 0.92f, 0.42f);
                if (plotting) fieldLineRenderer.Draw(battle.Equation, rect, visibleSegments,
                    new Color(0.28f, 0.8f, 0.92f, 0.18f), 1f);
                fieldLineRenderer.Draw(battle.Equation, rect, visibleSegments, color, plotting ? 2.2f : 1.15f, progress);
            }
#if UNITY_EDITOR
            if (ProfileFieldLines)
                FieldLineDrawAllocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
#endif
        }

#if UNITY_EDITOR
        private void DrawPolynomiographLinesImmediate(Rect rect)
        {
            bool plotting = battle.Phase == BattlePhase.Plotting;
            float progress = plotting
                ? Mathf.Clamp01((Time.unscaledTime - plotStartedAt) / PlotDuration)
                : 1f;
            int totalSegments = battle.Equation.FieldLineSegmentCount;
            int visibleSegments = Mathf.CeilToInt(totalSegments * progress);
            Color color = plotting
                ? new Color(0.42f, 0.98f, 1f, 0.94f)
                : new Color(0.28f, 0.8f, 0.92f, 0.42f);
            float thickness = plotting ? 2.2f : 1.15f;

            for (int i = 0; i < visibleSegments; i++)
            {
                battle.Equation.GetFieldLineSegment(
                    i,
                    out double startX,
                    out double startY,
                    out double endX,
                    out double endY);
                DrawLine(
                    GraphToScreen(rect, (float)startX, (float)startY),
                    GraphToScreen(rect, (float)endX, (float)endY),
                    color,
                    thickness);
            }
        }
#endif

        private void DrawEnemies(Rect rect)
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                EnemyDefinition definition = enemy.Definition;
                Vector2 center = GraphToScreen(rect, (float)enemy.X, (float)enemy.Y);

                if (enemy.Intent.Kind == EnemyIntentKind.Reposition)
                {
                    Vector2 targetCenter = GraphToScreen(
                        rect,
                        (float)enemy.Intent.TargetX,
                        (float)enemy.Intent.TargetY);
                    DrawLine(
                        center,
                        targetCenter,
                    new Color(0.72f, 0.64f, 1f, 0.72f),
                        2f);
                    DrawRing(
                        targetCenter,
                        25f,
                        28,
                        new Color(0.88f, 0.7f, 0.32f, 0.72f),
                        1.5f);
                    GUI.Label(
                        new Rect(targetCenter.x - 58f, targetCenter.y - 13f, 116f, 26f),
                        "이동 예정",
                        smallStyle);
                }

                if (previewDamage[i] > 0)
                {
                    GUI.color = new Color(0.25f, 0.95f, 1f, 0.3f);
                    GUI.DrawTexture(new Rect(center.x - 31f, center.y - 31f, 62f, 62f), circleTexture);
                }

                GUI.color = new Color(0.96f, 0.3f, 0.34f);
                DrawWorldDisc(rect, enemy.X, enemy.Y, BattleSession.EnemyHitRadius, new Color(0.96f, 0.3f, 0.34f, 0.55f));
                DrawWorldRing(rect, enemy.X, enemy.Y, BattleSession.EnemyHitRadius, new Color(1f, 0.5f, 0.6f));
                GUI.color = Color.white;

                GUI.Label(new Rect(center.x - 75f, center.y - 51f, 150f, 24f),
                    definition.DisplayName, centeredStyle);
                GUI.Label(new Rect(center.x - 135f, center.y + 28f, 270f, 62f),
                    enemyStatusLabels[i], smallStyle);

                if (previewDamage[i] > 0 && battle.Phase == BattlePhase.PlayerPlanning)
                {
                    GUI.Label(new Rect(center.x - 70f, center.y - 2f, 140f, 22f),
                        previewLabels[i], centeredStyle);
                }
            }
        }

        private void DrawHand(float top)
        {
            CalculateCardLayouts(top);
            int hoveredIndex = FindHoveredCard(Event.current.mousePosition);
            hoveredHandIndex = hoveredIndex;
            int topmostIndex = FindTopmostAnimatedCard(hoveredIndex);

            for (int i = 0; i < deck.HandCount; i++)
            {
                if (i != topmostIndex)
                {
                    if (DrawCard(i, false))
                    {
                        return;
                    }
                }
            }

            if (topmostIndex >= 0)
            {
                if (DrawCard(topmostIndex, true))
                {
                    return;
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
        }

        private void CalculateCardLayouts(float top)
        {
            int count = deck.HandCount;
            if (count == 0)
            {
                return;
            }

            float cardWidth = Mathf.Clamp(Screen.width * 0.1f, 138f, 190f);
            float cardHeight = Mathf.Clamp(Screen.height * 0.2f, 178f, 225f);
            float spacing = cardWidth * 0.69f;
            float totalWidth = cardWidth + spacing * (count - 1);
            float firstX = (Screen.width - totalWidth) * 0.5f;
            float centerIndex = (count - 1) * 0.5f;
            top = Mathf.Min(top, Screen.height - cardHeight - 65f);

            for (int i = 0; i < count; i++)
            {
                float offset = i - centerIndex;
                float verticalDrop = offset * offset * 3.5f;
                cardLayouts[i].Rect = new Rect(
                    firstX + spacing * i,
                    top + verticalDrop,
                    cardWidth,
                    cardHeight);
                cardLayouts[i].Angle = offset * 5f;
            }
        }

        private int FindHoveredCard(Vector2 mousePosition)
        {
#if UNITY_EDITOR
            if (DiagnosticHoveredCard >= 0 && DiagnosticHoveredCard < deck.HandCount) return DiagnosticHoveredCard;
#endif
            if (hoveredHandIndex >= 0 && hoveredHandIndex < deck.HandCount)
            {
                GetAnimatedCardTransform(hoveredHandIndex, out Rect animatedRect, out float animatedAngle);
                CardLayout currentLayout = cardLayouts[hoveredHandIndex];
                if (ContainsRotatedPoint(animatedRect, animatedAngle, mousePosition)
                    || ContainsRotatedPoint(currentLayout.Rect, currentLayout.Angle, mousePosition))
                {
                    return hoveredHandIndex;
                }
            }

            for (int i = deck.HandCount - 1; i >= 0; i--)
            {
                CardLayout layout = cardLayouts[i];
                if (ContainsRotatedPoint(layout.Rect, layout.Angle, mousePosition))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindTopmostAnimatedCard(int hoveredIndex)
        {
            if (hoveredIndex >= 0)
            {
                return hoveredIndex;
            }

            int topmost = -1;
            float strongestAmount = 0.001f;
            for (int i = 0; i < deck.HandCount; i++)
            {
                if (cardHoverAmounts[i] > strongestAmount)
                {
                    strongestAmount = cardHoverAmounts[i];
                    topmost = i;
                }
            }

            return topmost;
        }

        private void GetAnimatedCardTransform(int handIndex, out Rect drawRect, out float angle)
        {
            CardLayout layout = cardLayouts[handIndex];
            float amount = cardHoverAmounts[handIndex];
            float eased = 1f - Mathf.Pow(1f - amount, 3f);
            drawRect = layout.Rect;
            // IMGUI's positive Y points down; lifting a card therefore subtracts Y.
            Vector2 center = drawRect.center + Vector2.down * (72f * eased);
            float scale = 1f + 0.2f * eased;
            drawRect.width *= scale;
            drawRect.height *= scale;
            drawRect.center = center;
            angle = Mathf.Lerp(layout.Angle, 0f, eased * 0.92f);
        }

        private bool DrawCard(int handIndex, bool isTopmost)
        {
            CardDefinition card = deck.GetHandCard(handIndex);
            bool canUse = battle.CanPlayCard(card, out _);

            GetAnimatedCardTransform(handIndex, out Rect drawRect, out float angle);
            float hoverAmount = cardHoverAmounts[handIndex];

            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, drawRect.center);

            Color rarityColor = GetRarityColor(card.DiagramRarity);
            DrawRect(new Rect(drawRect.x - 3f, drawRect.y - 3f, drawRect.width + 6f, drawRect.height + 6f), rarityColor);
            Color baseColor = new Color(0.22f, 0.28f, 0.43f);

            if (!canUse)
            {
                baseColor *= 0.48f;
                baseColor.a = 1f;
            }

            if (hoverAmount > 0.01f || isTopmost && hoveredHandIndex == handIndex)
            {
                DrawRect(
                    new Rect(drawRect.x - 4f, drawRect.y - 4f, drawRect.width + 8f, drawRect.height + 8f),
                    canUse
                        ? new Color(0.35f, 0.95f, 1f, Mathf.Max(0.15f, hoverAmount))
                        : new Color(0.42f, 0.48f, 0.56f, Mathf.Max(0.15f, hoverAmount)));
                baseColor = Color.Lerp(
                    baseColor,
                    Color.white,
                    hoverAmount * (canUse ? 0.25f : 0.08f));
            }

            DrawRect(drawRect, new Color(0.09f, 0.12f, 0.2f));
            DrawRect(new Rect(drawRect.x, drawRect.y, drawRect.width, 5), rarityColor);
            GUI.enabled = true;
            GUI.backgroundColor = baseColor;

            if (GUI.Button(drawRect, cardLabels[handIndex], handCardStyle))
            {
                GUI.matrix = oldMatrix;
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
                PlayCard(handIndex);
                if (canUse) ResetCardHoverAnimation();
                return true;
            }

            GUI.matrix = oldMatrix;
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            return false;
        }

        private static bool ContainsRotatedPoint(Rect rect, float angle, Vector2 point)
        {
            Vector2 offset = point - rect.center;
            float radians = -angle * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            Vector2 local = new Vector2(
                offset.x * cosine - offset.y * sine,
                offset.x * sine + offset.y * cosine);

            return Mathf.Abs(local.x) <= rect.width * 0.5f
                && Mathf.Abs(local.y) <= rect.height * 0.5f;
        }

        private void UpdateCardHoverAnimation(float deltaTime)
        {
            if (cardHoverAmounts == null)
            {
                return;
            }

            int handCount = deck == null ? 0 : deck.HandCount;
            for (int i = 0; i < cardHoverAmounts.Length; i++)
            {
                float target = i == hoveredHandIndex && i < handCount ? 1f : 0f;
                float speed = target > cardHoverAmounts[i]
                    ? CardHoverInSpeed
                    : CardHoverOutSpeed;
                float blend = 1f - Mathf.Exp(-speed * deltaTime);
                cardHoverAmounts[i] = Mathf.Lerp(cardHoverAmounts[i], target, blend);

                if (cardHoverAmounts[i] < 0.001f)
                {
                    cardHoverAmounts[i] = 0f;
                }
                else if (cardHoverAmounts[i] > 0.999f)
                {
                    cardHoverAmounts[i] = 1f;
                }
            }
        }

        private void ResetCardHoverAnimation()
        {
            hoveredHandIndex = -1;
            if (cardHoverAmounts == null)
            {
                return;
            }

            for (int i = 0; i < cardHoverAmounts.Length; i++)
            {
                cardHoverAmounts[i] = 0f;
            }
        }

        private void DrawPlayedCardChain(Rect area)
        {
            if (game == null || game.PlayedCardCount == 0)
            {
                return;
            }

            int count = game.PlayedCardCount;
            float gap = 6f;
            float chipWidth = Mathf.Min(112f, (area.width - gap * (count - 1)) / count);
            float totalWidth = chipWidth * count + gap * (count - 1);
            float x = area.xMax - totalWidth;

            for (int i = 0; i < count; i++)
            {
                Rect chip = new Rect(x + i * (chipWidth + gap), area.y + 2f, chipWidth, area.height - 4f);
                bool isLast = i == count - 1;
                CardDefinition card = game.GetPlayedCard(i);
                GUI.backgroundColor = card.IsBaseFunction
                    ? new Color(0.24f, 0.48f, 0.78f)
                    : new Color(0.48f, 0.28f, 0.68f);

                if (isLast)
                {
                    if (GUI.Button(chip, playedCardLabels[i], cardStyle))
                    {
                        UndoLastCard();
                        return;
                    }
                }
                else
                {
                    DrawRect(chip, GUI.backgroundColor);
                    GUI.Label(chip, playedCardLabels[i], smallStyle);
                }
            }

            GUI.backgroundColor = Color.white;
        }

        private void HandleUndoInput()
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown
                || current.button != 1
                || battle.Phase != BattlePhase.PlayerPlanning)
            {
                return;
            }

            if (UndoLastCard())
            {
                current.Use();
            }
        }

        private bool UndoLastCard()
        {
            if (!run.TryUndoLastPlayedCard(out CardDefinition restoredCard))
            {
                return false;
            }

            ResetCardHoverAnimation();
            combatMessage = restoredCard.DisplayName
                + " 선택을 취소했습니다. 우클릭하면 이전 카드도 취소할 수 있습니다.";
            RefreshCachedText();
            return true;
        }

        private void PlayCard(int handIndex)
        {
            CardDefinition card;
            CardPlayFailure failure;
            if (!run.TryPlayHandCard(handIndex, out card, out failure))
            {
                combatMessage = GetFailureMessage(failure);
                return;
            }

            combatMessage = card.IsBaseFunction
                ? card.DisplayName + "을 입력으로 선택했습니다."
                : card.DisplayName + " 조각을 연결했습니다.";
            RefreshCachedText();
        }

        private void StartPlot()
        {
            if (!run.TryBeginPlot())
            {
                return;
            }

            plotStartedAt = Time.unscaledTime;
            combatMessage = "수식을 현실에 작도하는 중...";
        }

        private void ResolvePlot()
        {
            PlotReport report = run.ResolvePlot();
            if (run.Phase == RunPhase.CardReward)
            {
                combatMessage = "전투 승리. 덱에 추가할 카드 한 장을 선택하세요.";
                RefreshCachedText();
                return;
            }

            if (run.Phase == RunPhase.RelicReward)
            {
                combatMessage = "정예 전투 승리. 좌표계에 결속할 유물 하나를 선택하세요.";
                RefreshCachedText();
                return;
            }

            if (run.Phase == RunPhase.Completed)
            {
                combatMessage = "현재 프로토타입 런을 완주했습니다.";
                RefreshCachedText();
                return;
            }

            int incomingDamage = run.ResolveEnemyTurn();
            if (run.Phase == RunPhase.Defeated)
            {
                combatMessage = "좌표계가 붕괴했습니다.";
            }
            else if (report.HitCount > 0)
            {
                combatMessage = report.HitCount + "개 대상에 총 " + report.TotalDamage
                    + " 피해. 적의 반격으로 " + incomingDamage + " 피해를 받았습니다.";
            }
            else
            {
                combatMessage = "적을 빗나갔습니다. 적의 반격으로 "
                    + incomingDamage + " 피해를 받았습니다.";
            }

            if (report.PlayerHit) combatMessage += "  자신 적중 · 보호막 +" + report.Shield + " / 회복 " + report.Healing;
            if (run.Phase == RunPhase.CardReward || run.Phase == RunPhase.RelicReward || run.Phase == RunPhase.Completed)
                combatMessage = "잔불로 전투 승리. 다음 여정을 준비하세요.";
            RefreshCachedText();
        }

        private void ResetBattle()
        {
            if (!flow.TryRestartRun())
            {
                return;
            }

            run = flow.CurrentRun;
            BindCurrentBattle();
            combatMessage = "진입할 작도 지점을 선택하세요.";
            RefreshCachedText();
        }

        private void BindCurrentBattle()
        {
            if (run == null)
            {
                game = null;
                battle = null;
                deck = null;
                EnsurePresentationBuffers();
                return;
            }

            game = run.CurrentBattle;
            if (game == null)
            {
                battle = null;
                deck = null;
                EnsurePresentationBuffers();
                return;
            }

            battle = game.Battle;
            deck = game.Deck;

            EnsurePresentationBuffers();
            ResetCardHoverAnimation();
        }

        private void EnsurePresentationBuffers()
        {
            if (flow != null)
            {
                int characterCount = flow.CharacterCount;
                if (characterLabels == null || characterLabels.Length != characterCount)
                {
                    characterLabels = new string[characterCount];
                }
            }

            if (run != null)
            {
                int mapNodeCount = run.Map.Definition.NodeCount;
                if (mapNodeLabels == null || mapNodeLabels.Length != mapNodeCount)
                {
                    mapNodeLabels = new string[mapNodeCount];
                }
            }

            if (battle == null || deck == null)
            {
                return;
            }

            if (cardLabels == null
                || cardLayouts == null
                || cardHoverAmounts == null
                || cardLabels.Length != deck.HandCapacity
                || cardLayouts.Length != deck.HandCapacity
                || cardHoverAmounts.Length != deck.HandCapacity)
            {
                cardLabels = new string[deck.HandCapacity];
                cardLayouts = new CardLayout[deck.HandCapacity];
                cardHoverAmounts = new float[deck.HandCapacity];
            }

            EnsureTacticalBuffers();

            int playedCapacity = BattleSession.MaximumPlayedCards;
            if (playedCardLabels == null || playedCardLabels.Length != playedCapacity)
            {
                playedCardLabels = new string[playedCapacity];
            }

            if (enemyStatusLabels == null || enemyStatusLabels.Length != battle.Enemies.Count)
            {
                enemyStatusLabels = new string[battle.Enemies.Count];
                previewLabels = new string[battle.Enemies.Count];
                previewDamage = new int[battle.Enemies.Count];
            }

            if (rewardLabels == null || rewardLabels.Length != RunGameSession.RewardOptionCount)
            {
                rewardLabels = new string[RunGameSession.RewardOptionCount];
            }

            if (relicRewardLabels == null
                || relicRewardLabels.Length != RunGameSession.RelicRewardOptionCount)
            {
                relicRewardLabels = new string[RunGameSession.RelicRewardOptionCount];
            }
        }

        private void RefreshCachedText()
        {
            RefreshCharacterLabels();
            if (flow == null || flow.Phase != GameFlowPhase.Run || run == null)
            {
                statusText = string.Empty;
                formulaText = string.Empty;
                characterHudText = string.Empty;
                return;
            }

            characterHudText = flow.CurrentCharacter.DisplayName
                + " · " + flow.CurrentCharacter.Title;
            RefreshMapLabels();
            if (battle == null || deck == null)
            {
                statusText = "HP " + run.PlayerHealth + "/" + run.PlayerMaxHealth
                    + "    DECK " + run.Deck.Count
                    + "    RELIC " + run.Relics.Count;
                formulaText = "다음 작도 지점을 선택하세요";
                return;
            }

            statusText = "TURN " + battle.Turn + "     HP " + battle.PlayerHealth + "/" + battle.PlayerMaxHealth
                + "     ENERGY " + battle.Energy + "/" + battle.PlayerMaxEnergy
                + "     DECK " + run.Deck.Count;
            formulaText = battle.Equation.BuildFormula();
            RefreshTacticalText();

            for (int i = 0; i < playedCardLabels.Length; i++)
            {
                playedCardLabels[i] = string.Empty;
            }

            for (int i = 0; i < game.PlayedCardCount; i++)
            {
                CardDefinition played = game.GetPlayedCard(i);
                playedCardLabels[i] = i == game.PlayedCardCount - 1
                    ? "↩ " + played.DisplayName
                    : played.DisplayName;
            }

            for (int i = 0; i < cardLabels.Length; i++)
            {
                cardLabels[i] = string.Empty;
            }

            for (int i = 0; i < deck.HandCount; i++)
            {
                CardDefinition card = deck.GetHandCard(i);
                string prefix = GetCardRole(card.Type);
                cardLabels[i] = prefix
                    + " · " + card.Cost + " EN\n\n"
                    + card.DisplayName + "\n" + card.FormulaLabel + "\n\n" + CompactDescription(card);
            }

            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i];
                enemyStatusLabels[i] = enemy.Health + "/" + enemy.Definition.MaxHealth
                    + "  |  " + BuildEnemyIntentLabel(enemy) + BuildStatuses(enemy.Statuses);
                previewDamage[i] = battle.PreviewDamage(enemy);
                previewLabels[i] = previewDamage[i] > 0 ? "HIT " + previewDamage[i] : string.Empty;
            }

            if (rewardLabels != null && run.Phase == RunPhase.CardReward)
            {
                for (int i = 0; i < rewardLabels.Length; i++)
                {
                    CardDefinition reward = run.RewardOptions[i];
                    string prefix = GetCardRole(reward.Type);
                    rewardLabels[i] = prefix
                        + " · " + reward.Cost + " EN\n\n"
                        + reward.DisplayName + "\n" + reward.FormulaLabel + "\n\n"
                        + reward.Description + "\n적: " + reward.EnemyEffect + "\n자신: " + reward.PlayerEffect;
                }
            }

            if (relicRewardLabels != null && run.Phase == RunPhase.RelicReward)
            {
                for (int i = 0; i < relicRewardLabels.Length; i++)
                {
                    RelicDefinition relic = run.RelicRewardOptions[i];
                    relicRewardLabels[i] = relic.DisplayName + "\n\n" + relic.Description;
                }
            }
        }

        private void RefreshCharacterLabels()
        {
            if (flow == null || characterLabels == null)
            {
                return;
            }

            for (int i = 0; i < characterLabels.Length; i++)
            {
                CharacterDefinition character = flow.GetCharacter(i);
                characterLabels[i] = character.DisplayName + "\n"
                    + character.Title + "\n\n"
                    + "HP " + character.MaxHealth
                    + "    ENERGY " + character.MaxEnergy
                    + "    HAND " + character.HandSize + "\n\n"
                    + character.PlayStyle;
            }
        }

        private static string BuildEnemyIntentLabel(EnemyState enemy)
        {
            EnemyIntent intent = enemy.Intent;
            switch (intent.Kind)
            {
                case EnemyIntentKind.Charge:
                    return "축적 · 보호막 12 / 다음 턴 광역 공격";
                case EnemyIntentKind.Reposition:
                    return "의도: 전이 (" + intent.TargetX.ToString("0.#")
                        + ", " + intent.TargetY.ToString("0.#") + ")";
                default:
                    return "공격 " + System.Math.Max(0, intent.Damage - enemy.Statuses.Get(CombatStatusKind.Weaken))
                        + (enemy.Definition.Behavior.Kind == EnemyBehaviorKind.ChargeBurst ? " · 잔불 2" : " · 약화 1");
            }
        }

        private void RefreshMapLabels()
        {
            if (mapNodeLabels == null)
            {
                return;
            }

            RunMapDefinition definition = run.Map.Definition;
            for (int i = 0; i < mapNodeLabels.Length; i++)
            {
                RunMapNodeDefinition node = definition.GetNode(i);
                string kind;
                switch (node.Kind)
                {
                    case RunNodeKind.Elite:
                        kind = "정예";
                        break;
                    case RunNodeKind.Boss:
                        kind = "보스";
                        break;
                    default:
                        kind = "전투";
                        break;
                }

                mapNodeLabels[i] = kind + "\n" + node.DisplayName;
            }
        }

        private static string GetFailureMessage(CardPlayFailure failure)
        {
            switch (failure)
            {
                case CardPlayFailure.MissingBaseFunction:
                    return "먼저 좌표 또는 각도 조각을 선택하세요.";
                case CardPlayFailure.BaseFunctionAlreadySelected:
                    return "입력 조각은 하나만 사용합니다. 우클릭으로 되돌릴 수 있습니다.";
                case CardPlayFailure.NotEnoughEnergy:
                    return "에너지가 부족합니다.";
                case CardPlayFailure.DuplicateOrFull:
                    return "동일 각인 중복 또는 조립 한도(연산 8개)를 확인하세요.";
                case CardPlayFailure.InscriptionLimit:
                    return "각인과 유틸은 합쳐서 2개까지 연결할 수 있습니다.";
                case CardPlayFailure.WrongPhase:
                    return "지금은 카드를 사용할 수 없습니다.";
                default:
                    return "카드를 사용할 수 없습니다.";
            }
        }

        private void DrawMainMenu(float width, float height)
        {
            DrawFrontEndBackdrop(width, height, 0.42f);
            Vector2 center = new Vector2(width * 0.5f, height * 0.43f);
            float radius = Mathf.Min(width, height) * 0.27f;
            float rotation = Time.unscaledTime * 5f;
            DrawMenuRitualCircle(center, radius, rotation, 0);

            GUI.Label(new Rect(0f, height * 0.24f, width, 82f), "GRAPHACLYSM", menuTitleStyle);
            GUI.Label(new Rect(0f, height * 0.24f + 72f, width, 36f),
                "EQUATIONS BECOME RITUALS", centeredStyle);
            GUI.Label(new Rect(0f, height * 0.24f + 105f, width, 30f),
                "수식을 엮어 붕괴한 좌표계를 돌파하세요.", centeredStyle);

            float buttonWidth = 250f;
            float buttonX = (width - buttonWidth) * 0.5f;
            float buttonY = height * 0.67f;
            GUI.backgroundColor = new Color(0.43f, 0.48f, 0.72f);
            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, 54f),
                    "새로운 작도 시작", actionStyle))
            {
                flow.OpenCharacterSelection();
                RefreshCachedText();
            }

            GUI.backgroundColor = new Color(0.2f, 0.24f, 0.34f);
            GUI.Label(new Rect(buttonX, buttonY + 68f, buttonWidth, 32f),
                "전투 조립 프로토타입 · v0.5", smallStyle);
            GUI.backgroundColor = Color.white;
        }

        private void DrawCharacterSelection(float width, float height)
        {
            DrawFrontEndBackdrop(width, height, 0.46f);
            GUI.Label(new Rect(0f, 35f, width, 58f), "CHOOSE THE PLOTTER", resultStyle);
            GUI.Label(new Rect(0f, 87f, width, 30f),
                "작도사의 체력, 에너지와 시작 덱이 달라집니다.", centeredStyle);

            float cardWidth = Mathf.Min(440f, width * 0.36f);
            float cardHeight = Mathf.Min(720f, height - 245f);
            const float gap = 70f;
            float totalWidth = cardWidth * flow.CharacterCount + gap * (flow.CharacterCount - 1);
            float firstX = (width - totalWidth) * 0.5f;
            float cardY = 135f;

            for (int i = 0; i < flow.CharacterCount; i++)
            {
                Rect card = new Rect(firstX + i * (cardWidth + gap), cardY, cardWidth, cardHeight);
                bool selected = flow.SelectedCharacterIndex == i;
                if (selected)
                {
                    DrawRect(
                        new Rect(card.x - 5f, card.y - 5f, card.width + 10f, card.height + 10f),
                        new Color(0.72f, 0.76f, 1f));
                }

                GUI.backgroundColor = i == 0
                    ? new Color(0.2f, 0.42f, 0.68f)
                    : new Color(0.48f, 0.25f, 0.67f);
                if (GUI.Button(card, string.Empty, cardStyle))
                {
                    flow.TrySelectCharacter(i);
                }

                DrawCharacterEmblem(
                    new Rect(card.x + 24f, card.y + 14f, card.width - 48f, card.height * 0.6f),
                    i,
                    selected);
                GUI.Label(
                    new Rect(card.x + 24f, card.y + card.height * 0.61f,
                        card.width - 48f, card.height * 0.36f),
                    characterLabels[i],
                    centeredStyle);
            }

            float controlsY = height - 78f;
            GUI.backgroundColor = new Color(0.25f, 0.3f, 0.4f);
            if (GUI.Button(new Rect(34f, controlsY, 150f, 42f), "타이틀로", actionStyle))
            {
                flow.ReturnToMainMenu();
                RefreshCachedText();
                return;
            }

            GUI.backgroundColor = new Color(0.43f, 0.48f, 0.72f);
            if (GUI.Button(new Rect(width - 244f, controlsY, 210f, 42f),
                    "이 작도사로 시작", actionStyle))
            {
                StartSelectedCharacterRun();
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawCharacterEmblem(Rect area, int characterIndex, bool selected)
        {
            Vector2 center = new Vector2(area.center.x, area.center.y - 4f);
            float radius = Mathf.Min(area.width, area.height) * 0.42f;
            Color color = characterIndex == 0
                ? new Color(0.35f, 0.9f, 1f, selected ? 0.9f : 0.58f)
                : new Color(0.78f, 0.48f, 1f, selected ? 0.9f : 0.58f);
            float rotation = Time.unscaledTime * (characterIndex == 0 ? 5f : -8f);

            DrawRing(center, radius, 64, color, selected ? 2f : 1f);
            DrawDashedRing(center, radius * 0.8f, characterIndex == 0 ? 12 : 18,
                rotation, color, 1.5f);
            DrawRegularPolygon(center, radius * 0.58f, characterIndex == 0 ? 6 : 5,
                -rotation * 0.55f, color, 1.5f);

            Texture2D portrait = characterPortraits != null
                && characterIndex >= 0
                && characterIndex < characterPortraits.Length
                    ? characterPortraits[characterIndex]
                    : null;
            if (portrait != null)
            {
                GUI.DrawTexture(area, portrait, ScaleMode.ScaleToFit, true);
                return;
            }

            if (circleTexture != null)
            {
                GUI.color = color;
                GUI.DrawTexture(new Rect(center.x - 22f, center.y - 55f, 44f, 44f), circleTexture);
                GUI.color = Color.white;
            }

            Vector2 shoulderLeft = center + new Vector2(-42f, 35f);
            Vector2 shoulderRight = center + new Vector2(42f, 35f);
            DrawLine(center + new Vector2(0f, -6f), center + new Vector2(0f, 58f), color, 5f);
            DrawLine(shoulderLeft, center + new Vector2(0f, 4f), color, 4f);
            DrawLine(center + new Vector2(0f, 4f), shoulderRight, color, 4f);
        }

        private static void DrawMenuRitualCircle(
            Vector2 center,
            float radius,
            float rotation,
            int complexity)
        {
            Color gold = new Color(0.74f, 0.8f, 1f, 0.25f);
            Color ultramarine = new Color(0.2f, 0.42f, 0.82f, 0.13f);
            DrawRing(center, radius, 90, gold, 2f);
            DrawRing(center, radius * 0.72f, 72, ultramarine, 1.5f);
            DrawDashedRing(center, radius * 0.9f, 32, rotation, gold, 2f);
            DrawDashedRing(center, radius * 0.55f, 20, -rotation * 1.4f, ultramarine, 1.5f);
            DrawRegularPolygon(center, radius * 0.43f, 6 + complexity,
                rotation * 0.45f, gold, 1.5f);
        }

        private void DrawFrontEndBackdrop(float width, float height, float darkness)
        {
            Rect screenRect = new Rect(0f, 0f, width, height);
            if (titleBackground != null)
            {
                GUI.DrawTexture(screenRect, titleBackground, ScaleMode.ScaleAndCrop, true);
            }

            DrawRect(screenRect, new Color(0.015f, 0.012f, 0.025f, darkness));
        }

        private void LoadPresentationAssets()
        {
            if (presentationAssetsLoaded)
            {
                return;
            }

            titleBackground = Resources.Load<Texture2D>(
                "Art/Generated/graphaclysm-title-background-v2");
            characterPortraits = new Texture2D[2];
            characterPortraits[0] = Resources.Load<Texture2D>(
                "Art/Generated/ian-character-portrait-v2");
            characterPortraits[1] = Resources.Load<Texture2D>(
                "Art/Generated/luna-character-portrait-v2");
            presentationAssetsLoaded = true;
        }

        private void StartSelectedCharacterRun()
        {
            if (!flow.TryStartRun())
            {
                return;
            }

            run = flow.CurrentRun;
            BindCurrentBattle();
            combatMessage = flow.CurrentCharacter.DisplayName
                + "의 작도가 시작됩니다. 진입 지점을 선택하세요.";
            statusText = null;
            formulaText = null;
            RefreshCachedText();
        }

        private void ReturnToMainMenu()
        {
            flow.ReturnToMainMenu();
            run = null;
            BindCurrentBattle();
            combatMessage = "수식을 선택하고 현실에 작도하세요.";
            statusText = null;
            formulaText = null;
            RefreshCachedText();
        }

        private void ReturnToCharacterSelection()
        {
            flow.ReturnToCharacterSelection();
            run = null;
            BindCurrentBattle();
            combatMessage = "다음 작도사를 선택하세요.";
            statusText = null;
            formulaText = null;
            RefreshCachedText();
        }

        private void DrawMapScreen(float width, float height)
        {
            GUI.Label(new Rect(width - 420f, 16f, 390f, 30f), statusText, centeredStyle);
            GUI.Label(new Rect(30f, 58f, width - 60f, 44f), "RITUAL ROUTE", resultStyle);
            GUI.Label(new Rect(30f, 98f, width - 60f, 30f),
                "연결된 작도 지점 중 다음 조우를 선택하세요.", centeredStyle);

            Rect panel = new Rect(
                Mathf.Max(40f, width * 0.12f),
                142f,
                Mathf.Min(width - 80f, width * 0.76f),
                Mathf.Max(300f, height - 235f));
            panel.x = (width - panel.width) * 0.5f;
            DrawRect(panel, new Color(0.035f, 0.055f, 0.11f, 0.96f));

            RunMapDefinition definition = run.Map.Definition;
            RunMapProgress progress = run.Map;

            for (int i = 0; i < definition.NodeCount; i++)
            {
                RunMapNodeDefinition source = definition.GetNode(i);
                Vector2 sourceCenter = GetMapNodeCenter(panel, source, definition);
                for (int edge = 0; edge < source.NextNodeCount; edge++)
                {
                    int targetIndex = source.GetNextNodeIndex(edge);
                    RunMapNodeDefinition target = definition.GetNode(targetIndex);
                    Vector2 targetCenter = GetMapNodeCenter(panel, target, definition);
                    bool activeRoute = progress.IsCompleted(i)
                        && (progress.IsAvailable(targetIndex) || progress.IsCompleted(targetIndex));
                    DrawLine(
                        sourceCenter,
                        targetCenter,
                        activeRoute
                            ? new Color(0.25f, 0.85f, 0.92f)
                            : new Color(0.15f, 0.23f, 0.34f),
                        activeRoute ? 3f : 2f);
                }
            }

            for (int i = 0; i < definition.NodeCount; i++)
            {
                RunMapNodeDefinition node = definition.GetNode(i);
                Vector2 center = GetMapNodeCenter(panel, node, definition);
                Rect nodeRect = new Rect(center.x - 78f, center.y - 31f, 156f, 62f);
                bool available = progress.IsAvailable(i);
                bool completed = progress.IsCompleted(i);

                Color nodeColor;
                if (completed)
                {
                    nodeColor = new Color(0.16f, 0.55f, 0.48f);
                }
                else if (!available)
                {
                    nodeColor = new Color(0.14f, 0.18f, 0.27f);
                }
                else if (node.Kind == RunNodeKind.Boss)
                {
                    nodeColor = new Color(0.78f, 0.23f, 0.32f);
                }
                else if (node.Kind == RunNodeKind.Elite)
                {
                    nodeColor = new Color(0.62f, 0.34f, 0.76f);
                }
                else
                {
                    nodeColor = new Color(0.18f, 0.61f, 0.75f);
                }

                if (available)
                {
                    bool hovered = nodeRect.Contains(Event.current.mousePosition);
                    if (hovered)
                    {
                        DrawRect(
                            new Rect(nodeRect.x - 4f, nodeRect.y - 4f,
                                nodeRect.width + 8f, nodeRect.height + 8f),
                            new Color(0.42f, 0.96f, 1f));
                    }

                    GUI.backgroundColor = nodeColor;
                    if (GUI.Button(nodeRect, mapNodeLabels[i], cardStyle))
                    {
                        SelectMapNode(i);
                        return;
                    }
                }
                else
                {
                    DrawRect(nodeRect, nodeColor);
                    GUI.Label(nodeRect, mapNodeLabels[i], centeredStyle);
                }
            }

            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(panel.x, panel.yMax + 13f, panel.width, 26f), combatMessage, centeredStyle);
        }

        private static Vector2 GetMapNodeCenter(
            Rect panel,
            RunMapNodeDefinition node,
            RunMapDefinition definition)
        {
            float x = panel.x + panel.width * (node.Lane + 1f) / (definition.LaneCount + 1f);
            float y = panel.yMax - panel.height * (node.Layer + 0.65f) / definition.LayerCount;
            return new Vector2(x, y);
        }

        private void SelectMapNode(int nodeIndex)
        {
            RunMapNodeDefinition selected = run.Map.Definition.GetNode(nodeIndex);
            if (!run.TrySelectMapNode(nodeIndex))
            {
                return;
            }

            BindCurrentBattle();
            combatMessage = selected.DisplayName + "에 진입했습니다. 수식을 조합하세요.";
            RefreshCachedText();
        }

        private void DrawRunOverlay(float width, float height)
        {
            if (run.Phase == RunPhase.CardReward)
            {
                DrawRewardOverlay(width, height);
                return;
            }

            if (run.Phase == RunPhase.RelicReward)
            {
                DrawRelicRewardOverlay(width, height);
                return;
            }

            bool won = run.Phase == RunPhase.Completed;
            Rect overlay = new Rect(width * 0.5f - 240f, height * 0.5f - 100f, 480f, 200f);
            DrawRect(overlay, new Color(0.025f, 0.04f, 0.09f, 0.97f));
            GUI.Label(new Rect(overlay.x, overlay.y + 26f, overlay.width, 54f),
                won ? "SYSTEM STABILIZED" : "COORDINATE COLLAPSED", resultStyle);
            GUI.Label(new Rect(overlay.x + 30f, overlay.y + 85f, overlay.width - 60f, 30f),
                won ? "세 개의 조우를 모두 돌파했습니다." : "다른 함수 조합을 시험해 보세요.",
                centeredStyle);

            GUI.backgroundColor = new Color(0.15f, 0.72f, 0.82f);
            if (GUI.Button(new Rect(overlay.center.x - 185f, overlay.y + 132f, 175f, 42f),
                    "런 다시 시작", actionStyle))
            {
                ResetBattle();
            }

            GUI.backgroundColor = new Color(0.4f, 0.3f, 0.58f);
            if (GUI.Button(new Rect(overlay.center.x + 10f, overlay.y + 132f, 175f, 42f),
                    "캐릭터 변경", actionStyle))
            {
                ReturnToCharacterSelection();
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawRewardOverlay(float width, float height)
        {
            Rect overlay = new Rect(width * 0.5f - 500f, height * 0.5f - 260f, 1000f, 520f);
            DrawRect(overlay, new Color(0.025f, 0.04f, 0.09f, 0.98f));
            GUI.Label(new Rect(overlay.x, overlay.y + 22f, overlay.width, 50f),
                "CHOOSE AN EQUATION FRAGMENT", resultStyle);
            GUI.Label(new Rect(overlay.x, overlay.y + 68f, overlay.width, 28f),
                "선택한 카드는 이번 런의 덱에 영구적으로 추가됩니다.", centeredStyle);

            const float cardWidth = 285f;
            const float cardHeight = 320f;
            const float gap = 34f;
            float firstX = overlay.center.x - (cardWidth * 3f + gap * 2f) * 0.5f;
            float cardY = overlay.y + 112f;

            for (int i = 0; i < RunGameSession.RewardOptionCount; i++)
            {
                CardDefinition reward = run.RewardOptions[i];
                Rect cardRect = new Rect(firstX + i * (cardWidth + gap), cardY, cardWidth, cardHeight);
                bool hovered = cardRect.Contains(Event.current.mousePosition);
                DrawRect(new Rect(cardRect.x - 3, cardRect.y - 3, cardRect.width + 6, cardRect.height + 6), GetRarityColor(reward.DiagramRarity));

                if (hovered)
                {
                    cardRect.y -= 12f;
                    DrawRect(
                        new Rect(cardRect.x - 4f, cardRect.y - 4f, cardRect.width + 8f, cardRect.height + 8f),
                        new Color(0.35f, 0.95f, 1f));
                }

                GUI.backgroundColor = reward.IsBaseFunction
                    ? new Color(0.24f, 0.48f, 0.78f)
                    : new Color(0.48f, 0.28f, 0.68f);
                GUI.backgroundColor = Color.Lerp(
                    GUI.backgroundColor,
                    GetRarityColor(reward.DiagramRarity),
                    0.32f);

                if (GUI.Button(cardRect, rewardLabels[i], cardStyle))
                {
                    SelectReward(i);
                    return;
                }
            }

            GUI.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            if (GUI.Button(new Rect(overlay.center.x - 70f, overlay.yMax - 48f, 140f, 32f),
                    "건너뛰기", cardStyle))
            {
                SkipReward();
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawRelicRewardOverlay(float width, float height)
        {
            Rect overlay = new Rect(width * 0.5f - 410f, height * 0.5f - 175f, 820f, 350f);
            DrawRect(overlay, new Color(0.025f, 0.035f, 0.075f, 0.98f));
            GUI.Label(new Rect(overlay.x, overlay.y + 22f, overlay.width, 50f),
                "BIND A RELIC", resultStyle);
            GUI.Label(new Rect(overlay.x, overlay.y + 68f, overlay.width, 28f),
                "유물은 이번 런이 끝날 때까지 모든 이후 전투에 적용됩니다.", centeredStyle);

            const float relicWidth = 210f;
            const float relicHeight = 160f;
            const float gap = 40f;
            float firstX = overlay.center.x
                - (relicWidth * RunGameSession.RelicRewardOptionCount
                   + gap * (RunGameSession.RelicRewardOptionCount - 1)) * 0.5f;
            float relicY = overlay.y + 112f;

            for (int i = 0; i < RunGameSession.RelicRewardOptionCount; i++)
            {
                Rect relicRect = new Rect(
                    firstX + i * (relicWidth + gap),
                    relicY,
                    relicWidth,
                    relicHeight);
                bool hovered = relicRect.Contains(Event.current.mousePosition);
                if (hovered)
                {
                    DrawRect(
                        new Rect(relicRect.x - 4f, relicRect.y - 4f,
                            relicRect.width + 8f, relicRect.height + 8f),
                        new Color(0.72f, 0.76f, 1f));
                }

                GUI.backgroundColor = new Color(0.48f, 0.35f, 0.16f);
                if (GUI.Button(relicRect, relicRewardLabels[i], cardStyle))
                {
                    SelectRelicReward(i);
                    return;
                }
            }

            GUI.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            if (GUI.Button(new Rect(overlay.center.x - 70f, overlay.yMax - 48f, 140f, 32f),
                    "건너뛰기", cardStyle))
            {
                SkipReward();
            }

            GUI.backgroundColor = Color.white;
        }

        private void SelectReward(int optionIndex)
        {
            string selectedName = run.RewardOptions[optionIndex].DisplayName;
            if (!run.TrySelectReward(optionIndex))
            {
                return;
            }

            BindCurrentBattle();
            combatMessage = selectedName + " 카드를 획득했습니다. 다음 작도 지점을 선택하세요.";
            RefreshCachedText();
        }

        private void SelectRelicReward(int optionIndex)
        {
            string selectedName = run.RelicRewardOptions[optionIndex].DisplayName;
            if (!run.TrySelectRelicReward(optionIndex))
            {
                return;
            }

            BindCurrentBattle();
            combatMessage = selectedName + " 유물을 결속했습니다. 다음 전투부터 적용됩니다.";
            RefreshCachedText();
        }

        private void SkipReward()
        {
            if (!run.TrySkipReward())
            {
                return;
            }

            BindCurrentBattle();
            combatMessage = "보상을 건너뛰었습니다. 다음 작도 지점을 선택하세요.";
            RefreshCachedText();
        }

        private Vector2 GraphToScreen(Rect rect, float x, float y)
        {
            float normalizedX = Mathf.InverseLerp(DomainMin, DomainMax, x);
            float normalizedY = Mathf.InverseLerp(RangeMin, RangeMax, y);
            return new Vector2(
                Mathf.Lerp(rect.x, rect.xMax, normalizedX),
                Mathf.Lerp(rect.yMax, rect.y, normalizedY));
        }

        private static string GetRarityLabel(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return "일반";
                case CardRarity.Uncommon:
                    return "고급";
                case CardRarity.Rare:
                    return "희귀";
                case CardRarity.Legendary:
                    return "전설";
                default:
                    return "?";
            }
        }

        private static Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return new Color(0.7f, 0.76f, 0.85f);
                case CardRarity.Uncommon:
                    return new Color(0.38f, 0.84f, 0.8f);
                case CardRarity.Rare:
                    return new Color(0.77f, 0.64f, 1f);
                case CardRarity.Legendary:
                    return new Color(0.98f, 0.85f, 0.6f);
                default:
                    return Color.white;
            }
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, length, thickness),
                Texture2D.whiteTexture);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private void CreateCircleTexture()
        {
            const int size = 64;
            circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            circleTexture.name = "Prototype Circle";
            circleTexture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            circleTexture.SetPixels(pixels);
            circleTexture.Apply();
        }
    }
}
