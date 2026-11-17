namespace DOT_User_Interface.Models.Routines.Util
{
    public enum OptionType
    {
        On,
        Off,
        Auto
    }
    public class TextStyleProperties 
    {
        public int FontSize { get; set; } = 0;
        public string FontFamily { get; set; } = "";
        public OptionType Bold { get; set; } = OptionType.Auto;
        public OptionType Italic { get; set; } = OptionType.Auto;
        public string? Color { get; set; } = "";
        public bool IsColorEnable { get; set; } = false;
    }
}
