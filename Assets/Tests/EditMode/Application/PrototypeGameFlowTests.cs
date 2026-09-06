using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using NUnit.Framework;

namespace Graphaclysm.Tests.Application
{
    public sealed class PrototypeGameFlowTests
    {
        [Test]
        public void MenuToCharacterSelectionToRun_FollowsExplicitPhases()
        {
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 77u);

            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.MainMenu));
            flow.OpenCharacterSelection();
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.CharacterSelection));
            Assert.That(flow.TrySelectCharacter(1), Is.True);
            Assert.That(flow.TryStartRun(), Is.True);

            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.Run));
            Assert.That(flow.CurrentCharacter, Is.SameAs(PrototypeCharacterCatalog.All[1]));
            Assert.That(flow.CurrentRun.PlayerMaxHealth,
                Is.EqualTo(PrototypeCharacterCatalog.All[1].MaxHealth));
            Assert.That(flow.CurrentRun.Phase, Is.EqualTo(RunPhase.MapSelection));
        }

        [Test]
        public void RestartAndReturnToMenu_ReplaceOwnedRunSafely()
        {
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 88u);
            flow.OpenCharacterSelection();
            flow.TryStartRun();
            RunGameSession firstRun = flow.CurrentRun;

            Assert.That(flow.TryRestartRun(), Is.True);
            Assert.That(flow.CurrentRun, Is.Not.SameAs(firstRun));
            Assert.That(flow.CurrentCharacter, Is.Not.Null);

            flow.ReturnToMainMenu();
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.MainMenu));
            Assert.That(flow.CurrentRun, Is.Null);
            Assert.That(flow.CurrentCharacter, Is.Null);
        }
    }
}
