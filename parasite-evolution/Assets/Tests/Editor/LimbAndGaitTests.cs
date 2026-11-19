using NUnit.Framework;

namespace ParasiteEvolution.Tests
{
    // Pure gameplay-core tests: no UnityEngine types, so they also run under plain .NET
    // (Tools/GameplayCoreTests) as well as in the Unity Test Runner.
    public sealed class LimbAndGaitTests
    {
        const float Frame = 1f / 60f;
        // Seven support limbs: never more than four in the air at once.
        const int HalfOfSeven = 4;

        [Test]
        public void LimbExtendsAnchorsContractsAndFullyRetracts()
        {
            var limb = new LimbCycle();
            limb.Reach();
            Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Extending));
            limb.Tick(Frame);
            Assert.That(limb.NeedsAnchor, Is.False, "anchor request is consumed by the first tick");
            Run(limb, 1f);
            Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Anchored));
            Assert.That(limb.Extension, Is.EqualTo(1f));
            limb.Contract();
            Run(limb, .5f);
            Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Contracting));
            Assert.That(limb.Pull, Is.EqualTo(1f));
            limb.Release();
            Run(limb, 1f);
            Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Retracted));
            Assert.That(limb.Extension, Is.Zero);
            Assert.That(limb.Pull, Is.Zero);
        }

        [Test]
        public void StepLiftsBeforeReachingSoTheTipNeverJumps()
        {
            var limb = new LimbCycle();
            limb.Reach(); Run(limb, 1f);
            limb.Reach();
            Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Retracting));
            var previous = limb.Extension;
            var requested = 0;
            for (var i = 0; i < 120; i++)
            {
                limb.Tick(Frame);
                if (limb.NeedsAnchor) requested++;
                var step = System.Math.Abs(limb.Extension - previous);
                Assert.That(step, Is.LessThanOrEqualTo(System.Math.Max(limb.ExtendSpeed, limb.RetractSpeed) * Frame + 1e-5f));
                Assert.That(limb.Extension, Is.InRange(limb.StepLift - 1e-5f, 1f), "a stepping limb only lifts to StepLift");
                previous = limb.Extension;
            }
            Assert.That(requested, Is.EqualTo(1));
            Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Anchored));
        }

        [Test]
        public void ReleaseDuringStepCancelsTheReach()
        {
            var limb = new LimbCycle();
            limb.Reach(); Run(limb, 1f);
            limb.Reach(); limb.Tick(Frame);
            limb.Release();
            Run(limb, 1f);
            Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Retracted));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDeltaTimeIsIgnored(float dt)
        {
            var limb = new LimbCycle();
            limb.Reach(); limb.Tick(dt);
            Assert.That(limb.Extension, Is.Zero);
            Assert.That(float.IsNaN(limb.Pull), Is.False);
        }

        static GaitPlanner Planner() => new GaitPlanner(new[] { 0, 1, 1, 0, 0, 1, 1 });

        [Test]
        public void MovingGaitKeepsAtLeastHalfOfTheLimbsOutOfTheAir()
        {
            var gait = Planner();
            var strain = new float[7];
            var steps = new int[7];
            for (var frame = 0; frame < 600; frame++)
            {
                // Strain grows while planted, as it does when the body moves past a foothold.
                for (var i = 0; i < 7; i++) strain[i] = gait.Limbs[i].Planted ? strain[i] + Frame * (1.5f + i * .1f) : 0;
                var before = new LimbPhase[7];
                for (var i = 0; i < 7; i++) before[i] = gait.Limbs[i].Phase;
                gait.Tick(Frame, 1f, strain);
                for (var i = 0; i < 7; i++)
                {
                    if (before[i] != LimbPhase.Retracting && gait.Limbs[i].Phase == LimbPhase.Retracting) steps[i]++;
                    Assert.That(gait.Limbs[i].Extension, Is.InRange(0f, 1f));
                }
                if (frame > 60) Assert.That(gait.InFlight, Is.LessThanOrEqualTo(HalfOfSeven), "frame " + frame);
            }
            for (var i = 0; i < 7; i++) Assert.That(steps[i], Is.GreaterThan(3), "limb " + i + " keeps stepping");
        }

        [Test]
        public void ForcedStepsNeverLiftMoreThanHalfAtOnce()
        {
            var gait = Planner();
            var strain = new float[7];
            for (var frame = 0; frame < 120; frame++) gait.Tick(Frame, 1f, strain);
            foreach (var limb in gait.Limbs) Assert.That(limb.Planted, Is.True, "warm-up plants every limb");
            // Worst case: the body lurched and every foothold is far behind at the same moment.
            for (var i = 0; i < 7; i++) strain[i] = 5;
            var peak = 0;
            for (var frame = 0; frame < 90; frame++)
            {
                gait.Tick(Frame, 1f, strain);
                peak = System.Math.Max(peak, gait.InFlight);
                Assert.That(gait.InFlight, Is.LessThanOrEqualTo(HalfOfSeven), "frame " + frame);
            }
            Assert.That(gait.MaxInFlight, Is.EqualTo(HalfOfSeven));
            Assert.That(peak, Is.EqualTo(HalfOfSeven), "the cap is actually reached under load");
        }

        [Test]
        public void IdleOrganismWithdrawsEveryLimb()
        {
            var gait = Planner();
            var strain = new float[7];
            for (var frame = 0; frame < 120; frame++) gait.Tick(Frame, 1f, strain);
            for (var frame = 0; frame < 180; frame++) gait.Tick(Frame, 0f, strain);
            foreach (var limb in gait.Limbs)
            {
                Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Retracted));
                Assert.That(limb.Extension, Is.Zero);
            }
        }

        [Test]
        public void ShortPauseDoesNotRetractLimbs()
        {
            var gait = Planner();
            var strain = new float[7];
            for (var frame = 0; frame < 120; frame++) gait.Tick(Frame, 1f, strain);
            var planted = 0;
            foreach (var limb in gait.Limbs) if (limb.Planted) planted++;
            for (var frame = 0; frame < 20; frame++) gait.Tick(Frame, 0f, strain);
            var stillPlanted = 0;
            foreach (var limb in gait.Limbs) if (limb.Planted) stillPlanted++;
            Assert.That(planted, Is.GreaterThan(0));
            Assert.That(stillPlanted, Is.EqualTo(planted));
        }

        [Test]
        public void SuspendedGaitDoesNotStartSteps()
        {
            var gait = Planner();
            gait.Suspended = true;
            var strain = new float[] { 9, 9, 9, 9, 9, 9, 9 };
            for (var frame = 0; frame < 60; frame++) gait.Tick(Frame, 1f, strain);
            foreach (var limb in gait.Limbs) Assert.That(limb.Phase, Is.EqualTo(LimbPhase.Retracted));
        }

        [Test]
        public void NaNStrainAndSpeedDoNotPoisonTheGait()
        {
            var gait = Planner();
            var strain = new float[] { float.NaN, 1, float.NaN, 1, 1, float.NaN, 1 };
            for (var frame = 0; frame < 120; frame++) gait.Tick(Frame, frame % 2 == 0 ? float.NaN : 1f, strain);
            foreach (var limb in gait.Limbs) Assert.That(float.IsNaN(limb.Extension), Is.False);
        }

        [Test]
        public void GaitRejectsInvalidConfiguration()
        {
            Assert.Throws<System.ArgumentException>(() => new GaitPlanner(new int[0]));
            Assert.Throws<System.ArgumentException>(() => new GaitPlanner(new[] { 0, 2 }));
            Assert.Throws<System.ArgumentException>(() => Planner().Tick(Frame, 1f, new float[3]));
        }

        static void Run(LimbCycle limb, float seconds)
        {
            for (var t = 0f; t < seconds; t += Frame) limb.Tick(Frame);
        }
    }
}
