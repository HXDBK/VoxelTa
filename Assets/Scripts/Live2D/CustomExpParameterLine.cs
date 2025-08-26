using System.Globalization;
using Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WUI;

namespace Live2D
{
    public class CustomExpParameterLine : PageLineItem
    {
        public TMP_Text parameterNameText;
        public TMP_Text parameterIdText;
        public TMP_Text valueText;
        
        private ModelExp.TmpExpParameter _modelParameter;

        public override IPageListItem GetData()
        {
            return _modelParameter;
        }

        public override void SetData(IPageListItem item)
        {
            _modelParameter = (item as ModelExp.TmpExpParameter);
            if (_modelParameter == null) return;

            valueText.text = "值设置为 " + _modelParameter.value.ToString("F1");
            parameterIdText.text = _modelParameter.parameterId;
            parameterNameText.text = _modelParameter.parameterDisplayName is { Length: > 0 } ? _modelParameter.parameterDisplayName : _modelParameter.parameterId;
        }

        public void RemoveSelf()
        {
            CharacterManager.instance.RemoveCustomExpParameter(_modelParameter);
        }

        public void GotoTarget()
        {
            CharacterManager.instance.GotoTargetParam(_modelParameter);
        }
        private void SetUIByValue(float value)
        {
            valueText.text = "值设置为 " + value.ToString("F1");
        }
    }
}