import type { Metadata } from "next";
import { cookies } from "next/headers";
import Nav from "@/components/Nav";
import "./globals.css";

export const metadata: Metadata = {
  title: "mini-pos",
  description: "Training POS for IZI Soft",
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const locale = (await cookies()).get("locale")?.value ?? "fr";
  const dir = locale === "ar" ? "rtl" : "ltr";

  return (
    <html lang={locale} dir={dir}>
      <body className="flex min-h-screen bg-ink text-slate-100">
        <Nav />
        <main className="flex-1 overflow-x-auto p-8">{children}</main>
      </body>
    </html>
  );
}