using NUnit.Framework;

namespace ParasiteEvolution.Tests
{
    public sealed class MutationTests
    {
        static ProfileData AtLevel(int level, int bank)
        {
            var p = ProfileData.Fresh();
            for (var l = 1; l < level; l++) p.biomass += l * 30;
            p.bank = bank;
            return p;
        }

        [Test]
        public void MutationNeedsLevelAndChargesExactlyOnce()
        {
            var p = AtLevel(2, 1000);
            Assert.That(Economy.Mutate(p, MutationKind.ChitinSpikes), Is.False);
            Assert.That(p.bank, Is.EqualTo(1000));
            p = AtLevel(3, 1000);
            Assert.That(Balance.Level(p), Is.EqualTo(3));
            Assert.That(Economy.Mutate(p, MutationKind.ChitinSpikes), Is.True);
            Assert.That(Economy.Mutate(p, MutationKind.ChitinSpikes), Is.False);
            Assert.That(p.bank, Is.EqualTo(1000 - Balance.MutationPrices[0]));
            Assert.That(Balance.Mutations(p), Is.EqualTo(1));
            p.Validate();
        }

        [Test]
        public void InsufficientGenomeAndUnknownKindsAreRejected()
        {
            var p = AtLevel(8, Balance.MutationPrices[(int)MutationKind.ElasticStomach] - 1);
            Assert.That(Economy.Mutate(p, MutationKind.ElasticStomach), Is.False);
            Assert.That(Economy.Mutate(p, (MutationKind)99), Is.False);
            Assert.That(Economy.Mutate(p, (MutationKind)(-1)), Is.False);
            Assert.That(p.mutationMask, Is.Zero);
        }

        [Test]
        public void EffectsApplyOnlyWithTheMutation()
        {
            var p = AtLevel(8, 100000);
            var capacity = Balance.Capacity(p);
            var radius = Balance.Radius(p);
            var capture = Balance.CaptureDamage(p);
            Assert.That(Balance.StrikeDamage(p), Is.EqualTo(38f));
            Assert.That(Balance.DamageTaken(p, 20), Is.EqualTo(20f));
            Assert.That(Balance.Cooldown(p, "AB-MOVE"), Is.EqualTo(Balance.MoveCooldown));
            for (var i = 0; i < Balance.MutationCount; i++) Assert.That(Economy.Mutate(p, (MutationKind)i), Is.True);
            Assert.That(Balance.Capacity(p), Is.EqualTo(capacity + 4));
            Assert.That(Balance.Radius(p), Is.EqualTo(radius + .8f).Within(1e-5f));
            Assert.That(Balance.CaptureDamage(p), Is.EqualTo(capture * 1.25f).Within(1e-4f));
            Assert.That(Balance.StrikeDamage(p), Is.EqualTo(57f));
            Assert.That(Balance.DamageTaken(p, 20), Is.EqualTo(15f));
            Assert.That(Balance.DamageTaken(p, -5), Is.Zero);
            Assert.That(Balance.Cooldown(p, "AB-MOVE"), Is.EqualTo(Balance.MoveCooldown * .8f).Within(1e-5f));
            Assert.That(Balance.DashDistance(p), Is.GreaterThan(9f));
            p.Validate();
        }

        [Test]
        public void SaveWithUnknownMutationBitsIsRejected()
        {
            var p = ProfileData.Fresh();
            p.mutationMask = 1 << Balance.MutationCount;
            Assert.Throws<System.InvalidOperationException>(() => p.Validate());
        }

        [Test]
        public void OlderSaveWithoutMutationFieldStillValidates()
        {
            // JsonUtility leaves a missing int field at 0; a schema-1 save from before this change is valid.
            var p = ProfileData.Fresh();
            p.mutationMask = 0;
            p.questStage = 5;
            p.Validate();
            Assert.That(Balance.QuestProgress(p), Is.Zero);
        }

        [Test]
        public void NewQuestStagesProgressAndPay()
        {
            var p = AtLevel(3, 200);
            p.questStage = 5;
            Assert.That(Economy.ClaimQuest(p), Is.False);
            Assert.That(Economy.Mutate(p, MutationKind.AcidGlands), Is.True);
            var bank = p.bank;
            Assert.That(Economy.ClaimQuest(p), Is.True);
            Assert.That(p.bank, Is.EqualTo(bank + 80));
            p.totalDeposits = 10;
            Assert.That(Economy.ClaimQuest(p), Is.True);
            Assert.That(p.questStage, Is.EqualTo(Balance.QuestRewards.Length));
            Assert.That(Economy.ClaimQuest(p), Is.False);
            p.Validate();
        }
    }
}
