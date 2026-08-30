using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItemSpawnerPlus
{
    // per-tile pointer highlight that writes the Image colour directly, so a tile
    // always renders its real colour (a Button ColorTint flashes white for a frame
    // the first time the menu is shown)
    internal class TileHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        internal Image Target;
        internal Color Normal;
        internal Color Hover;
        internal Color Press;

        private bool _over;
        private bool _down;

        public void OnPointerEnter(PointerEventData e) { _over = true; Apply(); }
        public void OnPointerExit(PointerEventData e) { _over = false; _down = false; Apply(); }
        public void OnPointerDown(PointerEventData e) { _down = true; Apply(); }
        public void OnPointerUp(PointerEventData e) { _down = false; Apply(); }

        // a search that hides the tile mid-hover never sends OnPointerExit
        private void OnDisable() { _over = false; _down = false; Apply(); }

        internal void Apply()
        {
            if (Target != null) Target.color = _down ? Press : _over ? Hover : Normal;
        }
    }
}
