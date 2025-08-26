using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace WUI
{
    public class WPageList : MonoBehaviour
    {
        [Header("Prefabs & UI")]
        public PageLineItem lineItemPrefab;
        private VerticalLayoutGroup _layoutGroup;
        public TMP_Text pageText;
        public WButton firstPageButton;
        public WButton previousPageButton;
        public WButton nextPageButton;
        public WButton lastPageButton;
        public RectTransform contentParent;

        [Header("Page Settings")]
        public int pageSize = 5;

        private List<IPageListItem> _items = new();
        private readonly List<PageLineItem> _lineItems = new();
        private int _curPage;
        private int _maxPage;
        private Vector2 _lastContentSize;
        private bool _changeFlag;
        private float _changeFlagTime;

        private void Awake()
        {
            // 绑定按钮点击事件（可自定义为公开方法绑定）
            previousPageButton.onPointerClick.AddListener(PreviousPage);
            nextPageButton.onPointerClick.AddListener(NextPage);
            firstPageButton.onPointerClick.AddListener(FirstPage);
            lastPageButton.onPointerClick.AddListener(LastPage);
            if (_layoutGroup == null)
            {
                _layoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();
            }
            _lastContentSize = contentParent.rect.size;
        }
        private void Update()
        {
            if (contentParent.rect.size != _lastContentSize)
            {
                _lastContentSize = contentParent.rect.size;
                _changeFlag = true;
                _changeFlagTime = 0;
            }else if (_changeFlag)
            {
                _changeFlagTime+=Time.deltaTime;
                if (_changeFlagTime >= 0.5f)
                {
                    Init(); // 或延迟一帧调用 Init()
                    _changeFlag = false;
                }
            }
        }
        public void SetData<T>(List<T> target) where T : IPageListItem
        {
            _items = target.Cast<IPageListItem>().ToList();
            Init();
        }

        private void Init()
        {
            if (_layoutGroup == null)
            {
                _layoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();
            }
            pageSize = (int)(contentParent.rect.height / (lineItemPrefab.GetHeight() + _layoutGroup.spacing));
            _maxPage = Mathf.Max(0, Mathf.CeilToInt((float)_items.Count / pageSize) - 1);
            if (_curPage > _maxPage)
            {
                _curPage = 0;
            }
            Refresh();
        }
        public void Clear()
        {
            _items.Clear();
            _curPage = 0;
            _maxPage = 0;

            foreach (var item in _lineItems)
            {
                item.SetActive(false);
            }

            pageText.text = "0 / 0";
        }
        public void GotoItem(IPageListItem targetItem)
        {
            int index = _items.IndexOf(targetItem);
            if (index == -1) return;

            int targetPage = index / pageSize;
            _curPage = targetPage;
            Refresh();

            int startIndex = _curPage * pageSize;
            int relativeIndex = index - startIndex;

            if (relativeIndex >= 0 && relativeIndex < _lineItems.Count)
            {
                for (var i = 0; i < _lineItems.Count; i++)
                {
                    _lineItems[i].Highlight(i==relativeIndex);
                }
            }
        }
        private void NextPage()
        {
            if (_curPage < _maxPage)
            {
                _curPage++;
                Refresh();
            }
        }
        private void PreviousPage()
        {
            if (_curPage > 0)
            {
                _curPage--;
                Refresh();
            }
        }
        public void GotoPage(int target)
        {
            if (target >= 0 && target <= _maxPage)
            {
                _curPage = target;
                Refresh();
            }
        }
        public void FirstPage()
        {
            GotoPage(0);
        }
        public void LastPage()
        {
            GotoPage(_maxPage);
        }
        public List<IPageListItem> GetData()
        {
            return _items;
        }
        public void Refresh()
        {
            int startIndex = _curPage * pageSize;
            int endIndex = Mathf.Min(startIndex + pageSize, _items.Count);

            // 动态补齐UI行
            while (_lineItems.Count < pageSize)
            {
                PageLineItem newItem = Instantiate(lineItemPrefab, contentParent);
                newItem.gameObject.SetActive(false);
                _lineItems.Add(newItem);
            }

            for (int i = 0; i < _lineItems.Count; i++)
            {
                if (startIndex + i < endIndex)
                {
                    _lineItems[i].SetData(_items[startIndex + i]);
                    _lineItems[i].SetActive(true);
                }
                else
                {
                    _lineItems[i].SetActive(false);
                }
            }

            UpdateUIState();
        }

        private void UpdateUIState()
        {
            pageText.text = $"{_curPage + 1} / {_maxPage + 1}";

            previousPageButton.Interactable = (_curPage > 0);
            nextPageButton.Interactable = (_curPage < _maxPage);
        }
    }
}
