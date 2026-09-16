"use client";

import { useEffect, useMemo, useState } from "react";
import { customers, products, sales } from "@/lib/api";
import type { Customer, Product } from "@/lib/types";
import {
  Search,
  Plus,
  Trash2,
  Minus,
  Package,
  ShoppingCart,
} from "@/components/Icons";

type Cart = { product: Product; qty: number };

export default function Checkout() {
  const [items, setItems] = useState<Product[]>([]),
    [custs, setCusts] = useState<Customer[]>([]),
    [q, setQ] = useState(""),
    [cart, setCart] = useState<Cart[]>([]),
    [customerId, setCustomerId] = useState(0),
    [paid, setPaid] = useState(0),
    [method, setMethod] = useState("Cash"),
    [discount, setDiscount] = useState(0),
    [busy, setBusy] = useState(false);

  useEffect(() => {
    products
      .list({ activeOnly: true, pageSize: 100 })
      .then((r) => setItems(r.items))
      .catch((e) => alert(e.message));
    customers
      .list()
      .then((r) => setCusts(r.items))
      .catch((e) => alert(e.message));
  }, []);

  const shown = items.filter((p) =>
    `${p.name} ${p.sku}`.toLowerCase().includes(q.toLowerCase()),
  );

  const subtotal = useMemo(
    () => cart.reduce((a, x) => a + x.qty * x.product.price, 0),
    [cart],
  );

  const total = Math.max(0, subtotal - discount);

  const change = Math.max(0, paid - total);

  function add(p: Product) {
    setCart((c) => {
      const old = c.find((x) => x.product.id === p.id);
      if (old) {
        if (old.qty >= p.stock) return c;
        return c.map((x) =>
          x.product.id === p.id ? { ...x, qty: x.qty + 1 } : x,
        );
      }
      return [...c, { product: p, qty: 1 }];
    });
  }

  function changeQty(id: number, d: number) {
    setCart((c) =>
      c
        .map((x) =>
          x.product.id === id
            ? { ...x, qty: Math.max(0, Math.min(x.product.stock, x.qty + d)) }
            : x,
        )
        .filter((x) => x.qty > 0),
    );
  }

  async function confirm() {
    if (!cart.length) return alert("Cart is empty");
    if (paid < total) return alert("Paid amount is less than total");
    setBusy(true);
    try {
      await sales.create({
        customerId: customerId || null,
        paymentMethod: method,
        discount,
        paidAmount: paid,
        items: cart.map((x) => ({
          productId: x.product.id,
          quantity: x.qty,
          unitPrice: x.product.price,
        })),
      });
      alert("Sale completed successfully");
      setCart([]);
      setPaid(0);
      setDiscount(0);
      const r = await products.list({ activeOnly: true, pageSize: 100 });
      setItems(r.items);
    } catch (e) {
      alert(e instanceof Error ? e.message : "Sale failed");
    } finally {
      setBusy(false);
    }
  }
  
  return (
    <div className="grid gap-5 lg:grid-cols-[1fr_420px]">
      <section className="space-y-4">
        <h1 className="text-2xl font-bold">Checkout</h1>
        <div className="card p-4">
          <div className="relative">
            <input
              className="input pl-9"
              placeholder="Search products"
              value={q}
              onChange={(e) => setQ(e.target.value)}
            />
          </div>
          <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {shown.map((p) => (
              <button
                key={p.id}
                disabled={p.stock <= 0}
                onClick={() => add(p)}
                className="rounded-xl border p-4 text-left hover:border-blue-500 disabled:opacity-40"
              >
                <div className="font-semibold">{p.name}</div>
                <div className="text-xs text-slate-500">{p.sku}</div>
                <div className="mt-2 flex justify-between">
                  <b>{p.price.toFixed(2)} DZD</b>
                  <span className="text-xs">Stock: {p.stock}</span>
                </div>
              </button>
            ))}
          </div>
        </div>
      </section>
      <aside className="card flex flex-col p-5 h-fit lg:sticky lg:top-5 shadow-sm border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950">
        <div className="flex items-center justify-between border-b pb-3 border-slate-100 dark:border-slate-800">
          <h2 className="text-lg font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
            <ShoppingCart size={19} className="text-blue-600" />
            Cart Basket
          </h2>
          {cart.length > 0 && (
            <span className="badge bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300 font-bold px-2.5 py-0.5">
              {cart.reduce((acc, item) => acc + item.qty, 0)} items
            </span>
          )}
        </div>

        <div className="mt-4 space-y-3 max-h-[320px] overflow-y-auto pr-1 scrollbar-thin">
          {cart.map((x) => (
            <div
              key={x.product.id}
              className="group rounded-xl border border-slate-100 bg-slate-50/50 p-3 transition hover:border-slate-200 dark:border-slate-800 dark:bg-slate-900/40 dark:hover:border-slate-700"
            >
              <div className="flex items-start justify-between gap-2">
                <div className="flex flex-col">
                  <span className="font-semibold text-sm text-slate-800 dark:text-slate-200 leading-tight">
                    {x.product.name}
                  </span>
                  <span className="text-xs text-slate-400 mt-1">
                    {x.product.price.toFixed(2)} DZD each
                  </span>
                </div>
                <span className="font-bold text-sm text-slate-900 dark:text-slate-100 whitespace-nowrap">
                  {(x.qty * x.product.price).toFixed(2)}
                </span>
              </div>

              <div className="mt-3 flex items-center gap-2 border-t border-slate-100 pt-2 dark:border-slate-800">
                <div className="flex items-center rounded-lg border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900 overflow-hidden h-8">
                  <button
                    className="px-2.5 text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-800 transition"
                    onClick={() => changeQty(x.product.id, -1)}
                  >
                    <Minus size={12} />
                  </button>
                  <span className="px-2 text-sm font-bold min-w-8 text-center text-slate-800 dark:text-slate-200">
                    {x.qty}
                  </span>
                  <button
                    className="px-2.5 text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-800 transition"
                    onClick={() => changeQty(x.product.id, 1)}
                  >
                    <Plus size={12} />
                  </button>
                </div>

                <button
                  className="btn-danger ml-auto h-8 w-8 !p-0 rounded-lg flex items-center justify-center opacity-80 hover:opacity-100 transition"
                  onClick={() =>
                    setCart((c) =>
                      c.filter((y) => y.product.id !== x.product.id),
                    )
                  }
                >
                  <Trash2 size={13} />
                </button>
              </div>
            </div>
          ))}

          {!cart.length && (
            <div className="py-8 text-center flex flex-col items-center justify-center text-slate-400 dark:text-slate-600">
              <Package size={26} className="mb-2 stroke-1" />
              <p className="text-xs">No items added to current basket.</p>
            </div>
          )}
        </div>

        <div className="mt-5 pt-4 border-t border-slate-100 dark:border-slate-800 space-y-3.5">
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-500 dark:text-slate-400">
              Customer
            </label>
            <select
              className="input h-10"
              value={customerId}
              onChange={(e) => setCustomerId(Number(e.target.value))}
            >
              <option value={0}>Walk-in customer</option>
              {custs.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.fullName} · {c.phone}
                </option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs font-semibold text-slate-500 dark:text-slate-400">
                Payment Method
              </label>
              <select
                className="input h-10"
                value={method}
                onChange={(e) => setMethod(e.target.value)}
              >
                <option>Cash</option>
                <option>Card</option>
                <option>IZI Pay</option>
              </select>
            </div>

            <div>
              <label className="mb-1 block text-xs font-semibold text-slate-500 dark:text-slate-400">
                Discount
              </label>
              <div className="relative">
                <input
                  className="input h-10 pr-10 font-medium"
                  type="number"
                  min="0"
                  placeholder="0.00"
                  value={discount || ""}
                  onChange={(e) => setDiscount(Number(e.target.value))}
                />
                <span className="absolute right-3 top-2.5 text-xs font-bold text-slate-400">
                  DZD
                </span>
              </div>
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-500 dark:text-slate-400">
              Paid Amount (Tendered)
            </label>
            <div className="relative">
              <input
                className={`input h-11 pr-12 text-base font-bold transition-colors ${paid >= total && cart.length ? "focus:border-emerald-500 border-emerald-200 bg-emerald-50/10 dark:border-emerald-900/30" : ""}`}
                type="number"
                min="0"
                placeholder="0.00"
                value={paid || ""}
                onChange={(e) => setPaid(Number(e.target.value))}
              />
              <span className="absolute right-3 top-3 text-xs font-bold text-slate-400">
                DZD
              </span>
            </div>
          </div>
        </div>

        <div className="mt-5 space-y-2.5 rounded-xl bg-slate-50 p-4 dark:bg-slate-900/60 text-sm border border-slate-100 dark:border-slate-900">
          <div className="flex justify-between text-slate-500 dark:text-slate-400">
            <span>Subtotal</span>
            <span className="font-semibold">{subtotal.toFixed(2)} DZD</span>
          </div>
          <div className="flex justify-between text-red-600 dark:text-red-400">
            <span>Discount</span>
            <span className="font-semibold">-{discount.toFixed(2)} DZD</span>
          </div>
          <div className="flex justify-between border-t border-dashed border-slate-200 dark:border-slate-800 pt-2.5 text-base font-black text-slate-900 dark:text-slate-50">
            <span>Total to Pay</span>
            <span className="text-lg text-blue-600 dark:text-blue-400">
              {total.toFixed(2)} DZD
            </span>
          </div>
          <div className="flex justify-between border-t border-slate-200/60 dark:border-slate-800 pt-2 text-xs font-semibold text-slate-500 dark:text-slate-400">
            <span>Change Return</span>
            <span
              className={`font-bold ${change > 0 ? "text-amber-600 dark:text-amber-400 text-sm" : ""}`}
            >
              {change.toFixed(2)} DZD
            </span>
          </div>
        </div>

        <button
          className={`btn-primary mt-5 w-full h-11 text-base font-bold shadow-sm transition-transform active:scale-[0.99] ${paid < total && cart.length ? "!bg-slate-200 dark:!bg-slate-800 !text-slate-400 dark:!text-slate-500 cursor-not-allowed border-none shadow-none" : ""}`}
          disabled={busy || !cart.length || paid < total}
          onClick={confirm}
        >
          {busy ? (
            <span className="flex items-center justify-center gap-2">
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
              Processing...
            </span>
          ) : (
            "Confirm Sale"
          )}
        </button>
      </aside>
    </div>
  );
}
