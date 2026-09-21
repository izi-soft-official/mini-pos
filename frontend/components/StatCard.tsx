export default function StatCard({
  title,
  value,
  icon,
  note,
}: {
  title: string;
  value: string;
  icon: React.ReactNode;
  note?: string;
}) {
  
  return (
    <div className="card p-5">
      <div className="flex items-start justify-between">
        <div>
          <div className="text-sm text-slate-500">{title}</div>
          <div className="mt-2 text-2xl font-bold">{value}</div>
          {note && <div className="mt-1 text-xs text-slate-500">{note}</div>}
        </div>
        <div className="rounded-xl bg-blue-50 p-3 text-blue-600 dark:bg-blue-950/50 dark:text-blue-300">
          {icon}
        </div>
      </div>
    </div>
  );
}
