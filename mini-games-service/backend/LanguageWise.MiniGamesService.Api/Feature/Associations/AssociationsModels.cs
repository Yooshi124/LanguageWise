namespace LanguageWise.MiniGamesService.Api.Feature.Associations;

public sealed record AssociationsState(
    string Language,
    IReadOnlyList<string> Words,
    IReadOnlyList<string> SelectedWords,
    IReadOnlyList<AssociationGroup> SolvedGroups,
    int FailedAttempts,
    bool IsComplete,
    bool IsWon,
    IReadOnlyList<AssociationGroup> RevealedGroups);

public sealed record AssociationGroup(string Summary, IReadOnlyList<string> Words);

public sealed record AssociationsGuessRequest(IReadOnlyList<string> Words);

public sealed record AssociationResult(bool IsAssociation, AssociationGroup? Group, AssociationsState State);
