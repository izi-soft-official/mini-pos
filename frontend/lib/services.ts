import * as mock from "./mock";
import { api, type Paged, type ProductResponse, type CategoryResponse } from "./api";
import type { CreateProductInput, LoginInput, LoginResponse } from "./mock";

const useMock = process.env.NEXT_PUBLIC_USE_MOCK === "true";


const real = {
  getCategories: () => api.get<CategoryResponse[]>("/api/categories"),

  getProducts: (p: { search?: string; categoryId?: number | null; page?: number }) => {
    const q = new URLSearchParams();
    if (p.search) {
      q.set("search", p.search);
    }
    if (p.categoryId != null) {
      q.set("categoryId", String(p.categoryId));
    }
    q.set("page", String(p.page ?? 1));
    return api.get<Paged<ProductResponse>>(`/api/products?${q}`);
  },

  createProduct: (input: CreateProductInput) =>
    api.post<ProductResponse>("/api/products", input),

  login: (input: LoginInput) => api.post<LoginResponse>("/api/auth/login", input),
};

export const { getCategories, getProducts, createProduct, login } = useMock ? mock : real;