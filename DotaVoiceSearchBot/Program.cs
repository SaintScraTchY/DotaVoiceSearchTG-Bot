using DotaVoiceSearchBot.Data;
using DotaVoiceSearchBot.Entities;
using DotaVoiceSearchBot.Scrapper;
using DotaVoiceSearchBot.Service;
using DotaVoiceSearchBot.Services;
using Microsoft.EntityFrameworkCore;
using MihaZupan;

namespace DotaVoiceSearchBot;

class Program
{
    static async Task Main(string[] args)
    {
        await using var context = new DotaVoiceContext();
        // Ensure database is created
        await context.Database.MigrateAsync();
        
        //await DoScrap(context);
        await StartBot(context);
    }

    private static async Task StartBot(DotaVoiceContext context)
    {
        // Set up the voice search service
        var voiceSearchService = new DotaVoiceSearchService(context);
        
        var proxy = new HttpToSocks5Proxy("127.0.0.1", 8086,internalServerPort:8086);

        // Set up HTTP client handler with proxy
        var httpClientHandler = new HttpClientHandler
        {
            Proxy = proxy,
            UseProxy = true
        };

        // Create an HttpClient with the proxy settings
        var httpClient = new HttpClient(httpClientHandler);

        // Initialize and start the bot
        var botService = new TelegramBotService(Constants.ApiKey,httpClient, voiceSearchService);
        botService.Start();

        Console.ReadLine();
    }

    private static async Task DoScrap(DotaVoiceContext context)
    {
        HeroAudioScrapper scrapper = new HeroAudioScrapper(context);
        await scrapper.ScrapeAndStoreHeroAudios("Voices");
        Console.WriteLine("Scrapping Finished");
        Console.ReadLine();
    }
}