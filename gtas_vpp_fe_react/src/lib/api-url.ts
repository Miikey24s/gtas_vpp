const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(
  /\/$/,
  '',
)

export const apiBaseUrl = configuredBaseUrl ?? ''

export function toApiUrl(path: string) {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${apiBaseUrl}${normalizedPath}`
}
