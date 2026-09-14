// lib/db.ts

// ---------- Types ----------

export interface Product {
  id: string;
  sku: string;
  name: string;
  category: string;
  price: number;
  stock: number;
  lowStockThreshold: number;
  active: boolean;
}

export interface Customer {
  id: string;
  name: string;
  email?: string;
  phone?: string;
  createdAt: string;
}

export type PaymentMethod = "cash" | "card" | "izipay";

export interface SaleLine {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Sale {
  id: string;
  number: string;
  createdAt: string;
  customerId?: string;
  cashierName?: string;
  lines: SaleLine[];
  subtotal: number;
  discount: number;
  total: number;
  paidAmount: number;
  changeAmount: number;
  paymentMethod: PaymentMethod;
  status: "completed" | "voided" | "refunded";
}

// ---------- Mock data ----------

export const products: Product[] = [
  { id: "p1", sku: "STA-001", name: "Book", category: "Stationery", price: 5.0, stock: 3, lowStockThreshold: 5, active: true },
  { id: "p2", sku: "STA-002", name: "Pen Set", category: "Stationery", price: 12.5, stock: 15, lowStockThreshold: 10, active: true },
  { id: "p3", sku: "ELE-001", name: "Desk Lamp", category: "Electronics", price: 35.0, stock: 1, lowStockThreshold: 3, active: true },
  { id: "p4", sku: "STA-003", name: "Sticky Notes", category: "Stationery", price: 3.25, stock: 60, lowStockThreshold: 20, active: true },
  { id: "p5", sku: "ELE-002", name: "USB Cable", category: "Electronics", price: 7.99, stock: 25, lowStockThreshold: 15, active: true },
];

export const customers: Customer[] = [
  { id: "c1", name: "Walk-in Customer", createdAt: "2026-01-01T00:00:00.000Z" },
  { id: "c2", name: "Maria Torres", email: "maria@example.com", phone: "0555 01 02 03", createdAt: "2026-02-14T09:30:00.000Z" },
  { id: "c3", name: "Jonas Reyes", email: "jonas@example.com", phone: "0555 01 77 78", createdAt: "2026-03-02T15:00:00.000Z" },
];

export const sales: Sale[] = [
  {
    id: "s1",
    number: "S-1001",
    createdAt: "2026-08-10T14:22:00.000Z",
    customerId: "c2",
    cashierName: "Alice Johnson",
    lines: [
      { productId: "p1", productName: "Book", quantity: 2, unitPrice: 5.0, lineTotal: 10.0 },
      { productId: "p5", productName: "USB Cable", quantity: 1, unitPrice: 7.99, lineTotal: 7.99 },
    ],
    subtotal: 17.99,
    discount: 0,
    total: 17.99,
    paidAmount: 20,
    changeAmount: 2.01,
    paymentMethod: "cash",
    status: "completed",
  },
  {
    id: "s2",
    number: "S-1002",
    createdAt: "2026-08-11T10:05:00.000Z",
    customerId: "c1",
    cashierName: "Alice Johnson",
    lines: [
      { productId: "p3", productName: "Desk Lamp", quantity: 1, unitPrice: 35.0, lineTotal: 35.0 },
    ],
    subtotal: 35.0,
    discount: 5,
    total: 30.0,
    paidAmount: 30,
    changeAmount: 0,
    paymentMethod: "card",
    status: "completed",
  },
];

// ---------- Product helpers ----------

export function getProducts(): Product[] {
  return [...products];
}

export function getProductById(id: string): Product | undefined {
  return products.find((p) => p.id === id);
}

export function searchProducts(query: string): Product[] {
  const q = query.trim().toLowerCase();
  if (!q) return products;
  return products.filter(
    (p) => p.name.toLowerCase().includes(q) || p.category.toLowerCase().includes(q)
  );
}

export function adjustStock(productId: string, delta: number): void {
  const product = products.find((p) => p.id === productId);
  if (product) product.stock += delta;
}

// ---------- Customer helpers ----------

export function getCustomers(): Customer[] {
  return customers;
}

export function getCustomerById(id: string): Customer | undefined {
  return customers.find((c) => c.id === id);
}

export function addCustomer(data: Omit<Customer, "id" | "createdAt">): Customer {
  const customer: Customer = {
    id: `c${customers.length + 1}`,
    createdAt: new Date().toISOString(),
    ...data,
  };
  customers.push(customer);
  return customer;
}

// ---------- Sales helpers ----------

export function getSales(): Sale[] {
  return sales;
}

export function getSaleById(id: string): Sale | undefined {
  return sales.find((s) => s.id === id);
}

export function addSale(sale: Omit<Sale, "id">): Sale {
  const newSale: Sale = { id: `s${sales.length + 1}`, ...sale };
  sales.push(newSale);

  // keep stock in sync
  for (const line of newSale.lines) {
    adjustStock(line.productId, -line.quantity);
  }

  return newSale;
}

// ---------- Product management helpers ----------
export function addProduct(data: Omit<Product, "id">): Product {
  const product: Product = { id: `p${products.length + 1}-${Date.now()}`, ...data };
  products.push(product);
  return product;
}

export function updateProduct(updated: Product): void {
  const index = products.findIndex((p) => p.id === updated.id);
  if (index !== -1) products[index] = updated;
}

export function deleteProduct(id: string): void {
  const index = products.findIndex((p) => p.id === id);
  if (index !== -1) products.splice(index, 1);
}