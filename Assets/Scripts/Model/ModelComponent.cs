using System;
using UnityEngine;
using WUI;

namespace Model
{
    [Serializable]
    public class ModelComponent : IPageListItem
    {
        public string id = Guid.NewGuid().ToString("N");
        public string componentName;
        public string sourcePath;
        public string parentId = "";
        
        public Vector3 position;
        public Vector3 scale;
        public Vector3 rotation;
        public Color color;
        [NonSerialized]
        public SpriteRenderer actor;

        public void Save()
        {
            if (actor == null)
            {
                componentName = "";
                sourcePath = "";
                position = Vector3.zero;
                scale = Vector3.one;
                rotation = Vector3.zero;
                color = Color.white;
            }
            else
            {
                componentName = actor.name;
                sourcePath = actor.sprite.texture.name;
                position = actor.transform.localPosition;
                scale = actor.transform.localScale;
                rotation = actor.transform.localEulerAngles;
                color = actor.color;
            }
        }
        
    }
}