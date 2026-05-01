using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Ascon.Pilot.SDK;
using Ascon.Pilot.SDK.Menu;

namespace PilotMarkdownModule
{
    /// <summary>
    /// Плагин контекстного меню для создания Markdown файлов в базе Pilot
    /// </summary>
    [Export(typeof(IMenu<ObjectsViewContext>))]
    public class CreateMarkdownFileMenuPlugin : IMenu<ObjectsViewContext>
    {
        private const string CREATE_MD_COMMAND = "MarkdownModule_CreateMarkdownFile";

        private readonly IObjectsRepository _repository;
        private readonly ITabServiceProvider _tabService;
        private readonly IObjectModifier _objectModifier;

        [ImportingConstructor]
        public CreateMarkdownFileMenuPlugin(
            IObjectsRepository repository,
            ITabServiceProvider tabService,
            IObjectModifier objectModifier)
        {
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] CreateMarkdownFileMenuPlugin constructor called");
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Repository: {(repository != null ? "OK" : "NULL")}");
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] TabService: {(tabService != null ? "OK" : "NULL")}");
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] ObjectModifier: {(objectModifier != null ? "OK" : "NULL")}");
            
            _repository = repository;
            _tabService = tabService;
            _objectModifier = objectModifier;
            
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] CreateMarkdownFileMenuPlugin initialized successfully");
        }

        public void Build(IMenuBuilder builder, ObjectsViewContext context)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] CreateMarkdownFileMenuPlugin.Build called");
                
                // Проверяем наличие выбранных объектов
                var selectedObjects = context.SelectedObjects?.ToList();
                if (selectedObjects == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] SelectedObjects is null");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Selected objects count: {selectedObjects.Count}");
                
                if (selectedObjects.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] No objects selected");
                    return;
                }
                
                var selectedObject = selectedObjects.First();
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Selected object: {selectedObject.DisplayName}, ID: {selectedObject.Id}");
                
                // Добавляем пункт меню
                builder.AddItem(CREATE_MD_COMMAND, 0)
                       .WithHeader("📝 Создать Markdown файл");
                
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Menu item added successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Build error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Stack trace: {ex.StackTrace}");
            }
        }

        public void OnMenuItemClick(string name, ObjectsViewContext context)
        {
            if (name != CREATE_MD_COMMAND)
                return;

            try
            {
                var parentObject = context.SelectedObjects?.FirstOrDefault();
                if (parentObject == null)
                    return;

                CreateMarkdownFile(parentObject);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error creating file: {ex.Message}");
                MessageBox.Show(
                    $"Ошибка при создании файла: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void CreateMarkdownFile(Ascon.Pilot.SDK.IDataObject parentObject)
        {
            // Запрашиваем имя файла у пользователя
            var fileName = PromptForFileName();
            if (string.IsNullOrWhiteSpace(fileName))
                return; // Пользователь отменил

            // Добавляем расширение .md если нужно
            if (!fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
                !fileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".md";
            }

            // Получаем тип объекта для документа ЕСМ
            var fileType = _repository.GetType("ecm_doc");
            if (fileType == null)
            {
                MessageBox.Show(
                    "Тип 'ecm_doc' не найден в репозитории Pilot.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            try
            {
                // Создаем новый объект через IObjectModifier
                var objectBuilder = _objectModifier.Create(parentObject, fileType);
                
                // Устанавливаем имя
                objectBuilder.SetAttribute("Name", fileName);
                
                // Добавляем начальное содержимое как файл
                var initialContent = GetDefaultMarkdownContent(fileName);
                var contentBytes = Encoding.UTF8.GetBytes(initialContent);
                var contentStream = new MemoryStream(contentBytes);
                
                var now = DateTime.Now;
                objectBuilder.AddFile(fileName, contentStream, now, now, now);
                
                // Применяем изменения
                _objectModifier.Apply();
                
                System.Diagnostics.Debug.WriteLine(
                    $"[MarkdownModule] Markdown file created: {fileName} under parent {parentObject.Id}"
                );

                // Показываем сообщение об успехе
                MessageBox.Show(
                    $"Файл '{fileName}' успешно создан.\n\nВыберите его в дереве объектов для редактирования.",
                    "Создание файла",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Error creating object: {ex.Message}");
                throw new InvalidOperationException($"Не удалось создать документ: {ex.Message}", ex);
            }
        }

        private string GetDefaultMarkdownContent(string fileName)
        {
            var title = Path.GetFileNameWithoutExtension(fileName);
            
            return $@"# {title}

## Описание

<!-- Введите описание здесь -->

## Содержание

- [Раздел 1](#раздел-1)

## Раздел 1

Текст раздела...

---
*Создано: {DateTime.Now:dd.MM.yyyy}*
";
        }

        private string PromptForFileName()
        {
            // Создаем простой диалог ввода
            var inputDialog = new Window
            {
                Title = "Создание Markdown файла",
                Width = 400,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new System.Windows.Controls.Grid();
            grid.Margin = new Thickness(10);

            var stackPanel = new System.Windows.Controls.StackPanel();
            stackPanel.VerticalAlignment = VerticalAlignment.Center;

            var label = new System.Windows.Controls.TextBlock
            {
                Text = "Введите имя файла:",
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 12
            };

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = "Новый документ",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(4)
            };

            // Выделяем текст по умолчанию
            textBox.SelectAll();

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(0, 0, 5, 0),
                IsDefault = true
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Отмена",
                Width = 75,
                IsCancel = true
            };

            bool? result = false;
            okButton.Click += (s, e) =>
            {
                result = true;
                inputDialog.Close();
            };
            
            cancelButton.Click += (s, e) =>
            {
                result = false;
                inputDialog.Close();
            };

            // Обработка Enter
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    result = true;
                    inputDialog.Close();
                }
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            grid.Children.Add(stackPanel);
            inputDialog.Content = grid;

            // Показываем диалог
            inputDialog.ShowDialog();

            if (result == true)
            {
                var name = textBox.Text.Trim();
                
                // Проверяем на пустое имя
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show(
                        "Имя файла не может быть пустым.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return PromptForFileName(); // Повторяем запрос
                }

                return name;
            }

            return null;
        }
    }
}
