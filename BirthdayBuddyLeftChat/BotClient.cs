using BirthdayBuddyLeftChat.Models;
using BirthdayBuddyLeftChat.Services;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace BirthdayBuddyLeftChat
{
    public class BotClient
    {
        private static BotClient? _instance;
        public static BotClient Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BotClient(Settings.Instance.TokenToAccess!);
                return _instance;
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

        private readonly string _stringCMD = "Команды:\n" +
                        "/add ФИО@UserName,ДД.ММ.ГГГГ\n" +
                        "ФИО,ДД.ММ.ГГГГ (без UserName) и т.д.\n" +
                        "/edit ФИО@UserName,ДД.ММ.ГГГГ\n" +
                        "/del ФИО\n" +
                        "/clear - Очистить список именинников.\n" +
                        "/list - Вывести список именинников.\n" +
                        "/export — Выгрузить CSV.";

        public async Task StartAsync(CancellationToken ct)
        {
            _receiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
            {
                AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
                {
                    UpdateType.Unknown,
                    UpdateType.Message,
                    UpdateType.InlineQuery,
                    UpdateType.ChosenInlineResult,
                    UpdateType.CallbackQuery,
                    UpdateType.EditedMessage,
                    UpdateType.ChannelPost,
                    UpdateType.EditedChannelPost,
                    UpdateType.ShippingQuery,
                    UpdateType.PreCheckoutQuery,
                    UpdateType.Poll,
                    UpdateType.PollAnswer,
                    UpdateType.MyChatMember,
                    UpdateType.ChatMember,
                    UpdateType.ChatJoinRequest,
                    UpdateType.MessageReaction,
                    UpdateType.MessageReactionCount,
                    UpdateType.ChatBoost,
                    UpdateType.RemovedChatBoost,
                    UpdateType.BusinessConnection,
                    UpdateType.BusinessMessage,
                    UpdateType.EditedBusinessMessage,
                    UpdateType.DeletedBusinessMessages,
                    UpdateType.PurchasedPaidMedia,
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
                long chatId;
                UserBirthday? user;
                switch (update.Type)
                {
                    case UpdateType.Unknown:
                        break;
                    case UpdateType.Message:
                        if (update.Message is not { } message) break;
                        if (message.PinnedMessage != null) break;
                        try
                        {
                            chatId = message.Chat.Id;
                            long fromId = message.From!.Id;
                            string cmd = "";
                            string data = "";
                            InlineKeyboardMarkup keyboard;
                            string confirmText;
                            Message? msg;
                            int delay;
                            string mesg = "";
                            string[]? parts;

                            // Берём только первую строку
                            string firstLine = message.Text?.Split('\n', '\r').FirstOrDefault()?.Trim() ?? "";

                            var match = Regex.Match(firstLine, @"^/(\S+)\s*(.*)$");

                            if (match.Success)
                            {
                                cmd = match.Groups[1].Value;
                                data = message.Text?.Replace($"/{cmd}", "").Trim();
                            }
                            else
                            {
                                msg = await bot.SendMessage(chatId, $"❌ Это не команда.", cancellationToken: ct);
                                return;
                            }

                            switch (cmd)
                            {
                                case "start":
                                    msg = await bot.SendMessage(
                                        chatId: chatId,
                                        text: $"Привет! Я помогаю следить за днями рождениями.\n{_instance?._stringCMD}",
                                        cancellationToken: ct);

                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(30_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: msg.MessageId,
                                                cancellationToken: ct);
                                            await Task.Delay(1_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: message.MessageId,
                                                cancellationToken: ct);
                                        }
                                        catch { /* ignore */ }
                                    });
                                    break;
                                case "add":
                                    string textMembers = data.Trim();
                                    string[] members = textMembers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                    mesg = "";

                                    foreach (string item in members)
                                    {
                                        parts = item.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length != 2)
                                        {
                                            mesg += $"❌ Формат: /add ФИО,ДД.ММ.ГГГГ\n{item}\n";
                                            continue;
                                        }

                                        string nameOrUsername = parts[0];
                                        string dateStr = parts[1];

                                        if (!DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
                                        {
                                            mesg += $"❌ Неверный формат даты.\n{item}\n";
                                            continue;
                                        }

                                        user = new UserBirthday();
                                        user.Id = GuidGenerator.GuidRandom();
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
                                        if (nameParts.Length == 0) { mesg += $"❌ ФИО не распознано.\n{nameOrUsername}\n"; continue; }
                                        if (nameParts.Length == 1) user.FirstName = nameParts.First();
                                        if (nameParts.Length == 2) { user.LastName = nameParts.First(); user.FirstName = nameParts.Last(); }
                                        if (nameParts.Length >= 3) { user.LastName = nameParts[0]; user.FirstName = nameParts[1]; user.Patronymic = nameParts[2]; }

                                        List<UserBirthday> existing = new();
                                        if (user.UserId != 0)
                                        {
                                            existing = DataStorage.Instance.Birthdays.Where(b => b.ChatId == user.ChatId && b.UserId == user.UserId).ToList();
                                        }
                                        else
                                        {
                                            existing = DataStorage.Instance.Birthdays.Where(b => b.GetFullName() == user.GetFullName() && b.BirthDate == user.BirthDate).ToList();
                                        }

                                        if (existing.Count != 0)
                                        {
                                            mesg += $"❌ Найдены совпадения:\n{item}\n";
                                            foreach (UserBirthday u in existing)
                                            {
                                                mesg += $"⚠️ {u.GetFullName()} {u.BirthDate:dd.MM.yyyy}\n";
                                            }
                                            continue;
                                        }

                                        DataStorage.Instance.AddBirthday(user);

                                        mesg += $"✅ День рождения {user.GetFullName()} добавлен: {user.BirthDate:dd.MM.yyyy}\n";
                                    }
                                    await DataStorage.Instance.SaveBirthdayDataAsync();
                                    msg = await bot.SendMessage(
                                        chatId: chatId,
                                        text: mesg,
                                        cancellationToken: ct);

                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(30_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: msg.MessageId,
                                                cancellationToken: ct);
                                            await Task.Delay(1_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: message.MessageId,
                                                cancellationToken: ct);
                                        }
                                        catch { /* ignore */ }
                                    });
                                    break;
                                case "list":
                                    string list = "";
                                    List<UserBirthday> users = DataStorage.Instance.GetUsersByChatId(chatId);
                                    if (users.Count == 0)
                                    {
                                        msg = await bot.SendMessage(
                                            chatId: chatId,
                                            text: $"👥 У Вас пока нет ни одной записи.",
                                            cancellationToken: ct);
                                        delay = 5_000;
                                    }
                                    else
                                    {
                                        foreach (UserBirthday userItem in users)
                                        {
                                            if (userItem.ChatId == chatId)
                                                list += $"{userItem.GetFullName()}\n{userItem.BirthDate:dd.MM.yyyy} ({userItem.GetFutureAge()})\n";
                                        }

                                        msg = await bot.SendMessage(
                                            chatId: chatId,
                                            text: $"👥 Известные участники:\n{list}\n(исполнится лет)",
                                            cancellationToken: ct);
                                        delay = 300_000;
                                    }

                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(delay, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: msg.MessageId,
                                                cancellationToken: ct);
                                            await Task.Delay(1_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: message.MessageId,
                                                cancellationToken: ct);
                                        }
                                        catch { /* ignore */ }
                                    });
                                    break;
                                case "del":
                                    msg = null;
                                    string input = data.Trim();

                                    if (string.IsNullOrWhiteSpace(input) || input == "")
                                    {
                                        msg = await bot.SendMessage(chatId, "❌ Укажите ФИО или @username для удаления.\nПример: `/del Иванов Иван`  или `/del @ivan`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                                    }
                                    else
                                    {
                                        // Поиск по username
                                        List<UserBirthday> candidates = new();

                                        if (input.StartsWith("@"))
                                        {
                                            string username = input.Substring(1);
                                            user = DataStorage.Instance.GetUserByUserName(chatId, username);
                                            if (user != null) candidates.Add(user);
                                        }
                                        else
                                        {
                                            // Поиск по частичному совпадению ФИО (игнорируем регистр для удобства)
                                            candidates = DataStorage.Instance.GetUsersByChatId(chatId)
                                                .Where(u => !string.IsNullOrEmpty(u.GetFullName()) && u.GetFullName().Contains(input))
                                                .ToList();
                                        }

                                        if (candidates.Count == 0)
                                        {
                                            msg = await bot.SendMessage(chatId, $"⚠️ Никто не найден по запросу: `{input}`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                                        }
                                        else if (candidates.Count == 1)
                                        {
                                            user = candidates[0];
                                            confirmText = $"Вы действительно хотите удалить?\n\n👤 {user.GetFullName()}\n📅 {user.BirthDate:dd.MM.yyyy}";

                                            keyboard = new InlineKeyboardMarkup(new[]
                                            {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"del_confirm:{user.UrlSafeId} {message.MessageId}"),
                                                InlineKeyboardButton.WithCallbackData("❌ Нет", $"cancel:{message.MessageId}")
                                            }
                                        });

                                            await bot.SendMessage(chatId, confirmText, replyMarkup: keyboard, cancellationToken: ct);
                                        }
                                        else
                                        {
                                            // Несколько совпадений — показываем список с кнопками выбора
                                            mesg = $"Найдено записей: {candidates.Count}. Выберите, кого удалить:\n\n";
                                            var buttons = new List<InlineKeyboardButton>();

                                            foreach (UserBirthday userItem in candidates.Take(9)) // Telegram ограничивает inline-кнопки ~100, но для UX — не более 10
                                            {
                                                string label = $"{userItem.GetFullName()} ({userItem.BirthDate:dd.MM.yyyy})";
                                                string callbackData = $"del_confirm:{userItem.UrlSafeId} {message.MessageId}";
                                                buttons.Add(InlineKeyboardButton.WithCallbackData(label, callbackData));
                                            }
                                            buttons.Add(InlineKeyboardButton.WithCallbackData("❌ Отмена", $"cancel:{message.MessageId}"));

                                            keyboard = new InlineKeyboardMarkup(buttons.Select(b => new[] { b }).ToArray());

                                            await bot.SendMessage(chatId, mesg, replyMarkup: keyboard, cancellationToken: ct);
                                        }
                                    }

                                    if (msg != null)
                                    {
                                        _ = Task.Run(async () =>
                                        {
                                            try
                                            {
                                                await Task.Delay(5_000, ct);
                                                await bot.DeleteMessage(
                                                    chatId: chatId,
                                                    messageId: msg.MessageId,
                                                    cancellationToken: ct);
                                                await Task.Delay(1_000, ct);
                                                await bot.DeleteMessage(
                                                    chatId: chatId,
                                                    messageId: message.MessageId,
                                                    cancellationToken: ct);
                                            }
                                            catch { /* ignore */ }
                                        });
                                    }
                                    
                                    break;
                                case "clear":
                                    confirmText = $"Вы действительно хотите удалить всех именинников?";
                                    keyboard = new InlineKeyboardMarkup(new[]
                                    {
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("✅ Да, удалить все!", $"clear_confirm:{message.MessageId}"),
                                            InlineKeyboardButton.WithCallbackData("❌ Нет", $"cancel:{message.MessageId}")
                                        }
                                    });

                                    await bot.SendMessage(chatId, confirmText, replyMarkup: keyboard, cancellationToken: ct);
                                    break;
                                case "export":
                                    var birthdays = DataStorage.Instance.GetUsersByChatId(chatId);

                                    if (!birthdays.Any())
                                    {
                                        msg = await bot.SendMessage(chatId, "📭 В этом чате ещё нет дней рождений.", cancellationToken: ct);
                                    }
                                    else
                                    {
                                        // Генерируем CSV-контент
                                        var csvLines = new List<string>
                                        {
                                            "Фамилия;Имя;Отчество;Дата рождения;Username;UserId"
                                        };

                                        foreach (UserBirthday userItem in birthdays)
                                        {
                                            string line = $"{EscapeCsv(userItem.LastName)};" +
                                                          $"{EscapeCsv(userItem.FirstName)};" +
                                                          $"{EscapeCsv(userItem.Patronymic)};" +
                                                          $"{userItem.BirthDate:dd.MM.yyyy};" +
                                                          $"{EscapeCsv(userItem.UserName)};" +
                                                          $"{userItem.UserId}";
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
                                            msg = await bot.SendDocument(
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

                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(1_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: update.Message.MessageId,
                                                cancellationToken: ct);
                                            await Task.Delay(30_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: msg.MessageId,
                                                cancellationToken: ct);
                                        }
                                        catch { /* ignore */ }
                                    });
                                    break;
                                case "edit":
                                    mesg = "";
                                    parts = data.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length != 2)
                                    {
                                        msg = await bot.SendMessage(
                                            chatId: chatId,
                                            text: $"❌ Формат: /add ФИО,ДД.ММ.ГГГГ\n{data}\n",
                                            cancellationToken: ct);
                                    }
                                    else
                                    {
                                        string nameOrUsername = parts[0];
                                        string dateStr = parts[1];

                                        if (!DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
                                        {
                                            msg = await bot.SendMessage(
                                                chatId: chatId,
                                                text: $"❌ Неверный формат даты.\n{data}\n",
                                                cancellationToken: ct);
                                        }
                                        else
                                        {
                                            var nameParts = nameOrUsername.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                            if (nameParts.Length == 0)
                                            {
                                                msg = await bot.SendMessage(
                                                    chatId: chatId,
                                                    text: $"❌ ФИО не распознано.\n{nameOrUsername}\n",
                                                    cancellationToken: ct);
                                            }
                                            else
                                            {
                                                user = new UserBirthday();
                                                user.Id = GuidGenerator.GuidRandom();
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

                                                if (nameParts.Length == 1) user.FirstName = nameParts.First();
                                                else if (nameParts.Length == 2) { user.LastName = nameParts.First(); user.FirstName = nameParts.Last(); }
                                                else { user.LastName = nameParts[0]; user.FirstName = nameParts[1]; user.Patronymic = nameParts[2]; }

                                                DataStorage.Instance.AddBirthday(user);

                                                mesg += $"✅ День рождения {user.GetFullName()} добавлен/изменен: {user.BirthDate:dd.MM.yyyy}\n";

                                                await DataStorage.Instance.SaveBirthdayDataAsync();
                                                msg = await bot.SendMessage(
                                                    chatId: chatId,
                                                    text: mesg,
                                                    cancellationToken: ct);
                                            }
                                        }
                                    }
                                    
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(30_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: msg.MessageId,
                                                cancellationToken: ct);
                                            await Task.Delay(1_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: message.MessageId,
                                                cancellationToken: ct);
                                        }
                                        catch { /* ignore */ }
                                    });
                                    break;
                                default:
                                    await bot.SendMessage(chatId, $"❌ Команда не найдена.", cancellationToken: ct);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка в UpdateType.Message: {ex}");
                        }
                        break;
                    case UpdateType.InlineQuery:
                        break;
                    case UpdateType.ChosenInlineResult:
                        break;
                    case UpdateType.CallbackQuery:
                        if (update.CallbackQuery is not { } callbackQuery) break;
                        try
                        {
                            chatId = callbackQuery.Message?.Chat.Id ?? 0;
                            var fromId = callbackQuery.From.Id;
                            string btn = callbackQuery.Data.Split(':').First();
                            string data = callbackQuery.Data.Replace($"{btn}:", "").Trim();
                            Message msg;

                            switch (btn)
                            {
                                case "cancel":
                                    await bot.EditMessageText(
                                        chatId: chatId,
                                        messageId: callbackQuery.Message.MessageId,
                                        text: "❌ Действие отменено.",
                                        cancellationToken: ct);

                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(10_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: callbackQuery.Message.MessageId,
                                                cancellationToken: ct);
                                            await Task.Delay(1_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: int.Parse(data),
                                                cancellationToken: ct);
                                        }
                                        catch { /* ignore */ }
                                    });
                                    break;
                                case "del_confirm":
                                    string[] members = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    user = DataStorage.Instance.GetUserByUrlSafeId(members[0]);

                                    if (user == null)
                                    {
                                        await bot.EditMessageText(
                                            chatId: chatId,
                                            messageId: callbackQuery.Message.MessageId,
                                            text: "❌ Запись уже удалена или не найдена.",
                                            cancellationToken: ct);
                                        return;
                                    }

                                    DataStorage.Instance.Birthdays.Remove(user);
                                    await DataStorage.Instance.SaveBirthdayDataAsync();

                                    await bot.EditMessageText(
                                        chatId: chatId,
                                        messageId: callbackQuery.Message.MessageId,
                                        text: $"✅ Удалено: {user.GetFullName()}",
                                        cancellationToken: ct);

                                    // Опционально: обновить закреплённое сообщение сразу
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await BirthdayService.Instance.ForceUpdatePinnedMessage(chatId);
                                            await Task.Delay(10_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: callbackQuery.Message.MessageId,
                                                cancellationToken: ct);
                                            await Task.Delay(1_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: int.Parse(members[1]),
                                                cancellationToken: ct);
                                        }
                                        catch { /* ignore */ }
                                    });
                                    break;
                                case "clear_confirm":
                                    List<UserBirthday> users = DataStorage.Instance.GetUsersByChatId(chatId);
                                    if (users.Count == 0)
                                    {
                                        await bot.EditMessageText(
                                            chatId: chatId,
                                            messageId: callbackQuery.Message.MessageId,
                                            text: $"👥 У Вас пока нет ни одной записи.",
                                            cancellationToken: ct);
                                    }
                                    else
                                    {
                                        foreach (UserBirthday userItem in users)
                                        {
                                            DataStorage.Instance.Birthdays.Remove(userItem);
                                        }
                                        await DataStorage.Instance.SaveBirthdayDataAsync();

                                        await bot.EditMessageText(
                                           chatId: chatId,
                                           messageId: callbackQuery.Message.MessageId,
                                           text: $"👥 Все именинники удалены!.",
                                           cancellationToken: ct);
                                    }

                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(5_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: callbackQuery.Message.MessageId,
                                                cancellationToken: ct);
                                            await Task.Delay(1_000, ct);
                                            await bot.DeleteMessage(
                                                chatId: chatId,
                                                messageId: int.Parse(data),
                                                cancellationToken: ct);
                                        }
                                        catch { /* ignore */ }
                                    });
                                    break;
                                default:
                                    await bot.AnswerCallbackQuery(callbackQuery.Id, "Неизвестная команда.", cancellationToken: ct);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка в UpdateType.CallbackQuery: {ex}");
                        }
                        break;
                    case UpdateType.EditedMessage:
                        break;
                    case UpdateType.ChannelPost:
                        break;
                    case UpdateType.EditedChannelPost:
                        break;
                    case UpdateType.ShippingQuery:
                        break;
                    case UpdateType.PreCheckoutQuery:
                        break;
                    case UpdateType.Poll:
                        break;
                    case UpdateType.PollAnswer:
                        break;
                    case UpdateType.MyChatMember:
                        if (update.MyChatMember is not { } myChatMember) break;
                        try
                        {
                            var chat = myChatMember.Chat;
                            var newStatus = myChatMember.NewChatMember.Status;

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
                                          "📌 Я создам закреплённое сообщение с предстоящими ДР.\n" + _instance?._stringCMD,
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
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка в UpdateType.MyChatMember: {ex}");
                        }
                        break;
                    case UpdateType.ChatMember:
                        break;
                    case UpdateType.ChatJoinRequest:
                        break;
                    case UpdateType.MessageReaction:
                        break;
                    case UpdateType.MessageReactionCount:
                        break;
                    case UpdateType.ChatBoost:
                        break;
                    case UpdateType.RemovedChatBoost:
                        break;
                    case UpdateType.BusinessConnection:
                        break;
                    case UpdateType.BusinessMessage:
                        break;
                    case UpdateType.EditedBusinessMessage:
                        break;
                    case UpdateType.DeletedBusinessMessages:
                        break;
                    case UpdateType.PurchasedPaidMedia:
                        break;
                    default:
                        break;
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
    }
}
