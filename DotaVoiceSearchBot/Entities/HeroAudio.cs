using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace DotaVoiceSearchBot.Entities;

public class HeroAudio
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int HeroId { get; set; }
    public string AudioText { get; set; }
    public string AudioFilePath { get; set; }
    public string AudioWebUrl { get; set; } 
    public string TelegramFileId { get; set; }
    
    public NpgsqlTsVector SearchVector { get; set; } 

    [ForeignKey(nameof(HeroId))]
    public Hero Hero { get; set; }
}