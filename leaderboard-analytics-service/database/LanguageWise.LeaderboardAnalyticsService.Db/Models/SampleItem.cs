namespace LanguageWise.LeaderboardAnalyticsService.Db.Models;

/// <summary>A row of the SampleItems table.</summary>
public sealed record SampleItem(int Id, string Name, string Description, string CreatedAt);

/// <summary>The payload accepted by the create and update endpoints.</summary>
public sealed record SampleItemInput(string? Name, string? Description);
