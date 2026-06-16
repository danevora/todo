import { useState } from "react";
import type { Task, TaskStatus, Priority, CreateTaskPayload } from "./api/tasks";
import { useTasks, useCreateTask, usePatchTask, useDeleteTask } from "./hooks/useTasks";
import { errorMessage } from "./api/client";
import TaskCard from "./components/TaskCard";
import TaskForm from "./components/TaskForm";
import FilterBar from "./components/FilterBar";
import "./App.css";

interface Filters {
  status: TaskStatus | "";
  priority: Priority | "";
  sortBy: string;
  sortDesc: boolean;
  page: number;
}

export default function App() {
  const [filters, setFilters] = useState<Filters>({
    status: "", priority: "", sortBy: "createdAt", sortDesc: true, page: 1,
  });
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Task | null>(null);
  const [banner, setBanner] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const { data, isLoading, error } = useTasks({
    ...filters,
    status: filters.status || undefined,
    priority: filters.priority || undefined,
    pageSize: 20,
  });

  const createTask = useCreateTask();
  const patchTask = usePatchTask();
  const deleteTask = useDeleteTask();

  function updateFilters(updates: Partial<Filters>) {
    setFilters((f) => ({ ...f, ...updates, page: updates.page ?? 1 }));
  }

  function handleNew() {
    setEditing(null);
    setFormError(null);
    setShowForm(true);
  }

  function handleEdit(task: Task) {
    setEditing(task);
    setFormError(null);
    setShowForm(true);
  }

  function handleClose() {
    setShowForm(false);
    setEditing(null);
    setFormError(null);
  }

  async function handleSubmit(payload: CreateTaskPayload) {
    try {
      if (editing) {
        await patchTask.mutateAsync({ id: editing.id, payload });
      } else {
        await createTask.mutateAsync(payload);
      }
      handleClose();
    } catch (err) {
      // Keep the modal open with the user's input intact and show why it failed.
      setFormError(errorMessage(err, "Could not save the task."));
    }
  }

  async function handleDelete(id: number) {
    if (!confirm("Delete this task?")) return;
    try {
      await deleteTask.mutateAsync(id);
    } catch (err) {
      setBanner(errorMessage(err, "Could not delete the task."));
    }
  }

  async function handleStatusChange(id: number, status: TaskStatus) {
    try {
      await patchTask.mutateAsync({ id, payload: { status } });
    } catch (err) {
      setBanner(errorMessage(err, "Could not update the task."));
    }
  }

  const totalPages = data ? Math.ceil(data.total / 20) : 1;

  return (
    <div className="app">
      <header className="app-header">
        <h1>Todo</h1>
        <button onClick={handleNew}>+ New Task</button>
      </header>

      {banner && (
        <div className="banner-error" role="alert">
          <span>{banner}</span>
          <button className="banner-dismiss" onClick={() => setBanner(null)} aria-label="Dismiss">×</button>
        </div>
      )}

      <FilterBar
        status={filters.status}
        priority={filters.priority}
        sortBy={filters.sortBy}
        sortDesc={filters.sortDesc}
        onChange={updateFilters}
      />

      {showForm && (
        <div className="modal-overlay" onClick={handleClose}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>{editing ? "Edit Task" : "New Task"}</h2>
            <TaskForm
              initial={editing ?? undefined}
              onSubmit={handleSubmit}
              onCancel={handleClose}
              loading={createTask.isPending || patchTask.isPending}
              serverError={formError}
            />
          </div>
        </div>
      )}

      {isLoading && <p className="status-msg">Loading…</p>}
      {error && <p className="status-msg error">Failed to load tasks.</p>}

      {data && data.items.length === 0 && (
        <p className="status-msg">No tasks yet. Create one!</p>
      )}

      <div className="task-grid">
        {data?.items.map((task) => (
          <TaskCard
            key={task.id}
            task={task}
            onEdit={handleEdit}
            onDelete={handleDelete}
            onStatusChange={handleStatusChange}
          />
        ))}
      </div>

      {totalPages > 1 && (
        <div className="pagination">
          <button disabled={filters.page <= 1} onClick={() => updateFilters({ page: filters.page - 1 })}>
            ←
          </button>
          <span>Page {filters.page} of {totalPages}</span>
          <button disabled={filters.page >= totalPages} onClick={() => updateFilters({ page: filters.page + 1 })}>
            →
          </button>
        </div>
      )}
    </div>
  );
}
