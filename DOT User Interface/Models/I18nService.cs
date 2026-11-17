using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DOT_User_Interface.Models
{
    public interface I18n
    {
        string this[string key] { get; }
        string T(string key);
        string T(string key, params object[] args);
        event Action? OnChange;
        Task SetLanguageAsync(string langCode); // <-- agora é async
        string CurrentLanguage { get; }
    }
    public class I18nService : I18n
    {
        private readonly IStringLocalizer<Resources.Location.Strings> _localizer;

        public I18nService(IStringLocalizer<Resources.Location.Strings> localizer)
        {
            _localizer = localizer;
        }

        public string CurrentLanguage => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        public event Action? OnChange;
        public string this[string key] => T(key);
        public string T(string key) => _localizer[key];
        public string T(string key, params object[] args) => _localizer[key, args];

        public Task SetLanguageAsync(string langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode))
                langCode = CultureInfo.InvariantCulture.TwoLetterISOLanguageName;

            var culture = new CultureInfo(langCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Persiste a escolha para reinícios futuros
            Preferences.Set("app_language", langCode);

            // Atualiza também a classe de recursos fortemente tipada (se usada)
            Resources.Location.Strings.Culture = culture;

            OnChange?.Invoke();
            return Task.CompletedTask;
        }
    }
}
