using System;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Application
{
    public static class NodeTrials
    {
        public static CombatApproach ApproachFor(string id)
        {
            switch(id)
            {
                case "ian.inscription.core": return CombatApproach.Execution;
                case "ian.archive.core": return CombatApproach.Recording;
                case "luna.orbit.core": return CombatApproach.Tuning;
                case "luna.binary.core": return CombatApproach.Observation;
                default: return CombatApproach.None;
            }
        }
        public static string Title(CombatApproach approach)
        {
            switch(approach)
            {
                case CombatApproach.Execution: return "균열 집행 · 경로에 파열 남기기";
                case CombatApproach.Recording: return "기록 포격 · 다음 방출 준비하기";
                case CombatApproach.Tuning: return "궤도 조율 · 같은 식의 위치 바꾸기";
                case CombatApproach.Observation: return "쌍성 관측 · 몸 대신 위성 연결하기";
                default: return "시험 준비 중";
            }
        }
        public static string Lesson(CombatApproach approach)
        {
            switch(approach)
            {
                case CombatApproach.Execution: return "K로 오른쪽 돌진 → 적에게 파열을 남기고 작도로 소비해 보세요. 처치하면 기술을 다시 쓸 수 있습니다.";
                case CombatApproach.Recording: return "K로 기록 위치 선택 → 방출 → 다음 식 방출. 보라색은 다음 공격에 겹칠 도안입니다. 카드 능력을 복제하지 않습니다.";
                case CombatApproach.Tuning: return "K로 기준점을 옮기고 Q/E로 회전해 보세요. 식을 고치는 대신 J로 적을 모을 수도 있습니다. 턴당 둘 중 하나입니다.";
                case CombatApproach.Observation: return "K로 그래프 선 위에 위성을 놓고 몸만 피하세요. 다음 조립 전 J로 위성에서 시작할 수도 있습니다. 위성은 적 공격을 막지 않습니다.";
                default: return "이 노드의 실제 체험은 아직 준비되지 않았습니다.";
            }
        }
    }

    // Short-lived, isolated deterministic snapshots owned by the comparison window.
    // These are real battle commands, not a presentation-only simulated ability.
    public sealed class NodeTrialComparison
    {
        public NodeTrialComparison(string nodeId)
        {
            Approach = NodeTrials.ApproachFor(nodeId);
            if(Approach == CombatApproach.None) throw new ArgumentException("Unsupported trial node",nameof(nodeId));
            NodeId=nodeId; Before=SignatureBattleFactory.Create(Approach); After=SignatureBattleFactory.Create(Approach);
            Prepare(Before); Prepare(After);
            var b=After.CurrentBattle.Battle;
            bool applied=false;
            switch(Approach)
            {
                case CombatApproach.Execution: applied=After.TryUseExecutionDash(9,-2); break;
                case CombatApproach.Recording:
                    RecordGhost=b.PreviewDiagramAbility(5.5,-.2,0);
                    applied=After.TryUseDiagramAbility(5.5,-.2); break;
                case CombatApproach.Tuning: applied=After.TryUseDiagramAbility(5.25,-1.5,-90); break;
                case CombatApproach.Observation: applied=After.TryPlaceSatellite(6.4,-1.1); break;
            }
            if(!applied) throw new InvalidOperationException("Trial demonstration is no longer valid");
            BeforeEnemyText=EnemyText(Before); AfterEnemyText=EnemyText(After);
            BeforeSummary=Summary(Before); AfterSummary=Summary(After);
            if(RecordGhost!=null)
            {
                int hits=0;
                foreach(var enemy in b.Enemies)
                    if(EquationAnalyzer.IntersectsCircle(RecordGhost,enemy.X,enemy.Y,BattleSession.EnemyHitRadius,RecordGhost.CurveSegmentCount)) hits++;
                AfterSummary="이번 방출은 피해 75% · 기록 예약\n보라색 기록: 현재 배치에서 적 "+hits+"명 / 적 이동에 따라 달라짐";
            }
        }
        public string NodeId { get; }
        public CombatApproach Approach { get; }
        public RunGameSession Before { get; }
        public RunGameSession After { get; }
        public EquationState RecordGhost { get; }
        public string[] BeforeEnemyText { get; }
        public string[] AfterEnemyText { get; }
        public string BeforeSummary { get; }
        public string AfterSummary { get; }

        public static void Prepare(RunGameSession run)
        {
            Play(run,"frag.ellipse"); Play(run,"frag.expand");
        }
        private static void Play(RunGameSession run,string id)
        {
            for(int i=0;i<run.CurrentBattle.Deck.HandCount;i++)
                if(run.CurrentBattle.Deck.GetHandCard(i).Id==id)
                {
                    if(!run.TryPlayHandCard(i,out _,out _)) throw new InvalidOperationException("Trial card rejected");
                    return;
                }
            throw new InvalidOperationException("Trial card missing");
        }
        private static string[] EnemyText(RunGameSession run)
        {
            var b=run.CurrentBattle.Battle; var result=new string[b.Enemies.Count];
            for(int i=0;i<result.Length;i++)
            {
                var e=b.Enemies[i]; int mark=e.Statuses.Get(CombatStatusKind.Rupture);
                result[i]=e.Definition.DisplayName+" · HP "+e.Health+"\n방출 예측 −"+b.PreviewHealthDamage(e)
                    +(mark>0?" / 파열 "+mark:"");
            }
            return result;
        }
        private static string Summary(RunGameSession run)
        {
            var b=run.CurrentBattle.Battle; int count=0;
            for(int i=0;i<b.Enemies.Count;i++) if(b.PreviewDamage(b.Enemies[i])>0) count++;
            return "현재 도안: 적 "+count+"명 / "+(b.PreviewPlayerHit?"자가 강화 가능":"자가 강화 없음")
                +(b.RecordingArmed?" / 다음 방출용 기록 예약":"");
        }
    }
}
