using BirthdayBuddyLeftChat.Services;
using SemenNewsBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

            // =============== Основной цикл — проверка RSS каждые 10 минут ===============
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    //await SemenovNoblRu.Instance.SemenovNoblRuExecuter(_botClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в RSS-обработчике: {ex.Message}");
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

                var chatId = message.Chat.Id;
                var text = message.Text;
                var from = message.From!;

                // Формируем имя
                string name = !string.IsNullOrEmpty(from.Username) ?
                    $"{(string.IsNullOrEmpty(from.LastName) ? "" : $"{from.LastName} ")}{from.FirstName}".Trim() + $" @{from.Username}" :
                    $"{(string.IsNullOrEmpty(from.LastName) ? "" : $"{from.LastName} ")}{from.FirstName}".Trim();

                // Добавляем в базу, если ещё нет
                BirthdayService.Instance.AddOrUpdateUser(chatId, from.Id, name);

                if (text?.StartsWith("/start") == true)
                {
                    await bot.SendMessage(chatId,
                        "Привет! Я помогаю следить за днями рождениями.\n" +
                        "Команды:\n" +
                        "/addbirthday ФИО,ДД.ММ.ГГГГ\n" +
                        "ФИО2,ДД.ММ.ГГГГ . . . и т.д.\n" +
                        "/list - вывести список именинников",
                        cancellationToken: ct);
                    return;
                }

                if (text?.StartsWith("/addbirthday") == true)
                {
                    string textMembers = text.Replace("/addbirthday", "").Trim();
                    string[] members = textMembers.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    foreach (string item in members)
                    {
                        var parts = item.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length != 2)
                        {
                            await bot.SendMessage(chatId, $"❌ Формат: /addbirthday ФИО,ДД.ММ.ГГГГ\n{item}", cancellationToken: ct);
                            continue;
                        }

                        string nameOrUsername = parts[0];
                        string dateStr = parts[1];

                        if (!DateTime.TryParse(dateStr, out var birthDate))
                        {
                            await bot.SendMessage(chatId, $"❌ Неверный формат даты.\n{item}", cancellationToken: ct);
                            continue;
                        }

                        long? userId = null;
                        string finalName = nameOrUsername;

                        // Если указан @username
                        if (nameOrUsername.StartsWith("@"))
                        {
                            var userInChat = BirthdayService.Instance._birthdays
                                .FirstOrDefault(b => b.ChatId == chatId && b.Name.Equals(nameOrUsername, StringComparison.OrdinalIgnoreCase));

                            if (userInChat != null)
                            {
                                userId = userInChat.UserId;
                                finalName = userInChat.Name; // например, @ivan или "Иван Петров"
                            }
                        }

                        // Если userId не найден — используем текущего отправителя
                        //userId ??= from.Id;

                        BirthdayService.Instance.AddBirthday(chatId, userId, finalName, birthDate);

                        await bot.SendMessage(chatId, $"✅ День рождения {finalName} добавлен: {birthDate:dd.MM.yyyy}", cancellationToken: ct);
                    }
                    await BirthdayService.Instance.SaveDataAsync();
                }

                if (text?.StartsWith("/scanmembers") == true)
                {
                    var knownUsers = BirthdayService.Instance._birthdays
                        .Where(b => b.ChatId == chatId)
                        .Select(b => b.Name)
                        .OrderBy(n => n)
                        .ToList();

                    if (!knownUsers.Any())
                    {
                        await bot.SendMessage(chatId, "Пока нет известных участников.", cancellationToken: ct);
                    }
                    else
                    {
                        var list = string.Join("\n", knownUsers.Select(u => "• " + u));
                        await bot.SendMessage(chatId, $"👥 Известные участники:\n{list}", cancellationToken: ct);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateHandler: {ex.Message}");
            }
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
                          "👥 Через пару минут соберу список участников.\n\n" +
                          "Команды:\n" +
                          "/addbirthday — добавить день рождения\n" +
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
    }
}
