using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class MissingSpriteReplacer : EditorWindow
{
    public Sprite newSprite;

    [MenuItem("Tools/替换Missing Sprite")]
    static void Init()
    {
        GetWindow<MissingSpriteReplacer>("替换Missing Sprite");
    }

    void OnGUI()
    {
        newSprite = (Sprite)EditorGUILayout.ObjectField("新的 Sprite", newSprite, typeof(Sprite), false);

        if (GUILayout.Button("替换Missing"))
        {
            ReplaceMissingSprites();
        }
    }

    void ReplaceMissingSprites()
    {
        if (newSprite == null)
        {
            Debug.LogError("请先指定一个新的 Sprite！");
            return;
        }

        Image[] images = FindObjectsOfType<Image>(true);
        int count = 0;

        foreach (var img in images)
        {
            if (img.sprite == null || string.IsNullOrEmpty(img.sprite.name)) // null 或 Missing
            {
                img.sprite = newSprite;
                EditorUtility.SetDirty(img);
                count++;
            }
        }

        Debug.Log($"完成替换，共修复了 {count} 个 Missing Sprite。");
    }
}