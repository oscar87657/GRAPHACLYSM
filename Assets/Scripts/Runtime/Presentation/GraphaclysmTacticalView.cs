using System.Text;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using UnityEngine;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmPrototypeView
    {
        private static readonly string[] GridXLabels = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
        private static readonly string[] GridYLabels = { "-4", "-3", "-2", "-1", "0", "1", "2", "3", "4" };
        private static readonly string[] StatusNames = { "보호막", "집중", "재생", "잔불", "약화", "노출", "고정", "경쾌" };
        // View owns hand-capacity buffers; strings rebuild only after commands.
        private string[] cardTooltips;
        private string tacticalText, ultimateText, playerLabel, moveLabel;
        private bool playerWillBeHit;
        private GUIStyle tooltipStyle;

        private void EnsureTacticalBuffers()
        {
            if (cardTooltips == null || cardTooltips.Length != deck.HandCapacity)
                cardTooltips = new string[deck.HandCapacity];
        }

        private void RefreshTacticalText()
        {
            if (battle.Tactics == null) return;
            var state = battle.Tactics;
            playerWillBeHit = battle.PreviewPlayerHit;
            playerLabel = flow.CurrentCharacter.DisplayName + (playerWillBeHit ? " · 자신 적중" : " · 자신")
                + "\n(" + state.X.ToString("0.#") + ", " + state.Y.ToString("0.#") + ")";
            tacticalText = (playerWillBeHit ? "자신 적중: 기본 보호막 3 + 각인 효과" : "자신을 지나는 선을 만들면 보호막과 각인 효과를 받습니다.")
                + BuildStatuses(state.Statuses);
            moveLabel = state.HasMoved ? "이동 완료" : "이동 " + state.MoveCost + " EN · 턴당 1회";
            ultimateText = (state.Archetype == CombatArchetype.Ian ? "흑유리 개방" : "백야의 포옹")
                + "  " + state.Resonance + "/6" + (state.UltimateArmed ? " · 준비 취소" : " · 준비");
            for (int i = 0; i < deck.HandCount; i++)
            {
                CardDefinition card = deck.GetHandCard(i);
                bool playable = battle.CanPlayCard(card, out CardPlayFailure failure);
                cardTooltips[i] = card.DisplayName + "  |  " + GetRarityLabel(card.Rarity) + " · " + GetCardRole(card.Type)
                    + "\n" + card.FormulaLabel + "   ·   " + card.Cost + " EN\n\n" + card.Description
                    + "\n\n적 적중: " + (string.IsNullOrEmpty(card.EnemyEffect) ? "조립된 선으로 피해" : card.EnemyEffect)
                    + "\n자신 적중: " + (string.IsNullOrEmpty(card.PlayerEffect) ? "기본 보호막 3" : card.PlayerEffect)
                    + "\n\n" + (playable ? "클릭: 연결   /   우클릭: 마지막 선택 취소" : GetFailureMessage(failure));
            }
        }

        private static string BuildStatuses(CombatStatusState statuses)
        {
            var result = new StringBuilder(96);
            for (int i = 0; i < CombatStatusState.Capacity; i++)
            {
                var kind = (CombatStatusKind)i;
                if (statuses.Get(kind) <= 0) continue;
                result.Append("  ").Append(StatusNames[i]).Append(' ').Append(statuses.Get(kind))
                    .Append('(').Append(statuses.Duration(kind)).Append(')');
            }
            return result.Length == 0 ? string.Empty : "\n" + result;
        }

        private void DrawTacticalControls(float width)
        {
            if (battle.Tactics == null) return;
            DrawRect(new Rect(32, 94, width - 64, 60), new Color(0.15f, 0.18f, 0.29f, 0.95f));
            GUI.Label(new Rect(45, 98, 175, 25), moveLabel, smallStyle);
            GUI.enabled = battle.Phase == BattlePhase.PlayerPlanning && !battle.Tactics.HasMoved;
            if (GUI.Button(new Rect(225, 101, 38, 38), "←", actionStyle)) MovePlayer(-1.5, 0);
            if (GUI.Button(new Rect(266, 101, 38, 38), "↑", actionStyle)) MovePlayer(0, 1.5);
            if (GUI.Button(new Rect(307, 101, 38, 38), "↓", actionStyle)) MovePlayer(0, -1.5);
            if (GUI.Button(new Rect(348, 101, 38, 38), "→", actionStyle)) MovePlayer(1.5, 0);
            GUI.enabled = battle.Phase == BattlePhase.PlayerPlanning && battle.Tactics.Resonance >= 6;
            GUI.backgroundColor = battle.Tactics.UltimateArmed ? new Color(0.65f, 0.52f, 0.92f) : new Color(0.35f, 0.4f, 0.6f);
            if (GUI.Button(new Rect(width - 315, 101, 270, 38), ultimateText, cardStyle))
            {
                run.TryToggleUltimate();
                RefreshCachedText();
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(404, 96, width - 735, 55), tacticalText, smallStyle);
        }

        private void MovePlayer(double dx, double dy)
        {
            combatMessage = run.TryMovePlayer(dx, dy)
                ? "이동했습니다. 붉은 영역은 이번 적 공격의 예정 범위입니다."
                : "이동 불가: 남은 에너지, 필드 경계, 적과의 겹침, 고정 상태를 확인하세요.";
            RefreshCachedText();
        }

        private void DrawAttackZones(Rect rect)
        {
            if (battle.Tactics == null) return;
            GUI.BeginGroup(rect);
            var local = new Rect(0, 0, rect.width, rect.height);
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                var enemy = battle.Enemies[i];
                if (!enemy.IsAlive || enemy.Intent.Kind != EnemyIntentKind.Attack) continue;
                DrawWorldDisc(local, enemy.AimX, enemy.AimY, enemy.AimRadius, new Color(0.95f, 0.25f, 0.4f, 0.12f));
                DrawWorldRing(local, enemy.AimX, enemy.AimY, enemy.AimRadius, new Color(1f, 0.45f, 0.58f, 0.55f));
            }
            GUI.EndGroup();
        }

        private void DrawPlayer(Rect rect)
        {
            if (battle.Tactics == null) return;
            var state = battle.Tactics;
            Color color = playerWillBeHit ? new Color(0.48f, 1f, 0.86f) : new Color(0.65f, 0.72f, 1f);
            DrawWorldDisc(rect, state.X, state.Y, state.HitRadius, new Color(color.r, color.g, color.b, 0.18f));
            DrawWorldRing(rect, state.X, state.Y, state.HitRadius, color);
            Vector2 center = GraphToScreen(rect, (float)state.X, (float)state.Y);
            int index = state.Archetype == CombatArchetype.Ian ? 0 : 1;
            if (characterPortraits[index] != null)
                GUI.DrawTextureWithTexCoords(new Rect(center.x - 22, center.y - 32, 44, 55),
                    characterPortraits[index], new Rect(0.24f, 0.67f, 0.5f, 0.29f));
            GUI.Label(new Rect(center.x - 100, center.y + 29, 200, 46), playerLabel, smallStyle);
        }

        private void DrawWorldDisc(Rect rect, double x, double y, double radius, Color color)
        {
            Vector2 center = GraphToScreen(rect, (float)x, (float)y);
            float rx = (float)radius * rect.width / 10f, ry = (float)radius * rect.height / 8f;
            Color saved = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - rx, center.y - ry, rx * 2, ry * 2), circleTexture);
            GUI.color = saved;
        }

        private void DrawWorldRing(Rect rect, double x, double y, double radius, Color color)
        {
            Vector2 center = GraphToScreen(rect, (float)x, (float)y);
            float rx = (float)radius * rect.width / 10f, ry = (float)radius * rect.height / 8f;
            Vector2 previous = center + new Vector2(rx, 0);
            for (int i = 1; i <= 48; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                Vector2 next = center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry);
                DrawLine(previous, next, color, 1.6f);
                previous = next;
            }
        }

        private void DrawCardTooltip()
        {
            if (hoveredHandIndex < 0 || hoveredHandIndex >= deck.HandCount || battle.Tactics == null) return;
            GetAnimatedCardTransform(hoveredHandIndex, out Rect card, out _);
            const float tooltipWidth = 360;
            float x = card.center.x < Screen.width * 0.5f ? card.xMax + 18 : card.x - tooltipWidth - 18;
            Rect tooltip = new Rect(Mathf.Clamp(x, 16, Screen.width - tooltipWidth - 16),
                Mathf.Max(165, Screen.height - 385), tooltipWidth, 340);
            DrawRect(new Rect(tooltip.x - 2, tooltip.y - 2, tooltip.width + 4, tooltip.height + 4),
                GetRarityColor(deck.GetHandCard(hoveredHandIndex).Rarity));
            DrawRect(tooltip, new Color(0.06f, 0.075f, 0.13f, 0.99f));
            GUI.Label(tooltip, cardTooltips[hoveredHandIndex], tooltipStyle);
        }

        private void DrawUltimateTooltip(float width)
        {
            if (battle.Tactics == null || !new Rect(width - 315, 101, 270, 38).Contains(Event.current.mousePosition)) return;
            Rect rect = new Rect(width - 405, 155, 360, 210);
            DrawRect(rect, new Color(0.08f, 0.09f, 0.16f, 0.99f));
            GUI.Label(rect, battle.Tactics.Archetype == CombatArchetype.Ian
                ? "흑유리 개방\n다음 작도 적중 피해 +6. 적중한 적의 이번 이동을 봉쇄합니다.\n\n공명 6 소비 · 준비 취소 가능\n작도 +1 / 자신과 적 동시 적중 +1\n남은 공명은 다음 전투로 이어집니다."
                : "백야의 포옹\n다음 작도의 자신 적중 반경이 1로 확대됩니다. 자신 적중 시 정화·보호막 8·회복 5를 추가로 받습니다.\n\n공명 6 소비 · 준비 취소 가능\n작도 +1 / 자신과 적 동시 적중 +1", tooltipStyle);
        }

        private static string GetCardRole(CardType type)
        {
            switch (type)
            {
                case CardType.Input: return "입력";
                case CardType.Operation: return "연산";
                case CardType.Coefficient: return "계수";
                case CardType.Inscription: return "각인";
                case CardType.Utility: return "유틸";
                default: return "수학 보관함";
            }
        }

        private static string CompactDescription(CardDefinition card)
        {
            if (card.IsInscription) return "적: " + card.EnemyEffect + "\n자신: " + card.PlayerEffect;
            switch (card.Type)
            {
                case CardType.Input: return "이 조각부터 수식을 시작\n자신 적중 시 보호막 3";
                case CardType.Operation: return "앞에서 만든 값에 적용\n연결 순서에 따라 형태 변화";
                case CardType.Coefficient: return "지금까지 만든 값 조정\n같은 계수도 다시 연결 가능";
                default: return "완성 곡선 · 보관함";
            }
        }
    }
}
