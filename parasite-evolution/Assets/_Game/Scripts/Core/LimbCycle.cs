using System;

namespace ParasiteEvolution
{
    // Unity-free lifecycle of one retractile limb. Geometry lives in SlimeVisual; this class owns
    // only timing, so the extend -> anchor -> contract -> retract rules can be tested without Unity.
    public enum LimbPhase { Retracted, Extending, Anchored, Contracting, Retracting }

    public sealed class LimbCycle
    {
        public LimbPhase Phase { get; private set; } = LimbPhase.Retracted;
        // 0 = fully inside the body, 1 = tip reached its anchor.
        public float Extension { get; private set; }
        // 0..1 contraction while anchored; the visual shortens and thickens the limb with it.
        public float Pull { get; private set; }
        public float TimeInPhase { get; private set; }
        // True for exactly one Tick after the limb starts a new reach; the visual picks a new anchor then.
        public bool NeedsAnchor { get; private set; }
        public bool InFlight => Phase == LimbPhase.Extending || Phase == LimbPhase.Retracting;
        public bool Planted => Phase == LimbPhase.Anchored || Phase == LimbPhase.Contracting;

        public float ExtendSpeed = 7f;
        public float RetractSpeed = 6f;
        public float PullSpeed = 6f;
        // How far a stepping limb withdraws before it reaches for the next anchor.
        public float StepLift = .35f;

        bool reachAfterRetract;

        public void Reach()
        {
            if (Phase == LimbPhase.Extending) return;
            if (Phase == LimbPhase.Retracted)
            {
                Enter(LimbPhase.Extending);
                NeedsAnchor = true;
                return;
            }
            // Planted or retracting limbs lift first and then reach, so the tip never teleports.
            reachAfterRetract = true;
            if (Phase != LimbPhase.Retracting) Enter(LimbPhase.Retracting);
        }

        public void Contract()
        {
            if (Phase == LimbPhase.Anchored) Enter(LimbPhase.Contracting);
        }

        public void Release()
        {
            reachAfterRetract = false;
            if (Phase != LimbPhase.Retracted && Phase != LimbPhase.Retracting) Enter(LimbPhase.Retracting);
        }

        public void Snap()
        {
            reachAfterRetract = false;
            Extension = 0;
            Pull = 0;
            NeedsAnchor = false;
            Enter(LimbPhase.Retracted);
        }

        public void Tick(float dt)
        {
            NeedsAnchor = false;
            if (!(dt > 0) || float.IsInfinity(dt)) return;
            TimeInPhase += dt;
            Pull = Phase == LimbPhase.Contracting
                ? Math.Min(1, Pull + dt * PullSpeed)
                : Math.Max(0, Pull - dt * PullSpeed * 1.5f);
            switch (Phase)
            {
                case LimbPhase.Extending:
                    Extension = Math.Min(1, Extension + dt * ExtendSpeed);
                    if (Extension >= 1) Enter(LimbPhase.Anchored);
                    break;
                case LimbPhase.Retracting:
                    var floor = reachAfterRetract ? StepLift : 0;
                    Extension = Math.Max(floor, Extension - dt * RetractSpeed);
                    if (Extension <= floor)
                    {
                        if (reachAfterRetract)
                        {
                            reachAfterRetract = false;
                            Enter(LimbPhase.Extending);
                            NeedsAnchor = true;
                        }
                        else Enter(LimbPhase.Retracted);
                    }
                    break;
            }
        }

        void Enter(LimbPhase next)
        {
            Phase = next;
            TimeInPhase = 0;
        }
    }
}
