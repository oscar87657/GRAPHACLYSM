using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private BattleSession skillPreviewBattle;
        private int skillPreviewTarget = -2, skillPreviewRevision = -1;
        private CombatSkillPreview skillPreview;
        private string skillPreviewText = string.Empty;
        private string skillInputPath = "button";

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void TraceCombatSkill(string stage, BattleSession source, CombatSkillPreview p)
        {
            // Local Editor-only diagnostics, once per command, never in preview/repaint.
            string trace = "OverhaulSkill " + stage + " input=" + skillInputPath + " hover=" + hoveredEnemy
                + " targetSlot=" + p.TargetIndex + " round=" + source.Turn + " failure=" + p.Failure
                + " distance=" + p.Distance + " requested=(" + p.RequestedX + "," + p.RequestedY
                + ") landing=(" + p.EndX + "," + p.EndY + ")";
            for (int i = 0; i < source.Enemies.Count; i++)
            {
                EnemyState e = source.Enemies[i];
                CombatSkillContact c = source.PreviewCombatSkillContact(p, i);
                trace += " [" + i + ":" + e.Definition.Id + " hp=" + e.Health
                    + " shield=" + e.Statuses.Get(CombatStatusKind.Shield);
                if (stage == "before") trace += " contact=" + c.GeometryHit + " hit=" + c.Hit
                    + " predictedHp=" + c.HealthDamage + " predictedShield=" + c.ShieldDamage;
                trace += "]";
            }
            if (stage == "after") trace += " displayedHp=" + source.LastSkillDamage + " displayedShield=" + source.LastSkillShieldDamage;
            Debug.Log(trace);
        }

        private static string SkillFailureText(CombatSkillFailure failure)
        {
            switch (failure)
            {
                case CombatSkillFailure.InvalidTarget: return "살아 있는 적을 선택하세요.";
                case CombatSkillFailure.CoincidentTarget: return "겹친 적에게는 방향을 정할 수 없습니다. 이동 후 사용하세요.";
                case CombatSkillFailure.NoLanding: return "착지할 자리가 없습니다. 다른 대상을 선택하세요.";
                default: return "지금은 기술을 사용할 수 없습니다.";
            }
        }

        private void DrawCombatSkillPreview()
        {
            if (battle.UsesDiagramAbility || battle.Approach == CombatApproach.Execution || battle.Approach == CombatApproach.Observation || !combatSkillTargeting || hoveredEnemy < 0) return;
            if (skillPreviewBattle != battle || skillPreviewTarget != hoveredEnemy || skillPreviewRevision != run.Revision)
            {
                skillPreviewBattle = battle; skillPreviewTarget = hoveredEnemy; skillPreviewRevision = run.Revision;
                skillPreview = battle.PreviewCombatSkill(hoveredEnemy);
                int hits = 0, hp = 0, shield = 0; bool targeted = false;
                for (int i = 0; i < battle.Enemies.Count; i++)
                {
                    CombatSkillContact c = battle.PreviewCombatSkillContact(skillPreview, i);
                    if (c.Hit) hits++;
                    hp += c.HealthDamage; shield += c.ShieldDamage;
                    targeted |= c.Hit && !c.GeometryHit;
                }
                skillPreviewText = !skillPreview.CanUse ? SkillFailureText(skillPreview.Failure)
                    : "적 " + hits + "명 · 체력 −" + hp + " · 보호막 −" + shield
                    + (skillPreview.ReturnsToOrigin ? " · 제자리 복귀" : " · ◇ 착지")
                    + (targeted ? " · 주 대상 별도 타격" : "");
            }
            if (skillPreview.CanUse)
            {
                Vector2 origin = FieldPoint(skillPreview.OriginX, skillPreview.OriginY);
                Vector2 end = FieldPoint(skillPreview.EndX, skillPreview.EndY);
                Vector2 target = FieldPoint(skillPreview.TargetX, skillPreview.TargetY);
                Line(origin, end, Gold, 2);
                Diamond(end, 18, Gold, 2);
                if (skillPreview.BurstRadius > 0 || skillPreview.ChainRadius > 0)
                    Ring(target, (float)(skillPreview.BurstRadius > 0 ? skillPreview.BurstRadius + BattleSession.EnemyHitRadius : skillPreview.ChainRadius) * FieldUnit,
                        new Color(Gold.r, Gold.g, Gold.b, .5f));
                for (int i = 0; i < battle.Enemies.Count; i++)
                    if (battle.PreviewCombatSkillContact(skillPreview, i).Hit)
                        Ring(FieldPoint(battle.Enemies[i].X, battle.Enemies[i].Y), 37, Gold, 2);
            }
            Fill(new Rect(590, 121, 740, 36), new Color(.06f, .055f, .1f, .94f));
            Label(new Rect(598, 122, 724, 34), skillPreviewText, ui.SmallLight, true);
        }
    }
}
