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

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Загружаем данные
            await BirthdayService.Instance.LoadDataAsync();

            // Запускаем бота
            var botTask = BotClient.Instance.StartAsync(cts.Token);

            // Запускаем ежедневную проверку
            await BirthdayService.Instance.StartDailyCheck(
                sendMessage: BotClient.Instance.SendMessageAsync,
                botClient: BotClient.Instance.botClient!,
                ct: cts.Token);

            await botTask;
        }
    }
}
