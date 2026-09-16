"use client";

import { useEffect, useState } from "react";
import { sales, customers } from "@/lib/api";
import type { Sale, SaleListItem, Customer } from "@/lib/types";
import { Eye, RefreshCw } from "@/components/Icons";

export default function Sales() {
  const [items, setItems] = useState<SaleListItem[]>([]),
    [custs, setCusts] = useState<Customer[]>([]),
    [from, setFrom] = useState(""),
    [to, setTo] = useState(""),
    [customerId, setCustomerId] = useState(0),
    [selected, setSelected] = useState<Sale | null>(null),
    [err, setErr] = useState("");
  const [dateOrder, setDateOrder] = useState<"desc" | "asc">("desc");

  async function load() {
    try {
      setErr("");
      const [r, c] = await Promise.all([
        sales.list({
          from: from || undefined,
          to: to || undefined,
          customerId: customerId || undefined,
        }),
        customers.list(),
      ]);
      setItems(r.items);
      setCusts(c.items);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Load failed");
    }
  }

  useEffect(() => {
    load();
  }, [customerId]);

  async function detail(id: number) {
    try {
      setSelected(await sales.get(id));
    } catch (e) {
      alert(e instanceof Error ? e.message : "Failed");
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex justify-between">
        <h1 className="text-2xl font-bold">Sales History</h1>
        <button className="btn-secondary" onClick={load}>
          <RefreshCw size={16} /> Refresh
        </button>
      </div>
      <div className="card p-4 flex flex-wrap gap-3">
        <input
          className="input w-auto"
          type="date"
          value={from}
          onChange={(e) => setFrom(e.target.value)}
        />
        <input
          className="input w-auto"
          type="date"
          value={to}
          onChange={(e) => setTo(e.target.value)}
        />
        <select
          className="input w-auto"
          value={customerId}
          onChange={(e) => setCustomerId(Number(e.target.value))}
        >
          <option value={0}>All customers</option>
          {custs.map((c) => (
            <option key={c.id} value={c.id}>
              {c.fullName}
            </option>
          ))}
        </select>
      </div>
      {err && (
        <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {err}
        </div>
      )}
      <div className="card overflow-hidden">
        <table className="table">
          <thead>
            <tr>
              <th>Number</th>
              <th>Date</th>
              <th>Customer</th>
              <th>Items</th>
              <th>Total</th>
              <th>Payment</th>
              <th>Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {items.map((s) => (
              <tr key={s.id}>
                <td className="font-semibold">{s.number}</td>
                <td>{new Date(s.createdAt).toLocaleString()}</td>
                <td>{s.customerName || "Walk-in"}</td>
                <td>{s.itemCount}</td>
                <td>{s.total.toFixed(2)} DZD</td>
                <td>{s.paymentMethod}</td>
                <td>{s.status}</td>
                <td>
                  <button
                    className="btn-secondary px-3"
                    onClick={() => detail(s.id)}
                  >
                    <Eye size={15} />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {selected && (
        <SaleModal sale={selected} onClose={() => setSelected(null)} />
      )}
    </div>
  );
}

function SaleModal({ sale, onClose }: { sale: Sale; onClose: () => void }) {
  const isCompleted =
    sale.status.toLowerCase() === "completed" ||
    sale.status.toLowerCase() === "paid";
  const isRefunded =
    sale.status.toLowerCase() === "refunded" ||
    sale.status.toLowerCase() === "cancelled";

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4 dynamic-backdrop">
      <div className="card max-h-[90vh] w-full max-w-2xl overflow-hidden flex flex-col p-0 shadow-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950">
        <div className="p-6 border-b border-slate-100 dark:border-slate-800 flex items-start justify-between bg-slate-50/50 dark:bg-slate-900/20">
          <div className="space-y-1">
            <h2 className="text-2xl font-black text-slate-900 dark:text-slate-100 tracking-tight">
              {sale.number}
            </h2>
            <p className="text-xs text-slate-400 dark:text-slate-500">
              {new Date(sale.createdAt).toLocaleString()}
            </p>
          </div>
          <span
            className={`badge px-3 py-1 rounded-full text-xs font-bold ${
              isCompleted
                ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400"
                : isRefunded
                  ? "bg-red-100 text-red-700 dark:bg-red-950/40 dark:text-red-400"
                  : "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300"
            }`}
          >
            • {sale.status}
          </span>
        </div>

        <div className="px-6 py-4 grid grid-cols-2 gap-4 border-b border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-950 text-sm">
          <div className="rounded-xl bg-slate-50/60 p-3 border border-slate-100 dark:bg-slate-900/40 dark:border-slate-900">
            <span className="block text-xs font-semibold text-slate-400 dark:text-slate-500 uppercase tracking-wider mb-0.5">
              Customer Profile
            </span>
            <span className="font-bold text-slate-800 dark:text-slate-200">
              {sale.customerName || "Walk-in Customer"}
            </span>
          </div>
          <div className="rounded-xl bg-slate-50/60 p-3 border border-slate-100 dark:bg-slate-900/40 dark:border-slate-900">
            <span className="block text-xs font-semibold text-slate-400 dark:text-slate-500 uppercase tracking-wider mb-0.5">
              Assigned Cashier
            </span>
            <span className="font-bold text-slate-800 dark:text-slate-200">
              {sale.cashierName}
            </span>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-6 max-h-[40vh] scrollbar-thin">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="border-b border-slate-200 text-xs font-bold uppercase tracking-wider text-slate-400 dark:border-slate-800">
                <th className="pb-3 text-left">Product / Item</th>
                <th className="pb-3 text-center w-16">Qty</th>
                <th className="pb-3 text-right w-24">Unit Price</th>
                <th className="pb-3 text-right w-28">Total</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-900">
              {sale.items.map((i) => (
                <tr
                  key={i.id}
                  className="group hover:bg-slate-50/30 dark:hover:bg-slate-900/10"
                >
                  <td className="py-3.5 pr-2 font-semibold text-slate-800 dark:text-slate-200">
                    {i.productName}
                  </td>
                  <td className="py-3.5 text-center font-bold text-slate-600 dark:text-slate-400 bg-slate-50/30 dark:bg-slate-900/20 rounded-md">
                    {i.quantity}
                  </td>
                  <td className="py-3.5 text-right font-medium text-slate-500 dark:text-slate-400">
                    {i.unitPrice.toFixed(2)}
                  </td>
                  <td className="py-3.5 text-right font-bold text-slate-900 dark:text-slate-100">
                    {i.lineTotal.toFixed(2)} DZD
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="p-6 bg-slate-50 dark:bg-slate-900/50 border-t border-slate-100 dark:border-slate-800">
          <div className="w-full space-y-2.5 text-sm">
            <div className="flex justify-between text-slate-500 dark:text-slate-400">
              <span>Subtotal</span>
              <span className="font-semibold">
                {sale.subtotal.toFixed(2)} DZD
              </span>
            </div>
            {sale.discount > 0 && (
              <div className="flex justify-between text-red-600 dark:text-red-400 font-medium">
                <span>Discount</span>
                <span>-{sale.discount.toFixed(2)} DZD</span>
              </div>
            )}
            <div className="flex justify-between border-t border-dashed border-slate-200 dark:border-slate-800 pt-2.5 text-base font-black text-slate-900 dark:text-slate-50">
              <span>Total</span>
              <span className="text-lg font-black text-blue-600 dark:text-blue-400">
                {sale.total.toFixed(2)} DZD
              </span>
            </div>
            <div className="flex justify-between border-t border-slate-200/60 dark:border-slate-800 pt-2 text-slate-500 dark:text-slate-400 text-xs">
              <span>Paid</span>
              <span className="font-medium text-slate-700 dark:text-slate-300">
                {sale.paidAmount.toFixed(2)} DZD
              </span>
            </div>
            <div className="flex justify-between text-xs text-slate-500 dark:text-slate-400">
              <span>Change</span>
              <span
                className={`font-bold ${sale.changeAmount > 0 ? "text-amber-600 dark:text-amber-400" : ""}`}
              >
                {sale.changeAmount.toFixed(2)} DZD
              </span>
            </div>
          </div>
        </div>

        <div className="px-6 py-4 border-t border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-950 flex justify-end gap-2">
          <button
            className="btn-secondary w-full sm:w-auto h-10 px-6 font-bold shadow-sm transition-transform active:scale-[0.99]"
            onClick={onClose}
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
