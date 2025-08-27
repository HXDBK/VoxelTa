using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Other;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WUI;

public class MessageManager : MonoBehaviour
{
    public static MessageManager instance;
    //弹出
    public UIPanel propUpPanel;
    public TMP_Text titleText;
    public TMP_Text propUpMessageText;
    public WButton okButton;
    public WButton noButton;
    public Image okButtonImage;
    //消息
    public TMP_Text messageText;
    public UIPanel messagePanel;
    public Image iconImage;
    public Sprite infoSprite;
    public Sprite warningSprite;
    public Sprite successSprite;
    
    private Tween _hideTween;

    private void Awake()
    {
        instance = this;
    }
    public void ShowPropUpMessage(string title,string message,Action onOk,Action onNo = null,string okBtnStr = "确认",string noBtnStr = "取消")
    {
        titleText.text = title;
        propUpMessageText.text = message;
        if (okBtnStr == "确认")
        {
            if (LocalizerManager.GetCode() == "zh-Hans")
            {
                okButton.Text.text = okBtnStr;
            }
            else 
            {
                okButton.Text.text = "Confirm";
            }
        }

        if (noBtnStr == "取消")
        {
            if (LocalizerManager.GetCode() == "zh-Hans")
            {
                noButton.Text.text = noBtnStr;
            }
            else 
            {
                noButton.Text.text = "Cancel";
            }
        }


        noButton.onPointerClick.RemoveAllListeners();
        if (onNo == null)
        {
            noButton.onPointerClick.AddListener(propUpPanel.Hide);
        }
        else
        {
            noButton.onPointerClick.AddListener(()=>
            {
                onNo?.Invoke();
                propUpPanel.Hide();
            });
        }
        okButton.onPointerClick.RemoveAllListeners();
        okButton.onPointerClick.AddListener(()=>
        {
            onOk?.Invoke();
            propUpPanel.Hide();
        });
        propUpPanel.Show();
    }
    public void ShowMessage(string message, MessageType messageType = MessageType.Info)
    {
        message = LocalizerManager.GetValue(message);
        switch (messageType)
        {
            case MessageType.Info:
                iconImage.sprite = infoSprite;
                iconImage.color = MyColor.Blue;
                break;
            case MessageType.Warning:
                iconImage.sprite = warningSprite;
                iconImage.color = MyColor.Red;
                break;
            case MessageType.Success:
                iconImage.sprite = successSprite;
                iconImage.color = MyColor.Green;
                break;
        }
        messageText.text = message;
        messagePanel.RectTransform.DOComplete();
        if (_hideTween != null && _hideTween.IsActive())
        {
            _hideTween.Complete();
        }
        messagePanel.RectTransform.DOAnchorPosY(-100, 0.5f).SetEase(Ease.OutBack);
        _hideTween = DOVirtual.DelayedCall(2f, () => {
            messagePanel.RectTransform.DOAnchorPosY(100, 0.5f).SetEase(Ease.InBack);
        });
    }
}

public enum MessageType
{
    Info,
    Warning,
    Success,
}
