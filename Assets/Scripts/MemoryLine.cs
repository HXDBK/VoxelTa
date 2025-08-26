using System.Collections;
using System.Collections.Generic;
using Character;
using TMPro;
using UnityEngine ;
using WUI;

public class MemoryLine : PageLineItem
{
    public TMP_Text titleText;
    public Memory data;
    public void SetData(Memory target)
    {
        data = target;
        titleText.text = data.title;
    }
    public void ShowDetail()
    {
        CharacterManager.instance.ShowMemory(this);
    }

    public override IPageListItem GetData()
    {
        return data;
    }

    public override void SetData(IPageListItem item)
    {
        data = (item as Memory);
        if(data == null) return;
        titleText.text = data.title;
    }

    public void RemoveSelf()
    {
        
    }
}
