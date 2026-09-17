"use client";

import { useEffect, useState } from "react";
import { users } from "@/lib/api";
import type { Role, User } from "@/lib/types";
import { useApp } from "@/components/Providers";
import { Plus, Pencil, Trash2, LockKeyhole } from "@/components/Icons";

export default function UsersPage() {
  const { user, can, t } = useApp();
  const [items, setItems] = useState<User[]>([]),
    [edit, setEdit] = useState<User | null>(null),
    [add, setAdd] = useState(false);

  async function load() {
    try {
      setItems((await users.list()).items);
    } catch (e) {
      alert(e instanceof Error ? e.message : "Users API unavailable");
    }
  }

  useEffect(() => {
    if (user?.role === "Admin") load();
  }, [user?.role]);

  if (!can("users"))
    return (
      <div className="card p-8">
        <h1 className="text-xl font-bold">{t.accessDenied}</h1>
        <p className="mt-2 text-sm text-slate-500">
          {t.adminOnlyManagementDesc}
        </p>
      </div>
    );

  async function save(f: {
    username: string;
    fullName: string;
    password: string;
    role: Role;
    isActive: boolean;
  }) {
    try {
      if (edit) {
        await users.update(edit.id, {
          fullName: f.fullName,
          role: f.role,
          isActive: f.isActive,
        });
        if (f.password) await users.password(edit.id, f.password);
      } else await users.create(f);
      setEdit(null);
      setAdd(false);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Save failed");
    }
  }

  async function del(id: number) {
    if (id === user?.id) return alert(t.cannotDeleteOwnAccount);
    if (!confirm(t.confirmDeleteUser)) return;
    try {
      await users.remove(id);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : "Delete failed");
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex justify-between">
        <div>
          <h1 className="text-2xl font-bold">{t.users}</h1>
          <p className="text-sm text-slate-500">
            {t.adminOnlyAccountManagement}
          </p>
        </div>
        <button className="btn-primary" onClick={() => setAdd(true)}>
          <Plus size={17} /> {t.addUser}
        </button>
      </div>
      <div className="card overflow-hidden">
        <table className="table">
          <thead>
            <tr>
              <th>{t.name}</th>
              <th>{t.username}</th>
              <th>{t.role}</th>
              <th>{t.status}</th>
              <th>{t.actions}</th>
            </tr>
          </thead>
          <tbody>
            {items.map((u) => (
              <tr key={u.id}>
                <td className="font-semibold">
                  {u.fullName}
                  {u.id === user?.id && (
                    <span className="ml-2 text-xs text-blue-600">{t.you}</span>
                  )}
                </td>
                <td>{u.username}</td>
                <td>{u.role}</td>
                <td>
                  <span
                    className={`badge ${u.isActive ? "bg-emerald-100 text-emerald-700" : "bg-red-100 text-red-700"}`}
                  >
                    {u.isActive ? t.active : t.inactive}
                  </span>
                </td>
                <td>
                  <div className="flex gap-2">
                    <button
                      className="btn-secondary px-3"
                      onClick={() => setEdit(u)}
                    >
                      <Pencil size={15} />
                    </button>
                    {/* <button
                      className="btn-secondary px-3"
                      onClick={() => {
                        const p = prompt(t.newPasswordPrompt);
                        if (p)
                          users
                            .password(u.id, p)
                            .then(load)
                            .catch((e) => alert(e.message));
                      }}
                    >
                      <LockKeyhole size={15} />
                    </button> */}
                    <button
                      className="btn-danger px-3"
                      disabled={u.id === user?.id}
                      onClick={() => del(u.id)}
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(add || edit) && (
        <UserModal
          user={edit}
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

function UserModal({
  user,
  onClose,
  onSave,
}: {
  user: User | null;
  onClose: () => void;
  onSave: (f: {
    username: string;
    fullName: string;
    password: string;
    role: Role;
    isActive: boolean;
  }) => void;
}) {
  const { t } = useApp();
  const [f, setF] = useState({
    username: user?.username || "",
    fullName: user?.fullName || "",
    password: "",
    role: (user?.role || "User") as Role,
    isActive: user?.isActive ?? true,
  });

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <div className="card w-full max-w-lg p-6">
        <h2 className="text-lg font-bold">{user ? t.editUser : t.addUser}</h2>
        <div className="mt-5 space-y-3">
          <input
            className="input"
            placeholder={t.fullNamePlaceholder}
            value={f.fullName}
            onChange={(e) => setF({ ...f, fullName: e.target.value })}
          />
          <input
            className="input"
            placeholder={t.usernamePlaceholder}
            disabled={!!user}
            value={f.username}
            onChange={(e) => setF({ ...f, username: e.target.value })}
          />
          <input
            className="input"
            type="password"
            placeholder={
              user ? t.newPasswordOptionalPlaceholder : t.passwordPlaceholder
            }
            value={f.password}
            onChange={(e) => setF({ ...f, password: e.target.value })}
          />
          <select
            className="input"
            value={f.role}
            onChange={(e) => setF({ ...f, role: e.target.value as Role })}
          >
            <option value="Admin">Admin</option>
            <option value="Manager">Manager</option>
            <option value="User">User</option>
          </select>
          <label className="flex gap-2 text-sm">
            <input
              type="checkbox"
              checked={f.isActive}
              onChange={(e) => setF({ ...f, isActive: e.target.checked })}
            />{" "}
            {t.active}
          </label>
        </div>
        <div className="mt-6 flex justify-end gap-2">
          <button className="btn-secondary" onClick={onClose}>
            {t.cancel}
          </button>
          <button
            className="btn-primary"
            disabled={!f.fullName || !f.username || (!user && !f.password)}
            onClick={() => onSave(f)}
          >
            {t.save}
          </button>
        </div>
      </div>
    </div>
  );
}
