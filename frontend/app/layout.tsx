import "./globals.css";
import { Providers } from "@/components/Providers";
import AppShell from "@/components/AppShell";

export const metadata = {
  title: "Mini POS",
  description: "Point of sale management",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <Providers>
          <AppShell>{children}</AppShell>
        </Providers>
      </body>
    </html>
  );
}
