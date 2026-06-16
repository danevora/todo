import type { Task } from "../api/tasks";

const priorityColor: Record<string, string> = {
  Low: "#6b7280",
  Medium: "#d97706",
  High: "#dc2626",
};

const statusBadge: Record<string, string> = {
  Todo: "badge-todo",
  InProgress: "badge-inprogress",
  Done: "badge-done",
};

// Due dates are a date-only concept. Format from the YYYY-MM-DD part so the
// calendar day never shifts due to UTC parsing/timezone conversion.
function formatDueDate(iso: string): string {
  const [y, m, d] = iso.slice(0, 10).split("-").map(Number);
  return new Date(y, m - 1, d).toLocaleDateString();
}

interface Props {
  task: Task;
  busy?: boolean;
  onEdit: (task: Task) => void;
  onDelete: (id: number) => void;
  onStatusChange: (id: number, status: Task["status"]) => void;
}

export default function TaskCard({ task, busy, onEdit, onDelete, onStatusChange }: Props) {
  return (
    <div className="task-card">
      <div className="task-header">
        <span className={`badge ${statusBadge[task.status]}`}>{task.status}</span>
        <span className="priority-dot" style={{ color: priorityColor[task.priority] }}>
          ● {task.priority}
        </span>
      </div>
      <h3 className="task-title">{task.title}</h3>
      {task.notes && <p className="task-notes">{task.notes}</p>}
      {task.dueDate && (
        <p className="task-due">Due: {formatDueDate(task.dueDate)}</p>
      )}
      <div className="task-actions">
        <select
          value={task.status}
          disabled={busy}
          onChange={(e) => onStatusChange(task.id, e.target.value as Task["status"])}
        >
          <option>Todo</option>
          <option>InProgress</option>
          <option>Done</option>
        </select>
        <button onClick={() => onEdit(task)} disabled={busy}>Edit</button>
        <button className="btn-danger" onClick={() => onDelete(task.id)} disabled={busy}>Delete</button>
      </div>
    </div>
  );
}
