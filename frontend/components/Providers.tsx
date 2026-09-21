"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { auth, clearAuth, settings } from "@/lib/api";
import type { User } from "@/lib/types";
import { getTranslation } from "@/lib/dictionaries";
import AiAssistant from "@/components/AiAssistant";

type Ctx = {
  user: User | null;
  loading: boolean;
  login: (u: string, p: string) => Promise<void>;
  logout: () => void;
  can: (p: string) => boolean;
  lang: string; //LANG STATE TO CONTEXT DEFINITION
  setLang: (l: string) => void; //SETLANG TO CONTEXT DEFINITION
  t: ReturnType<typeof getTranslation>; //TRANSLATION DICTIONARY MAP TYPE
};

const AppContext = createContext<Ctx | null>(null);

export function Providers({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [lang, setLang] = useState("en"); // STATE FOR ACTIVE APP LANGUAGE

  //DYNAMICALLY RETRIEVE DICTIONARY FROM CURRENT STATE KEY VALUE
  const t = getTranslation(lang);

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

  // Apply the stored theme on every app load, not just on /settings
  useEffect(() => {
    settings
      .get()
      .then((s) => {
        setLang(s.language || "en");
        document.documentElement.classList.toggle("dark", s.theme === "dark");
      })
      .catch(() => {
        // settings unavailable — leave default (light) theme
      });
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
      User: ["products", "checkout", "sales", "customers", "settings"],
    };
    return m[user.role]?.includes(p) ?? false;
  };

  return (
    <AppContext.Provider
      value={{ user, loading, login, logout, can, lang, setLang, t }}
    >
      {children}
      <AiAssistant />
    </AppContext.Provider>
  );
}

export function useApp() {
  const c = useContext(AppContext);
  if (!c) throw new Error("useApp must be used inside Providers");
  return c;
}
