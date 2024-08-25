using System.ComponentModel.DataAnnotations;

namespace DotaVoiceSearchBot.Entities;

public class Hero
{
    [Key]
    public int Id { get; set; }
    public string HeroName { get; set; }

    public ICollection<HeroAudio> HeroAudios { get; set; }
}