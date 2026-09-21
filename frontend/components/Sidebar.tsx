"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect } from "react";
import {
  LayoutDashboard,
  Package,
  ShoppingCart,
  ClipboardList,
  Users,
  Settings,
  Boxes,
  Tag,
} from "./Icons";
import { useApp } from "./Providers";

const links = [
  {
    href: "/",
    label: "dashboard",
    icon: LayoutDashboard,
    permission: "dashboard",
  },
  {
    href: "/products",
    label: "products",
    icon: Package,
    permission: "products",
  },
  {
    href: "/checkout",
    label: "checkout",
    icon: ShoppingCart,
    permission: "checkout",
  },
  {
    href: "/sales",
    label: "salesHistory",
    icon: ClipboardList,
    permission: "sales",
  },
  {
    href: "/customers",
    label: "customers",
    icon: Users,
    permission: "customers",
  },
  // {
  //   href: "/categories",
  //   label: "categories",
  //   icon: Tag,
  //   permission: "categories",
  // },
  { href: "/users", label: "users", icon: Users, permission: "users" },
  {
    href: "/settings",
    label: "settings",
    icon: Settings,
    permission: "settings",
  },
];

export default function Sidebar({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const path = usePathname();
  const { can, t } = useApp();

  useEffect(() => {
    onClose();
  }, [path]);

  return (
    <>
      {open && (
        <div
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={onClose}
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-50 flex w-64 shrink-0 flex-col border-r border-slate-200 bg-white transition-transform duration-200 dark:border-slate-800 dark:bg-slate-950 md:static md:flex md:translate-x-0 ${
          open ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="p-5 text-xl font-black">
          Mini<span className="text-blue-600">POS</span>
        </div>
        <nav className="flex-1 space-y-1 px-3">
          {links
            .filter((x) => can(x.permission))
            .map((x) => {
              const I = x.icon;
              const labelText = (t as any)[x.label] || x.label;
              return (
                <Link
                  key={x.href}
                  href={x.href}
                  className={`navlink ${path === x.href ? "active" : ""}`}
                >
                  <I size={18} />
                  {labelText}
                </Link>
              );
            })}
        </nav>
      </aside>
    </>
  );
}
