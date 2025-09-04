using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LittleMessage : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public string tip;
    public Vector3 offset;
    public bool isShow;

    public void OnPointerEnter(PointerEventData eventData)
    {
        MessageManager.instance.ShowLittleTip(tip,transform.position+offset);
        isShow = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MessageManager.instance.HideLittleTip();
        isShow = false;
    }

    private void OnDisable()
    {
        if (isShow)
        {
            MessageManager.instance.HideLittleTip();
        }
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 worldOffsetPosition = transform.position + offset;

        // 画线从对象指向偏移位置
        Gizmos.DrawLine(transform.position, worldOffsetPosition);

        // 在偏移位置画一个小球
        Gizmos.DrawSphere(worldOffsetPosition, 0.05f);
    }
#endif
}