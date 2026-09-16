"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const links = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/products", label: "Products" },
  { href: "/checkout", label: "Checkout" },
  { href: "/sales", label: "Sales" },
  { href: "/customers", label: "Customers" },
  { href: "/settings", label: "Settings" },
];

export default function Nav() {
  const pathname = usePathname();
  if (pathname === "/login") return null;

  return (
    <aside className="w-60 shrink-0 border-r border-line bg-surface p-5">
      <div className="mb-8">
        <p className="text-lg font-bold text-white">mini-pos</p>
        <p className="text-sm text-muted">IZI Soft</p>
      </div>

      <nav className="flex flex-col gap-1">
        {links.map((link) => {
          const isActive = pathname === link.href;
          return (
            <Link
              key={link.href}
              href={link.href}
              className={
                isActive
                  ? "rounded-lg bg-mint-soft px-3 py-2 text-sm font-semibold text-mint"
                  : "rounded-lg px-3 py-2 text-sm text-muted hover:bg-surface-2 hover:text-white"
              }
            >
              {link.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}