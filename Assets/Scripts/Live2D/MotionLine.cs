using Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WUI;

namespace Live2D
{
    public class MotionLine : MonoBehaviour
    {
        public TMP_InputField motionNameInput;
        public TMP_Text fileNameText;
        public WButton playButton;
        public Toggle isOn;

        public AnimationClip Clip => _clip;
        private AnimationClip _clip;
        public void SetData(AnimationClip target)
        {
            _clip = target;
            motionNameInput.text = target.name;
            fileNameText.text = target.name;
            playButton.onPointerClick.AddListener(()=>CharacterManager.instance.PlayMotion(_clip));
        }
        public void SetData(ModelMotion target)
        {
            motionNameInput.text = target.motionNickname;
            isOn.isOn = target.motionOn;
        }
        public ModelMotion GetMotionData()
        {
            var result = new ModelMotion
            {
                motionName = _clip.name,
                motionNickname = motionNameInput.text,
                motionOn = isOn.isOn
            };
            return result;
        }
    }
}
