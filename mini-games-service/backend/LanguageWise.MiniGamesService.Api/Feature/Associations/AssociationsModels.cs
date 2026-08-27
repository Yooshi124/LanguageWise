namespace LanguageWise.MiniGamesService.Api.Feature.Associations;

public sealed record AssociationsState(
    string Language,
    IReadOnlyList<string> Words,
    IReadOnlyList<string> SelectedWords,
    bool IsComplete);

public sealed record AssociationResult(
    string FirstWord,
    string SecondWord,
    bool IsAssociation,
    AssociationsState State);
