using System;
using System.Collections.Generic;
using Graphaclysm.Core.Equations;
namespace Graphaclysm.Core.Cards
{
    public static class FragmentCardCatalog
    {
        private static CardAbility Self(CardAbilityKind k,int n,int d=1) => new CardAbility(k,AbilityTarget.Player,n,d);
        private static CardAbility Enemy(CardAbilityKind k,int n,int d=2) => new CardAbility(k,AbilityTarget.Enemy,n,d);
        private static readonly CardDefinition[] cards = {
            CardDefinition.CreateWeaveFragment("frag.echo","잔향",FragmentKind.Counterpoint,CardRarity.Common,"역회전 겹치기",0,Enemy(CardAbilityKind.Burn,2),Self(CardAbilityKind.Focus,1,2)),
            CardDefinition.CreateWeaveFragment("frag.orbit","윤무",FragmentKind.Orbit,CardRarity.Common,"전체를 회전",0,Enemy(CardAbilityKind.Anchor,1,1),Self(CardAbilityKind.Shield,3)),
            CardDefinition.CreateWeaveFragment("frag.petal","개화",FragmentKind.Petal,CardRarity.Uncommon,"반경에 물결",0,Enemy(CardAbilityKind.Exposure,2),Self(CardAbilityKind.Heal,3)),
            CardDefinition.CreateWeaveFragment("frag.expand","파열",FragmentKind.Expand,CardRarity.Common,"바깥으로 펼치기",0,Enemy(CardAbilityKind.Exposure,2),Self(CardAbilityKind.Shield,3)),
            CardDefinition.CreateWeaveFragment("frag.contract","고요",FragmentKind.Contract,CardRarity.Common,"안으로 모으기",1,Self(CardAbilityKind.Regeneration,2,2)),
            CardDefinition.CreateWeaveFragment("frag.mirror","거울밤",FragmentKind.Mirror,CardRarity.Rare,"위아래 뒤집기",0,Enemy(CardAbilityKind.Cleanse,1),Self(CardAbilityKind.Cleanse,1)),
            CardDefinition.CreateWeaveFragment("frag.right","여백",FragmentKind.TranslateRight,CardRarity.Common,"오른쪽으로 옮기기",1,Self(CardAbilityKind.Shield,3)),
            CardDefinition.CreateWeaveFragment("frag.down","낙화",FragmentKind.TranslateDown,CardRarity.Common,"아래로 옮기기",0,Enemy(CardAbilityKind.Burn,2),Self(CardAbilityKind.Heal,3)),
            CardDefinition.CreateWeaveFragment("frag.square","유리정원",FragmentKind.Square,CardRarity.Rare,"궤적을 제곱",0,Enemy(CardAbilityKind.Burn,3),Self(CardAbilityKind.Focus,2,2)),
            CardDefinition.CreateWeaveFragment("frag.overtone","성운",FragmentKind.Overtone,CardRarity.Legendary,"빠른 회전 겹치기",2,Self(CardAbilityKind.Heal,3)),
            CardDefinition.CreateWeaveFragment("frag.home","귀환점",FragmentKind.HomeAnchor,CardRarity.Common,"기준점을 내 위치로",0,Self(CardAbilityKind.Shield,2)),
            CardDefinition.CreateWeaveFragment("frag.west","서쪽의 문",FragmentKind.WestAnchor,CardRarity.Common,"기준점 왼쪽 2",0,Enemy(CardAbilityKind.Weaken,2)),
            CardDefinition.CreateWeaveFragment("frag.north","승천",FragmentKind.NorthAnchor,CardRarity.Uncommon,"기준점 위로 1.5",0,Self(CardAbilityKind.Focus,2,2)),
            CardDefinition.CreateWeaveFragment("frag.twin","속삭임",FragmentKind.TwinEcho,CardRarity.Common,"작은 역회전 두 겹",1,Enemy(CardAbilityKind.Burn,1)),
            CardDefinition.CreateWeaveFragment("frag.star","오엽성",FragmentKind.StarPetal,CardRarity.Rare,"다섯 갈래로 접기",0,Enemy(CardAbilityKind.Exposure,3),Self(CardAbilityKind.Heal,2)),
            CardDefinition.CreateWeaveFragment("frag.surge","격류",FragmentKind.Surge,CardRarity.Uncommon,"반경 1.7배",0,Enemy(CardAbilityKind.Burn,4),Self(CardAbilityKind.Shield,1)),
            CardDefinition.CreateWeaveFragment("frag.ellipse","긴 황혼",FragmentKind.Ellipse,CardRarity.Uncommon,"가로로 길게 누르기",0,Enemy(CardAbilityKind.Weaken,1),Self(CardAbilityKind.Shield,5)),
            CardDefinition.CreateWeaveFragment("frag.lissajous","쌍성직조",FragmentKind.Lissajous,CardRarity.Uncommon,"두 주기로 교차",0,Enemy(CardAbilityKind.Anchor,1,1),Self(CardAbilityKind.Focus,1,2)),
            CardDefinition.CreateWeaveFragment("frag.eclipse","월식",FragmentKind.Epitrochoid,CardRarity.Rare,"바깥 회전을 빼기",0,Enemy(CardAbilityKind.Burn,3),Self(CardAbilityKind.Shield,2)),
            CardDefinition.CreateWeaveFragment("frag.limacon","편월",FragmentKind.Limacon,CardRarity.Common,"한쪽 반경 부풀리기",0,Enemy(CardAbilityKind.Weaken,2),Self(CardAbilityKind.Heal,2)),
            CardDefinition.CreateWeaveFragment("frag.shear","비껴쓴 선",FragmentKind.Shear,CardRarity.Common,"높이에 따라 비틀기",0,Enemy(CardAbilityKind.Exposure,1),Self(CardAbilityKind.Shield,4)),
            CardDefinition.CreateWeaveFragment("frag.phase","엇박자",FragmentKind.PhaseOffset,CardRarity.Uncommon,"가로 박자 앞당기기",1,Self(CardAbilityKind.Cleanse,1)),
            CardDefinition.CreateWeaveFragment("frag.cube","삼중봉인",FragmentKind.ComplexCube,CardRarity.Rare,"궤적을 세제곱",0,Enemy(CardAbilityKind.Anchor,1,2),Self(CardAbilityKind.Focus,3,2)),
            CardDefinition.CreateWeaveFragment("frag.cinder","꺼지지 않는 재",FragmentKind.Counterpoint,CardRarity.Uncommon,"역회전 끝에 불씨를 묻기",0,Enemy(CardAbilityKind.Burn,2,3),Enemy(CardAbilityKind.Rupture,2,2)),
            CardDefinition.CreateWeaveFragment("frag.guardian","가시 성좌",FragmentKind.Orbit,CardRarity.Uncommon,"자신을 감싸는 가시 궤도",0,Self(CardAbilityKind.Thorns,3,2),Self(CardAbilityKind.Shield,4)),
            CardDefinition.CreateWeaveFragment("frag.momentum","유성 보폭",FragmentKind.CometBurst,CardRarity.Common,"궤적을 일곱 갈래 유성꼬리로 폭발",1,Self(CardAbilityKind.Momentum,3,2)),
            CardDefinition.CreateWeaveFragment("frag.bastion","별빛 보루",FragmentKind.KaleidoscopeFold,CardRarity.Uncommon,"궤적을 회전 거울로 접어 만화경 만들기",1,Self(CardAbilityKind.Fortify,5,2)),
            CardDefinition.CreateWeaveFragment("frag.shatter","파쇄의 정리",FragmentKind.ComplexCube,CardRarity.Rare,"균열을 세제곱해 깊게 새기기",0,Enemy(CardAbilityKind.Rupture,5,2)),
            CardDefinition.CreateWeaveFragment("frag.requiem","붉은 만가",FragmentKind.Epitrochoid,CardRarity.Rare,"불타는 바깥 회전을 되감기",0,Enemy(CardAbilityKind.Burn,4,3),Enemy(CardAbilityKind.Rupture,2,2)),
            CardDefinition.CreateWeaveFragment("frag.refraction","굴절 보법",FragmentKind.ShardFracture,CardRarity.Uncommon,"궤적을 여섯 조각처럼 벌려 꺾기",0,Enemy(CardAbilityKind.Exposure,2,2),Self(CardAbilityKind.Momentum,3,2)),
            CardDefinition.CreateWeaveFragment("frag.sanctuary","성운의 품",FragmentKind.NebulaRibbon,CardRarity.Rare,"가로 다섯 번·세로 두 번으로 성운 리본 짜기",0,Self(CardAbilityKind.Fortify,6,2),Self(CardAbilityKind.Heal,4)),
            CardDefinition.CreateWeaveFragment("frag.snare","붙잡는 초승",FragmentKind.Limacon,CardRarity.Common,"편월로 적을 위험 지대에 묶기",0,Enemy(CardAbilityKind.Anchor,1,2),Enemy(CardAbilityKind.Weaken,1,2)),
            CardDefinition.CreateWeaveFragment("frag.needle","균열 바늘",FragmentKind.Shear,CardRarity.Common,"기울인 선에 다음 타격 예약",0,Enemy(CardAbilityKind.Rupture,3,2),Self(CardAbilityKind.Momentum,2,2)),
            CardDefinition.CreateWeaveFragment("frag.harbor","달의 정박지",FragmentKind.Ellipse,CardRarity.Common,"넓은 황혼에 방어를 준비",0,Self(CardAbilityKind.Fortify,3,2),Enemy(CardAbilityKind.Weaken,1,2)),
            CardDefinition.CreateWeaveFragment("frag.purge","재를 걷는 바람",FragmentKind.ShardFracture,CardRarity.Uncommon,"파쇄 궤적으로 강화와 잔불을 정리",0,Enemy(CardAbilityKind.Cleanse,1),Self(CardAbilityKind.Cleanse,1))
        };
        public static IReadOnlyList<CardDefinition> Legacy => cards;
        private static readonly CardDefinition[] expanded = ExpandCatalog();
        public static IReadOnlyList<CardDefinition> Version27 => expanded;
        private static readonly CardDefinition[] archive = WeaveArchive.Build(expanded);
        public static IReadOnlyList<CardDefinition> Version28 => archive;
        private static readonly CardDefinition[] balanced = CardMarketBalance.Build(archive);
        public static IReadOnlyList<CardDefinition> Version29 => balanced;
        private static readonly CardDefinition[] statusCards = StatusCardRules.Build(balanced);
        public static IReadOnlyList<CardDefinition> All => statusCards;
        private static CardDefinition[] ExpandCatalog()
        {
            var additions = new[] {
                CardDefinition.CreateWeaveFragment("frag.spire","솟는 첨탑",FragmentKind.VerticalWeave,CardRarity.Common,"가로를 좁히고 세로를 1.4배",0,Enemy(CardAbilityKind.Rupture,2),Self(CardAbilityKind.Shield,4)),
                CardDefinition.CreateWeaveFragment("frag.quarter","직각의 춤",FragmentKind.QuarterTurn,CardRarity.Common,"기준점 둘레로 도안 전체를 90° 회전",1,Enemy(CardAbilityKind.Weaken,1)),
                CardDefinition.CreateWeaveFragment("frag.diagonal","오르는 사선",FragmentKind.DiagonalWeave,CardRarity.Uncommon,"오른쪽일수록 위로 기울이기",0,Enemy(CardAbilityKind.Exposure,2),Self(CardAbilityKind.Momentum,2,2)),
                CardDefinition.CreateWeaveFragment("frag.east","동쪽의 문",FragmentKind.EastAnchor,CardRarity.Common,"도안 그대로 기준점만 오른쪽 2",0,Enemy(CardAbilityKind.Anchor,1,1)),
                CardDefinition.CreateWeaveFragment("frag.south","별의 하강",FragmentKind.SouthAnchor,CardRarity.Common,"도안 그대로 기준점만 아래 1.5",0,Self(CardAbilityKind.Fortify,4,2)),
                CardDefinition.CreateWeaveFragment("frag.double_loop","두 개의 고리",FragmentKind.DoubleLoop,CardRarity.Rare,"가로는 유지하고 세로 궤적만 두 번 반복",0,Enemy(CardAbilityKind.Burn,3),Self(CardAbilityKind.Cleanse,1))
            };
            var result = new CardDefinition[cards.Length + additions.Length];
            Array.Copy(cards,result,cards.Length); Array.Copy(additions,0,result,cards.Length,additions.Length);
            return result;
        }
        // Utility fragments do not earn free damage simply for occupying a socket.
        public static int Power(FragmentKind kind)
        {
            if(AdvancedWeaves.IsAdvanced(kind)) return AdvancedWeaves.Get(kind).Tier>=3?3:2;
            switch(kind)
            {
                case FragmentKind.HomeAnchor: case FragmentKind.WestAnchor: case FragmentKind.NorthAnchor:
                case FragmentKind.Contract: case FragmentKind.TranslateRight: case FragmentKind.Mirror: case FragmentKind.PhaseOffset:
                case FragmentKind.QuarterTurn: case FragmentKind.EastAnchor: case FragmentKind.SouthAnchor: return 0;
                case FragmentKind.DoubleLoop: return 2;
                case FragmentKind.Surge: case FragmentKind.ComplexCube: return 3;
                case FragmentKind.CometBurst: case FragmentKind.KaleidoscopeFold: case FragmentKind.ShardFracture: return 2;
                case FragmentKind.NebulaRibbon: return 3;
                case FragmentKind.Counterpoint: case FragmentKind.Square: case FragmentKind.StarPetal: case FragmentKind.Lissajous: case FragmentKind.Epitrochoid: return 2;
                default: return 1;
            }
        }
        // Old starting decks/replays keep their exact definitions. New opt-in uses Current.
        public static CardDefinition Find(string id) { for(int i=0;i<expanded.Length;i++) if(expanded[i].Id==id) return expanded[i]; return Previous(id); }
        public static CardDefinition Previous(string id) { foreach(var card in archive) if(card.Id==id) return card; throw new ArgumentException("Unknown fragment.",nameof(id)); }
        public static CardDefinition MarketCard(string id) { foreach(var card in balanced) if(card.Id==id) return card; throw new ArgumentException("Unknown fragment.",nameof(id)); }
        public static CardDefinition Current(string id) { foreach(var card in statusCards) if(card.Id==id) return card; throw new ArgumentException("Unknown fragment.",nameof(id)); }
    }
}
