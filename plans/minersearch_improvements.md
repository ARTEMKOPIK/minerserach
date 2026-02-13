# Детальный план реализации: Высокий приоритет

## Содержание
1. [Миграция на .NET 6/8](#1-миграция-на-net-68)
2. [WPF вместо WinForms](#2-wpf-вместо-winforms)
3. [Облачная база угроз](#3-облачная-база-угроз)
4. [YARA правила](#4-yara-правила)
5. [Тёмная тема](#5-тёмная-тема)
6. [Системный трей](#6-системный-трей)
7. [Веб-интерфейс](#7-веб-интерфейс)
8. [Telegram бот](#8-telegram-бот)

---

## 1. Миграция на .NET 6/8

### Описание
Переход с .NET Framework 4.7.2 на современный .NET 6/8 для кроссплатформенности, производительности и долгосрочной поддержки.

### Текущее состояние
- .NET Framework 4.7.2 (только Windows)
- Привязка к Windows API (P/Invoke)
- Ограниченные возможности развёртывания

### План реализации

#### Этап 1: Подготовка (2-3 недели)
- [ ] Аудит зависимостей и NuGet пакетов
- [ ] Определение Windows-specific кода (P/Invoke, Win32 API)
- [ ] Создание списка несовместимых библиотек
- [ ] Настройка нового проекта .NET 8

#### Этап 2: Миграция Core логики (4-6 недель)
- [ ] Перенос логики сканирования (MinerSearch.cs)
- [ ] Адаптация P/Invoke вызовов через Windows Compatibility Pack
- [ ] Замена устаревших API (например, System.Drawing → SkiaSharp)
- [ ] Обновление работы с процессами, реестром, службами
- [ ] Адаптация работы с COM объектами (NetFwTypeLib)

#### Этап 3: Миграция UI (3-4 недели)
- [ ] Создание параллельной WPF ветки (см. раздел 2)
- [ ] Или обновление WinForms проекта
- [ ] Тестирование всех форм и диалогов

#### Этап 4: Тестирование и релиз (2-3 недели)
- [ ] Модульное тестирование
- [ ] Интеграционное тестирование
- [ ] Тестирование на Windows 10/11
- [ ] Публикация релиза

### Технические детали

**Требуемые NuGet пакеты:**
```xml
<PackageReference Include="Microsoft.Windows.Compatibility" Version="8.0.0" />
<PackageReference Include="System.Drawing.Common" Version="8.0.0" />
```

**Пример адаптации P/Invoke:**
```csharp
// Было (.NET Framework)
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

// Станет (.NET 8)
[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, SECURITY_ATTRIBUTES* lpSecurityAttributes);
```

### Ожидаемые результаты
- Кроссплатформенность (Windows, Linux, macOS в будущем)
- Улучшенная производительность (10-30%)
- Долгосрочная поддержка Microsoft
- Меньший размер приложения

---

## 2. WPF вместо WinForms

### Описание
Замена устаревшего WinForms на WPF с поддержкой XAML, стилей, анимаций и аппаратного ускорения графики.

### Текущее состояние
- 9+ форм WinForms (SplashForm, QuarantineForm, FinishEx и др.)
- Ограниченные возможности кастомизации
- Устаревший внешний вид

### План реализации

#### Этап 1: Подготовка и планирование (1 неделя)
- [ ] Аудит всех WinForms компонентов
- [ ] Определение общих паттернов и стилей
- [ ] Проектирование новой архитектуры MVVM
- [ ] Создание базовой темы (Light/Dark)

#### Этап 2: Создание базовой инфраструктуры (2-3 недели)
- [ ] Настройка WPF проекта
- [ ] Создание базовых стилей и ресурсов
- [ ] Реализация MVVM фреймворка (MVVM Light или CommunityToolkit.Mvvm)
- [ ] Создание навигационной системы
- [ ] Реализация темной темы (см. раздел 5)

#### Этап 3: Перенос UI компонентов (6-8 недель)
- [ ] Главное окно сканирования (MainWindow)
- [ ] Splash экран (SplashWindow)
- [ ] Форма карантина (QuarantineWindow)
- [ ] Форма завершения (FinishWindow)
- [ ] Диалоговые окна (MessageBox, License, HostsDeletion)

#### Этап 4: Тестирование и миграция (2-3 недели)
- [ ] Функциональное тестирование
- [ ] Тестирование DPI и разрешений
- [ ] Сравнение производительности
- [ ] Документация изменений

### Архитектура MVVM

```
MinerSearch/
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── ScanViewModel.cs
│   ├── QuarantineViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── MainWindow.xaml
│   ├── ScanView.xaml
│   ├── QuarantineView.xaml
│   └── Dialogs/
├── Models/
│   ├── ScanResult.cs
│   ├── ThreatInfo.cs
│   └── QuarantineItem.cs
├── Services/
│   ├── IScannerService.cs
│   ├── IQuarantineService.cs
│   └── INotificationService.cs
└── Resources/
    ├── Styles/
    └── Themes/
```

### Пример XAML стиля для кнопки

```xml
<Style x:Key="PrimaryButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="#0078D4"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="16,8"/>
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        CornerRadius="4"
                        Padding="{TemplateBinding Padding}">
                    <ContentPresenter HorizontalAlignment="Center"/>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#106EBE"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

### Ожидаемые результаты
- Современный внешний вид
- Аппаратное ускорение графики
- Легкая поддержка тем (Light/Dark)
- Анимации и плавные переходы
- Привязка данных (Data Binding)
- MVVM архитектура

---

## 3. Облачная база угроз

### Описание
Онлайн-синхронизация базы сигнатур угроз с облачным сервером для актуальности обнаружения.

### Текущее состояние
- Статическая база в коде (MSData.cs, хешированные строки)
- Обновления только с релизом приложения
- ~2000+ хешей доменов и IP адресов

### План реализации

#### Этап 1: Проектирование API (1 неделя)
- [ ] Определение структуры JSON ответа
- [ ] Проектирование REST API endpoints
- [ ] Выбор хостинга (Azure Blob, AWS S3, GitHub Releases)
- [ ] Определение версионирования базы

#### Этап 2: Backend (2-3 недели)
- [ ] Скрипт генерации JSON с хешами
- [ ] API для получения обновлений
- [ ] Система версионирования
- [ ] Кэширование на сервере

#### Этап 3: Интеграция в приложение (2 недели)
- [ ] Добавление UpdateService
- [ ] Проверка обновлений при запуске
- [ ] Фоновое скачивание обновлений
- [ ] Интеграция с локальной базой
- [ ] Интерфейс настроек обновлений

#### Этап 4: Тестирование и релиз (1 неделя)
- [ ] Нагрузочное тестирование API
- [ ] Тестирование офлайн режима
- [ ] Документация формата данных

### Структура JSON API

```json
{
  "version": "1.4.9.0",
  "lastUpdate": "2024-01-15T10:30:00Z",
  "minAppVersion": "1.4.8.0",
  "signatures": {
    "domains": [
      {
        "hash": "6319434ad50ad9ec528bc21a6b2e9694",
        "original": "193.228.54.23",
        "type": "mining_pool",
        "addedDate": "2023-06-15"
      }
    ],
    "fileHashes": [
      {
        "hash": "a1b2c3d4e5f6...",
        "name": "xmrig.exe",
        "type": "miner",
        "severity": "high"
      }
    ],
    "processNames": [
      {
        "name": "svchost.exe",
        "path": "temp",
        "type": "suspicious_process"
      }
    ],
    "ports": [3333, 4444, 5555, 7777]
  }
}
```

### Клиентская логика обновления

```csharp
public class UpdateService
{
    private const string ApiUrl = "https://api.minersearch.com/v1/signatures";
    
    public async Task<SignatureDatabase> CheckAndDownloadAsync()
    {
        var currentVersion = Settings.CurrentSignatureVersion;
        var response = await _httpClient.GetAsync($"{ApiUrl}?current={currentVersion}");
        
        if (response.StatusCode == HttpStatusCode.NotModified)
            return null;
            
        var signatures = await response.Content.ReadAsAsync<SignatureDatabase>();
        await SaveToLocalAsync(signatures);
        return signatures;
    }
}
```

### Ожидаемые результаты
- Актуальная база угроз между релизами
- Меньший размер приложения (не хранить все в коде)
- Быстрое реагирование на новые угрозы
- Пользователь контролирует обновления

---

## 4. YARA правила

### Описание
Интеграция движка YARA для мощного и гибкого обнаружения вредоносного ПО по правилам.

### Текущее состояние
- Хеширование строк (MD5)
- Статические сигнатуры
- Ограниченная гибкость обнаружения

### План реализации

#### Этап 1: Подготовка (1 неделя)
- [ ] Выбор YARA движка (yrdotnet или Yara.NET)
- [ ] Изучение существующих правил для майнеров
- [ ] Проектирование структуры правил

#### Этап 2: Интеграция движка (2-3 недели)
- [ ] Добавление YARA NuGet пакета
- [ ] Создание YaraScanner сервиса
- [ ] Загрузка и компиляция правил
- [ ] Сканирование файлов через YARA

#### Этап 3: Создание правил (3-4 недели)
- [ ] Правила для известных майнеров (XMRig, Claymore и др.)
- [ ] Правила для криптоджекинга
- [ ] Правила для сетевых соединений
- [ ] Обновляемая коллекция правил

#### Этап 4: Оптимизация (1-2 недели)
- [ ] Кэширование скомпилированных правил
- [ ] Параллельное сканирование
- [ ] Интеграция в основной сканер
- [ ] Отображение YARA результатов в UI

### Примеры YARA правил

```yara
rule XMRig_Miner {
    meta:
        description = "XMRig cryptocurrency miner"
        author = "MinerSearch"
        date = "2024-01-15"
        severity = "high"
    
    strings:
        $str1 = "xmrig" nocase
        $str2 = " Donation" nocase
        $str3 = "cryptonight" nocase
        $str4 = { 4D 5A 90 00 } // PE header
        
    condition:
        any of them
}

rule Suspicious_Mining_Connection {
    meta:
        description = "Suspicious connection to mining pool"
        severity = "medium"
    
    strings:
        $pool1 = "mine.pool" nocase
        $pool2 = " stratum+tcp" nocase
        
    condition:
        any of them
}
```

### Интеграция в сканер

```csharp
public class YaraScannerService
{
    private YaraContext _context;
    private Dictionary<string, YaraRule> _compiledRules;
    
    public async Task<List<YaraMatch>> ScanFileAsync(string filePath)
    {
        var matches = new List<YaraMatch>();
        
        using (var scanner = new YaraScanner())
        {
            foreach (var rule in _compiledRules.Values)
            {
                var result = await scanner.ScanFileAsync(filePath, rule);
                if (result.Matched)
                    matches.Add(result);
            }
        }
        
        return matches;
    }
}
```

### Ожидаемые результаты
- Гибкое обнаружение новых майнеров
- Использование экспертных знаний сообщества
- Легкое обновление правил
- Многоуровневая проверка (хеши + YARA + поведение)

---

## 5. Тёмная тема

### Описание
Добавление поддержки тёмной темы интерфейса для комфортной работы в условиях низкой освещённости.

### Текущее состояние
- Только светлая тема
- Ограниченная цветовая схема WinForms

### План реализации

#### Этап 1: Проектирование (1 неделя)
- [ ] Определение цветовой палитры
- [ ] Выбор цветов для всех UI элементов
- [ ] Проектирование переключателя тем
- [ ] Создание ресурсных словарей

#### Этап 2: Реализация в WPF (2-3 недели)
- [ ] Создание Light и Dark тем
- [ ] Определение стилей для всех контролов
- [ ] Реализация переключателя тем
- [ ] Сохранение выбора пользователя
- [ ] Поддержка системной темы Windows

#### Этап 3: Тестирование (1 неделя)
- [ ] Тестирование всех форм в обеих темах
- [ ] Проверка читаемости текста
- [ ] Тестирование переключения на лету

### Цветовая палитра (Dark Theme)

| Элемент | Цвет | Hex |
|---------|------|-----|
| Фон основной | Тёмно-серый | #1E1E1E |
| Фон вторичный | Средний серый | #252526 |
| Фон карточек | Светло-серый | #2D2D30 |
| Текст основной | Белый | #FFFFFF |
| Текст вторичный | Светло-серый | #CCCCCC |
| Акцент | Синий | #0078D4 |
| Успех | Зелёный | #4CAF50 |
| Предупреждение | Оранжевый | #FF9800 |
| Опасность | Красный | #F44336 |
| Границы | Тёмный серый | #3F3F46 |

### Пример XAML ресурсов

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- Dark Theme Colors -->
    <Color x:Key="DarkBackgroundColor">#1E1E1E</Color>
    <Color x:Key="DarkSecondaryBackgroundColor">#252526</Color>
    <Color x:Key="DarkCardBackgroundColor">#2D2D30</Color>
    <Color x:Key="DarkTextColor">#FFFFFF</Color>
    <Color x:Key="DarkSecondaryTextColor">#CCCCCC</Color>
    <Color x:Key="DarkAccentColor">#0078D4</Color>
    
    <!-- Brushes -->
    <SolidColorBrush x:Key="DarkBackgroundBrush" Color="{StaticResource DarkBackgroundColor}"/>
    <SolidColorBrush x:Key="DarkCardBrush" Color="{StaticResource DarkCardBackgroundColor}"/>
    <SolidColorBrush x:Key="DarkTextBrush" Color="{StaticResource DarkTextColor}"/>
    <SolidColorBrush x:Key="DarkAccentBrush" Color="{StaticResource DarkAccentColor}"/>
    
    <!-- Dark Theme Button Style -->
    <Style x:Key="DarkButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource DarkAccentBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource DarkTextBrush}"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

### Переключатель тем

```csharp
public class ThemeService
{
    public Theme CurrentTheme { get; private set; }
    
    public void SetTheme(Theme theme)
    {
        CurrentTheme = theme;
        var resources = Application.Current.Resources;
        
        if (theme == Theme.Dark)
        {
            resources.MergedDictionaries.Clear();
            resources.MergedDictionaries.Add(new ResourceDictionary { 
                Source = new Uri("pack://application:,,,/Themes/DarkTheme.xaml") 
            });
        }
        else
        {
            resources.MergedDictionaries.Clear();
            resources.MergedDictionaries.Add(new ResourceDictionary { 
                Source = new Uri("pack://application:,,,/Themes/LightTheme.xaml") 
            });
        }
        
        Settings.CurrentTheme = theme;
    }
    
    public void ApplySystemTheme()
    {
        var isDarkMode = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1) == 0;
            
        SetTheme(isDarkMode ? Theme.Dark : Theme.Light);
    }
}
```

### Ожидаемые результаты
- Комфортная работа в тёмное время суток
- Соответствие системной теме Windows
- Экономия заряда на OLED экранах
- Профессиональный вид приложения

---

## 6. Системный трей

### Описание
Добавление иконки в системный трей Windows с контекстным меню для быстрого доступа к функциям.

### Текущее состояние
- Только оконный режим
- Нет фоновой работы

### План реализации

#### Этап 1: Подготовка (1 неделя)
- [ ] Проектирование функциональности трея
- [ ] Создание иконок для разных состояний
- [ ] Определение контекстного меню

#### Этап 2: Реализация (1-2 недели)
- [ ] Добавление NotifyIcon компонента
- [ ] Обработка событий (двойной клик, правый клик)
- [ ] Реализация контекстного меню
- [ ] Управление состоянием иконки (сканирование, угроза, спокойно)
- [ ] Всплывающие уведомления (Toast)

#### Этап 3: Фоновая работа (2-3 недели)
- [ ] Служба мониторинга (опционально)
- [ ] Уведомления о найденных угрозах
- [ ] Быстрый запуск сканирования из трея
- [ ] Минимизация при закрытии (опционально)

### Функции контекстного меню

| Пункт | Описание |
|-------|----------|
| Запустить сканирование | Быстрый запуск полного сканирования |
| Быстрое сканирование | Сканирование только критических областей |
| Открыть приложение | Показать главное окно |
| Последний отчёт | Показать результаты последнего сканирования |
| Настройки | Открыть окно настроек |
| Выход | Закрыть приложение |

### Состояния иконки

```csharp
public enum TrayIconState
{
    Normal,        // Зелёная - система в порядке
    Scanning,      // Синяя - идёт сканирование
    ThreatFound,   // Красная - обнаружена угроза
    Warning,       // Оранжевая - требуется внимание
    Disabled       // Серая - приложение отключено
}
```

### Реализация

```csharp
public class TrayIconService
{
    private NotifyIcon _notifyIcon;
    private ContextMenuStrip _contextMenu;
    
    public void Initialize()
    {
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Запустить сканирование", null, OnScanClick);
        _contextMenu.Items.Add("Быстрое сканирование", null, OnQuickScanClick);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Открыть", null, OnOpenClick);
        _contextMenu.Items.Add("Настройки", null, OnSettingsClick);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Выход", null, OnExitClick);
        
        _notifyIcon = new NotifyIcon
        {
            Icon = Properties.Resources.tray_normal,
            Text = "MinerSearch",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };
        
        _notifyIcon.DoubleClick += OnTrayDoubleClick;
    }
    
    public void ShowNotification(string title, string message, ToolTipIcon icon)
    {
        _notifyIcon.ShowBalloonTip(5000, title, message, icon);
    }
    
    public void SetState(TrayIconState state)
    {
        _notifyIcon.Icon = state switch
        {
            TrayIconState.Normal => Properties.Resources.tray_normal,
            TrayIconState.Scanning => Properties.Resources.tray_scanning,
            TrayIconState.ThreatFound => Properties.Resources.tray_warning,
            _ => Properties.Resources.tray_normal
        };
    }
}
```

### Ожидаемые результаты
- Быстрый доступ к функциям
- Фоновые уведомления
- Экономия ресурсов при минимизации
- Удобство для опытных пользователей

---

## 7. Веб-интерфейс

### Описание
Добавление веб-интерфейса (SPA) для управления сканером через браузер.

### Текущее состояние
- Только десктопное WinForms приложение
- Нет удалённого доступа

### План реализации

#### Этап 1: Проектирование API (2 недели)
- [ ] Определение REST API endpoints
- [ ] Проектирование WebSocket для real-time обновлений
- [ ] Определение аутентификации
- [ ] Выбор технологий (ASP.NET Core + React/Vue/Blazor)

#### Этап 2: Backend реализация (4-6 недель)
- [ ] Настройка ASP.NET Core проекта
- [ ] Реализация SignalR хаба
- [ ] Создание API контроллеров
- [ ] Интеграция с Core логикой MinerSearch
- [ ] Аутентификация и авторизация
- [ ] WebSocket для прогресса сканирования

#### Этап 3: Frontend реализация (4-6 недель)
- [ ] Настройка React/Vue проекта
- [ ] Создание компонентов UI
- [ ] Реализация Dashboard
- [ ] Страница сканирования
- [ ] Менеджер карантина
- [ ] Настройки
- [ ] Подключение к WebSocket

#### Этап 4: Деплой и безопасность (2 недели)
- [ ] Настройка HTTPS
- [ ] Аутентификация
- [ ] Логирование
- [ ] Документация API
- [ ] Docker контейнер

### Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                      Web Browser                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │   React     │  │   Vue.js    │  │   Blazor WebAssembly │ │
│  │   SPA       │  │   SPA       │  │                     │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            │
                      HTTPS / WSS
                            │
┌─────────────────────────────────────────────────────────────┐
│                   ASP.NET Core API                          │
│  ┌───────────────┐  ┌───────────────┐  ┌────────────────┐  │
│  │ Scanner API   │  │  SignalR Hub  │  │ Auth/JWT       │  │
│  └───────────────┘  └───────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                     Internal IPC
                            │
┌─────────────────────────────────────────────────────────────┐
│                   MinerSearch Core                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ Scanner  │  │ Quarantine│  │  Logger  │  │ Updater  │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### API Endpoints

| Method | Endpoint | Описание |
|--------|----------|----------|
| GET | /api/status | Текущий статус системы |
| POST | /api/scan/start | Запустить сканирование |
| POST | /api/scan/stop | Остановить сканирование |
| GET | /api/scan/results | Получить результаты |
| GET | /api/quarantine | Список карантина |
| POST | /api/quarantine/{id}/restore | Восстановить файл |
| DELETE | /api/quarantine/{id}/delete | Удалить из карантина |
| GET | /api/settings | Получить настройки |
| PUT | /api/settings | Обновить настройки |

### WebSocket сообщения

```json
// Прогресс сканирования
{
  "type": "scan_progress",
  "data": {
    "currentFile": "C:\\Windows\\System32\\...",
    "filesScanned": 1250,
    "threatsFound": 3,
    "percentComplete": 45
  }
}

// Обнаружена угроза
{
  "type": "threat_detected",
  "data": {
    "path": "C:\\Users\\Admin\\Downloads\\miner.exe",
    "type": "miner",
    "severity": "high",
    "action": "quarantined"
  }
}
```

### Ожидаемые результаты
- Удалённое управление через браузер
- Мониторинг с мобильных устройств
- Real-time обновления
- История сканирований
- Централизованное управление (в будущем)

---

## 8. Telegram бот

### Описание
Расширение функциональности Telegram для управления сканером и получения уведомлений.

### Текущее состояние
- Отправка отчётов через Telegram (уже есть в netlib/TelegramAPI.cs)
- Односторонняя коммуникация

### План реализации

#### Этап 1: Проектирование (1 неделя)
- [ ] Определение команд бота
- [ ] Проектирование пользовательских сценариев
- [ ] Выбор библиотеки (Telegram.Bot)
- [ ] Проектирование клавиатур

#### Этап 2: Реализация (3-4 недели)
- [ ] Создание TelegramBotService
- [ ] Регистрация команд
- [ ] Обработчик команд
- [ ] Интерактивные клавиатуры
- [ ] Аутентификация пользователей
- [ ] Логирование

#### Этап 3: Интеграция (2 недели)
- [ ] Интеграция с основным приложением
- [ ] Уведомления о сканировании
- [ ] Отчёты в реальном времени
- [ ] Управление карантином

#### Этап 4: Безопасность (1 неделя)
- [ ] Аутентификация по username
- [ ]ホワイト/чёрный список пользователей
- [ ] Защита от спама
- [ ] Шифрование чувствительных данных

### Команды бота

| Команда | Описание | Доступ |
|---------|----------|--------|
| /start | Приветствие и регистрация | Все |
| /help | Справка по командам | Все |
| /status | Статус системы | Авторизованные |
| /scan | Запустить сканирование | Авторизованные |
| /quarantine | Список карантина | Авторизованные |
| /restore [id] | Восстановить файл | Авторизованные |
| /delete [id] | Удалить файл | Авторизованные |
| /settings | Настройки уведомлений | Авторизованные |
| /logs | Получить последние логи | Авторизованные |
| /auth [код] | Аутентификация | Все |

### Пример реализации

```csharp
public class TelegramBotService : IDisposable
{
    private TelegramBotClient _bot;
    private readonly Dictionary<long, UserSession> _sessions = new();
    
    public async Task StartAsync(string token)
    {
        _bot = new TelegramBotClient(token);
        await _bot.SetWebhookAsync("");
        
        _bot.OnMessage += OnMessage;
        _bot.OnCallbackQuery += OnCallbackQuery;
    }
    
    private async Task OnMessage(Message msg, UpdateType type)
    {
        if (msg.Type != MessageType.Text)
            return;
            
        var chatId = msg.Chat.Id;
        
        switch (msg.Text)
        {
            case "/start":
                await HandleStartAsync(chatId);
                break;
            case "/scan":
                await HandleScanAsync(chatId);
                break;
            case "/status":
                await HandleStatusAsync(chatId);
                break;
            case "/quarantine":
                await HandleQuarantineAsync(chatId);
                break;
            default:
                await _bot.SendTextMessageAsync(chatId, "Неизвестная команда. Используйте /help");
                break;
        }
    }
    
    private async Task HandleScanAsync(long chatId)
    {
        if (!IsAuthorized(chatId))
        {
            await _bot.SendTextMessageAsync(chatId, "Вы не авторизованы. Используйте /auth");
            return;
        }
        
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Полное сканирование", "scan_full") },
            new[] { InlineKeyboardButton.WithCallbackData("Быстрое сканирование", "scan_quick") },
            new[] { InlineKeyboardButton.WithCallbackData("Отмена", "cancel") }
        });
        
        await _bot.SendTextMessageAsync(chatId, "Выберите тип сканирования:", 
            replyMarkup: keyboard);
    }
    
    public async Task SendScanProgressAsync(long chatId, ScanProgress progress)
    {
        await _bot.EditMessageTextAsync(
            chatId,
            progress.MessageId,
            $"Сканирование...\n\nФайлов: {progress.FilesScanned}\nУгроз: {progress.ThreatsFound}",
            replyMarkup: GetProgressKeyboard(progress));
    }
    
    public async Task SendThreatNotificationAsync(long chatId, ThreatInfo threat)
    {
        await _bot.SendTextMessageAsync(chatId,
            $"⚠️ Обнаружена угроза!\n\n" +
            $"Файл: {threat.FileName}\n" +
            $"Тип: {threat.Type}\n" +
            $"Путь: {threat.Path}",
            parseMode: ParseMode.Markdown);
    }
}
```

### Пример взаимодействия

```
Пользователь: /start
Бот: Добро пожаловать в MinerSearch Bot!
     Для авторизации используйте /auth <код>

Пользователь: /scan
Бот: Выберите тип сканирования:
     [Полное сканирование] [Быстрое сканирование] [Отмена]

Пользователь: (нажимает кнопку)
Бot: ✅ Сканирование начато!

Бot: ⚠️ Обнаружена угроза!
     Файл: miner.exe
     Тип: Cryptocurrency Miner
     Путь: C:\Users\Admin\Downloads\

Бot: ✅ Сканирование завершено
     Файлов: 12,450
     Угроз: 3
     Время: 2:34
```

### Ожидаемые результаты
- Мобильное управление сканером
- Уведомления в реальном времени
- Быстрый доступ к функциям
- Интеграция с существующим Telegram каналом

---

## Сводная таблица задач

| # | Задача | Сложность | Время |
|---|--------|-----------|-------|
| 1 | Миграция на .NET 6/8 | Высокая | 8-12 недель |
| 2 | WPF вместо WinForms | Высокая | 10-14 недель |
| 3 | Облачная база угроз | Средняя | 4-6 недель |
| 4 | YARA правила | Средняя | 6-8 недель |
| 5 | Тёмная тема | Средняя | 3-4 недели |
| 6 | Системный трей | Низкая | 2-3 недели |
| 7 | Веб-интерфейс | Высокая | 10-14 недель |
| 8 | Telegram бот | Средняя | 5-7 недель |

**Общее время реализации (последовательно):** ~48-68 недель  
**Рекомендуемый подход:** Параллельная разработка нескольких задач

---

## Рекомендуемый порядок реализации

1. **Миграция на .NET 8** → основа для всех остальных улучшений
2. **WPF + Тёмная тема** → современный UI (выполнять параллельно)
3. **Облачная база + YARA** → улучшение обнаружения (параллельно с UI)
4. **Системный трей** → после UI (1-2 недели)
5. **Telegram бот** → после базового функционала
6. **Веб-интерфейс** → большая задача, можно отложить

Хотите детализировать какую-либо конкретную задачу или перейти к реализации?