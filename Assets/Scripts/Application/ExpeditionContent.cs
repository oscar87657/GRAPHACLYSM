using System;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    // New economy journals opt in to these definitions. Do not mutate the older generators.
    public static class ExpeditionContent
    {
        private static readonly RoomStory[] Shops = {
            new RoomStory("유리 상인의 좌판", "전투에서 모은 은화로 이번 원정을 준비합니다.", new RoomChoice("떠난다", "은화는 다음 상점까지 보관됩니다", RoomEffect.Leave)),
            new RoomStory("밤의 교환소", "다른 층에서 건져 올린 도구와 기록이 진열되어 있습니다.", new RoomChoice("떠난다", "은화는 다음 상점까지 보관됩니다", RoomEffect.Leave)),
            new RoomStory("마지막 행상", "다음 관문에 필요한 물건을 골라 보세요.", new RoomChoice("떠난다", "은화는 다음 상점까지 보관됩니다", RoomEffect.Leave))
        };
        private static readonly RoomStory[] Events = {
            new RoomStory("부서진 금고", "잠긴 금고의 틈에서 은화가 보입니다.",
                new RoomChoice("유리 틈을 벌린다", "체력 5 소모 · 은화 +16", RoomEffect.Coins,16,5),
                new RoomChoice("바닥의 동전만 줍는다", "은화 +5", RoomEffect.Coins,5)),
            new RoomStory("떠돌이 연구자", "연구자는 당신의 작도를 기록하고 싶어 합니다.",
                new RoomChoice("연구에 참여한다", "체력 4 소모 · 연구권 +2", RoomEffect.Research,2,4),
                new RoomChoice("남은 기록을 받는다", "연구권 +1", RoomEffect.Research,1)),
            new RoomStory("빛바랜 거래 장부", "잃어버린 물품 목록과 보수가 남아 있습니다.",
                new RoomChoice("물품을 찾아 돌려준다", "체력 6 소모 · 은화 +20", RoomEffect.Coins,20,6),
                new RoomChoice("쓸 만한 파편을 고른다", "카드 하나 선택", RoomEffect.CardReward)),
            new RoomStory("작은 모닥불", "다른 여행자가 자리를 내어 줍니다.",
                new RoomChoice("상처를 돌본다", "최대 체력의 20% 회복", RoomEffect.Heal,20),
                new RoomChoice("남은 연구 기록을 받는다", "연구권 +1", RoomEffect.Research,1))
        };
        private static readonly RoomStory[] Rests = {
            RoomStoryCatalog.Rest,
            new RoomStory("달빛 약수", "마실 물과 다음 전투를 위한 부적이 놓여 있습니다.",
                new RoomChoice("약수를 마신다", "최대 체력의 35% 회복", RoomEffect.Heal,35),
                new RoomChoice("부적을 챙긴다", "다음 전투 시작 보호막 +10", RoomEffect.Supply,10)),
            new RoomStory("기록자의 야영지", "쉬면서 다음 작도를 연구할 수 있습니다.",
                new RoomChoice("짧게 쉰다", "최대 체력의 20% 회복", RoomEffect.Heal,20),
                new RoomChoice("기록을 연구한다", "연구권 +1", RoomEffect.Research,1),
                new RoomChoice("호흡을 맞춘다", "공명 +3", RoomEffect.Resonance,3))
        };
        private static readonly RoomStory[] Workshops = {
            RoomStoryCatalog.Workshop,
            new RoomStory("깨진 도구의 공방", "고칠 도구와 교환할 파편을 살펴봅니다.",
                new RoomChoice("도구를 연마한다", "체력 3 소모 · 원정 그래프 피해 +1 (최대 +3)", RoomEffect.Training,1,3),
                new RoomChoice("남은 재료를 판다", "은화 +8", RoomEffect.Coins,8),
                new RoomChoice("불필요한 기록을 덜어낸다", "카드 1장 제거", RoomEffect.RemoveCard)),
            new RoomStory("야간 연구실", "도구 대신 다음 도안의 재료를 정리합니다.",
                new RoomChoice("연구를 마친다", "체력 4 소모 · 연구권 +2", RoomEffect.Research,2,4),
                new RoomChoice("기록을 정리한다", "카드 1장 제거", RoomEffect.RemoveCard),
                new RoomChoice("보호 부품을 챙긴다", "다음 전투 시작 보호막 +8", RoomEffect.Supply,8))
        };
        private static readonly string[] EncounterNames = { "돌격 잔영", "좌우 협공", "사선 호위대", "봉인의 순찰", "연결된 방진", "격앙의 추격대", "이동 포대", "매듭의 경비대" };

        public static RunMapDefinition Create(RunMapDefinition original, CharacterDefinition c, uint seed, bool expanded = false)
        {
            var random = new XorShiftRandom(seed ^ 0x769F04B1u);
            var nodes = new RunMapNodeDefinition[original.NodeCount];
            int branch = 0, previousLayer = -1, usedEncounters = 0, usedRests = 0, usedWorkshops = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = original.GetNode(i);
                if (previousLayer != node.Layer) { branch = 0; previousLayer = node.Layer; usedEncounters = usedRests = usedWorkshops = 0; }
                int option = branch++, depth = node.Layer % 8;
                var kind = node.Kind; var story = node.Story; var battle = node.Battle;
                string name = node.DisplayName;
                if (depth == 4)
                {
                    kind = option == 0 ? RunNodeKind.Shop : option == 1 ? RunNodeKind.Treasure : RunNodeKind.Event;
                    story = kind == RunNodeKind.Shop ? Shops[node.Layer / 8] : kind == RunNodeKind.Treasure
                        ? RoomStoryCatalog.Treasure : Events[random.Next(Events.Length)];
                    battle = null;
                }
                else if (node.Layer == 1 && option == 2 && kind == RunNodeKind.Rest) story = Rests[2];
                else if (node.Layer > 1 && battle == null)
                {
                    if (kind == RunNodeKind.Rest)
                    { int pick = random.Next(Rests.Length); while ((usedRests & (1 << pick)) != 0) pick = (pick+1)%Rests.Length;
                        usedRests |= 1 << pick; story = Rests[pick]; }
                    else if (kind == RunNodeKind.Workshop)
                    { int pick = random.Next(Workshops.Length); while ((usedWorkshops & (1 << pick)) != 0) pick = (pick+1)%Workshops.Length;
                        usedWorkshops |= 1 << pick; story = Workshops[pick]; }
                    else if (kind == RunNodeKind.Event)
                    {
                        int pick = random.Next(RoomStoryCatalog.EventCount + Events.Length);
                        story = pick < RoomStoryCatalog.EventCount ? RoomStoryCatalog.Event(pick) : Events[pick - RoomStoryCatalog.EventCount];
                    }
                }
                if (story != null) name = story.Title;
                if (node.Layer >= 5 && battle != null && kind != RunNodeKind.Boss)
                {
                    int templates=expanded?11:EncounterNames.Length;
                    int template = (random.Next(templates) + option + node.Layer) % templates;
                    while ((usedEncounters & (1 << template)) != 0) template = (template+1)%templates;
                    usedEncounters |= 1 << template;
                    name = template<8?EncounterNames[template]:template==8?"고리의 파수대":template==9?"십자 사격진":"쌍점 폭격대";
                    battle = Encounter(template, node.Layer / 8, kind == RunNodeKind.Elite, c, original.PlayerMaxHealth, random);
                }
                else if ((node.Layer == 2 || node.Layer == 3) && battle != null)
                {
                    // Preserve the lesson and fragile wave, but vary exact geometry across seeds and branches.
                    var enemies = new EnemyDefinition[battle.EnemyCount];
                    double shift = (random.Next(5) - 2) * .15;
                    for (int e = 0; e < enemies.Length; e++)
                    {
                        var old = battle.GetEnemy(e);
                        enemies[e] = new EnemyDefinition(old.Id, old.DisplayName, old.X + shift, old.Y, old.MaxHealth, old.Attack, old.Behavior);
                    }
                    battle = new BattleDefinition(original.PlayerMaxHealth, c.MaxEnergy, enemies, c.Archetype, fragments: true);
                }
                int[] edges = new int[node.NextNodeCount];
                for (int e = 0; e < edges.Length; e++) edges[e] = node.GetNextNodeIndex(e);
                if (depth == 3)
                {
                    int first = i + 1;
                    while (original.GetNode(first).Layer == node.Layer) first++;
                    int count = 0;
                    while (first + count < original.NodeCount && original.GetNode(first + count).Layer == node.Layer + 1) count++;
                    edges = new[] { first, first + (count == 3 && option % 2 == 1 ? 2 : 1) };
                }
                nodes[i] = new RunMapNodeDefinition(node.Id, name, node.Layer, node.Lane, kind, battle, edges, story, original.PlayerMaxHealth);
            }
            return new RunMapDefinition(nodes, original.RoomsPerFloor);
        }

        private static BattleDefinition Encounter(int template, int floor, bool elite, CharacterDefinition c, int health, XorShiftRandom random)
        {
            int count = elite ? 3 : 2 + random.Next(2);
            var enemies = new EnemyDefinition[count];
            double shift = (random.Next(7) - 3) * .2;
            for (int i = 0; i < count; i++)
            {
                var behavior = EnemyBehaviorDefinition.SteadyAttack();
                string name = "유리 잔영";
                double x = i == 0 ? 2.4 : i == 1 ? 7.6 : 5.4;
                double y = i == 2 ? 2.6 : (template % 2 == 0 ? .5 : 1.7);
                switch (template)
                {
                    case 0: behavior = i == 0 ? EnemyBehaviorDefinition.ChargeBurst() : EnemyBehaviorDefinition.SteadyAttack(); name = i == 0 ? "돌격대장" : "유리 척후"; break;
                    case 1: behavior = EnemyBehaviorDefinition.Skirmisher(10 - x, -1.6); name = "양면 추격자"; break;
                    case 2: behavior = i == 0 ? EnemyBehaviorDefinition.LineGunner() : EnemyBehaviorDefinition.ChargeBurst(); name = i == 0 ? "유리 포수" : "포대 호위병"; break;
                    case 3: behavior = i == 0 ? EnemyBehaviorDefinition.SealScribe() : EnemyBehaviorDefinition.AlternatingPosition(10 - x, -y); name = i == 0 ? "봉인 기록관" : "밤의 순찰자"; break;
                    case 4: behavior = i == 0 ? EnemyBehaviorDefinition.LinkGuardian() : EnemyBehaviorDefinition.SteadyAttack(); name = i == 0 ? "방진 수호자" : "방진 보병"; break;
                    case 5: behavior = i == 0 ? EnemyBehaviorDefinition.EscalatingAttack() : EnemyBehaviorDefinition.Skirmisher(10 - x, -1.2); name = i == 0 ? "격앙한 집행자" : "추격 잔영"; break;
                    case 6: behavior = i == 0 ? EnemyBehaviorDefinition.LineGunner() : EnemyBehaviorDefinition.Skirmisher(10 - x, -1.5); name = i == 0 ? "이동대 포수" : "이동대 척후"; break;
                    case 7: behavior = i == 0 ? EnemyBehaviorDefinition.LinkGuardian() : EnemyBehaviorDefinition.SealScribe(); name = i == 0 ? "매듭 파수꾼" : "매듭 서기관"; break;
                    case 8: behavior = i==0 ? EnemyBehaviorDefinition.RingSentinel() : EnemyBehaviorDefinition.Skirmisher(10-x,-1.2); name=i==0?"환형 파수꾼":"고리 척후"; break;
                    case 9: behavior = i==0 ? EnemyBehaviorDefinition.CrossGunner() : EnemyBehaviorDefinition.SteadyAttack(); name=i==0?"십자 포수":"사격진 보병"; break;
                    case 10: behavior = i==0 ? EnemyBehaviorDefinition.TwinMortar() : EnemyBehaviorDefinition.ChargeBurst(); name=i==0?"쌍점 폭격수":"폭격대 호위"; break;
                }
                enemies[i] = new EnemyDefinition("expedition." + i, name, x + shift, y,
                    25 + floor * 18 + (elite ? 12 : 0) + i * 3, 3 + floor * 2 + (elite ? 2 : 0), behavior);
            }
            var terrain = new[] {
                new BattleTerrainDefinition("expedition.prism", BattleTerrainKind.Prism, 5, 0, .48),
                new BattleTerrainDefinition("expedition.device", template % 3 == 0 ? BattleTerrainKind.Capacitor
                    : template % 3 == 1 ? BattleTerrainKind.Aegis : BattleTerrainKind.Pulse, 8, -2.5, .6)
            };
            return new BattleDefinition(health, c.MaxEnergy, enemies, c.Archetype, fragments: true, terrain: terrain);
        }
    }
}
