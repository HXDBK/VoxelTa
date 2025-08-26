using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace WUI
{
    public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler,IEndDragHandler
    {
        public RectTransform targetRect;
        private Canvas _canvas;
        private Vector2 _offset;
        public UnityEvent onEndDrag;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            _offset = targetRect.anchoredPosition - localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetRect.parent as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                targetRect.anchoredPosition = localPoint + _offset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            onEndDrag?.Invoke();
        }
    }
}