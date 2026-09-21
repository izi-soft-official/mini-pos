"use client";

import { FormEvent, useState } from "react";
import { useApp } from "@/components/Providers";
import { LockKeyhole } from "@/components/Icons";

export default function Login() {
  const { login } = useApp();
  const [u, setU] = useState("admin");
  const [p, setP] = useState("admin123");
  const [err, setErr] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setErr("");
    setBusy(true);
    try {
      await login(u, p);
      location.href = "/";
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Login failed");
    } finally {
      setBusy(false);
    }
  }
  
  return (
    <div className="min-h-screen grid place-items-center bg-slate-100 p-4 dark:bg-slate-900">
      <form onSubmit={submit} className="card w-full max-w-md p-7 shadow-xl">
        <div className="mb-7 text-center">
          <div className="mx-auto mb-3 grid h-12 w-12 place-items-center rounded-xl bg-blue-600 text-white">
            <LockKeyhole />
          </div>
          <h1 className="text-2xl font-black">
            Mini<span className="text-blue-600">POS</span>
          </h1>
          <p className="text-sm text-slate-500">Sign in to your account</p>
        </div>
        {err && (
          <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">
            {err}
          </div>
        )}
        <label className="mb-1 block text-sm font-medium">Username</label>
        <input
          className="input mb-4"
          value={u}
          onChange={(e) => setU(e.target.value)}
        />
        <label className="mb-1 block text-sm font-medium">Password</label>
        <input
          type="password"
          className="input mb-5"
          value={p}
          onChange={(e) => setP(e.target.value)}
        />
        <button className="btn-primary w-full" disabled={busy}>
          {busy ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </div>
  );
}
