using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;

namespace PilotMarkdownModule.Views
{
    /// <summary>
    /// Редактор Markdown на базе EasyMDE (WYSIWYG)
    /// </summary>
    public partial class LiveMarkdownEditor : UserControl
    {
        private bool _isInternalChange;
        private bool _isWebViewInitialized;
        private string? _pendingContent;

        public LiveMarkdownEditor()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        #region Dependency Properties

        public string MarkdownText
        {
            get { return (string)GetValue(MarkdownTextProperty); }
            set { SetValue(MarkdownTextProperty, value); }
        }

        public static readonly DependencyProperty MarkdownTextProperty =
            DependencyProperty.Register(nameof(MarkdownText), typeof(string), typeof(LiveMarkdownEditor),
                new PropertyMetadata(string.Empty, OnMarkdownTextChanged));

        public bool IsEditMode
        {
            get { return (bool)GetValue(IsEditModeProperty); }
            set { SetValue(IsEditModeProperty, value); }
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(LiveMarkdownEditor),
                new PropertyMetadata(true, OnEditModeChanged));

        #endregion

        #region Event Handlers

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeWebView();
        }

        private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LiveMarkdownEditor editor && !editor._isInternalChange && editor._isWebViewInitialized)
            {
                editor.SetEditorContent(e.NewValue as string ?? string.Empty);
            }
        }

        private static void OnEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // EasyMDE всегда в режиме редактирования, это свойство не используется
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] IsEditMode changed: {e.NewValue}");
        }

        #endregion

        #region WebView2 Initialization

        private async void InitializeWebView()
        {
            if (_isWebViewInitialized)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownModule] Initializing WebView2 for EasyMDE...");

                // Создаем среду WebView2 с кэшем во временной папке
                var cacheFolder = Path.Combine(Path.GetTempPath(), "PilotMarkdownModule", "WebView2Cache");
                var env = await CoreWebView2Environment.CreateAsync(null, cacheFolder);
                
                await EditorWebView.EnsureCoreWebView2Async(env);

                // Настраиваем обработку сообщений от JS
                EditorWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                
                // Разрешаем сообщения от домена
                EditorWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                EditorWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                EditorWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

                // Загружаем HTML-шаблон EasyMDE
                await LoadEasyMDETemplate();

                _isWebViewInitialized = true;
                System.Diagnostics.Debug.WriteLine("[MarkdownModule] WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] WebView2 initialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Stack trace: {ex.StackTrace}");
                
                MessageBox.Show(
                    $"Ошибка инициализации редактора: {ex.Message}\n\n" +
                    $"Убедитесь, что WebView2 Runtime установлен.\n" +
                    $"Скачать: https://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                    "Ошибка Markdown редактора",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private System.Threading.Tasks.Task LoadEasyMDETemplate()
        {
            try
            {
                // Пытаемся загрузить HTML из ресурсов
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "EasyMDE", "Editor.html");
                
                if (File.Exists(htmlPath))
                {
                    var html = File.ReadAllText(htmlPath);
                    EditorWebView.CoreWebView2.NavigateToString(html);
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] EasyMDE template loaded from: {htmlPath}");
                }
                else
                {
                    // Fallback: загружаем минимальный HTML с CDN
                    var fallbackHtml = GetFallbackHtml();
                    EditorWebView.CoreWebView2.NavigateToString(fallbackHtml);
                    System.Diagnostics.Debug.WriteLine("[MarkdownModule] EasyMDE fallback template loaded");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error loading EasyMDE template: {ex.Message}");
                
                // Пробуем fallback
                try
                {
                    var fallbackHtml = GetFallbackHtml();
                    EditorWebView.CoreWebView2.NavigateToString(fallbackHtml);
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Fallback also failed: {fallbackEx.Message}");
                }
            }
            
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private string GetFallbackHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/easymde/dist/easymde.min.css'>
    <script src='https://cdn.jsdelivr.net/npm/easymde/dist/easymde.min.js'></script>
    <style>
        body { margin: 0; padding: 0; font-family: 'Segoe UI', sans-serif; }
        .editor-container { height: 100vh; }
    </style>
</head>
<body>
    <div class='editor-container'>
        <textarea id='editor'></textarea>
    </div>
    <script>
        let easyMDE = null;
        let pendingContent = '';
        
        setTimeout(function() {
            easyMDE = new EasyMDE({
                element: document.getElementById('editor'),
                initialValue: pendingContent,
                toolbar: ['bold', 'italic', 'strikethrough', '|', 'heading', 'code', 'quote', '|', 'unordered-list', 'ordered-list', '|', 'link', 'image', '|', 'table', '|', 'preview', 'side-by-side'],
                onUpdate: function() {
                    if (window.chrome && window.chrome.webview) {
                        window.chrome.webview.postMessage(JSON.stringify({
                            type: 'content',
                            content: easyMDE.value()
                        }));
                    }
                }
            });
            
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify({ type: 'initialized' }));
            }
        }, 100);
        
        function setMarkdownContent(markdown) {
            if (easyMDE) {
                easyMDE.value(markdown || '');
            } else {
                pendingContent = markdown || '';
            }
        }
        
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.addEventListener('message', function(event) {
                try {
                    var data = JSON.parse(event.data);
                    if (data.action === 'setContent') {
                        setMarkdownContent(data.content);
                    }
                } catch(e) {}
            });
        }
    </script>
</body>
</html>";
        }

        #endregion

        #region Message Handling

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(messageJson))
                    return;

                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Received from JS: {messageJson}");

                // Парсим сообщение от JavaScript
                using var doc = JsonDocument.Parse(messageJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeProperty))
                {
                    var messageType = typeProperty.GetString();

                    switch (messageType)
                    {
                        case "initialized":
                            // Редактор инициализирован, можно отправлять отложенный контент
                            HandleEditorInitialized();
                            break;

                        case "content":
                            // Получили изменения контента от редактора
                            if (root.TryGetProperty("content", out var contentProperty))
                            {
                                HandleContentChanged(contentProperty.GetString() ?? string.Empty);
                            }
                            break;

                        case "contentResponse":
                            // Ответ на запрос контента
                            if (root.TryGetProperty("content", out var responseContentProperty))
                            {
                                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Content response: {responseContentProperty.GetString()?.Length} chars");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error processing message: {ex.Message}");
            }
        }

        private void HandleEditorInitialized()
        {
            System.Diagnostics.Debug.WriteLine("[MarkdownModule] Editor initialized, checking for pending content");
            
            // Если есть отложенный контент, отправляем его в редактор
            if (!string.IsNullOrEmpty(_pendingContent))
            {
                SetEditorContent(_pendingContent);
                _pendingContent = null;
            }
        }

        private void HandleContentChanged(string content)
        {
            if (_isInternalChange)
                return;

            _isInternalChange = true;
            try
            {
                MarkdownText = content;
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Content updated: {content.Length} chars");
            }
            finally
            {
                _isInternalChange = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Установка контента в редактор
        /// </summary>
        public void SetEditorContent(string markdown)
        {
            if (EditorWebView.CoreWebView2 == null)
            {
                // WebView2 ещё не готов, сохраняем отложенный контент
                _pendingContent = markdown;
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] WebView2 not ready, pending content set: {markdown.Length} chars");
                return;
            }

            try
            {
                var script = $"setMarkdownContent({JsonSerializer.Serialize(markdown)})";
                EditorWebView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Content set in editor: {markdown.Length} chars");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error setting content: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение контента из редактора
        /// </summary>
        public string GetEditorContent()
        {
            return MarkdownText;
        }

        /// <summary>
        /// Вставка текста в текущую позицию курсора
        /// </summary>
        public void InsertText(string text)
        {
            if (EditorWebView.CoreWebView2 == null)
                return;

            try
            {
                var script = $"insertTextAtCursor({JsonSerializer.Serialize(text)})";
                EditorWebView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Text inserted: {text.Length} chars");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error inserting text: {ex.Message}");
            }
        }

        /// <summary>
        /// Применение форматирования
        /// </summary>
        public void ApplyFormat(string command)
        {
            if (EditorWebView.CoreWebView2 == null)
                return;

            try
            {
                var script = $"applyFormat({JsonSerializer.Serialize(command)})";
                EditorWebView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Format applied: {command}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error applying format: {ex.Message}");
            }
        }

        /// <summary>
        /// Применение заголовка конкретного уровня (1-6)
        /// </summary>
        public void ApplyHeading(int level)
        {
            if (EditorWebView.CoreWebView2 == null || level < 1 || level > 6)
                return;

            try
            {
                var script = $"applyHeading({level})";
                EditorWebView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Heading H{level} applied");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error applying heading: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт содержимого редактора в PDF файл
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ExportToPdfAsync(string filePath)
        {
            if (EditorWebView.CoreWebView2 == null)
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownModule] WebView2 not ready for PDF export");
                return false;
            }

            try
            {
                // Создаем настройки печати
                var printSettings = EditorWebView.CoreWebView2.Environment.CreatePrintSettings();
                printSettings.Orientation = Microsoft.Web.WebView2.Core.CoreWebView2PrintOrientation.Portrait;
                printSettings.MarginTop = 0.5;
                printSettings.MarginBottom = 0.5;
                printSettings.MarginLeft = 0.5;
                printSettings.MarginRight = 0.5;
                printSettings.ShouldPrintBackgrounds = true;
                printSettings.ScaleFactor = 1.0;

                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Exporting to PDF: {filePath}");

                // Экспортируем в PDF
                var success = await EditorWebView.CoreWebView2.PrintToPdfAsync(filePath, printSettings);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] PDF exported successfully: {filePath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] PDF export failed");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error exporting to PDF: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Ошибка экспорта в PDF: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
                return false;
            }
        }

        /// <summary>
        /// Открытие диалога печати браузера
        /// </summary>
        public void ShowPrintDialog()
        {
            if (EditorWebView.CoreWebView2 == null)
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownModule] WebView2 not ready for print");
                return;
            }

            try
            {
                EditorWebView.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);
                System.Diagnostics.Debug.WriteLine("[MarkdownModule] Print dialog opened");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error opening print dialog: {ex.Message}");
            }
        }

        #endregion
    }
}
