using System;

namespace ParasiteEvolution
{
    public enum WardenState { Idle, Chase, SlamWindup, ChargeWindup, Charging, Recover, Return }
    public enum WardenEvent { None, Aggro, SlamWarning, SlamImpact, ChargeWarning, ChargeStart, ChargeEnd, Enraged, Reset }

    public struct WardenSenses
    {
        public float PlayerDistance;
        public float HomeDistance;
        public float HealthFraction;
        public bool PlayerSafe;
        public bool LineOfSight;
    }

    // Unity-free decision logic for the mechanised Warden. Every attack is telegraphed; after a
    // charge the Warden is exposed, which is the intended window for capture.
    public sealed class WardenBrain
    {
        public WardenState State { get; private set; } = WardenState.Idle;
        public bool Enraged { get; private set; }
        public float StateTime { get; private set; }
        public float Cooldown { get; private set; } = 1f;
        public bool ChargeHitWall { get; private set; }

        public float AggroRange = 9f;
        public float LeashRange = 17f;
        public float SlamRange = 3.6f;
        public float ChargeMinRange = 4.5f;
        public float ChargeMaxRange = 12f;
        public float SlamWindup = .9f;
        public float ChargeWindup = .75f;
        public float ChargeDuration = .8f;
        public float SlamRecover = .7f;
        public float ChargeRecover = 1.4f;
        public float WallStun = 2.4f;
        public float AttackCooldown = 2.4f;
        public const float MinimumTelegraph = .5f;

        // Capture damage multiplier: armoured normally, exposed while recovering.
        public float Vulnerability => State == WardenState.Recover ? 1.6f : .55f;
        public float SlamRadius => Enraged ? 4.2f : 3.2f;
        public float CurrentWindup => Math.Max(MinimumTelegraph, (State == WardenState.ChargeWindup ? ChargeWindup : SlamWindup) * (Enraged ? .75f : 1f));
        float recoverFor;

        public WardenEvent Tick(float dt, WardenSenses s)
        {
            if (!(dt > 0) || float.IsInfinity(dt)) return WardenEvent.None;
            StateTime += dt;
            if (!Enraged && s.HealthFraction > 0 && s.HealthFraction < .5f)
            {
                Enraged = true;
                return WardenEvent.Enraged;
            }
            switch (State)
            {
                case WardenState.Idle:
                    if (!s.PlayerSafe && s.LineOfSight && s.PlayerDistance < AggroRange && s.HomeDistance < LeashRange)
                    {
                        Enter(WardenState.Chase);
                        Cooldown = Math.Max(Cooldown, .6f);
                        return WardenEvent.Aggro;
                    }
                    return WardenEvent.None;
                case WardenState.Chase:
                    if (s.PlayerSafe || s.HomeDistance > LeashRange || s.PlayerDistance > AggroRange * 1.8f)
                    {
                        Enter(WardenState.Return);
                        return WardenEvent.None;
                    }
                    Cooldown -= dt;
                    if (Cooldown > 0 || !s.LineOfSight) return WardenEvent.None;
                    if (s.PlayerDistance <= SlamRange)
                    {
                        Enter(WardenState.SlamWindup);
                        return WardenEvent.SlamWarning;
                    }
                    if (s.PlayerDistance >= ChargeMinRange && s.PlayerDistance <= ChargeMaxRange)
                    {
                        Enter(WardenState.ChargeWindup);
                        return WardenEvent.ChargeWarning;
                    }
                    return WardenEvent.None;
                case WardenState.SlamWindup:
                    if (StateTime < CurrentWindup) return WardenEvent.None;
                    BeginRecover(SlamRecover);
                    return WardenEvent.SlamImpact;
                case WardenState.ChargeWindup:
                    if (StateTime < CurrentWindup) return WardenEvent.None;
                    ChargeHitWall = false;
                    Enter(WardenState.Charging);
                    return WardenEvent.ChargeStart;
                case WardenState.Charging:
                    if (StateTime < ChargeDuration) return WardenEvent.None;
                    BeginRecover(ChargeRecover);
                    return WardenEvent.ChargeEnd;
                case WardenState.Recover:
                    if (StateTime < recoverFor) return WardenEvent.None;
                    Enter(s.HomeDistance > LeashRange || s.PlayerSafe ? WardenState.Return : WardenState.Chase);
                    return WardenEvent.None;
                case WardenState.Return:
                    if (s.HomeDistance < .6f)
                    {
                        Enter(WardenState.Idle);
                        Enraged = false;
                        Cooldown = 1f;
                        return WardenEvent.Reset;
                    }
                    if (!s.PlayerSafe && s.LineOfSight && s.PlayerDistance < AggroRange * .7f && s.HomeDistance < LeashRange * .6f)
                        Enter(WardenState.Chase);
                    return WardenEvent.None;
            }
            return WardenEvent.None;
        }

        // Called by the Unity layer when a charge collides with level geometry.
        public WardenEvent HitWall()
        {
            if (State != WardenState.Charging) return WardenEvent.None;
            ChargeHitWall = true;
            BeginRecover(WallStun);
            return WardenEvent.ChargeEnd;
        }

        public void ForceReturn()
        {
            if (State != WardenState.Idle) Enter(WardenState.Return);
        }

        void BeginRecover(float seconds)
        {
            recoverFor = seconds;
            Cooldown = AttackCooldown * (Enraged ? .7f : 1f);
            Enter(WardenState.Recover);
        }

        void Enter(WardenState next)
        {
            State = next;
            StateTime = 0;
        }
    }
}
