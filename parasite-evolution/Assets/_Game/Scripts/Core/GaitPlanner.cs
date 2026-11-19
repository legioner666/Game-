using System;

namespace ParasiteEvolution
{
    // Spider-like alternating gait for the support limbs. The caller supplies per-limb strain
    // (how far the planted tip lags behind its ideal foothold, in limb-length units); the planner
    // decides when a limb lifts and reaches again. Two groups alternate, and at most half of the
    // limbs are allowed in flight, so the body always reads as anchored while it moves.
    public sealed class GaitPlanner
    {
        public readonly LimbCycle[] Limbs;
        readonly int[] group;
        public float IdleRetractDelay = .9f;
        public float StrainStep = .55f;
        public float StrainForce = 1.15f;
        public float SlowPeriod = .38f;
        public float FastPeriod = .17f;
        // While an ability owns the limbs, the planner only ticks them.
        public bool Suspended;
        public int ActiveGroup { get; private set; }
        public float IdleTime { get; private set; }

        float phaseTimer;

        public GaitPlanner(int[] groups)
        {
            if (groups == null || groups.Length == 0) throw new ArgumentException("At least one limb is required.");
            group = (int[])groups.Clone();
            Limbs = new LimbCycle[group.Length];
            for (var i = 0; i < Limbs.Length; i++)
            {
                if (group[i] != 0 && group[i] != 1) throw new ArgumentException("Gait groups must be 0 or 1.");
                Limbs[i] = new LimbCycle();
            }
        }

        public int MaxInFlight => (Limbs.Length + 1) / 2;

        public int InFlight
        {
            get
            {
                var n = 0;
                foreach (var limb in Limbs) if (limb.InFlight) n++;
                return n;
            }
        }

        public void Tick(float dt, float speed01, float[] strain)
        {
            if (!(dt > 0) || float.IsInfinity(dt)) return;
            if (strain == null || strain.Length < Limbs.Length) throw new ArgumentException("Strain per limb is required.");
            speed01 = Clamp01(speed01);
            if (!Suspended)
            {
                if (speed01 < .05f) Rest(dt);
                else Walk(dt, speed01, strain);
            }
            foreach (var limb in Limbs) limb.Tick(dt);
        }

        void Rest(float dt)
        {
            IdleTime += dt;
            phaseTimer = 0;
            if (IdleTime < IdleRetractDelay) return;
            // Idle organism withdraws every limb; the body alone stays recognisable.
            foreach (var limb in Limbs) limb.Release();
        }

        void Walk(float dt, float speed01, float[] strain)
        {
            IdleTime = 0;
            var period = SlowPeriod + (FastPeriod - SlowPeriod) * speed01;
            phaseTimer += dt;
            if (phaseTimer >= period)
            {
                phaseTimer = 0;
                ActiveGroup ^= 1;
            }
            var flying = InFlight;
            for (var i = 0; i < Limbs.Length; i++)
            {
                if (flying >= MaxInFlight) break;
                var limb = Limbs[i];
                var s = float.IsNaN(strain[i]) ? 0 : strain[i];
                var mine = group[i] == ActiveGroup;
                var wants = limb.Phase == LimbPhase.Retracted ? mine
                    : limb.Planted && (s >= StrainForce || (mine && s >= StrainStep));
                if (!wants) continue;
                limb.Reach();
                flying++;
            }
        }

        public void ReleaseAll()
        {
            foreach (var limb in Limbs) limb.Release();
        }

        public void SnapAll()
        {
            foreach (var limb in Limbs) limb.Snap();
            IdleTime = 0;
            phaseTimer = 0;
        }

        static float Clamp01(float v) => float.IsNaN(v) ? 0 : v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
