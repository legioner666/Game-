using UnityEngine;
using UnityEngine.EventSystems;

namespace ParasiteEvolution
{
    public sealed class VirtualJoystick : MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        public Vector2 Value { get; private set; }
        public RectTransform Knob;
        int pointer=int.MinValue;
        RectTransform Area => (RectTransform)transform;
        public void OnPointerDown(PointerEventData data)
        {
            if(pointer!=int.MinValue)return;
            pointer=data.pointerId;OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if(data.pointerId!=pointer)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(Area,data.position,data.pressEventCamera,out var local))
            {
                Value=Vector2.ClampMagnitude(local/(Area.rect.width*.36f),1);
                if(Knob!=null)Knob.anchoredPosition=Value*Area.rect.width*.28f;
            }
        }
        public void OnPointerUp(PointerEventData data){if(data.pointerId==pointer)ResetInput();}
        public void ResetInput(){pointer=int.MinValue;Value=Vector2.zero;if(Knob!=null)Knob.anchoredPosition=Vector2.zero;}
        void OnDisable(){ResetInput();}
    }
}
