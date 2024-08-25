using DotaVoiceSearchBot.Service;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace DotaVoiceSearchBot.Services
{
    public class TelegramBotService(string botToken,HttpClient httpClient, DotaVoiceSearchService voiceSearchService)
    {
        private readonly TelegramBotClient _botClient = new TelegramBotClient(botToken,httpClient);

        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions
            );

            Console.WriteLine("Bot is up and running.");
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.InlineQuery)
            {
                await OnInlineQueryReceived(botClient, update.InlineQuery!);
            }
            else if (update.Type == UpdateType.ChosenInlineResult)
            {
                await OnChosenInlineResultReceived(botClient, update.ChosenInlineResult!);
            }
        }

        private async Task OnInlineQueryReceived(ITelegramBotClient botClient, InlineQuery inlineQuery)
        {
            var results = await voiceSearchService.SearchHeroAudioClips(inlineQuery.Query);

            await botClient.AnswerInlineQueryAsync(inlineQuery.Id, results);
        }
        
        private async Task OnChosenInlineResultReceived(ITelegramBotClient botClient, ChosenInlineResult chosenInlineResult)
        {
            var audioId = int.Parse(chosenInlineResult.ResultId);
            var heroAudio = await voiceSearchService.GetHeroAudioByIdAsync(audioId);

            await using (var fileStream = new FileStream(heroAudio.AudioFilePath, FileMode.Open, FileAccess.Read))
            {
                var message = await botClient.SendAudioAsync(chosenInlineResult.From.Id, InputFile.FromStream(fileStream, Path.GetFileName(heroAudio.AudioFilePath)) , caption: heroAudio.AudioText);

                // Update the database with the new FileId
                heroAudio.TelegramFileId = message.Audio.FileId;
                await voiceSearchService.UpdateHeroAudioAsync(heroAudio);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error occurred: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}