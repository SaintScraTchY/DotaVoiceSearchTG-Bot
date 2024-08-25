using DotaVoiceSearchBot.Data;
using DotaVoiceSearchBot.Entities;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace DotaVoiceSearchBot.Scrapper;

public class HeroAudioScrapper
{
    private static readonly string LogFilePath = "debug.txt"; // Path to the log file
    private static readonly int MaxRetryAttempts = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(200);
    private readonly DotaVoiceContext _context;
    private static readonly string[] HeroNames = new string[]
    {
        // Add all hero names here, formatted correctly (e.g., "anti-mage", "axe", "bane", etc.)
        //"Abaddon","Alchemist","Ancient_Apparition","Anti-Mage","Arc_Warden","Axe","Bane","Batrider","Beastmaster","Bloodseeker","Bounty_Hunter","Brewmaster","Bristleback","Broodmother","Centaur_Warrunner","Chaos_Knight","Chen","Clinkz","Clockwerk","Crystal_Maiden","Dark_Seer","Dark_Willow","Dawnbreaker","Dazzle","Death_Prophet","Disruptor","Doom","Dragon_Knight","Drow_Ranger","Earth_Spirit","Earthshaker","Elder_Titan","Ember_Spirit","Enchantress","Enigma","Faceless_Void","Grimstroke","Gyrocopter","Hoodwink","Huskar","Invoker",
        "Io","Jakiro","Juggernaut","Keeper_of_the_Light","Kunkka","Legion_Commander","Leshrac","Lich","Lifestealer","Lina","Lion","Lone_Druid","Luna","Lycan","Magnus","Marci","Mars","Medusa","Meepo","Mirana","Monkey_King","Morphling","Muerta","Naga_Siren","Nature's_Prophet","Necrophos","Night_Stalker","Nyx_Assassin","Ogre_Magi","Omniknight","Oracle","Outworld_Destroyer","Pangolier","Phantom_Assassin","Phantom_Lancer","Phoenix","Primal_Beast","Puck","Pudge","Pugna","Queen_of_Pain","Razor","Riki","Ringmaster","Rubick","Sand_King","Shadow_Demon","Shadow_Fiend","Shadow_Shaman","Silencer","Skywrath_Mage","Slardar","Slark","Snapfire","Sniper","Spectre","Spirit_Breaker","Storm_Spirit","Sven","Techies","Templar_Assassin","Terrorblade","Tidehunter","Timbersaw","Tinker","Tiny","Treant_Protector","Troll_Warlord","Tusk","Underlord","Undying","Ursa","Vengeful_Spirit","Venomancer","Viper","Visage","Void_Spirit","Warlock","Weaver","Windranger","Winter_Wyvern","Witch_Doctor","Wraith_King","Zeus"
    };

    public HeroAudioScrapper(DotaVoiceContext context)
    {
        _context = context;
    }

    public async Task ScrapeAndStoreHeroAudios(string baseDirectory)
    {
        using HttpClient client = new HttpClient();

        var retryPolicy = CreateRetryPolicy();
        foreach (var heroName in HeroNames)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine($"starting Getting voices for {heroName}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            int currentHeroId;
            if (_context.Heroes.Any(x => x.HeroName == heroName))
            {
                currentHeroId = (await _context.Heroes.FirstOrDefaultAsync(x => x.HeroName == heroName)).Id;
            }
            else
            {
                var currentHero = await _context.Heroes.AddAsync(new Hero()
                {
                    HeroName = heroName
                });
                await _context.SaveChangesAsync();

                currentHeroId = currentHero.Entity.Id;
            }
            
            List<HeroAudioModel> audioModels = new List<HeroAudioModel>();
            var url = $"https://dota2.fandom.com/wiki/{heroName}/Responses";
            string response;
            try
            {
                response = await client.GetStringAsync(url);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
                await File.AppendAllTextAsync(LogFilePath,$" exception:{e.Message} for url : {url}\r\n");
                continue;
            }

            if (string.IsNullOrEmpty(response))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(url);
                Console.ForegroundColor = ConsoleColor.White;
                await File.AppendAllTextAsync(LogFilePath,$" null response for url : {url}\r\n");
                continue;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var liNodes = doc.DocumentNode.SelectNodes("//li[.//audio]");

            if (liNodes == null) continue;

            var heroDirectory = Path.Combine(baseDirectory, heroName);
            Directory.CreateDirectory(heroDirectory);
            
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine();
            Console.WriteLine($"Directory For {heroName} Created");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;

            foreach (var liNode in liNodes)
            {
                var audioElements = liNode.SelectNodes(".//audio/source");
                if (audioElements != null && audioElements.Count > 0)
                {
                    int version = 1;
                    foreach (var audioElement in audioElements)
                    {
                        var audioUrl = audioElement.GetAttributeValue("src", string.Empty);

                        var audioText = liNode.SelectSingleNode(".//span/following-sibling::text()")?.InnerText.Trim();

                        // Adjust audioText if there are multiple versions
                        if (audioElements.Count > 1)
                        {
                            audioText += $" (Version {version})";
                            version++;
                        }

                        if (await _context.HeroAudios.AnyAsync(x => x.AudioWebUrl == audioUrl))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine($"the {audioText} For {heroName} does exists , initiating Next");
                            Console.ForegroundColor = ConsoleColor.White;  
                            continue;
                        }

                        if (string.IsNullOrEmpty(audioUrl))
                            continue;

                        if (string.IsNullOrEmpty(audioText))
                        {
                            audioText = heroName switch
                            {
                                "Io" => "Io's Beeps",
                                "Marci" => "Marci's Whistle",
                                _ => heroName
                            };
                        }

                        var fileName = $"{(Path.GetFileName(audioUrl)).Sanitize()}.mp3";
                        var filePath = Path.Combine(heroDirectory, fileName);

                        // Download the audio file
                        await Task.Delay(125);

                        await retryPolicy.ExecuteAsync((async () =>
                        {
                            await using var audioStream = await  client.GetStreamAsync(audioUrl);
                            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                            await audioStream.CopyToAsync(fileStream);
                        }));

                        // Create the HeroAudio object (you can then save this to your database)
                        var heroAudio = new HeroAudioModel
                        {
                            AudioText = audioText,
                            AudioFilePath = filePath,
                            AudioWebUrl = audioUrl
                            // URL is not saved to DB, but can be used if needed
                        };
                        audioModels.Add(heroAudio);
                        Console.WriteLine($"Downloaded {audioText} for {heroName} to {filePath}");
                    }
                }
            }
            
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine();
            Console.WriteLine($"Getting voices for {heroName} Finished");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;

            if (audioModels.Count() <= 0) continue;
                
                List<HeroAudio> audioListToAdd = audioModels.Select(x => new HeroAudio()
                {
                    HeroId = currentHeroId,
                    AudioText = x.AudioText,
                    AudioFilePath = x.AudioFilePath,
                    AudioWebUrl = x.AudioWebUrl
                
                }).ToList();
                
                await _context.HeroAudios.AddRangeAsync(audioListToAdd);
                
                await _context.SaveChangesAsync();

        }
    }

    private AsyncRetryPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>(ex => ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(MaxRetryAttempts, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + InitialDelay, 
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    // Log each retry attempt
                    string logEntry = $"{DateTime.Now}: Retry {retryCount} after {timeSpan.Seconds} seconds delay due to {exception.Message}";
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                    Console.WriteLine(logEntry);
                });
    }
}

public class HeroAudioModel
{
    public string AudioText { get; set; }
    public string AudioFilePath { get; set; }
    public string AudioWebUrl { get; set; }
}