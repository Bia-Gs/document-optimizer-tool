using System.Text.Json;

namespace DOT_User_Interface.Models
{
    public enum TranslationOptions
    {
        Auto,
        EN,
        ES,
        FR,
        PT
    }
    public class TranslateFactory
    {
        private TranslationOptions? sourceLanguage;
        public TranslationOptions SourceLanguage
        {
            get { return sourceLanguage == null ? TranslationOptions.Auto : sourceLanguage.Value; }
            set { sourceLanguage = value; }
        }
        private TranslationOptions? targeLanguage;
        public TranslationOptions TargeLanguage
        {
            get { return targeLanguage != null ? targeLanguage.Value : TranslationOptions.EN; }
            set
            {
                if (value != TranslationOptions.Auto)
                    targeLanguage = value;
            }
        }
        public bool CanTranslate { get { return SourceLanguage != TargeLanguage; } }
        public bool UseGlossray { get; set; } = true;
        public string ProcessingArgs()
        {
            var payload = new Dictionary<string, object>
            {
                ["glossary"] = UseGlossray,
                ["model"] = "translate",
                ["target_language"] = TargeLanguage.ToString()
            };

            if (SourceLanguage != TranslationOptions.Auto)
                payload["source_language"] = SourceLanguage.ToString();

            return JsonSerializer.Serialize(payload, options: new JsonSerializerOptions { WriteIndented = false });
        }
    }
}
