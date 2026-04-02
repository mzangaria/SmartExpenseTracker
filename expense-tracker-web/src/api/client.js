const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5134'
const JSON_CONTENT_TYPE = 'application/json'

function buildHeaders(token, hasBody = false) {
  const headers = {}

  if (hasBody) {
    headers['Content-Type'] = 'application/json'
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  return headers
}

export async function apiRequest(path, { method = 'GET', token, body } = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers: buildHeaders(token, body !== undefined),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (response.status === 204) {
    return null
  }

  const text = await response.text()
  const contentType = response.headers.get('content-type') ?? ''
  const isJson = contentType.toLowerCase().includes(JSON_CONTENT_TYPE)
  let payload = null

  if (text && isJson) {
    try {
      payload = JSON.parse(text)
    } catch {
      payload = null
    }
  }

  if (!response.ok) {
    const fallbackMessage = text && !isJson ? text : response.statusText || 'Request failed'
    const error = new Error(payload?.message ?? fallbackMessage)
    error.status = response.status
    error.payload = payload
    error.rawBody = text || null
    throw error
  }

  return isJson ? payload : text || null
}
