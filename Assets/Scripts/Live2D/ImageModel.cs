using System.Collections.Generic;
using Model;
using UnityEngine;
using UnityEngine.Rendering;
using Object = System.Object;

namespace Live2D
{
    /// <summary>
    /// 极简图片模型：仅显示一张静态 Sprite。
    /// 兼容 Live2DController 的公共接口，但所有动作相关方法均为空实现。
    /// </summary>
    public class ImageModel : Live2DController
    {
        [Header("Image")]
        public SpriteRenderer spriteRenderer;
        public Sprite sprite;
        public bool fitColliderToSprite = true;
        
        private void Awake()
        {
            // 刻意不调 base.Awake()，避免 Cubism 组件的查找与启用
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer == null)
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            // 赋图
            if (sprite != null)
                spriteRenderer.sprite = sprite;

            // SortingGroup（层级）
            if (sortingGroup == null)
            {
                sortingGroup = GetComponent<SortingGroup>();
                if (sortingGroup == null)
                {
                    sortingGroup = gameObject.AddComponent<SortingGroup>();
                }
            }

            // 碰撞盒（可选）
            if (boxCollider2d == null)
            {
                boxCollider2d = GetComponent<BoxCollider2D>();
                if (boxCollider2d == null)
                {
                    boxCollider2d = gameObject.AddComponent<BoxCollider2D>();

                }
            }

            if (fitColliderToSprite)
            {
                FitColliderToSprite();
            }
            
        }
        public override void SetBreath(bool target)
        {
            
        }
        public override void SetBlink(bool target)
        {
            
        }
        public override void SetLookMouse(bool target)
        {

        }
        public override Bounds GetBounds()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                return new Bounds(Vector3.zero, Vector3.zero);
            return spriteRenderer.bounds;
        }

        // —— 基类公共接口的安全空实现 —— //

        public override void SetLayer(int layer)
        {
            if (sortingGroup == null)
                sortingGroup = GetComponent<SortingGroup>() ?? gameObject.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder = layer;
        }

        public override void SetColor(Color target)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = target;
        }

        // Cubism 表情接口彻底置空
        public override void SetExpression(Object expression)
        {
            base.SetExpression(expression);
        }

        public override void ClearAllExpressions() { /* no-op */ }

        public override void FakeTalk(float duration) {/* no-op */ }

        // —— 便捷：运行时换图 —— //
        public void SetSprite(Sprite newSprite, bool autoFitCollider = true)
        {
            sprite = newSprite;
            if (spriteRenderer != null) spriteRenderer.sprite = newSprite;
            if (autoFitCollider) FitColliderToSprite();
        }
        public override void OnLoadSuccess(CharacterData character,ModelLoader loader)
        {
            characterData = character;
        }
        private void FitColliderToSprite()
        {
            if (boxCollider2d == null || spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            var bounds = spriteRenderer.sprite.bounds;
            boxCollider2d.size = bounds.size;
            boxCollider2d.offset = bounds.center;
        }
    }
}
