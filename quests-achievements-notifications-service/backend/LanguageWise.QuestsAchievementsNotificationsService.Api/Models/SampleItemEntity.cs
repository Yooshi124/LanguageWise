using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

[Table("sample_items")]
public sealed class SampleItemEntity : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    public SampleItem ToModel() => new(Id, Name, Description, CreatedAt);

    public static SampleItemEntity FromModel(SampleItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        CreatedAt = item.CreatedAt
    };
}