using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Character;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Json;
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
        public List<ModelExp> expPageItems = new();
        public List<ModelParameter> parameterPageItems = new();
        public WPageList expPageList;

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
        private bool _needUseSnapshot;
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

        public void Show(CharacterData character)
        {
            _curCharacter = character;
            Show();
        }
        /// <summary>
        /// 显示和设置参数
        /// </summary>
        private void SetParametersValue()
        {
            parameterSearchInput.text = "";
            if (_curModel.modelData == null)
            {
                parameterPageItems.Clear();
                return;
            }
            var parameters = _curModel.modelData.Parameters;
            var savedParamDict = _curModel.characterData.modelParameters
                .ToDictionary(p => p.parameterId);

            parameterPageItems = new List<ModelParameter>();
            foreach (var parameter in parameters)
            {
                if (savedParamDict.TryGetValue(parameter.Id, out var savedParam))
                {
                    savedParam.SetParameter(parameter);
                    parameterPageItems.Add(savedParam);
                }
                else
                {
                    parameterPageItems.Add(new ModelParameter(parameter));
                }
            }
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
        /// 设置表情列表
        /// </summary>
        private void SetExpValue(Dictionary<string,CubismExp3Json> exps)
        {
            // curCharacter.modelExps.Clear();
            // 用户存储的表情数据
            var savedExpDict = _curCharacter.modelExps.ToDictionary(e => e.expName);

            // 最终页面展示的表情数据
            expPageItems = new List<ModelExp>();

            // 记录已添加的 key，避免重复添加
            HashSet<string> addedKeys = new();

            // 遍历模型提供的表情数据，优先使用用户存储数据
            foreach (var exp in exps)
            {
                if (savedExpDict.TryGetValue(exp.Key, out var savedExp))
                {
                    if (savedExp.exp3Json.Parameters == null)
                    {
                        savedExp.exp3Json = exp.Value;
                    }

                    savedExp.type = 0;
                    expPageItems.Add(savedExp);
                }
                else
                {
                    var newItem = new ModelExp(exp.Value, exp.Key, exp.Key, true)
                    {
                        type = 0
                    };
                    expPageItems.Add(newItem);
                }
                addedKeys.Add(exp.Key);
            }

            // 添加 savedExpDict 中独有的表情（模型中没有）
            foreach (var kv in savedExpDict)
            {
                if (!addedKeys.Contains(kv.Key))
                {
                    Debug.Log(kv.Value.expName);
                    kv.Value.type = 1;
                    expPageItems.Add(kv.Value);
                }
            }
            // expScrollList.SetData(expPageItems);

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
        /// 关闭自定义表情界面 不保存
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
            _needUseSnapshot = true;
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
            _curCharacter.lookCenter = _curModel.autoLookAtCenter.localPosition;
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