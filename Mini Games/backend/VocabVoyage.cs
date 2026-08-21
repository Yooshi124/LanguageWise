
public class VocabVoyage
{
    public VocabVoyage(string courseLanguage)
    {
        Language = courseLanguage;
        Answer = generateAnswer(Language);
    }

    private readonly string Language;
    private readonly string Answer;

    string generateAnswer(string language)
    {
        //Set up AI word generation in relevant language
        return "Vocab"; //Placeholder
    }

    public char[] getGuessColours(string currentGuess)
    {
        var colourList = new char[5];
        for (int i = 0; i < 5; i++)
        {
            if (currentGuess[i].Equals(Answer[i]))
            {
                colourList[i] = 'G';
            }
            else if (Answer.Contains(currentGuess[i]))
            {
                colourList[i] = 'O';
            }
            else
            {
                colourList[i] = 'R';
            }
        }
        return colourList;
    }

    bool isGuessCorrect(char[] currentGuessColours)
    {
        var isCorrect = true;
        for (int i = 0; i < 5; i++)
        {
            if (currentGuessColours[i] != 'G')
            {
                isCorrect = false;
                break;
            }
        } 
        return isCorrect;
    }


}