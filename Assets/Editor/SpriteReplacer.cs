using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class SpriteReplacer : EditorWindow
{
    public Sprite targetSprite;    // 原始 sprite
    public Sprite newSprite;       // 替换后的 sprite

    [MenuItem("Tools/批量替换Sprite")]
    static void Init()
    {
        GetWindow<SpriteReplacer>("批量替换Sprite");
    }

    void OnGUI()
    {
        targetSprite = (Sprite)EditorGUILayout.ObjectField("要替换的 Sprite", targetSprite, typeof(Sprite), false);
        newSprite = (Sprite)EditorGUILayout.ObjectField("新的 Sprite", newSprite, typeof(Sprite), false);

        if (GUILayout.Button("替换"))
        {
            ReplaceSprites();
        }
    }

    void ReplaceSprites()
    {
        Image[] images = FindObjectsOfType<Image>(true); // true 表示包含未激活物体
        int count = 0;
        foreach (Image img in images)
        {
            if (img.sprite == targetSprite)
            {
                img.sprite = newSprite;
                EditorUtility.SetDirty(img);
                count++;
            }
        }
        Debug.Log($"替换完成，共替换了 {count} 个 Image");
    }
}