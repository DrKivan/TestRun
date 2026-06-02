import type {
  AnswerResultResponse,
  AuthResponse,
  DifficultyPolicy,
  ExamSessionKind,
  ExamAnalyticsResponse,
  ExamResultSummaryResponse,
  ExamSessionResponse,
  LessonResponse,
  LessonType,
  OptionResponse,
  QuestionResponse,
  Topic,
} from './types'

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://127.0.0.1:5116'
const requestTimeoutMs = 2500
let authToken: string | null = null

export function setAuthToken(token: string | null) {
  authToken = token
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const controller = new AbortController()
  const timeoutId = window.setTimeout(() => controller.abort(), requestTimeoutMs)

  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(authToken ? { Authorization: `Bearer ${authToken}` } : {}),
      ...init?.headers,
    },
    signal: controller.signal,
    ...init,
  }).finally(() => window.clearTimeout(timeoutId))

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `HTTP ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export async function login(email: string, password: string) {
  return request<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}

export async function registerStudent(fullName: string, email: string, password: string) {
  return request<AuthResponse>('/api/auth/register-student', {
    method: 'POST',
    body: JSON.stringify({ fullName, email, password }),
  })
}

export async function listQuestions() {
  return request<QuestionResponse[]>('/api/questions')
}

export async function createQuestion(input: {
  topic: Topic
  competency: string
  text: string
  difficulty: string
  options: Array<Pick<OptionResponse, 'text' | 'isCorrect'>>
}) {
  return request<QuestionResponse>('/api/questions', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function listLessons() {
  return request<LessonResponse[]>('/api/lessons')
}

export async function createLesson(input: {
  topic: Topic
  competency: string | null
  type: LessonType
  title: string
  content: string
  resourceUrl: string | null
}) {
  return request<LessonResponse>('/api/lessons', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function deleteLesson(lessonId: string) {
  await request<void>(`/api/lessons/${lessonId}`, {
    method: 'DELETE',
  })
}

export async function deleteQuestion(questionId: string) {
  await request<void>(`/api/questions/${questionId}`, {
    method: 'DELETE',
  })
}

export async function listExamResults() {
  return request<ExamResultSummaryResponse[]>('/api/exam-sessions')
}

export async function getExamAnalytics() {
  return request<ExamAnalyticsResponse>('/api/exam-sessions/analytics')
}

export async function listStudentExamResults() {
  return request<ExamResultSummaryResponse[]>('/api/exam-sessions/mine')
}

export async function startExam(
  studentId: string,
  maxQuestions: number,
  policy: DifficultyPolicy,
  kind: ExamSessionKind,
  targetTopic?: Topic,
  targetCompetency?: string,
) {
  return request<ExamSessionResponse>('/api/exam-sessions', {
    method: 'POST',
    body: JSON.stringify({ studentId, maxQuestions, policy, kind, targetTopic, targetCompetency }),
  })
}

export async function getExamSession(sessionId: string) {
  return request<ExamSessionResponse>(`/api/exam-sessions/${sessionId}`)
}

export async function answerQuestion(
  sessionId: string,
  questionId: string,
  selectedOptionOrder: number,
) {
  return request<AnswerResultResponse>(`/api/exam-sessions/${sessionId}/answers`, {
    method: 'POST',
    body: JSON.stringify({ questionId, selectedOptionOrder }),
  })
}
