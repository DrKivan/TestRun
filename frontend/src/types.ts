export type DifficultyLevel = 'Easy' | 'Medium' | 'Hard'
export type DifficultyPolicy = 'Balanced' | 'Conservative'
export type SessionStatus = 'InProgress' | 'Completed'
export type ExamSessionKind = 'Standard' | 'Reinforcement'
export type UserRole = 'Student' | 'Teacher'
export type LessonType = 'PreExam' | 'PostExam'
export type Topic = 'Matematica' | 'Programacion' | 'Ciencias'

export interface AuthUser {
  id: string
  fullName: string
  email: string
  role: UserRole
  studentId: string | null
}

export interface AuthResponse {
  token: string
  user: AuthUser
}

export interface Student {
  id: string
  fullName: string
  email: string
  createdAt: string
}

export interface OptionResponse {
  id: string
  order: number
  text: string
  isCorrect: boolean
}

export interface QuestionResponse {
  id: string
  topic: Topic
  competency: string
  text: string
  difficulty: DifficultyLevel
  isActive: boolean
  options: OptionResponse[]
}

export interface PublicOptionResponse {
  order: number
  text: string
}

export interface CurrentQuestionResponse {
  id: string
  topic: Topic
  competency: string
  text: string
  difficulty: DifficultyLevel
  options: PublicOptionResponse[]
}

export interface CompetencyDiagnosticResponse {
  topic: Topic
  competency: string
  answeredQuestions: number
  correctAnswers: number
  scorePercentage: number
  weightedScorePercentage: number
  highestDifficulty: DifficultyLevel
  level: 'Dominado' | 'Competente' | 'En desarrollo' | 'Reforzar' | 'Sin evaluar' | string
  confidence: string
  pattern: string
  evaluationSummary: string
  recommendation: string
}

export interface LessonResponse {
  id: string
  topic: Topic
  competency: string | null
  type: LessonType
  title: string
  content: string
  resourceUrl: string | null
  isActive: boolean
}

export interface ExamResponseItem {
  questionId: string
  selectedOptionOrder: number
  isCorrect: boolean
  difficultyAtAnswer: DifficultyLevel
  answeredAt: string
}

export interface ErrorReviewItemResponse {
  questionId: string
  topic: Topic
  questionText: string
  selectedOptionOrder: number
  selectedOptionText: string
  correctOptionOrder: number
  correctOptionText: string
  explanation: string
}

export interface ExamSessionResponse {
  id: string
  studentId: string
  policy: DifficultyPolicy
  kind: ExamSessionKind
  targetTopic: Topic | null
  targetCompetency: string | null
  currentDifficulty: DifficultyLevel
  status: SessionStatus
  maxQuestions: number
  answeredQuestions: number
  correctAnswers?: number
  scorePercentage: number
  startedAt: string
  completedAt: string | null
  currentQuestion: CurrentQuestionResponse | null
  responses: ExamResponseItem[]
  diagnostic?: CompetencyDiagnosticResponse[]
  errorReview?: ErrorReviewItemResponse[]
}

export interface AnswerResultResponse {
  isCorrect: boolean
  previousDifficulty: DifficultyLevel
  nextDifficulty: DifficultyLevel
  status: SessionStatus
  scorePercentage: number
  nextQuestion: CurrentQuestionResponse | null
  session: ExamSessionResponse
}

export interface ExamResultSummaryResponse {
  id: string
  studentId: string
  studentName: string
  policy: DifficultyPolicy
  kind: ExamSessionKind
  targetTopic: Topic | null
  targetCompetency: string | null
  status: SessionStatus
  answeredQuestions: number
  maxQuestions: number
  correctAnswers?: number
  scorePercentage: number
  startedAt: string
  completedAt: string | null
  diagnostic?: CompetencyDiagnosticResponse[]
}

export interface TopicAnalyticsResponse {
  topic: Topic
  answerCount: number
  incorrectCount: number
  errorPercentage: number
}

export interface QuestionAnalyticsResponse {
  questionId: string
  topic: Topic
  competency: string
  text: string
  difficulty: DifficultyLevel
  answerCount: number
  incorrectCount: number
  errorPercentage: number
}

export interface ExamAnalyticsResponse {
  topics: TopicAnalyticsResponse[]
  questions: QuestionAnalyticsResponse[]
}
