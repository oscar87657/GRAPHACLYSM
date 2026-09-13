using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private bool lunaPullTargeting;
        private BattleSession spatialPreviewBattle;
        private int spatialRevision = -1;
        private Vector2 spatialPointer;
        private string spatialText = "";
        private Vector2[] pullFrom, pullTo;
        private BattleSession pullFxBattle;
        private float pullFxUntil;
#if UNITY_EDITOR
        // Explicit screenshot fixture only; never present in a player build.
        private bool spatialPreviewFixture;
        private Vector2 spatialFixturePointer;
#endif

        private void ToggleLunaPull()
        {
            if (castActive || battle == null || battle.Approach != CombatApproach.Tuning) return;
            if (combatSkillTargeting && lunaPullTargeting)
            { combatSkillTargeting = lunaPullTargeting = false; message = "견인 조준 취소"; Refresh(); return; }
            if (!battle.CanUseLunaPull) { message = "이번 턴에는 이미 기술을 사용했습니다."; Refresh(); return; }
            combatSkillTargeting = lunaPullTargeting = true;
            spatialRevision = -1;
            message = "적을 모을 지점을 클릭하세요 · 우클릭 취소 · 이번 턴 도안 조율 대신 사용";
            Refresh();
        }

        private void UseExecutionAt(double x, double y)
        {
            if (castActive || battle == null || run == null) return;
            var source = battle;
            if (!run.TryUseExecutionDash(x, y))
            { message = "돌진할 방향을 선택하세요. 벽에 막혔거나 대기시간이 남아 있으면 사용할 수 없습니다."; Refresh(); return; }
            ShowCommittedSkill(source);
        }

        private void UseLunaPullAt(double x, double y)
        {
            if (castActive || battle == null || run == null) return;
            if (pullFrom == null || pullFrom.Length != battle.Enemies.Count)
            { pullFrom = new Vector2[battle.Enemies.Count]; pullTo = new Vector2[battle.Enemies.Count]; }
            for (int i = 0; i < pullFrom.Length; i++)
                pullFrom[i] = FieldPoint(battle.Enemies[i].X, battle.Enemies[i].Y);
            if (!run.TryUseLunaPull(x, y))
            { message = "몸에서 3 이내 지점을 고르세요. 범위 안에 움직일 수 있는 적이 필요합니다."; Refresh(); return; }
            for (int i = 0; i < pullTo.Length; i++)
                pullTo[i] = FieldPoint(battle.Enemies[i].X, battle.Enemies[i].Y);
            pullFxBattle = battle; pullFxUntil = ViewTime + (preferences.ReduceMotion ? .35f : .75f);
            combatSkillTargeting = lunaPullTargeting = false;
            message = "성운 견인 · 적 " + battle.LastPullCount + "명 이동 · 상처 피해 " + battle.LastPullDamage
                + (battle.HasStatusRules ? " · 사선/고리도 함께 이동" : " · 기존 공격 예고 유지");
            Refresh();
        }

        private void DrawSpatialSkillPreview()
        {
            if (pullFxBattle == battle && ViewTime < pullFxUntil)
                for (int i = 0; i < pullTo.Length; i++)
                    if ((pullFrom[i] - pullTo[i]).sqrMagnitude > 1)
                    { Line(pullFrom[i], pullTo[i], Violet, 3); Ring(pullTo[i], 35, Violet, 2); }
            Vector2 pointer = Event.current.mousePosition;
#if UNITY_EDITOR
            if (spatialPreviewFixture) pointer = spatialFixturePointer;
#endif
            if (!combatSkillTargeting || !(lunaPullTargeting || battle.Approach == CombatApproach.Execution)
                || !Field.Contains(pointer)) return;
            ScreenToField(pointer, out double x, out double y);
            bool changed = spatialPreviewBattle != battle || spatialRevision != run.Revision || pointer != spatialPointer;
            spatialPreviewBattle = battle; spatialRevision = run.Revision; spatialPointer = pointer;
            if (lunaPullTargeting)
            {
                bool valid = battle.ValidLunaPullAim(x, y);
                Ring(FieldPoint(battle.Tactics.X, battle.Tactics.Y), (float)BattleSession.PullAimRange * FieldUnit,
                    new Color(Violet.r, Violet.g, Violet.b, .3f));
                Ring(pointer, (float)battle.CurrentPullRadius * FieldUnit, valid ? Violet : Threat, 2);
                Diamond(pointer, 10, valid ? Violet : Threat, 2);
                int moved = 0, hits = 0;
                for (int i = 0; i < battle.Enemies.Count; i++)
                {
                    var enemy = battle.Enemies[i]; if (!enemy.IsAlive) continue;
                    bool move = battle.PreviewLunaPull(x, y, i, out double ex, out double ey);
                    bool hit = battle.Equation.HasBase && EquationAnalyzer.CalculateIntersectionDamage(battle.Equation, ex, ey, BattleSession.EnemyHitRadius) > 0;
                    if (hit) hits++;
                    if (!move) continue;
                    moved++;
                    Vector2 destination = FieldPoint(ex, ey);
                    Line(FieldPoint(enemy.X, enemy.Y), destination, Violet, 2);
                    Ring(destination, (float)BattleSession.EnemyHitRadius * FieldUnit, hit ? Gold : Violet, 2);
                    Diamond(destination, 9, hit ? Gold : Violet, 2);
                }
                if (changed) spatialText = !valid ? "몸에서 3 이내 지점을 선택하세요 · 우클릭 취소"
                    : "이동 " + moved + "명 · 이동 후 도안 접촉 " + hits + "명 · 금색: 작도에 닿음 · 유도는 거리 증가 / 상처는 이동 피해";
            }
            else
            {
                var p = battle.PreviewExecutionDash(x, y);
                int hits = 0, hp = 0, shield = 0;
                if (p.CanUse)
                {
                    Vector2 from = FieldPoint(p.OriginX, p.OriginY), to = FieldPoint(p.EndX, p.EndY);
                    Line(from, to, new Color(Gold.r, Gold.g, Gold.b, .12f), (float)p.Width * FieldUnit * 2);
                    Line(from, to, Gold, 2); Ring(to, (float)p.Width * FieldUnit, Gold, 1); Diamond(to, 18, Gold, 2);
                    if(p.LaneCount==3)
                    {Vector2 normal=new Vector2(-(to-from).y,(to-from).x).normalized*.8f*FieldUnit;
                        Line(from+normal,to+normal,Gold,2);Line(from-normal,to-normal,Gold,2);}
                    if(p.ReturnsToOrigin){Diamond(from,22,Gold,2);Label(new Rect(from.x-60,from.y+26,120,26),"공격 후 귀환",ui.SmallLight,true);}
                    for (int i = 0; i < battle.Enemies.Count; i++)
                    {
                        var contact = battle.PreviewCombatSkillContact(p, i);
                        if (!contact.Hit) continue;
                        hits++; hp += contact.HealthDamage; shield += contact.ShieldDamage;
                        Ring(FieldPoint(battle.Enemies[i].X, battle.Enemies[i].Y), 37, Gold, 2);
                    }
                }
                if (changed) spatialText = !p.CanUse ? "방향을 바꿔 주세요 · 이동할 공간 필요 / 우클릭 취소"
                    : "경로 적중 " + hits + "명 · 체력 −" + hp + " / 보호막 −" + shield
                        + (p.Distance < battle.CurrentExecutionTravel - .001 ? " · 지형/경계에서 정지" : " · 관통 돌진") + " · 클릭 확정";
            }
            Fill(new Rect(1570,614,328,140), new Color(.06f,.055f,.1f,.94f));
            Label(new Rect(1580,622,308,124), spatialText, ui.SmallLight);
        }
    }
}
