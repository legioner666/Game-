using NUnit.Framework;

namespace ParasiteEvolution.Tests
{
    public sealed class EconomyTests
    {
        [Test]
        public void FullCargoRejectsCaptureWithoutGrantingExperience()
        {
            var p=ProfileData.Fresh();
            Assert.That(Economy.Capture(p,6,18,18,false),Is.True);
            Assert.That(Economy.Capture(p,1,3,3,false),Is.False);
            Assert.That(p.biomass,Is.EqualTo(18));Assert.That(p.totalCaptures,Is.EqualTo(1));
            Assert.That(p.cargoValue,Is.EqualTo(18));p.Validate();
        }
        [Test]
        public void DepositCannotCreditTheSameCargoTwice()
        {
            var p=ProfileData.Fresh();Economy.Capture(p,2,9,6,false);
            Assert.That(Economy.Deposit(p),Is.True);Assert.That(Economy.Deposit(p),Is.False);
            Assert.That(p.bank,Is.EqualTo(9));Assert.That(p.cargoUnits,Is.Zero);Assert.That(p.cargoValue,Is.Zero);
        }
        [Test]
        public void UnlockIsPermanentAndInsufficientFundsDoNotDebit()
        {
            var p=ProfileData.Fresh();p.bank=34;
            Assert.That(Economy.Unlock(p,"AB-MOVE"),Is.False);Assert.That(p.bank,Is.EqualTo(34));
            p.bank=100;Assert.That(Economy.Unlock(p,"AB-MOVE"),Is.True);
            Assert.That(Economy.Unlock(p,"AB-MOVE"),Is.False);Assert.That(p.bank,Is.EqualTo(65));
            Assert.That(p.abilities,Does.Contain("AB-MOVE"));
        }
        [TestCase(0,0)]
        [TestCase(7,7)]
        [TestCase(97,88)]
        [TestCase(100,90)]
        public void DeathRoundsDownAndCannotSettleTheSameSerialAgain(int wallet,int expected)
        {
            var p=ProfileData.Fresh();p.bank=wallet;Economy.Capture(p,1,3,3,false);
            Assert.That(Economy.SettleDeath(p,1,1000),Is.True);
            Assert.That(Economy.SettleDeath(p,1,1000),Is.False);
            Assert.That(p.bank,Is.EqualTo(expected));Assert.That(p.cargoUnits,Is.EqualTo(1));
            Assert.That(p.biomass,Is.EqualTo(3));
        }
        [Test]
        public void DeathRejectsSkippedSerialAndInvalidPenalty()
        {
            var p=ProfileData.Fresh();p.bank=100;
            Assert.That(Economy.SettleDeath(p,2,1000),Is.False);
            Assert.That(Economy.SettleDeath(p,1,10001),Is.False);
            Assert.That(p.bank,Is.EqualTo(100));Assert.That(p.deathSerial,Is.Zero);
        }
        [Test]
        public void ResourceOverflowRetainsCargo()
        {
            var p=ProfileData.Fresh();p.bank=Balance.ResourceLimit-1;
            Economy.Capture(p,1,3,3,false);
            Assert.That(Economy.Deposit(p),Is.False);Assert.That(p.cargoUnits,Is.EqualTo(1));
            Assert.That(p.bank,Is.EqualTo(Balance.ResourceLimit-1));
        }
        [Test]
        public void ChannelsNeedLevelTwoAndHaveAnEffectiveCap()
        {
            var p=ProfileData.Fresh();p.bank=1000;
            Assert.That(Economy.Upgrade(p,UpgradeKind.Channels),Is.False);
            p.biomass=30;
            Assert.That(Economy.Upgrade(p,UpgradeKind.Channels),Is.True);
            Assert.That(Economy.Upgrade(p,UpgradeKind.Channels),Is.True);
            var bank=p.bank;Assert.That(Economy.Upgrade(p,UpgradeKind.Channels),Is.False);
            Assert.That(Balance.Channels(p),Is.EqualTo(3));Assert.That(p.bank,Is.EqualTo(bank));
        }
        [Test]
        public void ClaimedQuestAdvancesAndCannotRepeatItsReward()
        {
            var p=ProfileData.Fresh();p.totalCaptures=4;
            Assert.That(Economy.ClaimQuest(p),Is.True);Assert.That(Economy.ClaimQuest(p),Is.False);
            Assert.That(p.questStage,Is.EqualTo(1));Assert.That(p.bank,Is.EqualTo(12));
        }
        [TestCase(0,1,0)]
        [TestCase(29,1,29)]
        [TestCase(30,2,0)]
        [TestCase(89,2,59)]
        [TestCase(90,3,0)]
        public void ExperienceCrossesLevelBoundaries(int experience,int level,int progress)
        {
            var p=ProfileData.Fresh();p.biomass=experience;
            Assert.That(Balance.Level(p),Is.EqualTo(level));Assert.That(Balance.LevelProgress(p),Is.EqualTo(progress));
        }
    }
}
