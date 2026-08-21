namespace LanguageWise.Student3.Api.Models;

/// <summary>The shape returned by the shared database microservice.</summary>
public sealed record SampleItem(int Id, string Name, string Description, string CreatedAt);
