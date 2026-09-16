"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { auth, clearAuth } from "@/lib/api";
import type { User } from "@/lib/types";

type Ctx = {
  user: User | null;
  loading: boolean;
  login: (u: string, p: string) => Promise<void>;
  logout: () => void;
  can: (p: string) => boolean;
};

const AppContext = createContext<Ctx | null>(null);

export function Providers({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem("mini-pos-token");
    if (!token) {
      setLoading(false);
      return;
    }
    auth
      .me()
      .then((u) => setUser(u))
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  const login = async (username: string, password: string) => {
    const r = await auth.login(username, password);
    localStorage.setItem("mini-pos-token", r.token);
    localStorage.setItem("mini-pos-user", JSON.stringify(r.user));
    setUser(r.user);
  };

  const logout = () => {
    clearAuth();
    setUser(null);
    if (typeof window !== "undefined") location.href = "/login";
  };

  const can = (p: string) => {
    if (!user) return false;
    const m: Record<string, string[]> = {
      Admin: [
        "dashboard",
        "products",
        "productWrite",
        "categories",
        "checkout",
        "sales",
        "customers",
        "settings",
        "users",
      ],
      Manager: [
        "dashboard",
        "products",
        "productWrite",
        "categories",
        "checkout",
        "sales",
        "customers",
        "settings",
      ],
      Cashier: ["products", "checkout", "salesOwn", "customers", "settings"],
    };
    return m[user.role]?.includes(p) ?? false;
  };

  return (
    <AppContext.Provider value={{ user, loading, login, logout, can }}>
      {children}
    </AppContext.Provider>
  );
}

export function useApp() {
  const c = useContext(AppContext);
  if (!c) throw new Error("useApp must be used inside Providers");
  return c;
}
