using System;
using Character;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WUI;

namespace Live2D
{
    public class ExpLine : PageLineItem
    {
        public TMP_InputField expNameInput;
        public TMP_Text fileNameText;
        public WButton playButton;
        public WButton removeButton;
        public WButton editButton;
        public Toggle isOn;

        public Image icon;
        public Sprite playSprite;
        public Sprite stopSprite;
        public Image diableImage;
        
        private CubismExp3Json _expData;
        private ModelExp _modelExp;

        private void Start()
        {
            playButton.onPointerClick.RemoveAllListeners();
            playButton.onPointerClick.AddListener(Play);
            isOn.onValueChanged.RemoveAllListeners();
            isOn.onValueChanged.AddListener(SetIsOn);
            expNameInput.onValueChanged.RemoveAllListeners();
            expNameInput.onValueChanged.AddListener(SetNickName);
        }

        private void Play()
        {
            CharacterManager.instance.SetExpression(_expData,true);
            playButton.onPointerClick.RemoveAllListeners();
            playButton.onPointerClick.AddListener(Stop);
            icon.sprite = stopSprite;
        }

        private void Stop()
        {
            CharacterManager.instance.SetExpression(_expData,false);
            playButton.onPointerClick.RemoveAllListeners();
            playButton.onPointerClick.AddListener(Play);
            icon.sprite = playSprite;
        }

        public override IPageListItem GetData()
        {
            return _modelExp;
        }
        private void SetNickName(string target)
        {
            _modelExp.expNickname = target;
            // CharacterManager.instance.Changed();
        }
        public override void SetData(IPageListItem item)
        {
            _modelExp = item as ModelExp;
            if(_modelExp == null) return;
            _expData = _modelExp.exp3Json;
            expNameInput.text = _modelExp.expNickname;
            fileNameText.text = _modelExp.expName;

            isOn.isOn = _modelExp.expOn;
            diableImage.gameObject.SetActive(!_modelExp.expOn);

            icon.sprite = playSprite;
            removeButton.gameObject.SetActive(_modelExp.type == 1);
            editButton.gameObject.SetActive(_modelExp.type == 1);
        }

        private void SetIsOn(bool target)
        {
            _modelExp.expOn = target;
            diableImage.gameObject.SetActive(!target);
        }

        public ModelExp GetModeExp()
        {
            return _modelExp;
        }

        public void RemoveSelf()
        {
            switch (LocalizerManager.GetCode())
            {
                case "zh-Hans":
                    MessageManager.instance.ShowPropUpMessage("确认",$"确定要移除表情 {_modelExp.expName} 吗？",DoRemove);
                    break;
                case "en":
                    MessageManager.instance.ShowPropUpMessage("Confirm",$"Are you sure you want to remove the expression {_modelExp.expName}?",DoRemove);
                    break;
            }
        }

        private void DoRemove()
        {
            CharacterManager.instance.live2dDetailPanel.RemoveCusExp(_modelExp);
        }

        public void EditExp()
        {
            CharacterManager.instance.live2dDetailPanel.ShowCustomExpPanel(this);
            CharacterManager.instance.SetExpression(_expData,true);
        }
    }
}