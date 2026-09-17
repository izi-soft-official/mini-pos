import type { Metadata } from "next";
import Nav from "@/components/Nav";
import "./globals.css";

export const metadata: Metadata = {
  title: "mini-pos",
  description: "Training POS for IZI Soft"
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <div className="flex min-h-screen">
          <Nav />
          <main className="page flex-1 overflow-y-auto p-6">{children}</main>
        </div>
      </body>
    </html>
  );
}