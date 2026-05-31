export type DifficultyLevel = 'Easy' | 'Medium' | 'Hard'
export type DifficultyPolicy = 'Balanced' | 'Conservative'
export type SessionStatus = 'InProgress' | 'Completed'
export type UserRole = 'Student' | 'Teacher'

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
  topic: string
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
  topic: string
  text: string
  difficulty: DifficultyLevel
  options: PublicOptionResponse[]
}

export interface ExamResponseItem {
  questionId: string
  selectedOptionOrder: number
  isCorrect: boolean
  difficultyAtAnswer: DifficultyLevel
  answeredAt: string
}

export interface ExamSessionResponse {
  id: string
  studentId: string
  policy: DifficultyPolicy
  currentDifficulty: DifficultyLevel
  status: SessionStatus
  maxQuestions: number
  answeredQuestions: number
  scorePercentage: number
  startedAt: string
  completedAt: string | null
  currentQuestion: CurrentQuestionResponse | null
  responses: ExamResponseItem[]
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
  status: SessionStatus
  answeredQuestions: number
  maxQuestions: number
  scorePercentage: number
  startedAt: string
  completedAt: string | null
}
