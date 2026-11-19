using UnityEngine;
using UnityEngine.EventSystems;

namespace ParasiteEvolution
{
    // Floating stick: the touch zone covers the lower-left part of the safe area; the base jumps to
    // where the thumb lands and returns to its resting spot on release. Only the first pointer is
    // tracked, so a second thumb on the ability buttons never steals movement.
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value { get; private set; }
        public RectTransform Base;
        public RectTransform Knob;
        // Resting spot measured from the lower-left corner of the touch zone.
        public Vector2 RestInset = new Vector2(150, 135);
        public float Range = 80;
        int pointer = int.MinValue;
        RectTransform Area => (RectTransform)transform;

        public void OnPointerDown(PointerEventData data)
        {
            if (pointer != int.MinValue) return;
            pointer = data.pointerId;
            if (Base != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(Area, data.position, data.pressEventCamera, out var local))
                Base.anchoredPosition = local;
            OnDrag(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != pointer || Base == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Base, data.position, data.pressEventCamera, out var local)) return;
            var raw = local / Range;
            // Small dead zone, then a linear response up to the rim.
            var magnitude = raw.magnitude;
            Value = magnitude < .12f ? Vector2.zero : raw / magnitude * Mathf.Clamp01((magnitude - .12f) / .88f);
            if (Knob != null) Knob.anchoredPosition = Vector2.ClampMagnitude(local, Range);
        }

        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) ResetInput(); }

        public void ResetInput()
        {
            pointer = int.MinValue; Value = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
            if (Base != null) Base.anchoredPosition = RestPosition;
        }

        // The canvas gets its size only after the first layout pass, so the resting spot is
        // re-applied while the stick is idle instead of being computed once at start-up.
        void LateUpdate()
        {
            if (pointer == int.MinValue && Base != null) Base.anchoredPosition = RestPosition;
        }

        Vector2 RestPosition => new Vector2(Area.rect.xMin + RestInset.x, Area.rect.yMin + RestInset.y);

        void OnDisable() { ResetInput(); }
    }
}
