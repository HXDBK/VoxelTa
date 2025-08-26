using System.Collections;
using System.Globalization;
using Character;
using DG.Tweening;
using Live2D.Cubism.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WUI;

namespace Live2D
{
    public class ParameterLine : PageLineItem
    {
        [Header("UI References")]
        public TMP_Text parameterNameText;
        public TMP_Text parameterIdText;
        public TMP_Text maxText;
        public TMP_Text minText;
        public TMP_Text valueText;
        public Slider valueSlider;
        public WButton defButton;
        public WButton addButton;
        public Transform baseParent;
        
        private ModelParameter _modelParameter;
        private CubismParameter _cubismParameter;

        private float _targetValue;
        private bool _isModified = false;
        private bool _isUpdatingSlider = false;

        private void Start()
        {
            if (defButton != null)
            {
                defButton.onPointerClick.RemoveAllListeners();
                defButton.onPointerClick.AddListener(ResetToDefault);
                defButton.gameObject.SetActive(false);
            }
        }
        
        private void LateUpdate()
        {
            if (_cubismParameter == null || !_isModified)
                return;

            _cubismParameter._value = _targetValue;
            _isModified = false;
        }

        /// <summary>
        /// 当滑动条值改变时触发
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            if (_isUpdatingSlider) return;

            _targetValue = value;
            _isModified = true;

            defButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// 将参数重置为默认值
        /// </summary>
        public void ResetToDefault()
        {
            if (_cubismParameter == null) return;

            _isUpdatingSlider = true;

            valueSlider.value = _cubismParameter.DefaultValue;
            _targetValue = _cubismParameter.DefaultValue;
            _isModified = true;

            defButton?.gameObject.SetActive(false);
            _isUpdatingSlider = false;
        }

        public override IPageListItem GetData()
        {
            return _modelParameter;
        }

        public override void SetData(IPageListItem item)
        {
            var modelParameter = (item as ModelParameter);
            if (modelParameter == null) return;
            if (_modelParameter != null)
            {
                _modelParameter.parameter.OnSetValue -= SetUIByValue;
            }

            _modelParameter = modelParameter;
            _cubismParameter = _modelParameter.parameter;
            _cubismParameter.OnSetValue += SetUIByValue;

            parameterIdText.text = _cubismParameter.name;
            if (_modelParameter.displayName is { Length: > 0 })
            {
                parameterNameText.text = _modelParameter.displayName;
            }
            else
            {
                parameterNameText.text = _cubismParameter.name;
            }
            maxText.text = _cubismParameter.MaximumValue.ToString(CultureInfo.InvariantCulture);
            minText.text = _cubismParameter.MinimumValue.ToString(CultureInfo.InvariantCulture);

            valueSlider.maxValue = _cubismParameter.MaximumValue;
            valueSlider.minValue = _cubismParameter.MinimumValue;
            valueSlider.value = _modelParameter.parameterValue;

            _targetValue = _modelParameter.parameterValue;

            valueSlider.onValueChanged.RemoveAllListeners();
            valueSlider.onValueChanged.AddListener(OnSliderValueChanged);

            defButton?.gameObject.SetActive(!Mathf.Approximately(_targetValue, _cubismParameter.DefaultValue));
            if (CharacterManager.instance.customExpPanel.isShow)
            {
                addButton.gameObject.SetActive(true);
                addButton.onPointerClick.RemoveAllListeners();
                addButton.onPointerClick.AddListener(() =>
                {
                    CharacterManager.instance.AddCustomExpParameter(_modelParameter);
                });
            }
            else
            {
                addButton.gameObject.SetActive(false);
            }
        }

        public override void Highlight(bool target)
        {
            if(!target){return;}
            baseParent.DOComplete();
            baseParent.DOShakeRotation(0.5f, new Vector3(0, 0, 10),20);
        }
        private void SetUIByValue(float value)
        {
            valueSlider.SetValueWithoutNotify(value);
            valueText.text = value.ToString("F1");
        }
    }
}
