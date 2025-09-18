using System;
using Character;
using TMPro;
using UnityEngine;
using WUI;

namespace Model
{
    public class ComponentLine : PageLineItem
    {
        private ModelComponent _modelComponent;
        public TMP_Text componentNameText;
        public TMP_Text componentParentNameText;
        public WButton self;
        public GameObject highlightObj;
        private void Start()
        {
            self.onPointerClick.AddListener(() =>
            {
                CharacterManager.instance.componentPanel.ShowComponentDetail(_modelComponent);
            });
        }

        public override void Highlight(bool highlight)
        {
            highlightObj.SetActive(highlight);
        }

        public override IPageListItem GetData()
        {
            throw new System.NotImplementedException();
        }

        public override void SetData(IPageListItem item)
        {
            _modelComponent = item as ModelComponent;
            if (_modelComponent == null) return;
            componentNameText.text = _modelComponent.componentName;
        }
    }
}
