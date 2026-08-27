namespace LanguageWise.MiniGamesService.Api.Feature.Associations;

public sealed class AssociationsService
{
    private readonly AssociationsGame game;

    public AssociationsService(string language)
    {
        game = new AssociationsGame(language);
    }

    public AssociationsState GetState() => game.GetState();

    public AssociationResult SelectPair(string firstWord, string secondWord) =>
        game.SelectPair(firstWord, secondWord);

    public void ResetGame() => game.Reset();
}
