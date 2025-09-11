using System;
using UnityEngine;

namespace Live2D
{
    [Serializable]
    public class ModelComponent
    {
        public string componentName;
        public string sourcePath;
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