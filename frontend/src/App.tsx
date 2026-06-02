import {
  BrainCircuit,
  CheckCircle2,
  ClipboardList,
  Database,
  Gauge,
  GraduationCap,
  LogOut,
  Play,
  Plus,
  Settings2,
  ShieldCheck,
  Trash2,
  UserRoundCheck,
  WifiOff,
  XCircle,
} from 'lucide-react'
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  answerQuestion,
  createLesson,
  createQuestion,
  deleteLesson,
  deleteQuestion,
  getExamAnalytics,
  getExamSession,
  listExamResults,
  listLessons,
  listQuestions,
  listStudentExamResults,
  login,
  registerStudent,
  setAuthToken,
  startExam,
} from './api'
import type {
  AnswerResultResponse,
  AuthResponse,
  CompetencyDiagnosticResponse,
  DifficultyLevel,
  DifficultyPolicy,
  ExamSessionKind,
  ExamAnalyticsResponse,
  ErrorReviewItemResponse,
  ExamResultSummaryResponse,
  ExamSessionResponse,
  LessonResponse,
  LessonType,
  QuestionResponse,
  Topic,
  UserRole,
} from './types'
import './App.css'

type RuntimeMode = 'login' | 'connecting' | 'api' | 'offline'
type TeacherPage = 'questions' | 'lessons' | 'results' | 'analytics'
type StudentPage = 'exam' | 'route' | 'history'
type ReinforcementFocus = { topic: Topic; competency?: string; maxQuestions?: number }

const authStorageKey = 'evaluat.auth'
const sessionStoragePrefix = 'evaluat.activeSessionId'

const difficultyText: Record<DifficultyLevel, string> = {
  Easy: 'Basica',
  Medium: 'Media',
  Hard: 'Alta',
}

const policyText: Record<DifficultyPolicy, string> = {
  Balanced: 'Balanceada',
  Conservative: 'Conservadora',
}

const studentPolicyText: Record<DifficultyPolicy, string> = {
  Balanced: 'Reto progresivo',
  Conservative: 'Inicio guiado',
}

const studentPolicyDescription: Record<DifficultyPolicy, string> = {
  Balanced: 'Empieza en nivel medio y ajusta rapido la dificultad segun tus respuestas.',
  Conservative: 'Empieza con preguntas basicas y sube gradualmente cuando demuestras dominio.',
}

const standardExamQuestionCount = 10
const focusedExamQuestionCount = 3
const allowedTopics: Topic[] = ['Matematica', 'Programacion', 'Ciencias']
const lessonTypeText: Record<LessonType, string> = {
  PreExam: 'Pre examen',
  PostExam: 'Post examen',
}

function App() {
  const [auth, setAuth] = useState<AuthResponse | null>(() => {
    const storedAuth = readStoredAuth()
    setAuthToken(storedAuth?.token ?? null)
    return storedAuth
  })
  const [mode, setMode] = useState<RuntimeMode>(auth ? 'connecting' : 'login')
  const [questions, setQuestions] = useState<QuestionResponse[]>([])
  const [results, setResults] = useState<ExamResultSummaryResponse[]>([])
  const [analytics, setAnalytics] = useState<ExamAnalyticsResponse | null>(null)
  const [lessons, setLessons] = useState<LessonResponse[]>([])
  const [session, setSession] = useState<ExamSessionResponse | null>(null)
  const [feedback, setFeedback] = useState<AnswerResultResponse | null>(null)
  const [selectedOption, setSelectedOption] = useState<number | null>(null)
  const [policy, setPolicy] = useState<DifficultyPolicy>('Balanced')
  const [isBusy, setIsBusy] = useState(false)
  const [connectionError, setConnectionError] = useState<string | null>(null)

  useEffect(() => {
    if (!auth) {
      setAuthToken(null)
      return
    }

    let isMounted = true
    const currentAuth = auth
    setAuthToken(auth.token)

    async function bootstrapAuthenticated() {
      try {
        if (currentAuth.user.role === 'Teacher') {
          const [questionsFromApi, resultsFromApi, analyticsFromApi, lessonsFromApi] = await Promise.all([
            listQuestions(),
            listExamResults(),
            getExamAnalytics(),
            listLessons(),
          ])

          if (!isMounted) return

          setQuestions(questionsFromApi.filter((question) => question.isActive))
          setResults(resultsFromApi)
          setAnalytics(analyticsFromApi)
          setLessons(lessonsFromApi.filter((lesson) => lesson.isActive))
        } else {
          const [studentResults, lessonsFromApi] = await Promise.all([
            listStudentExamResults(),
            listLessons(),
          ])
          if (!isMounted) return

          setResults(studentResults)
          setLessons(lessonsFromApi.filter((lesson) => lesson.isActive))

          const savedSessionId = window.localStorage.getItem(sessionStorageKey(currentAuth.user.id))

          if (savedSessionId) {
            try {
              const savedSession = await getExamSession(savedSessionId)

              if (!isMounted) return

              setSession(savedSession)
              setPolicy(savedSession.policy)
            } catch {
              window.localStorage.removeItem(sessionStorageKey(currentAuth.user.id))
            }
          }
        }

        if (!isMounted) return

        setMode('api')
        setConnectionError(null)
      } catch (error) {
        if (!isMounted) return

        setMode('offline')
        setConnectionError(getErrorMessage(error))
      }
    }

    bootstrapAuthenticated()

    return () => {
      isMounted = false
    }
  }, [auth])

  const currentQuestion = session?.currentQuestion ?? null
  const progress = session ? (session.answeredQuestions / session.maxQuestions) * 100 : 0
  const correctAnswers = getCorrectAnswers(session)
  const personalBest = getPersonalBest(results, policy, session?.id)
  const difficultyCounts = useMemo(() => {
    return questions.reduce<Record<DifficultyLevel, number>>(
      (accumulator, question) => {
        accumulator[question.difficulty] += 1
        return accumulator
      },
      { Easy: 0, Medium: 0, Hard: 0 },
    )
  }, [questions])

  function commitAuth(nextAuth: AuthResponse) {
    setAuthToken(nextAuth.token)
    window.localStorage.setItem(authStorageKey, JSON.stringify(nextAuth))
    setMode('connecting')
    setAuth(nextAuth)
  }

  function handleLogout() {
    setAuthToken(null)
    window.localStorage.removeItem(authStorageKey)
    setMode('login')
    setAuth(null)
    setQuestions([])
    setResults([])
    setAnalytics(null)
    setLessons([])
    setSession(null)
    setFeedback(null)
  }

  async function handleStartExam(focus?: ReinforcementFocus) {
    if (!auth?.user.studentId || mode !== 'api') return

    setIsBusy(true)
    setFeedback(null)
    setSelectedOption(null)

    try {
      const examPolicy: DifficultyPolicy = focus ? 'Conservative' : policy
      const kind: ExamSessionKind = focus ? 'Reinforcement' : 'Standard'
      const newSession = await startExam(
        auth.user.studentId,
        focus?.maxQuestions ?? standardExamQuestionCount,
        examPolicy,
        kind,
        focus?.topic,
        focus?.competency,
      )

      setPolicy(newSession.policy)
      setSession(newSession)
      window.localStorage.setItem(sessionStorageKey(auth.user.id), newSession.id)
      setConnectionError(null)
    } catch (error) {
      setConnectionError(getErrorMessage(error))
    } finally {
      setIsBusy(false)
    }
  }

  async function handleAnswer(optionOrder: number) {
    if (!session || !currentQuestion || mode !== 'api') return

    setIsBusy(true)
    setSelectedOption(optionOrder)

    try {
      const result = await answerQuestion(session.id, currentQuestion.id, optionOrder)

      setFeedback(result)
      setSession(result.session)
      window.localStorage.setItem(sessionStorageKey(auth?.user.id ?? 'student'), result.session.id)

      if (result.session.status === 'Completed') {
        const studentResults = await listStudentExamResults()
        setResults(studentResults)
      }

      setConnectionError(null)
    } catch (error) {
      setConnectionError(getErrorMessage(error))
    } finally {
      setIsBusy(false)
    }
  }

  async function refreshTeacherData() {
    const [questionsFromApi, resultsFromApi, analyticsFromApi] = await Promise.all([
      listQuestions(),
      listExamResults(),
      getExamAnalytics(),
    ])

    setQuestions(questionsFromApi.filter((question) => question.isActive))
    setResults(resultsFromApi)
    setAnalytics(analyticsFromApi)
  }

  async function refreshLessons() {
    const lessonsFromApi = await listLessons()
    setLessons(lessonsFromApi.filter((lesson) => lesson.isActive))
  }

  if (!auth) {
    return (
      <main className="app-shell">
        <Header />
        <LoginScreen onAuthenticated={commitAuth} />
      </main>
    )
  }

  return (
    <main className="app-shell">
      <Header userName={auth.user.fullName} onLogout={handleLogout} />

      {auth.user.role === 'Teacher' ? (
        <TeacherDashboard
          questions={questions}
          results={results}
          analytics={analytics}
          lessons={lessons}
          difficultyCounts={difficultyCounts}
          isBusy={isBusy}
          connectionError={connectionError}
          onBusyChange={setIsBusy}
          onError={setConnectionError}
          onRefresh={refreshTeacherData}
          onRefreshLessons={refreshLessons}
        />
      ) : (
        <StudentDashboard
          userName={auth.user.fullName}
          results={results}
          session={session}
          lessons={lessons}
          currentQuestion={currentQuestion}
          progress={progress}
          correctAnswers={correctAnswers}
          selectedOption={selectedOption}
          feedback={feedback}
          policy={policy}
          maxQuestions={standardExamQuestionCount}
          personalBest={personalBest}
          isBusy={isBusy}
          connectionError={connectionError}
          onPolicyChange={setPolicy}
          onStart={handleStartExam}
          onStartFocused={handleStartExam}
          onAnswer={handleAnswer}
        />
      )}
    </main>
  )
}

function Header({
  userName,
  onLogout,
}: {
  userName?: string
  onLogout?: () => void
}) {
  return (
    <header className="topbar">
      <div className="brand">
        <BrainCircuit aria-hidden="true" />
        <div>
          <strong>EvaluaT</strong>
          <span>Motor de examenes adaptativos</span>
        </div>
      </div>
      <div className="topbar-actions">
        {userName && (
          <span className="user-chip">
            <UserRoundCheck aria-hidden="true" />
            {userName}
          </span>
        )}
        {onLogout && (
          <button type="button" className="logout-button" onClick={onLogout}>
            <LogOut aria-hidden="true" />
            Salir
          </button>
        )}
      </div>
    </header>
  )
}

function LoginScreen({ onAuthenticated }: { onAuthenticated: (auth: AuthResponse) => void }) {
  const [role, setRole] = useState<UserRole>('Teacher')
  const [fullName, setFullName] = useState('Estudiante Local')
  const [email, setEmail] = useState('docente@evaluat.local')
  const [password, setPassword] = useState('Docente123!')
  const [isRegistering, setIsRegistering] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function selectRole(nextRole: UserRole) {
    setRole(nextRole)
    setError(null)

    if (nextRole === 'Teacher') {
      setEmail('docente@evaluat.local')
      setPassword('Docente123!')
      setIsRegistering(false)
    } else {
      setEmail('estudiante@evaluat.local')
      setPassword('Estudiante123!')
    }
  }

  async function submitLogin() {
    setError(null)

    try {
      const auth = isRegistering
        ? await registerStudent(fullName, email, password)
        : await login(email, password)

      onAuthenticated(auth)
    } catch (error) {
      setError(getErrorMessage(error))
    }
  }

  return (
    <section className="login-page">
      <div className="login-card">
        <div className="login-heading">
          <UserRoundCheck aria-hidden="true" />
          <div>
            <h1>Ingreso exclusivo</h1>
            <p>Docentes gestionan preguntas y resultados. Estudiantes rinden examenes.</p>
          </div>
        </div>

        <div className="segmented" role="group" aria-label="Tipo de usuario">
          {(['Teacher', 'Student'] as UserRole[]).map((item) => (
            <button
              type="button"
              key={item}
              className={role === item ? 'active' : ''}
              onClick={() => selectRole(item)}
            >
              {item === 'Teacher' ? 'Docente' : 'Estudiante'}
            </button>
          ))}
        </div>

        {role === 'Student' && (
          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={isRegistering}
              onChange={(event) => setIsRegistering(event.target.checked)}
            />
            Crear cuenta de estudiante
          </label>
        )}

        {isRegistering && (
          <label>
            Nombre completo
            <input value={fullName} onChange={(event) => setFullName(event.target.value)} />
          </label>
        )}

        <label>
          Correo
          <input value={email} onChange={(event) => setEmail(event.target.value)} />
        </label>
        <label>
          Contrasena
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </label>

        {error && (
          <section className="connection-alert">
            <WifiOff aria-hidden="true" />
            <div>
              <strong>No se pudo iniciar sesion.</strong>
              <span>{error}</span>
            </div>
          </section>
        )}

        <button type="button" className="primary-button" onClick={submitLogin}>
          <CheckCircle2 aria-hidden="true" />
          Entrar
        </button>

        <div className="credentials-note">
          <strong>Docente:</strong> docente@evaluat.local / Docente123!
          <br />
          <strong>Estudiante:</strong> estudiante@evaluat.local / Estudiante123!
        </div>
      </div>
    </section>
  )
}

function TeacherDashboard({
  questions,
  results,
  analytics,
  lessons,
  difficultyCounts,
  isBusy,
  connectionError,
  onBusyChange,
  onError,
  onRefresh,
  onRefreshLessons,
}: {
  questions: QuestionResponse[]
  results: ExamResultSummaryResponse[]
  analytics: ExamAnalyticsResponse | null
  lessons: LessonResponse[]
  difficultyCounts: Record<DifficultyLevel, number>
  isBusy: boolean
  connectionError: string | null
  onBusyChange: (isBusy: boolean) => void
  onError: (message: string | null) => void
  onRefresh: () => Promise<void>
  onRefreshLessons: () => Promise<void>
}) {
  const [topic, setTopic] = useState<Topic>('Programacion')
  const [competency, setCompetency] = useState('Patrones de diseno')
  const [text, setText] = useState('')
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('Medium')
  const [options, setOptions] = useState(['', '', '', ''])
  const [correctIndex, setCorrectIndex] = useState(0)
  const [lessonTopic, setLessonTopic] = useState<Topic>('Programacion')
  const [lessonCompetency, setLessonCompetency] = useState('')
  const [lessonType, setLessonType] = useState<LessonType>('PreExam')
  const [lessonTitle, setLessonTitle] = useState('')
  const [lessonContent, setLessonContent] = useState('')
  const [teacherPage, setTeacherPage] = useState<TeacherPage>('questions')

  async function submitQuestion() {
    onBusyChange(true)
    onError(null)

    try {
      await createQuestion({
        topic,
        competency,
        text,
        difficulty,
        options: options.map((option, index) => ({
          text: option,
          isCorrect: index === correctIndex,
        })),
      })
      setText('')
      setOptions(['', '', '', ''])
      setCorrectIndex(0)
      await onRefresh()
    } catch (error) {
      onError(getErrorMessage(error))
    } finally {
      onBusyChange(false)
    }
  }

  async function removeQuestion(questionId: string) {
    onBusyChange(true)
    onError(null)

    try {
      await deleteQuestion(questionId)
      await onRefresh()
    } catch (error) {
      onError(getErrorMessage(error))
    } finally {
      onBusyChange(false)
    }
  }

  async function submitLesson() {
    onBusyChange(true)
    onError(null)

    try {
      await createLesson({
        topic: lessonTopic,
        competency: lessonCompetency.trim() || null,
        type: lessonType,
        title: lessonTitle,
        content: lessonContent,
        resourceUrl: null,
      })
      setLessonTitle('')
      setLessonContent('')
      setLessonCompetency('')
      await onRefreshLessons()
    } catch (error) {
      onError(getErrorMessage(error))
    } finally {
      onBusyChange(false)
    }
  }

  async function removeLesson(lessonId: string) {
    onBusyChange(true)
    onError(null)

    try {
      await deleteLesson(lessonId)
      await onRefreshLessons()
    } catch (error) {
      onError(getErrorMessage(error))
    } finally {
      onBusyChange(false)
    }
  }

  const standardResults = results.filter((result) => result.kind === 'Standard')
  const reinforcementResults = results.filter((result) => result.kind === 'Reinforcement')

  return (
    <section className="teacher-workspace">
      {connectionError && (
        <section className="connection-alert">
          <WifiOff aria-hidden="true" />
          <div>
            <strong>No se pudo conectar con la API local.</strong>
            <span>{connectionError}</span>
          </div>
        </section>
      )}

      <section className="panel teacher-summary">
        <PanelTitle icon={<Gauge aria-hidden="true" />} title="Panel docente" />
        <div className="teacher-metrics">
          <Metric label="Preguntas" value={`${questions.length}`} />
          <Metric label="Examenes" value={`${standardResults.length}`} />
          <Metric label="Guias" value={`${reinforcementResults.length}`} />
          <Metric
            label="Promedio estandar"
            value={`${averageScore(standardResults)}%`}
          />
        </div>
        <div className="difficulty-row horizontal">
          <DifficultyCount difficulty="Easy" count={difficultyCounts.Easy} />
          <DifficultyCount difficulty="Medium" count={difficultyCounts.Medium} />
          <DifficultyCount difficulty="Hard" count={difficultyCounts.Hard} />
        </div>
      </section>

      <nav className="teacher-nav" aria-label="Navegacion docente">
        <button
          type="button"
          className={teacherPage === 'questions' ? 'active' : ''}
          onClick={() => setTeacherPage('questions')}
        >
          <Database aria-hidden="true" />
          Preguntas
        </button>
        <button
          type="button"
          className={teacherPage === 'lessons' ? 'active' : ''}
          onClick={() => setTeacherPage('lessons')}
        >
          <BrainCircuit aria-hidden="true" />
          Lecciones
        </button>
        <button
          type="button"
          className={teacherPage === 'results' ? 'active' : ''}
          onClick={() => setTeacherPage('results')}
        >
          <ClipboardList aria-hidden="true" />
          Resultados
        </button>
        <button
          type="button"
          className={teacherPage === 'analytics' ? 'active' : ''}
          onClick={() => setTeacherPage('analytics')}
        >
          <Gauge aria-hidden="true" />
          Analitica
        </button>
      </nav>

      {teacherPage === 'questions' && <section className="teacher-grid teacher-page-panel">
        <section className="panel">
          <PanelTitle icon={<Plus aria-hidden="true" />} title="Nueva pregunta" />
          <label>
            Tema
            <select value={topic} onChange={(event) => setTopic(event.target.value as Topic)}>
              {allowedTopics.map((item) => (
                <option value={item} key={item}>{item}</option>
              ))}
            </select>
          </label>
          <label>
            Competencia
            <input value={competency} onChange={(event) => setCompetency(event.target.value)} />
          </label>
          <label>
            Enunciado
            <input value={text} onChange={(event) => setText(event.target.value)} />
          </label>
          <label>
            Dificultad
            <select value={difficulty} onChange={(event) => setDifficulty(event.target.value as DifficultyLevel)}>
              <option value="Easy">Basica</option>
              <option value="Medium">Media</option>
              <option value="Hard">Alta</option>
            </select>
          </label>
          <div className="option-editor">
            {options.map((option, index) => (
              <label key={index}>
                Opcion {String.fromCharCode(65 + index)}
                <div className="option-input-row">
                  <input
                    value={option}
                    onChange={(event) => {
                      const nextOptions = [...options]
                      nextOptions[index] = event.target.value
                      setOptions(nextOptions)
                    }}
                  />
                  <input
                    type="radio"
                    name="correct-option"
                    checked={correctIndex === index}
                    onChange={() => setCorrectIndex(index)}
                    aria-label={`Marcar opcion ${String.fromCharCode(65 + index)} como correcta`}
                  />
                </div>
              </label>
            ))}
          </div>
          <button type="button" className="primary-button" onClick={submitQuestion} disabled={isBusy}>
            <Plus aria-hidden="true" />
            Crear pregunta
          </button>
        </section>

        <section className="panel">
          <PanelTitle icon={<Database aria-hidden="true" />} title="Banco de preguntas" />
          <div className="table-list scrollable-list">
            {questions.map((question) => (
              <div className="table-row" key={question.id}>
                <div>
                  <DifficultyBadge difficulty={question.difficulty} />
                  <strong>{question.topic}</strong>
                  <em>{question.competency}</em>
                  <span>{question.text}</span>
                </div>
                <button type="button" className="icon-button" onClick={() => removeQuestion(question.id)}>
                  <Trash2 aria-hidden="true" />
                </button>
              </div>
            ))}
          </div>
        </section>
      </section>}

      {teacherPage === 'lessons' && <section className="teacher-grid teacher-page-panel">
        <section className="panel">
          <PanelTitle icon={<BrainCircuit aria-hidden="true" />} title="Nueva leccion" />
          <label>
            Tema
            <select value={lessonTopic} onChange={(event) => setLessonTopic(event.target.value as Topic)}>
              {allowedTopics.map((item) => (
                <option value={item} key={item}>{item}</option>
              ))}
            </select>
          </label>
          <label>
            Competencia opcional
            <input value={lessonCompetency} onChange={(event) => setLessonCompetency(event.target.value)} />
          </label>
          <label>
            Tipo
            <select value={lessonType} onChange={(event) => setLessonType(event.target.value as LessonType)}>
              <option value="PreExam">Pre examen</option>
              <option value="PostExam">Post examen</option>
            </select>
          </label>
          <label>
            Titulo
            <input value={lessonTitle} onChange={(event) => setLessonTitle(event.target.value)} />
          </label>
          <label>
            Contenido
            <textarea value={lessonContent} onChange={(event) => setLessonContent(event.target.value)} />
          </label>
          <button type="button" className="primary-button" onClick={submitLesson} disabled={isBusy}>
            <Plus aria-hidden="true" />
            Crear leccion
          </button>
        </section>

        <section className="panel">
          <PanelTitle icon={<ClipboardList aria-hidden="true" />} title="Lecciones activas" />
          <div className="table-list lesson-list scrollable-list">
            {lessons.length === 0 ? (
              <p className="muted">Todavia no hay lecciones registradas.</p>
            ) : (
              lessons.map((lesson) => (
                <div className="lesson-row" key={lesson.id}>
                  <div>
                    <span className="lesson-type">{lessonTypeText[lesson.type]}</span>
                    <strong>{lesson.title}</strong>
                    <span>{lesson.topic}{lesson.competency ? ` - ${lesson.competency}` : ''}</span>
                    <p>{lesson.content}</p>
                  </div>
                  <button type="button" className="icon-button" onClick={() => removeLesson(lesson.id)}>
                    <Trash2 aria-hidden="true" />
                  </button>
                </div>
              ))
            )}
          </div>
        </section>
      </section>}

      {teacherPage === 'results' && <section className="teacher-results-grid teacher-page-panel">
        <section className="panel teacher-data-panel">
          <PanelTitle icon={<ClipboardList aria-hidden="true" />} title="Resultados de examenes estandar" />
          <div className="results-table scrollable-list large">
            {standardResults.length === 0 ? (
              <p className="muted">Todavia no existen examenes estandar completados o en curso.</p>
            ) : (
              standardResults.map((result) => (
                <div className="result-row" key={result.id}>
                  <strong>{result.studentName}</strong>
                  <span>{formatDateTime(result.startedAt)}</span>
                  <span>{result.status === 'Completed' ? 'Finalizado' : 'En curso'}</span>
                  <span>{policyText[result.policy]}</span>
                  <span>{summarizeDiagnostic(result.diagnostic)}</span>
                  <span>
                    {result.answeredQuestions}/{result.maxQuestions}
                  </span>
                  <strong>{result.scorePercentage}%</strong>
                </div>
              ))
            )}
          </div>
        </section>

        <section className="panel teacher-data-panel">
          <PanelTitle icon={<BrainCircuit aria-hidden="true" />} title="Guias de refuerzo" />
          <div className="results-table scrollable-list large">
            {reinforcementResults.length === 0 ? (
              <p className="muted">Todavia no hay guias de refuerzo iniciadas por estudiantes.</p>
            ) : (
              reinforcementResults.map((result) => (
                <div className="result-row" key={result.id}>
                  <strong>{result.studentName}</strong>
                  <span>{formatDateTime(result.startedAt)}</span>
                  <span>{result.status === 'Completed' ? 'Finalizada' : 'En curso'}</span>
                  <span>{result.targetTopic ?? 'Sin tema'}</span>
                  <span>{result.targetCompetency ?? summarizeDiagnostic(result.diagnostic)}</span>
                  <span>{result.answeredQuestions}/{result.maxQuestions}</span>
                  <strong>{result.scorePercentage}%</strong>
                </div>
              ))
            )}
          </div>
        </section>
      </section>}

      {teacherPage === 'analytics' && <section className="panel analytics-panel teacher-page-panel">
        <PanelTitle icon={<Gauge aria-hidden="true" />} title="Analitica de dificultad" />
        {!analytics || (analytics.topics.every((topic) => topic.answerCount === 0) && analytics.questions.length === 0) ? (
          <p className="muted">Todavia no hay suficientes examenes estandar completados para calcular estadisticas.</p>
        ) : (
          <div className="analytics-grid">
            <section>
              <h3>Temas con mas fallos</h3>
              <div className="analytics-list">
                {analytics.topics.map((topic) => (
                  <article key={topic.topic}>
                    <strong>{topic.topic}</strong>
                    <span>{topic.incorrectCount}/{topic.answerCount} fallos</span>
                    <b>{topic.errorPercentage}%</b>
                  </article>
                ))}
              </div>
            </section>
            <section>
              <h3>Preguntas con mas fallos</h3>
              <div className="analytics-list question-analytics-list">
                {analytics.questions.map((question) => (
                  <article key={question.questionId}>
                    <strong>{question.topic} - {difficultyText[question.difficulty]}</strong>
                    <span>{question.text}</span>
                    <b>{question.errorPercentage}% error ({question.incorrectCount}/{question.answerCount})</b>
                  </article>
                ))}
              </div>
            </section>
          </div>
        )}
      </section>}
    </section>
  )
}

function StudentDashboard({
  userName,
  results,
  session,
  lessons,
  currentQuestion,
  progress,
  correctAnswers,
  selectedOption,
  feedback,
  policy,
  maxQuestions,
  personalBest,
  isBusy,
  connectionError,
  onPolicyChange,
  onStart,
  onStartFocused,
  onAnswer,
}: {
  userName: string
  results: ExamResultSummaryResponse[]
  session: ExamSessionResponse | null
  lessons: LessonResponse[]
  currentQuestion: ExamSessionResponse['currentQuestion']
  progress: number
  correctAnswers: number
  selectedOption: number | null
  feedback: AnswerResultResponse | null
  policy: DifficultyPolicy
  maxQuestions: number
  personalBest: number | null
  isBusy: boolean
  connectionError: string | null
  onPolicyChange: (policy: DifficultyPolicy) => void
  onStart: () => void
  onStartFocused: (focus: ReinforcementFocus) => void | Promise<void>
  onAnswer: (optionOrder: number) => void
}) {
  const [showIntro, setShowIntro] = useState(true)
  const [studentPage, setStudentPage] = useState<StudentPage>('exam')
  const [timerNow, setTimerNow] = useState(() => Date.now())
  const hasActiveExam = session?.status === 'InProgress'
  const canStartExam = !hasActiveExam && !isBusy
  const elapsedSeconds = getElapsedSeconds(session, timerNow)
  const preExamLessons = lessons.filter((lesson) => lesson.type === 'PreExam')
  const isCompleted = session?.status === 'Completed'
  const isReinforcement = session?.kind === 'Reinforcement'
  const latestStandardResult = results.find((result) => result.kind === 'Standard' && result.status === 'Completed')
  const visibleDiagnosticRaw = session?.kind === 'Standard' && session.status === 'Completed'
    ? session.diagnostic ?? []
    : latestStandardResult?.diagnostic ?? []
  const hasDiagnosticData = visibleDiagnosticRaw.length > 0
  const diagnostic = normalizeDiagnosticByTopic(visibleDiagnosticRaw)
  const postExamLessons = hasDiagnosticData ? getPostExamLessons(lessons, diagnostic) : []
  const routeDiagnostic = buildGlobalDiagnostic(results, session)
  const routeLessons = getPostExamLessons(lessons, routeDiagnostic)
  const learningPathItems = buildLearningPathItems(routeDiagnostic, routeLessons)
  const standardHistory = results.filter((result) => result.kind === 'Standard')
  const reinforcementHistory = results.filter((result) => result.kind === 'Reinforcement')
  const errorReview = isCompleted ? session?.errorReview ?? [] : []

  async function startFocusedGuide(focus: ReinforcementFocus) {
    await onStartFocused(focus)
    setStudentPage('exam')
  }

  useEffect(() => {
    if (!hasActiveExam) {
      setTimerNow(Date.now())
      return
    }

    const intervalId = window.setInterval(() => setTimerNow(Date.now()), 1000)
    return () => window.clearInterval(intervalId)
  }, [hasActiveExam])

  return (
    <section className="workspace">
      {showIntro && (
        <section className="intro-modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="student-intro-title">
          <article className="intro-modal">
            <div className="intro-icon" aria-hidden="true">
              <BrainCircuit />
            </div>
            <span className="eyebrow">Bienvenido a EvaluaT</span>
            <h2 id="student-intro-title">Tu examen se adapta a tu nivel real</h2>
            <p>
              EvaluaT es un motor de examenes adaptativos que ajusta la dificultad de las preguntas segun tus respuestas. Su finalidad no es solo entregar una nota, sino identificar tus competencias dominadas y las que necesitan refuerzo.
            </p>
            <div className="intro-points">
              <div>
                <strong>Finalidad</strong>
                <span>Medir tu progreso con menos preguntas y darte un diagnostico util para mejorar.</span>
              </div>
              <div>
                <strong>Compromiso</strong>
                <span>La plataforma protege tus respuestas correctas, evita exponer el banco de preguntas y usa tus resultados para orientar tu aprendizaje.</span>
              </div>
            </div>
            {preExamLessons.length > 0 && (
              <div className="intro-lessons">
                <strong>Lecciones antes de iniciar</strong>
                {preExamLessons.slice(0, 3).map((lesson) => (
                  <article key={lesson.id}>
                    <span>{lesson.topic}</span>
                    <h3>{lesson.title}</h3>
                    <p>{lesson.content}</p>
                  </article>
                ))}
              </div>
            )}
            <button type="button" className="primary-button" onClick={() => setShowIntro(false)}>
              <ShieldCheck aria-hidden="true" />
              Entendido, iniciar experiencia
            </button>
          </article>
        </section>
      )}

      <aside className="sidebar">
        <section className="panel profile-panel">
          <div className="readonly-profile">
            <span className="student-avatar" aria-hidden="true">
              {getInitials(userName)}
            </span>
            <strong>{userName}</strong>
            <span>Acceso autenticado</span>
          </div>
        </section>

        <nav className="side-nav" aria-label="Navegacion del estudiante">
          <button
            type="button"
            className={studentPage === 'exam' ? 'side-nav-item active' : 'side-nav-item'}
            onClick={() => setStudentPage('exam')}
          >
            <GraduationCap aria-hidden="true" />
            Estudiante
          </button>
          <button
            type="button"
            className={studentPage === 'route' ? 'side-nav-item active' : 'side-nav-item'}
            onClick={() => setStudentPage('route')}
          >
            <BrainCircuit aria-hidden="true" />
            Ruta adaptativa
          </button>
          <button
            type="button"
            className={studentPage === 'history' ? 'side-nav-item active' : 'side-nav-item'}
            onClick={() => setStudentPage('history')}
          >
            <ClipboardList aria-hidden="true" />
            Mi historial
          </button>
        </nav>

      </aside>

      <section className="exam-surface">
        <div className="exam-header">
          <div>
            <span className="eyebrow">
              {studentPage === 'exam' ? 'Sesion activa' : studentPage === 'route' ? 'Plan personalizado' : 'Seguimiento personal'}
            </span>
            <h1>
              {studentPage === 'exam'
                ? currentQuestion ? currentQuestion.topic : 'Examen adaptativo'
                : studentPage === 'route'
                  ? 'Ruta adaptativa'
                  : 'Mi historial'}
            </h1>
          </div>
        </div>

        {studentPage === 'exam' && (
          <div className="progress-track" aria-label="Progreso de respuestas">
            <span style={{ width: `${progress}%` }} />
          </div>
        )}

        {connectionError && (
          <section className="connection-alert">
            <WifiOff aria-hidden="true" />
            <div>
              <strong>No se pudo conectar con la API local.</strong>
              <span>{connectionError}</span>
            </div>
          </section>
        )}

        {studentPage === 'exam' && !hasActiveExam && (
          <section className="panel settings-panel exam-settings-panel">
            <PanelTitle icon={<Settings2 aria-hidden="true" />} title="Configuracion" />
            <div className="segmented" role="group" aria-label="Politica de dificultad">
              {(['Balanced', 'Conservative'] as DifficultyPolicy[]).map((item) => (
                <button
                  type="button"
                  key={item}
                  className={policy === item ? 'active' : ''}
                  onClick={() => onPolicyChange(item)}
                >
                  {studentPolicyText[item]}
                </button>
              ))}
            </div>
            <p className="policy-helper">{studentPolicyDescription[policy]}</p>
            <div className="exam-settings-grid">
              <div className="record-card">
                <span>Prueba estandar</span>
                <strong>{maxQuestions} preguntas</strong>
              </div>
              <div className="record-card highlight">
                <span>Record anterior a superar en {studentPolicyText[policy]}</span>
                <strong>{personalBest === null ? 'Sin record previo' : `${personalBest}%`}</strong>
              </div>
              <div className="timer-card">
                <span>Cronometro</span>
                <strong>{formatDuration(elapsedSeconds)}</strong>
              </div>
            </div>
            <button type="button" className="primary-button" onClick={() => onStart()} disabled={!canStartExam}>
              <Play aria-hidden="true" />
              Comenzar prueba
            </button>
          </section>
        )}

        {studentPage === 'exam' && (currentQuestion ? (
            <article className="question-panel question-enter" key={currentQuestion.id}>
              <div className="question-meta">
                <DifficultyBadge difficulty={currentQuestion.difficulty} />
                <span>{currentQuestion.competency}</span>
                <span>
                  {session?.answeredQuestions ?? 0} / {session?.maxQuestions ?? maxQuestions}
                </span>
              </div>
              <h2>{currentQuestion.text}</h2>
              <div className="answer-grid">
                {currentQuestion.options.map((option) => (
                  <button
                    type="button"
                    key={option.order}
                    className={selectedOption === option.order ? 'selected-answer' : ''}
                    onClick={() => onAnswer(option.order)}
                    disabled={isBusy}
                  >
                    <span>{String.fromCharCode(65 + option.order)}</span>
                    {option.text}
                  </button>
                ))}
              </div>
            </article>
          ) : (
            <article className="empty-state">
              <ClipboardList aria-hidden="true" />
              <h2>{isCompleted ? 'Sesion finalizada' : 'Listo para iniciar'}</h2>
              <p>
                {isCompleted
                  ? isReinforcement
                    ? 'La guia de refuerzo termino y queda separada del record estandar.'
                    : 'El examen estandar termino. Puedes revisar el diagnostico desde esta misma pagina.'
                  : userName}
              </p>
              {isCompleted ? (
                <div className="completion-summary">
                  <div>
                    <span>Puntaje</span>
                    <strong>{session?.scorePercentage ?? 0}%</strong>
                  </div>
                  <div>
                    <span>Tipo</span>
                    <strong>{isReinforcement ? 'Guia' : 'Estandar'}</strong>
                  </div>
                  <div>
                    <span>Tiempo</span>
                    <strong>{formatDuration(elapsedSeconds)}</strong>
                  </div>
                </div>
              ) : (
                <div className="system-visual" aria-hidden="true">
                  <span />
                  <span />
                  <span />
                  <span />
                </div>
              )}
            </article>
          ))}

        {studentPage === 'route' && (
          <StudentRoutePage
            items={learningPathItems}
            isBusy={isBusy}
            onStartFocused={startFocusedGuide}
          />
        )}

        {studentPage === 'history' && (
          <StudentHistoryPage
            standardResults={standardHistory}
            reinforcementResults={reinforcementHistory}
          />
        )}

        {studentPage === 'exam' && feedback && (
          <section className={feedback.isCorrect ? 'feedback success' : 'feedback danger'}>
            {feedback.isCorrect ? <CheckCircle2 aria-hidden="true" /> : <XCircle aria-hidden="true" />}
            <div>
              <strong>{feedback.isCorrect ? 'Respuesta correcta' : 'Respuesta incorrecta'}</strong>
              <span>
                Siguiente dificultad: {difficultyText[feedback.nextDifficulty]} - Puntaje {feedback.scorePercentage}%
              </span>
            </div>
          </section>
        )}

        {studentPage === 'exam' && errorReview.length > 0 && (
          <StudentErrorReview items={errorReview} />
        )}
      </section>

      <aside className="inspector">
        <section className="panel metrics-panel">
          <PanelTitle icon={<Gauge aria-hidden="true" />} title="Resultado" />
          <div className="score-visual">
            <span>Puntaje</span>
            <strong>{session?.scorePercentage ?? 0}%</strong>
            <div className="progress-track compact" aria-hidden="true">
              <span style={{ width: `${session?.scorePercentage ?? 0}%` }} />
            </div>
          </div>
          <Metric label="Correctas" value={`${correctAnswers}/${session?.maxQuestions ?? maxQuestions}`} />
          <Metric label="Respondidas" value={`${session?.answeredQuestions ?? 0}/${session?.maxQuestions ?? maxQuestions}`} />
          <Metric label="Modo" value={studentPolicyText[session?.policy ?? policy]} />
          <Metric label="Record anterior" value={personalBest === null ? 'Sin record' : `${personalBest}%`} />
          <Metric label="Tiempo" value={formatDuration(elapsedSeconds)} />
        </section>

        {hasDiagnosticData && (
          <section className="panel diagnostic-panel">
            <PanelTitle icon={<BrainCircuit aria-hidden="true" />} title="Diagnostico" />
            <DiagnosticList diagnostic={diagnostic} />
          </section>
        )}

        {postExamLessons.length > 0 && (
          <section className="panel lessons-panel">
            <PanelTitle icon={<ClipboardList aria-hidden="true" />} title="Refuerzo" />
            <LessonList lessons={postExamLessons} />
          </section>
        )}

      </aside>
    </section>
  )
}

type LearningPathItem = CompetencyDiagnosticResponse & {
  lessons: LessonResponse[]
  targetScore: number
  practiceQuestions: number
}

function StudentErrorReview({ items }: { items: ErrorReviewItemResponse[] }) {
  return (
    <section className="error-review-panel">
      <div className="student-page-heading compact">
        <span>Retroalimentacion</span>
        <h2>Revision de errores</h2>
        <p>Estas explicaciones aparecen solo al finalizar la sesion.</p>
      </div>
      <div className="error-review-list">
        {items.map((item) => (
          <article className="error-review-item" key={item.questionId}>
            <span>{item.topic}</span>
            <h3>{item.questionText}</h3>
            <div className="answer-review-grid">
              <div>
                <strong>Tu respuesta</strong>
                <p>{item.selectedOptionText}</p>
              </div>
              <div>
                <strong>Respuesta correcta</strong>
                <p>{item.correctOptionText}</p>
              </div>
            </div>
            <p>{item.explanation}</p>
          </article>
        ))}
      </div>
    </section>
  )
}

function StudentRoutePage({
  items,
  isBusy,
  onStartFocused,
}: {
  items: LearningPathItem[]
  isBusy: boolean
  onStartFocused: (focus: ReinforcementFocus) => void | Promise<void>
}) {
  if (items.length === 0) {
    return (
      <article className="student-page-card empty-route">
        <BrainCircuit aria-hidden="true" />
        <h2>Ruta adaptativa pendiente</h2>
        <p>Finaliza un examen estandar para generar una ruta basada en tus temas principales.</p>
      </article>
    )
  }

  return (
    <section className="student-page-card route-page">
      <div className="student-page-heading">
        <span>Plan separado del examen estandar</span>
        <h2>Ruta adaptativa de refuerzo</h2>
        <p>Estas actividades no modifican tu record. Sirven para practicar el tema que necesita refuerzo.</p>
      </div>
      <div className="route-list">
        {items.map((item, index) => (
          <article className="route-card" key={item.topic}>
            <div className="route-card-index">{index + 1}</div>
            <div>
              <span>Tema principal</span>
              <h3>{item.topic}</h3>
              <p>{item.recommendation}</p>
              {item.lessons.length > 0 && <strong>Leccion sugerida: {item.lessons[0].title}</strong>}
              <button
                type="button"
                className="secondary-button"
                onClick={() => onStartFocused({
                  topic: item.topic,
                  maxQuestions: focusedExamQuestionCount,
                })}
                disabled={isBusy}
              >
                <Play aria-hidden="true" />
                Iniciar guia de {item.practiceQuestions} preguntas
              </button>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

function StudentHistoryPage({
  standardResults,
  reinforcementResults,
}: {
  standardResults: ExamResultSummaryResponse[]
  reinforcementResults: ExamResultSummaryResponse[]
}) {
  return (
    <section className="student-page-card history-page">
      <div className="student-page-heading">
        <span>Registro personal</span>
        <h2>Mi historial</h2>
        <p>Los examenes estandar cuentan para record. Las guias quedan separadas como practica.</p>
      </div>
      <HistoryList title="Examenes estandar" results={standardResults} emptyText="Todavia no tienes examenes estandar." />
      <HistoryList title="Guias de refuerzo" results={reinforcementResults} emptyText="Todavia no tienes guias de refuerzo." />
    </section>
  )
}

function HistoryList({
  title,
  results,
  emptyText,
}: {
  title: string
  results: ExamResultSummaryResponse[]
  emptyText: string
}) {
  return (
    <section className="history-section">
      <h3>{title}</h3>
      {results.length === 0 ? (
        <p className="muted">{emptyText}</p>
      ) : (
        <div className="history-list">
          {results.map((result) => (
            <article className="history-item" key={result.id}>
              <div>
                <strong>{result.scorePercentage}%</strong>
                <span>{result.status === 'Completed' ? 'Finalizado' : 'En curso'}</span>
              </div>
              <p>{formatDateTime(result.startedAt)}</p>
              <p>{result.targetTopic ?? summarizeDiagnostic(result.diagnostic)}</p>
              <span>{result.answeredQuestions}/{result.maxQuestions} preguntas</span>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

function buildLearningPathItems(
  diagnostic: CompetencyDiagnosticResponse[],
  lessons: LessonResponse[],
): LearningPathItem[] {
  const orderedDiagnostic = normalizeDiagnosticByTopic(diagnostic)
    .filter((item) => item.answeredQuestions > 0)
    .sort((first, second) => first.weightedScorePercentage - second.weightedScorePercentage)
  const targets = orderedDiagnostic.filter((item) => item.level !== 'Dominado')
  const selectedTargets = targets.length > 0 ? targets : orderedDiagnostic.slice(0, 1)

  return selectedTargets.slice(0, 3).map((item) => ({
    ...item,
    lessons: lessons.filter((lesson) => {
      return lesson.topic === item.topic && !lesson.competency
    }),
    targetScore: item.weightedScorePercentage < 60 ? 70 : 85,
    practiceQuestions: focusedExamQuestionCount,
  }))
}

function normalizeDiagnosticByTopic(
  diagnostic: CompetencyDiagnosticResponse[] = [],
): CompetencyDiagnosticResponse[] {
  return allowedTopics.map((topic) => {
    const items = diagnostic.filter((item) => item.topic === topic)
    const answeredQuestions = items.reduce((total, item) => total + item.answeredQuestions, 0)
    const correctAnswers = items.reduce((total, item) => total + item.correctAnswers, 0)
    const scorePercentage = answeredQuestions === 0
      ? 0
      : Math.round((correctAnswers / answeredQuestions) * 100)
    const weightedScorePercentage = getWeightedTopicScore(items, answeredQuestions, scorePercentage)
    const highestDifficulty = getHighestDifficulty(items)
    const confidence = getDiagnosticConfidence(answeredQuestions)
    const pattern = getDiagnosticPattern(items, answeredQuestions, scorePercentage, weightedScorePercentage)
    const level = getTopicDiagnosticLevel(
      answeredQuestions,
      weightedScorePercentage,
      highestDifficulty,
      confidence,
    )

    return {
      topic,
      competency: topic,
      answeredQuestions,
      correctAnswers,
      scorePercentage,
      weightedScorePercentage,
      highestDifficulty,
      level,
      confidence,
      pattern,
      evaluationSummary: getEvaluationSummary(topic, scorePercentage, weightedScorePercentage, confidence, pattern),
      recommendation: getTopicRecommendation(topic, answeredQuestions, weightedScorePercentage, pattern),
    }
  })
}

function buildGlobalDiagnostic(
  results: ExamResultSummaryResponse[],
  currentSession: ExamSessionResponse | null,
): CompetencyDiagnosticResponse[] {
  const diagnostics = results
    .filter((result) => result.kind === 'Standard')
    .filter((result) => result.status === 'Completed')
    .flatMap((result) => normalizeDiagnosticByTopic(result.diagnostic ?? []))

  if (
    currentSession?.kind === 'Standard'
    && currentSession.status === 'Completed'
    && !results.some((result) => result.id === currentSession.id)
  ) {
    diagnostics.push(...normalizeDiagnosticByTopic(currentSession.diagnostic ?? []))
  }

  return allowedTopics.map((topic) => {
    const items = diagnostics.filter((item) => item.topic === topic)
    const answeredQuestions = items.reduce((total, item) => total + item.answeredQuestions, 0)
    const correctAnswers = items.reduce((total, item) => total + item.correctAnswers, 0)
    const scorePercentage = answeredQuestions === 0
      ? 0
      : Math.round((correctAnswers / answeredQuestions) * 100)
    const weightedScorePercentage = getWeightedTopicScore(items, answeredQuestions, scorePercentage)
    const highestDifficulty = getHighestDifficulty(items)
    const confidence = getDiagnosticConfidence(answeredQuestions)
    const pattern = getDiagnosticPattern(items, answeredQuestions, scorePercentage, weightedScorePercentage)
    const level = getTopicDiagnosticLevel(
      answeredQuestions,
      weightedScorePercentage,
      highestDifficulty,
      confidence,
    )

    return {
      topic,
      competency: topic,
      answeredQuestions,
      correctAnswers,
      scorePercentage,
      weightedScorePercentage,
      highestDifficulty,
      level,
      confidence,
      pattern,
      evaluationSummary: getEvaluationSummary(topic, scorePercentage, weightedScorePercentage, confidence, pattern),
      recommendation: getTopicRecommendation(topic, answeredQuestions, weightedScorePercentage, pattern),
    }
  })
}

function getWeightedTopicScore(
  items: CompetencyDiagnosticResponse[],
  answeredQuestions: number,
  fallbackScore: number,
) {
  if (answeredQuestions === 0) {
    return 0
  }

  const weightedTotal = items.reduce((total, item) => {
    return total + ((item.weightedScorePercentage ?? item.scorePercentage) * item.answeredQuestions)
  }, 0)

  return weightedTotal === 0 ? fallbackScore : Math.round(weightedTotal / answeredQuestions)
}

function getHighestDifficulty(items: CompetencyDiagnosticResponse[]): DifficultyLevel {
  if (items.some((item) => item.highestDifficulty === 'Hard')) {
    return 'Hard'
  }

  if (items.some((item) => item.highestDifficulty === 'Medium')) {
    return 'Medium'
  }

  return 'Easy'
}

function getTopicDiagnosticLevel(
  answeredQuestions: number,
  weightedScorePercentage: number,
  highestDifficulty: DifficultyLevel,
  confidence = getDiagnosticConfidence(answeredQuestions),
) {
  if (answeredQuestions === 0) {
    return 'Sin evaluar'
  }

  if (weightedScorePercentage >= 85 && highestDifficulty !== 'Easy' && confidence !== 'Baja') {
    return 'Dominado'
  }

  if (weightedScorePercentage >= 70) {
    return 'Competente'
  }

  if (weightedScorePercentage >= 55) {
    return 'En desarrollo'
  }

  return 'Reforzar'
}

function getDiagnosticConfidence(answeredQuestions: number) {
  if (answeredQuestions === 0) {
    return 'Sin evidencia'
  }

  if (answeredQuestions <= 2) {
    return 'Baja'
  }

  if (answeredQuestions <= 4) {
    return 'Media'
  }

  return 'Alta'
}

function getDiagnosticPattern(
  items: CompetencyDiagnosticResponse[],
  answeredQuestions: number,
  scorePercentage: number,
  weightedScorePercentage: number,
) {
  const explicitPatterns = items
    .map((item) => item.pattern)
    .filter((pattern) => pattern && pattern !== 'Sin evidencia')

  if (explicitPatterns.includes('Dificultad en bases')) {
    return 'Dificultad en bases'
  }

  if (explicitPatterns.includes('Falla en aplicacion')) {
    return 'Falla en aplicacion'
  }

  if (answeredQuestions === 0) {
    return 'Sin evidencia'
  }

  if (scorePercentage >= 80 && weightedScorePercentage >= 80) {
    return 'Dominio consistente'
  }

  return 'Dominio parcial'
}

function getTopicRecommendation(
  topic: Topic,
  answeredQuestions: number,
  weightedScorePercentage: number,
  pattern: string,
) {
  if (answeredQuestions === 0) {
    return `Aun no hay respuestas suficientes en ${topic} para diagnosticar fortalezas o debilidades.`
  }

  if (pattern === 'Dificultad en bases') {
    return `Reforzar fundamentos de ${topic} antes de subir la dificultad.`
  }

  if (weightedScorePercentage >= 85) {
    return `Mantener ${topic} con problemas de mayor reto.`
  }

  if (pattern === 'Falla en aplicacion') {
    return `Practicar ${topic} con ejercicios guiados de aplicacion.`
  }

  return `Continuar practicando ${topic} para consolidar el dominio.`
}

function getEvaluationSummary(
  topic: Topic,
  scorePercentage: number,
  weightedScorePercentage: number,
  confidence: string,
  pattern: string,
) {
  return `En ${topic}, tu precision fue ${scorePercentage}% y tu puntaje ponderado por dificultad fue ${weightedScorePercentage}%. La confianza del diagnostico es ${confidence.toLowerCase()} y el patron detectado es: ${pattern.toLowerCase()}.`
}

function PanelTitle({ icon, title }: { icon: ReactNode; title: string }) {
  return (
    <div className="panel-title">
      {icon}
      <h2>{title}</h2>
    </div>
  )
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function DifficultyBadge({ difficulty }: { difficulty: DifficultyLevel }) {
  return <span className={`difficulty ${difficulty.toLowerCase()}`}>{difficultyText[difficulty]}</span>
}

function DifficultyCount({ difficulty, count }: { difficulty: DifficultyLevel; count: number }) {
  return (
    <span className={`difficulty-count ${difficulty.toLowerCase()}`}>
      {difficultyText[difficulty]} {count}
    </span>
  )
}

function DiagnosticList({
  diagnostic,
  compact = false,
}: {
  diagnostic: CompetencyDiagnosticResponse[]
  compact?: boolean
}) {
  return (
    <div className={compact ? 'diagnostic-list compact' : 'diagnostic-list'}>
      {diagnostic.map((item) => (
        <article className="diagnostic-item" key={item.topic}>
          <div className="diagnostic-heading">
            <div>
              <strong>{item.topic}</strong>
              <span>Diagnostico general</span>
            </div>
            <span className={`diagnostic-level ${diagnosticLevelClass(item.level)}`}>
              {item.level}
            </span>
          </div>
          <div className="diagnostic-score">
            <span>{item.correctAnswers}/{item.answeredQuestions} correctas</span>
            <strong>{item.scorePercentage}%</strong>
          </div>
          <div className="progress-track compact" aria-hidden="true">
            <span style={{ width: `${item.scorePercentage}%` }} />
          </div>
          {!compact && (
            <>
              <div className="diagnostic-factors">
                <span>Precision: {item.scorePercentage}%</span>
                <span>Ponderado: {item.weightedScorePercentage}%</span>
                <span>Confianza: {item.confidence}</span>
                <span>Dificultad maxima: {difficultyText[item.highestDifficulty]}</span>
                <span>Patron: {item.pattern}</span>
              </div>
              <p>{item.evaluationSummary}</p>
              <p>{item.recommendation}</p>
            </>
          )}
        </article>
      ))}
    </div>
  )
}

function LessonList({
  lessons,
  title,
  compact = false,
}: {
  lessons: LessonResponse[]
  title?: string
  compact?: boolean
}) {
  return (
    <div className={compact ? 'student-lesson-list compact' : 'student-lesson-list'}>
      {title && <strong className="student-lesson-title">{title}</strong>}
      {lessons.map((lesson) => (
        <article className="student-lesson-card" key={lesson.id}>
          <span>{lessonTypeText[lesson.type]} - {lesson.topic}</span>
          <h3>{lesson.title}</h3>
          <p>{lesson.content}</p>
          {lesson.resourceUrl && (
            <a href={lesson.resourceUrl} target="_blank" rel="noreferrer">Abrir recurso</a>
          )}
        </article>
      ))}
    </div>
  )
}

function diagnosticLevelClass(level: string) {
  if (level === 'Dominado') {
    return 'mastered'
  }

  if (level === 'En desarrollo') {
    return 'developing'
  }

  if (level === 'Competente') {
    return 'developing'
  }

  if (level === 'Sin evaluar') {
    return 'unevaluated'
  }

  return 'reinforce'
}

function summarizeDiagnostic(diagnostic: CompetencyDiagnosticResponse[] = []) {
  const byTopic = normalizeDiagnosticByTopic(diagnostic).filter((item) => item.answeredQuestions > 0)

  if (byTopic.length === 0) {
    return 'Sin diagnostico'
  }

  const priority = byTopic.sort((first, second) => first.weightedScorePercentage - second.weightedScorePercentage)[0]
  return `${priority.level}: ${priority.topic}`
}

function readStoredAuth() {
  const rawAuth = window.localStorage.getItem(authStorageKey)

  if (!rawAuth) {
    return null
  }

  try {
    return JSON.parse(rawAuth) as AuthResponse
  } catch {
    window.localStorage.removeItem(authStorageKey)
    return null
  }
}

function sessionStorageKey(userId: string) {
  return `${sessionStoragePrefix}.${userId}`
}

function getInitials(name: string) {
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')

  return initials.toUpperCase() || 'ET'
}

function averageScore(results: ExamResultSummaryResponse[]) {
  if (results.length === 0) {
    return 0
  }

  return Math.round(results.reduce((total, result) => total + result.scorePercentage, 0) / results.length)
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('es-PE', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}

function getCorrectAnswers(session: ExamSessionResponse | null) {
  if (!session) {
    return 0
  }

  return session.correctAnswers ?? session.responses?.filter((response) => response.isCorrect).length ?? 0
}

function getPersonalBest(
  results: ExamResultSummaryResponse[],
  policy: DifficultyPolicy,
  currentSessionId?: string,
) {
  const completedScores = results
    .filter((result) => result.status === 'Completed')
    .filter((result) => result.kind === 'Standard')
    .filter((result) => result.maxQuestions === standardExamQuestionCount)
    .filter((result) => result.policy === policy)
    .filter((result) => result.id !== currentSessionId)
    .map((result) => result.scorePercentage)

  if (completedScores.length === 0) {
    return null
  }

  return Math.max(...completedScores)
}

function getPostExamLessons(
  lessons: LessonResponse[],
  diagnostic: CompetencyDiagnosticResponse[],
) {
  if (diagnostic.length === 0) {
    return []
  }

  const weakItems = diagnostic.filter((item) => item.level !== 'Dominado')
  const targets = weakItems.length > 0 ? weakItems : diagnostic

  return lessons
    .filter((lesson) => lesson.type === 'PostExam')
    .filter((lesson) => targets.some((item) => {
      if (lesson.competency) {
        return lesson.topic === item.topic && lesson.competency === item.competency
      }

      return lesson.topic === item.topic
    }))
    .slice(0, 4)
}

function getElapsedSeconds(session: ExamSessionResponse | null, now: number) {
  if (!session) {
    return 0
  }

  const start = new Date(session.startedAt).getTime()
  const end = session.completedAt ? new Date(session.completedAt).getTime() : now
  return Math.max(0, Math.floor((end - start) / 1000))
}

function formatDuration(totalSeconds: number) {
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.name === 'AbortError'
      ? 'La API no respondio a tiempo. Verifica que el backend este ejecutandose en http://127.0.0.1:5116.'
      : error.message
  }

  return 'Verifica que PostgreSQL y ASP.NET Core esten ejecutandose.'
}

export default App
