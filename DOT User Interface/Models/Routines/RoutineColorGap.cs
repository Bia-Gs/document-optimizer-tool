using DOT_User_Interface.Models.Routines.Util;
using System.Text.Json;
using System.Globalization;

namespace DOT_User_Interface.Models.Routines
{
    public class RoutineColorGap : TextStyleProperties, RoutineSettings, IRoutine
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool ApplyOnElements { get; set; } = true;
        public bool ApplyOnHeaders { get; set; } = true;
        public bool ApplyOnFooters { get; set; } = true;
        public string StartColorGap { get; set; } = "";
        public string EndColorGap { get; set; } = "";
        public string Endpoint => ProcessEndpoints.ColorGap;
        public RoutineTypes Type => RoutineTypes.EditColorGap;

        public IRoutine CloneBase()
        {
            return new RoutineColorGap
            {
                Name = this.Name,
                Description = this.Description,
                ApplyOnElements = this.ApplyOnElements,
                ApplyOnHeaders = this.ApplyOnHeaders,
                ApplyOnFooters = this.ApplyOnFooters,
                StartColorGap = this.StartColorGap,
                EndColorGap = this.EndColorGap,
                FontSize = this.FontSize,
                Bold = this.Bold,
                Italic = this.Italic,
                Color = this.Color,
                IsColorEnable = this.IsColorEnable
            };
        }

        public string ProcessingArgs(bool applyFilter = false)
        {
            var styles = new Dictionary<string, object?>();

            if (FontSize > 0)
                styles["sz"] = (FontSize * 2).ToString();

            if (IsColorEnable && !string.IsNullOrWhiteSpace(Color))
                styles["color"] = Color!.TrimStart('#').ToUpperInvariant();

            if (Bold != OptionType.Auto)
                styles["b"] = Bold == OptionType.On;

            if (Italic != OptionType.Auto)
                styles["i"] = Italic == OptionType.On;

            var config = new Dictionary<string, object?>
            {
                ["apply_on_elements"] = ApplyOnElements,
                ["apply_on_headers"] = ApplyOnHeaders,
                ["apply_on_footers"] = ApplyOnFooters,
                ["start_color"] = string.IsNullOrWhiteSpace(StartColorGap) ? null : StartColorGap.TrimStart('#').ToUpperInvariant(),
                ["end_color"] = string.IsNullOrWhiteSpace(EndColorGap) ? null : EndColorGap.TrimStart('#').ToUpperInvariant()
            };

            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var suffixMap = new Dictionary<string, string>
            {
                ["en"] = " edited",
                ["pt"] = " editado",
                ["es"] = " editado",
                ["fr"] = " édité"
            };
            var suffix = suffixMap.ContainsKey(lang) ? suffixMap[lang] : suffixMap["en"];

            var payload = new
            {
                model = "edit",
                action = new { config, styles },
                suffix
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        }

        public async Task ShareAsync(I18n I18n)
        {
            try
            {
                var fileName = $"{Name.Replace(' ', '_')}_{Type}.json";
                var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                await File.WriteAllTextAsync(tempPath, json);

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = I18n["RoutineFactory.Routine.Share"] + $" \"{Name}\"",
                    File = new ShareFile(tempPath)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(I18n["RoutineFactory.Routine.Share_erro"] + $" {ex.Message}");
            }
        }

        public bool IsColorBetween(string value)
        {
            int v = ConvertHexToInt(value);
            int s = ConvertHexToInt(StartColorGap);
            int e = ConvertHexToInt(EndColorGap);

            // Inverte se necessário
            if (s > e)
                (s, e) = (e, s);

            return v >= s && v <= e;
        }

        private static int ConvertHexToInt(string hex)
        {
            // Remove # e converte RRGGBB → inteiro
            return int.Parse(hex.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
        }
    }
}
