"use client";

import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import Sidebar from "./Sidebar";
import { useApp } from "./Providers";
import { LogOut, Menu } from "./Icons";

export default function AppShell({ children }: { children: React.ReactNode }) {
  const path = usePathname();
  const { user, loading, logout, t } = useApp();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  useEffect(() => {
    if (!loading && !user && path !== "/login") location.href = "/login";
  }, [loading, user, path]);

  if (path === "/login") return <>{children}</>;

  if (loading || !user)
    return <div className="min-h-screen grid place-items-center">Loading…</div>;

  return (
    <div className="min-h-screen flex bg-slate-50 dark:bg-slate-900">
      <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      <main className="min-w-0 flex-1">
        <header className="h-16 border-b bg-white dark:border-slate-800 dark:bg-slate-950 px-5 flex items-center justify-between">
          <button
            className="btn-secondary px-2 md:hidden"
            onClick={() => setSidebarOpen(true)}
            aria-label={t.openMenu}
          >
            <Menu size={20} />
          </button>
          <div className="ml-auto flex items-center gap-4 text-sm">
            <span>
              {user.fullName} · {user.role}
            </span>
            <button
              className="btn border border-red-200 bg-white text-red-600 hover:bg-red-50 dark:border-red-900/30 dark:bg-slate-900 dark:text-red-400 dark:hover:bg-red-950/20 px-4 py-2 rounded-lg text-sm font-semibold transition inline-flex items-center gap-2"
              onClick={logout}
            >
              <LogOut size={16} className="text-red-500 dark:text-red-400" />
              {t.logout}
            </button>
          </div>
        </header>
        <div className="p-5 max-w-7xl mx-auto">{children}</div>
      </main>
    </div>
  );
}