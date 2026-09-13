"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

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
    <nav className="nav">
      <span className="nav-brand">mini-pos</span>
      {links.map((link) => (
        <Link
          key={link.href}
          href={link.href}
          className={pathname === link.href ? "nav-link nav-link-active" : "nav-link"}
        >
          {link.label}
        </Link>
      ))}
    </nav>
  );
}
