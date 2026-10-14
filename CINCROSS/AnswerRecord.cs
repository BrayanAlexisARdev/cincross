namespace CINCROSS;

public class AnswerRecord
{
    public Question? Question { get; set; }
    public bool IsCorrect { get; set; }
    public int SelectedOption { get; set; } = -1;
}
