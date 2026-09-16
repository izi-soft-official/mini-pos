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
    label: "Dashboard",
    icon: LayoutDashboard,
    permission: "dashboard",
  },
  {
    href: "/products",
    label: "Products",
    icon: Package,
    permission: "products",
  },
  {
    href: "/checkout",
    label: "Checkout",
    icon: ShoppingCart,
    permission: "checkout",
  },
  {
    href: "/sales",
    label: "Sales History",
    icon: ClipboardList,
    permission: "sales",
  },
  {
    href: "/customers",
    label: "Customers",
    icon: Users,
    permission: "customers",
  },
  // {
  //   href: "/categories",
  //   label: "Categories",
  //   icon: Tag,
  //   permission: "categories",
  // },
  // { href: "/users", label: "Users", icon: Users, permission: "users" },
  {
    href: "/settings",
    label: "Settings",
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
  const { can } = useApp();

  // close the drawer whenever the route changes
  useEffect(() => {
    onClose();
  }, [path]);

  return (
    <>
      {/* Backdrop, mobile only, closes drawer on click */}
      {open && (
        <div
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={onClose}
        />
      )}

      {/* Sidebar: fixed slide-in drawer on mobile, static docked panel on md+ */}
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
              return (
                <Link
                  key={x.href}
                  href={x.href}
                  className={`navlink ${path === x.href ? "active" : ""}`}
                >
                  <I size={18} />
                  {x.label}
                </Link>
              );
            })}
        </nav>
      </aside>
    </>
  );
}
