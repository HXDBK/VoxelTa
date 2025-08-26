using System;
using DG.Tweening;
using Other;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WUI
{
    public class WButton : MonoBehaviour,IPointerEnterHandler,IPointerClickHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        public Color enterColor = Color.white;
        private Color _baseColor;
        private float _baseRotate;
        private Vector3 _baseScale;
        public float enterScale = 1.2f;
        public float enterRotate = 0;
        public Image targetImage;
        public TMP_Text targetText;
        [Header("鼠标进入事件")]
        public AudioClip enterAudio;
        public UnityEvent onPointerEnter;
        [Header("鼠标点击事件")]
        public AudioClip clickAudio;
        public UnityEvent onPointerClick;
        [Header("鼠标退出事件")]
        public AudioClip exitAudio;
        public UnityEvent onPointerExit;
        [Header("鼠标按下事件")]
        public UnityEvent onPointerDown;
        [Header("鼠标放开事件")]
        public UnityEvent onPointerUp;

        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                if (value) return;
            
                if (enterScale > 1 && _baseScale != Vector3.zero)
                {
                    transform.DOScale(_baseScale, 0.2f).SetEase(Ease.OutBack);
                }
                if (enterRotate != 0)
                {
                    transform.DORotate(new Vector3(0,0,_baseRotate), 0.2f).SetEase(Ease.OutBack);
                }
                if (enterColor != Color.white)
                {
                    targetImage.DOColor(_baseColor, 0.2f);
                }
                onPointerExit?.Invoke();
            }
        }

        [Header("选中")] 
        public WButtonGroup group;
        public bool selected;
        public Image selectedImage;
        public Color selectedColor = MyColor.Green;
        [SerializeField]private bool _interactable=true;

        public Image Image
        {
            get
            {
                if (!targetImage)
                {
                    targetImage = GetComponent<Image>();
                }
                return targetImage;
            }
        }

        public TMP_Text Text
        {
            get
            {
                if (!targetText)
                {
                    targetText = GetComponentInChildren<TMP_Text>();
                }
                return targetText;
            }
        }
        
        protected virtual void Awake()
        {
            if (!targetImage)
            {
                targetImage = GetComponent<Image>();
            }
            if (!targetText)
            {
                targetText = GetComponentInChildren<TMP_Text>();
            }

            if (targetImage != null)
            {
                _baseColor = targetImage.color;
                _baseRotate = targetImage.transform.rotation.eulerAngles.z;
            }
            _baseScale = transform.localScale;
        }

        public void SetSelected(bool target)
        {
            selected = target;
            selectedImage.color = selectedColor;
            selectedImage.gameObject.SetActive(target);
            selectedImage.transform.DOComplete();
            selectedImage.transform.DOScale(1.2f, 0.4f).From();
        }
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if(!Interactable){return;}
            if (enterScale > 1)
            {
                transform.DOScale(_baseScale * enterScale, 0.2f).SetEase(Ease.OutBack);
            }
            if (enterRotate != 0)
            {
                transform.DORotate(new Vector3(0,0,enterRotate), 0.2f).SetEase(Ease.OutBack);
            }
            if (enterColor != Color.white)
            {
                targetImage.DOColor(enterColor, 0.2f);
            }
        
            onPointerEnter?.Invoke();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if(!Interactable){return;}

            if (group)
            {
                group.Select(this);
            }
            onPointerClick?.Invoke();
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if(!Interactable){return;}
            if (enterScale > 1)
            {
                transform.DOScale(_baseScale, 0.2f);
            }
            if (enterRotate != 0)
            {
                transform.DORotate(new Vector3(0,0,_baseRotate), 0.2f);
            }
            if (enterColor != Color.white)
            {
                targetImage.DOColor(_baseColor, 0.2f);
            }
            onPointerExit?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            onPointerDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onPointerUp.Invoke();
        }

        private void OnEnable()
        {
            OnPointerExit(null);
        }
    }
}
