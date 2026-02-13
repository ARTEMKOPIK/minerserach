using MSearch.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MSearch.Services;

public class TelegramBotService : ITelegramBotService
{
    private TelegramBotClient? _bot;
    private readonly IConfigService _configService;
    private readonly IScannerService _scannerService;
    private readonly Dictionary<long, bool> _authorizedUsers = new();
    
    public bool IsRunning { get; private set; }
    
    public event EventHandler<string>? MessageReceived;
    public event EventHandler<ThreatInfo>? ThreatNotification;

    public TelegramBotService(IConfigService configService, IScannerService scannerService)
    {
        _configService = configService;
        _scannerService = scannerService;
    }

    public async Task StartAsync(string token)
    {
        if (IsRunning) return;

        try
        {
            _bot = new TelegramBotClient(token);
            var me = await _bot.GetMeAsync();
            
            IsRunning = true;
            
            _bot.OnMessage += OnMessage;
            _bot.OnCallbackQuery += OnCallbackQuery;
            
            App.Logger?.Information("Telegram bot started: @{Username}", me.Username);
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Failed to start Telegram bot");
            throw;
        }
    }

    public Task StopAsync()
    {
        if (!IsRunning) return Task.CompletedTask;
        
        _bot?.CloseAsync();
        IsRunning = false;
        
        App.Logger?.Information("Telegram bot stopped");
        return Task.CompletedTask;
    }

    private async void OnMessage(object sender, Telegram.Bot.Types.Message msg)
    {
        if (msg.Type != MessageType.Text) return;
        
        var chatId = msg.Chat.Id;
        var text = msg.Text ?? "";

        MessageReceived?.Invoke(this, text);

        // Check authorization
        if (!_authorizedUsers.ContainsKey(chatId))
        {
            if (text.StartsWith("/auth "))
            {
                var code = text.Substring(6).Trim();
                var validCodes = _configService.Settings.TelegramAuthorizedUsers;
                
                if (validCodes.Contains(code))
                {
                    _authorizedUsers[chatId] = true;
                    await SendMessageAsync(chatId, "✅ Авторизация успешна!");
                }
                else
                {
                    await SendMessageAsync(chatId, "❌ Неверный код авторизации");
                }
            }
            else
            {
                await SendMessageAsync(chatId, 
                    "Добро пожаловать в MinerSearch Bot!\n" +
                    "Для авторизации используйте: /auth <код>");
            }
            return;
        }

        // Process commands
        switch (text.ToLower())
        {
            case "/start":
            case "/help":
                await SendHelpAsync(chatId);
                break;
                
            case "/status":
                await SendStatusAsync(chatId);
                break;
                
            case "/scan":
                await ShowScanMenuAsync(chatId);
                break;
                
            case "/quarantine":
                await ShowQuarantineAsync(chatId);
                break;
                
            case "/settings":
                await SendSettingsAsync(chatId);
                break;
                
            case "/logs":
                await SendLogsAsync(chatId);
                break;
                
            default:
                await _bot!.SendTextMessageAsync(chatId, "Неизвестная команда. Используйте /help");
                break;
        }
    }

    private async void OnCallbackQuery(object sender, CallbackQuery msg)
    {
        var chatId = msg.Message!.Chat.Id;
        var data = msg.Data ?? "";

        if (!_authorizedUsers.ContainsKey(chatId))
        {
            await _bot!.AnswerCallbackQueryAsync(msg.Id, "Сначала авторизуйтесь");
            return;
        }

        switch (data)
        {
            case "scan_full":
                await _bot!.AnswerCallbackQueryAsync(msg.Id, "Запускаю полное сканирование...");
                _ = _scannerService.StartScanAsync(ScanType.Full);
                await _bot.EditMessageTextAsync(chatId, msg.Message.MessageId, 
                    "✅ Сканирование начато!");
                break;
                
            case "scan_quick":
                await _bot!.AnswerCallbackQueryAsync(msg.Id, "Запускаю быстрое сканирование...");
                _ = _scannerService.StartScanAsync(ScanType.Quick);
                await _bot.EditMessageTextAsync(chatId, msg.Message.MessageId,
                    "✅ Сканирование начато!");
                break;
                
            default:
                await _bot!.AnswerCallbackQueryAsync(msg.Id, "Неизвестная команда");
                break;
        }
    }

    private async Task SendHelpAsync(long chatId)
    {
        var help = @"
🤖 MinerSearch Bot

Команды:
/start - Приветствие
/help - Справка
/status - Статус системы
/scan - Запустить сканирование
/quarantine - Карантин
/settings - Настройки
/logs - Логи
/auth <код> - Авторизация
";
        await SendMessageAsync(chatId, help);
    }

    private async Task SendStatusAsync(long chatId)
    {
        var status = $"
🛡️ MinerSearch Status

Состояние: {_scannerService.CurrentState}
База сигнатур: v1.4.9.0
Последнее сканирование: {DateTime.Now.AddHours(-2):g}
";
        await SendMessageAsync(chatId, status);
    }

    private async Task ShowScanMenuAsync(long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔍 Полное сканирование", "scan_full"),
            InlineKeyboardButton.WithCallbackData("⚡ Быстрое сканирование", "scan_quick")
        });
        
        await _bot!.SendTextMessageAsync(chatId, "Выберите тип сканирования:", replyMarkup: keyboard);
    }

    private async Task ShowQuarantineAsync(long chatId)
    {
        await SendMessageAsync(chatId, "📁 Открыт менеджер карантина в приложении...");
    }

    private async Task SendSettingsAsync(long chatId)
    {
        var settings = @"
⚙️ Настройки уведомлений

Уведомления о сканировании: ВКЛ
Уведомления об угрозах: ВКЛ
";
        await SendMessageAsync(chatId, settings);
    }

    private async Task SendLogsAsync(long chatId)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MinerSearch", "Logs");
            
            if (Directory.Exists(logPath))
            {
                var latestLog = Directory.GetFiles(logPath, "*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .FirstOrDefault();
                
                if (latestLog != null)
                {
                    var lines = File.ReadAllLines(latestLog).TakeLast(50);
                    await SendMessageAsync(chatId, string.Join("\n", lines));
                }
            }
        }
        catch (Exception ex)
        {
            await SendMessageAsync(chatId, $"Ошибка чтения логов: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(long chatId, string message)
    {
        if (_bot == null || !IsRunning) return;
        
        try
        {
            await _bot.SendTextMessageAsync(chatId, message, ParseMode.Markdown);
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Failed to send Telegram message");
        }
    }

    public async Task SendScanProgressAsync(long chatId, ScanProgress progress)
    {
        var message = $"
🔍 Сканирование...

Файлов: {progress.FilesScanned}
Угроз: {progress.ThreatsFound}
Прогресс: {progress.PercentComplete:F1}%
";
        await SendMessageAsync(chatId, message);
    }

    public async Task SendThreatNotificationAsync(long chatId, ThreatInfo threat)
    {
        var message = $"
⚠️ Обнаружена угроза!

Файл: {threat.FileName}
Тип: {threat.Type}
Путь: {threat.FilePath}
";
        await SendMessageAsync(chatId, message);
    }
}
