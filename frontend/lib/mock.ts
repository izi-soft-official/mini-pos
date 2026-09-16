import { Paged, ProductResponse, CategoryResponse } from "./api";

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

const CATEGORIES = [
  { id: 1, name: "Beverages", isActive: true },
  { id: 2, name: "Snacks", isActive: true },
  { id: 3, name: "Dairy", isActive: true },
  { id: 4, name: "Bakery", isActive: true },
  { id: 5, name: "Household", isActive: true },
  { id: 6, name: "Stationery", isActive: true },
];

let PRODUCTS: ProductResponse[] = [
  {
    id: 1,
    sku: "BEV-1001",
    name: "Ifri still water 1.5L",
    categoryId: 1,
    categoryName: "Beverages",
    price: 4500,
    cost: 3200,
    stock: 312,
    isActive: true,
  },
  {
    id: 2,
    sku: "BEV-1002",
    name: "Hamoud Boualem 1L",
    categoryId: 1,
    categoryName: "Beverages",
    price: 12000,
    cost: 8000,
    stock: 86,
    isActive: true,
  },
  {
    id: 3,
    sku: "SNK-2043",
    name: "Salted peanuts 200g",
    categoryId: 2,
    categoryName: "Snacks",
    price: 18000,
    cost: 12000,
    stock: 7,
    isActive: true,
  },
  {
    id: 4,
    sku: "SNK-2044",
    name: "Chocolate wafer bar",
    categoryId: 2,
    categoryName: "Snacks",
    price: 6000,
    cost: 4000,
    stock: 240,
    isActive: true,
  },
  {
    id: 5,
    sku: "DRY-3010",
    name: "Candia UHT milk 1L",
    categoryId: 3,
    categoryName: "Dairy",
    price: 15000,
    cost: 10000,
    stock: 54,
    isActive: true,
  },
  {
    id: 6,
    sku: "DRY-3011",
    name: "Plain yoghurt 4-pack",
    categoryId: 3,
    categoryName: "Dairy",
    price: 21000,
    cost: 15000,
    stock: 3,
    isActive: true,
  },
  {
    id: 7,
    sku: "BAK-4002",
    name: "Baguette",
    categoryId: 4,
    categoryName: "Bakery",
    price: 1500,
    cost: 1000,
    stock: 120,
    isActive: true,
  },
  {
    id: 8,
    sku: "BAK-4009",
    name: "Croissant, plain",
    categoryId: 4,
    categoryName: "Bakery",
    price: 4000,
    cost: 2500,
    stock: 0,
    isActive: false,
  },
  {
    id: 9,
    sku: "HSE-5120",
    name: "Dish soap 750ml",
    categoryId: 5,
    categoryName: "Household",
    price: 29000,
    cost: 20000,
    stock: 41,
    isActive: true,
  },
  {
    id: 10,
    sku: "HSE-5121",
    name: "Sponge scourer, 3-pack",
    categoryId: 5,
    categoryName: "Household",
    price: 9500,
    cost: 6000,
    stock: 18,
    isActive: true,
  },
  {
    id: 11,
    sku: "STA-6001",
    name: "Notebook A5, 96 pages",
    categoryId: 6,
    categoryName: "Stationery",
    price: 13000,
    cost: 9000,
    stock: 64,
    isActive: true,
  },
  {
    id: 12,
    sku: "STA-6014",
    name: "Blue ballpoint pen",
    categoryId: 6,
    categoryName: "Stationery",
    price: 2500,
    cost: 1500,
    stock: 9,
    isActive: false,
  },
];

export type CreateProductInput = {
    sku: string;
    name: string;
    categoryId: number;
    price: number;
    cost: number;
    stock: number;
    isActive: boolean;
};

const PAGE_SIZE = 6;

export async function getCategories(): Promise<CategoryResponse[]> {
    await sleep(500);
    return CATEGORIES;
}

export async function getProducts(params: {
    search?: string;
    categoryId?: number | null;
    page?: number;
    pageSize?: number;
}): Promise<Paged<ProductResponse>> {

    await sleep(500);

    const { search = "", categoryId = null, page = 1, pageSize = PAGE_SIZE } = params;

    const filteredProducts = PRODUCTS.filter((product) => {
        const term = search.toLowerCase();
        const matchesSearch =
            product.sku.toLowerCase().includes(term) ||
            product.name.toLowerCase().includes(term);
        const matchesCategory =
            categoryId === null || product.categoryId === categoryId;
        return matchesSearch && matchesCategory;
    });

    const data = {
        items: filteredProducts.slice((page - 1) * pageSize, page * pageSize),
        page,
        pageSize,
        total: filteredProducts.length, 
    };
    return data;

}

export async function createProduct(input: CreateProductInput): Promise<ProductResponse> {
        await sleep(500);

        const dublicate = PRODUCTS.some(
            (product)=> product.sku.toLowerCase() === input.sku.toLowerCase()
        );
        if (dublicate) {
            throw new Error("Product with the same SKU already exists.");
        }

        const category = CATEGORIES.find((c) => c.id === input.categoryId);
        if (!category) {
            throw new Error("unknown Category.");
        }
        const created: ProductResponse = {
            ...input,
            id: Math.max(...PRODUCTS.map((p) => p.id)) + 1,
            categoryName: category.name,
        };

        PRODUCTS =[...PRODUCTS, created];
        return created;
    
      }

      export type LoginInput = {
          username: string;
          password: string;
      };

      export type LoginResponse = {
          token: string;
          username: string;
      };

      export async function login(input: LoginInput): Promise<LoginResponse> {
          await sleep(500);

          if(input.username !== "admin" || input.password !== "admin123") {
              throw new Error("Invalid username or password.");
          }

          return {
              token: "mock-token"+Date.now(),
              username: input.username,
          };
      }