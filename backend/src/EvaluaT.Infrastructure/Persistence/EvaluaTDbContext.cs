using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Auth;
using EvaluaT.Domain.Exams;
using EvaluaT.Domain.Lessons;
using EvaluaT.Domain.Questions;
using EvaluaT.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvaluaT.Infrastructure.Persistence;

public sealed class EvaluaTDbContext : DbContext, IUnitOfWork
{
    public EvaluaTDbContext(DbContextOptions<EvaluaTDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<ExamResponse> ExamResponses => Set<ExamResponse>();
    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(student => student.Id);
            entity.Property(student => student.Id).ValueGeneratedNever();
            entity.Property(student => student.FullName).HasMaxLength(160).IsRequired();
            entity.Property(student => student.Email).HasMaxLength(180).IsRequired();
            entity.HasIndex(student => student.Email).IsUnique();
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).ValueGeneratedNever();
            entity.Property(user => user.FullName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(180).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(question => question.Id);
            entity.Property(question => question.Id).ValueGeneratedNever();
            entity.Property(question => question.Topic).HasMaxLength(120).IsRequired();
            entity.Property(question => question.Competency).HasMaxLength(160).IsRequired();
            entity.Property(question => question.Text).HasMaxLength(600).IsRequired();
            entity.Property(question => question.Difficulty)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(question => question.IsActive).IsRequired();

            entity.Metadata.FindNavigation(nameof(Question.Options))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(question => question.Options)
                .WithOne()
                .HasForeignKey(option => option.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnswerOption>(entity =>
        {
            entity.HasKey(option => option.Id);
            entity.Property(option => option.Id).ValueGeneratedNever();
            entity.Property(option => option.Text).HasMaxLength(300).IsRequired();
            entity.Property(option => option.Order).IsRequired();
            entity.Property(option => option.IsCorrect).IsRequired();
            entity.HasIndex(option => new { option.QuestionId, option.Order }).IsUnique();
        });

        modelBuilder.Entity<ExamSession>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Id).ValueGeneratedNever();
            entity.Property(session => session.CurrentDifficulty)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(session => session.Policy)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(session => session.Kind)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(ExamSessionKind.Standard)
                .IsRequired();
            entity.Property(session => session.TargetTopic).HasMaxLength(120);
            entity.Property(session => session.TargetCompetency).HasMaxLength(160);
            entity.Property(session => session.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(session => session.MaxQuestions).IsRequired();

            entity.Metadata.FindNavigation(nameof(ExamSession.Responses))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(session => session.Responses)
                .WithOne()
                .HasForeignKey(response => response.ExamSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamResponse>(entity =>
        {
            entity.HasKey(response => response.Id);
            entity.Property(response => response.Id).ValueGeneratedNever();
            entity.Property(response => response.DifficultyAtAnswer)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(response => response.SelectedOptionOrder).IsRequired();
            entity.Property(response => response.IsCorrect).IsRequired();
            entity.HasIndex(response => new { response.ExamSessionId, response.QuestionId }).IsUnique();
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(lesson => lesson.Id);
            entity.Property(lesson => lesson.Id).ValueGeneratedNever();
            entity.Property(lesson => lesson.Topic).HasMaxLength(120).IsRequired();
            entity.Property(lesson => lesson.Competency).HasMaxLength(160);
            entity.Property(lesson => lesson.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(lesson => lesson.Title).HasMaxLength(180).IsRequired();
            entity.Property(lesson => lesson.Content).HasMaxLength(1200).IsRequired();
            entity.Property(lesson => lesson.ResourceUrl).HasMaxLength(500);
            entity.Property(lesson => lesson.IsActive).IsRequired();
        });
    }
}
