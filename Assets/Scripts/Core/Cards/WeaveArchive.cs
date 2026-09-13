using System;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Cards
{
    public static class WeaveArchive
    {
        // Authored per variant: silhouette change, internal coverage, and composition leverage.
        // Rules29 uses these tiers for new cards; historical catalogs remain frozen.
        private static readonly string[] diagramTiers =
        {
            "1112", // Petals: construction materials, densest variant purple.
            "1122", // Wreath.
            "1223", // Fracture.
            "1223", // Reverse echo.
            "1223", // Cross shear.
            "1223", // Butterfly.
            "1122", // Satellite.
            "2223", // Squared echo.
            "1122", // Axis waves.
            "2223", // Ribbon: gold reserved for seven strings.
            "1223", // Lace.
            "2223", // Knot.
            "1223", // Fan.
            "1122", // Inward bloom.
            "2223", // Prism.
            "2333", // Comet ensemble.
        };

        public static CardRarity DiagramGrade(FragmentKind kind)
        {
            if (AdvancedWeaves.IsAdvanced(kind))
            {
                int index = (int)kind - (int)FragmentKind.Weave0_0;
                return (CardRarity)(diagramTiers[index / 4][index % 4] - '0');
            }
            switch (kind)
            {
                case FragmentKind.HomeAnchor: case FragmentKind.WestAnchor: case FragmentKind.NorthAnchor:
                case FragmentKind.EastAnchor: case FragmentKind.SouthAnchor: case FragmentKind.TranslateRight:
                case FragmentKind.TranslateDown: case FragmentKind.Contract: case FragmentKind.Expand:
                case FragmentKind.Surge: case FragmentKind.Mirror: case FragmentKind.QuarterTurn:
                case FragmentKind.Ellipse: case FragmentKind.VerticalWeave: case FragmentKind.Shear:
                case FragmentKind.DiagonalWeave: case FragmentKind.Limacon: case FragmentKind.TwinEcho:
                    return CardRarity.Common;
                case FragmentKind.Petal: case FragmentKind.Orbit: case FragmentKind.PhaseOffset:
                    return CardRarity.Uncommon;
                case FragmentKind.Counterpoint: case FragmentKind.Square: case FragmentKind.StarPetal:
                case FragmentKind.Lissajous: case FragmentKind.Epitrochoid: case FragmentKind.DoubleLoop:
                    return CardRarity.Rare;
                case FragmentKind.CometBurst: case FragmentKind.ShardFracture: case FragmentKind.KaleidoscopeFold:
                case FragmentKind.NebulaRibbon: case FragmentKind.ComplexCube: case FragmentKind.Overtone:
                    return CardRarity.Legendary;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // Tier measures intervention in the diagram and its tactical use, not a damage ladder.
        public static CardRarity Grade(CardDefinition card)
        {
            switch(card.Fragment)
            {
                case FragmentKind.HomeAnchor: case FragmentKind.WestAnchor: case FragmentKind.NorthAnchor:
                case FragmentKind.EastAnchor: case FragmentKind.SouthAnchor: case FragmentKind.TranslateRight:
                case FragmentKind.TranslateDown: case FragmentKind.Contract: case FragmentKind.Expand: return CardRarity.Common;
                case FragmentKind.Ellipse: case FragmentKind.VerticalWeave: case FragmentKind.Shear:
                case FragmentKind.DiagonalWeave: case FragmentKind.QuarterTurn: case FragmentKind.Surge:
                case FragmentKind.Limacon: case FragmentKind.Petal: return CardRarity.Uncommon;
                case FragmentKind.CometBurst: case FragmentKind.ShardFracture: case FragmentKind.KaleidoscopeFold:
                case FragmentKind.NebulaRibbon: case FragmentKind.ComplexCube: return CardRarity.Legendary;
                default: return CardRarity.Rare;
            }
        }
        public static CardDefinition[] Build(CardDefinition[] previous)
        {
            var cards=new CardDefinition[previous.Length+AdvancedWeaves.Count];
            for(int i=0;i<previous.Length;i++)
            {
                var old=previous[i]; var abilities=new CardAbility[old.AbilityCount];
                for(int a=0;a<abilities.Length;a++) abilities[a]=old.GetAbility(a);
                cards[i]=CardDefinition.CreateWeaveFragment(old.Id,old.DisplayName,old.Fragment,Grade(old),old.Description,old.DrawBonus,abilities);
            }
            for(int i=0;i<AdvancedWeaves.Count;i++)
            {
                var kind=(FragmentKind)((int)FragmentKind.Weave0_0+i); var profile=AdvancedWeaves.Get(kind);
                int role=(i/4+i%4)%8;
                CardAbility enemy,player;
                switch(role)
                {
                    case 0: enemy=Enemy(CardAbilityKind.Burn,2);player=Self(CardAbilityKind.Shield,4);break;
                    case 1: enemy=Enemy(CardAbilityKind.Rupture,3);player=Self(CardAbilityKind.Momentum,2,2);break;
                    case 2: enemy=Enemy(CardAbilityKind.Weaken,2);player=Self(CardAbilityKind.Heal,2);break;
                    case 3: enemy=Enemy(CardAbilityKind.Exposure,2);player=Self(CardAbilityKind.Focus,2,2);break;
                    case 4: enemy=Enemy(CardAbilityKind.Anchor,1,1);player=Self(CardAbilityKind.Fortify,3,2);break;
                    case 5: enemy=Enemy(CardAbilityKind.Burn,3);player=Self(CardAbilityKind.Thorns,2,2);break;
                    case 6: enemy=Enemy(CardAbilityKind.Rupture,2);player=Self(CardAbilityKind.Regeneration,2,2);break;
                    default: enemy=Enemy(CardAbilityKind.Cleanse,1);player=Self(CardAbilityKind.Cleanse,1);break;
                }
                var grade=role==7?CardRarity.Legendary:(CardRarity)profile.Tier;
                cards[previous.Length+i]=CardDefinition.CreateWeaveFragment("frag.weave."+(i/4)+"."+(i%4),profile.Name,kind,grade,profile.Description,0,enemy,player);
            }
            return cards;
        }
        private static CardAbility Enemy(CardAbilityKind k,int n,int duration=2)=>new CardAbility(k,AbilityTarget.Enemy,n,duration);
        private static CardAbility Self(CardAbilityKind k,int n,int duration=1)=>new CardAbility(k,AbilityTarget.Player,n,duration);
    }
}
