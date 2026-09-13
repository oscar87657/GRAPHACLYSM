using System;

namespace Graphaclysm.Core.Runs
{
    public enum RoomEffect { Heal, Resonance, CardReward, RelicReward, RemoveCard, Leave, Training, GrowthPoints, Supply, Coins, Research }
    public sealed class RoomChoice
    {
        public RoomChoice(string title, string consequence, RoomEffect effect, int amount = 0, int healthCost = 0)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(consequence)) throw new ArgumentException("A choice needs readable consequences.");
            if (effect < RoomEffect.Heal || effect > RoomEffect.Research || amount < 0 || amount > 100 || healthCost < 0) throw new ArgumentOutOfRangeException(nameof(effect));
            Title = title; Consequence = consequence; Effect = effect; Amount = amount; HealthCost = healthCost;
        }
        public string Title { get; }
        public string Consequence { get; }
        public RoomEffect Effect { get; }
        public int Amount { get; }
        public int HealthCost { get; }
    }
    public sealed class RoomStory
    {
        private readonly RoomChoice[] choices;
        public RoomStory(string title, string body, params RoomChoice[] choices)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body) || choices == null || choices.Length < 1 || choices.Length > 3) throw new ArgumentException("A story needs one to three choices.");
            for (int i = 0; i < choices.Length; i++) if (choices[i] == null) throw new ArgumentException("Missing choice.");
            Title = title; Body = body; this.choices = (RoomChoice[])choices.Clone();
        }
        public string Title { get; }
        public string Body { get; }
        public int ChoiceCount => choices.Length;
        public RoomChoice GetChoice(int index) => choices[index];
    }
    public static class RoomStoryCatalog
    {
        public static readonly RoomStory Workshop = new RoomStory("흑유리 공방", "깨진 도구를 갈아 내는 소리가 들린다.\n손에 익은 도구를 벼릴까, 필요 없는 기록을 덜어낼까?",
            new RoomChoice("공격 도구를 벼린다","체력 4 소모 · 이번 원정의 그래프 피해 +1 (최대 +3)",RoomEffect.Training,1,4),
            new RoomChoice("기록을 정리한다","덱에서 카드 1장 제거",RoomEffect.RemoveCard),
            new RoomChoice("그대로 떠난다","변화 없음",RoomEffect.Leave));
        public static readonly RoomStory Observatory = new RoomStory("열린 관측소", "천장에 다음 성좌의 자리가 비어 있다.\n새 길을 배우려면 잠시 빛을 견뎌야 한다.",
            new RoomChoice("새 성좌를 배운다","체력 5 소모 · 성장점 +2",RoomEffect.GrowthPoints,2,5),
            new RoomChoice("달의 박자를 듣는다","공명 +2",RoomEffect.Resonance,2),
            new RoomChoice("떠난다","변화 없음",RoomEffect.Leave));
        public static readonly RoomStory Supply = new RoomStory("여행자의 보급함", "아직 열리지 않은 보급함이 놓여 있다.\n한 가지만 챙길 공간이 남았다.",
            new RoomChoice("보호 부적을 챙긴다","다음 전투 시작 보호막 +8 (다음 전투에만 적용)",RoomEffect.Supply,8),
            new RoomChoice("비상 약을 마신다","최대 체력의 15% 회복",RoomEffect.Heal,15),
            new RoomChoice("새 파편을 고른다","카드 하나 선택",RoomEffect.CardReward));
        public static readonly RoomStory Rest = new RoomStory("잔광의 쉼터", "낡은 관측 의자가 아직 온기를 품고 있다.\n다음 방으로 가기 전에, 흐트러진 호흡을 고를 수 있다.",
            new RoomChoice("눈을 감고 쉰다", "최대 체력의 30% 회복", RoomEffect.Heal, 30),
            new RoomChoice("도구의 박자를 맞춘다", "공명 +3", RoomEffect.Resonance, 3),
            new RoomChoice("불필요한 기록을 덜어낸다", "덱에서 카드 1장 제거", RoomEffect.RemoveCard));
        public static readonly RoomStory Treasure = new RoomStory("봉인된 수장고", "먼지 한 점 없는 유리장 안에서 오래된 물건들이 빛난다.\n문이 닫히기 전에 하나만 가져갈 수 있을 것 같다.",
            new RoomChoice("유리장을 연다", "미보유 유물 중 하나 선택", RoomEffect.RelicReward),
            new RoomChoice("남은 빛만 받아 간다", "최대 체력의 15% 회복", RoomEffect.Heal, 15));
        private static readonly RoomStory[] events = {
            new RoomStory("멈춘 시계", "초침이 같은 한 초를 되풀이한다.\n당신의 맥박을 빌려주면, 다시 움직일지도 모른다.",
                new RoomChoice("맥박을 나눈다", "체력 5 소모 · 공명 +4", RoomEffect.Resonance, 4, 5),
                new RoomChoice("고요를 듣는다", "최대 체력의 10% 회복", RoomEffect.Heal, 10)),
            new RoomStory("파편을 파는 사람", "얼굴 없는 상인이 빈 손을 내민다.\n그는 동전 대신, 아직 따뜻한 기억을 원한다.",
                new RoomChoice("기억을 건넨다", "체력 7 소모 · 유물 하나 선택", RoomEffect.RelicReward, 0, 7),
                new RoomChoice("그의 기록을 살핀다", "체력 3 소모 · 카드 하나 선택", RoomEffect.CardReward, 0, 3),
                new RoomChoice("지나간다", "변화 없음", RoomEffect.Leave)),
            new RoomStory("지워진 도서관", "책의 제목은 사라졌지만, 지우개만은 새것이다.\n들고 온 기록 하나를 이곳에 남겨도 아무도 묻지 않을 것이다.",
                new RoomChoice("한 페이지를 지운다", "덱에서 카드 1장 제거", RoomEffect.RemoveCard),
                new RoomChoice("빈 페이지를 채운다", "카드 하나 선택", RoomEffect.CardReward)),
            new RoomStory("거꾸로 뜬 별", "바닥의 물웅덩이에만 별이 떠 있다.\n손끝이 닿자 빛이 피부 아래로 스며든다.",
                new RoomChoice("별을 붙잡는다", "체력 4 소모 · 공명 +3", RoomEffect.Resonance, 3, 4),
                new RoomChoice("물로 손을 씻는다", "최대 체력의 20% 회복", RoomEffect.Heal, 20)),
            new RoomStory("서툰 작도", "먼저 지나간 누군가가 벽에 식을 남겼다.\n틀린 부분을 지울 수도, 그 낯선 궤적을 배울 수도 있다.",
                new RoomChoice("낯선 풀이를 배운다", "카드 하나 선택", RoomEffect.CardReward),
                new RoomChoice("내 기록부터 고친다", "체력 2 소모 · 카드 1장 제거", RoomEffect.RemoveCard, 0, 2)),
            new RoomStory("갈라진 연습장", "책상 위에 같은 적을 수없이 그린 종이가 쌓여 있다.\n남은 한 장에 자신의 선을 보탤 수 있다.",
                new RoomChoice("손이 떨릴 때까지 연습한다","체력 7 소모 · 이번 원정의 그래프 피해 +1 (최대 +3)",RoomEffect.Training,1,7),
                new RoomChoice("쓸 만한 기록만 가져간다","카드 하나 선택",RoomEffect.CardReward)),
            new RoomStory("빛을 잃은 안내자", "안내자는 자신의 등불과 당신의 기록을 번갈아 본다.\n조금만 빛을 나눠 주면 잊힌 길을 알려 주겠다고 한다.",
                new RoomChoice("등불에 빛을 나눈다","체력 6 소모 · 성장점 +3",RoomEffect.GrowthPoints,3,6),
                new RoomChoice("오늘은 지나간다","변화 없음",RoomEffect.Leave)),
            new RoomStory("유리 우산", "접힌 우산 위로 먼지가 비처럼 쏟아진다.\n딱 한 번은 무엇이든 막아 줄 것 같다.",
                new RoomChoice("우산을 챙긴다","다음 전투 시작 보호막 +10",RoomEffect.Supply,10),
                new RoomChoice("우산 속 빛을 마신다","최대 체력의 15% 회복",RoomEffect.Heal,15)),
            new RoomStory("두 개의 서명", "한 장에는 지나온 선이, 다른 장에는 처음 보는 선이 그려져 있다.\n어느 쪽을 남길지 고르라는 듯 펜이 놓여 있다.",
                new RoomChoice("지나온 선을 지운다","체력 3 소모 · 카드 1장 제거",RoomEffect.RemoveCard,0,3),
                new RoomChoice("새 길을 연구한다","체력 4 소모 · 성장점 +2",RoomEffect.GrowthPoints,2,4),
                new RoomChoice("아무것도 쓰지 않는다","변화 없음",RoomEffect.Leave)),
            new RoomStory("닫히지 않는 상자", "상자 틈으로 작은 금속음이 새어 나온다.\n뚜껑을 누르면 안쪽의 무언가도 당신을 누를 것이다.",
                new RoomChoice("손을 넣어 꺼낸다", "체력 6 소모 · 유물 하나 선택", RoomEffect.RelicReward, 0, 6),
                new RoomChoice("박자만 기억한다", "공명 +2", RoomEffect.Resonance, 2))
        };
        public static int EventCount => events.Length;
        public static RoomStory Event(int index) => events[index];
    }
}
