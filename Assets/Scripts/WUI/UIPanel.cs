using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WUI
{
    public class UIPanel : MonoBehaviour
    {
        public Image targetImage;
        public CanvasGroup targetCanvasGroup;
        public bool isShow = false;
        private RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get => _rectTransform ??= GetComponent<RectTransform>();
            set => _rectTransform = value;
        }
        protected virtual void Start()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
            if (targetCanvasGroup == null)
            {
                targetCanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public virtual void Show()
        {
            isShow = true;
            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.DOComplete();
                targetCanvasGroup.alpha = 0;
                targetCanvasGroup.gameObject.SetActive(true);
                targetCanvasGroup.DOFade(1, 0.2f);
            }else if (targetImage != null)
            {
                targetImage.color = new Color(1, 1, 1, 0);
                targetImage.gameObject.SetActive(true);
                targetImage.DOFade(1, 0.2f);
            }
        }

        public virtual void Hide()
        {
            isShow = false;
            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.DOComplete();
                targetCanvasGroup.DOFade(0, 0.2f).OnComplete(() =>
                {
                    targetCanvasGroup.gameObject.SetActive(false);
                });
            }else if (targetImage != null)
            {
                targetImage.DOComplete();
                targetImage.DOFade(0, 0.2f).OnComplete(() =>
                {
                    targetImage.gameObject.SetActive(false);
                });
            }
        }
    
        /// <summary>
        /// 绑定数据到 UI 列表。
        /// </summary>
        /// <typeparam name="TData">数据类型</typeparam>
        /// <typeparam name="TLine">UI行组件类型</typeparam>
        /// <param name="dataList">数据列表</param>
        /// <param name="lineList">已有的 UI 行列表</param>
        /// <param name="linePrefab">行的预制体</param>
        /// <param name="parent">父级 Transform</param>
        /// <param name="setData">设置 UI 行的方法： (行对象, 数据, index) => void</param>
        public static void BindList<TData, TLine>(
            List<TData> dataList,
            List<TLine> lineList,
            TLine linePrefab,
            Transform parent,
            Action<TLine, TData, int> setData
        ) where TLine : Component
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                if (i < lineList.Count)
                {
                    setData(lineList[i], dataList[i], i);
                    lineList[i].gameObject.SetActive(true);
                }
                else
                {
                    var newLine = Instantiate(linePrefab, parent);
                    setData(newLine, dataList[i], i);
                    lineList.Add(newLine);
                }
            }

            for (int i = dataList.Count; i < lineList.Count; i++)
            {
                lineList[i].gameObject.SetActive(false);
            }
        }
    }
}