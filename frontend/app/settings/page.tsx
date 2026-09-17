"use client";

import { useEffect, useState } from "react";
import { settings } from "@/lib/api";
import type { Settings } from "@/lib/types";
import { useApp } from "@/components/Providers";

export default function SettingsPage() {
  const { user, t, setLang } = useApp(); //Access translation dictionary 't' and setLang handler
  const [s, setS] = useState<Settings>({
    language: "en",
    theme: "light",
    lowStockThreshold: 5,
  });
  const [err, setErr] = useState("");

  useEffect(() => {
    settings
      .get()
      .then((r) => {
        setS(r);
        setLang(r.language); //Update global language state on load
        document.documentElement.classList.toggle("dark", r.theme === "dark");
      })
      .catch((e) => setErr(e.message));
  }, []);

  async function save() {
    try {
      const r = await settings.update(s);
      setS(r);
      setLang(r.language); //Update global language state on save
      document.documentElement.classList.toggle("dark", r.theme === "dark");
      alert("Settings saved");
    } catch (e) {
      alert(e instanceof Error ? e.message : "Settings API unavailable");
    }
  }

  if (err)
    return (
      <div className="card p-8">
        <h1 className="text-xl font-bold">Settings API unavailable</h1>
        <p className="mt-2 text-sm text-slate-500">{err}</p>
      </div>
    );

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-bold">{t.settings}</h1>
      <div className="card max-w-xl p-6 space-y-5">
        <div>
          <label className="mb-1 block text-sm font-medium">{t.language}</label>
          <select
            className="input"
            value={s.language}
            onChange={(e) => setS({ ...s, language: e.target.value })}
          >
            <option value="en">English</option>
            <option value="fr">Français</option>
          </select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">{t.theme}</label>
          <select
            className="input"
            value={s.theme}
            onChange={(e) => setS({ ...s, theme: e.target.value })}
          >
            <option value="light">Light</option>
            <option value="dark">Dark</option>
          </select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium">
            {t.lowStockThreshold}
          </label>
          <input
            className="input"
            type="number"
            min="0"
            value={s.lowStockThreshold}
            onChange={(e) =>
              setS({ ...s, lowStockThreshold: Number(e.target.value) })
            }
          />
        </div>
        {user?.role === "Admin" && (
          <button className="btn-primary" onClick={save}>
            {t.saveSettings}
          </button>
        )}
      </div>
    </div>
  );
}
