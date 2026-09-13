using System;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Runs
{
    public enum ApproachRule { None, DashReach, DashWide, DashPower, BurstReach, BurstPower, BurstFocus, DashMark, DashGuard, DashMend,
        RecordPower, RecordFull, RecordRepeat, EchoCount, EchoPower, EchoGuard, RecordReach, RecordMark, RecordBurn,
        TuneReach, PullTravel, PullArea, ExtraPower, ExtraGuard, ExtraSkill, Reflect, PullWound, PullExposure,
        SatelliteReach, SatelliteRadius, SatelliteShield, SanctuaryTime, SanctuaryShield, SanctuaryHeal, RemotePower, SatelliteFocus, SatelliteWard,
        DashReturn, DashTriple, DashLong, BurstEcho, BurstConcentrate, BurstShield,
        RecordMirror, RecordFollow, RecordWide, EchoMirror, EchoMarch, EchoConcentrate,
        PullCompose, TuneFullAngle, TuneRepeat, ExtraEcho, ExtraPull, ExtraResonance,
        SatelliteDual, SatelliteRecall, SatelliteLink, SanctuaryWide, SanctuaryMobile, SanctuaryRemote }
    public sealed class ApproachGrowthEntry
    {
        public GrowthNodeDefinition Node { get; internal set; }
        public ApproachRule Rule { get; internal set; }
        public int Trigger { get; internal set; } = -1;
        public CombatStatusKind Status { get; internal set; }
        public bool Enemy { get; internal set; }
        public int Magnitude { get; internal set; }
    }
    public static class ApproachGrowthCatalog
    {
        public const int Count=55;
        public static readonly string[] Names={"","균열 집행","기록 포격","궤도 조율","쌍성 관측"};
        private static readonly string[] Prefix={"","path.execution.","path.recording.","path.tuning.","path.observation."};
        private static readonly int[] ParentLocal={-1,0,0,1,2,0,3,4,5,6,7,8,9,10,11,12,13,14};
        private static readonly string[] Conditions={"파편 3개 이하로 방출하면","파편 5개 이상으로 방출하면","이동 후 방출하면","자신을 맞히면","적 2명 이상을 맞히면","적 하나만 맞히면","응축한 식을 방출하면","기술 사용 후 같은 턴에 방출하면","궁극기로 방출하면","기록이 대기 중일 때 방출하면","체력이 절반 이하일 때 방출하면","보호막이 있을 때 방출하면","파열이 있는 적이 살아 있을 때 방출하면","두 번째 조립을 방출하면","위성이 식에 닿으면"};
        private static readonly CombatStatusKind[] Rewards={CombatStatusKind.Focus,CombatStatusKind.Shield,CombatStatusKind.Burn,CombatStatusKind.Exposure,CombatStatusKind.Rupture,CombatStatusKind.Regeneration,CombatStatusKind.Weaken,CombatStatusKind.Pierce,CombatStatusKind.Ward,CombatStatusKind.Haste,CombatStatusKind.Guidance,CombatStatusKind.Wound,CombatStatusKind.Fortify,CombatStatusKind.Thorns,CombatStatusKind.Focus};
        private static readonly string[] RewardNames={"집중","보호막","잔불","노출","파열","재생","약화","관통","수호","경쾌","유도","상처","요새화","가시","집중"};
        private static readonly string[][] Titles={null,
            new[]{"긴 집행선","넓은 유리날","집중 집행","퍼지는 종결","무거운 종결","종결 후 집중","깊은 파열","돌진 방호","처치 회복"},
            new[]{"선명한 기록","완전한 원본","두 번 읽기","세 번째 기록","무거운 재현","기록 방호","먼 기록점","파열의 필사","잔불의 필사"},
            new[]{"먼 기준점","깊은 견인","넓은 견인","두 번째 강타","두 번째 방호","다시 조율","역상 조율","상처의 견인","노출의 견인"},
            new[]{"먼 위성","넓은 관측","관측 방호","오래 남는 성역","두터운 성역","치유 성역","원격 포격","위성의 집중","위성의 수호"}};
        private static readonly string[][] Descriptions={null,
            new[]{"돌진 거리 +0.9.","돌진 공격 반폭 +0.35.","돌진 피해 +6.","궁극기 파열 반경 +0.6.","궁극기 파열 피해 +8.","궁극기 방출 뒤 집중 4 · 2턴.","돌진으로 새로 남기는 파열 +3.","돌진으로 적을 맞히면 보호막 6 · 1턴.","돌진으로 처치하면 체력 4 회복."},
            new[]{"일반 기록의 재현 피해 +4.","기록을 예약해도 이번 작도의 피해가 줄지 않습니다.","일반 기록이 다음 두 번의 방출에 재현됩니다.","궁극기 도안을 한 번 더 재현합니다.","궁극기 재현 피해 +6.","궁극기 기록이 재현되면 보호막 6 · 1턴.","기록 위치 지정 거리 +1.","일반 기록에 맞은 적에게 파열 3 · 2턴.","일반 기록에 맞은 적에게 잔불 3 · 2턴."},
            new[]{"기준점 조정 거리 +1.","견인 이동 거리 +0.8.","견인 범위 반경 +0.8.","궁극기의 두 번째 작도 피해 +4.","두 번째 작도를 방출하면 보호막 8 · 1턴.","두 번째 조립 시작 시 조율/견인 기회를 다시 얻습니다. 이동은 갱신하지 않습니다.","조율 조준 중 R로 도안을 반전할 수 있습니다.","견인으로 움직인 적에게 상처 3 · 2턴. 이번 이동에는 소급 적용하지 않습니다.","견인으로 움직인 적에게 노출 3 · 2턴."},
            new[]{"위성 설치 거리 +1.","위성의 자가 적중 반경 +0.3.","위성이 작도에 닿으면 보호막 4 · 1턴.","성역이 적 행동 한 번 더 유지됩니다.","성역에서 받는 보호막 +6.","성역의 보호막을 받을 때 체력 3 회복.","위성을 원점으로 선택한 작도의 피해 +3.","위성을 설치하거나 옮기면 집중 3 · 2턴.","위성을 설치하거나 옮기면 수호 1 · 2턴."}};
        private static readonly ApproachGrowthEntry[][] Entries=Build();
        public static bool IsId(string id)=>id!=null && id.StartsWith("path.",StringComparison.Ordinal);
        public static int Parent(int index) => index==0?-1:ParentLocal[(index-1)%18]<0?0:1+(index-1)/18*18+ParentLocal[(index-1)%18];
        public static int Depth(int index) { int n=0;while(index>0){index=Parent(index);n++;}return n; }
        public static ApproachGrowthEntry Get(CombatApproach approach,int index)=>Entries[(int)approach][index];
        public static ApproachGrowthEntry Find(string id)
        {
            if(StyleTreeCatalog.IsId(id))return StyleTreeCatalog.Find(id);
            if(!IsId(id)||id.Length<2)return null;
            int n=(id[id.Length-2]-'0')*10+id[id.Length-1]-'0';if(n<0||n>=Count)return null;
            for(int a=1;a<=4;a++)if(id==Entries[a][n].Node.Id)return Entries[a][n];return null;
        }
        public static GrowthNodeDefinition[] Nodes(CombatApproach approach)
        { var result=new GrowthNodeDefinition[Count];for(int i=0;i<Count;i++)result[i]=Get(approach,i).Node;return result; }
        private static ApproachGrowthEntry[][] Build()
        {
            var all=new ApproachGrowthEntry[5][];
            for(int a=1;a<=4;a++)
            {
                var entries=all[a]=new ApproachGrowthEntry[Count];
                for(int i=0;i<Count;i++)
                {
                    int local=(i-1)%18,branch=(i-1)/18;var e=new ApproachGrowthEntry();entries[i]=e;
                    string name,description;int cost=i==0?0:local<3?2:1;
                    if(i==0){name=Names[a];description="출발할 때 선택한 전투 기술과 전용 궁극기입니다. 아래에서 위로 능력을 확장하세요.";}
                    else if(local<3)
                    {int n=branch*3+local;e.Rule=(ApproachRule)(1+(a-1)*9+n);name=Titles[a][n];description=Descriptions[a][n];}
                    else
                    {
                        int trigger=local-3;
                        // Every condition is usable by this approach, not a dormant cross-character node.
                        if(trigger==9 && a!=2)trigger=7;
                        if(trigger==13 && a!=3)trigger=6;
                        if(trigger==14 && a!=4)trigger=3;
                        int reward=(local-3+branch*5+a-1)%Rewards.Length;
                        e.Trigger=trigger;e.Status=Rewards[reward];
                        e.Enemy=e.Status==CombatStatusKind.Burn || e.Status==CombatStatusKind.Exposure || e.Status==CombatStatusKind.Rupture || e.Status==CombatStatusKind.Weaken || e.Status==CombatStatusKind.Guidance || e.Status==CombatStatusKind.Wound;
                        e.Magnitude=e.Status==CombatStatusKind.Ward||e.Status==CombatStatusKind.Haste?1:2;
                        string[] shortNames={"짧은 작도","긴 작도","이동 연계","자기 적중","다중 적중","단독 적중","응축 연계","기술 연계","궁극 연계","기록 연계","위기 대응","방어 연계","파열 연계","두 번째 식","위성 적중"};
                        name=shortNames[trigger]+" · "+RewardNames[reward];
                        description=Conditions[trigger]+(e.Enemy?" 이번 작도에 맞은 생존 적에게 ":" 자신에게 ")+RewardNames[reward]+" "+e.Magnitude+" · 2턴. 방출마다 한 번.";
                    }
                    string id=Prefix[a]+i.ToString("00");int parent=Parent(i);
                    e.Node=new GrowthNodeDefinition(id,a<3?GrowthOwner.Ian:GrowthOwner.Luna,Names[a],name,description,"",cost,
                        i==0?"출발 능력":local<3?"운용 변경":"조건부 연계","","",i>0&&(local==1||local==2)?Prefix[a]+"choice."+branch:"","",0,
                        parent<0?Array.Empty<string>():new[]{Prefix[a]+parent.ToString("00")},0,Array.Empty<string>(),GrowthImplementationStatus.PrototypePartial);
                }
            }
            return all;
        }
    }
}
