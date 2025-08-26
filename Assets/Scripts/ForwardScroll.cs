using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ForwardScroll : MonoBehaviour, IScrollHandler
{
    private IScrollHandler _parentScrollRect;

    private void Awake()
    {
        _parentScrollRect = transform.parent.GetComponentInParent<IScrollHandler>();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_parentScrollRect != null)
        {
            _parentScrollRect.OnScroll(eventData);
        }
    }
}
