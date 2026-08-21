using DescendersModMenu.Mods;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace DescendersModMenu.UI
{
    public static class XboxWorkshopPage
    {
        public static GameObject CreatePage(Transform parent)
        {
            GameObject pg = null;
            try
            {
                pg = UIHelpers.Obj("P23R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));

                var scrollObj = UIHelpers.Obj("Scroll", pg.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                var scrollRect = scrollObj.AddComponent<ScrollRect>();
                scrollRect.horizontal = false; scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 25f;
                scrollRect.inertia = false;

                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                scrollRect.viewport = UIHelpers.RT(vp);

                var content = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(content);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1);
                crt.sizeDelta = new Vector2(0, 0);
                scrollRect.content = crt;
                UIHelpers.AddScrollbar(scrollRect);

                var csf = content.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = UIHelpers.RowGap;
                vlg.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var c = content.transform;

                UIHelpers.SectionHeader("WORKSHOP", c);

                var workshopRow = UIHelpers.StatRow("Workshop", c);
                UIHelpers.ActionBtn(workshopRow.transform, "Open", () =>
                {
                    StateNavigator.PushGameState(StateNavigator.State_FreerideWorkshop, "Workshop");
                }, 90);

                UIHelpers.InfoBox(c, "Game Pass hides Workshop in the normal menu. This opens it anyway.");
                UIHelpers.InfoBox(c, "Use this from the pause menu on Mount Palumbo (Starting Map).");

                UIHelpers.AddScrollForwarders(c);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("XboxWorkshopPage.CreatePage: " + ex.Message);
                Telemetry.ReportErrorAsync(ex, "XboxWorkshopPage");
                return null;
            }
            return pg;
        }
    }
}
