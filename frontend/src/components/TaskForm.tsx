import { useState } from "react";
import type { Task, TaskStatus, Priority, CreateTaskPayload } from "../api/tasks";

interface Props {
  initial?: Task;
  // The form always produces a complete payload (title included); the parent
  // sends it to create or patch as appropriate.
  onSubmit: (data: CreateTaskPayload) => void;
  onCancel: () => void;
  loading?: boolean;
  serverError?: string | null;
}

export default function TaskForm({ initial, onSubmit, onCancel, loading, serverError }: Props) {
  const [title, setTitle] = useState(initial?.title ?? "");
  const [notes, setNotes] = useState(initial?.notes ?? "");
  const [status, setStatus] = useState<TaskStatus>(initial?.status ?? "Todo");
  const [priority, setPriority] = useState<Priority>(initial?.priority ?? "Medium");
  const [dueDate, setDueDate] = useState(
    initial?.dueDate ? initial.dueDate.split("T")[0] : ""
  );
  const [error, setError] = useState("");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!title.trim()) { setError("Title is required."); return; }
    setError("");
    // Send null (not undefined) for empty notes/date so an edit can clear them.
    onSubmit({ title: title.trim(), notes: notes.trim() || null, status, priority, dueDate: dueDate || null });
  }

  return (
    <form onSubmit={handleSubmit} className="task-form">
      {(error || serverError) && <p className="form-error">{error || serverError}</p>}
      <label>
        Title *
        <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Task title" />
      </label>
      <label>
        Notes
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} placeholder="Optional notes" />
      </label>
      <div className="form-row">
        <label>
          Status
          <select value={status} onChange={(e) => setStatus(e.target.value as TaskStatus)}>
            <option>Todo</option>
            <option>InProgress</option>
            <option>Done</option>
          </select>
        </label>
        <label>
          Priority
          <select value={priority} onChange={(e) => setPriority(e.target.value as Priority)}>
            <option>Low</option>
            <option>Medium</option>
            <option>High</option>
          </select>
        </label>
        <label>
          Due Date
          <input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
        </label>
      </div>
      <div className="form-actions">
        <button type="button" onClick={onCancel}>Cancel</button>
        <button type="submit" disabled={loading}>{loading ? "Saving…" : "Save"}</button>
      </div>
    </form>
  );
}
