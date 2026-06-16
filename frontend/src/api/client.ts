import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5120/api",
});

/**
 * Turn any thrown request error into a human-readable message.
 * The API returns plain-text validation messages (e.g. "Title is required."),
 * so prefer the response body; fall back to the network/error message.
 */
export function errorMessage(err: unknown, fallback = "Something went wrong."): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data;
    if (typeof data === "string" && data.trim()) return data;
    if (data && typeof data === "object" && "message" in data) {
      return String((data as { message: unknown }).message);
    }
    return err.message || fallback;
  }
  return fallback;
}

export default api;
