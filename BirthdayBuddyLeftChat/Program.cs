using BirthdayBuddyLeftChat.Services;
using SemenNewsBot;

namespace BirthdayBuddyLeftChat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Инициализируем настройки
            Settings.Init();

            var birthdayService = new BirthdayService();
            var botClient = new BotClient(Settings.Instance.TokenToAccess!, birthdayService);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Загружаем данные
            await birthdayService.LoadDataAsync();

            // Запускаем бота
            var botTask = botClient.StartAsync(cts.Token);

            // Запускаем ежедневную проверку
            await birthdayService.StartDailyCheck(
                sendMessage: botClient.SendMessageAsync,
                botClient: botClient._botClient,
                ct: cts.Token);

            await botTask;
        }
    }
}