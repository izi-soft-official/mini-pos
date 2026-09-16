"use client";

import { useEffect, useState } from "react";
import { categories } from "@/lib/api";
import type { Category } from "@/lib/types";
import { useApp } from "@/components/Providers";
import { Plus, Pencil, Trash2, ArrowLeft } from "@/components/Icons";
import Link from "next/link";

export default function Categories() {
  const { can } = useApp();
  const [items, setItems] = useState<Category[]>([]),
    [edit, setEdit] = useState<Category | null>(null),
    [add, setAdd] = useState(false);
  async function load() {
    try {
      setItems(await categories.list());
    } catch (e) {
      alert(e instanceof Error ? e.message : "Load failed");
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function save(name: string, active: boolean) {
    try {
      if (edit) await categories.update(edit.id, { name, isActive: active });
      else await categories.create(name);
      setEdit(null);
      setAdd(false);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Save failed");
    }
  }

  async function del(id: number) {
    if (!confirm("Delete category?")) return;
    try {
      await categories.remove(id);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Cannot delete category");
    }
  }
  
  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Link
            href="/products"
            className="btn-secondary px-2"
            aria-label="Back to products"
          >
            <ArrowLeft size={18} />
          </Link>
          <h1 className="text-2xl font-bold">Categories</h1>
        </div>
        {can("categories") && (
          <button className="btn-primary" onClick={() => setAdd(true)}>
            <Plus size={17} /> Add category
          </button>
        )}
      </div>
      <div className="card overflow-hidden">
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Status</th>
              {can("categories") && <th>Actions</th>}
            </tr>
          </thead>
          <tbody>
            {items.map((c) => (
              <tr key={c.id}>
                <td className="font-semibold">{c.name}</td>
                <td>
                  <span
                    className={`badge ${c.isActive ? "bg-emerald-100 text-emerald-700" : "bg-slate-100 text-slate-600"}`}
                  >
                    {c.isActive ? "Active" : "Inactive"}
                  </span>
                </td>
                {can("categories") && (
                  <td>
                    <div className="flex gap-2">
                      <button
                        className="btn-secondary px-3"
                        onClick={() => setEdit(c)}
                      >
                        <Pencil size={15} />
                      </button>
                      <button
                        className="btn-danger px-3"
                        onClick={() => del(c.id)}
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(add || edit) && (
        <CatModal
          category={edit}
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
function CatModal({
  category,
  onClose,
  onSave,
}: {
  category: Category | null;
  onClose: () => void;
  onSave: (name: string, active: boolean) => void;
}) {
  const [name, setName] = useState(category?.name || "");
  const [active, setActive] = useState(category?.isActive ?? true);
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <div className="card w-full max-w-md p-6">
        <h2 className="font-bold">
          {category ? "Edit category" : "Add category"}
        </h2>
        <input
          className="input mt-5"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Category name"
        />
        <label className="mt-4 flex gap-2 text-sm">
          <input
            type="checkbox"
            checked={active}
            onChange={(e) => setActive(e.target.checked)}
          />{" "}
          Active
        </label>
        <div className="mt-6 flex justify-end gap-2">
          <button className="btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button
            className="btn-primary"
            disabled={!name.trim()}
            onClick={() => onSave(name.trim(), active)}
          >
            Save
          </button>
        </div>
      </div>
    </div>
  );
}
