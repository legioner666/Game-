// Hand-written compile-only stand-ins for the subset of the Unity API used by the runtime scripts.
// Written for this project from public API signatures; no Unity binaries or source are included.
// Nothing here executes: every member returns a default value. A successful build against these
// stubs proves the C# itself is well-formed and type-consistent with these signatures. It does NOT
// prove compatibility with real Unity 6000.3 assemblies; only a Unity import can prove that.
#pragma warning disable CS0067, CS0649, CS0414, CS1591, CS0660, CS0661
using System;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object obj) { }
        public static void Destroy(Object obj, float t) { }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static implicit operator bool(Object exists) => exists is not null;
        public static bool operator ==(Object x, Object y) => ReferenceEquals(x, y);
        public static bool operator !=(Object x, Object y) => !ReferenceEquals(x, y);
    }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform => null;
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf => false;
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component => null;
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public static GameObject CreatePrimitive(PrimitiveType type) => null;
    }

    public class Component : Object
    {
        public Transform transform => null;
        public GameObject gameObject => null;
        public string tag { get; set; }
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default;
    }

    public class Behaviour : Component { public bool enabled { get; set; } }
    public class MonoBehaviour : Behaviour { }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 forward { get; set; }
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public Vector3 TransformPoint(Vector3 position) => default;
        public Vector3 InverseTransformPoint(Vector3 position) => default;
        public Vector3 TransformVector(Vector3 vector) => default;
        public Vector3 InverseTransformDirection(Vector3 direction) => default;
        public void SetAsFirstSibling() { }
        public void SetSiblingIndex(int index) { }
    }

    public sealed class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect => default;
    }

    public struct Vector2 : IEquatable<Vector2>
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default;
        public static Vector2 one => default;
        public float magnitude => 0;
        public float sqrMagnitude => 0;
        public Vector2 normalized => default;
        public static Vector2 ClampMagnitude(Vector2 vector, float maxLength) => default;
        public static Vector2 operator +(Vector2 a, Vector2 b) => default;
        public static Vector2 operator -(Vector2 a, Vector2 b) => default;
        public static Vector2 operator -(Vector2 a) => default;
        public static Vector2 operator *(Vector2 a, float d) => default;
        public static Vector2 operator *(float d, Vector2 a) => default;
        public static Vector2 operator /(Vector2 a, float d) => default;
        public static bool operator ==(Vector2 a, Vector2 b) => false;
        public static bool operator !=(Vector2 a, Vector2 b) => true;
        public static implicit operator Vector3(Vector2 v) => default;
        public static implicit operator Vector2(Vector3 v) => default;
        public bool Equals(Vector2 other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
    }

    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public Vector2Int(int x, int y) { }
        public static bool operator ==(Vector2Int a, Vector2Int b) => false;
        public static bool operator !=(Vector2Int a, Vector2Int b) => true;
        public bool Equals(Vector2Int other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
    }

    public struct Vector3 : IEquatable<Vector3>
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero => default;
        public static Vector3 one => default;
        public static Vector3 up => default;
        public static Vector3 down => default;
        public static Vector3 forward => default;
        public static Vector3 right => default;
        public float magnitude => 0;
        public float sqrMagnitude => 0;
        public Vector3 normalized => default;
        public void Normalize() { }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => default;
        public static float Distance(Vector3 a, Vector3 b) => 0;
        public static float Dot(Vector3 lhs, Vector3 rhs) => 0;
        public static Vector3 Cross(Vector3 lhs, Vector3 rhs) => default;
        public static Vector3 Min(Vector3 lhs, Vector3 rhs) => default;
        public static Vector3 Max(Vector3 lhs, Vector3 rhs) => default;
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta) => default;
        public static Vector3 ClampMagnitude(Vector3 vector, float maxLength) => default;
        public static Vector3 operator +(Vector3 a, Vector3 b) => default;
        public static Vector3 operator -(Vector3 a, Vector3 b) => default;
        public static Vector3 operator -(Vector3 a) => default;
        public static Vector3 operator *(Vector3 a, float d) => default;
        public static Vector3 operator *(float d, Vector3 a) => default;
        public static Vector3 operator /(Vector3 a, float d) => default;
        public static bool operator ==(Vector3 a, Vector3 b) => false;
        public static bool operator !=(Vector3 a, Vector3 b) => true;
        public bool Equals(Vector3 other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Quaternion
    {
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => default;
        public static Quaternion LookRotation(Vector3 forward) => default;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => default;
        public static Vector3 operator *(Quaternion rotation, Vector3 point) => default;
        public static Quaternion operator *(Quaternion lhs, Quaternion rhs) => default;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public static Color white => default;
        public static Color Lerp(Color a, Color b, float t) => default;
        public static Color operator *(Color a, float b) => default;
        public static Color operator *(float b, Color a) => default;
    }

    public struct Rect : IEquatable<Rect>
    {
        public float xMin => 0;
        public float xMax => 0;
        public float yMin => 0;
        public float yMax => 0;
        public float width => 0;
        public float height => 0;
        public static bool operator ==(Rect a, Rect b) => false;
        public static bool operator !=(Rect a, Rect b) => true;
        public bool Equals(Rect other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
    }

    public struct Bounds { public Bounds(Vector3 center, Vector3 size) { } }

    public static class Mathf
    {
        public const float PI = 3.14159274f;
        public const float Deg2Rad = PI / 180f;
        public static float Sin(float f) => 0;
        public static float Cos(float f) => 0;
        public static float Exp(float power) => 0;
        public static float Sqrt(float f) => 0;
        public static float Abs(float f) => 0;
        public static int Abs(int value) => 0;
        public static float Min(float a, float b) => 0;
        public static int Min(int a, int b) => 0;
        public static float Max(float a, float b) => 0;
        public static int Max(int a, int b) => 0;
        public static float Clamp(float value, float min, float max) => 0;
        public static int Clamp(int value, int min, int max) => 0;
        public static float Clamp01(float value) => 0;
        public static float Lerp(float a, float b, float t) => 0;
        public static float MoveTowards(float current, float target, float maxDelta) => 0;
        public static float Repeat(float t, float length) => 0;
        public static float PerlinNoise(float x, float y) => 0;
        public static int CeilToInt(float f) => 0;
    }

    public static class Random
    {
        public static float value => 0;
        public static float Range(float minInclusive, float maxInclusive) => 0;
        public static int Range(int minInclusive, int maxExclusive) => 0;
    }

    public static class Time
    {
        public static float time => 0;
        public static float deltaTime => 0;
        public static float unscaledTime => 0;
        public static float unscaledDeltaTime => 0;
        public static float timeScale { get; set; }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogException(Exception exception) { }
    }

    public static class Application
    {
        public static int targetFrameRate { get; set; }
        public static string persistentDataPath => "";
        public static string dataPath => "";
        public static bool isBatchMode => false;
    }

    public enum ScreenOrientation { Portrait = 1, PortraitUpsideDown = 2, LandscapeLeft = 3, LandscapeRight = 4, AutoRotation = 5 }

    public sealed class SleepTimeout { public const int NeverSleep = -1; public const int SystemSetting = -2; }

    public static class Screen
    {
        public static int width => 0;
        public static int height => 0;
        public static Rect safeArea => default;
        public static ScreenOrientation orientation { get; set; }
        public static bool autorotateToPortrait { get; set; }
        public static bool autorotateToPortraitUpsideDown { get; set; }
        public static bool autorotateToLandscapeLeft { get; set; }
        public static bool autorotateToLandscapeRight { get; set; }
        public static int sleepTimeout { get; set; }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => null;
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
    }

    public sealed class Shader : Object { public static Shader Find(string name) => null; }

    public enum MaterialGlobalIlluminationFlags { None = 0, RealtimeEmissive = 1, BakedEmissive = 2, EmissiveIsBlack = 4 }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }
        public Color color { get; set; }
        public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        public bool HasProperty(string name) => false;
        public void SetColor(string name, Color value) { }
        public void SetFloat(string name, float value) { }
        public void EnableKeyword(string keyword) { }
    }

    public sealed class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public Vector3[] normals { get; set; }
        public int[] triangles { get; set; }
        public Bounds bounds { get; set; }
        public int subMeshCount { get; set; }
        public void MarkDynamic() { }
        public void SetTriangles(int[] triangles, int submesh) { }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
    }

    public sealed class MeshFilter : Component { public Mesh sharedMesh { get; set; } }

    public class Renderer : Component
    {
        public bool enabled { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] sharedMaterials { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
    }

    public sealed class MeshRenderer : Renderer { }

    public sealed class LineRenderer : Renderer
    {
        public int positionCount { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public bool useWorldSpace { get; set; }
        public void SetPosition(int index, Vector3 position) { }
    }

    public class Collider : Component { public bool enabled { get; set; } }

    [Flags] public enum CollisionFlags { None = 0, Sides = 1, Above = 2, Below = 4 }

    public sealed class CharacterController : Collider
    {
        public CollisionFlags Move(Vector3 motion) => CollisionFlags.None;
        public bool isGrounded => false;
        public float radius { get; set; }
        public float height { get; set; }
        public Vector3 center { get; set; }
        public float skinWidth { get; set; }
        public float stepOffset { get; set; }
        public float slopeLimit { get; set; }
    }

    public enum QueryTriggerInteraction { UseGlobal = 0, Ignore = 1, Collide = 2 }
    public struct RaycastHit { public float distance => 0; }

    public static class Physics
    {
        public const int DefaultRaycastLayers = -5;
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance = float.PositiveInfinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => false;
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance = float.PositiveInfinity,
            int layerMask = DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        { hitInfo = default; return false; }
        public static bool Linecast(Vector3 start, Vector3 end, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => false;
        public static bool CheckSphere(Vector3 position, float radius, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => false;
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => false;
        public static void SyncTransforms() { }
    }

    public enum PrimitiveType { Sphere = 0, Capsule = 1, Cylinder = 2, Cube = 3, Plane = 4, Quad = 5 }
    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }

    public sealed class Camera : Behaviour
    {
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public Color backgroundColor { get; set; }
        public CameraClearFlags clearFlags { get; set; }
    }

    public enum LightType { Spot = 0, Directional = 1, Point = 2 }
    public enum LightShadows { None = 0, Hard = 1, Soft = 2 }

    public sealed class Light : Behaviour
    {
        public LightType type { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
    }

    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    public static class RenderSettings
    {
        public static Color ambientLight { get; set; }
        public static bool fog { get; set; }
        public static Color fogColor { get; set; }
        public static FogMode fogMode { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
    }

    public static class JsonUtility
    {
        public static string ToJson(object obj) => "";
        public static string ToJson(object obj, bool prettyPrint) => "";
        public static T FromJson<T>(string json) => default;
    }

    public sealed class Font : Object { }

    public enum RuntimeInitializeLoadType { AfterSceneLoad = 0, BeforeSceneLoad = 1 }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint)
        { localPoint = default; return false; }
    }

    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap = 0, Overflow = 1 }
    public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }

    public sealed class Canvas : Behaviour { public RenderMode renderMode { get; set; } }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off = 0, On = 1, TwoSided = 2, ShadowsOnly = 3 }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public class UnityEvent { public void AddListener(UnityAction call) { } }
}

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class BaseInputModule : UIBehaviour { }
    public class EventSystem : UIBehaviour { public static EventSystem current { get; set; } }
    public class PointerEventData
    {
        public int pointerId { get; set; }
        public Vector2 position { get; set; }
        public Camera pressEventCamera => null;
    }
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData eventData); }
}

namespace UnityEngine.UI
{
    using UnityEngine.EventSystems;

    public abstract class Graphic : UIBehaviour
    {
        public virtual Color color { get; set; }
        public virtual bool raycastTarget { get; set; }
    }
    public abstract class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic { }
    public class Text : MaskableGraphic
    {
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public virtual string text { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
    }
    public class Selectable : UIBehaviour
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
    }
    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEngine.Events.UnityEvent { }
        public ButtonClickedEvent onClick { get; set; }
    }
    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
    }
    public class GraphicRaycaster : UIBehaviour { }
}

namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl
    {
        public bool isPressed => false;
        public bool wasPressedThisFrame => false;
    }
    public class KeyControl : ButtonControl { }
}

namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;

    public class Keyboard
    {
        public static Keyboard current => null;
        public KeyControl digit1Key => null;
        public KeyControl digit2Key => null;
        public KeyControl digit3Key => null;
        public KeyControl spaceKey => null;
        public KeyControl escapeKey => null;
        public KeyControl aKey => null;
        public KeyControl dKey => null;
        public KeyControl wKey => null;
        public KeyControl sKey => null;
    }
}

namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { }
}
