using NUnit.Framework;

namespace ParasiteEvolution.Tests
{
    public sealed class DashAndWardenTests
    {
        const float Frame = 1f / 60f;

        [TestCase(1f)]
        [TestCase(9f)]
        [TestCase(11.5f)]
        public void DashCoversExactlyTheRequestedDistanceWithoutOvershoot(float distance)
        {
            var dash = new DashMotion();
            dash.Begin(distance);
            var total = 0f;
            var frames = 0;
            var movedDuringReach = 0f;
            while (dash.Active && frames < 600)
            {
                var reaching = dash.Current == DashMotion.Stage.Reach;
                var step = dash.Tick(Frame);
                Assert.That(step, Is.GreaterThanOrEqualTo(0f));
                if (reaching && dash.Current == DashMotion.Stage.Reach) movedDuringReach += step;
                total += step;
                frames++;
            }
            Assert.That(dash.Active, Is.False);
            Assert.That(total, Is.EqualTo(distance).Within(1e-4f));
            Assert.That(movedDuringReach, Is.Zero, "limbs anchor before the body is hauled");
            Assert.That(frames * Frame, Is.LessThan(1.5f));
        }

        [Test]
        public void DashWorksWithLargeFrameSpikes()
        {
            var dash = new DashMotion();
            dash.Begin(9);
            var total = 0f;
            for (var i = 0; i < 20 && dash.Active; i++) total += dash.Tick(.25f);
            Assert.That(total, Is.EqualTo(9f).Within(1e-4f));
        }

        [Test]
        public void AbortedDashStopsImmediatelyAndRejectsBadInput()
        {
            var dash = new DashMotion();
            dash.Begin(9); dash.Tick(.2f); dash.Abort();
            Assert.That(dash.Active, Is.False);
            Assert.That(dash.Tick(Frame), Is.Zero);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => dash.Begin(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => dash.Begin(float.NaN));
        }

        static WardenSenses Near(float distance, bool safe = false, float health = 1, float home = 2) => new WardenSenses {
            PlayerDistance = distance, HomeDistance = home, HealthFraction = health, PlayerSafe = safe, LineOfSight = true };

        [Test]
        public void WardenNeverStartsAnAttackAgainstAPlayerInTheSafeZone()
        {
            var brain = new WardenBrain();
            for (var i = 0; i < 600; i++)
            {
                var e = brain.Tick(Frame, Near(2, safe: true));
                Assert.That(e, Is.Not.EqualTo(WardenEvent.SlamWarning));
                Assert.That(e, Is.Not.EqualTo(WardenEvent.ChargeWarning));
            }
        }

        [Test]
        public void EveryImpactIsPrecededByATelegraph()
        {
            var brain = new WardenBrain();
            float? warnedAt = null;
            var impacts = 0;
            var t = 0f;
            for (var i = 0; i < 1800; i++, t += Frame)
            {
                var e = brain.Tick(Frame, Near(i % 400 < 200 ? 2.5f : 8f, health: i > 900 ? .3f : 1f));
                if (e == WardenEvent.SlamWarning || e == WardenEvent.ChargeWarning) warnedAt = t;
                if (e == WardenEvent.SlamImpact || e == WardenEvent.ChargeStart)
                {
                    Assert.That(warnedAt.HasValue, Is.True);
                    Assert.That(t - warnedAt.Value, Is.GreaterThanOrEqualTo(WardenBrain.MinimumTelegraph - Frame));
                    warnedAt = null; impacts++;
                }
            }
            Assert.That(impacts, Is.GreaterThan(3));
            Assert.That(brain.Enraged, Is.True);
        }

        [Test]
        public void ChargeIntoWallOpensALongCaptureWindow()
        {
            var brain = new WardenBrain();
            var guard = 0;
            while (brain.State != WardenState.Charging && guard++ < 600) brain.Tick(Frame, Near(8));
            Assert.That(brain.State, Is.EqualTo(WardenState.Charging));
            Assert.That(brain.Vulnerability, Is.LessThan(1f));
            Assert.That(brain.HitWall(), Is.EqualTo(WardenEvent.ChargeEnd));
            Assert.That(brain.State, Is.EqualTo(WardenState.Recover));
            Assert.That(brain.Vulnerability, Is.GreaterThan(1f));
            var exposed = 0f;
            while (brain.State == WardenState.Recover && exposed < 10) { brain.Tick(Frame, Near(8)); exposed += Frame; }
            Assert.That(exposed, Is.GreaterThanOrEqualTo(brain.WallStun - Frame));
            Assert.That(brain.HitWall(), Is.EqualTo(WardenEvent.None), "wall hits only count while charging");
        }

        [Test]
        public void WardenReturnsHomeWhenLeashedAndResetsEnrage()
        {
            var brain = new WardenBrain();
            brain.Tick(Frame, Near(5));
            Assert.That(brain.State, Is.EqualTo(WardenState.Chase));
            brain.Tick(Frame, Near(5, health: .4f));
            Assert.That(brain.Enraged, Is.True);
            brain.Tick(Frame, Near(5, health: .4f, home: 30));
            Assert.That(brain.State, Is.EqualTo(WardenState.Return));
            Assert.That(brain.Tick(Frame, Near(30, home: .2f)), Is.EqualTo(WardenEvent.Reset));
            Assert.That(brain.State, Is.EqualTo(WardenState.Idle));
            Assert.That(brain.Enraged, Is.False);
        }

        [Test]
        public void EnrageIsAnnouncedOnceAndShortensButKeepsTelegraphs()
        {
            var brain = new WardenBrain();
            var announcements = 0;
            for (var i = 0; i < 300; i++) if (brain.Tick(Frame, Near(2.5f, health: .2f)) == WardenEvent.Enraged) announcements++;
            Assert.That(announcements, Is.EqualTo(1));
            Assert.That(brain.CurrentWindup, Is.GreaterThanOrEqualTo(WardenBrain.MinimumTelegraph));
            Assert.That(brain.SlamRadius, Is.GreaterThan(new WardenBrain().SlamRadius));
        }
    }
}
