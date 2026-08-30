namespace LanguageWise.MiniGamesService.Api.Feature.Associations;

public sealed class AssociationsService
{
    private readonly AssociationsGame game;

    public AssociationsService(string language)
    {
        game = new AssociationsGame(language);
    }

    public AssociationsState GetState() => game.GetState();

    public AssociationResult SubmitGuess(IReadOnlyList<string> words) => game.SubmitGuess(words);

    public void ResetGame() => game.Reset();
}
