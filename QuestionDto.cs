
namespace BilbakalimAPI;

public record QuestionDto(
    string CityId,
    string QuestionText,
    string? MediaUrl,
    List<string> Options,
    int CorrectAnswerIndex
);
