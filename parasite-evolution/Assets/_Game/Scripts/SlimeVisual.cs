using System.Collections.Generic;
using UnityEngine;

namespace ParasiteEvolution
{
    // Procedural, original look of the organism: an asymmetric heavy mass of dark slime with
    // spider-like retractile limbs of different length and thickness ending in sharp bone tips.
    // Locomotion reads as anchor -> contraction: limbs reach ahead, plant, and the mass is hauled
    // after them. All meshes are generated here; no external model, texture or animation is used.
    // Per-frame work reuses preallocated arrays (no managed allocations in LateUpdate).
    public sealed class SlimeVisual : MonoBehaviour
    {
        const int Rings = 14;
        const int Sides = 7;
        const int RingVerts = Sides + 1;
        const int TipRings = 3;

        sealed class Limb
        {
            public Mesh mesh;
            public MeshRenderer renderer;
            public Vector3[] vertices;
            public Vector3[] normals;
            public LimbCycle cycle;
            public float angle;       // rest direction in body space, degrees from forward
            public float length;      // reach while walking
            public float radius;      // base thickness
            public Vector3 rootLocal;
            public Vector3 anchor;
            public Vector3 tip;       // last drawn tip, so new reaches start from the body
            public Vector3 stabDir;
            public float stabLength;
        }

        // angle, length, radius, gait group. Uneven on purpose: four limbs on the left, three on the
        // right, the longest pair in front; every limb is longer than the torso is wide.
        static readonly float[,] SupportLayout = {
            { -34f, 2.65f, .17f, 0 }, { 28f, 2.95f, .19f, 1 }, { -82f, 2.2f, .15f, 1 }, { 76f, 2.45f, .16f, 0 },
            { -124f, 2.0f, .14f, 0 }, { 118f, 2.3f, .15f, 1 }, { -158f, 1.75f, .12f, 1 } };

        readonly List<Limb> supports = new List<Limb>();
        readonly List<Limb> working = new List<Limb>();
        // Positions the session wants grabbed this frame (capture channels / pulled enemies).
        public readonly List<Vector3> WorkTargets = new List<Vector3>();
        GaitPlanner gait;
        float[] strain;

        Mesh body;
        Transform bodyTransform;
        Vector3[] baseVertices, bodyVertices;
        Material skin, bone, glow;
        Transform[] nuclei;
        Vector3[] nucleiBase;
        Transform limbRoot;
        Vector3 previous;
        float motion, strikeTimer, hurtTimer, squash;
        bool dashing;
        Vector3 dashGoal, dashDir;

        public float Motion => motion;

        void Awake()
        {
            skin = Visuals.Material(new Color(.05f, .032f, .075f));
            if (skin.HasProperty("_Smoothness")) skin.SetFloat("_Smoothness", .74f);
            bone = Visuals.Material(new Color(.86f, .8f, .7f));
            glow = Visuals.Glow(new Color(.95f, .12f, .38f), 2.2f);
            BuildBody();
            BuildNuclei();
            limbRoot = new GameObject("Slime limbs").transform;
            var groups = new int[SupportLayout.GetLength(0)];
            for (var i = 0; i < groups.Length; i++)
            {
                var limb = CreateLimb("Support limb " + i);
                limb.angle = SupportLayout[i, 0];
                limb.length = SupportLayout[i, 1];
                limb.radius = SupportLayout[i, 2];
                groups[i] = (int)SupportLayout[i, 3];
                var a = limb.angle * Mathf.Deg2Rad;
                // Roots sit on the flank of the mass, slightly irregular.
                limb.rootLocal = new Vector3(Mathf.Sin(a) * .62f, .34f + .06f * Mathf.Cos(a * 3), Mathf.Cos(a) * .5f);
                supports.Add(limb);
            }
            gait = new GaitPlanner(groups);
            for (var i = 0; i < supports.Count; i++)
            {
                supports[i].cycle = gait.Limbs[i];
                supports[i].cycle.ExtendSpeed = 6.5f + i * .3f;
            }
            strain = new float[supports.Count];
            for (var i = 0; i < 3; i++)
            {
                var limb = CreateLimb("Grasping tendril " + i);
                limb.cycle = new LimbCycle { ExtendSpeed = 10f, RetractSpeed = 7f, PullSpeed = 5f };
                limb.radius = .12f - i * .015f;
                limb.length = 9f;
                limb.rootLocal = new Vector3((i - 1) * .24f, .62f - Mathf.Abs(i - 1) * .08f, .28f);
                working.Add(limb);
            }
            previous = transform.position;
            ResetContacts();
        }

        void BuildBody()
        {
            var blob = Visuals.Shape(PrimitiveType.Sphere, transform, new Vector3(0, .36f, 0), Vector3.one, skin);
            blob.name = "Slime mass";
            bodyTransform = blob.transform;
            var filter = blob.GetComponent<MeshFilter>();
            body = Instantiate(filter.sharedMesh);
            body.name = "Asymmetric slime mass";
            filter.sharedMesh = body;
            body.MarkDynamic();
            baseVertices = body.vertices;
            bodyVertices = new Vector3[baseVertices.Length];
            for (var i = 0; i < baseVertices.Length; i++) baseVertices[i] = Surface(baseVertices[i].normalized);
            body.vertices = baseVertices;
            body.RecalculateNormals();
            body.RecalculateBounds();
        }

        void BuildNuclei()
        {
            // Directions on the upper surface; nuclei sit half-embedded so they read as organs under the skin.
            var spots = new[] { new Vector3(-.3f, .9f, .25f), new Vector3(.25f, .75f, .55f), new Vector3(-.55f, .7f, -.3f) };
            var sizes = new[] { .17f, .11f, .08f };
            nuclei = new Transform[spots.Length];
            nucleiBase = new Vector3[spots.Length];
            for (var i = 0; i < spots.Length; i++)
            {
                var surface = bodyTransform.localPosition + Surface(spots[i].normalized) * .97f;
                var node = Visuals.Shape(PrimitiveType.Sphere, transform, surface, Vector3.one * sizes[i], glow);
                node.name = "Nucleus";
                nuclei[i] = node.transform;
                nucleiBase[i] = Vector3.one * sizes[i];
            }
        }

        static readonly Vector3 Hump = new Vector3(-.42f, .78f, -.46f).normalized;
        static readonly Vector3 Lobe = new Vector3(.62f, .22f, .5f).normalized;
        static readonly Vector3 Knot = new Vector3(-.18f, .42f, .88f).normalized;
        static readonly Vector3 Sag = new Vector3(.7f, -.3f, -.62f).normalized;

        // Rest shape of the mass in body space for a unit direction: a dorsal hump pushed back and
        // to the left, a heavier right flank, a forward knot, and a flattened skirt on the floor.
        static Vector3 Surface(Vector3 d)
        {
            var r = .5f * (1 + .42f * Bump(d, Hump, .5f) + .24f * Bump(d, Lobe, .45f) + .14f * Bump(d, Knot, .3f)
                + .18f * Bump(d, Sag, .55f) + .05f * Mathf.Sin(d.x * 7 + d.z * 4) * Mathf.Cos(d.y * 5));
            var p = new Vector3(d.x * r * 1.7f, d.y * r * 1.0f, d.z * r * 1.42f);
            if (p.y < -.17f)
            {
                var spread = 1 + (-.17f - p.y) * .9f;
                p = new Vector3(p.x * spread, -.17f - (-.17f - p.y) * .25f, p.z * spread);
            }
            return p;
        }

        static float Bump(Vector3 d, Vector3 axis, float width)
        {
            var c = Vector3.Dot(d, axis);
            var t = Mathf.Clamp01((c - (1 - width)) / width);
            return t * t * (3 - 2 * t);
        }

        Limb CreateLimb(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(limbRoot, false);
            var limb = new Limb {
                mesh = new Mesh { name = "Procedural retractile limb" },
                vertices = new Vector3[Rings * RingVerts + 1],
                normals = new Vector3[Rings * RingVerts + 1]
            };
            limb.mesh.MarkDynamic();
            limb.mesh.subMeshCount = 2;
            limb.mesh.vertices = limb.vertices;
            limb.mesh.normals = limb.normals;
            // Submesh 0: flesh of the limb. Submesh 1: last rings and the point, in bone colour.
            var flesh = new List<int>();
            var tip = new List<int>();
            for (var ring = 0; ring < Rings - 1; ring++)
                for (var side = 0; side < Sides; side++)
                {
                    var a = ring * RingVerts + side;
                    var b = a + RingVerts;
                    var list = ring >= Rings - 1 - TipRings ? tip : flesh;
                    list.Add(a); list.Add(b); list.Add(a + 1);
                    list.Add(a + 1); list.Add(b); list.Add(b + 1);
                }
            var point = Rings * RingVerts;
            for (var side = 0; side < Sides; side++)
            {
                var a = (Rings - 1) * RingVerts + side;
                tip.Add(a); tip.Add(point); tip.Add(a + 1);
            }
            limb.mesh.SetTriangles(flesh.ToArray(), 0);
            limb.mesh.SetTriangles(tip.ToArray(), 1);
            go.AddComponent<MeshFilter>().sharedMesh = limb.mesh;
            limb.renderer = go.AddComponent<MeshRenderer>();
            limb.renderer.sharedMaterials = new[] { skin, bone };
            limb.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            limb.cycle = new LimbCycle();
            return limb;
        }

        // Anchor -> contraction dash. The grasping tendrils shoot to the destination and haul the mass.
        public void BeginDash(Vector3 goal)
        {
            dashing = true;
            dashGoal = goal;
            dashDir = goal - transform.position;
            dashDir.y = 0;
            dashDir = dashDir.sqrMagnitude > .0001f ? dashDir.normalized : transform.forward;
            gait.Suspended = true;
            gait.ReleaseAll();
            var side = Vector3.Cross(Vector3.up, dashDir);
            for (var i = 0; i < working.Count; i++)
            {
                var limb = working[i];
                limb.anchor = goal + side * ((i - 1) * .7f) + dashDir * .3f;
                limb.anchor.y = .05f;
                limb.cycle.Snap();
                limb.cycle.ExtendSpeed = 14f;
                limb.cycle.Reach();
            }
        }

        public void PullDash()
        {
            foreach (var limb in working) limb.cycle.Contract();
        }

        public void EndDash()
        {
            if (!dashing) return;
            dashing = false;
            gait.Suspended = false;
            foreach (var limb in working)
            {
                limb.cycle.ExtendSpeed = 10f;
                limb.cycle.Release();
            }
        }

        // Radial stab of every support limb, used by the heavy strike ability.
        public void BeginStrike(float radius)
        {
            strikeTimer = .32f;
            gait.Suspended = true;
            for (var i = 0; i < supports.Count; i++)
            {
                var limb = supports[i];
                limb.stabDir = Quaternion.Euler(0, limb.angle, 0) * transform.forward;
                limb.stabLength = Mathf.Max(limb.length, radius * (.82f + .05f * (i % 3)));
                limb.anchor = transform.position + limb.stabDir * limb.stabLength;
                limb.anchor.y = .25f;
                limb.cycle.Snap();
                limb.cycle.ExtendSpeed = 16f;
                limb.cycle.Reach();
            }
        }

        public void Flinch() { hurtTimer = .25f; }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            if (dt <= 0) return;
            var position = transform.position;
            var velocity = (position - previous) / dt;
            previous = position;
            velocity.y = 0;
            var speed = velocity.magnitude;
            motion = Mathf.Lerp(motion, Mathf.Min(1, speed / 4.5f), 1 - Mathf.Exp(-10 * dt));
            var heading = speed > .1f ? velocity / speed : transform.forward;
            UpdateStrike(dt);
            UpdateSupports(dt, position, heading);
            UpdateWorking(dt);
            DeformBody(dt, heading);
        }

        void UpdateStrike(float dt)
        {
            if (strikeTimer <= 0) return;
            strikeTimer -= dt;
            if (strikeTimer > 0) return;
            foreach (var limb in supports) { limb.cycle.ExtendSpeed = 6.5f; limb.stabLength = 0; limb.cycle.Release(); }
            if (!dashing) gait.Suspended = false;
        }

        void UpdateSupports(float dt, Vector3 position, Vector3 heading)
        {
            for (var i = 0; i < supports.Count; i++)
            {
                var limb = supports[i];
                strain[i] = limb.cycle.Planted ? Vector3.Distance(Flat(limb.anchor), Flat(Foothold(limb, position, heading))) / (limb.length * .5f) : 0;
            }
            gait.Tick(dt, motion, strain);
            for (var i = 0; i < supports.Count; i++)
            {
                var limb = supports[i];
                if (limb.cycle.NeedsAnchor && strikeTimer <= 0)
                {
                    limb.anchor = Foothold(limb, position, heading) + heading * (limb.length * .35f * motion);
                    limb.anchor.y = .04f;
                }
                var root = transform.TransformPoint(limb.rootLocal);
                if (strikeTimer > 0)
                {
                    // The stab follows the body while the ability lasts.
                    limb.anchor = position + limb.stabDir * limb.stabLength;
                    limb.anchor.y = .3f;
                }
                Draw(limb, root, dt, false);
            }
        }

        Vector3 Foothold(Limb limb, Vector3 position, Vector3 heading)
        {
            var rest = Quaternion.Euler(0, limb.angle, 0) * transform.forward * (limb.length * .66f);
            return new Vector3(position.x + rest.x, .04f, position.z + rest.z) + heading * (limb.length * .12f * motion);
        }

        void UpdateWorking(float dt)
        {
            for (var i = 0; i < working.Count; i++)
            {
                var limb = working[i];
                if (!dashing)
                {
                    if (i < WorkTargets.Count)
                    {
                        if (limb.cycle.Phase == LimbPhase.Retracted || limb.cycle.Phase == LimbPhase.Retracting) limb.cycle.Reach();
                        limb.anchor = WorkTargets[i];
                        if (limb.cycle.Phase == LimbPhase.Anchored) limb.cycle.Contract();
                    }
                    else limb.cycle.Release();
                }
                limb.cycle.Tick(dt);
                // Grasp follows its target: the tendril thickens in pulses while it drains.
                Draw(limb, transform.TransformPoint(limb.rootLocal), dt, true);
            }
        }

        void Draw(Limb limb, Vector3 root, float dt, bool grasp)
        {
            var e = limb.cycle.Extension;
            if (e <= .001f)
            {
                if (limb.renderer.enabled) limb.renderer.enabled = false;
                limb.tip = root;
                return;
            }
            if (!limb.renderer.enabled) limb.renderer.enabled = true;
            var end = Vector3.Lerp(root, limb.anchor, e);
            limb.tip = end;
            var span = end - root;
            var distance = span.magnitude;
            var reach = grasp ? distance + .3f : Mathf.Max(limb.length, limb.stabLength) * e;
            // Spider-like arch: the knee rises when the tip is closer than the limb's own length.
            var half = Mathf.Max(reach, distance) * .5f;
            var lift = Mathf.Sqrt(Mathf.Max(0, half * half - distance * distance * .25f)) * (grasp ? .35f : .85f) + .12f * e;
            var knee = (root + end) * .5f + Vector3.up * lift;
            var control = knee * 2 - (root + end) * .5f;
            var pull = limb.cycle.Pull;
            var pulse = grasp && pull > 0 ? 1 + .25f * pull * Mathf.Sin(Time.time * 18) : 1;
            var thickness = limb.radius * Mathf.Min(1, e * 3f) * (1 + .35f * pull) * pulse;
            var min = root;
            var max = root;
            var up = Vector3.up;
            for (var ring = 0; ring < Rings; ring++)
            {
                var u = ring / (float)Rings;
                var a = 1 - u;
                var center = a * a * root + 2 * a * u * control + u * u * end;
                var tangent = 2 * a * (control - root) + 2 * u * (end - control);
                if (tangent.sqrMagnitude < 1e-6f) tangent = span.sqrMagnitude > 1e-6f ? span : Vector3.forward;
                tangent.Normalize();
                var right = Vector3.Cross(tangent, up);
                if (right.sqrMagnitude < 1e-4f) right = Vector3.Cross(tangent, Vector3.right);
                right.Normalize();
                var normalUp = Vector3.Cross(right, tangent);
                // Thick shoulder, long taper, slight swelling before the bone tip.
                var r = thickness * (1.15f - .95f * u + .18f * Mathf.Sin(u * Mathf.PI) - (u > .8f ? (u - .8f) * 1.6f : 0));
                r = Mathf.Max(r, thickness * .08f);
                for (var side = 0; side < RingVerts; side++)
                {
                    var angle = side / (float)Sides * Mathf.PI * 2;
                    var n = right * Mathf.Cos(angle) + normalUp * Mathf.Sin(angle);
                    var v = center + n * r;
                    var index = ring * RingVerts + side;
                    limb.vertices[index] = v;
                    limb.normals[index] = n;
                    min = Vector3.Min(min, v);
                    max = Vector3.Max(max, v);
                }
            }
            // Sharp point extends past the anchor along the final tangent.
            var lastTangent = (end - control).sqrMagnitude > 1e-6f ? (end - control).normalized : Vector3.forward;
            var point = end + lastTangent * (thickness * 2.6f + .08f);
            var last = Rings * RingVerts;
            limb.vertices[last] = point;
            limb.normals[last] = lastTangent;
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
            limb.mesh.vertices = limb.vertices;
            limb.mesh.normals = limb.normals;
            limb.mesh.bounds = new Bounds((min + max) * .5f, max - min + Vector3.one * .1f);
        }

        void DeformBody(float dt, Vector3 heading)
        {
            var time = Time.time;
            hurtTimer = Mathf.Max(0, hurtTimer - dt);
            var targetSquash = dashing ? .3f : strikeTimer > 0 ? -.18f : 0;
            squash = Mathf.Lerp(squash, targetSquash, 1 - Mathf.Exp(-14 * dt));
            var localHeading = transform.InverseTransformDirection(heading);
            localHeading.y = 0;
            if (localHeading.sqrMagnitude < 1e-4f) localHeading = Vector3.forward;
            localHeading.Normalize();
            var breathe = .035f * Mathf.Sin(time * 2.1f);
            var gaitPulse = .06f * motion * Mathf.Sin(time * 10.5f);
            var quiver = hurtTimer > 0 ? .06f * hurtTimer * 4 : 0;
            for (var i = 0; i < bodyVertices.Length; i++)
            {
                var p = baseVertices[i];
                var ripple = .022f * Mathf.Sin(time * 3.1f + p.x * 7 + p.z * 5) + quiver * Mathf.Sin(time * 45 + p.y * 20);
                var along = Vector3.Dot(p, localHeading);
                // Stretch toward the pull direction during a dash; flatten during a strike.
                var stretch = localHeading * (along * squash);
                var height = 1 + breathe + gaitPulse - squash * .45f;
                bodyVertices[i] = new Vector3(p.x * (1 + ripple) + stretch.x, p.y * height + ripple * .5f, p.z * (1 - ripple) + stretch.z);
            }
            body.vertices = bodyVertices;
            body.RecalculateNormals();
            for (var i = 0; i < nuclei.Length; i++)
            {
                var beat = 1 + .18f * Mathf.Sin(time * (2.6f + i * .9f) + i) + (hurtTimer > 0 ? .4f * hurtTimer : 0);
                nuclei[i].localScale = nucleiBase[i] * beat;
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        public void ResetContacts()
        {
            previous = transform.position;
            WorkTargets.Clear();
            dashing = false;
            strikeTimer = 0;
            if (gait != null) { gait.Suspended = false; gait.SnapAll(); }
            foreach (var limb in supports) { limb.anchor = transform.position; limb.tip = transform.position; Hide(limb); }
            foreach (var limb in working) { limb.cycle.Snap(); limb.anchor = transform.position; limb.tip = transform.position; Hide(limb); }
        }

        static void Hide(Limb limb)
        {
            if (limb.renderer != null) limb.renderer.enabled = false;
        }

        void OnDestroy()
        {
            if (body != null) Destroy(body);
            foreach (var limb in supports) if (limb.mesh != null) Destroy(limb.mesh);
            foreach (var limb in working) if (limb.mesh != null) Destroy(limb.mesh);
            if (limbRoot != null) Destroy(limbRoot.gameObject);
            if (skin != null) Destroy(skin);
            if (bone != null) Destroy(bone);
            if (glow != null) Destroy(glow);
        }
    }
}
