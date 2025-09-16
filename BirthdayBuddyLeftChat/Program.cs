using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using SemenNewsBot;
using BirthdayBuddyLeftChat;

internal static class Program
{
    [STAThread]
    static async Task Main()
    {
        Settings.Init();
        static async Task Main(string[] args)
        {
            var birthdayService = new BirthdayService();
            var botClient = new BotClient(Settings.Instance.TokenToAccess, birthdayService);

            // Добавим тестовых пользователей
            birthdayService.AddBirthday("Иван", new DateTime(2000, 5, 20));
            birthdayService.AddBirthday("Мария", new DateTime(1995, 5, 21));
            birthdayService.AddBirthday("Алексей", new DateTime(1990, 12, 1));

            // Запуск бота и планировщика
            var cts = new CancellationTokenSource();

            // Запускаем бота асинхронно
            var botTask = botClient.StartAsync(cts.Token);

            // Запускаем проверку напоминаний каждый день в 9:00
            birthdayService.StartDailyCheck(botClient.SendMessageAsync, TimeSpan.FromHours(24), cts.Token);

            Console.WriteLine("Бот запущен. Нажмите Ctrl+C для выхода.");
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            await botTask;
        }
    }
}