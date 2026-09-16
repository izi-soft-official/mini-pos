"use client";

import { useEffect, useState } from "react";
import { customers, sales } from "@/lib/api";
import type { Customer } from "@/lib/types";
import { useApp } from "@/components/Providers";
import { Plus, Pencil, Trash2, Search, Eye } from "@/components/Icons";

const empty = { fullName: "", phone: "", email: "", note: "" };

export default function Customers() {
  const { can } = useApp();
  const [items, setItems] = useState<Customer[]>([]),
    [q, setQ] = useState(""),
    [edit, setEdit] = useState<Customer | null>(null),
    [add, setAdd] = useState(false),
    [history, setHistory] = useState<Customer | null>(null),
    [total, setTotal] = useState(0);

  async function load() {
    try {
      setItems((await customers.list(q)).items);
    } catch (e) {
      alert(e instanceof Error ? e.message : "Load failed");
    }
  }

  useEffect(() => {
    load();
  }, [q]);

  async function save(f: typeof empty) {
    try {
      if (edit) await customers.update(edit.id, f);
      else await customers.create(f);
      setEdit(null);
      setAdd(false);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Save failed");
    }
  }

  async function del(id: number) {
    if (!confirm("Delete customer?")) return;
    try {
      await customers.remove(id);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Delete failed");
    }
  }

  async function view(c: Customer) {
    setHistory(c);
    try {
      const r = await sales.list({ customerId: c.id });
      setTotal(r.items.reduce((a, x) => a + x.total, 0));
    } catch {
      setTotal(0);
    }
  }
  
  return (
    <div className="space-y-5">
      <div className="flex flex-wrap justify-between gap-3">
          <h1 className="text-2xl font-bold">Customers</h1>
        <button className="btn-primary" onClick={() => setAdd(true)}>
          <Plus size={17} /> Add customer
        </button>
      </div>
      <div className="card p-4">
        <div className="relative">
          <input
            className="input pl-9"
            placeholder="Search name or phone"
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />
        </div>
      </div>
      <div className="card overflow-hidden">
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Phone</th>
              <th>Email</th>
              <th>Created</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {items.map((c) => (
              <tr key={c.id}>
                <td className="font-semibold">{c.fullName}</td>
                <td>{c.phone}</td>
                <td>{c.email || "—"}</td>
                <td>{new Date(c.createdAt).toLocaleDateString()}</td>
                <td>
                  <div className="flex gap-2">
                    <button
                      className="btn-secondary px-3"
                      onClick={() => view(c)}
                    >
                      <Eye size={15} />
                    </button>
                    {can("customers") && (
                      <>
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
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(add || edit) && (
        <CustomerModal
          customer={edit}
          onClose={() => {
            setAdd(false);
            setEdit(null);
          }}
          onSave={save}
        />
      )}{" "}
      {history && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
          <div className="card w-full max-w-md p-6">
            <h2 className="text-lg font-bold">{history.fullName}</h2>
            <p className="mt-2 text-sm text-slate-500">{history.phone}</p>
            <div className="mt-5 rounded-lg bg-slate-50 p-4 dark:bg-slate-900">
              <div className="text-sm text-slate-500">Total spent</div>
              <div className="text-2xl font-black">{total.toFixed(2)} DZD</div>
            </div>
            <button
              className="btn-secondary mt-5 w-full"
              onClick={() => setHistory(null)}
            >
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function CustomerModal({
  customer,
  onClose,
  onSave,
}: {
  customer: Customer | null;
  onClose: () => void;
  onSave: (f: typeof empty) => void;
}) {
  const [f, setF] = useState(
    customer
      ? {
          fullName: customer.fullName,
          phone: customer.phone,
          email: customer.email,
          note: customer.note,
        }
      : empty,
  );

  // Helper function to format raw numeric string to "0000 00 00 00" layout blueprint
  const formatDisplayPhone = (raw: string) => {
    const nums = raw.replace(/\D/g, ""); // Strip anything that isn't a number digit
    if (nums.length <= 4) return nums;
    if (nums.length <= 6) return `${nums.slice(0, 4)} ${nums.slice(4)}`;
    if (nums.length <= 8)
      return `${nums.slice(0, 4)} ${nums.slice(4, 6)} ${nums.slice(6)}`;
    return `${nums.slice(0, 4)} ${nums.slice(4, 6)} ${nums.slice(6, 8)} ${nums.slice(8, 10)}`;
  };

  // 1. Validate underlying raw phone data values (Must be 10 digits starting with 0)
  const rawPhone = f.phone.replace(/\D/g, "");
  const isPhoneValid = /^0\d{9}$/.test(rawPhone);

  // 2. Validate Email layout
  const isEmailValid =
    f.email.trim() === "" || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.trim());

  // 3. Complete Form Validation Check
  const isFormValid = f.fullName.trim() !== "" && isPhoneValid && isEmailValid;

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <div className="card w-full max-w-lg p-6">
        <h2 className="text-lg font-bold">
          {customer ? "Edit customer" : "Add customer"}
        </h2>

        <div className="mt-5 space-y-4">
          {/* Full Name Field */}
          <div>
            <label className="mb-1 block text-sm font-medium">Full Name</label>
            <input
              className="input"
              placeholder="e.g. Mohamed Benali"
              value={f.fullName}
              onChange={(e) => setF({ ...f, fullName: e.target.value })}
            />
          </div>

          {/* Phone Field with dynamic chunked format layout spacer */}
          <div>
            <div className="flex justify-between items-center mb-1">
              <label className="text-sm font-medium">Phone Number</label>
              {rawPhone.length > 0 && !isPhoneValid && (
                <span className="text-xs text-red-500 font-medium">
                  Must be 10 digits starting with 0
                </span>
              )}
            </div>
            <input
              className={`input ${rawPhone.length > 0 && !isPhoneValid ? "border-red-400 focus:border-red-500" : ""}`}
              placeholder="e.g. 0550 12 34 56"
              maxLength={13} // Account for the 3 visual spaces added in our format blueprint (10 digits + 3 spaces)
              value={formatDisplayPhone(f.phone)}
              onChange={(e) =>
                setF({ ...f, phone: e.target.value.replace(/\D/g, "") })
              }
            />
          </div>

          {/* Email Field */}
          <div>
            <div className="flex justify-between items-center mb-1">
              <label className="text-sm font-medium">Email Address</label>
              {!isEmailValid && (
                <span className="text-xs text-red-500 font-medium">
                  Invalid email address layout
                </span>
              )}
            </div>
            <input
              className={`input ${!isEmailValid ? "border-red-400 focus:border-red-500" : ""}`}
              type="email"
              placeholder="e.g. customer@mail.com"
              value={f.email}
              onChange={(e) => setF({ ...f, email: e.target.value })}
            />
          </div>

          {/* Note Field */}
          <div>
            <label className="mb-1 block text-sm font-medium">
              Internal Notes (Optional)
            </label>
            <textarea
              className="input min-h-24"
              placeholder="Add payment patterns or delivery notes..."
              value={f.note}
              onChange={(e) => setF({ ...f, note: e.target.value })}
            />
          </div>
        </div>

        {/* Action Controls Panel */}
        <div className="mt-6 flex justify-end gap-2">
          <button className="btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button
            className={`btn-primary ${!isFormValid ? "opacity-50 cursor-not-allowed" : ""}`}
            disabled={!isFormValid}
            onClick={() => onSave({ ...f, phone: rawPhone })} // Saves the clean raw 10-digit number back to C# database
          >
            Save Customer
          </button>
        </div>
      </div>
    </div>
  );
}