using Telegram.Bot.Types.Enums;
using Telegram.Bot;

namespace BirthdayBuddyLeftChat.Services
{
    public class BirthdayService
    {
        private static BirthdayService? _instance;
        public static BirthdayService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BirthdayService();
                return _instance;
            }
        }

        public async Task StartDailyCheck(Func<long, string, Task> sendMessage, ITelegramBotClient botClient, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;

                // Начинаем с сегодняшних 9:00
                DateTime nextRun = now.Date.AddHours(9);

                // Пока это время в прошлом — добавляем день
                while (now > nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;

                Console.WriteLine($"[{now:T}] Следующая проверка: {nextRun:dd.MM HH:mm}");
                await Task.Delay(delay, ct);
                if (ct.IsCancellationRequested) break;

                try
                {
                    await ProcessDailyEvents(botClient, sendMessage);
                    //await SaveDataAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    await sendMessage(Settings.Instance.RootId, $"❌ Ошибка в ежедневной проверке: {ex.Message}");
                }

                delay = TimeSpan.FromHours(24);
            }
        }

        /// <summary>
        /// Обновление сообщения
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="sendMessage"></param>
        /// <returns></returns>
        private async Task ProcessDailyEvents(ITelegramBotClient botClient, Func<long, string, Task> sendMessage)
        {
            foreach (long chatId in DataStorage.Instance.ChatIds)
            {
                try
                {
                    string text = DataStorage.Instance.GenerateUpcomingBirthdaysText(chatId);
                    var msgId = DataStorage.Instance.GetPinnedMsgId(chatId);

                    if (msgId.HasValue)
                    {
                        if (DataStorage.Instance.IsNewBirthdaysList(chatId))
                        {
                            DataStorage.Instance.SaveBirthdaysList(chatId);

                            await botClient.UnpinChatMessage(chatId: chatId, messageId: msgId.Value, cancellationToken: default);

                            await botClient.DeleteMessage(chatId: chatId, messageId: msgId.Value, cancellationToken: default);

                            var msg = await botClient.SendMessage(chatId: chatId, text: text, parseMode: ParseMode.Markdown, cancellationToken: default);

                            DataStorage.Instance.SetPinnedMsgId(chatId, msg.MessageId);

                            await botClient.PinChatMessage(chatId: chatId, messageId: msg.MessageId, disableNotification: true, cancellationToken: default);
                        }
                        else
                            await botClient.EditMessageText(chatId: chatId, messageId: msgId.Value, text: text, parseMode: ParseMode.Markdown, cancellationToken: default);
                    }
                    else
                    {
                        var msg = await botClient.SendMessage(chatId: chatId, text: text, parseMode: ParseMode.Markdown, cancellationToken: default);

                        DataStorage.Instance.SetPinnedMsgId(chatId, msg.MessageId);

                        await botClient.PinChatMessage(chatId: chatId, messageId: msg.MessageId, disableNotification: true, cancellationToken: default);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Шапка чата {chatId}: {ex.Message}");
                }
            }
        }

        public void EnsureChatExists(long chatId)
        {
            // Просто гарантируем, что чат "известен" — любая операция его создаст
            // Можно расширить: сохранять название чата, флаг активности и т.п.
        }

        public async Task ForceUpdatePinnedMessage(long chatId)
        {
            try
            {
                string text = DataStorage.Instance.GenerateUpcomingBirthdaysText(chatId);
                var msgId = DataStorage.Instance.GetPinnedMsgId(chatId);

                if (msgId.HasValue)
                {
                    await BotClient.Instance.botClient!.EditMessageText(
                        chatId: chatId,
                        messageId: msgId.Value,
                        text: text,
                        parseMode: ParseMode.Markdown);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления шапки в {chatId}: {ex.Message}");
            }
        }
    }
}
