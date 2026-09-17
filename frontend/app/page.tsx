"use client";

import { useEffect, useState } from "react";
import { dashboard } from "@/lib/api";
import type { DashboardSummary, SalesByDay, TopProduct } from "@/lib/types";
import { useApp } from "@/components/Providers";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  AlertTriangle,
  RefreshCw,
  TrendingUp,
  Receipt,
  Wallet,
} from "@/components/Icons";

export default function Dashboard() {
  const { can, t } = useApp();
  const router = useRouter();
  const [s, setS] = useState<DashboardSummary | null>(null),
    [top, setTop] = useState<TopProduct[]>([]),
    [days, setDays] = useState<SalesByDay[]>([]),
    [err, setErr] = useState("");

  async function load() {
    try {
      setErr("");
      const now = new Date(),
        from = new Date(now);
      from.setDate(now.getDate() - 6);
      const f = from.toISOString().slice(0, 10),
        t = now.toISOString().slice(0, 10);
      const [a, b, c] = await Promise.all([
        dashboard.summary(),
        dashboard.topProducts(f, t),
        dashboard.salesByDay(f, t),
      ]);
      setS(a);
      setTop(b);
      setDays(c);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Dashboard endpoint unavailable");
    }
  }

  useEffect(() => {
    load();
  }, []);

  if (!can("dashboard"))
    return (
      <div className="card p-8">
        <h1 className="text-xl font-bold">{t.dashboardUnavailable}</h1>
        <p className="mt-2 text-sm text-slate-500">
          {t.cashierNoAccess}
        </p>
      </div>
    );

  if (err)
    return (
      <div className="card p-8">
        <h1 className="text-xl font-bold">{t.dashboardUnavailable}</h1>
        <p className="mt-2 text-sm text-slate-500">{err}</p>
        <button className="btn-secondary mt-4" onClick={load}>
          <RefreshCw size={16} /> {t.retry}
        </button>
      </div>
    );

    const todayStr = new Date().toLocaleDateString("sv-SE");
    
  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-bold">{t.dashboard}</h1>
      {!s ? (
        <div>{t.loading}</div>
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Link
              href={`/sales?from=${todayStr}&to=${todayStr}`}
              className="stat transition hover:ring-2 hover:ring-blue-400 cursor-pointer"
            >
              <Receipt className="text-blue-600" />
              <div className="mt-3 text-2xl font-black">{s.salesCount}</div>
              <div className="text-sm text-slate-500">{t.salesToday}</div>
            </Link>

            <div className="stat">
              <Wallet className="text-emerald-600" />
              <div className="mt-3 text-2xl font-black">
                {s.salesTotal.toFixed(2)} DZD
              </div>
              <div className="text-sm text-slate-500">{t.revenueToday}</div>
            </div>

            <div className="stat">
              <TrendingUp className="text-violet-600" />
              <div className="mt-3 text-2xl font-black">
                {s.averageSale.toFixed(2)} DZD
              </div>
              <div className="text-sm text-slate-500">{t.averageSale}</div>
            </div>

            <Link
              href="/products?lowStock=true"
              className="stat transition hover:ring-2 hover:ring-amber-400 cursor-pointer"
            >
              <AlertTriangle className="text-amber-600" />
              <div className="mt-3 text-2xl font-black">{s.lowStockCount}</div>
              <div className="text-sm text-slate-500">{t.lowStock}</div>
            </Link>
          </div>
          <div className="grid gap-5 lg:grid-cols-2">
            <section className="card p-5">
              <h2 className="font-bold">{t.salesByDay}</h2>
              <div className="mt-5 space-y-3">
                {days.map((d) => (
                  <Link
                    key={d.date}
                    href={`/sales?from=${d.date}&to=${d.date}`}
                    className="block group cursor-pointer rounded p-1 hover:bg-slate-50 dark:hover:bg-slate-800 transition"
                  >
                    <div className="mb-1 flex justify-between text-xs font-medium group-hover:text-blue-600">
                      <span>{d.date}</span>
                      <span>{d.total.toFixed(2)} DZD</span>
                    </div>
                    <div className="h-3 rounded-full bg-slate-100 dark:bg-slate-800">
                      <div
                        className="h-3 rounded-full bg-blue-600 transition-all group-hover:bg-blue-500"
                        style={{
                          width: `${Math.min(100, Math.max(2, (d.total / Math.max(1, ...days.map((x) => x.total))) * 100))}%`,
                        }}
                      />
                    </div>
                  </Link>
                ))}
              </div>
            </section>

            <section className="card p-5">
              <h2 className="font-bold">{t.topProducts}</h2>
              <div className="mt-4 space-y-3">
                {top.map((p, i) => (
                  <div
                    key={p.productId}
                    className="flex items-center justify-between border-b pb-3 last:border-0"
                  >
                    <div>
                      <span className="mr-2 text-slate-400">#{i + 1}</span>
                      <span className="font-semibold">{p.name}</span>
                      <div className="text-xs text-slate-500">
                        {p.sku} · {p.quantitySold} {t.sold}
                      </div>
                    </div>
                    <b>{p.total.toFixed(2)} DZD</b>
                  </div>
                ))}
              </div>
            </section>
          </div>
        </>
      )}
    </div>
  );
}
