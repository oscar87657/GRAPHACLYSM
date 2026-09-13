using System;
using System.Collections.Generic;

namespace Graphaclysm.Core.Combat
{
    public sealed class EnemyArchiveEntry
    {
        public EnemyArchiveEntry(EnemyBehaviorKind kind,string name,string pattern,string counter,int health,int attack)
        { Kind=kind;Name=name;Pattern=pattern;Counter=counter;Health=health;Attack=attack;Stats="기본 체력 "+health+" · 기본 공격 "+attack; }
        public EnemyBehaviorKind Kind { get; }
        public string Name { get; }
        public string Pattern { get; }
        public string Counter { get; }
        public int Health { get; }
        public int Attack { get; }
        public string Stats { get; }
        public bool IsBoss => Kind>=EnemyBehaviorKind.GlassRegent && Kind<=EnemyBehaviorKind.AstralSovereign
            || Kind>=EnemyBehaviorKind.LatticeArchitect && Kind<=EnemyBehaviorKind.NightConductor;
        public EnemyDefinition Create(string id,double x,double y,int floor=0)
        {
            EnemyBehaviorDefinition behavior;
            switch(Kind)
            {
                case EnemyBehaviorKind.SteadyAttack: behavior=EnemyBehaviorDefinition.SteadyAttack();break;
                case EnemyBehaviorKind.ChargeBurst: behavior=EnemyBehaviorDefinition.ChargeBurst();break;
                case EnemyBehaviorKind.AlternatingPosition: behavior=EnemyBehaviorDefinition.AlternatingPosition(10-x,-y);break;
                case EnemyBehaviorKind.EscalatingAttack: behavior=EnemyBehaviorDefinition.EscalatingAttack();break;
                case EnemyBehaviorKind.Skirmisher: behavior=EnemyBehaviorDefinition.Skirmisher(10-x,-y);break;
                case EnemyBehaviorKind.LineGunner: behavior=EnemyBehaviorDefinition.LineGunner();break;
                case EnemyBehaviorKind.SealScribe: behavior=EnemyBehaviorDefinition.SealScribe();break;
                case EnemyBehaviorKind.LinkGuardian: behavior=EnemyBehaviorDefinition.LinkGuardian();break;
                case EnemyBehaviorKind.RingSentinel: behavior=EnemyBehaviorDefinition.RingSentinel();break;
                case EnemyBehaviorKind.CrossGunner: behavior=EnemyBehaviorDefinition.CrossGunner();break;
                case EnemyBehaviorKind.TwinMortar: behavior=EnemyBehaviorDefinition.TwinMortar();break;
                default: behavior=EnemyBehaviorDefinition.Archive(Kind);break;
            }
            return new EnemyDefinition(id,Name,x,y,Health+(IsBoss?0:floor*7),Attack+(IsBoss?0:floor),behavior);
        }
    }
    public static class EnemyArchive
    {
        private static readonly EnemyArchiveEntry[] entries={
            new EnemyArchiveEntry(EnemyBehaviorKind.SteadyAttack,"유리 보병","매 턴 원형 조준 공격 · 체력 피격 시 약화","조준 원을 피하거나 약화로 공격을 줄이세요.",24,4),
            new EnemyArchiveEntry(EnemyBehaviorKind.ChargeBurst,"축적 돌격병","보호막 축적 → 두 배 공격 · 잔불","축적 턴에 준비하고 다음 턴 큰 원을 피하세요.",30,4),
            new EnemyArchiveEntry(EnemyBehaviorKind.AlternatingPosition,"왕복 정찰병","이동 → 공격 → 원위치 이동 → 공격","상처는 이동을 벌하고, 고정은 이동을 막습니다.",23,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.EscalatingAttack,"격앙 집행자","매 턴 공격 · 두 턴마다 위력 +1, 최대 +4","오래 두지 않거나 약화를 유지하세요.",32,4),
            new EnemyArchiveEntry(EnemyBehaviorKind.Skirmisher,"기동 추격자","이동 → 공격 → 공격","이동할 자리에 도안을 준비하세요.",25,4),
            new EnemyArchiveEntry(EnemyBehaviorKind.LineGunner,"선형 포수","고정 방향 사선 · 체력 피격 시 방벽 약화","옆으로 피하거나 견인으로 사선을 옮기세요.",28,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.SealScribe,"봉인 서기관","봉인 예고 → 봉쇄 유지와 공격","중앙 표식을 작도로 맞히면 봉인을 풉니다.",27,4),
            new EnemyArchiveEntry(EnemyBehaviorKind.LinkGuardian,"연결 수호자","아군 작도 피해 −4 · 홀수 턴 수호","본체와 연결 매듭을 함께 맞히세요. 수호는 상태 적용 한 건을 막습니다.",32,3),
            new EnemyArchiveEntry(EnemyBehaviorKind.RingSentinel,"고리 감시자","매 턴 보호막3 · 몸 주위 고리 공격","작은 원 안/큰 원 밖으로 이동. 관통·견인이 유효합니다.",30,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.CrossGunner,"십자 포수","가로·세로 지면 포격 · 방벽 약화","대각선으로 이동하세요. 견인해도 지면 조준은 남습니다.",27,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.TwinMortar,"쌍점 포격수","현재 조준점과 반대편 동시 폭격 · 잔불","두 원 밖으로 피하세요. 겹쳐도 피해는 한 번입니다.",28,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.RotatingGun,"회전 포탑","직선 → 십자 → 반복","직선 턴에는 견인, 십자 턴에는 대각선 회피가 유효합니다.",31,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.HaloWeaver,"환월 직조자","몸 주위 고리 → 지면 쌍점 → 반복","몸을 옮겨 취소할 수 있는 공격인지 구분하세요.",33,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.RepairPriest,"유리 수복사","생존 아군 전체 회복5 → 원형 공격","회복 턴 전에 먼저 처치하세요. 죽은 적은 되살리지 않습니다.",24,3),
            new EnemyArchiveEntry(EnemyBehaviorKind.ShieldLeech,"방패 거머리","원형 공격 · 체력 피해를 주면 보호막6 획득","완전히 막거나 피하면 보호막을 얻지 못합니다.",27,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.AshHunter,"재를 밟는 사냥꾼","이동 → 잔불 공격 → 잔불 공격","상처로 이동을 벌하거나 수호·정화로 잔불을 막으세요.",28,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.HexCaller,"금기의 주술사","수호1·보호막5 준비 → 십자 공격","준비 때 강화 제거, 공격 때 대각선 이동을 노리세요.",29,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.GlassRegent,"흑유리 섭정","1층 보스: 직선 → 십자 → 고리 → 보호막12","체력 절반 이하의 다음 턴부터 순서가 한 칸 이동하고 공격 +2. 공격 예고를 확인하세요.",120,6),
            new EnemyArchiveEntry(EnemyBehaviorKind.ArchiveKeeper,"침묵의 서고장","2층 보스: 십자 → 고리 → 쌍점 → 아군 회복8","체력 절반 이하에는 고리부터 순환, 공격 +2. 회복을 돕는 수복사를 먼저 처리하세요.",170,7),
            new EnemyArchiveEntry(EnemyBehaviorKind.AstralSovereign,"무명의 천문왕","3층 보스: 고리 → 직선 → 십자 → 보호막12·수호1","체력 절반 이하에는 직선부터 순환, 공격 +2. 정화·관통·견인을 역할에 맞게 사용하세요.",220,8),
            new EnemyArchiveEntry(EnemyBehaviorKind.GridSentry,"격자 파수꾼","격자 공격 두 번 → 보호막4. 매 턴 격자 위치 교대","붉은 선 사이의 빈 칸으로 이동하세요. 견인해도 지면 격자는 남습니다.",35,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.LaneArtillerist,"횡단 포격수","교대 가로 포격 두 번 → 보호막4","이번 턴에 표시된 가로 띠를 벗어나세요. 다음 턴에는 안전한 줄이 바뀝니다.",33,5),
            new EnemyArchiveEntry(EnemyBehaviorKind.LatticeArchitect,"격자의 설계자","4층 보스: 격자·가로 띠·체크무늬 순환. 네 턴마다 보호막12","선 사이 → 띠 사이 → 빈 타일로 회피. 체력 절반 이하에는 순서 변경·공격+2.",270,9),
            new EnemyArchiveEntry(EnemyBehaviorKind.EclipseEmperor,"일식의 군주","5층 보스: 체크무늬·격자·가로 띠 순환. 네 턴마다 회복12·수호1","빈 타일도 다음 턴에는 위험해집니다. 체력 절반 이하에는 순서 변경·공격+2.",340,10),
            new EnemyArchiveEntry(EnemyBehaviorKind.MirrorDuelist,"거울의 결투사","1층 후보: 쌍점 → 직선 → 쌍점 → 보호막12","쌍점은 두 원 밖으로, 직선은 옆으로 회피. 체력 절반 이하에는 순서 변경·공격+2.",120,6),
            new EnemyArchiveEntry(EnemyBehaviorKind.AshBishop,"잿빛 주교","2층 후보: 십자 → 가로 띠 → 쌍점 → 보호막12","대각선 회피와 안전한 가로 줄을 번갈아 찾으세요. 체력 절반 이하에는 순서 변경·공격+2.",170,7),
            new EnemyArchiveEntry(EnemyBehaviorKind.OrbitDevourer,"궤도를 삼키는 자","3층 후보: 고리 → 가로 띠 → 직선 → 보호막12","고리 중앙으로 접근한 뒤 가로 띠를 피하세요. 체력 절반 이하에는 순서 변경·공격+2.",220,8),
            new EnemyArchiveEntry(EnemyBehaviorKind.VoidCartographer,"공백의 측량사","4층 후보: 체크무늬 → 쌍점 → 격자 → 보호막12","빈 칸에 머무르기만 하면 다음 쌍점의 표적이 됩니다. 체력 절반 이하에는 순서 변경·공격+2.",270,9),
            new EnemyArchiveEntry(EnemyBehaviorKind.NightConductor,"종야의 지휘자","5층 후보: 격자 → 고리 → 체크무늬 → 보호막12","전장 전체 회피와 보스와의 거리 조절을 번갈아 요구합니다. 체력 절반 이하에는 순서 변경·공격+2.",340,10)
        };
        public static IReadOnlyList<EnemyArchiveEntry> All => entries;
        public static EnemyArchiveEntry Find(EnemyBehaviorKind kind) => entries[(int)kind];
    }
}
