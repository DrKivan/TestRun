using EvaluaT.Application.Auth;
using EvaluaT.Domain.Auth;
using EvaluaT.Domain.Questions;
using EvaluaT.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace EvaluaT.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureAuthTableAsync(dbContext, cancellationToken);

        Student? defaultStudent = null;

        if (!await dbContext.Students.AnyAsync(cancellationToken))
        {
            defaultStudent = Student.Create("Estudiante Local", "estudiante@evaluat.local", DateTime.UtcNow);
            await dbContext.Students.AddAsync(defaultStudent, cancellationToken);
        }
        else
        {
            defaultStudent = await dbContext.Students
                .SingleOrDefaultAsync(student => student.Email == "estudiante@evaluat.local", cancellationToken);

            if (defaultStudent is null)
            {
                defaultStudent = Student.Create("Estudiante Local", "estudiante@evaluat.local", DateTime.UtcNow);
                await dbContext.Students.AddAsync(defaultStudent, cancellationToken);
            }
        }

        if (!await dbContext.Questions.AnyAsync(cancellationToken))
        {
            await dbContext.Questions.AddRangeAsync(CreateSeedQuestions(), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        defaultStudent ??= await dbContext.Students
            .SingleAsync(student => student.Email == "estudiante@evaluat.local", cancellationToken);

        if (!await dbContext.UserAccounts.AnyAsync(user => user.Email == "docente@evaluat.local", cancellationToken))
        {
            await dbContext.UserAccounts.AddAsync(
                UserAccount.CreateTeacher(
                    "Docente EvaluaT",
                    "docente@evaluat.local",
                    PasswordHasher.Hash("Docente123!"),
                    DateTime.UtcNow),
                cancellationToken);
        }

        if (!await dbContext.UserAccounts.AnyAsync(user => user.Email == defaultStudent.Email, cancellationToken))
        {
            await dbContext.UserAccounts.AddAsync(
                UserAccount.CreateStudent(
                    defaultStudent.FullName,
                    defaultStudent.Email,
                    PasswordHasher.Hash("Estudiante123!"),
                    defaultStudent.Id,
                    DateTime.UtcNow),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAuthTableAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "UserAccounts" (
                "Id" uuid NOT NULL,
                "FullName" character varying(160) NOT NULL,
                "Email" character varying(180) NOT NULL,
                "PasswordHash" character varying(500) NOT NULL,
                "Role" character varying(30) NOT NULL,
                "StudentId" uuid NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_UserAccounts" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAccounts_Email"
            ON "UserAccounts" ("Email");
            """,
            cancellationToken);
    }

    private static IReadOnlyList<Question> CreateSeedQuestions()
    {
        return
        [
            Question.Create(
                "Matematica",
                "Cuanto es 8 + 5?",
                DifficultyLevel.Easy,
                [("11", false), ("13", true), ("15", false), ("18", false)]),
            Question.Create(
                "Matematica",
                "Que numero completa la secuencia 3, 6, 12, 24, ...?",
                DifficultyLevel.Medium,
                [("30", false), ("36", false), ("48", true), ("60", false)]),
            Question.Create(
                "Matematica",
                "Si f(x)=2x^2 - 3, cual es f(4)?",
                DifficultyLevel.Hard,
                [("16", false), ("29", true), ("32", false), ("35", false)]),
            Question.Create(
                "Programacion",
                "Que estructura evita repetir codigo cuando varios componentes comparten comportamiento?",
                DifficultyLevel.Easy,
                [("Duplicacion", false), ("Abstraccion reutilizable", true), ("Variable global", false), ("Comentario", false)]),
            Question.Create(
                "Programacion",
                "Que principio recomienda que una clase tenga una sola razon para cambiar?",
                DifficultyLevel.Medium,
                [("SRP", true), ("DRY", false), ("YAGNI", false), ("KISS", false)]),
            Question.Create(
                "Programacion",
                "Que patron permite variar un algoritmo sin modificar el cliente que lo usa?",
                DifficultyLevel.Hard,
                [("Adapter", false), ("Strategy", true), ("Facade", false), ("Prototype", false)]),
            Question.Create(
                "Ciencias",
                "Cual es el estado del agua a 100 grados Celsius a nivel del mar?",
                DifficultyLevel.Easy,
                [("Solido", false), ("Liquido", false), ("Gaseoso", true), ("Plasma", false)]),
            Question.Create(
                "Ciencias",
                "Que particula tiene carga negativa?",
                DifficultyLevel.Medium,
                [("Proton", false), ("Electron", true), ("Neutron", false), ("Nucleo", false)]),
            Question.Create(
                "Ciencias",
                "Que ley relaciona fuerza, masa y aceleracion?",
                DifficultyLevel.Hard,
                [("Primera ley de Newton", false), ("Segunda ley de Newton", true), ("Ley de Ohm", false), ("Ley de Hooke", false)])
        ];
    }
}
