using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Ascon.Pilot.SDK;
using Ascon.Pilot.SDK.Menu;

namespace PilotMarkdownModule
{
    /// <summary>
    /// Плагин контекстного меню для добавления атрибутов объекта в Markdown редактор
    /// </summary>
    [Export(typeof(IMenu<ObjectsViewContext>))]
    public class ObjectAttributesMenuPlugin : IMenu<ObjectsViewContext>
    {
        private const string ADD_ATTRIBUTES_COMMAND = "MarkdownModule_AddAttributes";

        [ImportingConstructor]
        public ObjectAttributesMenuPlugin()
        {
        }

        public void Build(IMenuBuilder builder, ObjectsViewContext context)
        {
            // Показываем пункт меню только если выбран один объект
            var selectedObjects = context.SelectedObjects?.ToList();
            if (selectedObjects == null || selectedObjects.Count != 1)
                return;

            // Добавляем пункт в контекстное меню
            builder.AddItem(ADD_ATTRIBUTES_COMMAND, 0)
                   .WithHeader("Добавить атрибуты в Markdown редактор");
        }

        public void OnMenuItemClick(string name, ObjectsViewContext context)
        {
            if (name != ADD_ATTRIBUTES_COMMAND)
                return;

            var selectedObject = context.SelectedObjects?.FirstOrDefault();
            if (selectedObject == null)
                return;

            // Получаем атрибуты объекта
            var attributesText = GetAttributesText(selectedObject);
            
            // Публикуем событие через статический агрегатор
            ExtensionEventAggregator.Publish(new InsertMarkdownTextEvent
            {
                Text = attributesText
            });
            
            System.Diagnostics.Debug.WriteLine($"[MarkdownModule] Published attributes: {attributesText.Length} chars");
        }

        private string GetAttributesText(IDataObject obj)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## Атрибуты объекта");
            sb.AppendLine();
            sb.AppendLine("```");
            
            foreach (var attr in obj.Attributes)
            {
                var value = attr.Value == DBNull.Value ? string.Empty : attr.Value?.ToString();
                sb.AppendLine($"{attr.Key} \"{value}\"");
            }
            
            sb.AppendLine("```");
            sb.AppendLine();
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// Событие для вставки текста в Markdown редактор
    /// </summary>
    public class InsertMarkdownTextEvent
    {
        public string Text { get; set; } = string.Empty;
    }
}
