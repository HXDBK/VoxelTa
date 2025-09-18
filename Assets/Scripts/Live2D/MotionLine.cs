using System;
using Character;
using Live2D.Cubism.Framework.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WUI;

namespace Live2D
{
    public class MotionLine : PageLineItem
    {
        public TMP_InputField motionNameInput;
        public TMP_Text fileNameText;
        public WButton playButton;
        public Toggle isOn;
        public Toggle isLoop;

        private ModelMotion _modelMotion;

        private void Start()
        {
            playButton.onPointerClick.RemoveAllListeners();
            playButton.onPointerClick.AddListener(()=>CharacterManager.instance.PlayMotion(_modelMotion));
            isLoop.onValueChanged.RemoveAllListeners();
            isLoop.onValueChanged.AddListener(SetLoop);
            motionNameInput.onValueChanged.RemoveAllListeners();
            motionNameInput.onValueChanged.AddListener(SetNickName);
        }

        private void SetLoop(bool target)
        {
            _modelMotion.motionLoop = target;
            // CharacterManager.instance.Changed();
        }
        private void SetNickName(string target)
        {
            _modelMotion.motionNickname = target;
            // CharacterManager.instance.Changed();
        }
        public override IPageListItem GetData()
        {
            return _modelMotion;
        }

        public override void SetData(IPageListItem item)
        {
            var target = item as ModelMotion;
            if (target == null) return;
            _modelMotion = target;
            motionNameInput.text = _modelMotion.motionNickname;
            fileNameText.text = _modelMotion.motionName;
            isOn.isOn = _modelMotion.motionOn;
            isLoop.isOn = _modelMotion.motionLoop;
        }
    }
}
