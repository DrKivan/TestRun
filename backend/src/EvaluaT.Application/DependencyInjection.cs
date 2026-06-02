using EvaluaT.Application.Abstractions;
using EvaluaT.Application.Auth;
using EvaluaT.Application.Common;
using EvaluaT.Application.Exams;
using EvaluaT.Application.Exams.Diagnostics;
using EvaluaT.Application.Lessons;
using EvaluaT.Application.Questions;
using EvaluaT.Application.Students;
using EvaluaT.Domain.Exams;
using Microsoft.Extensions.DependencyInjection;

namespace EvaluaT.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IDifficultyAdjustmentStrategy, BalancedDifficultyAdjustmentStrategy>();
        services.AddSingleton<IDifficultyAdjustmentStrategy, ConservativeDifficultyAdjustmentStrategy>();
        services.AddSingleton<DifficultyAdjustmentStrategyFactory>();
        services.AddSingleton<ITopicDiagnosticStrategy, CompositeTopicDiagnosticStrategy>();

        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<IDomainEventObserver, ExamEventLogObserver>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<IExamSessionService, ExamSessionService>();

        return services;
    }
}
