using UnityEngine;

namespace WUI
{
    public abstract class PageLineItem : MonoBehaviour
    {
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
        public abstract IPageListItem GetData();
        public abstract void SetData(IPageListItem item);
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public virtual void Highlight(bool highlight)
        {
            
        }
        public virtual float GetHeight()
        {
            return RectTransform.rect.height;
        }
        public virtual float GetHeight(IPageListItem targetData)
        {
            return RectTransform.rect.height;
        }
    }
    public abstract class ScrollLineItem : MonoBehaviour
    {
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
        public abstract void SetData(IPageListItem item);
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public virtual void Highlight(bool highlight)
        {
            
        }
    }
}