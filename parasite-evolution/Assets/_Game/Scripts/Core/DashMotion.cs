using System;

namespace ParasiteEvolution
{
    // Anchor -> contraction dash: limbs shoot out (Reach), the body is hauled along an eased curve
    // (Pull), then the mass settles. Tick returns the distance to move this frame; the sum of all
    // returned distances equals the requested distance exactly, with no overshoot.
    public sealed class DashMotion
    {
        public enum Stage { Idle, Reach, Pull, Settle }

        public Stage Current { get; private set; }
        public float Distance { get; private set; }
        public float Travelled { get; private set; }
        public float PullDuration { get; private set; }
        public bool Active => Current != Stage.Idle;
        // Body is airborne/committed; used for brief invulnerability and stretch.
        public bool Pulling => Current == Stage.Pull;
        public float PullProgress => PullDuration > 0 && Current == Stage.Pull ? Clamp01(timer / PullDuration) : Current == Stage.Settle ? 1 : 0;

        public float ReachTime = .12f;
        public float PullSpeed = 20f;
        public float SettleTime = .1f;

        float timer;

        public void Begin(float distance)
        {
            if (!(distance > 0) || float.IsInfinity(distance)) throw new ArgumentOutOfRangeException(nameof(distance));
            Distance = distance;
            Travelled = 0;
            PullDuration = Math.Max(.15f, distance / PullSpeed * 1.5f);
            timer = 0;
            Current = Stage.Reach;
        }

        public void Abort()
        {
            Current = Stage.Idle;
            timer = 0;
        }

        public float Tick(float dt)
        {
            if (!Active || !(dt > 0) || float.IsInfinity(dt)) return 0;
            timer += dt;
            switch (Current)
            {
                case Stage.Reach:
                    if (timer < ReachTime) return 0;
                    var overflow = timer - ReachTime;
                    Current = Stage.Pull;
                    timer = 0;
                    return overflow > 0 ? Tick(overflow) : 0;
                case Stage.Pull:
                    var target = Distance * Ease(Clamp01(timer / PullDuration));
                    if (timer >= PullDuration) target = Distance;
                    var step = Math.Max(0, target - Travelled);
                    Travelled += step;
                    if (timer >= PullDuration)
                    {
                        Travelled = Distance;
                        Current = Stage.Settle;
                        timer = 0;
                    }
                    return step;
                case Stage.Settle:
                    if (timer >= SettleTime) Current = Stage.Idle;
                    return 0;
            }
            return 0;
        }

        // Slow start while the limbs take the load, fast middle, soft arrival.
        static float Ease(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
