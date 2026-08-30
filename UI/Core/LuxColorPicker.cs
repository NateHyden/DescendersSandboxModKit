using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    /// <summary>
    /// Windows-style hue/saturation plane + luminance bar for Lux glow picking.
    /// </summary>
    public class LuxColorPicker : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const int PlaneTexW = 256;
        private const int PlaneTexH = 144;
        private const int LumTexW = 20;
        private const int LumTexH = 144;

        private RawImage _planeImg;
        private RawImage _lumImg;
        private RectTransform _planeRT;
        private RectTransform _lumRT;
        private RectTransform _crosshair;
        private RectTransform _lumHandle;
        private Texture2D _planeTex;
        private Texture2D _lumTex;
        private LuxGlowTint.Part _part;
        private bool _dragPlane;
        private bool _dragLum;

        public static LuxColorPicker Build(Transform parent)
        {
            var row = UIHelpers.Obj("LuxWinPicker", parent);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 148;
            rowLe.minHeight = 148;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var picker = row.AddComponent<LuxColorPicker>();

            var planeFrame = UIHelpers.Obj("LuxHSPlane", row.transform);
            var planeFrameLe = planeFrame.AddComponent<LayoutElement>();
            planeFrameLe.preferredWidth = 220;
            planeFrameLe.preferredHeight = PlaneTexH;
            var planeFrameImg = planeFrame.AddComponent<Image>();
            planeFrameImg.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            planeFrameImg.raycastTarget = false;

            var plane = UIHelpers.Obj("LuxHS", planeFrame.transform);
            picker._planeRT = UIHelpers.RT(plane);
            picker._planeRT.anchorMin = Vector2.zero;
            picker._planeRT.anchorMax = Vector2.one;
            picker._planeRT.offsetMin = new Vector2(1f, 1f);
            picker._planeRT.offsetMax = new Vector2(-1f, -1f);
            picker._planeImg = plane.AddComponent<RawImage>();
            picker._planeImg.raycastTarget = true;

            var cross = UIHelpers.Obj("LuxCross", plane.transform);
            picker._crosshair = UIHelpers.RT(cross);
            picker._crosshair.anchorMin = Vector2.zero;
            picker._crosshair.anchorMax = Vector2.zero;
            picker._crosshair.pivot = new Vector2(0.5f, 0.5f);
            picker._crosshair.sizeDelta = new Vector2(11f, 11f);
            var crossImg = cross.AddComponent<Image>();
            crossImg.color = Color.white;
            crossImg.raycastTarget = false;
            var crossOut = UIHelpers.Obj("LuxCrossOut", cross.transform);
            var crossOutImg = crossOut.AddComponent<Image>();
            crossOutImg.color = Color.black;
            crossOutImg.raycastTarget = false;
            UIHelpers.RT(crossOut).anchorMin = Vector2.zero;
            UIHelpers.RT(crossOut).anchorMax = Vector2.one;
            UIHelpers.RT(crossOut).offsetMin = new Vector2(-1f, -1f);
            UIHelpers.RT(crossOut).offsetMax = new Vector2(1f, 1f);
            UIHelpers.RT(cross).SetAsLastSibling();

            var lumFrame = UIHelpers.Obj("LuxLumFrame", row.transform);
            var lumFrameLe = lumFrame.AddComponent<LayoutElement>();
            lumFrameLe.preferredWidth = LumTexW + 2;
            lumFrameLe.preferredHeight = PlaneTexH;
            var lumFrameImg = lumFrame.AddComponent<Image>();
            lumFrameImg.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            lumFrameImg.raycastTarget = false;

            var lum = UIHelpers.Obj("LuxLum", lumFrame.transform);
            picker._lumRT = UIHelpers.RT(lum);
            picker._lumRT.anchorMin = Vector2.zero;
            picker._lumRT.anchorMax = Vector2.one;
            picker._lumRT.offsetMin = new Vector2(1f, 1f);
            picker._lumRT.offsetMax = new Vector2(-1f, -1f);
            picker._lumImg = lum.AddComponent<RawImage>();
            picker._lumImg.raycastTarget = true;

            var lumH = UIHelpers.Obj("LuxLumH", lum.transform);
            picker._lumHandle = UIHelpers.RT(lumH);
            picker._lumHandle.anchorMin = new Vector2(0.5f, 0f);
            picker._lumHandle.anchorMax = new Vector2(0.5f, 0f);
            picker._lumHandle.pivot = new Vector2(0.5f, 0.5f);
            picker._lumHandle.sizeDelta = new Vector2(LumTexW + 4f, 3f);
            var lumHImg = lumH.AddComponent<Image>();
            lumHImg.color = Color.white;
            lumHImg.raycastTarget = false;
            var lumHOut = UIHelpers.Obj("LuxLumHOut", lumH.transform);
            var lumHOutImg = lumHOut.AddComponent<Image>();
            lumHOutImg.color = Color.black;
            lumHOutImg.raycastTarget = false;
            UIHelpers.Fill(UIHelpers.RT(lumHOut));
            UIHelpers.RT(lumHOut).offsetMin = new Vector2(-1f, -1f);
            UIHelpers.RT(lumHOut).offsetMax = new Vector2(1f, 1f);

            picker.InitTextures();
            return picker;
        }

        private void InitTextures()
        {
            _planeTex = new Texture2D(PlaneTexW, PlaneTexH, TextureFormat.RGB24, false);
            _planeTex.wrapMode = TextureWrapMode.Clamp;
            _planeTex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < PlaneTexH; y++)
            {
                float sat = 1f - (float)y / (PlaneTexH - 1);
                for (int x = 0; x < PlaneTexW; x++)
                {
                    float hue = (float)x / (PlaneTexW - 1);
                    _planeTex.SetPixel(x, y, Color.HSVToRGB(hue, sat, 1f));
                }
            }
            _planeTex.Apply();
            _planeImg.texture = _planeTex;

            _lumTex = new Texture2D(LumTexW, LumTexH, TextureFormat.RGB24, false);
            _lumTex.wrapMode = TextureWrapMode.Clamp;
            _lumTex.filterMode = FilterMode.Bilinear;
            _lumImg.texture = _lumTex;
            RebuildLumTexture(0f, 1f);
        }

        private void RebuildLumTexture(float hue, float sat)
        {
            if ((object)_lumTex == null) return;
            for (int y = 0; y < LumTexH; y++)
            {
                float v = 1f - (float)y / (LumTexH - 1);
                Color c = Color.HSVToRGB(hue, sat, v);
                for (int x = 0; x < LumTexW; x++)
                    _lumTex.SetPixel(x, y, c);
            }
            _lumTex.Apply();
        }

        public void SetPart(LuxGlowTint.Part part)
        {
            _part = part;
            SyncFromPart();
        }

        public void SyncFromPart()
        {
            if ((object)_planeRT == null) return;
            float hue = LuxGlowTint.GetHue01(_part);
            float sat = LuxGlowTint.GetSaturation(_part);
            float lum = LuxGlowTint.GetBrightnessLuminance01(_part);
            RebuildLumTexture(hue, sat);
            PlaceCrosshair(hue, sat);
            PlaceLumHandle(lum);
        }

        private void PlaceCrosshair(float hue, float sat)
        {
            if ((object)_crosshair == null || (object)_planeRT == null) return;
            float w = _planeRT.rect.width;
            float h = _planeRT.rect.height;
            _crosshair.anchoredPosition = new Vector2(hue * w, (1f - sat) * h);
        }

        private void PlaceLumHandle(float lum)
        {
            if ((object)_lumHandle == null || (object)_lumRT == null) return;
            float h = _lumRT.rect.height;
            _lumHandle.anchoredPosition = new Vector2(0f, (1f - lum) * h);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(_planeRT, e.position, e.pressEventCamera))
            {
                _dragPlane = true;
                _dragLum = false;
                ApplyFromPlane(e);
            }
            else if (RectTransformUtility.RectangleContainsScreenPoint(_lumRT, e.position, e.pressEventCamera))
            {
                _dragLum = true;
                _dragPlane = false;
                ApplyFromLum(e);
            }
        }

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            if (_dragPlane) ApplyFromPlane(e);
            else if (_dragLum) ApplyFromLum(e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            _dragPlane = false;
            _dragLum = false;
        }

        private void ApplyFromPlane(PointerEventData e)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _planeRT, e.position, e.pressEventCamera, out local))
                return;

            Rect r = _planeRT.rect;
            float hue = Mathf.Clamp01((local.x - r.xMin) / r.width);
            float sat = Mathf.Clamp01(1f - (local.y - r.yMin) / r.height);
            PlaceCrosshair(hue, sat);
            RebuildLumTexture(hue, sat);
            float lum = LuxGlowTint.GetBrightnessLuminance01(_part);
            LuxGlowTint.ApplyPickerSelection(_part, hue, sat, lum);
            OutfitPage.RefreshAll();
        }

        private void ApplyFromLum(PointerEventData e)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _lumRT, e.position, e.pressEventCamera, out local))
                return;

            Rect r = _lumRT.rect;
            float lum = Mathf.Clamp01(1f - (local.y - r.yMin) / r.height);
            PlaceLumHandle(lum);
            float hue = LuxGlowTint.GetHue01(_part);
            float sat = LuxGlowTint.GetSaturation(_part);
            LuxGlowTint.ApplyPickerSelection(_part, hue, sat, lum);
            OutfitPage.RefreshAll();
        }
    }
}
