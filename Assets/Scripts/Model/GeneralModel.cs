using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;
 
 namespace Model
 {
     public class GeneralModel : MonoBehaviour
     {
         public CharacterData characterData;
         public BoxCollider2D boxCollider2d;
         public List<ModelComponent> modelComponents = new ();
         /// <summary>
         /// 设置层级
         /// </summary>
         /// <param name="layer"></param>
         public virtual void SetLayer(int layer)
         {
             
         }
         
         /// <summary>
         /// 加载 组件
         /// </summary>
         public virtual void LoadComponents()
         {
            
         }
         
         /// <summary>
         /// 新增组件
         /// </summary>
         public virtual ModelComponent AddModelComponent(SpriteRenderer target)
         {
             var newComponent = new ModelComponent
             {
                 actor = target,
                 id = target.name,
                 parentId = null,
                 componentName = target.name,
                 sourcePath = "",
                 color = target.color,
                 position = target.transform.localPosition,
                 rotation = target.transform.localEulerAngles,
             };
             if (target.transform.parent && target.transform.parent.TryGetComponent<ModelComponent>(out var parentComponent))
             {
                 newComponent.parentId = parentComponent.id;
             }
             modelComponents.Add(newComponent);
             return newComponent;
         }
         /// <summary>
         /// 移除组件
         /// </summary>
         public virtual void RemoveModelComponent(ModelComponent target)
         {
             if (modelComponents.Contains(target))
             {
                 modelComponents.Remove(target);
             }
             Destroy(target.actor.gameObject);;
         }
         
         /// <summary>
         /// 设置颜色
         /// </summary>
         /// <param name="targetColor"></param>
         public virtual void SetColor(Color targetColor)
         {
             
         }
         /// <summary>
         /// 加载成功后触发
         /// </summary>
         public virtual void OnLoadSuccess(CharacterData character,ModelLoader loader)
         {
             characterData = character;
         }
         /// <summary>
         /// 检测对话中有没有对应的关键字
         /// </summary>
         /// <param name="entry"></param>
         /// <returns></returns>
         public virtual void CheckExp(DialogueEntry entry)
         {
             
         }
         
         public virtual void CheckMotion(DialogueEntry entry)
         {
             
         }
         /// <summary>
         /// 设置表情
         /// </summary>
         /// <param name="expression"></param>
         public virtual void SetExpression(Object expression)
         {
                
         }
         /// <summary>
         /// 取消表情
         /// </summary>
         /// <param name="expression"></param>
         public virtual void CancelExpression(Object expression)
         {
            
         }
         /// <summary>
         /// 设置动画
         /// </summary>
         /// <param name="motion"></param>
         public virtual void PlayMotion(Object motion)
         {
             
         }
         /// <summary>
         /// 清除所有表情
         /// </summary>
         public virtual void ClearAllExpressions()
         {
 
         }
         /// <summary>
         /// 保存数据 （表情，参数）
         /// </summary>
         public virtual void SaveData()
         {
             
         }
         /// <summary>
         /// 重置模型相关数据
         /// </summary>
         /// <param name="character"></param>
         public virtual void DoResetModel(CharacterData character)
         {
             
         }
         /// <summary>
         /// 返回模型的边界
         /// </summary>
         public virtual Bounds GetBounds()
         {
             return new Bounds(transform.position, Vector3.zero);
         }
         /// <summary>
         /// 假装说话
         /// </summary>
         /// <param name="durtion"></param>
         public virtual void FakeTalk(float durtion)
         {
             
         }
     }
 }