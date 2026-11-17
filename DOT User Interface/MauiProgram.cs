using DOT_User_Interface.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DOT_User_Interface
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Registra serviços de localização / i18n
            builder.Services.AddLocalization();
            builder.Services.AddSingleton<I18n, I18nService>();

            // Carrega idioma salvo (Preferences) ou usa 'es' como fallback
            var savedLang = Preferences.Get("app_language", string.Empty);
            var cultureCode = string.IsNullOrWhiteSpace(savedLang) ? "en" : savedLang;
            var defaultCulture = new CultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

    #if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
    #endif

            return builder.Build();
        }
    }
}
