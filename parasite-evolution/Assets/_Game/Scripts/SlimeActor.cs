using UnityEngine;
using UnityEngine.InputSystem;

namespace ParasiteEvolution
{
    // Authoritative movement stays on CharacterController; the slime visuals only present it.
    public sealed class SlimeActor : MonoBehaviour
    {
        // Matches the visual mass (SlimeVisual.BodyScale) so the body does not sink into walls.
        public const float Radius = .78f;
        public const float Height = 1.56f;
        public GameSession Session;
        public SlimeVisual Visual { get; private set; }
        public float Health;
        public float PullRemaining, MoveRemaining, DamageRemaining;
        public Vector3 Aim { get; private set; } = Vector3.forward;
        public bool Dashing => dash.Active;
        public bool Invulnerable => invulnerable > 0 || dash.Pulling;
        public float Speed01 { get; private set; }
        CharacterController controller;
        readonly DashMotion dash = new DashMotion();
        Vector3 dashDir, velocity;
        float verticalSpeed, invulnerable, blockedTime;

        public void Initialize(GameSession session)
        {
            Session = session; gameObject.layer = 9;
            controller = gameObject.AddComponent<CharacterController>();
            controller.radius = Radius; controller.height = Height; controller.center = new Vector3(0, Height * .5f, 0);
            controller.skinWidth = .05f; controller.stepOffset = .3f; controller.slopeLimit = 50;
            Visual = gameObject.AddComponent<SlimeVisual>();
            var p = session.Profile; Health = p.health;
            PullRemaining = p.pullRemaining; MoveRemaining = p.moveRemaining; DamageRemaining = p.damageRemaining;
            var saved = new Vector3(p.x, 0, p.z);
            // A save written by an older map may place the organism inside new geometry.
            Teleport(IsFree(saved) ? saved : WorldBuilder.SafePoint);
        }

        void Update()
        {
            if (Session == null || Session.Paused) return;
            var dt = Time.deltaTime;
            PullRemaining = Mathf.Max(0, PullRemaining - dt); MoveRemaining = Mathf.Max(0, MoveRemaining - dt);
            DamageRemaining = Mathf.Max(0, DamageRemaining - dt); invulnerable = Mathf.Max(0, invulnerable - dt);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) Session.Cast("AB-PULL");
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Session.Cast("AB-MOVE");
                if (keyboard.digit3Key.wasPressedThisFrame) Session.Cast("AB-DAMAGE");
                if (keyboard.escapeKey.wasPressedThisFrame) Session.TogglePause();
            }
            if (Session.Paused) return;
            Vector3 delta;
            if (dash.Active)
            {
                var wasReach = dash.Current == DashMotion.Stage.Reach;
                var step = dash.Tick(dt);
                if (wasReach && dash.Current != DashMotion.Stage.Reach) Visual.PullDash();
                delta = dashDir * step;
                velocity = dashDir * (step / dt);
                if (!dash.Active) Visual.EndDash();
            }
            else
            {
                var input = Session.Hud.Move;
                if (keyboard != null) input += new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                input = Vector2.ClampMagnitude(input, 1);
                var direction = new Vector3(input.x, 0, input.y);
                if (direction.sqrMagnitude > .01f) Aim = direction.normalized;
                // A heavy mass: it builds speed as limbs take hold and slides to a stop.
                var surge = 1 - .14f * Mathf.Cos(Time.time * 10.5f);
                var target = direction * Balance.Speed(Session.Profile) * surge;
                var response = direction.sqrMagnitude > .01f ? 9f : 6f;
                velocity = Vector3.Lerp(velocity, target, 1 - Mathf.Exp(-response * dt));
                delta = velocity * dt;
            }
            verticalSpeed = controller.isGrounded ? -2f : Mathf.Max(-25f, verticalSpeed - 30f * dt);
            var prior = transform.position;
            controller.Move(delta + Vector3.up * verticalSpeed * dt);
            var moved = transform.position - prior; moved.y = 0;
            Speed01 = Mathf.Clamp01(moved.magnitude / dt / Mathf.Max(1, Balance.Speed(Session.Profile)));
            if (dash.Pulling && delta.sqrMagnitude > 1e-6f)
            {
                // Abort a dash that is pressed against geometry instead of grinding through it.
                blockedTime = moved.sqrMagnitude < delta.sqrMagnitude * .09f ? blockedTime + dt : 0;
                if (blockedTime > .05f) EndDash();
            }
            var face = dash.Active ? dashDir : velocity;
            face.y = 0;
            if (face.sqrMagnitude > .04f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 1 - Mathf.Exp(-8 * dt));
            if (transform.position.y < -4) Teleport(WorldBuilder.SafePoint);
            Health = Mathf.Min(Health, Balance.MaxHealth(Session.Profile));
        }

        public bool StartDash(float distance)
        {
            if (dash.Active) return false;
            var origin = transform.position + Vector3.up * (Height * .5f);
            if (Physics.SphereCast(origin, Radius + .02f, Aim, out var hit, distance, WorldBuilder.WallMask, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0, hit.distance - .2f);
            if (distance < 1) return false;
            var goal = transform.position + Aim * distance;
            if (!Physics.Raycast(goal + Vector3.up * 3, Vector3.down, 5, WorldBuilder.EnvironmentMask)) return false;
            dashDir = Aim; blockedTime = 0;
            dash.Begin(distance);
            Visual.BeginDash(new Vector3(goal.x, 0, goal.z));
            return true;
        }

        void EndDash()
        {
            dash.Abort(); blockedTime = 0;
            if (Visual != null) Visual.EndDash();
        }

        public void Teleport(Vector3 point)
        {
            if (controller != null) controller.enabled = false;
            transform.position = point;
            if (controller != null) controller.enabled = true;
            dash.Abort(); velocity = Vector3.zero; verticalSpeed = 0;
            if (Visual != null) Visual.ResetContacts();
        }

        public void Protect(float seconds) { invulnerable = Mathf.Max(invulnerable, seconds); }

        // Moves the organism to the nearest free floor spot; used by the pause-menu unstick action.
        public bool Unstick()
        {
            var origin = transform.position; origin.y = 0;
            if (IsFree(origin) && controller.isGrounded) return false;
            for (var ring = 1; ring <= 8; ring++)
                for (var k = 0; k < 12; k++)
                {
                    var a = k / 12f * Mathf.PI * 2;
                    var candidate = origin + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * ring * .75f;
                    if (!IsFree(candidate)) continue;
                    Teleport(candidate);
                    return true;
                }
            Teleport(WorldBuilder.SafePoint);
            return true;
        }

        public static bool IsFree(Vector3 point)
        {
            var bottom = point + Vector3.up * (Radius + .05f);
            var top = point + Vector3.up * (Height - Radius);
            return !Physics.CheckCapsule(bottom, top, Radius, WorldBuilder.WallMask, QueryTriggerInteraction.Ignore) &&
                Physics.Raycast(point + Vector3.up * 2, Vector3.down, 4, WorldBuilder.EnvironmentMask, QueryTriggerInteraction.Ignore);
        }

        public void Hurt(float damage)
        {
            if (Session.Paused || Invulnerable || WorldBuilder.IsSafe(transform.position) || damage <= 0 || Health <= 0) return;
            var taken = Balance.DamageTaken(Session.Profile, damage);
            Health = Mathf.Max(0, Health - taken);
            Visual.Flinch();
            Session.OnPlayerHurt(taken);
            if (Health <= 0) Session.Die();
        }
    }
}
