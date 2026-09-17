"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Boxes } from "./Icons";

const links = [
  { href: "/", label: "Home" },
  { href: "/dashboard", label: "Dashboard" },
  { href: "/products", label: "Products" },
  { href: "/checkout", label: "Checkout" },
  { href: "/sales", label: "Sales" },
  { href: "/customers", label: "Customers" },
  { href: "/settings", label: "Settings" }
];

export default function Nav() {
  const pathname = usePathname();

  return (
    <nav className="flex min-h-screen w-64 shrink-0 flex-col gap-1 border-r border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-950">
      <div className="mb-7 flex items-center gap-3 px-2">
        <span className="font-bold text-slate-900 dark:text-white">Mini-POS</span>
      </div>

      {links.map((link) => (
        <Link
          key={link.href}
          href={link.href}
          className={`rounded-xl px-3 py-2.5 text-sm font-medium transition-colors ${
            pathname === link.href
              ? "bg-blue-50 text-blue-700 dark:bg-blue-950/50 dark:text-blue-300"
              : "text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
          }`}
        >
          {link.label}
        </Link>
      ))}
    </nav>
  );
}