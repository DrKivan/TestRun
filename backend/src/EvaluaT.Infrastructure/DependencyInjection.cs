using EvaluaT.Application.Abstractions;
using EvaluaT.Infrastructure.Persistence;
using EvaluaT.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EvaluaT.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EvaluaT")
            ?? "Host=localhost;Port=5432;Database=evaluat;Username=evaluat;Password=evaluat";

        services.AddDbContext<EvaluaTDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<EvaluaTDbContext>());
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IExamSessionRepository, ExamSessionRepository>();

        return services;
    }
}
