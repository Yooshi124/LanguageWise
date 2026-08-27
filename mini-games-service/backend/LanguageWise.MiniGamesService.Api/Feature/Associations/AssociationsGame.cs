namespace LanguageWise.MiniGamesService.Api.Feature.Associations;

public sealed class AssociationsGame
{
    private readonly string language;
    private readonly List<string> words = [];
    private readonly List<string> selectedWords = [];
    private bool isComplete = false;

    public AssociationsGame(string language)
    {
        this.language = language;
    }

    public AssociationsState GetState() =>
        new(language, words, selectedWords, isComplete);

    public string[] GetWords()
    {
        throw new NotImplementedException();
    }

    public AssociationResult SelectPair(string firstWord, string secondWord)
    {
        throw new NotImplementedException();
    }

    public bool IsAssociation(string firstWord, string secondWord)
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }
}
