// app/checkout/page.tsx
"use client";

import { useMemo, useState } from "react";

interface Product {
  id: string;
  name: string;
  sku: string;
  category: string;
  price: number;
  stock: number;
}

interface CartLine {
  productId: string;
  name: string;
  unitPrice: number;
  quantity: number;
}

type PaymentMethod = "cash" | "card" | "izipay";

const mockProducts: Product[] = [
  { id: "p1", sku: "STA-001", name: "Notebook", category: "Stationery", price: 5.0, stock: 3 },
  { id: "p2", sku: "STA-002", name: "Pen Set", category: "Stationery", price: 12.5, stock: 15 },
  { id: "p3", sku: "ELE-001", name: "Desk Lamp", category: "Electronics", price: 35.0, stock: 1 },
  { id: "p4", sku: "STA-003", name: "Sticky Notes", category: "Stationery", price: 3.25, stock: 60 },
  { id: "p5", sku: "ELE-002", name: "USB Cable", category: "Electronics", price: 7.99, stock: 25 },
];

export default function CheckoutPage() {
  const [query, setQuery] = useState("");
  const [cart, setCart] = useState<CartLine[]>([]);
  const [discount, setDiscount] = useState("0");
  const [paid, setPaid] = useState("");
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod | null>(null);
  const [stockErrors, setStockErrors] = useState<Record<string, string>>({});

  const filteredProducts = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return mockProducts;
    return mockProducts.filter(
      (p) => p.name.toLowerCase().includes(q) || p.category.toLowerCase().includes(q)
    );
  }, [query]);

  function addToCart(product: Product) {
    setCart((prev) => {
      const existing = prev.find((l) => l.productId === product.id);
      if (existing) {
        return prev.map((l) =>
          l.productId === product.id ? { ...l, quantity: l.quantity + 1 } : l
        );
      }
      return [
        ...prev,
        { productId: product.id, name: product.name, unitPrice: product.price, quantity: 1 },
      ];
    });
  }

  function updateQuantity(productId: string, quantity: number) {
    if (quantity <= 0) {
      setCart((prev) => prev.filter((l) => l.productId !== productId));
      return;
    }
    setCart((prev) =>
      prev.map((l) => (l.productId === productId ? { ...l, quantity } : l))
    );

    setStockErrors((prev) => {
      const next = { ...prev };
      delete next[productId];
      return next;
    });
  }

  function removeLine(productId: string) {
    setCart((prev) => prev.filter((l) => l.productId !== productId));
  }

  const subtotal = cart.reduce((sum, l) => sum + l.unitPrice * l.quantity, 0);
  const discountAmount = parseFloat(discount) || 0;
  const total = Math.max(subtotal - discountAmount, 0);
  const paidAmount = parseFloat(paid) || 0;
  const changeAmount = paidAmount - total;

  const canPay = cart.length > 0 && paidAmount >= total && paymentMethod !== null;

  function validateStock(): Record<string, string> {
    const errors: Record<string, string> = {};
    for (const line of cart) {
      const product = mockProducts.find((p) => p.id === line.productId);
      if (!product) {
        errors[line.productId] = "Product no longer exists.";
        continue;
      }
      if (line.quantity > product.stock) {
        errors[line.productId] = `Only ${product.stock} left in stock (requested ${line.quantity}).`;
      }
    }
    return errors;
  }

  function completeSale() {
    if (!canPay) return;

    const errors = validateStock();
    if (Object.keys(errors).length > 0) {
      setStockErrors(errors);
      return;
    }

    setStockErrors({});

    const sale = {
      number: `S-${Date.now()}`,
      createdAt: new Date().toISOString(),
      lines: cart.map((l) => ({
        productName: l.name,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        lineTotal: l.unitPrice * l.quantity,
      })),
      subtotal,
      discount: discountAmount,
      total,
      paidAmount,
      changeAmount,
      paymentMethod,
      status: "completed",
    };
    // TODO: replace with POST /api/sales once backend exists
    console.log("Sale created:", sale);
    setCart([]);
    setDiscount("0");
    setPaid("");
    setPaymentMethod(null);
  }

  return (
    <>
      <h1 className="text-xl font-semibold text-gray-900 mb-4">Checkout</h1>

      <div className="bg-white border border-gray-300 rounded p-4">
        <input
          className="mb-4 px-2.5 py-2 border border-gray-300 rounded w-full max-w-xs text-sm focus:outline-none focus:border-blue-600"
          type="text"
          placeholder="Search products by name or category..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr>
              <th className="text-left px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                Sku
              </th>
              <th className="text-left px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                Product Name
              </th>
              <th className="text-left px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                Category
              </th>
              <th className="text-right px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                Price
              </th>
              <th className="text-right px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                Stock
              </th>
            </tr>
          </thead>
          <tbody>
            {filteredProducts.map((p) => (
              <tr key={p.id}>
                <td className="px-2.5 py-2 border-b border-gray-300 align-middle">{p.sku}</td>
                <td className="px-2.5 py-2 border-b border-gray-300 align-middle">
                  <button
                    className="bg-transparent border-none p-0 text-blue-600 no-underline cursor-pointer text-sm hover:text-blue-900 disabled:text-gray-500 disabled:no-underline disabled:cursor-not-allowed"
                    onClick={() => addToCart(p)}
                    disabled={p.stock === 0}
                  >
                    {p.name}
                  </button>
                </td>
                <td className="px-2.5 py-2 border-b border-gray-300 align-middle">{p.category}</td>
                <td className="px-2.5 py-2 border-b border-gray-300 align-middle text-right">
                  {p.price.toFixed(2)}
                </td>
                <td className="px-2.5 py-2 border-b border-gray-300 align-middle text-right">
                  {p.stock}
                </td>
              </tr>
            ))}
            {filteredProducts.length === 0 && (
              <tr>
                <td className="text-center text-gray-500 py-6" colSpan={5}>
                  No products match "{query}".
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="mt-6 flex gap-6 items-start flex-wrap">
        <div className="flex-1 basis-120 bg-white border border-gray-300 rounded p-4">
          <h2 className="text-base font-semibold mb-3">Cart</h2>
          {cart.length === 0 ? (
            <p className="text-gray-500 text-sm py-6 text-center">
              No items yet — click a product name to add it.
            </p>
          ) : (
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr>
                  <th className="text-left px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                    Item
                  </th>
                  <th className="text-right px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                    Qty
                  </th>
                  <th className="text-right px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                    Unit
                  </th>
                  <th className="text-right px-2.5 py-2 border-b border-gray-300 text-gray-500 font-semibold text-xs uppercase tracking-wide">
                    Total
                  </th>
                  <th className="border-b border-gray-300"></th>
                  <th className="border-b border-gray-300"></th>
                </tr>
              </thead>
              <tbody>
                {cart.map((l) => (
                  <tr key={l.productId}>
                    <td className="px-2.5 py-2 border-b border-gray-300 align-middle font-medium">
                      {l.name}
                    </td>
                    <td className="px-2.5 py-2 border-b border-gray-300 align-middle text-right">
                      <input
                        className="w-14 px-1.5 py-1 border border-gray-300 rounded text-xs text-center"
                        type="number"
                        min={0}
                        value={l.quantity}
                        onChange={(e) =>
                          updateQuantity(l.productId, parseInt(e.target.value, 10) || 0)
                        }
                      />
                      {stockErrors[l.productId] && (
                        <div className="text-red-700 text-[11px] mt-1">
                          {stockErrors[l.productId]}
                        </div>
                      )}
                    </td>
                    <td className="px-2.5 py-2 border-b border-gray-300 align-middle text-right">
                      {l.unitPrice.toFixed(2)}
                    </td>
                    <td className="px-2.5 py-2 border-b border-gray-300 align-middle text-right">
                      {(l.unitPrice * l.quantity).toFixed(2)}
                    </td>
                    <td className="px-2.5 py-2 border-b border-gray-300 align-middle">
                      <button
                        className="px-2.5 py-1 border border-gray-300 rounded bg-white text-gray-500 text-xs hover:border-blue-600 hover:text-blue-600"
                        onClick={() => updateQuantity(l.productId, l.quantity + 1)}
                      >
                        Add
                      </button>
                    </td>
                    <td className="px-2.5 py-2 border-b border-gray-300 align-middle">
                      <button
                        className="px-2.5 py-1 border border-gray-300 rounded bg-white text-gray-500 text-xs hover:border-blue-600 hover:text-blue-600"
                        onClick={() => removeLine(l.productId)}
                      >
                        Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <div className="flex-none basis-75 bg-white border border-gray-300 rounded p-4">
          <div className="flex flex-col gap-2">
            <div className="flex justify-between items-center text-sm text-gray-500">
              <span>Subtotal</span>
              <span>{subtotal.toFixed(2)}</span>
            </div>
            <div className="flex justify-between items-center text-sm text-gray-500">
              <span>Discount</span>
              <input
                className="w-24 text-right px-1.5 py-1.5 border border-gray-300 rounded text-sm"
                type="number"
                min={0}
                step="0.01"
                value={discount}
                onChange={(e) => setDiscount(e.target.value)}
              />
            </div>
            <div className="flex justify-between items-center text-[15px] font-semibold text-gray-900 pt-2 mt-1 border-t border-gray-300">
              <span>Total</span>
              <span>{total.toFixed(2)}</span>
            </div>
            <div className="flex justify-between items-center text-sm text-gray-500">
              <span>Paid</span>
              <input
                className="w-24 text-right px-1.5 py-1.5 border border-gray-300 rounded text-sm"
                type="number"
                min={0}
                step="0.01"
                value={paid}
                onChange={(e) => setPaid(e.target.value)}
              />
            </div>
            <div className="flex justify-between items-center text-sm text-gray-500">
              <span>Change</span>
              <span>{changeAmount >= 0 ? changeAmount.toFixed(2) : "0.00"}</span>
            </div>
          </div>

          <div className="mt-4 flex gap-2">
            <button
              className={
                paymentMethod === "cash"
                  ? "flex-1 py-2 border border-blue-600 rounded bg-blue-600 text-white text-xs font-semibold"
                  : "flex-1 py-2 border border-gray-300 rounded bg-white text-gray-900 text-xs hover:border-blue-600 hover:text-blue-600"
              }
              onClick={() => setPaymentMethod("cash")}
            >
              Cash
            </button>
            <button
              className={
                paymentMethod === "card"
                  ? "flex-1 py-2 border border-blue-600 rounded bg-blue-600 text-white text-xs font-semibold"
                  : "flex-1 py-2 border border-gray-300 rounded bg-white text-gray-900 text-xs hover:border-blue-600 hover:text-blue-600"
              }
              onClick={() => setPaymentMethod("card")}
            >
              Card
            </button>
            <button
              className={
                paymentMethod === "izipay"
                  ? "flex-1 py-2 border border-blue-600 rounded bg-blue-600 text-white text-xs font-semibold"
                  : "flex-1 py-2 border border-gray-300 rounded bg-white text-gray-900 text-xs hover:border-blue-600 hover:text-blue-600"
              }
              onClick={() => setPaymentMethod("izipay")}
            >
              Izi Pay
            </button>
          </div>

          <div className="mt-4">
            <button
              className="w-full py-2.5 bg-blue-600 text-white border-none rounded text-sm font-semibold hover:bg-blue-900 disabled:bg-gray-500 disabled:cursor-not-allowed"
              onClick={completeSale}
              disabled={!canPay}
            >
              Pay
            </button>
          </div>
        </div>
      </div>
    </>
  );
}