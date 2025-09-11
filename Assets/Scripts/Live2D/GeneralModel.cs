using System;
 using System.Text.RegularExpressions;
 using Live2D.Cubism.Framework.Json;
 using UnityEngine;
 using Object = System.Object;
 
 namespace Live2D
 {
     public class GeneralModel : MonoBehaviour
     {
         public CharacterData characterData;
         public Collider2D collider2d;
         
         /// <summary>
         /// 设置层级
         /// </summary>
         /// <param name="layer"></param>
         public virtual void SetLayer(int layer)
         {
             
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
         public virtual void OnLoadSuccess(CharacterData character)
         {
             
         }
         /// <summary>
         /// 检测对话中有没有对应的关键字
         /// </summary>
         /// <param name="entry"></param>
         /// <returns></returns>
         public virtual bool CheckExp(DialogueEntry entry)
         {
             return true;
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