using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace FieldCommand
{
    public enum UiAccent
    {
        Orange,
        Cyan,
        Lime
    }

    /// <summary>
    /// "Flat modern" look for every IMGUI window of the mod: dark charcoal panels, a side menu,
    /// tiles and one accent colour. Replaces the stock BepInEx/Unity grey skin.
    /// </summary>
    internal static class Theme
    {
        public static ConfigEntry<UiAccent> Accent;

        public static readonly Color Bg = Hex("1b1c1f");
        public static readonly Color Side = Hex("141517");
        public static readonly Color Tile = Hex("26282d");
        public static readonly Color TileHover = Hex("2f3238");
        public static readonly Color Line = Hex("3a3d44");
        public static readonly Color Input = Hex("101113");
        public static readonly Color Text = Hex("f4f4f5");
        public static readonly Color Muted = Hex("a1a1aa");
        public static readonly Color Dark = Hex("101113");
        public static readonly Color Ok = Hex("4ade80");
        public static readonly Color Bad = Hex("f87171");

        public static Color AccentColor
        {
            get
            {
                switch (Accent?.Value ?? UiAccent.Orange)
                {
                    case UiAccent.Cyan: return Hex("3dd6ff");
                    case UiAccent.Lime: return Hex("c6ff3d");
                    default: return Hex("ff7a3d");
                }
            }
        }

        public static string AccentHex
        {
            get
            {
                var c = AccentColor;
                return ((int)(c.r * 255f + 0.5f)).ToString("X2") + ((int)(c.g * 255f + 0.5f)).ToString("X2") + ((int)(c.b * 255f + 0.5f)).ToString("X2");
            }
        }

        public static GUISkin Skin { get; private set; }
        public static GUIStyle Window, Sidebar, Content, SideTab, SideTabOn, Title, Logo, Caps, Small, Big, TileBox,
            Primary, Seg, SegOn, Chip, PriceOk, PriceBad, OkLabel, BadLabel, Header, Toast, RowLabel, Mini, PrimaryMini, Value, NoScroll;

        /// <summary>Changes every time the skin is rebuilt — callers cache their own styles against it.</summary>
        public static int Version { get; private set; }

        private static UiAccent builtFor;
        private static Font body, semi, display;

        /// <summary>Height of one control row (buttons, inputs, sliders, check boxes).</summary>
        public const float Row = 28f;
        private static readonly List<Texture2D> textures = new List<Texture2D>();

        public static void Bind(ConfigFile cfg)
        {
            Accent = cfg.Bind("0. Interface", "Accent colour", UiAccent.Orange,
                new ConfigDescription("Accent colour of the mod's windows: Orange, Cyan or Lime.\nАкцентный цвет окон мода: оранжевый (Orange), бирюзовый (Cyan) или лаймовый (Lime).",
                    null, new ConfigurationManagerAttributes { Order = 900 }));
        }

        /// <summary>Call at the top of OnGUI. Sets the themed skin for this OnGUI pass.</summary>
        public static void Apply()
        {
            var accent = Accent?.Value ?? UiAccent.Orange;
            if (Skin == null || builtFor != accent || textures.Any(t => t == null))
            {
                Build(accent);
            }
            GUI.skin = Skin;
        }

        /// <summary>A side-menu tab; draws the accent bar on the selected one.</summary>
        public static bool SideButton(string text, bool on, params GUILayoutOption[] opts)
        {
            bool clicked = GUILayout.Button(text, on ? SideTabOn : SideTab, opts);
            if (on && Event.current.type == EventType.Repaint)
            {
                var r = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture(new Rect(r.x, r.y, 3f, r.height), Tex(AccentColor));
            }
            return clicked;
        }

        /// <summary>Two-way segmented switch ("For me" / "For the faction").</summary>
        public static bool Segmented(bool second, string a, string b, float width)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(a, second ? Seg : SegOn, GUILayout.Width(width))) second = false;
            if (GUILayout.Button(b, second ? SegOn : Seg, GUILayout.Width(width))) second = true;
            GUILayout.EndHorizontal();
            return second;
        }

        public static void Rule()
        {
            var r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(r, Tex(Line));
        }

        // ---------------------------------------------------------------

        private static void Build(UiAccent accent)
        {
            foreach (var t in textures)
            {
                if (t != null) UnityEngine.Object.Destroy(t);
            }
            textures.Clear();
            texCache.Clear();
            builtFor = accent;
            LoadFonts();
            Color acc = AccentColor;
            Color accDown = Color.Lerp(acc, Color.black, 0.2f);
            Color accHover = Color.Lerp(acc, Color.white, 0.15f);

            var baseSkin = GUI.skin;
            var s = UnityEngine.Object.Instantiate(baseSkin);
            s.hideFlags = HideFlags.HideAndDontSave;
            s.font = body;

            s.window = new GUIStyle(baseSkin.window) { name = "window" };
            SetBg(s.window, Bg);
            s.window.onNormal.background = Tex(Bg);
            s.window.onFocused.background = Tex(Bg);
            s.window.onHover.background = Tex(Bg);
            s.window.onActive.background = Tex(Bg);
            s.window.border = new RectOffset(0, 0, 0, 0);
            s.window.padding = new RectOffset(0, 0, 0, 0);
            s.window.margin = new RectOffset(0, 0, 0, 0);
            s.window.overflow = new RectOffset(0, 0, 0, 0);
            s.window.contentOffset = Vector2.zero;
            s.window.normal.textColor = s.window.onNormal.textColor = Text;

            s.box = Plain(Tile, Text, 13, new RectOffset(10, 10, 8, 8));
            s.box.margin = new RectOffset(0, 0, 3, 3);
            s.box.alignment = TextAnchor.UpperLeft;
            s.box.wordWrap = true;

            s.label = new GUIStyle(baseSkin.label) { font = body, fontSize = 14, wordWrap = true, richText = true };
            s.label.normal.textColor = Text;
            s.label.padding = new RectOffset(2, 2, 3, 3);

            s.button = Btn(Tile, TileHover, accDown, Text, 14);
            s.button.fixedHeight = Row;
            s.button.onNormal.background = Tex(acc);
            s.button.onHover.background = Tex(accHover);
            s.button.onActive.background = Tex(accDown);
            s.button.onNormal.textColor = s.button.onHover.textColor = s.button.onActive.textColor = Dark;

            s.toggle = new GUIStyle(baseSkin.toggle) { font = body, fontSize = 14, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            var box = Check(Input, Line, false);
            var boxOn = Check(acc, acc, true);
            s.toggle.normal.background = box;
            s.toggle.hover.background = Check(Input, Muted, false);
            s.toggle.active.background = box;
            s.toggle.focused.background = box;
            s.toggle.onNormal.background = boxOn;
            s.toggle.onHover.background = boxOn;
            s.toggle.onActive.background = boxOn;
            s.toggle.onFocused.background = boxOn;
            s.toggle.border = new RectOffset(22, 0, 0, 0);
            s.toggle.overflow = new RectOffset(0, 0, 0, 0);
            s.toggle.padding = new RectOffset(26, 6, 0, 0);
            s.toggle.margin = new RectOffset(2, 10, 2, 2);
            s.toggle.fixedHeight = Row;
            s.toggle.imagePosition = ImagePosition.TextOnly;
            foreach (var st in States(s.toggle)) st.textColor = Text;

            s.textField = Plain(Input, Text, 14, new RectOffset(8, 8, 5, 5));
            s.textField.border = new RectOffset(1, 1, 1, 1);
            s.textField.normal.background = Frame(8, Input, Line);
            s.textField.hover.background = Frame(8, Input, Muted);
            s.textField.focused.background = Frame(8, Input, acc);
            s.textField.focused.textColor = Text;
            s.textField.margin = new RectOffset(2, 2, 2, 2);
            s.textField.fixedHeight = Row;
            s.textField.alignment = TextAnchor.MiddleLeft;
            s.textField.clipping = TextClipping.Clip;
            s.textArea = new GUIStyle(s.textField) { wordWrap = true, fixedHeight = 0 };

            // the track and the thumb are both a full row high, so the thumb sits exactly on the line
            s.horizontalSlider = new GUIStyle(baseSkin.horizontalSlider);
            var track = Band(4, (int)Row, Line, 4);
            foreach (var st in States(s.horizontalSlider)) st.background = track;
            s.horizontalSlider.border = new RectOffset(0, 0, 0, 0);
            s.horizontalSlider.overflow = new RectOffset(0, 0, 0, 0);
            s.horizontalSlider.fixedHeight = Row;
            s.horizontalSlider.margin = new RectOffset(8, 8, 2, 2);
            s.horizontalSlider.padding = new RectOffset(0, 0, 0, 0);
            s.horizontalSliderThumb = new GUIStyle(baseSkin.horizontalSliderThumb);
            var thumb = Band(12, (int)Row, acc, 18);
            var thumbHi = Band(12, (int)Row, accHover, 18);
            foreach (var st in States(s.horizontalSliderThumb)) st.background = thumb;
            s.horizontalSliderThumb.hover.background = thumbHi;
            s.horizontalSliderThumb.active.background = thumbHi;
            s.horizontalSliderThumb.border = new RectOffset(0, 0, 0, 0);
            s.horizontalSliderThumb.overflow = new RectOffset(0, 0, 0, 0);
            s.horizontalSliderThumb.padding = new RectOffset(0, 0, 0, 0);
            s.horizontalSliderThumb.margin = new RectOffset(0, 0, 0, 0);
            s.horizontalSliderThumb.fixedWidth = 12;
            s.horizontalSliderThumb.fixedHeight = Row;

            s.verticalScrollbar = new GUIStyle(baseSkin.verticalScrollbar);
            SetBg(s.verticalScrollbar, Side);
            s.verticalScrollbar.border = new RectOffset(0, 0, 0, 0);
            s.verticalScrollbar.fixedWidth = 8;
            s.verticalScrollbar.margin = new RectOffset(4, 0, 0, 0);
            s.verticalScrollbar.padding = new RectOffset(0, 0, 0, 0);
            s.verticalScrollbarThumb = new GUIStyle(baseSkin.verticalScrollbarThumb);
            SetBg(s.verticalScrollbarThumb, Line);
            s.verticalScrollbarThumb.hover.background = Tex(Muted);
            s.verticalScrollbarThumb.border = new RectOffset(0, 0, 0, 0);
            s.verticalScrollbarThumb.fixedWidth = 8;
            s.verticalScrollbarUpButton = new GUIStyle { fixedHeight = 0, fixedWidth = 0 };
            s.verticalScrollbarDownButton = new GUIStyle { fixedHeight = 0, fixedWidth = 0 };
            s.horizontalScrollbar = new GUIStyle(s.verticalScrollbar) { fixedWidth = 0, fixedHeight = 8 };
            s.horizontalScrollbarThumb = new GUIStyle(s.verticalScrollbarThumb) { fixedWidth = 0, fixedHeight = 8 };
            s.horizontalScrollbarLeftButton = new GUIStyle { fixedHeight = 0, fixedWidth = 0 };
            s.horizontalScrollbarRightButton = new GUIStyle { fixedHeight = 0, fixedWidth = 0 };
            s.scrollView = new GUIStyle(baseSkin.scrollView);
            SetBg(s.scrollView, Color.clear);

            // toolbar / selection grid use the button styles; keep them flat
            s.customStyles = new GUIStyle[0];

            Skin = s;

            Window = s.window;
            Sidebar = Plain(Side, Text, 14, new RectOffset(0, 0, 16, 14));
            Sidebar.margin = new RectOffset(0, 0, 0, 0);
            Content = Plain(Color.clear, Text, 14, new RectOffset(16, 16, 14, 14));
            Content.margin = new RectOffset(0, 0, 0, 0);

            SideTab = Btn(Side, Hex("1d1f23"), Tile, Muted, 15);
            SideTab.alignment = TextAnchor.MiddleLeft;
            SideTab.padding = new RectOffset(18, 10, 9, 9);
            SideTab.margin = new RectOffset(0, 0, 0, 0);
            SideTab.hover.textColor = Text;
            SideTabOn = Btn(Hex("23252a"), Hex("26282d"), Tile, Text, 15);
            SideTabOn.alignment = TextAnchor.MiddleLeft;
            SideTabOn.padding = SideTab.padding;
            SideTabOn.margin = SideTab.margin;
            SideTabOn.font = semi;

            Logo = Lbl(display, 24, Text, FontStyle.Normal);
            Logo.wordWrap = false;
            Logo.padding = new RectOffset(18, 10, 0, 0);
            Title = Lbl(display, 22, Text, FontStyle.Normal);
            Title.wordWrap = false;
            Big = Lbl(display, 26, Text, FontStyle.Normal);
            Big.wordWrap = false;
            Big.padding = new RectOffset(18, 10, 0, 6);
            Caps = Lbl(semi, 11, Muted, FontStyle.Normal);
            Caps.wordWrap = false;
            Caps.clipping = TextClipping.Clip;
            Small = Lbl(body, 12, Muted, FontStyle.Normal);
            Header = Lbl(semi, 16, Text, FontStyle.Normal);
            Header.padding = new RectOffset(2, 2, 8, 2);

            TileBox = new GUIStyle(s.box) { padding = new RectOffset(12, 12, 10, 10), margin = new RectOffset(3, 3, 3, 3) };

            Primary = Btn(acc, accHover, accDown, Dark, 15);
            Primary.font = semi;
            Primary.padding = new RectOffset(12, 12, 4, 4);
            Primary.fixedHeight = Row;
            PrimaryMini = new GUIStyle(Primary) { padding = new RectOffset(4, 4, 2, 2), fontSize = 13 };
            Mini = new GUIStyle(s.button) { padding = new RectOffset(4, 4, 2, 2), fontSize = 13 };

            Seg = Btn(Tile, TileHover, Tile, Muted, 14);
            Seg.margin = new RectOffset(0, 0, 2, 2);
            SegOn = Btn(Text, Text, Text, Dark, 14);
            SegOn.margin = Seg.margin;
            SegOn.font = semi;
            Seg.fixedHeight = SegOn.fixedHeight = Row;

            Chip = Btn(Tile, TileHover, accDown, Muted, 13);
            Chip.onNormal.background = Tex(acc);
            Chip.onHover.background = Tex(accHover);
            Chip.onNormal.textColor = Chip.onHover.textColor = Dark;
            Chip.padding = new RectOffset(8, 8, 2, 2);
            Chip.fontSize = 12;
            Chip.fixedHeight = 26;

            PriceOk = Lbl(semi, 16, acc, FontStyle.Normal);
            PriceOk.wordWrap = false;
            PriceOk.alignment = TextAnchor.MiddleLeft;
            PriceOk.fixedHeight = Row;
            PriceBad = new GUIStyle(PriceOk);
            foreach (var x in States(PriceBad)) x.textColor = Bad;
            OkLabel = Lbl(semi, 14, Ok, FontStyle.Normal);
            BadLabel = Lbl(semi, 14, Bad, FontStyle.Normal);
            RowLabel = Lbl(body, 13, Muted, FontStyle.Normal);
            RowLabel.wordWrap = false;
            RowLabel.alignment = TextAnchor.MiddleLeft;
            RowLabel.fixedHeight = Row;
            RowLabel.clipping = TextClipping.Clip;
            Value = new GUIStyle(RowLabel) { alignment = TextAnchor.MiddleCenter };
            foreach (var x in States(Value)) x.textColor = Text;
            NoScroll = new GUIStyle { fixedHeight = 0, fixedWidth = 0 };

            Toast = Plain(Hex("141517"), Text, 14, new RectOffset(14, 14, 8, 8));
            Toast.alignment = TextAnchor.MiddleCenter;
            Toast.wordWrap = true;

            Version++;
        }

        private static void LoadFonts()
        {
            if (body != null && semi != null && display != null) return;
            string[] installed;
            try
            {
                installed = Font.GetOSInstalledFontNames();
            }
            catch
            {
                installed = new string[0];
            }
            body = Pick(installed, 14, "Segoe UI", "Arial");
            semi = Pick(installed, 14, "Segoe UI Semibold", "Segoe UI", "Arial");
            display = Pick(installed, 18, "Bahnschrift SemiBold", "Bahnschrift", "Segoe UI Semibold", "Arial");
        }

        private static Font Pick(string[] installed, int size, params string[] names)
        {
            var have = names.Where(n => installed.Contains(n)).ToArray();
            try
            {
                var f = Font.CreateDynamicFontFromOSFont(have.Length > 0 ? have : names, size);
                if (f != null)
                {
                    f.hideFlags = HideFlags.HideAndDontSave;
                    return f;
                }
            }
            catch
            {
            }
            return GUI.skin.font;
        }

        private static IEnumerable<GUIStyleState> States(GUIStyle st)
        {
            return new[] { st.normal, st.hover, st.active, st.focused, st.onNormal, st.onHover, st.onActive, st.onFocused };
        }

        private static GUIStyle Plain(Color bg, Color text, int size, RectOffset padding)
        {
            var st = new GUIStyle { font = body, fontSize = size, padding = padding, richText = true };
            SetBg(st, bg);
            foreach (var x in States(st)) x.textColor = text;
            return st;
        }

        private static GUIStyle Btn(Color bg, Color hover, Color active, Color text, int size)
        {
            var st = new GUIStyle
            {
                font = body,
                fontSize = size,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6),
                margin = new RectOffset(2, 2, 2, 2),
                richText = true,
                clipping = TextClipping.Clip
            };
            st.normal.background = Tex(bg);
            st.hover.background = Tex(hover);
            st.active.background = Tex(active);
            st.focused.background = Tex(bg);
            st.onNormal.background = Tex(bg);
            st.onHover.background = Tex(hover);
            st.onActive.background = Tex(active);
            foreach (var x in States(st)) x.textColor = text;
            return st;
        }

        private static GUIStyle Lbl(Font f, int size, Color c, FontStyle fs)
        {
            var st = new GUIStyle { font = f, fontSize = size, fontStyle = fs, wordWrap = true, richText = true, padding = new RectOffset(2, 2, 2, 2) };
            foreach (var x in States(st)) x.textColor = c;
            return st;
        }

        private static void SetBg(GUIStyle st, Color c)
        {
            var t = Tex(c);
            foreach (var x in States(st)) x.background = t;
        }

        private static readonly Dictionary<Color, Texture2D> texCache = new Dictionary<Color, Texture2D>();

        public static Texture2D Tex(Color c)
        {
            if (texCache.TryGetValue(c, out var t) && t != null) return t;
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            t.SetPixel(0, 0, c);
            t.Apply();
            texCache[c] = t;
            textures.Add(t);
            return t;
        }

        /// <summary>Check box image: a 16-px square at the left, vertically centred in a row; the rest is transparent.</summary>
        private static Texture2D Check(Color fill, Color edge, bool tick)
        {
            int w = 64, h = (int)Row, top = (h - 16) / 2;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = clear;
                    int ly = y - top;
                    if (x < 16 && ly >= 0 && ly < 16)
                    {
                        bool border = x == 0 || ly == 0 || x == 15 || ly == 15;
                        c = border ? edge : fill;
                        if (tick && x >= 4 && x <= 11 && ly >= 4 && ly <= 11) c = Dark;
                    }
                    t.SetPixel(x, y, c);
                }
            }
            t.Apply();
            textures.Add(t);
            return t;
        }

        /// <summary>A horizontal band of the given thickness centred in an otherwise transparent texture.</summary>
        private static Texture2D Band(int w, int h, Color c, int thickness)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            int from = (h - thickness) / 2;
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) t.SetPixel(x, y, y >= from && y < from + thickness ? c : clear);
            }
            t.Apply();
            textures.Add(t);
            return t;
        }

        /// <summary>Vertical-only scroll view (no stray horizontal bar).</summary>
        public static Vector2 Scroll(Vector2 pos, params GUILayoutOption[] opts)
        {
            return GUILayout.BeginScrollView(pos, false, false, NoScroll, Skin.verticalScrollbar, Skin.scrollView, opts);
        }

        /// <summary>Square with a 1-px frame (inputs).</summary>
        private static Texture2D Frame(int size, Color fill, Color edge)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    t.SetPixel(x, y, border ? edge : fill);
                }
            }
            t.Apply();
            textures.Add(t);
            return t;
        }

        private static Color Hex(string h)
        {
            int v = Convert.ToInt32(h, 16);
            return new Color(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f, 1f);
        }
    }
}
