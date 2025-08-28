using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Character;
using Dialog;
using Other;
using TMPro;
using TTS;
using UnityEngine;
using UnityEngine.UI;
using WUI;
using Random = UnityEngine.Random;

public class DialogLine : PageLineItem
{
    public RectTransform headRect;
    public Image headImage;
    public TMP_InputField dialogLineText;
    public TMP_InputField thinkText;
    public TMP_Text talkerText;
    public TMP_Text timeText;
    public RectTransform selfRect;
    public RectTransform buttons;
    public HorizontalLayoutGroup buttonsLayoutGroup;
    public GameObject playAudioButton, deleteBtn, editBtn, submitButton, thinkBtn;
    
    private bool _isShowThink = false;
    private bool _isShowAdditional = false;
    [HideInInspector]
    public DialogueEntry entry;
    
    private Coroutine _sizeCoroutine;
    
    // public override void SetData(IPageListItem item)
    // {
    //     throw new NotImplementedException();
    // }
    public override void SetData(IPageListItem item)
    {
        if(!gameObject.activeInHierarchy){return;}
        var target = (DialogueEntry) item;
        entry = target;
        string talker;
        if (entry.role == "user")
        {
            if (string.IsNullOrEmpty(CharacterManager.instance.curCharacter.userName))
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        talker = "你";
                        break;
                    case "en":
                        talker = "You";
                        break;
                    default:
                        talker = "You";
                        break;
                }
            }
            else
            {
                talker = CharacterManager.instance.curCharacter.userName;
            }

        }
        else if (entry.role == "assistant")
        {
            talker = string.IsNullOrEmpty(CharacterManager.instance.curCharacter.characterName)
                ? "Ta"
                : CharacterManager.instance.curCharacter.characterName;
        }
        else
        {
            talker = entry.role;
        }

        string time = entry.time.ToString("yy-MM-dd HH:mm");
        ChangeTalker(entry.role,entry.content);
        var str1 = ConvertMarkdownToTMP(target.content);
        dialogLineText.text = str1;
        talkerText.text = talker;
        if (talker == CharacterManager.instance.curCharacter.characterName || talker=="Ta")
        {
            headImage.sprite = CharacterManager.instance.GetHeadIcon();
            headImage.color = Color.white;
            talkerText.color =  CharacterManager.instance.curCharacter.aiNameColor;
        }
        else
        {
            headImage.sprite = null;
            headImage.color = Color.white;
            talkerText.color = new Color(0.4f,0.3f,0.23f);
        }

        if (thinkText != null && target.think is { Length: > 0 })
        {
            thinkText.text = target.think;
            thinkText.gameObject.SetActive(_isShowThink);
        }
        timeText.text = time;
        dialogLineText.gameObject.SetActive(!_isShowThink);
        SetSize();
    }

    private void ChangeTalker(string talker,string content)
    { 
        // public GameObject playAudioButton, deleteBtn, editBtn, submitButton, thinkBtn;
        if (talker == "user")
        {
            buttons.anchorMin = Vector2.up;
            buttons.anchorMax = Vector2.up;
            buttons.anchoredPosition = new Vector2(140f, -24f);
            buttonsLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            playAudioButton.SetActive(false);
            thinkBtn.SetActive(false);
            submitButton.SetActive(false);
            editBtn.SetActive(true);
            deleteBtn.SetActive(true);
            
            headRect.anchorMin = Vector2.one;
            headRect.anchorMax = Vector2.one;
            headRect.anchoredPosition = new Vector2(-37.5f, -37.5f);

            talkerText.rectTransform.anchorMin = Vector2.one;
            talkerText.rectTransform.anchorMax = Vector2.one;
            talkerText.rectTransform.anchoredPosition = new Vector2(-258f, -26f);
            talkerText.alignment = TextAlignmentOptions.Right;

            timeText.rectTransform.anchorMin = Vector2.one;
            timeText.rectTransform.anchorMax = Vector2.one;
            timeText.rectTransform.anchoredPosition = new Vector2(-258f, -48f);
            timeText.alignment = TextAlignmentOptions.Right;
            
            var dialogTextRect = dialogLineText.GetComponent<RectTransform>();
            Vector2 preferredSize = dialogLineText.textComponent.GetPreferredValues(content,dialogLineText.textComponent.rectTransform.rect.width,0);
            dialogTextRect.anchoredPosition = new Vector2(0, -76f);
            dialogTextRect.sizeDelta = new Vector2(dialogTextRect.sizeDelta.x, preferredSize.y);
            dialogTextRect.anchorMin = Vector2.up;
            dialogTextRect.anchorMax = Vector2.one;
            dialogTextRect.offsetMin = new Vector2(7, dialogTextRect.offsetMin.y);
            dialogTextRect.offsetMax = new Vector2(-7, dialogTextRect.offsetMax.y);
            dialogLineText.textComponent.alignment = TextAlignmentOptions.Right;
            
            thinkText.gameObject.SetActive(false);
        }
        else
        {
            buttons.anchorMin = Vector2.one;
            buttons.anchorMax = Vector2.one;
            buttons.anchoredPosition = new Vector2(-140f, -24f);
            buttonsLayoutGroup.childAlignment = TextAnchor.MiddleRight;
            playAudioButton.SetActive(DialogManager.instance.hasAudioEntry != null && DialogManager.instance.hasAudioEntry == entry);
            deleteBtn.SetActive(false);
            submitButton.SetActive(false);
            thinkBtn.SetActive(true);
            editBtn.SetActive(true);
            
            headRect.anchorMin = Vector2.up;
            headRect.anchorMax = Vector2.up;
            headRect.anchoredPosition = new Vector2(37.5f, -37.5f);
            
            talkerText.rectTransform.anchorMin = Vector2.up;
            talkerText.rectTransform.anchorMax = Vector2.up;
            talkerText.rectTransform.anchoredPosition = new Vector2(258f, -26f);
            talkerText.alignment = TextAlignmentOptions.Left;
            
            timeText.rectTransform.anchorMin = Vector2.up;
            timeText.rectTransform.anchorMax = Vector2.up;
            timeText.rectTransform.anchoredPosition = new Vector2(258f, -48f);
            timeText.alignment = TextAlignmentOptions.Left;
            
            var dialogTextRect = dialogLineText.GetComponent<RectTransform>();
            Vector2 preferredSize = dialogLineText.textComponent.GetPreferredValues(content,dialogLineText.textComponent.rectTransform.rect.width,0);
            dialogTextRect.anchoredPosition = new Vector2(0, -76f);
            dialogTextRect.sizeDelta = new Vector2(dialogTextRect.sizeDelta.x, preferredSize.y);
            dialogTextRect.anchorMin = Vector2.up;
            dialogTextRect.anchorMax = Vector2.one;
            dialogTextRect.offsetMin = new Vector2(7, dialogTextRect.offsetMin.y);
            dialogTextRect.offsetMax = new Vector2(-7, dialogTextRect.offsetMax.y);
            dialogLineText.textComponent.alignment = TextAlignmentOptions.Left;
            
            var thinkTextRect = thinkText.GetComponent<RectTransform>();
            thinkTextRect.anchoredPosition = new Vector2(0, -76f);
            thinkTextRect.sizeDelta = new Vector2(dialogTextRect.sizeDelta.x, 36.21f);
            thinkTextRect.anchorMin = Vector2.up;
            thinkTextRect.anchorMax = Vector2.one;
            thinkTextRect.offsetMin = new Vector2(7, dialogTextRect.offsetMin.y);
            thinkTextRect.offsetMax = new Vector2(-7, dialogTextRect.offsetMax.y);
            dialogLineText.textComponent.alignment = TextAlignmentOptions.Left;
        }
    }
    private void OnDisable()
    {
        if (_sizeCoroutine != null)
        {
            StopCoroutine(_sizeCoroutine);
        }
    }

    public void ChangeShowThink()
    {
        _isShowThink = !_isShowThink;
        if (_isShowThink)
        {
            _isShowAdditional = false;
        }
        thinkText.text = entry.think;
        thinkText.gameObject.SetActive(_isShowThink);
        dialogLineText.gameObject.SetActive(!_isShowThink);
        SetSize();
    }
    public void RemoveLine()
    {
        switch (LocalizerManager.GetCode())
        {
            case "zh-Hans":
                MessageManager.instance.ShowPropUpMessage("确认",$"确认删除这条对话以及后续对话吗？\n为保证对话结构合理性，本条对话以及下面的所有对话都会被删除。",()=>DialogManager.instance.RemoveLine(entry));
                break;
            case "en":
                MessageManager.instance.ShowPropUpMessage("Confirm",$"Are you sure you want to delete this dialogue and all following dialogues?\nTo ensure the dialogue structure remains consistent, this dialogue and all subsequent ones will be deleted.",()=>DialogManager.instance.RemoveLine(entry));
                break;
        }
    }

    public void EditLine()
    {
        dialogLineText.readOnly = false;

        submitButton.SetActive(true);
        editBtn.SetActive(false);
        if (_isShowAdditional)
        {
            if (thinkText)
            {
                thinkText.readOnly = false;
                thinkText.Select();              // 选择该输入框
                thinkText.ActivateInputField(); // 使其聚焦并可直接输入
            }
        }
        else
        {
            dialogLineText.Select();              // 选择该输入框
            dialogLineText.ActivateInputField(); // 使其聚焦并可直接输入
        }
    }

    public void PlayAudio()
    {
        TTSManager.instance.PlayLastClip();
    }
    public void SubmitEdit()
    {
        if (_isShowAdditional)
        {
            DialogManager.instance.EditLine(entry,dialogLineText.text,thinkText.text);
        }
        else
        {
            DialogManager.instance.EditLine(entry,dialogLineText.text);
        }
        dialogLineText.readOnly = true;
        thinkText.readOnly = true;
        submitButton.SetActive(false);
        editBtn.SetActive(true);
    }
    /// <summary>
    /// 解析markdown
    /// </summary>
    /// <param name="markdown"></param>
    /// <returns></returns>
    public string ConvertMarkdownToTMP(string markdown)
    {
        string result = markdown;

        // 多级标题 # ## ### #### #####
        result = Regex.Replace(result, @"^(######) (.+)$", "<size=110%><b>$2</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^(#####) (.+)$", "<size=120%><b>$2</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^(####) (.+)$", "<size=140%><b>$2</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^(###) (.+)$", "<size=160%><b>$2</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^(##) (.+)$", "<size=180%><b>$2</b></size>", RegexOptions.Multiline);
        result = Regex.Replace(result, @"^(#) (.+)$", "<size=200%><b>$2</b></size>", RegexOptions.Multiline);

        // 加粗 **
        result = Regex.Replace(result, @"\*\*(.+?)\*\*", "<b>$1</b>");

        // 斜体 *
        result = Regex.Replace(result, @"\*(.+?)\*", "<i>$1</i>");

        // 列表项 - 
        result = Regex.Replace(result, @"^- (.+)$", "• $1", RegexOptions.Multiline);

        // 水平线 ---
        result = Regex.Replace(result, @"^\s*---\s*$", "<color=#000000>————————————</color></size>", RegexOptions.Multiline);

        // 对话解析（支持中文引号 “” 和英文引号 ""）
        // 示例：“你好” → <b><color=#FF5555>“你好”</color></b>
        string dialogColor = "#294A70"; // 自定义颜色（如橙色）
        result = Regex.Replace(result, @"[“""](.+?)[”""]", $"<b><color={dialogColor}>“$1”</color></b>");

        // 换行处理
        result = result.Replace("\r\n", "\n").Replace("\r", "\n");

        return result;
    }
    public void RefreshSize()
    {
        
    }

    public override float GetHeight(IPageListItem targetData)
    {
        // return 116.21f;
        var content  = targetData as DialogueEntry;
        if (content == null) return 80;
        Vector2 preferredSize = dialogLineText.textComponent.GetPreferredValues(content.content,dialogLineText.textComponent.rectTransform.rect.width,Mathf.Infinity);
        // Debug.Log("--- Get Height ---");
        // Debug.Log(dialogLineText.textComponent.rectTransform.rect.width);
        // Debug.Log(content.content);
        // Debug.Log(preferredSize.y);
        return preferredSize.y + 80;
    }

    private void SetSize()
    {
        Vector2 preferredSize = dialogLineText.textComponent.GetPreferredValues(entry.content,dialogLineText.textComponent.rectTransform.rect.width,Mathf.Infinity);
        selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,preferredSize.y + 80f);
        // Debug.Log("--- Set Size ---");
        // Debug.Log(dialogLineText.textComponent.rectTransform.rect.width);
        // Debug.Log(entry.content);
        // Debug.Log(preferredSize.y);
    }

    public override IPageListItem GetData()
    {
        return entry;
    }

}