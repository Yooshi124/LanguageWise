namespace LanguageWise.MiniGamesService.Api.Feature.WordSearch;

public sealed class WordSearchService
{
    private readonly WordSearchGame game;

    public WordSearchService(string language)
    {
        game = new WordSearchGame(language);
    }

    public WordSearchState GetState() => game.GetState();

    public WordSearchMoveResult SubmitWord(string word, IReadOnlyList<int> indices) => game.SubmitWord(word, indices);

    public WordSearchHintResult UseHint() => game.UseHint();

    public WordSearchState GiveUp() => game.GiveUp();

    public void ResetGame() => game.Reset();
}
