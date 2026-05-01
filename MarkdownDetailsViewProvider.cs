using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using Ascon.Pilot.SDK;
using PilotMarkdownModule.Services;
using PilotMarkdownModule.ViewModels;
using PilotMarkdownModule.Views;

namespace PilotMarkdownModule
{
    /// <summary>
    /// Провайдер для отображения Markdown редактора в карточке элемента Обозревателя документов
    /// </summary>
    [Export(typeof(IDocumentsExplorerDetailsViewProvider))]
    public class MarkdownDetailsViewProvider : IDocumentsExplorerDetailsViewProvider
    {
        private readonly IObjectsRepository _repository;
        private readonly ITempFileService _tempFileService;
        private readonly IObjectModifier _objectModifier;
        private readonly IMarkdownConverterService _converterService;
        
        // Настраиваемые имена типов объектов
        private static readonly string[] SupportedTypeNames = new[]
        {
            "ecm_doc",    // Документ ЕСМ (основной тип)
            "document",   // Обычный документ
            "file",       // Файл
            "section"     // Раздел
        };

        [ImportingConstructor]
        public MarkdownDetailsViewProvider(
            IObjectsRepository repository,
            ITempFileService tempFileService,
            IObjectModifier objectModifier)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownDetailsViewProvider] Constructor started");
                
                _repository = repository;
                _tempFileService = tempFileService;
                _objectModifier = objectModifier;
                
                // Создаем конвертер напрямую, без MEF
                _converterService = new MarkdownConverterService();
                System.Diagnostics.Debug.WriteLine("[MarkdownDetailsViewProvider] MarkdownConverterService created directly");
            
            // Инициализируем список типов на основе настроенных имен
            Types = new List<IType>();
            
            foreach (var typeName in SupportedTypeNames)
            {
                var type = repository.GetType(typeName);
                if (type != null)
                {
                    Types.Add(type);
                    System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] Added type: {typeName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] Type not found: {typeName}");
                }
            }
            
            // Если ни один тип не найден, используем все типы из репозитория
            if (Types.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownDetailsViewProvider] No types found, using all types from repository");
                try
                {
                    Types = repository.GetTypes().ToList();
                    System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] Loaded {Types.Count} types from repository");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] Error loading types: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] Provider initialized with {Types.Count} types");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] ERROR in constructor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] Stack trace: {ex.StackTrace}");
            }
        }

        public FrameworkElement GetDetailsView(ObjectsViewContext context)
        {
            var selectedObject = context.SelectedObjects?.FirstOrDefault();
            System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] GetDetailsView called. Selected object: {selectedObject?.DisplayName ?? "null"}");
            
            if (selectedObject == null)
            {
                System.Diagnostics.Debug.WriteLine("[MarkdownDetailsViewProvider] No selected object, returning null");
                return null;
            }
            
            System.Diagnostics.Debug.WriteLine($"[MarkdownDetailsViewProvider] Selected object: {selectedObject.DisplayName}, ID: {selectedObject.Id}");
            
            // Всегда создаем вкладку Markdown для поддерживаемых типов
            // Файл будет создан во временной папке при первом редактировании
            System.Diagnostics.Debug.WriteLine("[MarkdownDetailsViewProvider] Creating MarkdownDetailsTabView...");
            var viewModel = new MarkdownDetailsViewModel(selectedObject, _converterService, _tempFileService, _objectModifier);
            var view = new MarkdownDetailsTabView
            {
                DataContext = viewModel
            };
            
            System.Diagnostics.Debug.WriteLine("[MarkdownDetailsViewProvider] View created successfully");
            return view;
        }

        public List<IType> Types { get; }
    }
}
