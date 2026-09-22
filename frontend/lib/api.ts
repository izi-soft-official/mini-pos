import type {
  Category,
  Customer,
  DashboardSummary,
  PagedResponse,
  Product,
  Sale,
  SaleListItem,
  SalesByDay,
  Settings,
  TopProduct,
  User,
} from "./types";

const API_URL = (
  process.env.NEXT_PUBLIC_API_URL || "http://localhost:5080/api"
).replace(/\/$/, "");

export function getToken() {
  return typeof window === "undefined"
    ? null
    : localStorage.getItem("mini-pos-token");
}

export function clearAuth() {
  if (typeof window !== "undefined") {
    localStorage.removeItem("mini-pos-token");
    localStorage.removeItem("mini-pos-user");
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();

  const headers = new Headers(options.headers);

  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const url = `${API_URL}${path}`;

  console.log("API REQUEST:", {
    method: options.method || "GET",
    url,
    path,
    hasToken: !!token,
  });

  const r = await fetch(url, {
    ...options,
    headers,
    cache: "no-store",
  });

  console.log("API RESPONSE:", {
    status: r.status,
    url,
  });

  if (r.status === 401) {
    clearAuth();

    if (
      typeof window !== "undefined" &&
      !location.pathname.startsWith("/login")
    ) {
      location.href = "/login";
    }
  }

  if (!r.ok) {
    let msg = `Request failed (${r.status})`;

    try {
      const d = await r.json();
      msg = d.error || d.title || d.message || msg;
    } catch {}

    throw new Error(`${msg} | ${options.method || "GET"} ${url}`);
  }

  if (r.status === 204) {
    return undefined as T;
  }

  return r.json();
}

export const auth = {
  login: (username: string, password: string) =>
    request<{ token: string; expiresAt: string; user: User }>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password }),
    }),
  me: () => request<User>("/auth/me"),
};

export const categories = {
  list: () => request<Category[]>("/categories"),
  create: (name: string) =>
    request<Category>("/categories", {
      method: "POST",
      body: JSON.stringify({ name }),
    }),
  update: (id: number, data: { name: string; isActive: boolean }) =>
    request<Category>(`/categories/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  remove: (id: number) =>
    request<void>(`/categories/${id}`, { method: "DELETE" }),
};

export const products = {
  list: (p?: {
    search?: string;
    categoryId?: number;
    activeOnly?: boolean;
    page?: number;
    pageSize?: number;
  }) => {
    const q = new URLSearchParams();
    if (p?.search) q.set("search", p.search);
    if (p?.categoryId) q.set("categoryId", String(p.categoryId));
    if (p?.activeOnly !== undefined) q.set("activeOnly", String(p.activeOnly));
    q.set("page", String(p?.page ?? 1));
    q.set("pageSize", String(p?.pageSize ?? 100));
    return request<PagedResponse<Product>>(`/products?${q}`);
  },
  get: (id: number) => request<Product>(`/products/${id}`),
  create: (data: Omit<Product, "id" | "categoryName">) =>
    request<Product>("/products", {
      method: "POST",
      body: JSON.stringify(data),
    }),
  update: (id: number, data: Omit<Product, "id" | "categoryName">) =>
    request<Product>(`/products/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  remove: (id: number) =>
    request<void>(`/products/${id}`, { method: "DELETE" }),
};

export const customers = {
  list: (search = "") => {
    const q = new URLSearchParams({ page: "1", pageSize: "100" });
    if (search) q.set("search", search);
    return request<PagedResponse<Customer>>(`/customers?${q}`);
  },
  get: (id: number) => request<Customer>(`/customers/${id}`),
  create: (data: Omit<Customer, "id" | "createdAt">) =>
    request<Customer>("/customers", {
      method: "POST",
      body: JSON.stringify(data),
    }),
  update: (id: number, data: Omit<Customer, "id" | "createdAt">) =>
    request<Customer>(`/customers/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  remove: (id: number) =>
    request<void>(`/customers/${id}`, { method: "DELETE" }),
};

export const sales = {
  list: (p?: {
    from?: string;
    to?: string;
    customerId?: number;
    status?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const q = new URLSearchParams({
      page: String(p?.page ?? 1),
      pageSize: String(p?.pageSize ?? 100),
    });
    if (p?.from) q.set("from", p.from);
    if (p?.to) q.set("to", p.to);
    if (p?.customerId) q.set("customerId", String(p.customerId));
    if (p?.status) q.set("status", p.status);
    return request<PagedResponse<SaleListItem>>(`/sales?${q}`);
  },
  get: (id: number) => request<Sale>(`/sales/${id}`),
  create: (data: {
    customerId: number | null;
    paymentMethod: string;
    discount: number;
    paidAmount: number;
    items: { productId: number; quantity: number; unitPrice: number }[];
  }) => request<Sale>("/sales", { method: "POST", body: JSON.stringify(data) }),
  returnSale: (
    id: number,
    data: { reason: string; items: { saleItemId: number; quantity: number }[] },
  ) =>
    request<Sale>(`/sales/${id}/return`, {
      method: "POST",
      body: JSON.stringify(data),
    }),
};

export const dashboard = {
  summary: () => request<DashboardSummary>("/dashboard/summary"),
  topProducts: (from?: string, to?: string) => {
    const q = new URLSearchParams();
    if (from) q.set("from", from);
    if (to) q.set("to", to);
    return request<TopProduct[]>(`/dashboard/top-products?${q}`);
  },
  salesByDay: (from?: string, to?: string) => {
    const q = new URLSearchParams();
    if (from) q.set("from", from);
    if (to) q.set("to", to);
    return request<SalesByDay[]>(`/dashboard/sales-by-day?${q}`);
  },
};

export const users = {
  list: () => request<PagedResponse<User>>("/users?page=1&pageSize=100"),
  create: (data: {
    username: string;
    fullName: string;
    password: string;
    role: string;
    isActive: boolean;
  }) => request<User>("/users", { method: "POST", body: JSON.stringify(data) }),
  update: (
    id: number,
    data: { fullName: string; role: string; isActive: boolean },
  ) =>
    request<User>(`/users/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  password: (id: number, newPassword: string) =>
    request<void>(`/users/${id}/password`, {
      method: "PUT",
      body: JSON.stringify({ newPassword }),
    }),
  remove: (id: number) => request<void>(`/users/${id}`, { method: "DELETE" }),
};

export const settings = {
  get: () => request<Settings>("/settings"),
  update: (data: Settings) =>
    request<Settings>("/settings", {
      method: "PUT",
      body: JSON.stringify(data),
    }),
};

type AiProductInfo = {
  id: number;
  sku: string;
  name: string;
  category: string;
  price: number;
  stock: number;
  isActive: boolean;
};

export const ai = {
  parseSale: (text: string) =>
    request<{
      items: {
        productId: number;
        sku: string;
        name: string;
        quantity: number;
        availableStock: number;
        insufficientStock: boolean;
      }[];
      unmatched: string[];
      stockWarnings: string[];
    }>("/ai/parse-sale", {
      method: "POST",
      body: JSON.stringify({ text }),
    }),

  assistant: (text: string) =>
    request<{
      intent: string;
      message: string;

      requiresConfirmation: boolean;

      saleCreated: boolean;

      saleId?: number;

      saleNumber?: string;

      preview?: {
        customerId?: number;
        customerName?: string;

        paymentMethod: string;

        subtotal: number;
        discount: number;
        total: number;

        paidAmount: number;
        changeAmount: number;

        items: {
          productId: number;
          sku: string;
          name: string;
          quantity: number;
          unitPrice: number;
          lineTotal: number;

          availableStock: number;
          insufficientStock: boolean;
        }[];
      };

      unmatched: string[];

      warnings: string[];

      product?: AiProductInfo;

      products?: AiProductInfo[];
    }>("/ai/assistant", {
      method: "POST",
      body: JSON.stringify({ text }),
    }),
};