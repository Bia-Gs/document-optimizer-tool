using Color = System.Drawing.Color;

namespace DOT_User_Interface.Models.Forms
{
    public class Text : IForms
    {
        public Text? Parent { get; set; }
        public bool IsEditable { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public FormsType Type { get; } = FormsType.Text;

        // ----------------------------
        // VALUE
        // ----------------------------
        private string? valueInternal;
        public string Value
        {
            get
            {
                if (!string.IsNullOrEmpty(valueInternal))
                    return valueInternal;

                if (Parent != null)
                    return Parent.Value;

                return string.Empty;
            }
            set => valueInternal = value;
        }

        // ----------------------------
        // BOLD
        // ----------------------------
        private bool? boldInternal;
        public bool Bold
        {
            get
            {
                if (boldInternal != null)
                    return boldInternal.Value;

                if (Parent != null)
                    return Parent.Bold;

                return false;
            }
            set => boldInternal = value;
        }

        // ----------------------------
        // ITALIC
        // ----------------------------
        private bool? italicInternal;
        public bool Italic
        {
            get
            {
                if (italicInternal != null)
                    return italicInternal.Value;

                if (Parent != null)
                    return Parent.Italic;

                return false;
            }
            set => italicInternal = value;
        }

        // ----------------------------
        // FONT SIZE
        // ----------------------------
        private int? fontSizeInternal;
        public int FontSize
        {
            get
            {
                if (fontSizeInternal != null)
                    return fontSizeInternal.Value;

                if (Parent != null)
                    return Parent.FontSize;

                return 12;
            }
            set => fontSizeInternal = value;
        }

        // ----------------------------
        // COLOR
        // ----------------------------
        private Color? textColorInternal;
        public Color TextColor
        {
            get
            {
                if (textColorInternal != null)
                    return textColorInternal.Value;

                if (Parent != null)
                    return Parent.TextColor;

                return Color.Black;
            }
            set => textColorInternal = value;
        }

        // HEX helper
        public string ColorHex
        {
            get { return $"#{TextColor.R:X2}{TextColor.G:X2}{TextColor.B:X2}"; }
            set
            {
                if (value == ColorHex) return;
                if (string.IsNullOrWhiteSpace(value))
                {
                    TextColor = Color.Black;
                    return;
                }
                value = value.TrimStart('#');
                try
                {
                    if (value.Length == 6)
                    {
                        var r = Convert.ToInt32(value[..2], 16);
                        var g = Convert.ToInt32(value[2..4], 16);
                        var b = Convert.ToInt32(value[4..6], 16);
                        TextColor = Color.FromArgb(r, g, b);
                        return;
                    }
                    if (value.Length == 8)
                    {
                        var a = Convert.ToInt32(value[..2], 16);
                        var r = Convert.ToInt32(value[2..4], 16);
                        var g = Convert.ToInt32(value[4..6], 16);
                        var b = Convert.ToInt32(value[6..8], 16);
                        TextColor = Color.FromArgb(a, r, g, b);
                        return;
                    }
                }
                catch { }
                TextColor = Color.Black;
            }
        }

        // ----------------------------
        // FONT FAMILY
        // ----------------------------
        private string? fontFamily;
        public string? FontFamily
        {
            get
            {
                if (fontFamily != null)
                    return fontFamily;
                if (Parent != null)
                    return Parent.FontFamily;
                return string.Empty;
            }
            set => fontFamily = value;
        }

        // ----------------------------
        // JUSTIFICATION
        // ----------------------------
        private Justification? justification;
        public Justification Justification
        {
            get
            {
                if (justification != null)
                    return justification.Value;
                if (Parent != null)
                    return Parent.Justification;
                return Justification.Left;
            }
            set => justification = value;
        }
    }
}
