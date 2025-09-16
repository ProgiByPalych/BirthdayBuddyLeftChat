using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using BirthdayBuddyLeftChat.Services;

namespace BirthdayBuddyLeftChat
{
    public class BotClient
    {
        // Это клиент для работы с Telegram Bot API, который позволяет отправлять сообщения, управлять ботом, подписываться на обновления и многое другое.
        private readonly ITelegramBotClient? _botClient;

        // Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
        private static ReceiverOptions? _receiverOptions;

        private readonly BirthdayService? _birthdayService;

        public BotClient(string token, BirthdayService birthdayService)
        {
            _botClient = new TelegramBotClient(token);  // Присваиваем нашей переменной значение, в параметре передаем Token, полученный от BotFather
            _birthdayService = birthdayService;
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
            _botClient.StartReceiving(
                updateHandler: UpdateHandler,
                errorHandler: ErrorHandler,
                receiverOptions: _receiverOptions,
                cancellationToken: cts.Token
            ); // Запускаем бота

            User me = await _botClient.GetMe(); // Создаем переменную, в которую помещаем информацию о нашем боте.
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

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
            try
            {
                if (update.Message?.Text is not { } messageText)
                    return;

                var chatId = update.Message.Chat.Id;
                var message = update.Message.Text;

                switch (message.ToLower())
                {
                    case "/start":
                        await botClient.SendMessage(chatId, "Привет! Я бот для напоминаний о днях рождениях.");
                        break;

                    case "/birthdays":
                        var today = DateTime.Today;
                        var birthdays = _birthdayService.GetBirthdaysToday();
                        if (birthdays.Any())
                        {
                            var text = "🎉 Сегодня день рождения у:\n" +
                                       string.Join("\n", birthdays.Select(b => $"{b.Name} — {b.Age} лет"));
                            await botClient.SendMessage(chatId, text);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, "Сегодня никто не празднует день рождения.");
                        }
                        break;

                    default:
                        await botClient.SendMessage(chatId, "Я понимаю только команды /start и /birthdays");
                        break;
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
            return _botClient.SendMessage(chatId, message);
        }
    }
}
