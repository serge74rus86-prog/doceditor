using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ascon.Pilot.SDK;
using PilotMarkdownModule.Services;
using PilotMarkdownModule.ViewModels;

namespace PilotMarkdownModule.Views
{
    /// <summary>
    /// Логика взаимодействия для MarkdownEditorWithTreeControl.xaml
    /// </summary>
    public partial class MarkdownEditorWithTreeControl : UserControl, IHandle<InsertMarkdownTextEvent>
    {
        private readonly MarkdownEditorViewModel _viewModel;
        private readonly ObjectTreeViewModel _treeViewModel;
        private readonly IEventAggregator? _eventAggregator;

        public MarkdownEditorWithTreeControl()
            : this(new MarkdownConverterService(), null, null)
        {
        }

        public MarkdownEditorWithTreeControl(
            IMarkdownConverterService converterService,
            IObjectsRepository? objectsRepository,
            IEventAggregator? eventAggregator)
        {
            InitializeComponent();

            _viewModel = new MarkdownEditorViewModel(converterService);
            _treeViewModel = new ObjectTreeViewModel(objectsRepository, eventAggregator);
            _eventAggregator = eventAggregator;

            DataContext = _viewModel;
            ObjectsTreeView.DataContext = _treeViewModel;

            // Подписка на события
            _eventAggregator?.Subscribe(this);
            ExtensionEventAggregator.Subscribe<InsertMarkdownTextEvent>(this);

            // Подписка на команды клавиатуры
            PreviewKeyDown += OnPreviewKeyDown;

            System.Diagnostics.Debug.WriteLine("[MarkdownModule] MarkdownEditorWithTreeControl initialized with EasyMDE");
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Пропускаем обработку, если включен режим предпросмотра
            if (_viewModel.IsPreviewEnabled)
                return;

            // Ctrl+B - жирный
            if (e.Key == Key.B && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownModule] Ctrl+B pressed - applying bold");
                LiveEditor?.ApplyFormat("bold");
                e.Handled = true;
            }
            // Ctrl+I - курсив
            else if (e.Key == Key.I && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownModule] Ctrl+I pressed - applying italic");
                LiveEditor?.ApplyFormat("italic");
                e.Handled = true;
            }
            // Ctrl+Alt+1..6 - заголовки H1-H6
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                int headingLevel = 0;
                
                if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                    headingLevel = 1;
                else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                    headingLevel = 2;
                else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                    headingLevel = 3;
                else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
                    headingLevel = 4;
                else if (e.Key == Key.D5 || e.Key == Key.NumPad5)
                    headingLevel = 5;
                else if (e.Key == Key.D6 || e.Key == Key.NumPad6)
                    headingLevel = 6;

                if (headingLevel > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Ctrl+Alt+{headingLevel} pressed - applying H{headingLevel}");
                    LiveEditor?.ApplyHeading(headingLevel);
                    e.Handled = true;
                }
            }
        }

        private void ObjectsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ObjectTreeNode node)
            {
                _treeViewModel.SelectedNode = node;
            }
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is ObjectTreeNode node)
            {
                _treeViewModel.LoadChildrenCommand.Execute(node);
            }
        }

        private void AddAttributesToEditor_Click(object sender, RoutedEventArgs e)
        {
            _treeViewModel.AddSelectedAttributesToEditor();
        }

        private void CopyName_Click(object sender, RoutedEventArgs e)
        {
            _treeViewModel.CopySelectedName();
        }

        private async void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    DefaultExt = ".pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Exporting to PDF: {dialog.FileName}");
                    
                    var success = await LiveEditor.ExportToPdfAsync(dialog.FileName);
                    
                    if (success)
                    {
                        MessageBox.Show(
                            $"Документ успешно экспортирован в PDF:\n{dialog.FileName}",
                            "Экспорт в PDF",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            "Не удалось экспортировать документ в PDF.",
                            "Ошибка экспорта",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] PDF export error: {ex.Message}");
                MessageBox.Show(
                    $"Ошибка при экспорте в PDF: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LiveEditor?.ShowPrintDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Print error: {ex.Message}");
                MessageBox.Show(
                    $"Ошибка при открытии диалога печати: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        public void Handle(InsertMarkdownTextEvent message)
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownModule] InsertMarkdownTextEvent received: {message.Text.Length} chars");
                
                // Вставляем текст через LiveEditor (EasyMDE)
                if (!_viewModel.IsPreviewEnabled)
                {
                    LiveEditor?.InsertText(message.Text);
                }
                else
                {
                    // В режиме предпросмотра просто добавляем к тексту
                    _viewModel.InsertText(message.Text);
                }
            }
        }
    }
}
