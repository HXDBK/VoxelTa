using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using WUI;

public class RingMenu : UIPanel
{
    public RingMenuItem[] items;

    public int itemCount = 6;
    public float innerRadius = 50f;
    public float outerRadius = 100f;
    public float startAngle = -90f; // 顶部开始
    public float spiralAngleTurns = 1f; // 旋转圈数（正 = 顺时针，负 = 逆时针）
    public float spiralDuration = 0.5f;

    public List<Vector2> innerPositions = new List<Vector2>();
    public List<Vector2> outerPositions = new List<Vector2>();

    public RingMenuItem targetButton;
    public override void Show()
    {
        isShow = true;
        targetCanvasGroup.DOComplete();
        targetCanvasGroup.alpha = 0;
        targetCanvasGroup.gameObject.SetActive(true);
        targetCanvasGroup.DOFade(1, 0.2f);

        targetImage.transform.DOComplete();
        targetImage.transform.localScale = Vector3.zero;
        targetImage.transform.DOScale(1, 0.2f);

        CalculateMenuPositions();
        float angleStep = 360f / itemCount;
        float spiralAngleOffset = spiralAngleTurns * Mathf.PI * 2f;

        for (int i = 0; i < itemCount; i++)
        {
            RectTransform rect = items[i].GetComponent<RectTransform>();
            rect.DOComplete();
            rect.anchoredPosition = innerPositions[i];

            float baseAngleDeg = startAngle + angleStep * i;
            float baseAngleRad = baseAngleDeg * Mathf.Deg2Rad;

            DOVirtual.Float(0, 1, spiralDuration, t =>
            {
                float angle = baseAngleRad + (1 - t) * spiralAngleOffset;
                float radius = Mathf.Lerp(innerRadius, outerRadius, t);
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                rect.anchoredPosition = pos;
            }).SetEase(Ease.OutBack);
        }
    }
    public override void Hide()
    {
        isShow = false;
        if (targetButton)
        {
            targetButton.itemEvent?.Invoke();
            targetButton = null;
        }
        float angleStep = 360f / itemCount;
        float spiralAngleOffset = spiralAngleTurns * Mathf.PI * 2f;

        int finishedCount = 0;

        for (int i = 0; i < itemCount; i++)
        {
            RectTransform rect = items[i].GetComponent<RectTransform>();
            rect.DOComplete();

            float baseAngleDeg = startAngle + angleStep * i;
            float baseAngleRad = baseAngleDeg * Mathf.Deg2Rad;

            DOVirtual.Float(0, 1, spiralDuration, t =>
            {
                float angle = baseAngleRad + t * spiralAngleOffset;
                float radius = Mathf.Lerp(outerRadius, innerRadius, t);
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                rect.anchoredPosition = pos;
            }).SetEase(Ease.InCubic);
            targetCanvasGroup.DOFade(0, 0.2f).OnComplete(() =>
            {
                targetCanvasGroup.gameObject.SetActive(false);
            });
        }
    }
    
    void CalculateMenuPositions()
    {
        innerPositions.Clear();
        outerPositions.Clear();

        float angleStep = 360f / itemCount;

        for (int i = 0; i < itemCount; i++)
        {
            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 innerPos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * innerRadius;
            Vector2 outerPos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * outerRadius;

            innerPositions.Add(innerPos);
            outerPositions.Add(outerPos);

            // Debug.Log($"Item {i}: Inner = {innerPos}, Outer = {outerPos}");
        }
    }
}
