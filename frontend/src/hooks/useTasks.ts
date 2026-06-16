import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { tasksApi } from "../api/tasks";
import type { TaskFilters, CreateTaskPayload, PatchTaskPayload } from "../api/tasks";

const TASKS_KEY = "tasks";

export function useTasks(filters: TaskFilters = {}) {
  return useQuery({
    queryKey: [TASKS_KEY, filters],
    queryFn: () => tasksApi.getAll(filters),
  });
}

export function useCreateTask() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateTaskPayload) => tasksApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [TASKS_KEY] }),
  });
}

export function usePatchTask() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: PatchTaskPayload }) =>
      tasksApi.patch(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [TASKS_KEY] }),
  });
}

export function useDeleteTask() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => tasksApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [TASKS_KEY] }),
  });
}
