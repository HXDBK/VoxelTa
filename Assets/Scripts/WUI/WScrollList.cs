using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace WUI
{
    public class WScrollList : UIPanel, IScrollHandler
    {
        public RectTransform content;
        private readonly List<PageLineItem> _lineList = new();
        private readonly List<float> _lineHeightList = new();
        private float _totalMoveHeight;
        public PageLineItem itemPrefab;
        [Header("冗余数量")]
        public int redundantNum = 3; // 比可见范围多几个
        [Header("元素间距")]
        public float spacing;
        [Header("滚动速度")]
        public float scrollSpeed = 10;

        [Header("滑动条")]
        public RectTransform scrollBar;
        public ScrollBarHandler handler;
        private bool _isDragging = false;
        private float _dragOffsetY = 0f;


        private int _endDataInx=-1;
        private int _startDataInx=-1;
        private int _curItemInx;
        private List<IPageListItem> _dataList = new ();
        private Vector2 _lastMaskSize;
        private bool _changeFlag;
        private float _changeFlagTime;
        private bool _shouldScrollToBottom = false;

        protected override void Start()
        {
            if (handler.scrollList == null)
            {
                handler.scrollList = this;
            } 
            _lastMaskSize = content.rect.size;
        }

        private void Update()
        {
            if (_dataList is not { Count: > 0 }) return;

            if (content.rect.size != _lastMaskSize)
            {
                _lastMaskSize = content.rect.size;
                _changeFlag = true;
                _changeFlagTime = 0;
            }
            else if (_changeFlag)
            {
                _changeFlagTime += Time.deltaTime;
                if (_changeFlagTime >= 0.5f)
                {
                    foreach (var lineItem in _lineList)
                    {
                        Destroy(lineItem.gameObject);
                    }
                    _lineList.Clear();
                    Refresh();
                    _shouldScrollToBottom = true; // 延迟处理
                    _changeFlag = false;
                }
            }
        }
        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="target"></param>
        /// <typeparam name="T"></typeparam>
        public void SetData<T>(List<T> target) where T : IPageListItem
        {
            _dataList = target.Cast<IPageListItem>().ToList();
            Refresh();
        }
        /// <summary>
        /// 刷新显示
        /// </summary>
        public void Refresh()
        {
            if (_dataList.Count <= 0)
            {
                Clear();
                return;
            }
            if (handler.scrollList == null)
            {
                handler.scrollList = this;
            } 
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float heightSum = content.rect.height / 2f;
            int hiddenCount = 0;
            _startDataInx = 0;
            for (int i = 0; i < _dataList.Count; i++)
            {
                // 提前判断是否超过隐藏限制
                if (i > 0)
                {
                    int state = CheckState(_lineList[i - 1].RectTransform, GetBottomEdge(content));
                    if (state != 0 && state != 1)
                    {
                        hiddenCount++;
                        if (hiddenCount >= redundantNum)
                            break;
                    }
                }
                PageLineItem line;
                if (i < _lineList.Count)
                {
                    line = _lineList[i];
                    line.gameObject.SetActive(true);
                }
                else
                {
                    line = Instantiate(itemPrefab, content);
                    _lineList.Add(line);
                }

                line.SetData(_dataList[i]);
                _endDataInx = i;
                float itemHeight = line.GetHeight();
                line.RectTransform.anchoredPosition = new Vector2(0, heightSum - itemHeight / 2 - spacing);
                heightSum -= itemHeight + spacing;
            }

            for (int i = _dataList.Count; i < _lineList.Count; i++)
            {
                _lineList[i].gameObject.SetActive(false);
            }
            
            // 设置高度数据
            _lineHeightList.Clear();
            foreach (var listItem in _dataList)
            {
                var height = _lineList[0].GetHeight(listItem);
                _lineHeightList.Add(height);
            }
            // 设置拖动条
            handler.RectTransform.sizeDelta = new Vector2(scrollBar.sizeDelta.x, (float)_endDataInx / _dataList.Count * scrollBar.rect.height);
            
            var maskBtm = -content.rect.height / 2;

            var itemBottom = _lineList[^1].RectTransform.anchoredPosition.y - _lineList[^1].GetHeight() / 2;
            var toBottomLength = maskBtm - itemBottom;

            var noShowDataLength = 0f;
            for (int i = _endDataInx + 1; i < _dataList.Count; i++)
            {
                noShowDataLength += spacing;
                noShowDataLength += _lineHeightList[i];
            }

            _totalMoveHeight = toBottomLength + noShowDataLength;
            UpdateScrollBar(); // 新增：设置滑动条
        }
        /// <summary>
        /// 更新滚动条
        /// </summary>
        private void UpdateScrollBar()
        {
            if(_isDragging){return;}
            var maskBtm = -content.rect.height / 2;

            var itemBottom = _lineList[^1].RectTransform.anchoredPosition.y - _lineList[^1].GetHeight() / 2;
            var toBottomLength = maskBtm - itemBottom;

            var noShowDataLength = 0f;
            for (int i = _endDataInx + 1; i < _dataList.Count; i++)
            {
                noShowDataLength += spacing;
                noShowDataLength += _lineHeightList[i];
            }

            var allMoveLength = scrollBar.rect.height - handler.RectTransform.rect.height;

            float ratio = (toBottomLength + noShowDataLength) /_totalMoveHeight;

            handler.RectTransform.anchoredPosition = new Vector2(
                handler.RectTransform.anchoredPosition.x,
                -(1-ratio) * allMoveLength
            );
            // Debug.Log("--------------------");
            // Debug.Log($"_dataList : {_dataList.Count},_endDataInx : {_endDataInx}");
            // Debug.Log($"ratio ： {ratio}，-(1 - ratio) * allMoveLength : {-(1 - ratio) * allMoveLength}");
            // Debug.Log($"[ScrollBar] toBottomLength: {toBottomLength}, noShowDataLength: {noShowDataLength}");
        }
        /// <summary>
        /// 滚动时触发
        /// </summary>
        /// <param name="eventData"></param>
        public void OnScroll(PointerEventData eventData)
        {
            if (_endDataInx >= _dataList.Count - 1 && CheckState(_lineList[^1].RectTransform,GetBottomEdge(content)) == 1 && eventData.scrollDelta.y < 0)
            {
                _endDataInx = _dataList.Count - 1;
                return;
            }
            if (_startDataInx <= 0 && CheckState(_lineList[0].RectTransform,GetTopEdge(content)) == -1 && eventData.scrollDelta.y > 0)
            {
                _startDataInx = 0;
                return;
            }
            var moveHeight = eventData.scrollDelta.y * scrollSpeed;
            foreach (var lineItem in _lineList)
            {
                lineItem.RectTransform.anchoredPosition -= new Vector2(0, moveHeight);
            }
            //列表向上滑动
            if (eventData.scrollDelta.y < 0)
            {
                CheckBottom();
            }
            //列表向下滑动
            else
            {
                CheckTop();
            }

            UpdateScrollBar();
        }
        /// <summary>
        /// 滚动条开始拖动时调用
        /// </summary>
        /// <param name="eventData"></param>
        public void OnScrollBarBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    scrollBar, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                // 计算从顶部起的偏移
                float handlerTopY = handler.RectTransform.anchoredPosition.y;
                _dragOffsetY = localPoint.y - handlerTopY + scrollBar.rect.height/2;
            }
        }
        /// <summary>
        /// 滚动条拖动时调用
        /// </summary>
        /// <param name="eventData"></param>
        public void OnScrollBarDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    scrollBar, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                float moveRange = scrollBar.rect.height - handler.RectTransform.rect.height;
                
                float targetY = localPoint.y - _dragOffsetY + scrollBar.rect.height/2;
                targetY = Mathf.Clamp(targetY, -moveRange,0);
                handler.RectTransform.anchoredPosition = new Vector2(
                    handler.RectTransform.anchoredPosition.x,
                    targetY
                );
                               
                float handlerMove = handler.RectTransform.anchoredPosition.y;  // 顶部锚点，取负值作为“从顶部的距离”
                float ratio1 = - handlerMove / moveRange;
                float uiTargetPos = ratio1 * _totalMoveHeight - _lineHeightList[0] / 2 + content.rect.height / 2;

                for (int i = 0; i < _startDataInx; i++)
                {
                    uiTargetPos -= _lineHeightList[i];
                    uiTargetPos -= spacing;
                }
                var needMove = _lineList[0].RectTransform.anchoredPosition.y - uiTargetPos;

                foreach (var lineItem in _lineList)
                {
                    lineItem.RectTransform.anchoredPosition -= new Vector2(0, needMove);
                }
                //列表向上滑动
                if (needMove < 0)
                {
                    CheckBottom();
                }
                //列表向下滑动
                else
                {
                    CheckTop();
                }
            }
        }
        /// <summary>
        /// 滚动条拖动结束时调用
        /// </summary>
        /// <param name="eventData"></param>
        public void OnScrollBarEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _dragOffsetY = 0f;
        }
        /// <summary>
        /// 下边缘数据检测
        /// </summary>
        private void CheckBottom()
        {
            while (CheckState(_lineList[^1].RectTransform,GetBottomEdge(content)) != -1 && _endDataInx < _dataList.Count - 1)
            {
                if (_endDataInx < _dataList.Count - 1)
                {
                    var first = _lineList[0];
                    _lineList.RemoveAt(0);
                    _lineList.Add(first);
                    _endDataInx++;
                    _startDataInx++;
                    first.SetData(_dataList[_endDataInx]);
                    first.RectTransform.anchoredPosition = new Vector2(0, _lineList[^2].RectTransform.anchoredPosition.y - _lineList[^2].GetHeight()/2 - first.GetHeight()/2 - spacing);
                }
            }

            if (CheckState(_lineList[^1].RectTransform, GetBottomEdge(content)) == 1 && _endDataInx == _dataList.Count - 1)
            {
                var targetPos = -content.rect.height / 2 + _lineList[^1].GetHeight()/2;
                var needMove = _lineList[^1].RectTransform.anchoredPosition.y - targetPos;
                foreach (var lineItem in _lineList)
                {
                    lineItem.RectTransform.anchoredPosition -= new Vector2(0, needMove);
                }
            }
        }
        /// <summary>
        /// 上边缘数据检测
        /// </summary>
        private void CheckTop()
        {
            while (CheckState(_lineList[0].RectTransform,GetTopEdge(content)) != 1 && _startDataInx > 0)
            {
                if (_startDataInx > 0)
                {
                    var last = _lineList[^1];
                    _lineList.RemoveAt(_lineList.Count - 1);
                    _lineList.Insert(0,last);
                    _startDataInx--;
                    _endDataInx--;
                    last.SetData(_dataList[_startDataInx]);
                    last.RectTransform.anchoredPosition = new Vector2(0, _lineList[1].RectTransform.anchoredPosition.y + _lineList[1].GetHeight()/2 + last.GetHeight()/2 + spacing);
                }
            }
            
            if (CheckState(_lineList[0].RectTransform, GetTopEdge(content)) == -1 && _startDataInx == 0)
            {
                var targetPos = content.rect.height / 2 - _lineList[0].GetHeight()/2;
                var needMove = targetPos - _lineList[0].RectTransform.anchoredPosition.y;
                // Debug.Log($"do move ： {needMove} targetPos : {targetPos} anchoredPosition.y ： {_lineList[0].RectTransform.anchoredPosition.y}");
                foreach (var lineItem in _lineList)
                {
                    lineItem.RectTransform.anchoredPosition += new Vector2(0, needMove);
                }
            }
        }
        /// <summary>
        /// 检查元素状态 1：在pos上面，0：在pos中间，-1：在pos下面
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int CheckState(RectTransform target, float pos)
        {
            if (GetBottomEdge(target) < pos && GetTopEdge(target) > pos)
            {
                return 0;
            }
            if (GetBottomEdge(target) > pos)
            {
                return 1;
            }
            if (GetTopEdge(target) < pos)
            {
                return -1;
            }
            return -2;
        }
        /// <summary>
        /// 得到UI元素上边框位置
        /// </summary>
        /// <param name="rectT"></param>
        /// <returns></returns>
        private float GetTopEdge(RectTransform rectT)
        {
            Vector3[] corners = new Vector3[4];
            rectT.GetWorldCorners(corners);
            return corners[1].y; // 左上角
        }
        /// <summary>
        /// 得到UI元素下边框位置
        /// </summary>
        /// <param name="rectT"></param>
        /// <returns></returns>
        private float GetBottomEdge(RectTransform rectT)
        {
            Vector3[] corners = new Vector3[4];
            rectT.GetWorldCorners(corners);
            return corners[0].y; // 左下角
        }
        #region 公共方法
        /// <summary>
        /// 滑动到底部
        /// </summary>
        public void ScrollToBottom()
        {
            float moveRange = scrollBar.rect.height - handler.RectTransform.rect.height;

            // 设置滑动条到底部
            float targetY = -moveRange;
            handler.RectTransform.anchoredPosition = new Vector2(
                handler.RectTransform.anchoredPosition.x,
                targetY
            );
            if (_dataList.Count <= 0)
            {
                return;
            }
            // 根据滑动条位置计算偏移并移动内容
            float uiTargetPos = _totalMoveHeight - _lineHeightList[0] / 2 + content.rect.height / 2;

            for (int i = 0; i < _startDataInx; i++)
            {
                uiTargetPos -= _lineHeightList[i];
                uiTargetPos -= spacing;
            }

            float needMove = _lineList[0].RectTransform.anchoredPosition.y - uiTargetPos;

            foreach (var lineItem in _lineList)
            {
                lineItem.RectTransform.anchoredPosition -= new Vector2(0, needMove);
            }

            if (needMove < 0)
            {
                CheckBottom();
            }
            else
            {
                CheckTop();
            }
        }
        /// <summary>
        /// 根据数据获取指定实例
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public PageLineItem GetItem(IPageListItem data)
        {
            foreach (var listItem in _lineList)
            {
                if (listItem.GetData() == data)
                {
                    return listItem;
                }
            }

            return null;
        }

        public List<PageLineItem> GetItems()
        {
            return _lineList;
        }

        public void Clear()
        {
            foreach (var lineItem in _lineList)
            {
                lineItem.gameObject.SetActive(false);
            }
            handler.RectTransform.anchoredPosition = new Vector2(0,0);
            handler.RectTransform.sizeDelta = scrollBar.sizeDelta;
        }
        #endregion
    }
}