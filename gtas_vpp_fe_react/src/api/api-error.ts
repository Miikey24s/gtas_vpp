export class ApiRequestError extends Error {
  readonly status?: number
  readonly code?: string

  constructor(message: string, status?: number, code?: string) {
    super(message)
    this.name = 'ApiRequestError'
    this.status = status
    this.code = code
  }
}

function readString(value: unknown, keys: string[]): string | undefined {
  if (!value || typeof value !== 'object') return undefined

  const candidate = value as Record<string, unknown>
  for (const key of keys) {
    const result = candidate[key]
    if (typeof result === 'string' && result.trim()) {
      return result.trim()
    }
  }

  return undefined
}

export function toApiRequestError(
  error: unknown,
  response?: Response,
): ApiRequestError {
  return new ApiRequestError(
    readString(error, ['message', 'detail', 'title']) ??
      'The request could not be completed.',
    response?.status,
    readString(error, ['code', 'errorCode']),
  )
}

export function isApiRequestError(error: unknown): error is ApiRequestError {
  return error instanceof ApiRequestError
}
