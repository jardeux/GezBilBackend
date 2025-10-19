
namespace BilbakalimAPI;

public record Question(
    int Id,
    string CityId,
    string QuestionText,
    string? MediaUrl,
    List<string> Options,
    int CorrectAnswerIndex
);
