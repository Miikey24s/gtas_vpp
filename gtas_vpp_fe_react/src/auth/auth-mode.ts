export const cookieSessionEnabled =
  import.meta.env.VITE_AUTH_MODE?.toLowerCase() === 'cookie' ||
  import.meta.env.PROD

export const antiforgeryCookieName = 'XSRF-TOKEN'
export const antiforgeryHeaderName = 'X-XSRF-TOKEN'

export function isCookieSessionEnabled() {
  return cookieSessionEnabled
}

export function readAntiforgeryToken() {
  const cookie = document.cookie
    .split('; ')
    .find((item) => item.startsWith(`${antiforgeryCookieName}=`))
  if (!cookie) return undefined
  return decodeURIComponent(cookie.slice(antiforgeryCookieName.length + 1))
}
