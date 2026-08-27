namespace LanguageWise.MiniGamesService.Api.Feature.WordStrings;

public sealed class WordStringsService
{
    private readonly WordStringsGame game;

    public WordStringsService(string language)
    {
        game = new WordStringsGame(language);
    }

    public WordStringsState GetState() => game.GetState();

    public WordStringsMoveResult SubmitWord(string word) => game.SubmitWord(word);

    public void ResetGame() => game.Reset();
}
