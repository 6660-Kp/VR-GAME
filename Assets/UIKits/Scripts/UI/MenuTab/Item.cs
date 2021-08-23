using UnityEngine;
using UnityEngine.UI;

namespace VRUiKits.Utils
{
    public class Item : MonoBehaviour
    {
        public Button button;
        public delegate void OnItemSelectedHandler(Item item);
        public event OnItemSelectedHandler OnItemSelected;
        protected Color normalColor; // record the current normal color of the button
        protected Color highlightedColor;

        void Awake()
        {
            if (null == button && null != GetComponent<Button>())
            {
                button = GetComponent<Button>();
            }
            else
            {
                Debug.LogError("Item requires button to be assigned");
            }

            normalColor = button.colors.normalColor;
            highlightedColor = button.colors.highlightedColor;

            button.onClick.AddListener(() =>
            {
                if (null != OnItemSelected)
                {
                    OnItemSelected(this);
                }
            });
        }

        public virtual void Activate()
        {
            //Changes the button's Normal color to the new color.
            ColorBlock cb = button.colors;
            cb.normalColor = button.colors.pressedColor;
            cb.highlightedColor = button.colors.pressedColor;
            button.colors = cb;
        }

        public virtual void Deactivate()
        {
            //Changes the button's Normal color to the original color.
            ColorBlock cb = button.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = highlightedColor;
            button.colors = cb;
        }
    }
}
