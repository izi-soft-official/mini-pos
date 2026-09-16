export type Role = "Admin" | "Manager" | "Cashier";

export type User = {
  id: number;
  username: string;
  fullName: string;
  role: Role;
  isActive: boolean;
};

export type Category = { 
    id: number; 
    name: string; 
    isActive: boolean 
};

export type Product = {
  id: number;
  sku: string;
  name: string;
  categoryId: number;
  categoryName: string;
  price: number;
  cost: number;
  stock: number;
  isActive: boolean;
};

export type Customer = {
  id: number;
  fullName: string;
  phone: string;
  email: string;
  note: string;
  createdAt: string;
};

export type SaleItem = {
  id: number;
  productId: number;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  returnedQuantity: number;
};

export type Sale = {
  id: number;
  number: string;
  createdAt: string;
  customerId: number | null;
  customerName: string | null;
  cashierName: string;
  items: SaleItem[];
  subtotal: number;
  discount: number;
  total: number;
  paidAmount: number;
  changeAmount: number;
  refundedAmount: number;
  paymentMethod: string;
  status: string;
};

export type SaleListItem = {
  id: number;
  number: string;
  createdAt: string;
  customerName: string | null;
  itemCount: number;
  total: number;
  paymentMethod: string;
  status: string;
};

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export type Settings = {
  language: string;
  theme: string;
  lowStockThreshold: number;
};

export type DashboardSummary = {
  salesCount: number;
  salesTotal: number;
  averageSale: number;
  itemsSold: number;
  lowStockCount: number;
};

export type TopProduct = {
  productId: number;
  sku: string;
  name: string;
  quantitySold: number;
  total: number;
};

export type SalesByDay = { 
    date: string; 
    salesCount: number; 
    total: number 
};
