using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WUI;

public class RingMenuItem : WButton
{
    public RingMenu ringMenu;
    public UnityEvent itemEvent;
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        ringMenu.targetButton = this;
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        ringMenu.targetButton = null;
    }
}
