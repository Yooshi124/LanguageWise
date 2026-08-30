namespace LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;

public sealed class GuessTheWordService
{
    private readonly GuessTheWordGame game;

    public GuessTheWordService(string language, ILearningContextProvider contextProvider)
    {
        var context = contextProvider.GetContext();
        game = new GuessTheWordGame(language, VocabularySelector.GetCandidates(context));
    }

    public GuessTheWordState GetState() => game.GetState();

    public GuessTheWordGuessResult SubmitGuess(string guess) => game.SubmitGuess(guess);

    public void ResetGame() => game.Reset();
}
