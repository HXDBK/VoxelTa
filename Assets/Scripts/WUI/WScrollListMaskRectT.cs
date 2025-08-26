using UnityEngine.EventSystems;

namespace WUI
{
    public class WScrollListMaskRectT : UIBehaviour
    {
        public WScrollList wScrollList;
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            // wScrollList?.Init();
        }
    }
}