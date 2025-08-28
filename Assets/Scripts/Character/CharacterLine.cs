using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WUI;

namespace Character
{
    public class CharacterLine : PageLineItem
    {
        public TMP_Text characterNameText;
        public TMP_Text characterDescriptionText;
        public Image characterImage;
        public GameObject highlightObj;
        [HideInInspector]
        public CharacterData characterData;
        
        public WButton delete;
        public WButton copy;
        public WButton self;
        public WButton export;

        private void Start()
        {
            self.onPointerClick.AddListener(() =>
            {
                CharacterManager.instance.SetCurCharacter(characterData);
            });
            export.onPointerClick.AddListener(() =>
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowPropUpMessage("确认",$"确认导出 {characterData.characterTitle} 的数据吗？",()=>CharacterManager.instance.ExportCharacter(characterData));
                        break;
                    case "en":
                        MessageManager.instance.ShowPropUpMessage("Confirm",$"Confirm to export {characterData.characterTitle} data?",()=>CharacterManager.instance.ExportCharacter(characterData));
                        break;
                    default:
                        MessageManager.instance.ShowPropUpMessage("Confirm",$"Confirm to export {characterData.characterTitle} data?",()=>CharacterManager.instance.ExportCharacter(characterData));
                        break;
                }
            });
            copy.onPointerClick.AddListener(() =>
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowPropUpMessage("确认",$"确认复制 {characterData.characterTitle} 吗？\n将会创建一个 {characterData.characterTitle} 副本 对话",()=>CharacterManager.instance.CopyCharacter(characterData));
                        break;
                    case "en":
                        MessageManager.instance.ShowPropUpMessage("Confirm",$"Confirm to copy {characterData.characterTitle} data?",()=>CharacterManager.instance.CopyCharacter(characterData));
                        break;
                    default:
                        MessageManager.instance.ShowPropUpMessage("Confirm",$"Confirm to copy {characterData.characterTitle} data?",()=>CharacterManager.instance.CopyCharacter(characterData));
                        break;
                }
            });
            
            delete.onPointerClick.AddListener(() =>
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowPropUpMessage("确认",$"确认删除 {characterData.characterTitle} 吗？",()=>CharacterManager.instance.RemoveCharacter(characterData));
                        break;
                    case "en":
                        MessageManager.instance.ShowPropUpMessage("Confirm",$"Confirm to delete {characterData.characterTitle} data?",()=>CharacterManager.instance.RemoveCharacter(characterData));
                        break;
                    default:
                        MessageManager.instance.ShowPropUpMessage("Confirm",$"Confirm to delete {characterData.characterTitle} data?",()=>CharacterManager.instance.RemoveCharacter(characterData));
                        break;
                }
            });
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="targetCharacterData"></param>
        public void SetData(CharacterData targetCharacterData)
        { 
            characterData = targetCharacterData;
            characterNameText.text = this.characterData.characterTitle;
            characterDescriptionText.text = this.characterData.characterDescription;
            GameManager.instance.LoadImage(characterData.iconPath,characterImage,CharacterManager.instance.defHeadIcon); 
        }

        public override void Highlight(bool highlight)
        {
            highlightObj.SetActive(highlight);
        }
        public override IPageListItem GetData()
        {
            return characterData;
        }

        public override void SetData(IPageListItem item)
        {
            characterData = item as CharacterData;
            if(characterData == null) return;
            characterNameText.text = this.characterData.characterTitle;
            characterDescriptionText.text = this.characterData.characterDescription;
            GameManager.instance.LoadImage(characterData.iconPath,characterImage,CharacterManager.instance.defHeadIcon); 
        }
    }
}