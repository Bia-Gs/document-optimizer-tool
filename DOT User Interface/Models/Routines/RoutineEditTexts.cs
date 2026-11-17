using DOT_User_Interface.Models.Routines.Util;
using System.Text.Json;
using System.Globalization;
using System.Collections.Generic;
using System.IO;

namespace DOT_User_Interface.Models.Routines
{
    public class RoutineEditTexts : TextStyleProperties, RoutineSettings, IRoutine
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string SourceText { get; set; } = "";
        public string TargetText { get; set; } = "";
        public string Endpoint => ProcessEndpoints.EditTexts;
        public RoutineTypes Type => RoutineTypes.EditTexts;
        public bool ApplyOnElements { get; set; } = true;
        public bool ApplyOnHeaders { get; set; } = true;
        public bool ApplyOnFooters { get; set; } = true;

        public IRoutine CloneBase()
        {
            return new RoutineEditTexts
            {
                Name = this.Name,
                Description = this.Description,
                SourceText = this.SourceText,
                TargetText = this.TargetText,
                ApplyOnElements = this.ApplyOnElements,
                ApplyOnHeaders = this.ApplyOnHeaders,
                ApplyOnFooters = this.ApplyOnFooters,
                FontSize = this.FontSize,
                Bold = this.Bold,
                Italic = this.Italic,
                Color = this.Color,
                IsColorEnable = this.IsColorEnable,
                FontFamily = this.FontFamily,
            };
        }

        public string ProcessingArgs(bool applyFilter = false)
        {
            var styles = new Dictionary<string, object?>();

            if (FontSize > 0)
                styles["sz"] = (FontSize * 2).ToString();

            if (!string.IsNullOrWhiteSpace(FontFamily))
                styles["font-family"] = FontFamily;

            if (IsColorEnable && !string.IsNullOrWhiteSpace(Color))
                styles["color"] = Color!.TrimStart('#').ToUpperInvariant();

            if (Bold != OptionType.Auto)
                styles["b"] = Bold == OptionType.On;

            if (Italic != OptionType.Auto)
                styles["i"] = Italic == OptionType.On;

            if (!string.IsNullOrWhiteSpace(SourceText))
                styles["original-text"] = SourceText;

            if (!string.IsNullOrWhiteSpace(TargetText))
                styles["new-text"] = TargetText;

            var config = new Dictionary<string, object?>
            {
                ["apply_on_elements"] = ApplyOnElements,
                ["apply_on_headers"] = ApplyOnHeaders,
                ["apply_on_footers"] = ApplyOnFooters
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
    }
}
