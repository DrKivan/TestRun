using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Questions;

public sealed record OptionRequest(string Text, bool IsCorrect);

public sealed record OptionResponse(Guid Id, int Order, string Text, bool IsCorrect);

public sealed record CreateQuestionRequest(
    string Topic,
    string Text,
    DifficultyLevel Difficulty,
    IReadOnlyList<OptionRequest> Options);

public sealed record UpdateQuestionRequest(
    string Topic,
    string Text,
    DifficultyLevel Difficulty,
    IReadOnlyList<OptionRequest> Options);

public sealed record QuestionResponse(
    Guid Id,
    string Topic,
    string Text,
    DifficultyLevel Difficulty,
    bool IsActive,
    IReadOnlyList<OptionResponse> Options);

public sealed record PublicOptionResponse(int Order, string Text);

public sealed record CurrentQuestionResponse(
    Guid Id,
    string Topic,
    string Text,
    DifficultyLevel Difficulty,
    IReadOnlyList<PublicOptionResponse> Options);
