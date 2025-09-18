using System;
using System.Collections.Generic;
using Live2D;
using UnityEngine;
using UnityEngine.Rendering;

namespace Model
{
    public class CustomModel : GeneralModel
    {
        public CustomModelData customModelData;
        public SortingGroup SortingGroup
        {
            get
            {
                if (_sortingGroup == null)
                {
                    _sortingGroup = GetComponent<SortingGroup>();
                }
                if (_sortingGroup == null)
                {
                    _sortingGroup = gameObject.AddComponent<SortingGroup>();
                }
                return _sortingGroup;
            }
        }

        private SortingGroup _sortingGroup;
        public void SetModelData(CustomModelData data)
        {
            customModelData = data;
            modelComponents = customModelData.components;
        }
        /// <summary>
        /// 可选传入一个根节点；不传就挂在当前组件所在对象的 transform 下
        /// </summary>
        public override void LoadComponents()
        {
            var rootParent = transform;
            modelComponents = customModelData.components;
            // 1) 先创建所有节点，建立 id -> Transform 的映射
            var idToTransform = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var component in modelComponents)
            {
                var goName = string.IsNullOrEmpty(component.componentName)
                    ? $"Comp_{component.id}"
                    : component.componentName;

                var go = new GameObject(goName);
                var spriteRenderer = go.AddComponent<SpriteRenderer>();

                // 贴图与颜色
                GameManager.instance.LoadSprite(component.sourcePath, spriteRenderer);
                spriteRenderer.color = component.color;

                // 记录回组件
                component.actor = spriteRenderer;

                idToTransform[component.id] = go.transform;
            }

            // 2) 统一设置父子关系并恢复本地变换
            foreach (var component in modelComponents)
            {
                var t = idToTransform[component.id];

                // 解析父节点
                Transform parentT = rootParent;
                if (!string.IsNullOrEmpty(component.parentId))
                {
                    if (component.parentId == component.id)
                    {
                        Debug.LogWarning($"[CustomModel] 组件 {component.componentName}({component.id}) 的 parentId 指向自身，已挂到根。");
                    }
                    else if (idToTransform.TryGetValue(component.parentId, out var foundParent))
                    {
                        parentT = foundParent;
                    }
                    else
                    {
                        Debug.LogWarning($"[CustomModel] 未找到组件 {component.componentName}({component.id}) 的父 {component.parentId}，已挂到根。");
                    }
                }

                // 先设父，再恢复 local TRS（因为保存的是 local）
                t.SetParent(parentT, false);
                t.localPosition = component.position;
                t.localEulerAngles = component.rotation;
                t.localScale = component.scale;
            }
        }

        public override void SetLayer(int layer)
        {
            SortingGroup.sortingOrder = layer;
        }
        /// <summary>
        /// 返回模型的边界
        /// </summary>
        public override Bounds GetBounds()
        {
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();

            if (sprites.Length == 0)
            {
                // 没有 SpriteRenderer，就返回一个零大小的包围盒
                return new Bounds(transform.position, Vector3.zero);
            }

            // 以第一个 SpriteRenderer 的 bounds 为初始值
            Bounds bounds = sprites[0].bounds;

            // 逐个合并
            for (int i = 1; i < sprites.Length; i++)
            {
                bounds.Encapsulate(sprites[i].bounds);
            }

            return bounds;
        }
    }
}