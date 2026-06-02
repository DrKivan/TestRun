using EvaluaT.Application.Auth;
using EvaluaT.Domain.Auth;
using EvaluaT.Domain.Lessons;
using EvaluaT.Domain.Questions;
using EvaluaT.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace EvaluaT.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureAuthTableAsync(dbContext, cancellationToken);
        await EnsureQuestionCompetencyColumnAsync(dbContext, cancellationToken);
        await EnsureExamSessionFocusColumnsAsync(dbContext, cancellationToken);
        await EnsureExamSessionKindColumnAsync(dbContext, cancellationToken);
        await EnsureLessonsTableAsync(dbContext, cancellationToken);

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

        await DeactivateOutdatedSeedQuestionsAsync(dbContext, cancellationToken);
        await AddMissingSeedQuestionsAsync(dbContext, cancellationToken);

        if (!await dbContext.Lessons.AnyAsync(cancellationToken))
        {
            await dbContext.Lessons.AddRangeAsync(CreateSeedLessons(), cancellationToken);
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

    private static async Task EnsureQuestionCompetencyColumnAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Questions"
            ADD COLUMN IF NOT EXISTS "Competency" character varying(160) NOT NULL DEFAULT 'General';

            UPDATE "Questions"
            SET "Competency" = "Topic"
            WHERE "Competency" = 'General' OR "Competency" = '';
            """,
            cancellationToken);
    }

    private static async Task EnsureLessonsTableAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "Lessons" (
                "Id" uuid NOT NULL,
                "Topic" character varying(120) NOT NULL,
                "Competency" character varying(160) NULL,
                "Type" character varying(20) NOT NULL,
                "Title" character varying(180) NOT NULL,
                "Content" character varying(1200) NOT NULL,
                "ResourceUrl" character varying(500) NULL,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_Lessons" PRIMARY KEY ("Id")
            );
            """,
            cancellationToken);
    }

    private static async Task EnsureExamSessionFocusColumnsAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "ExamSessions"
            ADD COLUMN IF NOT EXISTS "TargetTopic" character varying(120) NULL;

            ALTER TABLE "ExamSessions"
            ADD COLUMN IF NOT EXISTS "TargetCompetency" character varying(160) NULL;
            """,
            cancellationToken);
    }

    private static async Task EnsureExamSessionKindColumnAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "ExamSessions"
            ADD COLUMN IF NOT EXISTS "Kind" character varying(30) NOT NULL DEFAULT 'Standard';
            """,
            cancellationToken);
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

    private static async Task AddMissingSeedQuestionsAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingTexts = await dbContext.Questions
            .Select(question => question.Text)
            .ToListAsync(cancellationToken);
        var existingTextSet = existingTexts.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingQuestions = CreateSeedQuestions()
            .Where(question => !existingTextSet.Contains(question.Text))
            .ToList();

        if (missingQuestions.Count > 0)
        {
            await dbContext.Questions.AddRangeAsync(missingQuestions, cancellationToken);
        }
    }

    private static async Task DeactivateOutdatedSeedQuestionsAsync(EvaluaTDbContext dbContext, CancellationToken cancellationToken)
    {
        var outdatedTexts = OutdatedSeedQuestionTexts().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var outdatedQuestions = await dbContext.Questions
            .Where(question => question.IsActive && outdatedTexts.Contains(question.Text))
            .ToListAsync(cancellationToken);

        foreach (var question in outdatedQuestions)
        {
            question.Deactivate();
        }
    }

    private static IReadOnlyList<string> OutdatedSeedQuestionTexts()
    {
        return
        [
            "Cuanto es 8 + 5?",
            "Que numero completa la secuencia 3, 6, 12, 24, ...?",
            "Si f(x)=2x^2 - 3, cual es f(4)?",
            "Cuanto es 10 - 4?",
            "Cuanto es 3 x 4?",
            "Cuanto es 20 dividido entre 5?",
            "Que numero sigue en la secuencia 2, 4, 6, 8, ...?",
            "Cual numero es mayor?",
            "Cuanto es la mitad de 10?",
            "Cuanto es 30 + 20?",
            "Ana tiene 5 manzanas y compra 3 mas. Cuantas tiene?",
            "Cuanto es 7 x 3?",
            "Que numero falta: 5, 10, 15, __, 25?",
            "Si una caja tiene 6 lapices y hay 2 cajas, cuantos lapices hay?",
            "Que es una variable en programacion?",
            "Que estructura evita repetir codigo cuando varios componentes comparten comportamiento?",
            "Que principio recomienda que una clase tenga una sola razon para cambiar?",
            "Que patron permite variar un algoritmo sin modificar el cliente que lo usa?",
            "Que simbolo suele indicar una suma en programacion?",
            "Para que sirve un if?",
            "Para que sirve un comentario en el codigo?",
            "Para que sirve un bucle?",
            "Que es una funcion?",
            "Que tipo de dato representa verdadero o falso?",
            "Que guarda un arreglo o lista?",
            "Que ayuda a evitar repetir codigo?",
            "Que significa depurar un programa?",
            "Que patron permite elegir entre varias formas de hacer una tarea?",
            "Que es mejor para entender codigo?",
            "Que necesitan las plantas para crecer?",
            "Cual es el estado del agua a 100 grados Celsius a nivel del mar?",
            "Que ley relaciona fuerza, masa y aceleracion?",
            "Que organo usamos principalmente para respirar?",
            "Cual es un estado de la materia?",
            "Cual es el planeta donde vivimos?",
            "Que pasa con el hielo cuando se calienta?",
            "Que particula tiene carga negativa?",
            "Que fuente nos da luz y calor durante el dia?",
            "Que animal es un mamifero?",
            "Que mezcla se puede separar con un filtro?",
            "Que pasa si empujas una pelota quieta?",
            "Como se llama cuando el agua se convierte en vapor?",
            "Que grupo de alimentos ayuda a obtener energia rapida?"
        ];
    }

    private static IReadOnlyList<Question> CreateSeedQuestions()
    {
        return
        [
            Question.Create(
                "Matematica",
                "Operaciones aritmeticas",
                "Si tienes 8 soles y recibes 5 mas, cuanto tienes en total?",
                DifficultyLevel.Easy,
                [("11", false), ("13", true), ("15", false), ("18", false)]),
            Question.Create(
                "Matematica",
                "Operaciones aritmeticas",
                "Si compras algo de 4 soles y pagas con 10, cuanto cambio recibes?",
                DifficultyLevel.Easy,
                [("4", false), ("5", false), ("6", true), ("7", false)]),
            Question.Create(
                "Matematica",
                "Multiplicacion basica",
                "Hay 3 bolsas con 4 caramelos cada una. Cuantos caramelos hay?",
                DifficultyLevel.Easy,
                [("7", false), ("12", true), ("14", false), ("16", false)]),
            Question.Create(
                "Matematica",
                "Division basica",
                "Repartes 20 hojas entre 5 estudiantes. Cuantas recibe cada uno?",
                DifficultyLevel.Easy,
                [("2", false), ("4", true), ("5", false), ("10", false)]),
            Question.Create(
                "Matematica",
                "Patrones numericos",
                "Que numero completa la secuencia 4, 8, 12, 16, ...?",
                DifficultyLevel.Medium,
                [("18", false), ("20", true), ("22", false), ("24", false)]),
            Question.Create(
                "Matematica",
                "Comparacion de numeros",
                "Cual resultado es mayor?",
                DifficultyLevel.Medium,
                [("9 + 8", false), ("6 x 3", true), ("20 - 4", false), ("7 + 9", false)]),
            Question.Create(
                "Matematica",
                "Fracciones simples",
                "La mitad de un grupo de 18 estudiantes sale al recreo. Cuantos salen?",
                DifficultyLevel.Medium,
                [("8", false), ("9", true), ("10", false), ("12", false)]),
            Question.Create(
                "Matematica",
                "Suma con decenas",
                "Un libro cuesta 30 soles y un cuaderno 20. Cuanto cuestan juntos?",
                DifficultyLevel.Medium,
                [("40", false), ("50", true), ("60", false), ("70", false)]),
            Question.Create(
                "Matematica",
                "Resolucion de problemas",
                "Ana tiene 12 manzanas, regala 4 y compra 3. Cuantas tiene al final?",
                DifficultyLevel.Hard,
                [("9", false), ("11", true), ("12", false), ("15", false)]),
            Question.Create(
                "Matematica",
                "Multiplicacion basica",
                "Un aula tiene 7 filas con 3 sillas cada una. Cuantas sillas hay?",
                DifficultyLevel.Hard,
                [("18", false), ("21", true), ("24", false), ("27", false)]),
            Question.Create(
                "Matematica",
                "Orden numerico",
                "Que numero falta si la regla es sumar 5: 5, 10, 15, __, 25?",
                DifficultyLevel.Hard,
                [("18", false), ("20", true), ("22", false), ("30", false)]),
            Question.Create(
                "Matematica",
                "Problemas cotidianos",
                "Si cada caja tiene 6 lapices y compras 2 cajas, pero prestas 3 lapices, cuantos quedan?",
                DifficultyLevel.Hard,
                [("7", false), ("9", true), ("12", false), ("15", false)]),
            Question.Create(
                "Programacion",
                "Conceptos basicos",
                "Si necesitas guardar la edad de un usuario, que usarias?",
                DifficultyLevel.Easy,
                [("Un dato guardado", true), ("Un error", false), ("Un color", false), ("Una pantalla", false)]),
            Question.Create(
                "Programacion",
                "Conceptos basicos",
                "En una expresion como total + precio, que representa el signo +?",
                DifficultyLevel.Easy,
                [("+", true), ("-", false), ("/", false), ("=", false)]),
            Question.Create(
                "Programacion",
                "Condicionales",
                "Si un programa debe decidir si una nota aprueba o desaprueba, que estructura usarias?",
                DifficultyLevel.Easy,
                [("Tomar una decision", true), ("Borrar archivos", false), ("Pintar una imagen", false), ("Cerrar el equipo", false)]),
            Question.Create(
                "Programacion",
                "Comentarios",
                "Cuando una parte del codigo no es obvia, que ayuda a explicarla sin ejecutarse?",
                DifficultyLevel.Easy,
                [("Explicar el codigo", true), ("Ejecutar mas rapido", false), ("Crear una cuenta", false), ("Cambiar el monitor", false)]),
            Question.Create(
                "Programacion",
                "Bucles",
                "Si quieres mostrar 10 nombres sin escribir 10 instrucciones iguales, que usarias?",
                DifficultyLevel.Medium,
                [("Repetir instrucciones", true), ("Apagar el programa", false), ("Ocultar datos", false), ("Cambiar el teclado", false)]),
            Question.Create(
                "Programacion",
                "Funciones",
                "Si una operacion se usa en varias partes del programa, que conviene crear?",
                DifficultyLevel.Medium,
                [("Un bloque reutilizable de codigo", true), ("Una imagen", false), ("Un cable", false), ("Un numero fijo", false)]),
            Question.Create(
                "Programacion",
                "Tipos de datos",
                "Que tipo de dato conviene para saber si un usuario esta activo o no?",
                DifficultyLevel.Medium,
                [("Booleano", true), ("Texto", false), ("Imagen", false), ("Archivo", false)]),
            Question.Create(
                "Programacion",
                "Arreglos",
                "Si necesitas guardar varios puntajes de un estudiante, que estructura conviene usar?",
                DifficultyLevel.Medium,
                [("Varios valores", true), ("Solo colores", false), ("Solo errores", false), ("Una contrasena", false)]),
            Question.Create(
                "Programacion",
                "Reutilizacion de codigo",
                "Si copias la misma logica en tres lugares, que mejora reduce esa repeticion?",
                DifficultyLevel.Hard,
                [("Usar funciones", true), ("Copiar todo", false), ("Borrar nombres", false), ("Cerrar el editor", false)]),
            Question.Create(
                "Programacion",
                "Depuracion",
                "Un programa calcula mal un total. Que accion corresponde a depurar?",
                DifficultyLevel.Hard,
                [("Buscar y corregir errores", true), ("Cambiar el fondo", false), ("Subir el volumen", false), ("Imprimir una foto", false)]),
            Question.Create(
                "Programacion",
                "Patrones de diseno",
                "Si una app puede calcular descuentos con reglas distintas, que patron permite intercambiarlas?",
                DifficultyLevel.Hard,
                [("Adapter", false), ("Strategy", true), ("Facade", false), ("Prototype", false)]),
            Question.Create(
                "Programacion",
                "Buenas practicas",
                "Que practica ayuda mas a entender rapidamente que hace una variable?",
                DifficultyLevel.Hard,
                [("Nombres claros", true), ("Nombres al azar", false), ("Todo en una linea", false), ("Sin espacios", false)]),
            Question.Create(
                "Ciencias",
                "Seres vivos",
                "Una planta esta en una caja cerrada sin luz. Que le falta principalmente para crecer bien?",
                DifficultyLevel.Easy,
                [("Agua y luz", true), ("Piedras", false), ("Plastico", false), ("Vidrio", false)]),
            Question.Create(
                "Ciencias",
                "Cuerpo humano",
                "Cuando inhalamos aire, que organos trabajan principalmente?",
                DifficultyLevel.Easy,
                [("Pulmones", true), ("Estomago", false), ("Higado", false), ("Piel", false)]),
            Question.Create(
                "Ciencias",
                "Estados de la materia",
                "El agua en un vaso conserva volumen pero cambia de forma segun el recipiente. Que estado es?",
                DifficultyLevel.Easy,
                [("Liquido", true), ("Rapido", false), ("Alto", false), ("Dulce", false)]),
            Question.Create(
                "Ciencias",
                "Sistema solar",
                "En que planeta se encuentran los seres humanos de forma natural?",
                DifficultyLevel.Easy,
                [("Tierra", true), ("Marte", false), ("Jupiter", false), ("Venus", false)]),
            Question.Create(
                "Ciencias",
                "Agua",
                "Si un cubo de hielo recibe calor durante varios minutos, que cambio ocurre primero?",
                DifficultyLevel.Medium,
                [("Se derrite", true), ("Se vuelve piedra", false), ("Desaparece siempre", false), ("Se vuelve metal", false)]),
            Question.Create(
                "Ciencias",
                "Estructura atomica",
                "En un atomo, que particula tiene carga negativa?",
                DifficultyLevel.Medium,
                [("Proton", false), ("Electron", true), ("Neutron", false), ("Nucleo", false)]),
            Question.Create(
                "Ciencias",
                "Energia",
                "Que fuente natural explica la mayor parte de la luz y calor que recibimos durante el dia?",
                DifficultyLevel.Medium,
                [("El Sol", true), ("La Luna", false), ("Una roca", false), ("El hielo", false)]),
            Question.Create(
                "Ciencias",
                "Animales",
                "Cual de estos animales alimenta a sus crias con leche?",
                DifficultyLevel.Medium,
                [("Perro", true), ("Pez", false), ("Hormiga", false), ("Serpiente", false)]),
            Question.Create(
                "Ciencias",
                "Mezclas",
                "Tienes agua mezclada con arena. Que metodo simple permite separar la arena?",
                DifficultyLevel.Hard,
                [("Filtracion", true), ("Evaporacion", false), ("Congelacion", false), ("Respiracion", false)]),
            Question.Create(
                "Ciencias",
                "Leyes de Newton",
                "Una pelota esta quieta y la empujas. Que cambio esperas observar?",
                DifficultyLevel.Hard,
                [("Puede moverse", true), ("Se vuelve agua", false), ("Desaparece", false), ("Se vuelve luz", false)]),
            Question.Create(
                "Ciencias",
                "Ciclo del agua",
                "Cuando el sol calienta un charco y el agua pasa a vapor, como se llama el proceso?",
                DifficultyLevel.Hard,
                [("Evaporacion", true), ("Congelacion", false), ("Filtracion", false), ("Respiracion", false)]),
            Question.Create(
                "Ciencias",
                "Alimentacion",
                "Antes de hacer ejercicio, que grupo de alimentos suele aportar energia rapida?",
                DifficultyLevel.Hard,
                [("Carbohidratos", true), ("Rocas", false), ("Metales", false), ("Vidrios", false)])
        ];
    }

    private static IReadOnlyList<Lesson> CreateSeedLessons()
    {
        return
        [
            Lesson.Create(
                "Programacion",
                null,
                LessonType.PreExam,
                "Antes de programacion",
                "Repasa conceptos de abstraccion, responsabilidad unica y patrones. Lee cada pregunta buscando la intencion del diseno, no solo palabras clave.",
                null),
            Lesson.Create(
                "Programacion",
                "Patrones de diseno",
                LessonType.PostExam,
                "Refuerzo de patrones",
                "Si tu diagnostico marco patrones de diseno, vuelve a revisar Strategy, Factory y Adapter. Compara que problema resuelve cada patron antes de memorizar nombres.",
                null),
            Lesson.Create(
                "Matematica",
                null,
                LessonType.PreExam,
                "Antes de matematica",
                "Resuelve con calma, identifica datos relevantes y verifica operaciones basicas antes de avanzar a dificultad media o alta.",
                null),
            Lesson.Create(
                "Ciencias",
                null,
                LessonType.PostExam,
                "Refuerzo de ciencias",
                "Revisa las competencias con menor porcentaje y relaciona cada concepto con un ejemplo cotidiano antes de iniciar otra prueba.",
                null)
        ];
    }
}
