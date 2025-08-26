using UnityEngine;

namespace WUI
{
    public class WButtonGroup : MonoBehaviour
    {
        public WButton[] buttons;
        public int maxSelectCount = 1;
        private int _currentSelectCount = 0;
        void Start()
        {
            if (buttons == null || buttons.Length == 0)
            {
                buttons = GetComponentsInChildren<WButton>();
            }

            foreach (WButton button in buttons)
            {
                button.group = this;
            }
        }

        public void Select(WButton button)
        {
            if (maxSelectCount == 1)
            {
                foreach (WButton buttonItem in buttons)
                {
                    buttonItem.SetSelected(buttonItem == button);
                }
            }
            else
            {
                if (_currentSelectCount >= maxSelectCount)
                {
                    return;
                }

                if (button.selected)
                {
                    button.SetSelected(false);
                    _currentSelectCount--;
                }else
                {
                    button.SetSelected(true);
                    _currentSelectCount++;
                } 
            }
        }
    }
}
