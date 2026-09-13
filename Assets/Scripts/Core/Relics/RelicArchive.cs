using System;
using System.Collections.Generic;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Relics
{
    public enum RelicTrigger { None,First,Short,Long,Moved,Still,Self,Shared,Multiple,Solo,Condensed,Wounded,Shielded,Burning,Cracked,Guided,Rooted,Boss,Prism,Alternating }
    public enum RelicReward { Focus,Shield,Pierce,Ward,Regeneration,Fortify,Haste,Thorns,Burn,Weaken,Guidance,Wound }
    public static class RelicArchive
    {
        private static readonly string[] conditions={"","첫 방출","파편 3개 이하 방출","파편 6개 이상 방출","이동 후 방출","이동하지 않고 방출","자가 적중","자신과 적 동시 적중","적 2명 이상 적중","적 1명만 적중","응축 후 방출","체력 절반 이하에서 방출","보호막이 있을 때 방출","잔불 상태의 적 적중","균열 상태의 적 적중","유도 상태의 적 적중","고정 상태의 적 적중","보스 적중","프리즘을 지난 방출","홀수 턴 방출"};
        private static readonly string[] rewards={"집중","보호막","방패 관통","수호","재생","요새화","경쾌","가시","잔불","약화","유도","찢긴 상처"};
        private static readonly string[] names={
            "첫 문장의 잉크|새벽 유리잔|개문자의 송곳|여명의 호부",
            "세 줄의 방패|소형 구급함|접힌 망루|가벼운 구두",
            "장문의 날개|일곱 가시 책갈피|긴 밤의 화로|침묵의 인장",
            "여행자의 재|바람을 묶는 끈|길잡이 바늘|갈라진 발자국",
            "정좌의 필사침|고요한 유리벽|잠복자의 창끝|침묵의 수호패",
            "몸에 새긴 별|흰 붕대 두루마리|내면의 성벽|안쪽의 발판",
            "쌍성의 신발|서로 맞물린 가시|함께 타는 심지|공동의 판결문",
            "합창의 재통|군세를 누르는 추|무리를 엮는 실|산개한 유리조각",
            "독주의 펜촉|결투사의 버클|일점의 바늘|고독한 성패",
            "압축된 부적|숙성의 약병|겹겹의 방어판|늦게 뜨는 바람",
            "벼랑의 날개|상처의 장미|붉은 역전극|최후의 경고장",
            "벽 안의 불씨|완충의 천칭|보호자의 인력추|성벽의 깨진 돌",
            "잿빛 기록봉|불연성 유리함|탄화된 창날|불씨 수호석",
            "균열 봉합패|흑유리 연고|틈새의 성채|파쇄 보행구",
            "견인 고리신|끌려온 가시관|인력의 화덕|조석 억제기",
            "붙잡힌 불꽃|뿌리의 무게추|묶인 별의 실|고정된 단검",
            "왕을 쓰는 펜|관문의 방패|왕관 파쇄침|옥좌의 금부",
            "굴절 수호석|분광 약병|칠색 망루|무지개 발걸음",
            "엇박자 날개|격일의 가시|한 박자 늦은 재|번갈아 닫는 봉인"
        };
        public static CombatStatusKind RewardStatus(RelicReward reward)
        {
            switch(reward)
            {
                case RelicReward.Focus:return CombatStatusKind.Focus;case RelicReward.Shield:return CombatStatusKind.Shield;
                case RelicReward.Pierce:return CombatStatusKind.Pierce;case RelicReward.Ward:return CombatStatusKind.Ward;
                case RelicReward.Regeneration:return CombatStatusKind.Regeneration;case RelicReward.Fortify:return CombatStatusKind.Fortify;
                case RelicReward.Haste:return CombatStatusKind.Haste;case RelicReward.Thorns:return CombatStatusKind.Thorns;
                case RelicReward.Burn:return CombatStatusKind.Burn;case RelicReward.Weaken:return CombatStatusKind.Weaken;
                case RelicReward.Guidance:return CombatStatusKind.Guidance;default:return CombatStatusKind.Wound;
            }
        }
        public static RelicDefinition[] Build(IReadOnlyList<RelicDefinition> old)
        {
            var result=new RelicDefinition[100];
            for(int i=0;i<old.Count;i++)
            {
                var r=old[i];int n=r.Magnitude;var effect=r.Effect;
                string description=r.Description.Replace("파열","균열").Replace("추진","집중");
                if(i==3){n=5;description="전투 승리 시 체력5 회복.";}
                if(i==4){effect=RelicEffectKind.PrismDamage;n=3;description="프리즘을 지난 작도 피해 +3.";}
                if(i==6){n=8;description="전투 시작 보호막8. 첫 적 행동까지 유지.";}
                if(i==8){n=3;description="자가 적중 시 보호막 +3.";}
                if(i==9){n=5;description="전투의 첫 방출 피해 +5.";}
                if(i==11){n=6;description="파편6개 이상 방출 피해 +6.";}
                if(i==14){n=5;description="응축 시 보호막5. 응축 체력 비용은 막지 못합니다.";}
                if(i==17)description="이동 후 집중 2 획득.";
                if(i==20)description="방출로 적 하나만 맞히면 고정 · 1턴.";
                if(i==22)description="응축 후 방출로 맞힌 적에게 균열 3.";
                result[i]=new RelicDefinition(r.Id,r.DisplayName,description,effect,n,r.ImageResource);
                if(i==5) result[i]=new RelicDefinition(r.Id,r.DisplayName,"이동 후 방출하면 경쾌 1 · 2턴.",RelicEffectKind.Conditional,1,r.ImageResource,RelicTrigger.Moved,RelicReward.Haste);
            }
            for(int c=0;c<19;c++)
            {
                var titles=names[c].Split('|');
                for(int v=0;v<4;v++)
                {
                    var reward=(RelicReward)((c*3+v)%12);bool enemy=(int)reward>=8;
                    int n=reward==RelicReward.Ward || reward==RelicReward.Haste || reward==RelicReward.Guidance?1:reward==RelicReward.Shield?5:3;
                    string effect=enemy?"적중한 생존 적에게 ":"자신에게 ";
                    string desc=conditions[c+1]+" 시\n"+effect+rewards[(int)reward]+n+" · 2턴\n본 방출당 1회 · 추가 공격에서는 재발동 안 함";
                    result[24+c*4+v]=new RelicDefinition("relic.archive."+c+"."+v,titles[v],desc,RelicEffectKind.Conditional,n,trigger:(RelicTrigger)(c+1),reward:reward);
                }
            }
            return result;
        }
    }
}
