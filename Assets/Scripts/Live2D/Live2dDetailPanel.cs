using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Character;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Json;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WUI;

namespace Live2D
{
    /// <summary>
    /// Live2d 模型的详细设置面板
    /// </summary>
    public class Live2dDetailPanel : UIPanel
    {
        private Live2DController _curModel;
        private CharacterData _curCharacter;
        
        public Toggle isBreathToggle;
        public Toggle isBlinkToggle;
        public Toggle isLookMouseToggle;
        
        [Header("立绘参数")] 
        public WPageList parameterPageList;
        public TMP_InputField parameterSearchInput;
        private readonly List<ModelParameter> _needSetDef = new (); 
        private static readonly NaturalSortComparer NaturalComparer = new NaturalSortComparer();

        [Header("立绘表情")]
        [HideInInspector]
        public List<ModelExp> expPageItems;
        public List<ModelParameter> parameterPageItems;
        public WPageList expPageList;
        
        [Header("立绘动画")]
        [HideInInspector]
        public List<ModelMotion> motionPageItems;
        public WPageList motionPageList;

        public Toggle isShowDisableExpToggle;
        // 自定义表情
        public UIPanel customExpPanel;
        public WPageList customExpParameterPageList;
        public TMP_InputField customExpNameInput;
        public TMP_InputField customExpNickNameInput;
        public TMP_InputField customExpFadeInInput;
        public TMP_InputField customExpFadeOutInput;
        private ModelExp _curCustomExp;
        private readonly Dictionary<ModelParameter,float> _paramsSnapshot = new();
        private bool _customExpChanged;
        private Camera Camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = Camera.main;
                }
                return _camera;
            }
        }
        private Camera _camera;
        //设置模型中心
        private bool _isSettingCenter;
        public GameObject setCenterPanel;

        protected override void Start()
        {
            base.Start();
            isShowDisableExpToggle.onValueChanged.AddListener(IsShowDisableExps);
            isBreathToggle.onValueChanged.AddListener(_=>SaveData());
            isBlinkToggle.onValueChanged.AddListener(_=>SaveData());
            isLookMouseToggle.onValueChanged.AddListener(_=>SaveData());
            parameterSearchInput.onSubmit.AddListener(_=>Search());
        }

        private void Update()
        {
            if (_isSettingCenter)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _curModel.autoLookAtCenter.transform.position = Camera.ScreenToWorldPoint(Input.mousePosition);
                    EndSetCenter();
                }
            }
        }

        private void LateUpdate()
        {
            if (_needSetDef.Count > 0)
            {
                foreach (var parameterListItemData in _needSetDef)
                {
                    parameterListItemData.ResetToDefault();
                }
                _needSetDef.Clear();
            }
        }

        public void Show(CharacterData character,GeneralModel model)
        {
            _curCharacter = character;
            _curModel = model as Live2DController;
            if (_curModel == null){return;}
            
            Show();
            ShowParametersValue();
            SetExpValue(_curModel.expressions);
            SetMotionValue(null);
        }

        public override void Hide()
        {
            SaveParameters();
            SaveExp();
            SaveMotion();
            if (_curModel)
            {
                _curModel.ClearAllExpressions();
                _curModel.motionPlayer.Stop();
            }
            base.Hide();
        }

        /// <summary>
        /// 显示和设置参数
        /// </summary>
        private void ShowParametersValue()
        {
            parameterSearchInput.text = "";
            parameterPageItems = _curModel.parameterPageItems;
            //排序
            // parameterPageItems = parameterPageItems
            //     .OrderBy(p => p.parameterId, NaturalComparer)
            //     .ToList();

            parameterPageList.SetData(parameterPageItems);
        }
        /// <summary>
        /// 所有参数设置默认值
        /// </summary>
        public void SetParametersValuesDef()
        {
            foreach (var parameterItem in parameterPageList.GetData())
            {
                if (parameterItem is ModelParameter itemData)
                {
                    _needSetDef.Add(itemData);
                }
            }
        }
        /// <summary>
        /// 翻页到指定参数
        /// </summary>
        /// <param name="target"></param>
        public void GotoTargetParam(ModelExp.TmpExpParameter target)
        {
            foreach (var parameterPageItem in parameterPageItems)
            {
                if (parameterPageItem.parameterId == target.parameterId)
                {
                    parameterPageList.GotoItem(parameterPageItem);
                    break;
                }
            }
        }
        /// <summary>
        /// 搜索参数
        /// </summary>
        public void Search()
        {
            var keyword = parameterSearchInput.text?.ToLower();
    
            if (string.IsNullOrEmpty(keyword))
            {
                // 默认排序：按 parameterId 升序
                parameterPageList.SetData(parameterPageItems);
                return;
            }

            var results = parameterPageItems
                .Select(p => new
                {
                    Item = p,
                    Score = GetMatchScore(p, keyword)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Item)
                .ToList();

            parameterPageList.SetData(results);
        }
        private int GetMatchScore(ModelParameter p, string keyword)
        {
            int score = 0;

            // Exact matches: +100
            if (string.Equals(p.parameterId, keyword, StringComparison.OrdinalIgnoreCase)) score += 100;
            else if (!string.IsNullOrEmpty(p.parameterId) && p.parameterId.ToLower().StartsWith(keyword)) score += 50;
            else if (!string.IsNullOrEmpty(p.parameterId) && p.parameterId.ToLower().Contains(keyword)) score += 20;

            if (string.Equals(p.parameterName, keyword, StringComparison.OrdinalIgnoreCase)) score += 80;
            else if (!string.IsNullOrEmpty(p.parameterName) && p.parameterName.ToLower().StartsWith(keyword)) score += 40;
            else if (!string.IsNullOrEmpty(p.parameterName) && p.parameterName.ToLower().Contains(keyword)) score += 15;

            if (string.Equals(p.displayName, keyword, StringComparison.OrdinalIgnoreCase)) score += 60;
            else if (!string.IsNullOrEmpty(p.displayName) && p.displayName.ToLower().StartsWith(keyword)) score += 30;
            else if (!string.IsNullOrEmpty(p.displayName) && p.displayName.ToLower().Contains(keyword)) score += 10;

            return score;
        }
        
        /// <summary>
        /// 设置表情列表
        /// </summary>
        private void SetExpValue(Dictionary<string,CubismExp3Json> exps)
        {
            expPageItems = _curModel.expPageItems;

            // 根据是否显示禁用表情进行过滤
            if (isShowDisableExpToggle.isOn)
            {
                expPageList.SetData(expPageItems);
            }
            else
            {
                var list = expPageItems.Where(expPageItem => expPageItem.expOn).ToList();
                expPageList.SetData(list);
            }
        }        
        private void SetMotionValue(Dictionary<string,CubismMotion3Json> motions)
        {
            motionPageItems = _curModel.motionPageItems;
            motionPageList.SetData(motionPageItems);
        }
        /// <summary>
        /// 移除指定表情
        /// </summary>
        /// <param name="target"></param>
        public void RemoveCusExp(ModelExp target)
        {
            if (expPageItems.Contains(target))
            {
                expPageItems.Remove(target);
                expPageList.SetData(expPageItems);
                SaveExp();
            }
        }
        /// <summary>
        /// 显示自定义表情界面 创建
        /// </summary>
        public void ShowCustomExpPanel()
        {
            _curCustomExp = new ModelExp();
            TakeASnapshot();
            customExpPanel.Show();
            parameterPageList.Refresh();
            customExpNameInput.text = "";
            customExpNickNameInput.text = "";
            customExpFadeInInput.text = "0.5";
            customExpFadeOutInput.text = "0.5";
            customExpParameterPageList.Clear();
            _customExpChanged = false;
            
            _curModel.SetBlink(false);
            _curModel.SetBreath(false);
            _curModel.SetLookMouse(false);
        }
        /// <summary>
        /// 显示自定义表情界面 编辑
        /// </summary>
        public void ShowCustomExpPanel(ExpLine expLine)
        {
            _curCustomExp = expLine.GetModeExp();
            TakeASnapshot();
            customExpPanel.Show();
            parameterPageList.Refresh();
            _customExpChanged = false;
            customExpNameInput.text = _curCustomExp.expName;
            customExpNickNameInput.text = _curCustomExp.expNickname;;
            customExpFadeInInput.text = _curCustomExp.exp3Json.FadeInTime.ToString(CultureInfo.InvariantCulture);
            customExpFadeOutInput.text = _curCustomExp.exp3Json.FadeOutTime.ToString(CultureInfo.InvariantCulture);
            // customExpParameterPageList.SetData();
            _curCustomExp.tempParameters.Clear();
            foreach (var jsonParameter in _curCustomExp.exp3Json.Parameters)
            {
                foreach (var parameterPageItem in parameterPageItems)
                {
                    if (parameterPageItem.parameterId == jsonParameter.Id)
                    {
                        _curCustomExp.tempParameters.Add(new ModelExp.TmpExpParameter()
                        {
                            parameterId = jsonParameter.Id,
                            parameterDisplayName = parameterPageItem.displayName,
                            value = jsonParameter.Value,
                        });
                        break;
                    }
                }
            }
            customExpParameterPageList.SetData(_curCustomExp.tempParameters);
            _customExpChanged = false;
            _curModel.SetBlink(false);
            _curModel.SetBreath(false);
            _curModel.SetLookMouse(false);
        }
        /// <summary>
        /// 给自定义表情添加参数
        /// </summary>
        /// <param name="target"></param>
        public void AddCustomExpParameter(ModelParameter target)
        {
            _customExpChanged = true;
            _curCustomExp.AddTmpExpParameter(target);
            customExpParameterPageList.SetData(_curCustomExp.tempParameters);
        }
        /// <summary>
        /// 移除自定义表情参数
        /// </summary>
        /// <param name="target"></param>
        public void RemoveCustomExpParameter(ModelExp.TmpExpParameter target)
        {
            _customExpChanged = true;
            _curCustomExp.RemoveTmpExpParameter(target);
            customExpParameterPageList.SetData(_curCustomExp.tempParameters);
        }
        /// <summary>
        /// 是否显示不使用的表情
        /// </summary>
        /// <param name="target"></param>
        private void IsShowDisableExps(bool target)
        {
            if (isShow)
            {
                expPageList.SetData(expPageItems);
            }
            else
            {
                var list = expPageItems.Where(expPageItem => expPageItem.expOn).ToList();
                expPageList.SetData(list);
            }
        }
        /// <summary>
        /// 关闭自定义表情界面
        /// </summary>
        public void HideCustomExpPanel()
        {
            if (_customExpChanged)
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowPropUpMessage("保存","是否需要保存当前的自定义表情？", SaveAndHideCustomExp, () =>
                        {
                            _curCustomExp = null;
                            customExpPanel.Hide();
                            UseSnapshot();
                            parameterPageList.Refresh();
                            _curModel.SetBlink(_curCharacter.isBlink);
                            _curModel.SetBreath(_curCharacter.isBreath);
                            _curModel.SetLookMouse(_curCharacter.isLookAt);
                            _curModel.ClearAllExpressions();
                        });
                        break;
                    case "en":
                        MessageManager.instance.ShowPropUpMessage("Save?","Do you want to save the current custom expression?", SaveAndHideCustomExp, () =>
                        {
                            _curCustomExp = null;
                            customExpPanel.Hide();
                            UseSnapshot();
                            parameterPageList.Refresh();
                            _curModel.SetBlink(_curCharacter.isBlink);
                            _curModel.SetBreath(_curCharacter.isBreath);
                            _curModel.SetLookMouse(_curCharacter.isLookAt);
                            _curModel.ClearAllExpressions();
                        });
                        break;
                }
            }
            else
            {
                customExpPanel.Hide();
                UseSnapshot();
                parameterPageList.Refresh();
                _curModel.SetBlink(_curCharacter.isBlink);
                _curModel.SetBreath(_curCharacter.isBreath);
                _curModel.SetLookMouse(_curCharacter.isLookAt);
                _curModel.ClearAllExpressions();
            }
        }
        /// <summary>
        /// 关闭自定义表情界面 并保存
        /// </summary>
        public void SaveAndHideCustomExp()
        {
            if (customExpNameInput.text.Length <= 0)
            {
                MessageManager.instance.ShowMessage("请填写 表情名称");
                return;
            }
            if (customExpNickNameInput.text.Length <= 0)
            {
                MessageManager.instance.ShowMessage("请填写 表情识别名称");
                return;
            }
            if (customExpFadeInInput.text.Length <= 0)
            {
                MessageManager.instance.ShowMessage("请填写 淡入时长");
                return;
            }
            if (customExpFadeOutInput.text.Length <= 0)
            {
                MessageManager.instance.ShowMessage("请填写 淡出时长");
                return;
            }
            if (_curCustomExp.tempParameters.Count <= 0)
            {
                MessageManager.instance.ShowMessage("请至少添加一个表情参数");
                return;
            }

            _customExpChanged = false;
            _curCustomExp.expName = customExpNameInput.text;
            _curCustomExp.expNickname = customExpNickNameInput.text;
            _curCustomExp.exp3Json.FadeInTime = float.Parse(customExpFadeInInput.text);
            _curCustomExp.exp3Json.FadeOutTime = float.Parse(customExpFadeOutInput.text);
            _curCustomExp.exp3Json.Parameters = new CubismExp3Json.SerializableExpressionParameter[_curCustomExp.tempParameters.Count];
            for (var i = 0; i < _curCustomExp.tempParameters.Count; i++)
            {
                var tmp = new CubismExp3Json.SerializableExpressionParameter
                {
                    Id = _curCustomExp.tempParameters[i].parameterId,
                    Value = _curCustomExp.tempParameters[i].value,
                    Blend = nameof(CubismParameterBlendMode.Override)
                };
                _curCustomExp.exp3Json.Parameters[i] = tmp;
            }

            var flag = false;
            for (var i = 0; i < expPageItems.Count; i++)
            {
                if (expPageItems[i].expName == _curCustomExp.expName)
                {
                    expPageItems[i] = _curCustomExp;
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                expPageItems.Add(_curCustomExp);
            }
            customExpPanel.Hide();
            CharacterManager.instance.Changed();
            UseSnapshot();
            parameterPageList.Refresh();
            expPageList.SetData(expPageItems);
            _curModel.SetBlink(_curCharacter.isBlink);
            _curModel.SetBreath(_curCharacter.isBreath);
            _curModel.SetLookMouse(_curCharacter.isLookAt);
        }
        /// <summary>
        /// 为所有参数创建一个快照
        /// </summary>
        private void TakeASnapshot()
        {
            _paramsSnapshot.Clear();
            foreach (var parameterPageItem in parameterPageItems)
            {
                _paramsSnapshot.Add(parameterPageItem,parameterPageItem.parameterValue);
            }
        }
        /// <summary>
        /// 使用上一个快照
        /// </summary>
        private void UseSnapshot()
        {
            _curModel.SetParameterValue(_paramsSnapshot);
        }
        /// <summary>
        /// 开始设置眼睛中心点
        /// </summary>
        public void StartSetCenter()
        {
            _isSettingCenter = true;
            _curModel.SetLookMouse(false);
            setCenterPanel.SetActive(true);
            _curModel.autoLookAtCenter.gameObject.SetActive(true);
        }
        /// <summary>
        /// 停止设置眼睛中心点
        /// </summary>
        private void EndSetCenter()
        {
            _isSettingCenter = false;
            _curModel.SetLookMouse(isLookMouseToggle.isOn);
            setCenterPanel.SetActive(false);
            _curModel.autoLookAtCenter.gameObject.SetActive(false);
            SaveData();
            MessageManager.instance.ShowMessage("已设置模型视线中心点");
        }
        /// <summary>
        /// 将数据设置到 实例
        /// </summary>
        public void SaveData()
        {
            _curCharacter.isBlink = isBlinkToggle.isOn;
            _curCharacter.isBreath = isBreathToggle.isOn;
            _curCharacter.isLookAt = isLookMouseToggle.isOn;
            
            _curModel.SetBlink(isBlinkToggle.isOn);
            _curModel.SetBreath(isBreathToggle.isOn);
            _curModel.SetLookMouse(isLookMouseToggle.isOn);
            
            _curCharacter.lookCenter = _curModel.autoLookAtCenter.localPosition;
            CharacterManager.instance.Changed();
        }
        /// <summary>
        /// 保存表情
        /// </summary>
        private void SaveExp()
        {
            _curCharacter.modelExps.Clear();
            foreach (var exp in expPageItems)
            {
                _curCharacter.modelExps.Add(exp);
            }
            CharacterManager.instance.Changed();
        }
        private void SaveMotion()
        {
            _curCharacter.modelMotions.Clear();
            foreach (var motion in motionPageItems)
            {
                _curCharacter.modelMotions.Add(motion);
            }
            CharacterManager.instance.Changed();
        }
        /// <summary>
        /// 保存参数
        /// </summary>
        public void SaveParameters()
        {
            Debug.Log("SaveParameters");
            _curCharacter.modelParameters = new List<ModelParameter>();
            foreach (var parameterItem in  parameterPageItems)
            {
                _curCharacter.modelParameters.Add(parameterItem);
            }
            CharacterManager.instance.Changed();
        }
    }
}