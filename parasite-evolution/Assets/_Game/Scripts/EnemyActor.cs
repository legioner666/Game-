using UnityEngine;

namespace ParasiteEvolution
{
    public enum EnemyKind { Drone, Guard, Zapper, Warden }

    public sealed class EnemyActor : MonoBehaviour
    {
        public GameSession Session;
        public bool Alive { get; private set; } = true;
        public EnemyKind Kind { get; private set; }
        public bool Warden => Kind == EnemyKind.Warden;
        public int Zone { get; private set; }
        public int Units => Warden ? 3 : Zone == 2 ? 2 : 1;
        public int Value => Warden ? 30 : 3 + Zone * 2 + (Kind == EnemyKind.Zapper ? 1 : 0);
        public int Experience => Warden ? 20 : 3 + Zone;
        public float Health { get; private set; }
        public float HealthFraction => maximum > 0 ? Health / maximum : 0;
        public bool Pulled => pullUntil > Time.time;
        public float CaptureMultiplier => Warden ? brain.Vulnerability : 1;
        public WardenBrain Brain => brain;
        CharacterController controller;
        Transform model, bar;
        Material shell, core;
        Color shellColor, coreColor;
        Vector3 home, goal, knock, lastPosition, sidestep, lockedDir, beamFrom, beamTo;
        float maximum, attackTimer, wanderTimer, respawnTimer, pullUntil, warning, flash, stuckTimer, sidestepTimer;
        bool chargeHit;
        LineRenderer telegraph;
        WardenBrain brain;

        public void Initialize(GameSession session, Vector3 point, int zone, EnemyKind kind)
        {
            Session = session; home = goal = lastPosition = point; transform.position = point; Zone = zone; Kind = kind;
            maximum = kind == EnemyKind.Warden ? 260 : kind == EnemyKind.Drone ? 18 : kind == EnemyKind.Zapper ? 34 + zone * 10 : 24 + zone * 18;
            Health = maximum; gameObject.layer = 10;
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = Warden ? 2.4f : 1.5f; controller.center = Vector3.up * controller.height * .5f;
            controller.radius = Warden ? .95f : .38f; controller.stepOffset = .2f;
            model = new GameObject("Procedural " + kind).transform; model.SetParent(transform, false);
            shellColor = kind == EnemyKind.Warden ? new Color(.42f, .25f, .2f) : kind == EnemyKind.Zapper ? new Color(.3f, .3f, .42f)
                : kind == EnemyKind.Drone ? new Color(.5f, .55f, .58f) : new Color(.36f, .45f, .48f);
            coreColor = kind == EnemyKind.Warden ? new Color(1, .48f, .12f) : kind == EnemyKind.Zapper ? new Color(.45f, .7f, 1)
                : new Color(.25f, .85f, .72f);
            shell = Visuals.Material(shellColor); core = Visuals.Glow(coreColor, 1.6f);
            BuildModel();
            var hp = Visuals.Shape(PrimitiveType.Cube, model, new Vector3(0, Warden ? 2.9f : 1.75f, 0), new Vector3(.9f, .06f, .1f), core);
            bar = hp.transform;
            if (kind == EnemyKind.Warden || kind == EnemyKind.Zapper)
            {
                telegraph = Visuals.Line(transform, Visuals.Material(new Color(1, .2f, .15f)), kind == EnemyKind.Zapper ? .12f : .1f);
                telegraph.enabled = false;
            }
            if (Warden) brain = new WardenBrain();
            session.Enemies.Add(this);
        }

        void BuildModel()
        {
            switch (Kind)
            {
                case EnemyKind.Warden:
                    Visuals.Shape(PrimitiveType.Capsule, model, new Vector3(0, 1.3f, 0), new Vector3(1.5f, 1.3f, 1.5f), shell);
                    foreach (var side in new[] { -1, 1 })
                    {
                        Visuals.Shape(PrimitiveType.Cube, model, new Vector3(side * .95f, .95f, .1f), new Vector3(.6f, .85f, 1f), shell);
                        Visuals.Shape(PrimitiveType.Cube, model, new Vector3(side * .95f, .5f, .75f), new Vector3(.45f, .25f, .5f), core);
                    }
                    Visuals.Shape(PrimitiveType.Cube, model, new Vector3(0, 1.75f, .7f), new Vector3(.8f, .18f, .12f), core);
                    break;
                case EnemyKind.Drone:
                    Visuals.Shape(PrimitiveType.Sphere, model, new Vector3(0, .75f, 0), new Vector3(.62f, .5f, .62f), shell);
                    Visuals.Shape(PrimitiveType.Cylinder, model, new Vector3(0, 1.05f, 0), new Vector3(.9f, .02f, .9f), core);
                    break;
                case EnemyKind.Zapper:
                    Visuals.Shape(PrimitiveType.Cylinder, model, new Vector3(0, .7f, 0), new Vector3(.55f, .7f, .55f), shell);
                    Visuals.Shape(PrimitiveType.Cube, model, new Vector3(0, 1.3f, .25f), new Vector3(.18f, .18f, .6f), core);
                    break;
                default:
                    Visuals.Shape(PrimitiveType.Capsule, model, new Vector3(0, .8f, 0), new Vector3(.68f, .68f, .68f), shell);
                    Visuals.Shape(PrimitiveType.Cube, model, new Vector3(0, 1.15f, .3f), new Vector3(.43f, .12f, .1f), core);
                    break;
            }
        }

        void Update()
        {
            if (Session == null || Session.Paused || Session.Player == null) return;
            var dt = Time.deltaTime;
            if (!Alive) { TickRespawn(dt); return; }
            UpdateFeedback(dt);
            var player = Session.Player.transform.position;
            var distance = Flat(transform.position - player).magnitude;
            if (Pulled)
            {
                if (!WorldBuilder.ClearLine(transform.position, player)) { pullUntil = 0; return; }
                CancelAttack();
                var away = Flat(transform.position - player).normalized;
                var target = player + away * 1.9f;
                Move(Vector3.ClampMagnitude(Flat(target - transform.position), 12 * dt));
                return;
            }
            if (Warden) { UpdateWarden(dt, player, distance); return; }
            if (warning > 0) { UpdateZap(dt, player); return; }
            attackTimer -= dt; wanderTimer -= dt;
            var safe = WorldBuilder.IsSafe(player);
            var leashed = Flat(transform.position - home).magnitude > 15;
            var sees = distance < 8 && !safe && WorldBuilder.ClearLine(transform.position, player);
            var speed = 2.1f;
            if (leashed || (safe && distance < 10)) goal = home;
            else if (sees)
            {
                switch (Kind)
                {
                    case EnemyKind.Drone:
                        goal = transform.position + Flat(transform.position - player).normalized * 3; speed = 2.6f;
                        break;
                    case EnemyKind.Zapper:
                        // Keeps distance and fires a telegraphed beam.
                        goal = distance < 5 ? transform.position + Flat(transform.position - player).normalized * 2
                            : distance > 7 ? player : transform.position;
                        if (attackTimer <= 0 && distance < 9)
                        {
                            attackTimer = 3.2f; warning = .75f;
                            beamFrom = transform.position + Vector3.up * 1.3f; beamTo = player + Vector3.up * .5f;
                            var dir = Flat(beamTo - beamFrom).normalized; beamTo = beamFrom + dir * 10; beamTo.y = .5f;
                            DrawLine(beamFrom, beamTo);
                        }
                        break;
                    default:
                        goal = player; speed = Zone == 2 ? 2.5f : 2.2f;
                        if (attackTimer <= 0 && distance < 1.6f)
                        { attackTimer = 1.5f; Session.Player.Hurt(4 + Zone * 2); }
                        break;
                }
            }
            else if (wanderTimer <= 0)
            {
                wanderTimer = 2 + Random.value * 2;
                var candidate = home + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f));
                if (!Physics.CheckSphere(candidate + Vector3.up * .7f, .5f, WorldBuilder.WallMask, QueryTriggerInteraction.Ignore)) goal = candidate;
            }
            if (WorldBuilder.IsSafe(goal)) goal = home;
            Steer(dt, speed);
            if (leashed && Flat(transform.position - home).magnitude < .6f) Health = maximum;
        }

        void UpdateWarden(float dt, Vector3 player, float distance)
        {
            var senses = new WardenSenses {
                PlayerDistance = distance, HomeDistance = Flat(transform.position - home).magnitude,
                HealthFraction = HealthFraction, PlayerSafe = WorldBuilder.IsSafe(player),
                LineOfSight = WorldBuilder.ClearLine(transform.position, player)
            };
            var e = brain.Tick(dt, senses);
            switch (e)
            {
                case WardenEvent.Aggro: Session.Note("Наглядач помітив організм."); break;
                case WardenEvent.Enraged:
                    Session.Note("Наглядач перегрівається: атаки частіші!");
                    core.SetColor("_EmissionColor", new Color(1, .1f, .05f) * 3); break;
                case WardenEvent.SlamWarning: DrawRing(transform.position, brain.SlamRadius); break;
                case WardenEvent.SlamImpact:
                    telegraph.enabled = false;
                    if (Flat(player - transform.position).magnitude < brain.SlamRadius) Session.Player.Hurt(brain.Enraged ? 20 : 15);
                    Session.Ring(transform.position, brain.SlamRadius, new Color(1, .35f, .15f)); Session.Shake(.35f);
                    break;
                case WardenEvent.ChargeWarning:
                    lockedDir = Flat(player - transform.position).normalized;
                    if (lockedDir.sqrMagnitude < .01f) lockedDir = transform.forward;
                    DrawLine(transform.position + Vector3.up * .1f, transform.position + lockedDir * 11 + Vector3.up * .1f);
                    break;
                case WardenEvent.ChargeStart: telegraph.enabled = false; chargeHit = false; break;
                case WardenEvent.ChargeEnd: break;
                case WardenEvent.Reset: Health = maximum; core.SetColor("_EmissionColor", coreColor * 1.6f); break;
            }
            switch (brain.State)
            {
                case WardenState.Chase:
                    goal = player; Steer(dt, 1.6f); Face(player - transform.position, dt);
                    break;
                case WardenState.Return:
                    goal = home; Steer(dt, 2.4f);
                    break;
                case WardenState.Charging:
                    var before = transform.position;
                    var flags = controller.Move(lockedDir * 11 * dt + Vector3.down * 4 * dt);
                    if (!chargeHit && Flat(player - transform.position).magnitude < 1.7f)
                    { chargeHit = true; Session.Player.Hurt(brain.Enraged ? 22 : 18); Session.Shake(.3f); }
                    var progress = Flat(transform.position - before).magnitude;
                    if ((flags & CollisionFlags.Sides) != 0 && progress < 11 * dt * .5f)
                    {
                        brain.HitWall(); Session.Shake(.45f);
                        Session.Note("Наглядач врізався й відкритий для захоплення!");
                    }
                    break;
                case WardenState.SlamWindup:
                case WardenState.ChargeWindup:
                    Face(lockedDir.sqrMagnitude > .01f && brain.State == WardenState.ChargeWindup ? lockedDir : player - transform.position, dt);
                    break;
                case WardenState.Recover:
                    // Exposed: the core blinks so the player reads the capture window.
                    core.SetColor("_EmissionColor", coreColor * (Mathf.Repeat(Time.time * 6, 1) < .5f ? 3.5f : .4f));
                    break;
            }
            if (brain.State != WardenState.Recover && e == WardenEvent.None && brain.StateTime < dt * 1.5f)
                core.SetColor("_EmissionColor", (brain.Enraged ? new Color(1, .1f, .05f) : coreColor) * 1.6f);
        }

        void UpdateZap(float dt, Vector3 player)
        {
            warning -= dt;
            if (warning > 0) return;
            telegraph.enabled = false;
            if (DistanceToSegment(player + Vector3.up * .5f, beamFrom, beamTo) < .95f &&
                WorldBuilder.ClearLine(beamFrom, player)) Session.Player.Hurt(7 + Zone * 2);
            Session.Beam(beamFrom, beamTo, coreColor);
        }

        void Steer(float dt, float speed)
        {
            var motion = Flat(goal - transform.position);
            if (sidestepTimer > 0) { sidestepTimer -= dt; motion = sidestep; }
            if (motion.magnitude > .25f)
            {
                Move(motion.normalized * speed * dt);
                Face(motion, dt);
                // Stuck detection: if the body barely moves while it wants to, sidestep around the obstacle.
                stuckTimer += dt;
                if (stuckTimer > .6f)
                {
                    if (Flat(transform.position - lastPosition).magnitude < speed * .6f * .25f && sidestepTimer <= 0)
                    {
                        sidestep = Vector3.Cross(Vector3.up, motion.normalized) * (Random.value < .5f ? -1 : 1);
                        sidestepTimer = .7f;
                    }
                    stuckTimer = 0; lastPosition = transform.position;
                }
            }
            else Move(Vector3.zero);
        }

        void Move(Vector3 delta)
        {
            knock = Vector3.MoveTowards(knock, Vector3.zero, 18 * Time.deltaTime);
            controller.Move(delta + knock * Time.deltaTime + Vector3.down * 4 * Time.deltaTime);
        }

        void Face(Vector3 direction, float dt)
        {
            direction.y = 0;
            if (direction.sqrMagnitude < .01f) return;
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-8 * dt));
        }

        void UpdateFeedback(float dt)
        {
            var fraction = Mathf.Clamp01(HealthFraction);
            bar.gameObject.SetActive(fraction < .999f);
            bar.localScale = new Vector3(.9f * fraction, .06f, .1f);
            if (flash <= 0) return;
            flash = Mathf.Max(0, flash - dt);
            var c = Color.Lerp(shellColor, Color.white, flash * 6);
            shell.color = c;
            if (shell.HasProperty("_BaseColor")) shell.SetColor("_BaseColor", c);
        }

        void TickRespawn(float dt)
        {
            respawnTimer -= dt;
            var player = Session.Player.transform.position;
            if (respawnTimer > 0 || Flat(home - player).magnitude < 14) return;
            // Respawn at home, out of the player's sight, never where the body was captured.
            transform.position = home; goal = home; lastPosition = home; knock = Vector3.zero;
            Alive = true; Health = maximum; controller.enabled = true; model.gameObject.SetActive(true);
            if (brain != null) brain = new WardenBrain();
        }

        public void Pull()
        {
            if (!Alive) return;
            CancelAttack();
            pullUntil = Time.time + .65f;
        }

        public void Knock(Vector3 direction, float force)
        {
            if (!Alive) return;
            direction.y = 0;
            knock = direction.normalized * force * (Warden ? .2f : 1);
            if (!Warden) CancelAttack();
        }

        void CancelAttack()
        {
            if (Warden) return; // Warden attacks are only cancelled by its own state machine.
            warning = 0; if (telegraph != null) telegraph.enabled = false;
        }

        public void Damage(float amount)
        {
            if (!Alive || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            flash = Mathf.Max(flash, .12f);
            if (Health > 0) return;
            if (!Session.Collect(this)) { Health = 1; return; }
            Alive = false; pullUntil = 0; warning = 0;
            if (telegraph != null) telegraph.enabled = false;
            controller.enabled = false; model.gameObject.SetActive(false);
            respawnTimer = Warden ? 90 : 25;
        }

        public void ResetToHome()
        {
            if (!Alive) return;
            CancelAttack(); brain?.ForceReturn();
            goal = home;
        }

        void DrawRing(Vector3 center, float radius)
        {
            telegraph.enabled = true; telegraph.positionCount = 49;
            for (var i = 0; i <= 48; i++)
            { var a = i / 48f * Mathf.PI * 2; telegraph.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, .08f, Mathf.Sin(a) * radius)); }
        }

        void DrawLine(Vector3 from, Vector3 to)
        {
            telegraph.enabled = true; telegraph.positionCount = 2;
            telegraph.SetPosition(0, from); telegraph.SetPosition(1, to);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
            return Vector3.Distance(p, a + ab * t);
        }

        void OnDestroy()
        {
            if (Session != null) Session.Enemies.Remove(this);
            if (shell != null) Destroy(shell);
            if (core != null) Destroy(core);
        }
    }
}
