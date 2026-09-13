using System;
using Graphaclysm.Core.Combat;
namespace Graphaclysm.Core.Runs
{
    public static class StyleTreeCatalog
    {
        public const int Count=16;
        private static readonly int[] Source={1,2,37,38,3,39};
        private static readonly string[][] Titles={null,
            new[]{"귀환 집행","삼중 집행","긴 종결선","종결의 잔상","집중 파쇄","파열 방벽"},
            new[]{"거울 포격","몸을 따르는 기록","넓은 기록","쌍면 서가","전진하는 서가","집중 재현"},
            new[]{"견인 후 조율","온전한 회전","두 번의 조율","이어지는 도안","두 번째 조석","악장 회수"},
            new[]{"쌍원 포격","위성 귀환","별자리 도선","넓은 성역","몸을 따르는 성역","원격 성역"}};
        private static readonly string[][] Effects={null,
            new[]{"돌진 경로를 공격한 뒤 출발점으로 돌아옵니다.","돌진 경로 양쪽에도 평행 공격선을 만듭니다. 적마다 한 번만 피해를 줍니다.","돌진 거리 +1.8. 방향을 골라 더 먼 적까지 관통합니다.","궁극기 도안을 다음 방출에 한 번 재현해 피해 8. 카드 효과는 복제하지 않습니다.","궁극기 파열에 맞는 적 중 체력이 가장 많은 한 명에게 추가 피해 12.","궁극기 파열에 실제로 맞은 적마다 보호막 4 · 1턴."},
            new[]{"일반 기록을 지정 원점에서 반전한 도안으로 저장합니다. 원본과 다른 위치를 공격합니다.","일반 기록이 다음 방출마다 현재 몸 위치를 원점으로 삼습니다.","일반 기록의 적중 반경이 0.48에서 0.96으로 넓어집니다.","궁극기 기록을 반전한 도안으로 보존합니다. 일반 기록에는 적용하지 않습니다.","궁극기 기록이 재현될 때마다 원점이 오른쪽으로 0.75 이동합니다.","궁극기 기록이 적 하나에만 닿으면 그 재현 피해가 두 배가 됩니다."},
            new[]{"견인 뒤 같은 턴에 도안 조율을 한 번 더 사용할 수 있습니다. 추가 견인은 불가합니다.","조율의 회전 범위를 ±150도로 넓힙니다. 30도씩 조정합니다. 반전은 R로 따로 사용합니다.","한 턴에 도안 조율을 두 번 사용할 수 있습니다. 견인은 횟수를 늘리지 않습니다.","궁극기의 첫 도안을 보존해 두 번째 방출에 피해 8로 겹칩니다. 카드 효과는 복제하지 않습니다.","궁극기의 두 번째 방출에 맞은 적을 몸 쪽으로 최대 1 끌어옵니다. 고정된 적은 이동하지 않습니다.","궁극기의 두 번째 방출을 마치면 공명 2를 돌려받습니다. 추가 궁극기를 허용하지 않습니다."},
            new[]{"위성을 원점으로 방출할 때 몸을 원점으로 한 같은 도안을 추가 발사합니다. 추가 도안은 피해 6만 줍니다.","위성 설치·이동을 확정하면 몸도 그 위치로 전이합니다. 일반 이동 기회는 복원하지 않습니다.","작도가 위성에 닿으면 몸과 위성을 잇는 선 위의 적에게 추가 피해 6. 카드 효과는 복제하지 않습니다.","성역의 보호 판정 반경이 0.85에서 1.7로 넓어집니다.","성역이 몸의 이동량만큼 함께 이동합니다. 접촉 판정도 이동한 도안을 따릅니다.","위성이 있으면 성역 도안에 닿지 않아도 성역의 보호를 받습니다. 몸과 위성으로 중복 지급하지 않습니다."}};
        private static readonly ApproachGrowthEntry[][] Entries=Build();
        public static int Parent(int i)=>i==0?-1:(i-1)%5==0?0:(i-1)%5==1|| (i-1)%5==3?i-(i-1)%5:i-1;
        public static int Depth(int i){int d=0;while(i>0){i=Parent(i);d++;}return d;}
        public static ApproachGrowthEntry Get(CombatApproach a,int i)=>Entries[(int)a][i];
        public static bool IsId(string id)=>id!=null&&id.StartsWith("path.v35.",StringComparison.Ordinal);
        public static ApproachGrowthEntry Find(string id)
        {if(!IsId(id))return null;for(int a=1;a<=4;a++)for(int i=0;i<Count;i++)if(Entries[a][i].Node.Id==id)return Entries[a][i];return null;}
        public static GrowthNodeDefinition[] Nodes(CombatApproach a)
        {var n=new GrowthNodeDefinition[Count];for(int i=0;i<Count;i++)n[i]=Get(a,i).Node;return n;}
        private static ApproachGrowthEntry[][] Build()
        {
            var all=new ApproachGrowthEntry[5][];
            for(int a=1;a<=4;a++)
            {
                var entries=all[a]=new ApproachGrowthEntry[Count];string prefix="path.v35."+a+".";
                for(int i=0;i<Count;i++)
                {
                    int g=i==0?0:(i-1)/5,l=i==0?0:(i-1)%5;bool cap=i>0&&(l==2||l==4);
                    var old=ApproachGrowthCatalog.Get((CombatApproach)a,i==0?0:l==3?19+g:Source[g*2+(l==1?1:0)]);
                    int c=g+(l==4?3:0);int p=Parent(i);
                    entries[i]=new ApproachGrowthEntry {Rule=cap?(ApproachRule)(37+(a-1)*6+c):old.Rule,
                        Node=new GrowthNodeDefinition(prefix+i.ToString("00"),a<3?GrowthOwner.Ian:GrowthOwner.Luna,ApproachGrowthCatalog.Names[a],
                        cap?Titles[a][c]:old.Node.Name,cap?Effects[a][c]:old.Node.Effect,"",i==0?0:cap?2:1,
                        i==0?"출발 능력":cap?"각성":l==3?"궁극기 강화":"기술 강화","","",cap?prefix+(l==2?"skill":"ultimate"):"","",0,
                        p<0?Array.Empty<string>():new[]{prefix+p.ToString("00")},0,Array.Empty<string>(),GrowthImplementationStatus.PrototypePartial)};
                }
            }
            return all;
        }
    }
}
