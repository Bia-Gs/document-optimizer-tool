using Color = System.Drawing.Color;

namespace DOT_User_Interface.Models.Forms
{
    public enum FormsType
    {
        Text,
        Table,
        Image
    }
    public interface IForms
    {
        public FormsType Type { get; }
        public string Name { get; set; }
    }
    public enum Justification
    {
        Left,
        Center,
        Right,
        Both
    }
    public enum Border
    {
        None,
        Top,
        Bottom,
        Left,
        Right,
        All,
        TopBottom,
        LeftRight,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
    public enum BorderStyle
    {
        None,
        Solid,
        Dashed,
        Dotted,
        Double
    }
    public enum Merge
    {
        None,
        Continue,
        Restart
    }

    public class Shading
    {
        public string Value { get; set; } = string.Empty;
        public Color Color { get; set; }
        public Color Fill { get; set; }
    }
    public class Spacing
    {
        public string LineRule { get; set; } = string.Empty;
        public int Line { get; set; }
        public int After { get; set; }
    }
}
