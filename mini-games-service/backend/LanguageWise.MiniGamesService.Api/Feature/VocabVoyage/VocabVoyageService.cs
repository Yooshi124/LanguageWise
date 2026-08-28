namespace LanguageWise.MiniGamesService.Api.Feature.VocabVoyage;

public sealed class VocabVoyageService
{
    private readonly VocabVoyageGame game;

    public VocabVoyageService(string language, ILearningContextProvider contextProvider)
    {
        var context = contextProvider.GetContext();
        game = new VocabVoyageGame(language, VocabularySelector.GetCandidates(context));
    }

    public VocabVoyageState GetState() => game.GetState();

    public VocabVoyageGuessResult SubmitGuess(string guess) => game.SubmitGuess(guess);

    public void ResetGame() => game.Reset();
}
