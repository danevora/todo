import api from "./client";

export type TaskStatus = "Todo" | "InProgress" | "Done";
export type Priority = "Low" | "Medium" | "High";

export interface Task {
  id: number;
  title: string;
  notes: string | null;
  status: TaskStatus;
  priority: Priority;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface TaskFilters {
  page?: number;
  pageSize?: number;
  status?: TaskStatus | "";
  priority?: Priority | "";
  sortBy?: string;
  sortDesc?: boolean;
}

export interface CreateTaskPayload {
  title: string;
  notes?: string;
  status?: TaskStatus;
  priority?: Priority;
  dueDate?: string | null;
}

export interface PatchTaskPayload {
  title?: string;
  notes?: string;
  status?: TaskStatus;
  priority?: Priority;
  dueDate?: string | null;
}

export const tasksApi = {
  getAll: (filters: TaskFilters = {}) =>
    api.get<PagedResult<Task>>("/tasks", { params: filters }).then((r) => r.data),

  getById: (id: number) =>
    api.get<Task>(`/tasks/${id}`).then((r) => r.data),

  create: (payload: CreateTaskPayload) =>
    api.post<Task>("/tasks", payload).then((r) => r.data),

  patch: (id: number, payload: PatchTaskPayload) =>
    api.patch<Task>(`/tasks/${id}`, payload).then((r) => r.data),

  delete: (id: number) =>
    api.delete(`/tasks/${id}`),
};
