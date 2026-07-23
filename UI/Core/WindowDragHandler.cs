using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    /// <summary>
    /// Attach to the menu header. Lets the whole window be dragged by its title bar.
    ///
    /// Moves MenuWindow.RootRT live during the drag - cheap, just a Vector2 write - and only
    /// pushes the final position into MenuCustomiser (which persists to MenuLayout.json) once
    /// the drag ends. Writing the save file every frame is a known lag source elsewhere in this
    /// project (BikeStats saves are explicitly throttled for the same reason), so this follows
    /// the same rule: live movement each frame, disk write only on release.
    ///
    /// First grab of a session re-anchors the window to a fixed top-left pivot (same convention
    /// MenuCustomiser's "Custom" position mode uses) without visually moving it, via a standard
    /// Unity world-to-local RectTransform conversion, so switching from Centre/Top Left/Top Right
    /// into free dragging never causes a jump.
    /// </summary>
    public class WindowDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);

        private RectTransform _target;
        private RectTransform _parent;
        private Canvas _canvas;
        private Vector2 _pointerStart;
        private Vector2 _anchoredStart;

        public void OnBeginDrag(PointerEventData eventData)
        {
            try
            {
                _target = MenuWindow.RootRT;
                if ((object)_target == null) return;

                _parent = _target.parent as RectTransform;
                _canvas = _target.GetComponentInParent<Canvas>();

                SnapToTopLeftPivotPreservingPosition();

                _pointerStart = eventData.position;
                _anchoredStart = _target.anchoredPosition;
            }
            catch (System.Exception ex) { MelonLogger.Error("[WindowDrag] OnBeginDrag: " + ex.Message); }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if ((object)_target == null) return;
            try
            {
                float scale = ((object)_canvas != null && _canvas.scaleFactor > 0f) ? _canvas.scaleFactor : 1f;
                Vector2 delta = (eventData.position - _pointerStart) / scale;
                _target.anchoredPosition = ClampToScreen(_anchoredStart + delta);
            }
            catch (System.Exception ex) { MelonLogger.Error("[WindowDrag] OnDrag: " + ex.Message); }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if ((object)_target == null) return;
            try
            {
                MenuCustomiser.SetCustomPosition(_target.anchoredPosition.x, _target.anchoredPosition.y);
            }
            catch (System.Exception ex) { MelonLogger.Error("[WindowDrag] OnEndDrag: " + ex.Message); }
        }

        private void SnapToTopLeftPivotPreservingPosition()
        {
            if ((object)_parent == null || (object)_canvas == null) return;
            if (_target.anchorMin == TopLeft && _target.anchorMax == TopLeft && _target.pivot == TopLeft)
                return; // already in Custom mode from an earlier drag this session

            Vector3[] corners = new Vector3[4];
            _target.GetWorldCorners(corners); // [1] = top-left corner in world space

            Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, screenPoint, cam, out localPoint);

            _target.anchorMin = TopLeft;
            _target.anchorMax = TopLeft;
            _target.pivot = TopLeft;
            _target.anchoredPosition = localPoint;
        }

        // Keeps at least a grabbable sliver of the header on-screen at all times.
        // anchorMin=anchorMax=pivot=(0,1): anchoredPosition.x=0 aligns the window's left edge
        // with the parent's left edge (+x moves right); anchoredPosition.y=0 aligns the window's
        // top edge with the parent's top edge (+y moves further up/off-screen, -y moves down).
        private Vector2 ClampToScreen(Vector2 pos)
        {
            if ((object)_parent == null) return pos;

            float parentW = _parent.rect.width;
            float parentH = _parent.rect.height;
            float winW = _target.rect.width;

            const float margin = 80f;

            float minX = -(winW - margin);
            float maxX = parentW - margin;
            float minY = -(parentH - margin);
            float maxY = 0f;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            return pos;
        }
    }
}
