using MelonLoader;
using DescendersModMenu;
using UnityEngine;
using UnityEngine.UI;
using DescendersModMenu.Mods;

namespace DescendersModMenu.UI
{
    public static class ChatPage
    {
        private static ScrollRect _chatScroll = null;
        private static Transform _chatContent = null;
        private static string _inputBuffer = "";
        private static Text _inputText = null;
        private static Text _statusText = null;
        private static Text _onlineText = null;
        private static bool _chatFocused = false;
        public static bool IsChatFocused => _chatFocused;
        private static Text _chatCursor = null;
        private static RectTransform _chatBoxRect = null;
        private static Text _hudTogVal = null;
        private static Image _hudTrack = null;
        private static RectTransform _hudKnob = null;
        private static LayoutElement _inputRowLe = null;
        private static LayoutElement _inputBgLe = null;
        private static LayoutElement _sendBtnLe = null;

        private const float InputLineH = 14f;
        private const float InputPadV = 6f;
        private const int InputMaxLines = 4;
        private const float InputMinH = 20f;

        public static void CreatePage(Transform parent)
        {
            try
            {
                var pg = UIHelpers.Obj("P12R", parent);
                UIHelpers.Fill(UIHelpers.RT(pg));
                var root = pg.AddComponent<VerticalLayoutGroup>();
                root.spacing = UIHelpers.RowGap;
                root.padding = new RectOffset((int)UIHelpers.ContentPad, (int)UIHelpers.ContentPad, 8, 8);
                root.childAlignment = TextAnchor.UpperLeft;
                root.childForceExpandWidth = true;
                root.childForceExpandHeight = false;

                var hdrRow = UIHelpers.Obj("ChatHdrRow", pg.transform);
                hdrRow.AddComponent<LayoutElement>().preferredHeight = 28;
                var hdrHlg = hdrRow.AddComponent<HorizontalLayoutGroup>();
                hdrHlg.spacing = 8;
                hdrHlg.childAlignment = TextAnchor.MiddleLeft;
                hdrHlg.childForceExpandWidth = false;
                hdrHlg.childForceExpandHeight = false;
                hdrHlg.padding = new RectOffset(8, 0, 0, 0);

                var accentBar = UIHelpers.Panel("ABar", hdrRow.transform, UIHelpers.Accent);
                var abRT = UIHelpers.RT(accentBar);
                abRT.anchorMin = new Vector2(0, 0.5f); abRT.anchorMax = new Vector2(0, 0.5f);
                abRT.pivot = new Vector2(0, 0.5f);
                abRT.sizeDelta = new Vector2(3, 14);
                abRT.anchoredPosition = Vector2.zero;
                accentBar.AddComponent<LayoutElement>().ignoreLayout = true;

                var titleTxt = UIHelpers.Txt("ChatTitle", hdrRow.transform, "MOD CHAT", 11,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent);
                var tle = titleTxt.gameObject.AddComponent<LayoutElement>();
                tle.preferredWidth = 76; tle.preferredHeight = 28;

                var hdrSpacer = UIHelpers.Obj("HdrSp", hdrRow.transform);
                hdrSpacer.AddComponent<LayoutElement>().flexibleWidth = 1;

                var hudLbl = UIHelpers.Txt("HudLbl", hdrRow.transform, "On-Screen Chat Popup", 10,
                    FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.TextMid);
                hudLbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 128;
                _hudTogVal = UIHelpers.Txt("HudVal", hdrRow.transform,
                    ChatHUD.Enabled ? "ON" : "OFF", 11, FontStyle.Bold, TextAnchor.MiddleRight,
                    ChatHUD.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor);
                _hudTogVal.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Toggle(hdrRow.transform, "ChatHudT", () =>
                {
                    ChatHUD.Toggle();
                    RefreshHudToggle();
                }, out _hudTrack, out _hudKnob);
                UIHelpers.SetToggle(_hudTrack, _hudKnob, ChatHUD.Enabled);

                // ── Online row ────────────────────────────────────────
                var onlineRow = UIHelpers.Obj("ORow", pg.transform);
                onlineRow.AddComponent<Image>().color = UIHelpers.RowBg;
                var orLe = onlineRow.AddComponent<LayoutElement>();
                orLe.preferredHeight = 16; orLe.minHeight = 16; orLe.flexibleHeight = 0;
                var orHlg = onlineRow.AddComponent<HorizontalLayoutGroup>();
                orHlg.padding = new RectOffset(8, 8, 0, 0);
                orHlg.spacing = 0;
                orHlg.childAlignment = TextAnchor.MiddleLeft;
                orHlg.childForceExpandWidth = true;
                orHlg.childForceExpandHeight = true;
                orHlg.childControlWidth = true;
                orHlg.childControlHeight = true;
                _onlineText = UIHelpers.Txt("OL", onlineRow.transform,
                    "\u25CF Nobody else with the mod in this session",
                    9, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.OnColor);
                _onlineText.horizontalOverflow = HorizontalWrapMode.Overflow;
                _onlineText.verticalOverflow = VerticalWrapMode.Truncate;
                var olLe = _onlineText.gameObject.AddComponent<LayoutElement>();
                olLe.flexibleWidth = 1; olLe.minHeight = 14; olLe.preferredHeight = 14;

                var chatBox = UIHelpers.Obj("ChatBox", pg.transform);
                chatBox.AddComponent<Image>().color = UIHelpers.WinPanel;
                var cbLe = chatBox.AddComponent<LayoutElement>();
                cbLe.flexibleHeight = 1f;
                cbLe.minHeight = 120f;
                cbLe.preferredHeight = 200f;
                var scrollObj = UIHelpers.Obj("Scroll", chatBox.transform);
                UIHelpers.Fill(UIHelpers.RT(scrollObj));
                _chatScroll = scrollObj.AddComponent<ScrollRect>();
                _chatScroll.horizontal = false; _chatScroll.vertical = true;
                _chatScroll.movementType = ScrollRect.MovementType.Clamped;
                _chatScroll.scrollSensitivity = 30f; _chatScroll.inertia = false;
                var vp = UIHelpers.Obj("VP", scrollObj.transform);
                UIHelpers.Fill(UIHelpers.RT(vp));
                vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                vp.AddComponent<Mask>().showMaskGraphic = true;
                _chatScroll.viewport = UIHelpers.RT(vp);
                var content = UIHelpers.Obj("Content", vp.transform);
                var crt = UIHelpers.RT(content);
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = Vector2.zero;
                _chatScroll.content = crt;
                content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var cvlg = content.AddComponent<VerticalLayoutGroup>();
                cvlg.spacing = 4; cvlg.padding = new RectOffset(6, 6, 4, 4);
                cvlg.childAlignment = TextAnchor.UpperLeft;
                cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
                cvlg.childControlWidth = true; cvlg.childControlHeight = true;
                _chatContent = content.transform;
                UIHelpers.AddScrollbar(_chatScroll);

                var inputRow = UIHelpers.Obj("InputRow", pg.transform);
                inputRow.AddComponent<Image>().color = UIHelpers.RowBg;
                _inputRowLe = inputRow.AddComponent<LayoutElement>();
                _inputRowLe.preferredHeight = 26; _inputRowLe.minHeight = 26; _inputRowLe.flexibleHeight = 0;
                var irHlg = inputRow.AddComponent<HorizontalLayoutGroup>();
                irHlg.padding = new RectOffset(6, 6, 3, 3);
                irHlg.spacing = 6;
                irHlg.childAlignment = TextAnchor.UpperLeft;
                irHlg.childForceExpandHeight = false;
                irHlg.childForceExpandWidth = false;
                irHlg.childControlHeight = true;
                irHlg.childControlWidth = true;
                var inputBg = UIHelpers.Obj("IB", inputRow.transform);
                inputBg.AddComponent<Image>().color = UIHelpers.WinOuter;
                _inputBgLe = inputBg.AddComponent<LayoutElement>();
                _inputBgLe.flexibleWidth = 1; _inputBgLe.minHeight = InputMinH; _inputBgLe.preferredHeight = InputMinH;
                var ibHlg = inputBg.AddComponent<HorizontalLayoutGroup>();
                ibHlg.padding = new RectOffset(8, 20, 2, 2);
                ibHlg.childAlignment = TextAnchor.UpperLeft;
                ibHlg.childForceExpandWidth = true; ibHlg.childForceExpandHeight = false;
                ibHlg.childControlWidth = true; ibHlg.childControlHeight = true;
                _inputText = UIHelpers.Txt("IT", inputBg.transform, "Type a message...",
                    11, FontStyle.Normal, TextAnchor.UpperLeft, UIHelpers.TextDim);
                _inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
                _inputText.verticalOverflow = VerticalWrapMode.Overflow;
                var itLe = _inputText.gameObject.AddComponent<LayoutElement>();
                itLe.flexibleWidth = 1; itLe.minWidth = 0; itLe.minHeight = InputLineH;

                _chatCursor = UIHelpers.Txt("ChCur", inputBg.transform, "|",
                    12, FontStyle.Bold, TextAnchor.UpperRight, UIHelpers.Accent);
                _chatCursor.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var ccRT = UIHelpers.RT(_chatCursor.gameObject);
                ccRT.anchorMin = new Vector2(1, 1); ccRT.anchorMax = new Vector2(1, 1);
                ccRT.pivot = new Vector2(1, 1);
                ccRT.sizeDelta = new Vector2(10, 16);
                ccRT.anchoredPosition = new Vector2(-4, -2);
                _chatCursor.gameObject.SetActive(false);

                _chatBoxRect = UIHelpers.RT(inputBg);
                var chatFocusBtn = inputBg.AddComponent<UnityEngine.UI.Button>();
                chatFocusBtn.targetGraphic = inputBg.GetComponent<UnityEngine.UI.Image>();
                chatFocusBtn.onClick.AddListener(() => { _chatFocused = true; });

                var sendBtn = UIHelpers.Btn("SB", inputRow.transform, "SEND",
                    new Vector2(64, 20), 11, () => { SendMessage(); },
                    UIHelpers.NeonBlue, Color.black);
                _sendBtnLe = sendBtn.gameObject.AddComponent<LayoutElement>();
                _sendBtnLe.preferredWidth = 64; _sendBtnLe.minWidth = 64;
                _sendBtnLe.preferredHeight = 20; _sendBtnLe.minHeight = 20;

                // ── Status ────────────────────────────────────────────
                var statusRow = UIHelpers.Obj("SR", pg.transform);
                statusRow.AddComponent<Image>().color = UIHelpers.RowBg;
                var srLe = statusRow.AddComponent<LayoutElement>();
                srLe.preferredHeight = 16; srLe.minHeight = 16; srLe.flexibleHeight = 0;
                var srHlg = statusRow.AddComponent<HorizontalLayoutGroup>();
                srHlg.padding = new RectOffset(8, 8, 0, 0);
                srHlg.childAlignment = TextAnchor.MiddleLeft;
                srHlg.childForceExpandHeight = true;
                srHlg.childControlHeight = true;
                _statusText = UIHelpers.Txt("ST", statusRow.transform,
                    "0/" + ModChat.MaxLength + " \u2014 Enter to send \u2014 Only visible to mod users",
                    9, FontStyle.Italic, TextAnchor.MiddleLeft, UIHelpers.TextDim);
                var stLe = _statusText.gameObject.AddComponent<LayoutElement>();
                stLe.flexibleWidth = 1; stLe.preferredHeight = 14;

                RebuildMessages();
                ResizeInputBox();
            }
            catch (System.Exception ex) { MelonLogger.Error("ChatPage: " + ex.Message);  Telemetry.ReportErrorAsync(ex, "ChatPage"); }
        }


        public static void Tick()
        {
            if (!_inputText) return;

            if (ModChat.HasNewMessages)
            {
                RebuildMessages();
                ModChat.ClearNewFlag();
                if (MenuWindow.IsChatOpen)
                    ModChat.MarkAsRead();
            }

            if (_chatFocused && Input.GetMouseButtonDown(0))
            {
                if (_chatBoxRect
                    && !RectTransformUtility.RectangleContainsScreenPoint(_chatBoxRect, Input.mousePosition, null))
                    _chatFocused = false;
            }

            if (_chatCursor) _chatCursor.gameObject.SetActive(false);

            if (_statusText)
            {
                if (!ModChat.InRoom)
                    _statusText.text = "Photon: " + ModChat.ConnectionStateLabel
                        + " - need a Casual multiplayer room";
                else
                    _statusText.text = _inputBuffer.Length + "/" + ModChat.MaxLength
                        + " characters - Click box to type - Only visible to mod users";
            }
            if (_onlineText)
            {
                if (!ModChat.InRoom)
                {
                    _onlineText.text = "○ Not in Photon room (" + ModChat.ConnectionStateLabel
                        + ", players=" + ModChat.PlayerListCount + ")";
                }
                else
                {
                    _onlineText.text = "● " + FormatModUsersOnline(ModDetection.ModUsers);
                }
            }
            RefreshHudToggle();

            if (!_chatFocused) return;

            foreach (char ch in Input.inputString)
            {
                if (ch == '\b') { if (_inputBuffer.Length > 0) _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1); }
                else if (ch == '\n' || ch == '\r') { SendMessage(); return; }
                else if (ch == (char)27) { _chatFocused = false; return; }
                else if (_inputBuffer.Length < ModChat.MaxLength) _inputBuffer += ch;
            }
            if (_inputBuffer.Length > 0)
            {
                _inputText.text = SoftWrapForDisplay(UIHelpers.WithCaret(_inputBuffer, true));
                _inputText.color = UIHelpers.TextLight;
            }
            else
            {
                _inputText.text = UIHelpers.WithCaret("Type a message...", true);
                _inputText.color = UIHelpers.TextDim;
            }
            ResizeInputBox();
        }

        public static void ClearUiRefs()
        {
            _chatScroll = null;
            _chatContent = null;
            _inputText = null;
            _statusText = null;
            _onlineText = null;
            _chatCursor = null;
            _chatBoxRect = null;
            _hudTogVal = null;
            _hudTrack = null;
            _hudKnob = null;
            _inputRowLe = null;
            _inputBgLe = null;
            _sendBtnLe = null;
            _chatFocused = false;
            _inputBuffer = "";
        }

        private static void SendMessage()
        {
            string msg = _inputBuffer.Trim();
            _inputBuffer = "";
            if (string.IsNullOrEmpty(msg)) return;
            _chatFocused = false;
            if (_inputText)
            {
                _inputText.text = "Type a message...";
                _inputText.color = UIHelpers.TextDim;
            }
            ResizeInputBox();
            ModChat.Send(msg);
        }

        private static void RebuildMessages()
        {
            if (!_chatContent) return;
            for (int i = _chatContent.childCount - 1; i >= 0; i--)
                GameObject.Destroy(_chatContent.GetChild(i).gameObject);
            foreach (var msg in ModChat.Messages)
            {
                var row = UIHelpers.Obj("MR", _chatContent);
                row.AddComponent<Image>().color = msg.IsSelf
                    ? new Color(0f, 0.16f, 0.32f, 0.35f)
                    : new Color(0, 0, 0, 0);
                var le = row.AddComponent<LayoutElement>();
                le.minHeight = 28; le.flexibleHeight = 0;
                row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var vlg = row.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(msg.IsSelf ? 6 : 4, 6, 4, 4);
                vlg.spacing = 2;
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;

                var meta = UIHelpers.Obj("Meta", row.transform);
                meta.AddComponent<LayoutElement>().preferredHeight = 16;
                var mHlg = meta.AddComponent<HorizontalLayoutGroup>();
                mHlg.spacing = 4;
                mHlg.childAlignment = TextAnchor.MiddleLeft;
                mHlg.childForceExpandWidth = false;
                mHlg.childForceExpandHeight = false;
                if (msg.IsSelf)
                {
                    var bar = UIHelpers.Obj("B", meta.transform);
                    bar.AddComponent<Image>().color = UIHelpers.NeonBlue;
                    var barLe = bar.AddComponent<LayoutElement>();
                    barLe.preferredWidth = 2; barLe.preferredHeight = 12;
                }
                UIHelpers.Txt("T", meta.transform, msg.Time, 9, FontStyle.Normal, TextAnchor.MiddleLeft, UIHelpers.TextDim)
                    .gameObject.AddComponent<LayoutElement>().preferredWidth = 28;
                UIHelpers.Txt("G", meta.transform, "[MOD]", 9, FontStyle.Bold, TextAnchor.MiddleLeft, UIHelpers.Accent)
                    .gameObject.AddComponent<LayoutElement>().preferredWidth = 32;
                UIHelpers.Txt("N", meta.transform, msg.PlayerName, 11, FontStyle.Bold, TextAnchor.MiddleLeft,
                    msg.IsSelf ? UIHelpers.NeonBlue : UIHelpers.Orange)
                    .gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                var body = UIHelpers.Txt("M", row.transform, SoftWrapForDisplay(msg.Text),
                    11, FontStyle.Normal, TextAnchor.UpperLeft, UIHelpers.TextLight);
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Overflow;
                var bodyLe = body.gameObject.AddComponent<LayoutElement>();
                bodyLe.flexibleWidth = 1;
                bodyLe.minHeight = InputLineH;
            }
            Canvas.ForceUpdateCanvases();
            if (_chatScroll) _chatScroll.verticalNormalizedPosition = 0f;
        }

        public static void RefreshAll() => RebuildMessages();

        private static string SoftWrapForDisplay(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            const int chunk = 28;
            var sb = new System.Text.StringBuilder(text.Length + 8);
            int run = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                sb.Append(c);
                if (char.IsWhiteSpace(c))
                    run = 0;
                else
                {
                    run++;
                    if (run >= chunk)
                    {
                        sb.Append('\u200B');
                        run = 0;
                    }
                }
            }
            return sb.ToString();
        }

        private static void ResizeInputBox()
        {
            if (!_inputText || (object)_inputBgLe == null || (object)_inputRowLe == null) return;

            float textH = InputLineH;
            if (_inputBuffer.Length > 0)
            {
                float w = UIHelpers.RT(_inputText.gameObject).rect.width;
                if (w < 40f && _chatBoxRect) w = _chatBoxRect.rect.width - 28f;
                if (w < 40f) w = 400f;
                textH = Mathf.Max(InputLineH, _inputText.preferredHeight);
                float maxH = InputLineH * InputMaxLines;
                if (textH > maxH) textH = maxH;
            }

            float bgH = Mathf.Max(InputMinH, textH + 4f);
            float rowH = bgH + InputPadV;
            _inputBgLe.preferredHeight = bgH;
            _inputBgLe.minHeight = bgH;
            _inputRowLe.preferredHeight = rowH;
            _inputRowLe.minHeight = rowH;
            if ((object)_sendBtnLe != null)
                _sendBtnLe.preferredHeight = Mathf.Min(bgH, 28f);
        }

        private static string FormatModUsersOnline(System.Collections.Generic.IList<ModDetection.ModUser> users)
        {
            if (users == null || users.Count == 0)
                return "Nobody else with the mod in this session";

            if (users.Count == 1)
            {
                string n = string.IsNullOrEmpty(users[0].Name) ? "Someone" : users[0].Name;
                return n + " is in this session";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < users.Count; i++)
            {
                if (i > 0)
                    sb.Append(i == users.Count - 1 ? " and " : ", ");
                sb.Append(string.IsNullOrEmpty(users[i].Name) ? "Someone" : users[i].Name);
            }
            sb.Append(" are in this session");
            return sb.ToString();
        }

        private static void RefreshHudToggle()
        {
            if ((object)_hudTogVal != null)
            {
                _hudTogVal.text = ChatHUD.Enabled ? "ON" : "OFF";
                _hudTogVal.color = ChatHUD.Enabled ? UIHelpers.OnColor : UIHelpers.OffColor;
            }
            if ((object)_hudTrack != null)
                UIHelpers.SetToggle(_hudTrack, _hudKnob, ChatHUD.Enabled);
        }
    }
}

