using UnityEngine;
using UnityEngine.EventSystems;

namespace WUI
{
    public class ScrollBarHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public WScrollList scrollList;
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }
        private RectTransform _rectTransform;
        public void OnBeginDrag(PointerEventData eventData)
        {
            scrollList?.OnScrollBarBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            scrollList?.OnScrollBarDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            scrollList?.OnScrollBarEndDrag(eventData);
        }
    }
}