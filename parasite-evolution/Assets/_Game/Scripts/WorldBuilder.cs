using UnityEngine;

namespace ParasiteEvolution
{
    public static class Visuals
    {
        public static Material Material(Color color)
        {
            var template = Resources.Load<Material>("PrototypeMaterial");
            var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var result = template != null ? new Material(template) : new Material(shader);
            result.color = color;
            if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", color);
            return result;
        }
        // Emissive variant. The template is created by the editor with _EMISSION enabled, so the
        // keyword variant is kept in player builds instead of being stripped.
        public static Material Glow(Color color, float intensity)
        {
            var template = Resources.Load<Material>("PrototypeGlow");
            var result = template != null ? new Material(template) : Material(color);
            result.color = color;
            if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", color);
            if (result.HasProperty("_EmissionColor"))
            {
                result.EnableKeyword("_EMISSION");
                result.SetColor("_EmissionColor", color * intensity);
            }
            return result;
        }
        public static GameObject Shape(PrimitiveType type, Transform parent, Vector3 pos,
            Vector3 scale, Material material, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) { go.GetComponent<Collider>().enabled=false; Object.Destroy(go.GetComponent<Collider>()); }
            else go.layer = 8;
            return go;
        }
        public static LineRenderer Line(Transform parent, Material material, float width)
        {
            var line = new GameObject("Line").AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false); line.sharedMaterial = material;
            line.startWidth = width; line.endWidth = width; line.useWorldSpace = true;
            return line;
        }
    }

    public sealed class WorldBuilder
    {
        public static readonly Vector3 SafePoint = new Vector3(-22, 0, -10);
        public const float SafeRadius = 5;
        public const int WallMask = 1 << 8;
        public const int EnvironmentMask = (1 << 8) | (1 << 11);
        public readonly Transform Root;
        public WorldBuilder(GameSession session)
        {
            Root = new GameObject("Laboratory").transform;
            var wall = Visuals.Material(new Color(.13f, .19f, .25f));
            var edge = Visuals.Material(new Color(.28f, .55f, .60f));
            var floors = new[] { new Color(.26f,.34f,.38f), new Color(.31f,.31f,.39f), new Color(.36f,.27f,.32f) };
            for (var zone = 0; zone < 3; zone++)
            {
                var x = -23 + zone * 23;
                var floor=Visuals.Shape(PrimitiveType.Cube, Root, new Vector3(x,-.3f,0), new Vector3(23,.6f,44),
                    Visuals.Material(floors[zone]), true);
                floor.layer=11;
                for (var z = -20; z <= 20; z += 4)
                    Visuals.Shape(PrimitiveType.Cube, Root, new Vector3(x,.006f,z), new Vector3(22,.012f,.035f), edge);
            }
            foreach (var x in new[] { -35f, 35f })
                Visuals.Shape(PrimitiveType.Cube, Root, new Vector3(x,1,0), new Vector3(1,2,46), wall, true);
            foreach (var z in new[] { -23f, 23f })
                Visuals.Shape(PrimitiveType.Cube, Root, new Vector3(0,1,z), new Vector3(71,2,1), wall, true);
            // Both openings in each partition are traversable from the start.
            foreach (var x in new[] { -11.5f, 11.5f })
                foreach (var span in new[] { new Vector2(-15.5f,13), new Vector2(1.5f,9), new Vector2(17,10) })
                    Visuals.Shape(PrimitiveType.Cube, Root, new Vector3(x,.7f,span.x),
                        new Vector3(.65f,1.4f,span.y), wall, true);
            var safe = Visuals.Material(new Color(.12f,.62f,.44f));
            Visuals.Shape(PrimitiveType.Cylinder, Root, SafePoint + Vector3.up*.025f,
                new Vector3(SafeRadius*2,.025f,SafeRadius*2), safe);
            Visuals.Shape(PrimitiveType.Cylinder, Root, SafePoint + new Vector3(-1.8f,.6f,-1.2f),
                new Vector3(1.6f,.6f,1.6f), wall);
            Visuals.Shape(PrimitiveType.Sphere, Root, SafePoint + new Vector3(-1.8f,1.3f,-1.2f),
                Vector3.one*.8f, safe);
            // Cover and pillars: break sight lines, give the Warden's charge something to crash into.
            var crate = Visuals.Material(new Color(.22f,.27f,.31f));
            foreach (var block in Obstacles)
                Visuals.Shape(PrimitiveType.Cube, Root, new Vector3(block.x, block.w*.5f, block.y),
                    new Vector3(block.z, block.w, block.z), crate, true);
            for (var zone = 0; zone < 3; zone++)
                for (var n = 0; n < 7; n++)
                {
                    var x = -24 + zone*23 + (n%3)*3;
                    var z = zone == 0 ? 4 + (n/3)*4 : -13 + (n/3)*10;
                    var kind = zone == 0 ? EnemyKind.Drone : zone == 2 && n % 3 == 1 ? EnemyKind.Zapper : EnemyKind.Guard;
                    var enemy = new GameObject(kind+"-"+zone+"-"+n).AddComponent<EnemyActor>();
                    enemy.Initialize(session, new Vector3(x,0,z), zone, kind);
                }
            var boss = new GameObject("Warden").AddComponent<EnemyActor>();
            boss.Initialize(session, new Vector3(27,0,15), 2, EnemyKind.Warden);
            var sun = new GameObject("Light").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.35f;
            sun.transform.rotation = Quaternion.Euler(55,-35,0); sun.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(.48f,.55f,.66f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.17f,.22f,.28f);
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 80;
            Physics.SyncTransforms();
        }
        // x, z, footprint, height. Kept clear of the base, spawn points and partition openings.
        public static readonly Vector4[] Obstacles = {
            new Vector4(-30,14,2,1.6f), new Vector4(-15.5f,-17,2.4f,1.2f), new Vector4(-31,-19,1.6f,2),
            new Vector4(-6,0,2.2f,1.4f), new Vector4(8,13,2,2), new Vector4(-5,16,1.6f,1.2f), new Vector4(7.5f,-8,1.8f,1.6f),
            new Vector4(16,4,2,1.8f), new Vector4(31,-6,2.2f,1.4f), new Vector4(18,-18,1.8f,2), new Vector4(21.5f,18,1.6f,2.2f),
            new Vector4(32.5f,12,1.6f,2) };
        public static int ZoneAt(Vector3 p) => p.x < -11.5f ? 0 : p.x < 11.5f ? 1 : 2;
        public static bool IsSafe(Vector3 p) => Vector3.Distance(new Vector3(p.x,0,p.z),SafePoint) <= SafeRadius;
        public static bool ClearLine(Vector3 a, Vector3 b)
        {
            a.y = .65f; b.y = .65f;
            return !Physics.Linecast(a,b,EnvironmentMask,QueryTriggerInteraction.Ignore);
        }
    }
}
