using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

        private new void Awake()
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
            if (boxCollider2D == null)
            {
                boxCollider2D = GetComponent<BoxCollider2D>();
                if (boxCollider2D == null)
                {
                    boxCollider2D = gameObject.AddComponent<BoxCollider2D>();

                }
            }

            if (fitColliderToSprite)
            {
                FitColliderToSprite();
            }

            // 明确：图片模型不使用这些字段/组件
            modelData = null;
            motionCtrl = null;
            needPlayQueue = null;
            isBreath = false;
            isBlink = false;
            isLookMouse = false;

            // 如果基类里曾创建过这些对象，统一禁用/隐藏
            if (lookAtTarget != null) lookAtTarget.gameObject.SetActive(false);
            if (autoLookAtCenter != null) autoLookAtCenter.gameObject.SetActive(false);
            if (harmonicMotionController != null) harmonicMotionController.enabled = false;
            if (autoBlink != null) autoBlink.enabled = false;
            if (mouthController != null) mouthController.enabled = false;
            if (autoLookAt != null) autoLookAt.enabled = false;
            
            autoLookAtCenter = new GameObject("autoLookAtCenter").transform;
            autoLookAtCenter.parent = transform;
        }

        public override void Start() { /* 空实现：不需要队列或动画控制 */ }
        public override void Update() { /* 空实现：不需要轮询 */ }

        // —— 基类公共接口的安全空实现 —— //
        public override void PlayMotion(AnimationClip target) { /* no-op */ }
        public override void PlayMotions(List<AnimationClip> targets) { /* no-op */ }

        public override void SetBreath(bool target) { isBreath = false; /* no-op */ }
        public override void SetBlink(bool target) { isBlink = false; /* no-op */ }
        public override void SetLookMouse(bool target)
        {
            isLookMouse = false; // 静态图不做看向
            if (lookAtTarget != null) lookAtTarget.gameObject.SetActive(false);
            if (autoLookAt != null) autoLookAt.enabled = false;
        }

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
        public override void SetExpression(Live2D.Cubism.Framework.Json.CubismExp3Json _json) { /* no-op */ }
        public override void CancelExpression(Live2D.Cubism.Framework.Json.CubismExp3Json _json) { /* no-op */ }
        public override void ClearAllExpressions() { /* no-op */ }

        // —— 便捷：运行时换图 —— //
        public void SetSprite(Sprite newSprite, bool autoFitCollider = true)
        {
            sprite = newSprite;
            if (spriteRenderer != null) spriteRenderer.sprite = newSprite;
            if (autoFitCollider) FitColliderToSprite();
        }

        private void FitColliderToSprite()
        {
            if (boxCollider2D == null || spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            var bounds = spriteRenderer.sprite.bounds;
            boxCollider2D.size = bounds.size;
            boxCollider2D.offset = bounds.center;
        }
    }
}
