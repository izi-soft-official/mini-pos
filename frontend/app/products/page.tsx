"use client";

import { formatPrice } from "@/lib/format";
import Button from "@/components/Button";
import { useEffect, useState } from "react";
import { getProducts, getCategories, createProduct } from "@/lib/services";
import type { Paged, ProductResponse, CategoryResponse } from "@/lib/api";

export default function ProductsPage() {
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [data, setData] = useState<Paged<ProductResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [isSaving, setIsSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [page, setPage] = useState(1);

  const [form, setForm] = useState({
    sku: "",
    name: "",
    categoryId: 1,
    price: "",
    stock: "",
    isActive: true,
  });

  useEffect(() => {
    let ignore = false;
    getCategories()
      .then((result) => {
        if (!ignore) setCategories(result);
      })
      .catch(() => {
        if (!ignore) setCategories([]);
      });
    return () => {
      ignore = true;
    };
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    let ignore = false;
    setIsLoading(true);
    setError(null);

    getProducts({ search: debouncedSearch, categoryId, page })
      .then((result) => {
        if (ignore) return;
        setData(result);
        setIsLoading(false);
      })
      .catch((err: unknown) => {
        if (ignore) return;
        setError(err instanceof Error ? err.message : "Failed to load products");
        setIsLoading(false);
      });

    return () => {
      ignore = true;
    };
  }, [debouncedSearch, categoryId, page, refreshKey]);

  const items = data?.items ?? [];
  const total = data?.total ?? 0;
  const pageSize = data?.pageSize ?? 6;
  const pageCount = Math.ceil(total / pageSize);

  const emptyForm = {
    sku: "",
    name: "",
    categoryId: 1,
    price: "",
    stock: "",
    isActive: true,
  };

  function closeModal(){
    setIsModalOpen(false);
    setForm(emptyForm);
    setFormError(null);
  }

   async function handleSave() {
     setFormError(null);

     if (!form.sku.trim() || !form.name.trim()) {
       setFormError("SKU and Name are required.");
       return;
     }

     const price = Number(form.price);
     const stock = Number(form.stock);

     if (!Number.isFinite(price) || price <= 0) {
       setFormError("Price must be positive number.");
       return;
     }

     if (!Number.isInteger(stock) || stock < 0) {
       setFormError("Stock must be real number zero or more.");
       return;
     }

     setIsSaving(true);
     try {
       await createProduct({
         sku: form.sku.trim(),
         name: form.name.trim(),
         categoryId: form.categoryId,
         price: Math.round(price * 100),
         cost: 0,
         stock,
         isActive: form.isActive,
       });
       closeModal();
       setRefreshKey((key) => key + 1);
     } catch (error: unknown) {
       setFormError(error instanceof Error ? error.message : "Failed to save product");
     } finally {
       setIsSaving(false);
     }
   }

  return (
    <>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Products</h1>
          <p className="mt-1 text-sm text-muted">{total} products</p>
        </div>
        <Button onClick={() => setIsModalOpen(true)}>New product</Button>
      </div>

      <div className="mt-6 flex gap-3">
        <input
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
          placeholder="Search products"
          className="w-80 rounded-lg border border-line bg-surface px-4 py-2.5 text-sm placeholder:text-muted focus:border-mint focus:outline-none"
        />

        <select
          value={categoryId ?? ""}
          onChange={(event) => {
            setCategoryId(
              event.target.value === "" ? null : Number(event.target.value),
            );
            setPage(1);
          }}
          className="rounded-lg border border-line bg-surface px-4 py-2.5 text-sm focus:border-mint focus:outline-none"
        >
          <option value="">All categories</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </div>

      {error && (
        <p className="mt-4 rounded-lg border border-red-900 bg-red-950 px-4 py-3 text-sm text-red-300">
          {error}
        </p>
      )}

      <div className="mt-4 overflow-hidden rounded-xl border border-line">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-line bg-surface-2 text-muted">
              <th className="px-4 py-3 text-left font-medium">SKU</th>
              <th className="px-4 py-3 text-left font-medium">Name</th>
              <th className="px-4 py-3 text-left font-medium">Category</th>
              <th className="px-4 py-3 text-right font-medium">Price</th>
              <th className="px-4 py-3 text-right font-medium">Stock</th>
              <th className="px-4 py-3 text-left font-medium">Active</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr className="bg-surface">
                <td colSpan={6} className="px-4 py-8 text-center text-muted">
                  Loading...
                </td>
              </tr>
            )}

            {!isLoading && items.length === 0 && (
              <tr className="bg-surface">
                <td colSpan={6} className="px-4 py-8 text-center text-muted">
                  No products found.
                </td>
              </tr>
            )}

            {!isLoading &&
              items.map((product) => (
                <tr
                  key={product.id}
                  className="border-b border-line bg-surface last:border-b-0 hover:bg-surface-2"
                >
                  <td className="px-4 py-3 text-muted">{product.sku}</td>
                  <td className="px-4 py-3 font-medium">{product.name}</td>
                  <td className="px-4 py-3">{product.categoryName}</td>
                  <td className="px-4 py-3 text-right">
                    {formatPrice(product.price)}
                  </td>
                  <td className="px-4 py-3 text-right">{product.stock}</td>
                  <td className="px-4 py-3">
                    <span
                      className={
                        product.isActive
                          ? "rounded-full bg-mint-soft px-2.5 py-1 text-xs font-semibold text-mint"
                          : "rounded-full bg-surface-2 px-2.5 py-1 text-xs font-semibold text-muted"
                      }
                    >
                      {product.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>

        {!isLoading && pageCount > 1 && (
          <div className="flex items-center justify-between border-t border-line bg-surface px-4 py-3 text-sm">
            <span className="text-muted">
              showing {(page - 1) * pageSize + 1} –{" "}
              {Math.min(page * pageSize, total)} of {total}
            </span>
            <div className="flex gap-1">
              {Array.from({ length: pageCount }, (_, index) => index + 1).map(
                (number) => (
                  <button
                    key={number}
                    onClick={() => setPage(number)}
                    className={
                      number === page
                        ? "h-8 w-8 rounded-md bg-mint font-semibold text-ink"
                        : "h-8 w-8 rounded-md border border-line text-muted hover:bg-surface-2"
                    }
                  >
                    {number}
                  </button>
                ),
              )}
            </div>
          </div>
        )}
      </div>

      {isModalOpen && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-6">
          <div className="w-full max-w-lg rounded-xl border border-line bg-surface p-6">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">New product</h2>
              <button
                onClick={closeModal}
                disabled={isSaving}
                aria-label="Close"
                className="text-muted hover:text-white disabled:opacity-60"
              >
                ✕
              </button>
            </div>

            <div className="mt-5 flex flex-col gap-4">
              <div>
                <label htmlFor="sku" className="mb-1.5 block text-sm text-muted">
                  SKU
                </label>
                <input
                  id="sku"
                  value={form.sku}
                  onChange={(event) =>
                    setForm({ ...form, sku: event.target.value })
                  }
                  className="w-full rounded-lg border border-line bg-surface-2 px-3 py-2 text-sm focus:border-mint focus:outline-none"
                />
              </div>

              <div>
                <label htmlFor="name" className="mb-1.5 block text-sm text-muted">
                  Name
                </label>
                <input
                  id="name"
                  value={form.name}
                  onChange={(event) =>
                    setForm({ ...form, name: event.target.value })
                  }
                  className="w-full rounded-lg border border-line bg-surface-2 px-3 py-2 text-sm focus:border-mint focus:outline-none"
                />
              </div>

              <div>
                <label
                  htmlFor="category"
                  className="mb-1.5 block text-sm text-muted"
                >
                  Category
                </label>
                <select
                  id="category"
                  value={form.categoryId}
                  onChange={(event) =>
                    setForm({ ...form, categoryId: Number(event.target.value) })
                  }
                  className="w-full rounded-lg border border-line bg-surface-2 px-3 py-2 text-sm focus:border-mint focus:outline-none"
                >
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex gap-4">
                <div className="flex-1">
                  <label
                    htmlFor="price"
                    className="mb-1.5 block text-sm text-muted"
                  >
                    Price
                  </label>
                  <input
                    id="price"
                    type="number"
                    value={form.price}
                    onChange={(event) =>
                      setForm({ ...form, price: event.target.value })
                    }
                    className="w-full rounded-lg border border-line bg-surface-2 px-3 py-2 text-sm focus:border-mint focus:outline-none"
                  />
                </div>
                <div className="flex-1">
                  <label
                    htmlFor="stock"
                    className="mb-1.5 block text-sm text-muted"
                  >
                    Stock
                  </label>
                  <input
                    id="stock"
                    type="number"
                    value={form.stock}
                    onChange={(event) =>
                      setForm({ ...form, stock: event.target.value })
                    }
                    className="w-full rounded-lg border border-line bg-surface-2 px-3 py-2 text-sm focus:border-mint focus:outline-none"
                  />
                </div>
              </div>

              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(event) =>
                    setForm({ ...form, isActive: event.target.checked })
                  }
                  className="h-4 w-4 accent-mint"
                />
                Active
              </label>
            </div>

                        {formError && (
              <p className="mt-4 rounded-lg border border-red-900 bg-red-950 px-3 py-2 text-sm text-red-300">
                {formError}
              </p>
            )}

            <div className="mt-6 flex justify-end gap-2">
              <Button onClick={closeModal} variant="secondary" disabled={isSaving}>
                Cancel
              </Button>
              <Button onClick={handleSave} disabled={isSaving}>
                {isSaving ? "Saving..." : "Save"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}