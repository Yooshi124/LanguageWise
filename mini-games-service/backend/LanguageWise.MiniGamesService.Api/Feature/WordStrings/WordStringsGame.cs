namespace LanguageWise.MiniGamesService.Api.Feature.WordStrings;

public sealed class WordStringsGame
{
    private readonly string language;
    private readonly List<string> words = [];
    private int score = 0;
    private bool isComplete = false;

    public WordStringsGame(string language)
    {
        this.language = language;
    }

    public WordStringsState GetState() =>
        new(language, words, score, isComplete);

    public string[] GetWordChain()
    {
        throw new NotImplementedException();
    }

    public WordStringsMoveResult SubmitWord(string word)
    {
        throw new NotImplementedException();
    }

    public bool IsValidWord(string word)
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }
}
