"use client";

import { AlertTriangle, BarChart3 } from "@/components/Icons";

export default function DashboardPage() {

  return (
    <>
      <div className="space-y-6">
        <div>
          <h1 className="text-xl font-semibold text-gray-900">Dashboard</h1>
          <p className="mt-1 text-sm text-slate-500">Store overview.</p>
        </div>
      </div>

      <div className="grid gap-4 m-2.5 lg:grid-cols-3">
        {/* Revenue per day chart — placeholder */}
        <div className="bg-white border border-gray-300 rounded overflow-hidden lg:col-span-2">
          <div className="card p-5 h-full flex flex-col">
            <div className="text-sm text-slate-500 mb-3">Revenue per day</div>
            <div className="flex-1 min-h-50 flex items-center justify-center rounded border border-dashed border-gray-300 text-sm text-gray-400">
              <BarChart3 />
            </div>
          </div>
        </div>

        {/* Stats stacked on the right */}
        <div className="flex flex-col gap-4">
          {/* Today's Sales */}
          <div className="bg-white border border-gray-300 rounded overflow-hidden">
            <div className="card p-5">
              <div className="flex items-start justify-between">
                <div>
                  <div className="mt-2 text-2xl font-bold">Today's Sales</div>
                  <div className="text-sm text-slate-500">sales</div>
                </div>
              </div>
            </div>
          </div>

          {/* Total Revenue */}
          <div className="bg-white border border-gray-300 rounded overflow-hidden">
            <div className="card p-5">
              <div className="flex items-start justify-between">
                <div>
                  <div className="mt-2 text-2xl font-bold">Total Revenue</div>
                  <div className="text-sm text-slate-500">total</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Low Stock */}
      <div className="bg-white border border-gray-300 rounded overflow-hidden m-2.5">
        <div className="card p-5">
          <div className="flex items-start justify-between">
            <div>
              <div className="mt-2 text-2xl font-bold">Low Stock</div>
              <div className="mt-1 text-sm text-slate-500">
                At or below threshold
              </div>
              <div className="mt-2 text-2xl font-semibold">Products</div>
            </div>
            <div className="rounded-xl bg-blue-50 p-3 text-blue-600 dark:bg-blue-950/50 dark:text-blue-300">
              <AlertTriangle />
            </div>
          </div>
        </div>
      </div>

      {/* Top Selling Products */}
      <div className="bg-white border border-gray-300 rounded overflow-hidden m-2.5">
        <div className="card p-5">
          <div className="flex items-start justify-between">
            <div>
              <div className="mt-2 text-2xl font-bold">
                Top Selling Products
              </div>
              <div className="text-sm text-slate-500">
                Last week's bestsellers
              </div>
              <div className="mt-2 text-2xl font-semibold">Products</div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
