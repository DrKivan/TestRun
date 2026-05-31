import {
  BrainCircuit,
  CheckCircle2,
  ClipboardList,
  Database,
  Gauge,
  GraduationCap,
  HelpCircle,
  LockKeyhole,
  LogOut,
  Play,
  Plus,
  RotateCcw,
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
  createQuestion,
  deleteQuestion,
  getExamSession,
  listExamResults,
  listQuestions,
  login,
  registerStudent,
  setAuthToken,
  startExam,
} from './api'
import type {
  AnswerResultResponse,
  AuthResponse,
  DifficultyLevel,
  DifficultyPolicy,
  ExamResultSummaryResponse,
  ExamSessionResponse,
  QuestionResponse,
  UserRole,
} from './types'
import './App.css'

type RuntimeMode = 'login' | 'connecting' | 'api' | 'offline'

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

function App() {
  const [auth, setAuth] = useState<AuthResponse | null>(() => {
    const storedAuth = readStoredAuth()
    setAuthToken(storedAuth?.token ?? null)
    return storedAuth
  })
  const [mode, setMode] = useState<RuntimeMode>(auth ? 'connecting' : 'login')
  const [questions, setQuestions] = useState<QuestionResponse[]>([])
  const [results, setResults] = useState<ExamResultSummaryResponse[]>([])
  const [session, setSession] = useState<ExamSessionResponse | null>(null)
  const [feedback, setFeedback] = useState<AnswerResultResponse | null>(null)
  const [selectedOption, setSelectedOption] = useState<number | null>(null)
  const [policy, setPolicy] = useState<DifficultyPolicy>('Balanced')
  const [maxQuestions, setMaxQuestions] = useState(5)
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
          const [questionsFromApi, resultsFromApi] = await Promise.all([
            listQuestions(),
            listExamResults(),
          ])

          if (!isMounted) return

          setQuestions(questionsFromApi.filter((question) => question.isActive))
          setResults(resultsFromApi)
        } else {
          const savedSessionId = window.localStorage.getItem(sessionStorageKey(currentAuth.user.id))

          if (savedSessionId) {
            try {
              const savedSession = await getExamSession(savedSessionId)

              if (!isMounted) return

              setSession(savedSession)
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
  const correctAnswers = session?.responses.filter((response) => response.isCorrect).length ?? 0
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
    setSession(null)
    setFeedback(null)
  }

  async function handleStartExam() {
    if (!auth?.user.studentId || mode !== 'api') return

    setIsBusy(true)
    setFeedback(null)
    setSelectedOption(null)

    try {
      const newSession = await startExam(auth.user.studentId, maxQuestions, policy)

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
      setConnectionError(null)
    } catch (error) {
      setConnectionError(getErrorMessage(error))
    } finally {
      setIsBusy(false)
    }
  }

  function handleReset() {
    setSession(null)
    setFeedback(null)
    setSelectedOption(null)

    if (auth) {
      window.localStorage.removeItem(sessionStorageKey(auth.user.id))
    }
  }

  async function refreshTeacherData() {
    const [questionsFromApi, resultsFromApi] = await Promise.all([
      listQuestions(),
      listExamResults(),
    ])

    setQuestions(questionsFromApi.filter((question) => question.isActive))
    setResults(resultsFromApi)
  }

  if (!auth) {
    return (
      <main className="app-shell">
        <Header mode={mode} />
        <LoginScreen onAuthenticated={commitAuth} />
      </main>
    )
  }

  return (
    <main className="app-shell">
      <Header mode={mode} userName={auth.user.fullName} onLogout={handleLogout} />

      {auth.user.role === 'Teacher' ? (
        <TeacherDashboard
          questions={questions}
          results={results}
          difficultyCounts={difficultyCounts}
          isBusy={isBusy}
          connectionError={connectionError}
          onBusyChange={setIsBusy}
          onError={setConnectionError}
          onRefresh={refreshTeacherData}
        />
      ) : (
        <StudentDashboard
          userName={auth.user.fullName}
          session={session}
          currentQuestion={currentQuestion}
          progress={progress}
          correctAnswers={correctAnswers}
          selectedOption={selectedOption}
          feedback={feedback}
          policy={policy}
          maxQuestions={maxQuestions}
          isBusy={isBusy}
          connectionError={connectionError}
          onPolicyChange={setPolicy}
          onMaxQuestionsChange={setMaxQuestions}
          onStart={handleStartExam}
          onAnswer={handleAnswer}
          onReset={handleReset}
        />
      )}
    </main>
  )
}

function Header({
  mode,
  userName,
  onLogout,
}: {
  mode: RuntimeMode
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
        <RuntimeBadge mode={mode} />
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
  difficultyCounts,
  isBusy,
  connectionError,
  onBusyChange,
  onError,
  onRefresh,
}: {
  questions: QuestionResponse[]
  results: ExamResultSummaryResponse[]
  difficultyCounts: Record<DifficultyLevel, number>
  isBusy: boolean
  connectionError: string | null
  onBusyChange: (isBusy: boolean) => void
  onError: (message: string | null) => void
  onRefresh: () => Promise<void>
}) {
  const [topic, setTopic] = useState('Programacion')
  const [text, setText] = useState('')
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('Medium')
  const [options, setOptions] = useState(['', '', '', ''])
  const [correctIndex, setCorrectIndex] = useState(0)

  async function submitQuestion() {
    onBusyChange(true)
    onError(null)

    try {
      await createQuestion({
        topic,
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
          <Metric label="Sesiones" value={`${results.length}`} />
          <Metric
            label="Promedio"
            value={`${averageScore(results)}%`}
          />
        </div>
        <div className="difficulty-row horizontal">
          <DifficultyCount difficulty="Easy" count={difficultyCounts.Easy} />
          <DifficultyCount difficulty="Medium" count={difficultyCounts.Medium} />
          <DifficultyCount difficulty="Hard" count={difficultyCounts.Hard} />
        </div>
      </section>

      <section className="teacher-grid">
        <section className="panel">
          <PanelTitle icon={<Plus aria-hidden="true" />} title="Nueva pregunta" />
          <label>
            Tema
            <input value={topic} onChange={(event) => setTopic(event.target.value)} />
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
          <div className="table-list">
            {questions.map((question) => (
              <div className="table-row" key={question.id}>
                <div>
                  <DifficultyBadge difficulty={question.difficulty} />
                  <strong>{question.topic}</strong>
                  <span>{question.text}</span>
                </div>
                <button type="button" className="icon-button" onClick={() => removeQuestion(question.id)}>
                  <Trash2 aria-hidden="true" />
                </button>
              </div>
            ))}
          </div>
        </section>
      </section>

      <section className="panel">
        <PanelTitle icon={<ClipboardList aria-hidden="true" />} title="Resultados de examenes" />
        <div className="results-table">
          {results.length === 0 ? (
            <p className="muted">Todavia no existen examenes completados o en curso.</p>
          ) : (
            results.map((result) => (
              <div className="result-row" key={result.id}>
                <strong>{result.studentName}</strong>
                <span>{result.status === 'Completed' ? 'Finalizado' : 'En curso'}</span>
                <span>{policyText[result.policy]}</span>
                <span>
                  {result.answeredQuestions}/{result.maxQuestions}
                </span>
                <strong>{result.scorePercentage}%</strong>
              </div>
            ))
          )}
        </div>
      </section>
    </section>
  )
}

function StudentDashboard({
  userName,
  session,
  currentQuestion,
  progress,
  correctAnswers,
  selectedOption,
  feedback,
  policy,
  maxQuestions,
  isBusy,
  connectionError,
  onPolicyChange,
  onMaxQuestionsChange,
  onStart,
  onAnswer,
  onReset,
}: {
  userName: string
  session: ExamSessionResponse | null
  currentQuestion: ExamSessionResponse['currentQuestion']
  progress: number
  correctAnswers: number
  selectedOption: number | null
  feedback: AnswerResultResponse | null
  policy: DifficultyPolicy
  maxQuestions: number
  isBusy: boolean
  connectionError: string | null
  onPolicyChange: (policy: DifficultyPolicy) => void
  onMaxQuestionsChange: (maxQuestions: number) => void
  onStart: () => void
  onAnswer: (optionOrder: number) => void
  onReset: () => void
}) {
  return (
    <section className="workspace">
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
          <button type="button" className="side-nav-item active">
            <GraduationCap aria-hidden="true" />
            Estudiante
          </button>
          <button type="button" className="side-nav-item">
            <Settings2 aria-hidden="true" />
            Ajustes avanzados
          </button>
        </nav>

        <section className="panel settings-panel">
          <PanelTitle icon={<Settings2 aria-hidden="true" />} title="Configuracion" />
          <div className="segmented" role="group" aria-label="Politica de dificultad">
            {(['Balanced', 'Conservative'] as DifficultyPolicy[]).map((item) => (
              <button
                type="button"
                key={item}
                className={policy === item ? 'active' : ''}
                onClick={() => onPolicyChange(item)}
              >
                {policyText[item]}
              </button>
            ))}
          </div>
          <label>
            Preguntas
            <input
              type="number"
              min="3"
              max="10"
              value={maxQuestions}
              onChange={(event) => onMaxQuestionsChange(Number(event.target.value))}
            />
          </label>
          <button type="button" className="primary-button" onClick={onStart} disabled={isBusy}>
            <Play aria-hidden="true" />
            Iniciar
          </button>
        </section>

        <section className="sidebar-footer">
          <button type="button">
            <HelpCircle aria-hidden="true" />
            Ayuda
          </button>
          <button type="button" onClick={onReset}>
            <RotateCcw aria-hidden="true" />
            Reiniciar
          </button>
        </section>
      </aside>

      <section className="exam-surface">
        <div className="exam-header">
          <div>
            <span className="eyebrow">Sesion activa</span>
            <h1>{currentQuestion ? currentQuestion.topic : 'Examen adaptativo'}</h1>
          </div>
          <button type="button" className="icon-button" onClick={onReset} aria-label="Reiniciar examen">
            <RotateCcw aria-hidden="true" />
          </button>
        </div>

        <div className="progress-track" aria-label="Progreso de respuestas">
          <span style={{ width: `${progress}%` }} />
        </div>

        {connectionError && (
          <section className="connection-alert">
            <WifiOff aria-hidden="true" />
            <div>
              <strong>No se pudo conectar con la API local.</strong>
              <span>{connectionError}</span>
            </div>
          </section>
        )}

        {currentQuestion ? (
          <article className="question-panel">
            <div className="question-meta">
              <DifficultyBadge difficulty={currentQuestion.difficulty} />
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
            <h2>{session?.status === 'Completed' ? 'Examen finalizado' : 'Listo para iniciar'}</h2>
            <p>
              {session?.status === 'Completed'
                ? 'Has completado todas las preguntas del examen adaptativo.'
                : userName}
            </p>
            <div className="system-visual" aria-hidden="true">
              <span />
              <span />
              <span />
              <span />
            </div>
          </article>
        )}

        {feedback && (
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
          <Metric label="Correctas" value={`${correctAnswers}/${session?.answeredQuestions ?? 0}`} />
          <Metric label="Politica" value={policyText[session?.policy ?? policy]} />
        </section>

        <section className="panel privacy-panel">
          <PanelTitle icon={<ShieldCheck aria-hidden="true" />} title="Privacidad" />
          <p className="muted">
            El estudiante solo recibe la pregunta actual. Las respuestas correctas y el banco completo quedan en el panel docente.
          </p>
        </section>

        <section className="panel analysis-card">
          <div className="analysis-visual" aria-hidden="true">
            <LockKeyhole />
            <span />
            <span />
            <span />
          </div>
          <p className="muted">Analisis adaptativo en tiempo real habilitado.</p>
        </section>
      </aside>
    </section>
  )
}

function RuntimeBadge({ mode }: { mode: RuntimeMode }) {
  if (mode === 'login') {
    return (
      <span className="runtime-badge">
        <UserRoundCheck aria-hidden="true" />
        Login
      </span>
    )
  }

  if (mode === 'connecting') {
    return (
      <span className="runtime-badge">
        <Database aria-hidden="true" />
        Conectando
      </span>
    )
  }

  if (mode === 'api') {
    return (
      <span className="runtime-badge online">
        <Database aria-hidden="true" />
        API
      </span>
    )
  }

  return (
    <span className="runtime-badge offline">
      <WifiOff aria-hidden="true" />
      Sin API
    </span>
  )
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

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.name === 'AbortError'
      ? 'La API no respondio a tiempo. Verifica que el backend este ejecutandose en http://127.0.0.1:5116.'
      : error.message
  }

  return 'Verifica que PostgreSQL y ASP.NET Core esten ejecutandose.'
}

export default App
