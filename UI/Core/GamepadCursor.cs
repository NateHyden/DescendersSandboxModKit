using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    /// <summary>
    /// Virtual mouse cursor driven by the left stick while the menu is open. A (Action1)
    /// fires the same click event a real mouse click would, at whatever the cursor is over -
    /// so every existing button, toggle, and stepper works exactly as it already does with a
    /// mouse, with no separate navigation logic needed per row type.
    ///
    /// Auto-switches between mouse and gamepad: moving the stick shows/activates the cursor,
    /// moving the real mouse afterward hides it again.
    ///
    /// Only fires pointerClickHandler, deliberately not pointerDown/pointerUp - Selectable's
    /// own OnPointerDown auto-selects whatever it's called on, and once something is Selected,
    /// Unity's own built-in EventSystem input module starts independently reading the exact
    /// same physical stick/button for its native gamepad navigation and Submit handling. That
    /// caused every click to double-fire (on then instantly off) and the stick to also drive
    /// Unity's own neighbor-to-neighbor selection jump, fighting this cursor. Also explicitly
    /// clears selection every frame while active as a second line of defence against the same
    /// problem creeping back in from anywhere else.
    /// </summary>
    public class GamepadCursor : MonoBehaviour
    {
        private const float CursorSpeed = 950f; // canvas units/sec at full stick deflection
        private const float Deadzone = 0.2f;

        private RectTransform _cursorRT;
        private Image _cursorImg;
        private GraphicRaycaster _raycaster;
        private Canvas _canvas;
        private Vector2 _pos;
        private Vector3 _lastMousePos;
        private bool _gamepadActive;

        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private GameObject _lastHover;

        public void Init(GraphicRaycaster raycaster, Canvas canvas)
        {
            try
            {
                _raycaster = raycaster;
                _canvas = canvas;

                // Own overlay canvas with a higher sortingOrder, so the cursor draws on top
                // of the entire menu regardless of sibling order - it's created before the
                // menu's "root" panel and everything inside it.
                var cursorCanvasGO = new GameObject("GamepadCursorCanvas");
                cursorCanvasGO.transform.SetParent(canvas.transform, false);
                var cursorCanvas = cursorCanvasGO.AddComponent<Canvas>();
                cursorCanvas.overrideSorting = true;
                cursorCanvas.sortingOrder = canvas.sortingOrder + 1;
                UIHelpers.Fill(UIHelpers.RT(cursorCanvasGO));

                var go = UIHelpers.Panel("GamepadCursor", cursorCanvasGO.transform, new Color(1f, 0.92f, 0f, 1f), UIHelpers.BtnSp);
                _cursorImg = go.GetComponent<Image>();
                _cursorImg.raycastTarget = false; // never blocks its own raycast

                _cursorRT = UIHelpers.RT(go);
                _cursorRT.sizeDelta = new Vector2(18, 18);
                _cursorRT.anchorMin = new Vector2(0.5f, 0.5f);
                _cursorRT.anchorMax = new Vector2(0.5f, 0.5f);
                _cursorRT.pivot = new Vector2(0.5f, 0.5f);

                _pos = Vector2.zero;
                _cursorRT.anchoredPosition = _pos;
                _lastMousePos = Input.mousePosition;
                go.SetActive(false);
            }
            catch (Exception ex) { MelonLogger.Error("[GamepadCursor] Init: " + ex.Message); }
        }

        void Update()
        {
            if ((object)_cursorRT == null) return;
            try
            {
                InControl.InputDevice dev = InControl.InputManager.ActiveDevice;
                float rawX = ((object)dev != null) ? (float)dev.LeftStick.X : 0f;
                float rawY = ((object)dev != null) ? (float)dev.LeftStick.Y : 0f;
                float sx = (Mathf.Abs(rawX) < Deadzone) ? 0f : rawX;
                float sy = (Mathf.Abs(rawY) < Deadzone) ? 0f : rawY;

                Vector3 mouseNow = Input.mousePosition;
                bool mouseMoved = (mouseNow - _lastMousePos).sqrMagnitude > 4f;
                _lastMousePos = mouseNow;

                if (sx != 0f || sy != 0f) _gamepadActive = true;
                else if (mouseMoved) _gamepadActive = false;

                if (_cursorImg.gameObject.activeSelf != _gamepadActive)
                    _cursorImg.gameObject.SetActive(_gamepadActive);

                if (!_gamepadActive) return;

                // Belt-and-suspenders: keep Unity's own EventSystem selection empty every
                // frame this is active, so its native Submit/navigate handling never has a
                // target to act on independently of this cursor.
                if ((object)EventSystem.current != null && (object)EventSystem.current.currentSelectedGameObject != null)
                    EventSystem.current.SetSelectedGameObject(null);

                if (sx != 0f || sy != 0f)
                {
                    float scale = ((object)_canvas != null && _canvas.scaleFactor > 0f) ? _canvas.scaleFactor : 1f;
                    _pos += new Vector2(sx, sy) * (CursorSpeed / scale) * Time.unscaledDeltaTime;

                    RectTransform parentRT = _cursorRT.parent as RectTransform;
                    if ((object)parentRT != null)
                    {
                        float halfW = parentRT.rect.width * 0.5f;
                        float halfH = parentRT.rect.height * 0.5f;
                        _pos.x = Mathf.Clamp(_pos.x, -halfW, halfW);
                        _pos.y = Mathf.Clamp(_pos.y, -halfH, halfH);
                    }
                    _cursorRT.anchoredPosition = _pos;
                }

                UpdateHover();

                if ((object)dev != null && dev.Action1.WasPressed) SimulateClick();
            }
            catch (Exception ex) { MelonLogger.Error("[GamepadCursor] Update: " + ex.Message); }
        }

        private PointerEventData BuildPointerData()
        {
            Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, _cursorRT.position);
            return new PointerEventData(EventSystem.current) { position = screenPos };
        }

        private GameObject RaycastTopHit()
        {
            if ((object)_raycaster == null) return null;
            _hits.Clear();
            _raycaster.Raycast(BuildPointerData(), _hits);
            for (int i = 0; i < _hits.Count; i++)
            {
                if ((object)_hits[i].gameObject != null)
                    return _hits[i].gameObject;
            }
            return null;
        }

        private void UpdateHover()
        {
            GameObject rawHit = RaycastTopHit();
            // Walk up from whatever graphic was actually hit to find the ancestor that
            // handles pointer-enter - same thing Unity's own EventSystem does internally.
            // Some rows (the tab sidebar in particular) put the clickable component on a
            // parent object while the raycastable image is a child, so hitting the child
            // directly and firing straight on it finds nothing.
            GameObject hit = ((object)rawHit != null)
                ? ExecuteEvents.GetEventHandler<IPointerEnterHandler>(rawHit)
                : null;
            if (hit == _lastHover) return;

            if ((object)_lastHover != null)
                ExecuteEvents.Execute(_lastHover, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            if ((object)hit != null)
                ExecuteEvents.Execute(hit, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);

            _lastHover = hit;
        }

        // Deliberately click-only - see class remarks for why pointerDown/pointerUp are
        // NOT fired here.
        private void SimulateClick()
        {
            GameObject rawHit = RaycastTopHit();
            if ((object)rawHit == null) return;

            GameObject clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(rawHit);
            if ((object)clickTarget == null) return;

            ExecuteEvents.Execute(clickTarget, BuildPointerData(), ExecuteEvents.pointerClickHandler);

            // In case Unity's own Selectable machinery selected something anyway, clear it
            // immediately rather than waiting for next frame's sweep.
            if ((object)EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
