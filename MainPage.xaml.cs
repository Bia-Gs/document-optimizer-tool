using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace DOT_User_Interface
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            // Confirma quando o handler nativo (WebView) estiver pronto
            blazorWebView.HandlerChanged += BlazorWebView_HandlerChanged;
        }

        private void BlazorWebView_HandlerChanged(object? sender, EventArgs e)
        {
            Debug.WriteLine("BlazorWebView.HandlerChanged -> handler nativo pronto (ou alterado).");

            // Apenas como verificação rápida em tempo de execução:
            #if WINDOWS
            try
            {
                Debug.WriteLine("Plataforma: Windows. Verificando propriedade interna (informativa)...");
                var webViewProperty = blazorWebView?.GetType().GetProperty("WebView")?.GetValue(blazorWebView);
                Debug.WriteLine(webViewProperty is null ? "WebView interno NÃO acessível via reflexão." : "WebView interno acessível.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exceção durante verificação do WebView: {ex.Message}");
            }
            #endif
        }
    }
}