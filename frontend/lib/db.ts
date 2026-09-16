import { Customer, Product, Sale, User } from "./types";

export const seedUsers: User[] = [
  {
    id: "u1",
    name: "Admin User",
    username: "admin",
    password: "admin123",
    role: "admin",
    active: true,
  },
  {
    id: "u2",
    name: "Manager User",
    username: "manager",
    password: "manager123",
    role: "manager",
    active: true,
  },
  {
    id: "u3",
    name: "Cashier User",
    username: "cashier",
    password: "cashier123",
    role: "cashier",
    active: true,
  },
];

export const seedProducts: Product[] = [
  {
    id: "p1",
    name: "Wireless Mouse",
    sku: "ELEC-001",
    category: "Electronics",
    price: 2500,
    stock: 18,
    lowStockThreshold: 5,
    active: true,
  },
  {
    id: "p2",
    name: "USB-C Cable",
    sku: "ELEC-002",
    category: "Electronics",
    price: 1200,
    stock: 4,
    lowStockThreshold: 5,
    active: true,
  },
  {
    id: "p3",
    name: "Keyboard",
    sku: "ELEC-003",
    category: "Electronics",
    price: 4200,
    stock: 11,
    lowStockThreshold: 5,
    active: true,
  },
  {
    id: "p4",
    name: "Notebook A5",
    sku: "STAT-001",
    category: "Stationery",
    price: 450,
    stock: 40,
    lowStockThreshold: 10,
    active: true,
  },
  {
    id: "p5",
    name: "Gel Pen Pack",
    sku: "STAT-002",
    category: "Stationery",
    price: 700,
    stock: 7,
    lowStockThreshold: 10,
    active: true,
  },
  {
    id: "p6",
    name: "Desk Lamp",
    sku: "HOME-001",
    category: "Home",
    price: 5800,
    stock: 3,
    lowStockThreshold: 5,
    active: true,
  },
  {
    id: "p7",
    name: "Water Bottle",
    sku: "HOME-002",
    category: "Home",
    price: 1800,
    stock: 22,
    lowStockThreshold: 5,
    active: true,
  },
  {
    id: "p8",
    name: "Backpack",
    sku: "BAG-001",
    category: "Bags",
    price: 6500,
    stock: 8,
    lowStockThreshold: 5,
    active: true,
  },
];

export const seedCustomers: Customer[] = [
  { id: "c1", name: "Amine Benali", phone: "0550 12 34 56" },
  { id: "c2", name: "Sarah Haddad", phone: "0661 22 33 44" },
  { id: "c3", name: "Yacine Karim", phone: "0770 55 66 77" },
];

const daysAgo = (n: number) => {
  const d = new Date();
  d.setDate(d.getDate() - n);
  d.setHours(12, 0, 0, 0);
  return d.toISOString();
};

export const seedSales: Sale[] = [
  {
    id: "S-1003",
    date: daysAgo(0),
    cashierId: "u3",
    cashierName: "Cashier User",
    customerId: "c1",
    customerName: "Amine Benali",
    lines: [
      {
        productId: "p1",
        name: "Wireless Mouse",
        quantity: 2,
        unitPrice: 2500,
        total: 5000,
      },
      {
        productId: "p4",
        name: "Notebook A5",
        quantity: 3,
        unitPrice: 450,
        total: 1350,
      },
    ],
    subtotal: 6350,
    total: 6350,
  },
  {
    id: "S-1002",
    date: daysAgo(1),
    cashierId: "u2",
    cashierName: "Manager User",
    customerId: "c2",
    customerName: "Sarah Haddad",
    lines: [
      {
        productId: "p3",
        name: "Keyboard",
        quantity: 1,
        unitPrice: 4200,
        total: 4200,
      },
    ],
    subtotal: 4200,
    total: 4200,
  },
  {
    id: "S-1001",
    date: daysAgo(3),
    cashierId: "u3",
    cashierName: "Cashier User",
    customerId: "c1",
    customerName: "Amine Benali",
    lines: [
      {
        productId: "p8",
        name: "Backpack",
        quantity: 1,
        unitPrice: 6500,
        total: 6500,
      },
    ],
    subtotal: 6500,
    total: 6500,
  },
];

export const STORAGE = {
  users: "mini-pos-users",
  products: "mini-pos-products",
  customers: "mini-pos-customers",
  sales: "mini-pos-sales",
  session: "mini-pos-session",
  settings: "mini-pos-settings",
};

export function load<T>(key: string, fallback: T): T {
  if (typeof window === "undefined") return fallback;
  const raw = localStorage.getItem(key);
  if (!raw) {
    localStorage.setItem(key, JSON.stringify(fallback));
    return fallback;
  }
  try {
    return JSON.parse(raw) as T;
  } catch {
    return fallback;
  }
}

export function save<T>(key: string, value: T) {
  localStorage.setItem(key, JSON.stringify(value));
}
