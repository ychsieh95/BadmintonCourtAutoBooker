using System.Threading.Tasks;
using Telegram.Bot;

namespace BadmintonCourtAutoBooker
{
    internal class TelegramBot
    {
        private readonly string botToken;

        private readonly string channelId;

        public TelegramBot(string botToken, string channelId)
        {
            this.botToken = botToken;
            this.channelId = channelId;
        }

        public async Task SendMessageByTelegramBotAsync(string message)
        {
            TelegramBotClient telegramBotClient = new TelegramBotClient(botToken);
            await telegramBotClient.SendTextMessageAsync(
                chatId: channelId,
                text: message);
        }
    }
}
