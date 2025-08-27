using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class DropdownLocalizer : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    // 对应每个下拉项的字符串表键名，例如 "quality-high","quality-medium",…
    public string[] optionKeys;
    public string tableName = "UI_Text";

    IEnumerator Start()
    {
        // 等待本地化系统初始化
        yield return LocalizationSettings.InitializationOperation;

        // 初始化一次选项并注册语言切换回调
        UpdateOptions();
        LocalizationSettings.SelectedLocaleChanged += locale => UpdateOptions();
    }

    void UpdateOptions()
    {
        var stringTable = LocalizationSettings.StringDatabase.GetTable(tableName);
        if (stringTable == null) return;

        // 确保 optionKeys 数量和 Dropdown.options 对齐
        for (int i = 0; i < optionKeys.Length && i < dropdown.options.Count; i++)
        {
            var entry = stringTable.GetEntry(optionKeys[i]);
            string localized = entry?.GetLocalizedString() ?? optionKeys[i];

            // 直接修改已有 OptionData 的 text，image 不动
            dropdown.options[i].text = localized;
        }

        // 刷新显示的文本
        dropdown.RefreshShownValue();
    }
}