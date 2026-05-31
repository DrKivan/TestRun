# EvaluaT

EvaluaT es una linea de producto para un motor de examenes adaptativos. La aplicacion se separa de forma explicita entre frontend y backend para mantener responsabilidades claras.

## Estructura

```text
EvaluaT/
|-- frontend/
|   `-- React + TypeScript + Vite
`-- backend/
    |-- EvaluaT.sln
    |-- src/
    |   |-- EvaluaT.Api              # API REST ASP.NET Core
    |   |-- EvaluaT.Application      # Casos de uso del sistema
    |   |-- EvaluaT.Domain           # Reglas de negocio del motor adaptativo
    |   `-- EvaluaT.Infrastructure   # Persistencia con EF Core y PostgreSQL
    `-- tests/
        `-- EvaluaT.Tests            # Pruebas unitarias, integracion y aceptacion
```

## Responsabilidades

- Frontend: muestra login, vista de estudiante y vista de docente; consume la API por HTTP.
- Backend API: expone endpoints REST y valida solicitudes externas.
- Application: coordina casos de uso como iniciar examen y responder preguntas.
- Domain: contiene entidades, reglas de dificultad adaptativa y eventos de dominio.
- Infrastructure: implementa repositorios y acceso a PostgreSQL.

## Ejecucion local completa

1. Levantar PostgreSQL:

```powershell
docker compose up -d
```

2. Ejecutar backend:

```powershell
cd backend
dotnet run --project src/EvaluaT.Api/EvaluaT.Api.csproj --launch-profile http
```

La API queda en `http://localhost:5116` y Swagger en `http://localhost:5116/swagger`.

3. Ejecutar frontend:

```powershell
cd frontend
npm run dev
```

El frontend queda en `http://localhost:5173`. La interfaz necesita que la API este disponible para leer y guardar datos en PostgreSQL.

## Accesos locales

El sistema usa login exclusivo con roles.

- Docente: `docente@evaluat.local` / `Docente123!`
- Estudiante: `estudiante@evaluat.local` / `Estudiante123!`

El docente gestiona el banco de preguntas y consulta resultados. El estudiante solo puede iniciar/responder examenes; no recibe respuestas correctas ni el banco completo.

## Pruebas

```powershell
cd backend
dotnet test EvaluaT.sln --collect:"XPlat Code Coverage"
```

Ultima verificacion local:

- Backend: compilacion correcta.
- Frontend: `npm run build` correcto.
- Pruebas: 7/7 correctas.
- Cobertura de lineas: 74,59%.

## Endpoints principales

- `GET /api/questions`
- `POST /api/questions`
- `POST /api/auth/login`
- `POST /api/auth/register-student`
- `POST /api/students`
- `GET /api/exam-sessions`
- `POST /api/exam-sessions`
- `POST /api/exam-sessions/{id}/answers`
