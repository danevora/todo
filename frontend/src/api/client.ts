import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5120/api",
});

/**
 * Turn any thrown request error into a human-readable message.
 * The API returns RFC-7807 ProblemDetails JSON ({ detail, title, ... }), so prefer
 * `detail`, then `title`; fall back to a plain-text body or the network error message.
 */
export function errorMessage(err: unknown, fallback = "Something went wrong."): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data;
    if (typeof data === "string" && data.trim()) return data;
    if (data && typeof data === "object") {
      const problem = data as { detail?: string; title?: string };
      if (problem.detail) return problem.detail;
      if (problem.title) return problem.title;
    }
    return err.message || fallback;
  }
  return fallback;
}

export default api;
