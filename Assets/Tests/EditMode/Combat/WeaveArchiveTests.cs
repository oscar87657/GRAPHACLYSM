using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class WeaveArchiveTests
    {
        [Test] public void DiagramReclassificationCoversEveryCardWithoutRepricingOrChangingPower()
        {
            var counts = new int[4];
            foreach (var card in FragmentCardCatalog.Version28)
            {
                counts[(int)card.DiagramRarity]++;
                Assert.That(card.DiagramRarity, Is.EqualTo(WeaveArchive.DiagramGrade(card.Fragment)), card.Id);
                var old = FragmentCardCatalog.Find(card.Id);
                Assert.That(old.DiagramRarity, Is.EqualTo(card.DiagramRarity), "Old saves show current colors: " + card.Id);
                if (!AdvancedWeaves.IsAdvanced(card.Fragment))
                    Assert.That(card.Rarity, Is.EqualTo(WeaveArchive.Grade(card)), "Preserve v28 economy: " + card.Id);
                else
                {
                    int index = (int)card.Fragment - (int)FragmentKind.Weave0_0;
                    var profile = AdvancedWeaves.Get(card.Fragment);
                    var expected = (index / 4 + index % 4) % 8 == 7 ? CardRarity.Legendary : (CardRarity)profile.Tier;
                    Assert.That(card.Rarity, Is.EqualTo(expected), "Preserve v28 economy: " + card.Id);
                }
            }
            foreach (int count in counts) Assert.That(count, Is.GreaterThan(0));
            TestContext.WriteLine("Diagram colors: " + string.Join(",", counts));
        }

        [TestCase("frag.weave.9.3", CardRarity.Legendary)]
        [TestCase("frag.weave.9.2", CardRarity.Rare)]
        [TestCase("frag.weave.9.0", CardRarity.Rare)]
        [TestCase("frag.weave.0.3", CardRarity.Rare)]
        [TestCase("frag.weave.0.0", CardRarity.Uncommon)]
        [TestCase("frag.weave.7.0", CardRarity.Rare)]
        [TestCase("frag.mirror", CardRarity.Common)]
        [TestCase("frag.surge", CardRarity.Common)]
        [TestCase("frag.overtone", CardRarity.Legendary)]
        [TestCase("frag.purge", CardRarity.Legendary)]
        [TestCase("frag.momentum", CardRarity.Legendary)]
        public void ColorMeasuresDiagramImpactNotFamilyOrCleanse(string id, CardRarity expected)
        {
            Assert.That(FragmentCardCatalog.Current(id).DiagramRarity, Is.EqualTo(expected));
        }

        [Test] public void CatalogHas105UniqueCardsFourGradesAndRequestedGoldBenchmarks()
        {
            var ids=new HashSet<string>();var counts=new int[4];int shapes=0;
            foreach(var card in FragmentCardCatalog.All)
            {
                Assert.That(ids.Add(card.Id),Is.True,card.Id);counts[(int)card.Rarity]++;
                Assert.That(card.AbilityCount+card.DrawBonus,Is.GreaterThan(0));
                if(AdvancedWeaves.IsAdvanced(card.Fragment)) shapes++;
            }
            Assert.That(ids.Count,Is.EqualTo(105)); Assert.That(shapes,Is.EqualTo(64));
            foreach(int count in counts) Assert.That(count,Is.GreaterThan(0));
            Assert.That(FragmentCardCatalog.Current("frag.purge").Rarity,Is.EqualTo(CardRarity.Legendary));
            Assert.That(FragmentCardCatalog.Current("frag.momentum").Rarity,Is.EqualTo(CardRarity.Legendary));
            Assert.That(FragmentCardCatalog.Current("frag.home").Rarity,Is.EqualTo(CardRarity.Common));
            Assert.That(FragmentCardCatalog.Version27.Count,Is.EqualTo(41));
            Assert.That(FragmentCardCatalog.Find("frag.momentum").Rarity,Is.EqualTo(CardRarity.Common),"Prior price/replay must remain unchanged");
        }
        [Test] public void All64NewOperationsChangePreviousGeometryWithoutMovingOriginAndUndoExactly()
        {
            var signatures=new HashSet<string>();
            foreach(var card in FragmentCardCatalog.All)
            {
                if(!AdvancedWeaves.IsAdvanced(card.Fragment)) continue;
                var f=new FragmentEquation();f.TryAppend(FragmentKind.Limacon,4,-1);f.TryAppend(FragmentKind.Shear);
                f.Sample(.137,out double beforeX,out double beforeY);
                Assert.That(f.TryAppend(card.Fragment),Is.True,card.Id);
                Assert.That(f.OriginX,Is.EqualTo(4));Assert.That(f.OriginY,Is.EqualTo(-1));
                var key=new StringBuilder();
                for(int i=0;i<128;i++)
                { f.Sample(i/128.0,out double x,out double y); Assert.That(double.IsNaN(x)||double.IsInfinity(x)||double.IsNaN(y)||double.IsInfinity(y),Is.False);key.Append(Math.Round(x,5)).Append('/').Append(Math.Round(y,5)).Append(';'); }
                Assert.That(signatures.Add(key.ToString()),Is.True,"Duplicate actual composite curve: "+card.Id);
                Assert.That(f.TryUndo(),Is.True);f.Sample(.137,out double ux,out double uy);
                Assert.That(ux,Is.EqualTo(beforeX));Assert.That(uy,Is.EqualTo(beforeY));
            }
        }
        [TestCase(FragmentKind.Weave0_0)] [TestCase(FragmentKind.Weave3_2)]
        [TestCase(FragmentKind.Weave7_1)] [TestCase(FragmentKind.Weave9_3)] [TestCase(FragmentKind.Weave14_2)]
        public void FamiliesMatchIndependentMathematicalComposition(FragmentKind kind)
        {
            var before=new FragmentEquation();before.TryAppend(FragmentKind.Limacon,5,0);
            var after=new FragmentEquation();after.TryAppend(FragmentKind.Limacon,5,0);after.TryAppend(kind);
            var p=AdvancedWeaves.Get(kind); const double u=.125;
            before.Sample(u,out double a,out double b);a-=5;
            before.Sample(-p.P*u,out double rx,out double ry);rx-=5;
            before.Sample(p.P*u,out double px,out double py);px-=5;
            before.Sample(p.Q*u,out double qx,out double qy);qx-=5;
            double c=Math.Cos(u*Math.PI*2),s=Math.Sin(u*Math.PI*2),x=0,y=0;
            switch(p.Family)
            {
                case 0: double r=.8+.45*Math.Cos(p.P*u*Math.PI*2);x=r*a;y=r*b;break;
                case 3: x=a+.45*(c*rx-s*ry);y=b+.45*(s*rx+c*ry);break;
                case 7: x=(a*a-b*b)/3+.5*rx;y=2*a*b/3+.5*ry;break;
                case 9: x=px;y=qy;break;
                case 14: x=c*(.75*px+.35*qx)-s*(.75*py-.35*qy);y=s*(.75*px+.35*qx)+c*(.75*py-.35*qy);break;
            }
            after.Sample(u,out double ax,out double ay);Assert.That(ax,Is.EqualTo(x+5).Within(1e-9));Assert.That(ay,Is.EqualTo(y).Within(1e-9));
        }
        [Test] public void EveryNewCardCanStartAndItsActualDamageMatchesTheSharedPreview()
        {
            foreach(var card in FragmentCardCatalog.All)
            {
                if(!AdvancedWeaves.IsAdvanced(card.Fragment)) continue;
                var f=new FragmentEquation();Assert.That(f.TryAppend(card.Fragment,4,-2),Is.True);
                double x=0,y=0;bool point=false;
                for(int i=0;i<128;i++) { f.Sample(i/128.0,out x,out y);if(x>.6&&x<9.4&&y>-3.4&&y<3.4) { point=true;break; } }
                Assert.That(point,Is.True,card.Id);
                var battle=new BattleSession(new BattleDefinition(100,1,new[]{new EnemyDefinition("target","Target",x,y,999,0)},CombatArchetype.Ian,fragments:true));
                Assert.That(battle.TryPlayCard(card,out _),Is.True,card.Id);
                int expected=battle.PreviewDamage(battle.Enemies[0]); Assert.That(expected,Is.GreaterThan(0),card.Id);
                Assert.That(battle.TryBeginPlot(),Is.True);var report=battle.ResolvePlot();
                Assert.That(report.HitCount,Is.EqualTo(1),card.Id);Assert.That(999-battle.Enemies[0].Health,Is.EqualTo(expected),card.Id);
            }
        }
        [Test] public void HighFrequencyAndCapacityRejectWithoutChangingTheDiagram()
        {
            foreach(var card in FragmentCardCatalog.All)
            {
                if(!AdvancedWeaves.IsAdvanced(card.Fragment)) continue;
                var f=new FragmentEquation();int successful=0;
                while(f.TryAppend(card.Fragment)) { successful++;Assert.That(successful,Is.LessThanOrEqualTo(8));Assert.That(f.Frequency,Is.LessThanOrEqualTo(96)); }
                f.Sample(.25,out double x,out double y);Assert.That(f.TryAppend(card.Fragment),Is.False);f.Sample(.25,out double nx,out double ny);
                Assert.That(nx,Is.EqualTo(x));Assert.That(ny,Is.EqualTo(y));Assert.That(f.Count,Is.EqualTo(successful));
            }
        }
        [Test] public void Version27Keeps41CardPoolAndGrandArchiveReplaysWithoutChangingOldCommands()
        {
            var run=PrototypeRunFactory.Create(302,PrototypeCharacterCatalog.All[1]);run.TryChooseApproach(CombatApproach.Tuning);run.TryEnableOpeningRoute();run.TryEnableEconomy();run.TryEnableContentExpansion();
            Assert.That(run.HasGrandArchive,Is.False);var old=run.CaptureSave();
            Assert.That(RunGameSession.TryRestore(old,out var restored,out _),Is.True);Assert.That(restored.HasGrandArchive,Is.False);
            Assert.That(run.TryEnableGrandArchive(),Is.True); Assert.That(run.TryEnableGrandArchive(),Is.False);
            Assert.That(RunGameSession.TryRestore(run.CaptureSave(),out restored,out _),Is.True);Assert.That(restored.HasGrandArchive,Is.True);
            var bytes=RunSaveStore.Encode(run.CaptureSave());Array.Copy(BitConverter.GetBytes(27),0,bytes,8,4);
            using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out _),Is.False);
            restored.TrySelectMapNode(0);Assert.That(restored.TryEnableGrandArchive(),Is.False);
        }
    }
}
