"use client";

import { login } from "@/lib/services";
import { useRouter } from "next/navigation";
import {setToken} from "@/lib/api";
import Button from "@/components/Button";
import { useEffect, useState } from "react";

export default function LoginForm() {
  const router = useRouter();
  const [form, setForm] = useState({ username: "", password: "" });
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [failedAttempts, setFailedAttempts] = useState(0);
  const [lockoutUntil, setLockoutUntil] = useState<number | null>(null);
  const [remainingSeconds, setRemainingSeconds] = useState(0);

  useEffect(() => {
    if (!lockoutUntil) {
      setRemainingSeconds(0);
      return;
    }

    const updateRemainingTime = () => {
      const seconds = Math.ceil((lockoutUntil - Date.now()) / 1000);

      if (seconds <= 0) {
        setLockoutUntil(null);
        setRemainingSeconds(0);
        setFailedAttempts(0);
        setError(null);
        return;
      }

      setRemainingSeconds(seconds);
    };

    updateRemainingTime();
    const timer = window.setInterval(updateRemainingTime, 1000);

    return () => window.clearInterval(timer);
  }, [lockoutUntil]);

  const isLockedOut = lockoutUntil !== null;
  const isDisabled = isLoading || isLockedOut;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (isDisabled) {
      return;
    }

    if (!form.username || !form.password) {
      setError("Username and password are required");
      return;
    }

    setIsLoading(true);
    setError(null);

    try{
      const result = await login(form);
      setToken(result.token);
      setFailedAttempts(0);
      router.push("/dashboard");
    }catch(error: unknown) {
      const nextFailedAttempts = failedAttempts + 1;
      setFailedAttempts(nextFailedAttempts);

      if (nextFailedAttempts >= 3) {
        setLockoutUntil(Date.now() + 30_000);
        setError("Too many failed attempts. Try again in 30 seconds.");
      } else {
        setError(error instanceof Error ? error.message : "Invalid username or password");
      }
    } finally {
      setIsLoading(false);
    }
  };

  const buttonLabel = isLockedOut
    ? `Locked (${remainingSeconds}s)`
    : isLoading
      ? "Signing in..."
      : "Sign in";

  return (
    <div className="grid min-h-full place-items-center">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm rounded-xl border border-line bg-surface p-8"
      >
        <div className="text-center">
          <h1 className="text-2xl font-bold text-white">mini-pos</h1>
          <p className="mt-1 text-sm text-muted">Sign in to continue</p>
        </div>

        <div className="mt-8 flex flex-col gap-4">
          <div>
            <label
              htmlFor="username"
              className="mb-1.5 block text-sm text-muted"
            >
              Username
            </label>
            <input
              id="username"
              value={form.username}
              disabled={isDisabled}
              onChange={(e) => setForm({ ...form, username: e.target.value })}
              className="w-full rounded-lg border border-line bg-surface-2 px-3 py-2 text-sm focus:border-mint focus:outline-none disabled:opacity-60"
            />
          </div>

          <div>
            <label
              htmlFor="password"
              className="mb-1.5 block text-sm text-muted"
            >
              Password
            </label>
            <input
              id="password"
              type="password"
              value={form.password}
              disabled={isDisabled}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              className="w-full rounded-lg border border-line bg-surface-2 px-3 py-2 text-sm focus:border-mint focus:outline-none disabled:opacity-60"
            />
          </div>
        </div>
        {error && (
          <p className="mt-4 rounded-lg border border-red-900 bg-red-950 px-3 py-2 text-sm text-red-300">
            {isLockedOut
              ? `Too many failed attempts. Try again in ${remainingSeconds}s.`
              : error}
          </p>
        )}

        <Button
          type="submit"
          disabled={isDisabled}
          fullWidth
          className="mt-6"
        >
          {buttonLabel}
        </Button>
      </form>
    </div>
  );
}
