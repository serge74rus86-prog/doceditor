using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Ascon.Pilot.SDK;
using Ascon.Pilot.Theme.ColorScheme;
using System.Windows.Media;
using PilotMarkdownModule.Services;
using PilotMarkdownModule.Views;

namespace PilotMarkdownModule
{
    /// <summary>
    /// Точка входа расширения Pilot Markdown Module
    /// </summary>
    [Export(typeof(INewTabPage))]
    public class Extension : INewTabPage
    {
        private readonly ITabServiceProvider _tabServiceProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly IPilotDialogService _dialogService;
        private readonly IObjectsRepository _objectsRepository;

        private const string CommandName = "OpenMarkdownEditor";
        private const string Title = "Markdown редактор";
        private const string HelpCommandName = "MarkdownModule_Help";
        private const string HelpTitle = "Markdown Справка";

        [ImportingConstructor]
        public Extension(
            ITabServiceProvider tabServiceProvider,
            IPilotDialogService dialogService,
            IEventAggregator eventAggregator,
            IObjectsRepository objectsRepository)
        {
            _tabServiceProvider = tabServiceProvider;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _objectsRepository = objectsRepository;

            _eventAggregator.Subscribe(this);

            var accentColor = (Color)ColorConverter.ConvertFromString(dialogService.AccentColor);
            ColorScheme.Initialize(accentColor, dialogService.Theme);

            // Лог для отладки
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Extension loaded");
        }

        public void BuildNewTabPage(INewTabPageHost host)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] ========== BuildNewTabPage CALLED ==========");
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Host type: {host?.GetType().FullName ?? "null"}");

            var groupId = Guid.Parse("74D48E03-4E09-4F9C-A657-C23615F0A862");

            host.SetGroup("Markdown Module", groupId);
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Group 'Markdown Module' set with GUID {groupId}");

            // Кнопка справки
            host.AddButtonToGroup(
                "❓ " + HelpTitle,
                HelpCommandName,
                "Показать справку по модулю",
                null,
                groupId
            );
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Help button added: {HelpCommandName}");

            // Кнопка открытия редактора
            host.AddButtonToGroup(
                " " + Title,
                CommandName,
                "Открыть редактор Markdown",
                null,
                groupId
            );
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Editor button added: {CommandName}");

                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] ========== BuildNewTabPage COMPLETED ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] ERROR in BuildNewTabPage: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Stack trace: {ex.StackTrace}");
            }
        }

        public void OnButtonClick(string name)
        {
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Button clicked: {name}");

            if (name == HelpCommandName)
            {
                ShowHelp();
            }
            else if (name == CommandName)
            {
                OpenMarkdownEditor();
            }
        }

        private void ShowHelp()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var versionString = $"v{version.Major}.{version.Minor}.{version.Build} (build {version.Revision})";

                var helpText = GetHelpText(versionString);

                MessageBox.Show(
                    helpText,
                    "Markdown Module - Справка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error showing help: {ex.Message}");
            }
        }

        private string GetHelpText(string versionString)
        {
            return $@"**Markdown Module {versionString}**

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📖 ОПИСАНИЕ

Markdown Module — это расширение для Pilot-ICE/ECM, которое предоставляет удобный редактор Markdown с интегрированным деревом объектов Pilot.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🎯 ОСНОВНЫЕ ВОЗМОЖНОСТИ

1. **Редактор Markdown с Live Preview**
   - Редактирование текста в формате Markdown
   - Мгновенный предпросмотр HTML
   - Поддержка всего спектра Markdown синтаксиса

2. **Дерево объектов Pilot**
   - Отображение иерархии объектов Pilot
   - Загрузка объектов из репозитория Pilot
   - Разворачивание/сворачивание узлов
   - Контекстное меню для работы с объектами

3. **Интеграция с Pilot**
   - Вставка атрибутов объекта в редактор
   - Копирование имен объектов
   - Синхронизация с изменениями в Pilot

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 СИНТАКСИС MARKDOWN

Заголовки:
  # Заголовок 1
  ## Заголовок 2
  ### Заголовок 3

Текст:
  **жирный текст**
  *курсив*
  ~~зачеркнутый~~

Списки:
  - Маркированный список
  1. Нумерованный список

Ссылки:
  [текст](https://example.com)

Изображения:
  ![описание](url)

Код:
  `встроенный код`
  
  ```
  блок кода
  ```

Цитаты:
  > текст цитаты

Таблицы:
  | Заголовок 1 | Заголовок 2 |
  |-------------|-------------|
  | Ячейка 1    | Ячейка 2    |

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔧 ПРИНЦИПЫ РАБОТЫ

1. **Архитектура MVVM**
   - Разделение логики (ViewModel) и представления (View)
   - Привязка данных через Binding
   - Команды для взаимодействия с UI

2. **Live Preview (живой предпросмотр)**
   - Конвертация Markdown → HTML через Markdig
   - Отображение HTML в WebView2
   - Debouncing (задержка 300мс) для оптимизации

3. **Загрузка объектов Pilot**
   - Подписка на объекты через IObjectsRepository
   - Асинхронная загрузка через SubscribeObjects
   - Обработка событий изменений объектов

4. **Редактирование HTML → Markdown**
   - Редактирование напрямую в WebView2
   - Автоматическая конвертация HTML обратно в Markdown
   - Поддержка основных HTML тегов

5. **Стилизация Pilot**
   - Использование цветов Pilot-ICE (#0078D4)
   - Иконки в стиле Pilot
   - Интеграция с темой приложения

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⌨️ ГОРЯЧИЕ КЛАВИШИ

  Ctrl+B — Жирный текст
  Ctrl+I — Курсив

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📁 СТРУКТУРА МОДУЛЯ

Views/
  - MarkdownEditorWithTreeControl.xaml (основное окно)
  - LiveMarkdownEditor.xaml (редактор с Live Preview)
  - ObjectTreeControl.xaml (дерево объектов)

ViewModels/
  - MarkdownEditorViewModel.cs (логика редактора)
  - ObjectTreeViewModel.cs (логика дерева)

Services/
  - MarkdownConverterService.cs (конвертация Markdown↔HTML)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

ℹ️ ТЕХНИЧЕСКАЯ ИНФОРМАЦИЯ

Версия: {versionString}
GUID: 74D48E03-4E09-4F9C-A657-C23615F0A862
Платформа: Pilot-ICE/ECM 26.1.0
.NET Framework: 4.8

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📞 ПОДДЕРЖКА

При возникновении проблем:
1. Проверьте версию Pilot-ICE/ECM
2. Убедитесь, что WebView2 Runtime установлен
3. Проверьте логи в Output Window Visual Studio

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }

        private void OpenMarkdownEditor()
        {
            try
            {
                var converterService = new MarkdownConverterService(AppDomain.CurrentDomain.BaseDirectory);
                var view = new MarkdownEditorWithTreeControl(converterService, _objectsRepository, _eventAggregator);
                
                // Получаем заголовок активной вкладки с проверкой
                var activeTabPageTitle = _tabServiceProvider.GetActiveTabPageTitle();
                if (string.IsNullOrEmpty(activeTabPageTitle))
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] No active tab page found");
                    return;
                }
                
                _tabServiceProvider.UpdateTabPageContent(activeTabPageTitle, Title, view);
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Editor with tree opened");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error opening editor: {ex.Message}");
            }
        }
    }
}
