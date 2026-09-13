using System;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Core.Combat
{
    public enum CombatApproach { None, Execution, Recording, Tuning, Observation }

    public sealed partial class BattleSession
    {
        private EquationState approachPreview, recordedDiagram;
        private int approachUsedTurn, arrangementLockCount, recordedPower;
        private bool recordingArmed, hasRecording;
        private double recordOriginX, recordOriginY;
        public CombatApproach Approach => definition.Approach;
        public bool HasSatellite { get; private set; }
        public double SatelliteX { get; private set; }
        public double SatelliteY { get; private set; }
        public bool SatelliteOriginSelected { get; private set; }
        public bool CanPlaceSatellite => Approach == CombatApproach.Observation
            && Phase == BattlePhase.PlayerPlanning && approachUsedTurn != Turn;
        public bool BodyPlotHit => Tactics != null && Tactics.IsHit(Equation);
        public bool SatellitePlotHit => HasSatellite && Equation.HasBase &&
            EquationAnalyzer.IntersectsCircle(Equation, SatelliteX, SatelliteY, Tactics.HitRadius+(HasRule(ApproachRule.SatelliteRadius)?.3:0), Equation.CurveSegmentCount);
        public bool ValidSatelliteAim(double x, double y)
        {
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (!CanPlaceSatellite || !FieldAim(x, y) || x < .5 || x > 9.5 || y < -3.5 || y > 3.5 || PositionBlocked(x, y)) return false;
            double dx = x - Tactics.X, dy = y - Tactics.Y;
            return dx * dx + dy * dy <= CurrentSatelliteReach*CurrentSatelliteReach+.000001;
        }
        public bool TryPlaceSatellite(double x, double y)
        {
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (!ValidSatelliteAim(x, y)) return false;
            SatelliteX = x; SatelliteY = y; HasSatellite = true; approachUsedTurn = Turn;
            if(HasRule(ApproachRule.SatelliteRecall))Tactics.SkillDashTo(x,y);
            if(HasRule(ApproachRule.SatelliteFocus))Tactics.Statuses.Add(CombatStatusKind.Focus,3,2);
            if(HasRule(ApproachRule.SatelliteWard))Tactics.Statuses.Add(CombatStatusKind.Ward,1,2);
            return true;
        }
        public bool CanToggleSatelliteOrigin => Approach == CombatApproach.Observation && HasSatellite
            && Phase == BattlePhase.PlayerPlanning && !Equation.HasBase;
        public bool TryToggleSatelliteOrigin()
        {
            if (!CanToggleSatelliteOrigin) return false;
            SatelliteOriginSelected = !SatelliteOriginSelected;
            return true;
        }
        public const double ExecutionTravel = 3.6, PullAimRange = 3, PullRadius = 2.2, PullTravel = 1.5;
        public bool HasStatusRules => Tactics != null && Tactics.Statuses.Enhanced;
        public void EnableStatusRules()
        {
            if (HasStatusRules || Turn != 1 || playedCardCount != 0 || Phase != BattlePhase.PlayerPlanning) throw new InvalidOperationException("Status rules must be enabled once at battle entry.");
            if (Tactics != null) Tactics.Statuses.Enhanced=true;
            foreach(var enemy in enemies) { enemy.Statuses.Enhanced=true; enemy.PrepareIntent(Turn); }
        }
        public bool CanUseLunaPull => Approach == CombatApproach.Tuning && Phase == BattlePhase.PlayerPlanning
            && approachUsedTurn != Turn && HasLivingEnemy();
        public int LastPullCount { get; private set; }
        public int LastPullDamage { get; private set; }

        private static bool FieldAim(double x, double y) => !double.IsNaN(x) && !double.IsNaN(y)
            && !double.IsInfinity(x) && !double.IsInfinity(y) && x >= 0 && x <= 10 && y >= -4 && y <= 4;

        public CombatSkillPreview PreviewExecutionDash(double x, double y)
        {
            var p = new CombatSkillPreview { TargetIndex = -1, Failure = CombatSkillFailure.Unavailable };
            if (Approach != CombatApproach.Execution || !CanUseCombatSkill) return p;
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (!FieldAim(x, y)) { p.Failure = CombatSkillFailure.InvalidTarget; return p; }
            double dx = x - Tactics.X, dy = y - Tactics.Y, length = Math.Sqrt(dx * dx + dy * dy);
            if (length < .001) { p.Failure = CombatSkillFailure.CoincidentTarget; return p; }
            dx /= length; dy /= length;
            p.OriginX = Tactics.X; p.OriginY = Tactics.Y;
            p.TargetX = x; p.TargetY = y;
            p.RequestedX = p.OriginX + dx * CurrentExecutionTravel; p.RequestedY = p.OriginY + dy * CurrentExecutionTravel;
            p.Distance = LegalTravel(p.OriginX, p.OriginY, dx, dy, CurrentExecutionTravel, TacticalCombatState.PlayerRadius);
            if (p.Distance < .001) { p.Failure = CombatSkillFailure.NoLanding; return p; }
            p.EndX = p.OriginX + dx * p.Distance; p.EndY = p.OriginY + dy * p.Distance;
            p.Damage = HasStatusRules ? Math.Max(0,10-Tactics.Statuses.Get(CombatStatusKind.Weaken)) : 10;
            p.Damage+=HasRule(ApproachRule.DashPower)?6:0;
            p.Width = .5+(HasRule(ApproachRule.DashWide)?.35:0); p.LaneCount = 1; p.Style = 0;
            if(HasRule(ApproachRule.DashTriple)){p.LaneCount=3;p.Style=1;p.Width=.35;}
            p.ReturnsToOrigin=HasRule(ApproachRule.DashReturn);
            p.Failure = CombatSkillFailure.None;
            return p;
        }

        public bool TryUseExecutionDash(double x, double y) => CommitCombatSkill(PreviewExecutionDash(x, y));

        // Swept circles: neither actor can tunnel through a pillar or an active seal.
        // Enemy bodies are not obstacles. The route never searches sideways for a landing.
        private double LegalTravel(double x, double y, double dx, double dy, double travel, double radius)
        {
            if (dx > 1e-9) travel = Math.Min(travel, (10 - radius - x) / dx);
            else if (dx < -1e-9) travel = Math.Min(travel, (radius - x) / dx);
            if (dy > 1e-9) travel = Math.Min(travel, (4 - radius - y) / dy);
            else if (dy < -1e-9) travel = Math.Min(travel, (-4 + radius - y) / dy);
            for (int i = 0; i < definition.TerrainCount; i++)
            {
                var t = definition.GetTerrain(i);
                if (t.BlocksMovement) travel = ClipCircle(x, y, dx, dy, travel, t.X, t.Y, t.Radius + radius);
            }
            for (int i = 0; i < enemies.Length; i++)
                if (SealActive(enemies[i])) travel = ClipCircle(x, y, dx, dy, travel,
                    enemies[i].SealX, enemies[i].SealY, SealRadius + radius);
            return Math.Max(0, travel);
        }

        private static double ClipCircle(double x, double y, double dx, double dy, double travel, double cx, double cy, double radius)
        {
            double ox = x - cx, oy = y - cy, along = ox * dx + oy * dy;
            double c = ox * ox + oy * oy - radius * radius;
            if (c < 0) return along >= 0 ? travel : 0; // Allow escape from an existing overlap.
            double disc = along * along - c;
            if (along >= 0 || disc <= 0) return travel;
            return Math.Min(travel, Math.Max(0, -along - Math.Sqrt(disc) - .00001));
        }

        public bool ValidLunaPullAim(double x, double y)
        {
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (!CanUseLunaPull || !FieldAim(x, y)) return false;
            double dx = x - Tactics.X, dy = y - Tactics.Y;
            return dx * dx + dy * dy <= PullAimRange * PullAimRange + .000001;
        }

        // Returns each enemy's actual destination, including unchanged/anchored enemies.
        public bool PreviewLunaPull(double x, double y, int index, out double endX, out double endY)
        {
            endX = endY = 0;
            if (index < 0 || index >= enemies.Length) return false;
            var enemy = enemies[index]; endX = enemy.X; endY = enemy.Y;
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (!ValidLunaPullAim(x, y) || !enemy.IsAlive || enemy.Statuses.Get(CombatStatusKind.Anchor) > 0) return false;
            double dx = x - enemy.X, dy = y - enemy.Y, length = Math.Sqrt(dx * dx + dy * dy);
            if (length > CurrentPullRadius || length < .001) return false;
            dx /= length; dy /= length;
            double travel = LegalTravel(enemy.X, enemy.Y, dx, dy, Math.Min(CurrentPullTravel + enemy.Statuses.Get(CombatStatusKind.Guidance), length), EnemyHitRadius);
            endX += dx * travel; endY += dy * travel;
            return travel >= .001;
        }

        public bool TryUseLunaPull(double x, double y)
        {
            if (!ValidLunaPullAim(x, y)) return false;
            int count = 0, pullDamage=0;
            // Destinations depend on static terrain/seal coordinates, never other moved enemies.
            for (int i = 0; i < enemies.Length; i++)
                if (PreviewLunaPull(x, y, i, out double ex, out double ey))
                { int hp=enemies[i].Health; DisplaceEnemy(enemies[i], ex, ey); pullDamage+=hp-enemies[i].Health; count++;
                    if(HasRule(ApproachRule.PullWound))enemies[i].Statuses.Add(CombatStatusKind.Wound,3,2);
                    if(HasRule(ApproachRule.PullExposure))enemies[i].Statuses.Add(CombatStatusKind.Exposure,3,2); }
            if (count == 0) return false;
            LastPullCount = count; LastPullDamage=pullDamage; approachUsedTurn = Turn;
            if(HasRule(ApproachRule.PullCompose))pullComposeTurn=Turn;
            if(AreAllEnemiesDefeated()) Phase=BattlePhase.Victory;
            return true;
        }
        private void DisplaceEnemy(EnemyState enemy,double x,double y)
        {
            if(Math.Abs(enemy.X-x)+Math.Abs(enemy.Y-y)<.001) return;
            enemy.DisplaceTo(x,y);
            enemy.Statuses.Remove(CombatStatusKind.Guidance);
            if(enemy.WoundTurn != Turn && enemy.Statuses.Get(CombatStatusKind.Wound)>0)
            {
                enemy.TakeDamage(enemy.Statuses.Get(CombatStatusKind.Wound));
                enemy.WoundTurn=Turn;
            }
        }
        private int ResolveCardPulls()
        {
            if(!HasStatusRules) return 0;
            int strength=0;
            for(int c=0;c<playedCardCount;c++)
                for(int a=0;a<playedCards[c].AbilityCount;a++)
                    if(playedCards[c].GetAbility(a).Kind==Cards.CardAbilityKind.Pull) strength=Math.Max(strength,playedCards[c].GetAbility(a).Magnitude);
            if(strength==0) return 0;
            int damage=0;
            foreach(var enemy in enemies)
            {
                if(!enemy.IsAlive || enemy.Statuses.Get(CombatStatusKind.Anchor)>0) continue;
                int index=Array.IndexOf(enemies,enemy);
                if(threatPlotDamage[index]<=0) continue;
                double dx=Tactics.X-enemy.X,dy=Tactics.Y-enemy.Y,length=Math.Sqrt(dx*dx+dy*dy);
                if(length<.001) continue;
                double distance=LegalTravel(enemy.X,enemy.Y,dx/length,dy/length,Math.Min(length,strength+enemy.Statuses.Get(CombatStatusKind.Guidance)),EnemyHitRadius);
                int hp=enemy.Health;
                DisplaceEnemy(enemy,enemy.X+dx/length*distance,enemy.Y+dy/length*distance);
                damage+=hp-enemy.Health;
            }
            return damage;
        }
        public bool CanReflectDiagram => Approach == CombatApproach.Tuning && (HasApproachTrait("luna.rotate.m2") || HasRule(ApproachRule.Reflect));
        private bool HasApproachTrait(string id) => skillLoadout.Growth != null && skillLoadout.Growth.IsEquipped(id);
        internal int ExecutionRupture(EnemyState enemy) => HasStatusRules || Approach == CombatApproach.Execution && HasApproachTrait("ian.execute.m2")
            ? enemy.Statuses.Get(CombatStatusKind.Rupture) : 0;
        private void RecordCondensedDiagram()
        {
            if (Approach != CombatApproach.Recording || !HasApproachTrait("ian.archive.m3") || hasRecording || recordingArmed) return;
            Equation.CopyDiagramTo(recordedDiagram, Equation.Fragments.OriginX, Equation.Fragments.OriginY, 0);
            recordedPower = WeaveDamageBonus + plotDamageBonus; hasRecording = true;
        }
        public bool HasRecording => hasRecording;
        public bool RecordingArmed => recordingArmed;
        public EquationState RecordedDiagram => hasRecording ? CurrentRecord : null;
        public EquationState LastRecordingDiagram => LastRecordingHits > 0 ? recordedDiagram : null;
        public int UndoFloor => Math.Max(SealedCardCount, arrangementLockCount);
        public int LastRecordingHits { get; private set; }
        public bool CannonDisconnected { get; private set; }
        public bool LastCannonCut { get; private set; }
        public bool UsesDiagramAbility => Approach == CombatApproach.Recording || Approach == CombatApproach.Tuning;
        public bool CanUseDiagramAbility => UsesDiagramAbility && Phase == BattlePhase.PlayerPlanning
            && Equation.HasBase && (approachUsedTurn != Turn || AdditionalTune) && !recordingArmed && !hasRecording;

        private void ResetApproach()
        {
            ResetStyleTree();
            normalRecordUses = specializationSkillTurn = specializationConditions = LastGrowthTriggers = 0;
            approachUsedTurn = 0; arrangementLockCount = 0; hasRecording = recordingArmed = false;
            HasSatellite = SatelliteOriginSelected = false; SatelliteX = SatelliteY = 0;
            CannonDisconnected = LastCannonCut = false; LastRecordingHits = LastPullCount = 0;
            if (UsesDiagramAbility && approachPreview == null)
            { approachPreview = new EquationState(); approachPreview.EnableFragments(); }
            if (Approach == CombatApproach.Recording && recordedDiagram == null)
            { recordedDiagram = new EquationState(); recordedDiagram.EnableFragments(); }
        }

        private bool ValidDiagramAim(double x, double y, int angle)
        {
            if (!CanUseDiagramAbility || double.IsNaN(x) || double.IsNaN(y)
                || double.IsInfinity(x) || double.IsInfinity(y) || x < .5 || x > 9.5 || y < -3.5 || y > 3.5
                || ((angle < -CurrentRotationLimit || angle > CurrentRotationLimit || angle % 30 != 0) && !(angle == 180 && CanReflectDiagram))
                || Approach == CombatApproach.Recording && angle != 0) return false;
            double dx = x - Equation.Fragments.OriginX, dy = y - Equation.Fragments.OriginY;
            return dx * dx + dy * dy <= CurrentDiagramReach*CurrentDiagramReach+.000001;
        }

        // Reuses a battle-owned buffer. No live equation, hand, statuses or random stream are touched.
        public EquationState PreviewDiagramAbility(double x, double y, int angle)
        {
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (!ValidDiagramAim(x, y, angle)) return null;
            Equation.CopyDiagramTo(approachPreview, x, y, angle);
            return approachPreview;
        }

        // Display-only invalid aim; never bypasses TryUseDiagramAbility validation.
        // Reuses the same battle-owned preview buffer as the legal preview.
        public EquationState PreviewRecordingPlacement(double x, double y, out bool canPlace)
        {
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            canPlace = Approach == CombatApproach.Recording && ValidDiagramAim(x, y, 0);
            if (Approach != CombatApproach.Recording || !CanUseDiagramAbility
                || double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) return null;
            Equation.CopyDiagramTo(approachPreview, x, y, 0);
            return approachPreview;
        }

        public bool TryUseDiagramAbility(double x, double y, int angle = 0)
        {
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (!ValidDiagramAim(x, y, angle)) return false;
            if (Approach == CombatApproach.Tuning)
            {
                if(tuneTurn!=Turn){tuneTurn=Turn;tuneUses=0;}tuneUses++;pullComposeTurn=0;
                Equation.ArrangeDiagram(x, y, angle);
                // Earlier cards cannot be undone through an already-used spatial operation.
                arrangementLockCount = playedCardCount;
            }
            else
            {
                recordingArmed = true; recordOriginX = x; recordOriginY = y;
                arrangementLockCount = playedCardCount;
            }
            approachUsedTurn = Turn;
            return true;
        }

        public int PreviewRecordingDamage(EnemyState enemy)
        {
            if (!hasRecording || !enemy.IsAlive) return 0;
            int damage = EquationAnalyzer.CalculateIntersectionDamage(CurrentRecord, enemy.X, enemy.Y, HasRule(ApproachRule.RecordWide)?.96:.48);
            return damage == 0 ? 0 : damage + recordedPower + enemy.Statuses.Get(CombatStatusKind.Rupture)+(HasRule(ApproachRule.RecordPower)?4:0);
        }

        public int PreviewRecordingFollowupDamage(EnemyState enemy)
        {
            if (!Equation.HasBase) return 0;
            int damage = PreviewRecordingDamage(enemy);
            if (damage == 0 || PreviewDamage(enemy) == 0) return damage;
            // The direct plot consumes old rupture before replay. Only newly applied marks remain.
            damage -= enemy.Statuses.Get(CombatStatusKind.Rupture);
            int newMarks = 0;
            for (int i = 0; i < playedCardCount; i++)
                for (int a = 0; a < playedCards[i].AbilityCount; a++)
                {
                    var ability = playedCards[i].GetAbility(a);
                    if (ability.Target == Graphaclysm.Core.Cards.AbilityTarget.Enemy
                        && ability.Kind == Graphaclysm.Core.Cards.CardAbilityKind.Rupture) newMarks += ability.Magnitude;
                }
            if (!HasApproachUltimates && Tactics != null && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Ian)
            {
                if (Tactics.UltimateVariant == 1) newMarks += 3;
                else if (Tactics.UltimateVariant == 3 && skillLoadout.HasTrait(17)) newMarks += 2;
            }
            if (UsesFragments && playedCardCount >= 6) newMarks += longWeaveRupture;
            return damage + Math.Min(24, newMarks);
        }

        private int ResolveRecording()
        {
            LastRecordingHits = 0; int total = 0;
            if (hasRecording)
            {
                for (int i = 0; i < enemies.Length; i++)
                {
                    int damage = PreviewRecordingDamage(enemies[i]);
                    if (damage == 0) continue;
                    enemies[i].TakeDamage(damage); enemies[i].Statuses.Remove(CombatStatusKind.Rupture);
                    if(HasRule(ApproachRule.RecordMark))enemies[i].Statuses.Add(CombatStatusKind.Rupture,3,2);
                    if(HasRule(ApproachRule.RecordBurn))enemies[i].Statuses.Add(CombatStatusKind.Burn,3,2);
                    LastRecordingHits++; total += damage;
                }
                if(normalRecordUses>0)normalRecordUses--;
                else hasRecording = false;
            }
            if (recordingArmed)
            {
                Equation.CopyDiagramTo(recordedDiagram, recordOriginX, recordOriginY, HasRule(ApproachRule.RecordMirror)?180:0);
                recordedPower = WeaveDamageBonus + plotDamageBonus; hasRecording = true; recordingArmed = false;
                normalRecordUses=HasRule(ApproachRule.RecordRepeat)?1:0;
            }
            return total;
        }

        public bool PreviewCannonCut(EquationState diagram)
        {
            if (Approach == CombatApproach.None || CannonDisconnected || !diagram.HasBase) return false;
            for (int e = 0; e < enemies.Length; e++)
            {
                var enemy = enemies[e];
                if (enemy.Definition.Id != "signature.cannon" || !enemy.IsAlive) continue;
                diagram.Sample(0, out double ax, out double ay);
                for (int i = 1; i <= diagram.CurveSegmentCount; i++)
                {
                    diagram.Sample((double)i / diagram.CurveSegmentCount, out double bx, out double by);
                    if (SegmentsTouch(ax, ay, bx, by, enemy.X, enemy.Y, 5, 1.6)) return true;
                    ax = bx; ay = by;
                }
            }
            return false;
        }

        private static bool SegmentsTouch(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
        {
            double ux = bx - ax, uy = by - ay, vx = dx - cx, vy = dy - cy;
            double cross = ux * vy - uy * vx;
            if (Math.Abs(cross) > 1e-10)
            {
                double t = ((cx - ax) * vy - (cy - ay) * vx) / cross;
                double u = ((cx - ax) * uy - (cy - ay) * ux) / cross;
                if (t >= 0 && t <= 1 && u >= 0 && u <= 1) return true;
            }
            return DistanceToSegment(ax, ay, cx, cy, dx, dy) <= .12
                || DistanceToSegment(bx, by, cx, cy, dx, dy) <= .12
                || DistanceToSegment(cx, cy, ax, ay, bx, by) <= .12
                || DistanceToSegment(dx, dy, ax, ay, bx, by) <= .12;
        }
    }
}
