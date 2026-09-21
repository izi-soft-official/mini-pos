"use client";

import { useEffect, useState } from "react";
import { categories, products, settings } from "@/lib/api";
import type { Category, Product } from "@/lib/types";
import { useApp } from "@/components/Providers";
import { useSearchParams } from "next/navigation";
import {
  Plus,
  Pencil,
  Trash2,
  Search,
  RefreshCw,
  Tags,
} from "@/components/Icons";
import Link from "next/link";

const empty = {
  sku: "",
  name: "",
  categoryId: 0,
  price: 0,
  cost: 0,
  stock: 0,
  isActive: true,
};
export default function ProductsPage() {
  const { can, t } = useApp();
  const searchParams = useSearchParams();
  const [items, setItems] = useState<Product[]>([]),
    [cats, setCats] = useState<Category[]>([]),
    [q, setQ] = useState(""),
    [cat, setCat] = useState(0),
    [low, setLow] = useState(searchParams.get("lowStock") === "true"),
    [lowStockThreshold, setLowStockThreshold] = useState(5), // fallback until settings load
    [edit, setEdit] = useState<Product | null>(null),
    [add, setAdd] = useState(false),
    [busy, setBusy] = useState(false),
    [err, setErr] = useState("");

  async function load() {
    setBusy(true);
    try {
      const [r, c, st] = await Promise.all([
        products.list({
          search: q,
          categoryId: cat || undefined,
          activeOnly: false,
          pageSize: 100,
        }),
        categories.list(),
        settings.get(),
      ]);
      setItems(r.items);
      setCats(c);
      setLowStockThreshold(st.lowStockThreshold);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Failed to load");
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    load();
  }, [q, cat]);

  const shown = low ? items.filter((p) => p.stock <= lowStockThreshold) : items;

  async function save(form: typeof empty) {
    try {
      if (edit) await products.update(edit.id, form);
      else await products.create(form);
      setAdd(false);
      setEdit(null);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Save failed");
    }
  }

  async function remove(id: number) {
    if (!confirm("Delete this product?")) return;
    try {
      await products.remove(id);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Delete failed");
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap justify-between gap-3">
        <h1 className="text-2xl font-bold">{t.products}</h1>
        <div className="flex gap-2">
          <Link href="/categories" className="btn-secondary">
            <Tags size={17} /> {t.manageCategories}
          </Link>
          {can("productWrite") && (
            <button className="btn-primary" onClick={() => setAdd(true)}>
              <Plus size={17} /> {t.addProduct}
            </button>
          )}
        </div>
      </div>
      <div className="card p-4 flex flex-wrap gap-3">
        <div className="relative flex-1 min-w-56">
          <input
            className="input pl-9"
            placeholder={t.searchProductPlaceholder}
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />
        </div>
        <select
          className="input w-auto"
          value={cat}
          onChange={(e) => setCat(Number(e.target.value))}
        >
          <option value={0}>{t.allCategories}</option>
          {cats.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        <label className="flex items-center gap-2 px-2 text-sm">
          <input
            type="checkbox"
            checked={low}
            onChange={(e) => setLow(e.target.checked)}
          />{" "}
          {t.lowStock}
        </label>
        <button className="btn-secondary" onClick={load}>
          <RefreshCw size={16} />
        </button>
      </div>
      {err && (
        <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {err}
        </div>
      )}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>{t.sku}</th>
                <th>{t.name}</th>
                <th>{t.category}</th>
                <th>{t.price}</th>
                <th>{t.cost}</th>
                <th>{t.stock}</th>
                <th>{t.status}</th>
                {can("productWrite") && <th>{t.actions}</th>}
              </tr>
            </thead>
            <tbody>
              {busy ? (
                <tr>
                  <td colSpan={8} className="p-8 text-center">
                    Loading…
                  </td>
                </tr>
              ) : (
                shown.map((p) => (
                  <tr key={p.id}>
                    <td>{p.sku}</td>
                    <td className="font-semibold">{p.name}</td>
                    <td>{p.categoryName}</td>
                    <td>{p.price.toFixed(2)} DZD</td>
                    <td>{p.cost.toFixed(2)} DZD</td>
                    <td>
                      <span
                        className={`badge ${p.stock <= 5 ? "bg-red-100 text-red-700" : "bg-slate-100 text-slate-700"}`}
                      >
                        {p.stock}
                      </span>
                    </td>
                    <td>
                      <span
                        className={`badge ${p.isActive ? "bg-emerald-100 text-emerald-700" : "bg-slate-100 text-slate-600"}`}
                      >
                        {p.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    {can("productWrite") && (
                      <td>
                        <div className="flex gap-2">
                          <button
                            className="btn-secondary px-3"
                            onClick={() => setEdit(p)}
                          >
                            <Pencil size={15} />
                          </button>
                          <button
                            className="btn-danger px-3"
                            onClick={() => remove(p.id)}
                          >
                            <Trash2 size={15} />
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
      {(add || edit) && (
        <ProductModal
          product={edit}
          categories={cats}
          onClose={() => {
            setAdd(false);
            setEdit(null);
          }}
          onSave={save}
        />
      )}
    </div>
  );
}

function ProductModal({
  product,
  categories,
  onClose,
  onSave,
}: {
  product: Product | null;
  categories: Category[];
  onClose: () => void;
  onSave: (f: typeof empty) => void;
}) {
  const { t } = useApp();
  const [f, setF] = useState(
    product
      ? {
          sku: product.sku,
          name: product.name,
          categoryId: product.categoryId,
          price: product.price,
          cost: product.cost,
          stock: product.stock,
          isActive: product.isActive,
        }
      : empty,
  );

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <div className="card w-full max-w-lg p-6">
        <h2 className="text-lg font-bold">
          {product ? t.editProduct : t.addProduct}
        </h2>
        <div className="mt-5 grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium">{t.sku}</label>
            <input
              className="input"
              placeholder={t.sku}
              value={f.sku}
              onChange={(e) => setF({ ...f, sku: e.target.value })}
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">
              {t.productName}
            </label>
            <input
              className="input"
              placeholder={t.name}
              value={f.name}
              onChange={(e) => setF({ ...f, name: e.target.value })}
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">
              {t.category}
            </label>
            <select
              className="input"
              value={f.categoryId}
              onChange={(e) =>
                setF({ ...f, categoryId: Number(e.target.value) })
              }
            >
              <option value={0}>{t.selectCategory}</option>
              {categories
                .filter((c) => c.isActive)
                .map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">
              {t.sellingPrice}
            </label>
            <input
              className="input"
              type="number"
              min="0"
              step="0.01"
              placeholder={t.price}
              value={f.price}
              onChange={(e) => setF({ ...f, price: Number(e.target.value) })}
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">
              {t.costPrice}
            </label>
            <input
              className="input"
              type="number"
              min="0"
              step="0.01"
              placeholder={t.cost}
              value={f.cost}
              onChange={(e) => setF({ ...f, cost: Number(e.target.value) })}
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">
              {t.initialStock}
            </label>
            <input
              className="input"
              type="number"
              min="0"
              placeholder={t.stock}
              value={f.stock}
              onChange={(e) => setF({ ...f, stock: Number(e.target.value) })}
            />
          </div>
        </div>
        <label className="mt-4 flex gap-2 text-sm">
          <input
            type="checkbox"
            checked={f.isActive}
            onChange={(e) => setF({ ...f, isActive: e.target.checked })}
          />{" "}
          {t.active}
        </label>
        <div className="mt-6 flex justify-end gap-2">
          <button className="btn-secondary" onClick={onClose}>
            {t.cancel}
          </button>
          <button
            className="btn-primary"
            disabled={!f.sku || !f.name || !f.categoryId}
            onClick={() => onSave(f)}
          >
            {t.save}
          </button>
        </div>
      </div>
    </div>
  );
}

