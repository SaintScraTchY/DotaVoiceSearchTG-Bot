using DotaVoiceSearchBot.Data;
using DotaVoiceSearchBot.Entities;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.InlineQueryResults;

namespace DotaVoiceSearchBot.Service
{
    public class DotaVoiceSearchService(DotaVoiceContext context)
    {
        public async Task<InlineQueryResult[]> SearchHeroAudioClips(string query)
        {
            return await (from audio in context.HeroAudios
                    join hero in context.Heroes on audio.HeroId equals hero.Id
                    where EF.Functions.ToTsVector("english", hero.HeroName)
                              .Matches(EF.Functions.PlainToTsQuery("english", query))
                          || EF.Functions.ToTsVector("english", audio.AudioText)
                              .Matches(EF.Functions.PlainToTsQuery("english", query))
                    select new InlineQueryResultAudio(
                        audio.Id.ToString(),
                        audio.AudioWebUrl,
                    hero.HeroName + ": " + audio.AudioText
                    )
                    {
                        Caption = hero.HeroName + ": " + audio.AudioText
                    }
                ).Take(10).ToArrayAsync();
        }

        public async Task<HeroAudio> GetHeroAudioByIdAsync(int id)
        {
            return await context.HeroAudios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateHeroAudioAsync(HeroAudio heroAudio)
        {
            context.Attach(heroAudio);
            context.HeroAudios.Update(heroAudio);
            await context.SaveChangesAsync();
        }
    }
}
