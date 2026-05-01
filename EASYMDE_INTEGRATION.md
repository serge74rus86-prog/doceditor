# EasyMDE Integration Guide

## Обзор

Интеграция WYSIWYG-редактора **EasyMDE** в модуль Markdown для Pilot-ICE/ECM.

## Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                     Pilot-ICE/ECM                           │
│  ┌─────────────────────────────────────────────────────┐    │
│  │          MarkdownEditorWithTreeControl              │    │
│  │  ┌──────────────┐  ┌────────────────────────────┐   │    │
│  │  │ ObjectTree   │  │   LiveMarkdownEditor       │   │    │
│  │  │ ViewModel    │  │   (WebView2 + EasyMDE)     │   │    │
│  │  │              │  │                            │   │    │
│  │  │ - Дерево     │  │  ┌──────────────────────┐  │   │    │
│  │  │   объектов   │  │  │   HTML + JS + CSS    │  │   │    │
│  │  │ - Атрибуты   │  │  │   EasyMDE Editor     │  │   │    │
│  │  └──────────────┘  │  └──────────────────────┘  │   │    │
│  │                    └────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## Компоненты

### 1. HTML-шаблон EasyMDE
**Файл:** `Resources/EasyMDE/Editor.html`

Содержит:
- Подключение EasyMDE CSS/JS (CDN или локально)
- Стилизацию под тему Pilot-ICE (#0078D4)
- JavaScript для инициализации редактора
- Обмен сообщениями с C# через `window.chrome.webview`

### 2. LiveMarkdownEditor (WebView2 Control)
**Файл:** `Views/LiveMarkdownEditor.xaml(.cs)`

Функции:
- Инициализация WebView2
- Загрузка HTML-шаблона EasyMDE
- Обмен данными C# ↔ JS
- Методы: `SetEditorContent()`, `InsertText()`, `ApplyFormat()`

### 3. MarkdownEditorViewModel
**Файл:** `ViewModels/MarkdownEditorViewModel.cs`

Функции:
- Хранение Markdown-текста
- Переключение режимов (EasyMDE / Предпросмотр)
- Команды: CopyHtml, Clear, TogglePreview

## Обмен данными

### C# → JavaScript

```csharp
// Установка контента
var script = $"setMarkdownContent({JsonSerializer.Serialize(markdown)})";
EditorWebView.CoreWebView2.ExecuteScriptAsync(script);

// Вставка текста
var script = $"insertTextAtCursor({JsonSerializer.Serialize(text)})";
EditorWebView.CoreWebView2.ExecuteScriptAsync(script);

// Применение форматирования
var script = $"applyFormat({JsonSerializer.Serialize("bold")})";
EditorWebView.CoreWebView2.ExecuteScriptAsync(script);
```

### JavaScript → C#

```javascript
// Отправка сообщения из JS
window.chrome.webview.postMessage(JSON.stringify({
    type: 'content',
    content: easyMDE.value()
}));
```

```csharp
// Получение в C#
EditorWebView.CoreWebView2.WebMessageReceived += (s, e) =>
{
    var messageJson = e.TryGetWebMessageAsString();
    var message = JsonSerializer.Deserialize<EasyMdeMessage>(messageJson);
    
    if (message.type == "content")
    {
        MarkdownText = message.content;
    }
};
```

## API редактора

### Методы JavaScript (вызываются из C#)

| Метод | Описание | Пример |
|-------|----------|--------|
| `setMarkdownContent(markdown)` | Установить контент | `setMarkdownContent("# Hello")` |
| `getMarkdownContent()` | Получить контент | `getMarkdownContent()` |
| `insertTextAtCursor(text)` | Вставить в позицию курсора | `insertTextAtCursor("**bold**")` |
| `applyFormat(command)` | Применить форматирование | `applyFormat("bold")` |

### Команды форматирования

- `bold` - Жирный текст
- `italic` - Курсив
- `strikethrough` - Зачеркнутый
- `heading` - Заголовок
- `code` - Блок кода
- `quote` - Цитата
- `unordered-list` - Маркированный список
- `ordered-list` - Нумерованный список
- `link` - Ссылка
- `image` - Изображение
- `table` - Таблица

## Горячие клавиши

| Клавиши | Действие |
|---------|----------|
| `Ctrl+B` | Жирный текст |
| `Ctrl+I` | Курсив |
| `Ctrl+Alt+H` | Заголовок |
| `Ctrl+Alt+T` | Таблица |

## Стилизация

### Цвета темы Pilot-ICE

```css
:root {
    --pilot-accent: #0078D4;
    --pilot-accent-hover: #005A9E;
    --pilot-background: #FFFFFF;
    --pilot-background-secondary: #F3F2F1;
    --pilot-text: #323130;
    --pilot-text-secondary: #605E5C;
    --pilot-border: #8A8886;
    --pilot-border-light: #E1DFDD;
}
```

### Темная тема

Поддерживается через атрибут `data-theme="dark"`:

```css
[data-theme="dark"] {
    --pilot-accent: #4FC3F7;
    --pilot-background: #1E1E1E;
    --pilot-text: #CCCCCC;
}
```

## Режимы работы

### 1. Режим редактирования (EasyMDE)
- По умолчанию
- Визуальное редактирование Markdown
- Встроенная панель инструментов
- Live Preview (side-by-side)

### 2. Режим предпросмотра
- HTML-предпросмотр
- Генерация через Markdig
- Копирование HTML

## Подключение локальных файлов (опционально)

Вместо CDN можно использовать локальные файлы:

1. Скачайте файлы EasyMDE:
   - https://cdn.jsdelivr.net/npm/easymde/dist/easymde.min.css
   - https://cdn.jsdelivr.net/npm/easymde/dist/easymde.min.js

2. Сохраните в `Resources/EasyMDE/`

3. Обновите `Editor.html`:
```html
<link rel="stylesheet" href="easymde.min.css">
<script src="easymde.min.js"></script>
```

4. Добавьте в `.csproj`:
```xml
<ItemGroup>
  <None Include="Resources\EasyMDE\easymde.min.css">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Include="Resources\EasyMDE\easymde.min.js">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Зависимости

### NuGet пакеты
- `Microsoft.Web.WebView2` (1.0.3800.47)
- `System.Text.Json` (8.0.5)
- `Markdig` (0.45.0) - для режима предпросмотра

### JavaScript библиотеки
- `EasyMDE` (последняя версия через CDN)
- `CodeMirror` (входит в EasyMDE)

## Требования

- .NET Framework 4.8
- WebView2 Runtime
- Pilot-ICE/ECM 26.1.0

## Установка WebView2 Runtime

Если WebView2 не установлен:
1. Скачайте с https://developer.microsoft.com/en-us/microsoft-edge/webview2/
2. Установите WebView2 Runtime
3. Перезапустите Pilot-ICE

## Структура файлов

```
PilotMarkdownModule/
├── Resources/
│   └── EasyMDE/
│       └── Editor.html         # HTML-шаблон редактора
├── Views/
│   ├── LiveMarkdownEditor.xaml # WebView2 контрол
│   ├── LiveMarkdownEditor.xaml.cs
│   └── MarkdownEditorWithTreeControl.xaml
├── ViewModels/
│   └── MarkdownEditorViewModel.cs
└── PilotMarkdownModule.csproj
```

## Отладка

### Включение DevTools

```csharp
EditorWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
```

Откройте DevTools: `Ctrl+Shift+I` (в режиме отладки)

### Логирование

```javascript
console.log('[EasyMDE] Message');
```

```csharp
System.Diagnostics.Debug.WriteLine("[MarkdownModule] Message");
```

## Возможные проблемы

### 1. Редактор не загружается
**Причина:** WebView2 не установлен или CDN недоступен

**Решение:**
- Установите WebView2 Runtime
- Проверьте подключение к интернету
- Используйте локальные файлы EasyMDE

### 2. Текст не синхронизируется
**Причина:** Ошибка в обмене сообщениями

**Решение:**
- Проверьте логи в Output Window
- Убедитесь, что `WebMessageReceived` обработчик подключен

### 3. Стили не применяются
**Причина:** Неправильный путь к CSS

**Решение:**
- Проверьте путь в `Editor.html`
- Используйте абсолютные пути или CDN

## Расширение функционала

### Добавление кнопок на панель EasyMDE

```javascript
toolbar: [
    'bold',
    'italic',
    '|',
    'heading',
    '|',
    {
        name: "custom-button",
        className: "fa fa-star",
        title: "Custom Action",
        action: function(editor) {
            // Ваше действие
            return false;
        }
    }
]
```

### Добавление расширений

```javascript
import CodeMirror from 'codemirror';
import 'codemirror/addon/merge/merge';

const easyMDE = new EasyMDE({
    // ...
    codeMirrorConfig: {
        theme: 'default',
        mode: 'markdown'
    }
});
```

## Поддержка

При возникновении проблем:
1. Проверьте логи в Output Window Visual Studio
2. Убедитесь, что WebView2 установлен
3. Проверьте версию Pilot-ICE/ECM
4. Убедитесь, что CDN доступен (или используйте локальные файлы)

## Версии

- **EasyMDE:** 2.18.0+ (CDN)
- **WebView2:** 1.0.3800.47
- **.NET Framework:** 4.8
- **Pilot-ICE/ECM:** 26.1.0
