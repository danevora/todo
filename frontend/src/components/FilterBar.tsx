import type { TaskStatus, Priority } from "../api/tasks";

interface Props {
  status: TaskStatus | "";
  priority: Priority | "";
  sortBy: string;
  sortDesc: boolean;
  onChange: (updates: {
    status?: TaskStatus | "";
    priority?: Priority | "";
    sortBy?: string;
    sortDesc?: boolean;
  }) => void;
}

export default function FilterBar({ status, priority, sortBy, sortDesc, onChange }: Props) {
  return (
    <div className="filter-bar">
      <select value={status} onChange={(e) => onChange({ status: e.target.value as TaskStatus | "" })}>
        <option value="">All Statuses</option>
        <option>Todo</option>
        <option>InProgress</option>
        <option>Done</option>
      </select>
      <select value={priority} onChange={(e) => onChange({ priority: e.target.value as Priority | "" })}>
        <option value="">All Priorities</option>
        <option>Low</option>
        <option>Medium</option>
        <option>High</option>
      </select>
      <select value={sortBy} onChange={(e) => onChange({ sortBy: e.target.value })}>
        <option value="createdAt">Sort: Created</option>
        <option value="duedate">Sort: Due Date</option>
        <option value="priority">Sort: Priority</option>
        <option value="title">Sort: Title</option>
      </select>
      <button onClick={() => onChange({ sortDesc: !sortDesc })}>
        {sortDesc ? "↓ Desc" : "↑ Asc"}
      </button>
    </div>
  );
}
