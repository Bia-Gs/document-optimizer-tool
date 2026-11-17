using DOT_User_Interface.Models.Tables.Util;

namespace DOT_User_Interface.Models.Tables
{
    public class GlossaryRow : IRow
    {
        public string SourceTerm { get; set; } = string.Empty;
        public string TargetTerm { get; set; } = string.Empty;
        public bool Matches(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;

            var tokens = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var t in tokens)
            {
                if ((SourceTerm?.Contains(t, StringComparison.OrdinalIgnoreCase) == true) ||
                    (TargetTerm?.Contains(t, StringComparison.OrdinalIgnoreCase) == true))
                    return true;
            }
            return false;
        }
    }
}
