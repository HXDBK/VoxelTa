using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WUI;

public class AudioIcon : UIPanel
{
    public Image backgroundImage;
    public Color loadingColor,playColor,errorColor;
    public GameObject loading;
    public Animator iconAnimator;
    public RectTransform iconRect;
    
    private Coroutine _hideCoroutine;
    
    public void Loading()
    {
        StopAllCoroutines();
        Show();
        backgroundImage.color = loadingColor;
        loading.SetActive(true);
        iconRect.anchoredPosition = new Vector2(-30, iconRect.anchoredPosition.y);
        iconAnimator.Play("loading");
    }
    public void Play()
    {
        StopAllCoroutines();
        Show();
        backgroundImage.color = playColor;
        loading.SetActive(false);
        iconRect.anchoredPosition = new Vector2(0, iconRect.anchoredPosition.y);
        iconAnimator.Play("play");
    }
    public void LoadingAndPlay()
    {
        StopAllCoroutines();
        Show();
        backgroundImage.color = playColor;
        loading.SetActive(true);
        iconRect.anchoredPosition = new Vector2(-30, iconRect.anchoredPosition.y);
        iconAnimator.Play("play");
    }
    public void Error()
    {
        StopAllCoroutines();
        Show();
        backgroundImage.color = errorColor;
        loading.SetActive(false);
        iconRect.anchoredPosition = new Vector2(30, iconRect.anchoredPosition.y);
        StartCoroutine(HideSelf());
    }
    public void Stop()
    {
        StopAllCoroutines();
        if (!isActiveAndEnabled) return;     // 或者走同步清理
        // Show();
        loading.SetActive(false);
        iconRect.anchoredPosition = new Vector2(0, iconRect.anchoredPosition.y);
        iconAnimator.Play("idle");
        StartCoroutine(HideSelf());
    }
    
    IEnumerator HideSelf()
    {
        yield return new WaitForSeconds(3);
        Hide();
    }
}
