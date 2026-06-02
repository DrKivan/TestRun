using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EvaluaT.Application.Auth;
using EvaluaT.Application.Exams;
using EvaluaT.Application.Questions;
using EvaluaT.Domain.Exams;

namespace EvaluaT.Tests.Integration;

public sealed class ExamFlowTests : IClassFixture<EvaluaTApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ExamFlowTests(EvaluaTApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdaptiveExamFlow_StartsSession_AnswersQuestion_AndReturnsNextStep()
    {
        var teacher = await LoginAsync("docente@evaluat.local", "Docente123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher.Token);

        var questions = await _client.GetFromJsonAsync<List<QuestionResponse>>("/api/questions", JsonOptions);
        Assert.NotNull(questions);
        Assert.NotEmpty(questions);

        var student = await _client.PostAsJsonAsync(
            "/api/auth/register-student",
            new RegisterStudentRequest("Ada Lovelace", "ada@evaluat.test", "Ada12345!"),
            JsonOptions);
        student.EnsureSuccessStatusCode();
        var studentAuth = await student.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(studentAuth);
        Assert.NotNull(studentAuth.User.StudentId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentAuth.Token);

        var startResponse = await _client.PostAsJsonAsync(
            "/api/exam-sessions",
            new StartExamRequest(studentAuth.User.StudentId.Value, MaxQuestions: 3, DifficultyPolicy.Balanced),
            JsonOptions);
        startResponse.EnsureSuccessStatusCode();
        var session = await startResponse.Content.ReadFromJsonAsync<ExamSessionResponse>(JsonOptions);

        Assert.NotNull(session);
        Assert.NotNull(session.CurrentQuestion);
        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.Equal(ExamSessionKind.Standard, session.Kind);
        Assert.Equal(10, session.MaxQuestions);

        var correctOptionOrder = questions
            .Single(question => question.Id == session.CurrentQuestion.Id)
            .Options
            .Single(option => option.IsCorrect)
            .Order;

        var answerResponse = await _client.PostAsJsonAsync(
            $"/api/exam-sessions/{session.Id}/answers",
            new AnswerQuestionRequest(session.CurrentQuestion.Id, correctOptionOrder),
            JsonOptions);
        if (!answerResponse.IsSuccessStatusCode)
        {
            var problem = await answerResponse.Content.ReadAsStringAsync();
            Assert.Fail(problem);
        }

        var result = await answerResponse.Content.ReadFromJsonAsync<AnswerResultResponse>(JsonOptions);

        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
        Assert.Equal(1, result.Session.AnsweredQuestions);
        Assert.NotEqual(default, result.NextDifficulty);
        Assert.Contains(result.Session.Diagnostic, item => item.Topic == session.CurrentQuestion.Topic);
        Assert.Equal(3, result.Session.Diagnostic.Count);
        Assert.Contains(result.Session.Diagnostic, item => item.Topic == "Matematica");
        Assert.Contains(result.Session.Diagnostic, item => item.Topic == "Programacion");
        Assert.Contains(result.Session.Diagnostic, item => item.Topic == "Ciencias");
    }

    [Fact]
    public async Task FocusedReinforcementExam_StartsWithinTargetTopic()
    {
        var student = await _client.PostAsJsonAsync(
            "/api/auth/register-student",
            new RegisterStudentRequest("Grace Hopper", "grace@evaluat.test", "Grace12345!"),
            JsonOptions);
        student.EnsureSuccessStatusCode();
        var studentAuth = await student.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(studentAuth);
        Assert.NotNull(studentAuth.User.StudentId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentAuth.Token);

        var startResponse = await _client.PostAsJsonAsync(
            "/api/exam-sessions",
            new StartExamRequest(
                studentAuth.User.StudentId.Value,
                MaxQuestions: 3,
                DifficultyPolicy.Conservative,
                Kind: ExamSessionKind.Reinforcement,
                TargetTopic: "Programacion",
                TargetCompetency: "Patrones de diseno"),
            JsonOptions);
        startResponse.EnsureSuccessStatusCode();
        var session = await startResponse.Content.ReadFromJsonAsync<ExamSessionResponse>(JsonOptions);

        Assert.NotNull(session);
        Assert.NotNull(session.CurrentQuestion);
        Assert.Equal(ExamSessionKind.Reinforcement, session.Kind);
        Assert.Equal(3, session.MaxQuestions);
        Assert.Equal("Programacion", session.TargetTopic);
        Assert.Equal("Patrones de diseno", session.TargetCompetency);
        Assert.Equal("Programacion", session.CurrentQuestion.Topic);
    }

    private async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password),
            JsonOptions);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Authentication response was empty.");
    }
}
