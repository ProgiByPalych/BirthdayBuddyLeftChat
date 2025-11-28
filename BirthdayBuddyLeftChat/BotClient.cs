using BirthdayBuddyLeftChat.Models;
using BirthdayBuddyLeftChat.Services;
using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BirthdayBuddyLeftChat
{
    public class BotClient
    {
        private static BotClient? instance;
        public static BotClient Instance
        {
            get
            {
                if (instance == null)
                    instance = new BotClient(Settings.Instance.TokenToAccess!);
                return instance;
            }
        }

        // Это клиент для работы с Telegram Bot API, который позволяет отправлять сообщения, управлять ботом, подписываться на обновления и многое другое.
        private ITelegramBotClient? _botClient;
        public ITelegramBotClient? botClient { get { return _botClient; } }

        // Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
        private static ReceiverOptions? _receiverOptions;

        public BotClient(string token)
        {
            _botClient = new TelegramBotClient(token);  // Присваиваем нашей переменной значение, в параметре передаем Token, полученный от BotFather
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _receiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
            {
                AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
            {
                UpdateType.Message, // Сообщения (текст, фото/видео, голосовые/видео сообщения и т.д.)
            }
            };

            using var cts = new CancellationTokenSource();

            // UpdateHander - обработчик приходящих Update`ов
            // ErrorHandler - обработчик ошибок, связанных с Bot API
            // Запускаем приём обновлений
            _botClient!.StartReceiving(
                updateHandler: UpdateHandler,
                errorHandler: ErrorHandler,
                receiverOptions: _receiverOptions,
                cancellationToken: cts.Token
            ); // Запускаем бота

            User me = await _botClient!.GetMe(); // Создаем переменную, в которую помещаем информацию о нашем боте.
            Console.WriteLine($"{me.FirstName} запущен!");

            // =============== Основной цикл — каждые 10 минут ===============
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }

                // Асинхронная задержка — не блокирует поток
                await Task.Delay(TimeSpan.FromMinutes(10), cts.Token);
            }
            Console.WriteLine($"{me.FirstName} остановлен!");
        }

        private static async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
            try
            {
                if (update.MyChatMember != null)
                {
                    await Instance.HandleMyChatMemberAsync(bot, update.MyChatMember, ct);
                    return;
                }

                if (update.Message is not { } message) return;

                if (update.CallbackQuery is { } callback)
                {
                    await Instance.HandleCallbackAsync(bot, callback, ct);
                    return;
                }

                var chatId = message.Chat.Id;
                var text = message.Text;
                var from = message.From!;

                if (text?.StartsWith("/start") == true)
                {
                    await bot.SendMessage(chatId,
                        "Привет! Я помогаю следить за днями рождениями.\n" +
                        "Команды:\n" +
                        "/add ФИО@UserName,ДД.ММ.ГГГГ\n" +
                        "ФИО,ДД.ММ.ГГГГ (без UserName) и т.д.\n" +
                        "/del ФИО\n" +
                        "/list - вывести список именинников" +
                        "/export — выгрузить CSV",
                        cancellationToken: ct);
                    return;
                }

                if (text?.StartsWith("/add") == true)
                {
                    string textMembers = text.Replace("/add", "").Trim();
                    string[] members = textMembers.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    foreach (string item in members)
                    {
                        var parts = item.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length != 2)
                        {
                            await bot.SendMessage(chatId, $"❌ Формат: /add ФИО,ДД.ММ.ГГГГ\n{item}", cancellationToken: ct);
                            continue;
                        }

                        string nameOrUsername = parts[0];
                        string dateStr = parts[1];

                        if (!DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
                        {
                            await bot.SendMessage(chatId, $"❌ Неверный формат даты.\n{item}", cancellationToken: ct);
                            continue;
                        }

                        UserBirthday user = new UserBirthday();
                        user.ChatId = chatId;

                        // Если указан @username
                        if (nameOrUsername.Contains('@'))
                        {
                            var userInChat = DataStorage.Instance.GetUserByUserName(chatId, nameOrUsername.Split('@').Last());

                            if (userInChat != null)
                            {
                                user = userInChat;
                            }
                            nameOrUsername = nameOrUsername.Split('@').First();
                        }

                        user.BirthDate = birthDate;

                        var nameParts = nameOrUsername.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length == 0) { await bot.SendMessage(chatId, $"❌ ФИО не распознано.\n{nameOrUsername}", cancellationToken: ct); continue; }
                        if (nameParts.Length == 1) user.FirstName = nameParts.First();
                        if (nameParts.Length == 2) { user.LastName = nameParts.First(); user.FirstName = nameParts.Last(); }
                        if (nameParts.Length >= 3) { user.LastName = nameParts[0]; user.FirstName = nameParts[1]; user.Patronymic = nameParts[2]; }

                        DataStorage.Instance.AddBirthday(user);

                        await bot.SendMessage(chatId, $"✅ День рождения {user.GetFullName()} добавлен: {user.BirthDate:dd.MM.yyyy}", cancellationToken: ct);
                    }
                    await DataStorage.Instance.SaveBirthdayDataAsync();
                }

                if (text?.StartsWith("/list") == true)
                {
                    string list = "";
                    foreach (UserBirthday user in DataStorage.Instance.GetUsersByChatId(chatId))
                    {
                        if (user.ChatId == chatId)
                            list += $"{user.GetFullName()}\n{user.BirthDate:dd.MM.yyyy} ({user.GetAge()})\n";
                    }
                    
                    await bot.SendMessage(chatId, $"👥 Известные участники:\n{list}", cancellationToken: ct);
                }

                if (text?.StartsWith("/del") == true)
                {
                    string input = text.Replace("/del", "").Trim();

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        await bot.SendMessage(chatId, "❌ Укажите ФИО или @username для удаления.\nПример: `/del Иванов Иван` или `/del @ivan`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                        return;
                    }

                    // Поиск по username
                    List<UserBirthday> candidates = new();

                    if (input.StartsWith("@"))
                    {
                        string username = input.Substring(1);
                        var user = DataStorage.Instance.GetUserByUserName(chatId, username);
                        if (user != null) candidates.Add(user);
                    }
                    else
                    {
                        // Поиск по частичному совпадению ФИО (игнорируем регистр для удобства)
                        candidates = DataStorage.Instance.GetUsersByChatId(chatId)
                            .Where(u => !string.IsNullOrEmpty(u.GetFullName()) &&
                                        u.GetFullName().IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToList();
                    }

                    if (!candidates.Any())
                    {
                        await bot.SendMessage(chatId, $"⚠️ Никто не найден по запросу: `{input}`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                        return;
                    }

                    if (candidates.Count == 1)
                    {
                        var user = candidates[0];
                        string confirmText = $"Вы действительно хотите удалить?\n\n👤 {user.GetFullName()}\n📅 {user.BirthDate:dd.MM.yyyy}";

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new InlineKeyboardButton[] {
                                InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"del_confirm:{chatId}:{user.BirthDate.Ticks}:{user.GetFullName()}"),
                                InlineKeyboardButton.WithCallbackData("❌ Нет", "del_cancel")
                            }
                        });

                        await bot.SendMessage(chatId, confirmText, replyMarkup: keyboard, cancellationToken: ct);
                    }
                    else
                    {
                        // Несколько совпадений — показываем список с кнопками выбора
                        string msg = $"Найдено {candidates.Count} записей. Выберите, кого удалить:\n\n";
                        var buttons = new List<InlineKeyboardButton>();

                        foreach (var user in candidates.Take(10)) // Telegram ограничивает inline-кнопки ~100, но для UX — не более 10
                        {
                            string label = $"{user.GetFullName()} ({user.BirthDate:dd.MM})";
                            string callbackData = $"del_select:{chatId}:{user.BirthDate.Ticks}:{user.GetFullName()}";
                            buttons.Add(InlineKeyboardButton.WithCallbackData(label, callbackData));
                        }

                        var keyboard = new InlineKeyboardMarkup(buttons.Select(b => new[] { b }).ToArray());

                        await bot.SendMessage(chatId, msg, replyMarkup: keyboard, cancellationToken: ct);
                    }

                    return;
                }

                if (text?.StartsWith("/export") == true)
                {
                    var birthdays = DataStorage.Instance.GetUsersByChatId(chatId);

                    if (!birthdays.Any())
                    {
                        await bot.SendMessage(chatId, "📭 В этом чате ещё нет дней рождений.", cancellationToken: ct);
                        return;
                    }

                    // Генерируем CSV-контент
                    var csvLines = new List<string>
                    {
                        "Фамилия;Имя;Отчество;Дата рождения;Username;UserId"
                    };

                    foreach (var user in birthdays)
                    {
                        string line = $"{EscapeCsv(user.LastName)};" +
                                      $"{EscapeCsv(user.FirstName)};" +
                                      $"{EscapeCsv(user.Patronymic)};" +
                                      $"{user.BirthDate:dd.MM.yyyy};" +
                                      $"{EscapeCsv(user.UserName)};" +
                                      $"{user.UserId}";
                        csvLines.Add(line);
                    }

                    string csvContent = string.Join("\n", csvLines);
                    byte[] csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvContent)).ToArray();

                    // Создаём временный файл
                    string tempFileName = $"birthdays_{chatId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    string tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                    try
                    {
                        await File.WriteAllBytesAsync(tempPath, csvBytes, ct);

                        using var stream = File.OpenRead(tempPath);
                        await bot.SendDocument(
                            chatId: chatId,
                            document: new InputFileStream(stream, tempFileName),
                            caption: $"📄 Экспорт дней рождений ({birthdays.Count} записей)",
                            cancellationToken: ct);
                    }
                    finally
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateHandler: {ex.Message}");
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains('"') || value.Contains(';') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        private Task HandlePollingError(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            // Тут создадим переменную, в которую поместим код ошибки и её сообщение 
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(long chatId, string message)
        {
            try
            {
                _botClient!.SendMessage(chatId, message, parseMode: ParseMode.Markdown);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Send failed to {chatId}: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        private async Task HandleMyChatMemberAsync(ITelegramBotClient bot, ChatMemberUpdated chatMemberUpdated, CancellationToken ct)
        {
            var chat = chatMemberUpdated.Chat;
            var newStatus = chatMemberUpdated.NewChatMember.Status;

            // Проверяем, стал ли бот администратором
            if (newStatus == ChatMemberStatus.Administrator || newStatus == ChatMemberStatus.Creator)
            {
                Console.WriteLine($"🤖 Бот добавлен как админ в чат: {chat.Title} (ID: {chat.Id})");

                // Сохраняем чат в список отслеживаемых
                BirthdayService.Instance.EnsureChatExists(chat.Id);

                // Отправляем приветственное сообщение
                await bot.SendMessage(
                    chatId: chat.Id,
                    text: "🎉 Спасибо, что добавили меня как администратора!\n" +
                          "Теперь я могу помогать отслеживать дни рождения.\n\n" +
                          "📌 Я создам закреплённое сообщение с предстоящими ДР.\n" +
                          "Команды:\n" +
                          "/add — добавить день рождения\n" +
                          "/del — удалить день рождения\n" +
                          "/list — вывести список именинников" +
                          "/export — выгрузить CSV",
                    cancellationToken: ct);

                // Запускаем сбор участников
                //await BirthdayService.Instance.CollectChatMembersAsync(bot, chat.Id, ct);
            }
            else if (newStatus == ChatMemberStatus.Left || newStatus == ChatMemberStatus.Kicked)
            {
                Console.WriteLine($"❌ Бот был удалён из чата: {chat.Title} (ID: {chat.Id})");
                // Можно по желанию очистить данные или оставить
            }
        }

        private async Task HandleCallbackAsync(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
        {
            var chatId = callback.Message?.Chat.Id ?? 0;
            var fromId = callback.From.Id;
            var data = callback.Data;

            try
            {
                if (data == "del_cancel")
                {
                    await bot.EditMessageText(
                        chatId: chatId,
                        messageId: callback.Message.MessageId,
                        text: "❌ Удаление отменено.",
                        cancellationToken: ct);
                    return;
                }

                if (data.StartsWith("del_select:"))
                {
                    // Формат: del_select:chatId:ticks:fullName
                    var parts = data.Split(':', 4);
                    if (parts.Length != 4) throw new FormatException("Неверный формат данных");

                    long targetChatId = long.Parse(parts[1]);
                    long ticks = long.Parse(parts[2]);
                    string fullName = parts[3];

                    var birthDate = new DateTime(ticks);
                    var user = DataStorage.Instance.Birthdays
                        .FirstOrDefault(u => u.ChatId == targetChatId &&
                                             u.BirthDate.Ticks == ticks &&
                                             u.GetFullName() == fullName);

                    if (user == null)
                    {
                        await bot.AnswerCallbackQuery(callback.Id, "Запись не найдена.", cancellationToken: ct);
                        return;
                    }

                    string confirmText = $"Вы действительно хотите удалить?\n\n👤 {user.GetFullName()}\n📅 {user.BirthDate:dd.MM.yyyy}";

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                new InlineKeyboardButton[] {
                    InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"del_confirm:{targetChatId}:{ticks}:{fullName}"),
                    InlineKeyboardButton.WithCallbackData("❌ Нет", "del_cancel")
                }
            });

                    await bot.EditMessageText(
                        chatId: chatId,
                        messageId: callback.Message.MessageId,
                        text: confirmText,
                        replyMarkup: keyboard,
                        cancellationToken: ct);

                    return;
                }

                if (data.StartsWith("del_confirm:"))
                {
                    var parts = data.Split(':', 4);
                    if (parts.Length != 4) throw new FormatException("Неверный формат данных");

                    long targetChatId = long.Parse(parts[1]);
                    long ticks = long.Parse(parts[2]);
                    string fullName = parts[3];

                    var user = DataStorage.Instance.Birthdays
                        .FirstOrDefault(u => u.ChatId == targetChatId &&
                                             u.BirthDate.Ticks == ticks &&
                                             u.GetFullName() == fullName);

                    if (user == null)
                    {
                        await bot.EditMessageText(
                            chatId: chatId,
                            messageId: callback.Message.MessageId,
                            text: "❌ Запись уже удалена или не найдена.",
                            cancellationToken: ct);
                        return;
                    }

                    DataStorage.Instance.Birthdays.Remove(user);
                    await DataStorage.Instance.SaveBirthdayDataAsync();

                    await bot.EditMessageText(
                        chatId: chatId,
                        messageId: callback.Message.MessageId,
                        text: $"✅ Удалено: {user.GetFullName()}",
                        cancellationToken: ct);

                    // Опционально: обновить закреплённое сообщение сразу
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await BirthdayService.Instance.ForceUpdatePinnedMessage(targetChatId);
                        }
                        catch { /* ignore */ }
                    });

                    return;
                }

                await bot.AnswerCallbackQuery(callback.Id, "Неизвестная команда.", cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в HandleCallbackAsync: {ex}");
                await bot.AnswerCallbackQuery(callback.Id, "Ошибка обработки.", cancellationToken: ct);
            }
        }
    }
}
