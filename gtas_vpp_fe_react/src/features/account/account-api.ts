import { toApiRequestError } from '@/api/api-error'

type ApiResult = {
  data?: unknown
  error?: unknown
  response?: Response
}

export function requireApiSuccess(result: ApiResult) {
  if (!result.response?.ok) {
    throw toApiRequestError(result.error, result.response)
  }
  return result.data
}
