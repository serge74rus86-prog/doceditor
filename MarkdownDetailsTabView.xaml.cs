using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using PilotMarkdownModule.ViewModels;

namespace PilotMarkdownModule.Views
{
    public partial class MarkdownDetailsTabView : UserControl
    {
        private WebView2? _webView;
        private MarkdownDetailsViewModel? _viewModel;

        public MarkdownDetailsTabView()
        {
            InitializeComponent();
            Unloaded += MarkdownDetailsTabView_Unloaded;
        }

        private void MarkdownDetailsTabView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от событий
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private async void PreviewWebView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is WebView2 webView)
            {
                _webView = webView;
                _viewModel = DataContext as MarkdownDetailsViewModel;
                
                if (_viewModel == null)
                    return;

                try
                {
                    // Инициализируем WebView2 с отдельной папкой кэша
                    var cacheFolder = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(), 
                        "PilotMarkdownModule", 
                        "DetailsViewCache");
                    
                    var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment
                        .CreateAsync(null, cacheFolder);
                    
                    await webView.EnsureCoreWebView2Async(env);
                    
                    // Загружаем начальный контент
                    UpdateWebViewContent(webView, _viewModel.PreviewHtml);
                    
                    // Подписываемся на изменения свойства PreviewHtml
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] WebView2 init error: {ex.Message}");
                }
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MarkdownDetailsViewModel.PreviewHtml))
            {
                if (_webView != null && _viewModel != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateWebViewContent(_webView, _viewModel.PreviewHtml);
                    });
                }
            }
        }

        private void UpdateWebViewContent(WebView2 webView, string html)
        {
            if (string.IsNullOrEmpty(html))
                return;

            var fullHtml = $@"<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset='utf-8'>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ 
            font-family: 'Segoe UI', 'Segoe UI Web (West European)', -apple-system, BlinkMacSystemFont, Roboto, 'Helvetica Neue', sans-serif; 
            padding: 16px;
            color: #323130;
            line-height: 1.6;
        }}
        h1, h2, h3, h4, h5, h6 {{ 
            color: #0078D4;
            margin-top: 1em;
            margin-bottom: 0.5em;
            font-weight: 600;
        }}
        h1 {{ font-size: 2em; border-bottom: 2px solid #E1DFDD; padding-bottom: 0.3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid #E1DFDD; padding-bottom: 0.3em; }}
        h3 {{ font-size: 1.25em; }}
        h4 {{ font-size: 1em; }}
        h5 {{ font-size: 0.875em; }}
        h6 {{ font-size: 0.85em; color: #605E5C; }}
        a {{ 
            color: #0078D4; 
            text-decoration: none; 
        }}
        a:hover {{ 
            text-decoration: underline; 
        }}
        code {{ 
            background-color: #F3F2F1; 
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: 0.9em;
        }}
        pre {{ 
            background-color: #F3F2F1; 
            padding: 12px;
            border-radius: 4px;
            overflow-x: auto;
            border: 1px solid #E1DFDD;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
        }}
        blockquote {{ 
            border-left: 4px solid #0078D4;
            padding-left: 16px;
            color: #605E5C;
            margin-left: 0;
        }}
        table {{ 
            border-collapse: collapse; 
            width: 100%; 
            margin: 1em 0;
        }}
        th, td {{ 
            border: 1px solid #E1DFDD; 
            padding: 8px 12px; 
            text-align: left;
        }}
        th {{ 
            background-color: #F3F2F1; 
            font-weight: 600;
        }}
        tr:nth-child(even) {{
            background-color: #FAFAFA;
        }}
        ul, ol {{
            padding-left: 2em;
            margin: 0.5em 0;
        }}
        li {{
            margin: 0.25em 0;
        }}
        img {{
            max-width: 100%;
            height: auto;
        }}
        hr {{
            border: none;
            border-top: 2px solid #E1DFDD;
            margin: 2em 0;
        }}
    </style>
</head>
<body>
{html}
</body>
</html>";

            try
            {
                webView.NavigateToString(fullHtml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error updating WebView: {ex.Message}");
            }
        }
    }
}
