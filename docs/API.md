# API

Base URL: `http://localhost:5080`. All payloads are JSON with camelCase field names.

## Conventions

- Authentication is JWT bearer: `Authorization: Bearer <token>`. Roles are `Admin`, `Manager`,
  and `User`. Every route requires a token unless marked anonymous. A route listing roles rejects
  every other role with 403.
- Money is `decimal` in C#, `numeric(14,3)` in the database, and a JSON number with up to three
  decimals. There is no tax and no VAT anywhere in this API.
- Ids are `int`. Timestamps are UTC ISO 8601, for example `2026-09-13T10:24:00Z`.
- Errors return `{ "error": "message" }` with 400 for validation, 401 without a valid token,
  403 for the wrong role, 404 when the id does not exist.
- List endpoints are paged: query `page` (default 1) and `pageSize` (default 20, max 100).
  They return `{ "items": [...], "page": 1, "pageSize": 20, "total": 0 }`.

## Roles

| Area | Admin | Manager | User |
| --- | --- | --- | --- |
| Settings and users | yes | no | no |
| Products and categories, write | yes | yes | no |
| Products and categories, read | yes | yes | yes |
| Customers, write | yes | yes | yes |
| Customers, delete | yes | yes | no |
| Sales, create and read | yes | yes | yes |
| Returns | yes | yes | no |
| Dashboard | yes | yes | no |

## Auth

### POST /api/auth/login

Anonymous.

Request `LoginRequest`: `username` string, `password` string.

Response 200 `LoginResponse`: `token` string, `expiresAt` datetime, `user` `UserResponse`.

401 when the username or password is wrong, or the user is inactive.

### GET /api/auth/me

Response 200 `UserResponse`.

## Users

Admin only.

`UserResponse`: `id` int, `username` string, `fullName` string, `role` string, `isActive` bool.

### GET /api/users

Query: `search` string, `page` int, `pageSize` int. `search` matches `username` or `fullName`.

Response 200 paged `UserResponse`.

### POST /api/users

Request `CreateUserRequest`: `username` string, `fullName` string, `password` string,
`role` string, `isActive` bool.

Response 201 `UserResponse`. 400 when the username is already used, the role is not one of
`Admin`, `Manager`, `User`, or the password is shorter than 6 characters.

### PUT /api/users/{id}

Request `UpdateUserRequest`: `fullName` string, `role` string, `isActive` bool.

Response 200 `UserResponse`. 400 when this would leave no active admin.

### PUT /api/users/{id}/password

Request `ChangePasswordRequest`: `newPassword` string.

Response 204.

### DELETE /api/users/{id}

Response 204. 400 when the user has sales or is the last active admin; deactivate instead.

## Settings

One row for the whole shop.

`SettingsResponse`: `language` string, `theme` string, `lowStockThreshold` int.

`language` is one of `en`, `fr`, `ar`. `theme` is one of `light`, `dark`.

### GET /api/settings

Any authenticated user; the frontend reads the language and theme at startup.

Response 200 `SettingsResponse`.

### PUT /api/settings

Admin. Request `UpdateSettingsRequest`: `language` string, `theme` string,
`lowStockThreshold` int.

Response 200 `SettingsResponse`. 400 when the language or theme is outside the list above, or
`lowStockThreshold` is negative.

## Categories

`CategoryResponse`: `id` int, `name` string, `isActive` bool.

### GET /api/categories

Response 200 `CategoryResponse[]`. Not paged.

### POST /api/categories

Admin, Manager. Request `CreateCategoryRequest`: `name` string.

Response 201 `CategoryResponse`. 400 when the name is empty or already used.

### PUT /api/categories/{id}

Admin, Manager. Request `UpdateCategoryRequest`: `name` string, `isActive` bool.

Response 200 `CategoryResponse`.

### DELETE /api/categories/{id}

Admin, Manager. Response 204. 400 when products still reference the category.

## Products

`ProductResponse`: `id` int, `sku` string, `name` string, `categoryId` int, `categoryName` string,
`price` decimal, `cost` decimal, `stock` int, `isActive` bool.

### GET /api/products

Query: `search` string, `categoryId` int, `activeOnly` bool, `page` int, `pageSize` int.
`search` matches `sku` or `name`, case insensitive.

Response 200 paged `ProductResponse`.

### GET /api/products/{id}

Response 200 `ProductResponse`.

### POST /api/products

Admin, Manager. Request `CreateProductRequest`: `sku` string, `name` string, `categoryId` int,
`price` decimal, `cost` decimal, `stock` int, `isActive` bool.

Response 201 `ProductResponse`. 400 when the sku is already used, the category does not exist,
or `price`, `cost`, or `stock` is negative.

### PUT /api/products/{id}

Admin, Manager. Request `UpdateProductRequest`: same fields as `CreateProductRequest`.

Response 200 `ProductResponse`. `stock` here is a correction, not a delta.

### DELETE /api/products/{id}

Admin, Manager. Response 204. 400 when the product appears on a sale; deactivate it instead.

## Customers

`CustomerResponse`: `id` int, `fullName` string, `phone` string, `email` string, `note` string,
`createdAt` datetime.

### GET /api/customers

Query: `search` string, `page` int, `pageSize` int. `search` matches `fullName` or `phone`.

Response 200 paged `CustomerResponse`.

### GET /api/customers/{id}

Response 200 `CustomerResponse`.

### POST /api/customers

Request `CreateCustomerRequest`: `fullName` string, `phone` string, `email` string, `note` string.

Response 201 `CustomerResponse`. 400 when `fullName` is empty.

### PUT /api/customers/{id}

Request `UpdateCustomerRequest`: same fields as `CreateCustomerRequest`.

Response 200 `CustomerResponse`.

### DELETE /api/customers/{id}

Admin, Manager. Response 204. 400 when the customer has sales.

## Sales

`paymentMethod` is one of `cash`, `card`, `izipay`. `status` is one of `completed`,
`partiallyReturned`, `returned`. A sale is never deleted and never voided; it is returned.

`SaleItemResponse`: `id` int, `productId` int, `sku` string, `productName` string,
`quantity` int, `unitPrice` decimal, `lineTotal` decimal, `returnedQuantity` int.

`SaleResponse`: `id` int, `number` string, `createdAt` datetime, `customerId` int or null,
`customerName` string or null, `cashierName` string, `items` `SaleItemResponse[]`,
`subtotal` decimal, `discount` decimal, `total` decimal, `paidAmount` decimal,
`changeAmount` decimal, `refundedAmount` decimal, `paymentMethod` string, `status` string.

`SaleListItemResponse`: `id` int, `number` string, `createdAt` datetime,
`customerName` string or null, `itemCount` int, `total` decimal, `paymentMethod` string,
`status` string.

### GET /api/sales

Query: `from` date, `to` date, `customerId` int, `status` string, `page` int, `pageSize` int.
`from` and `to` are inclusive and filter on `createdAt`.

Response 200 paged `SaleListItemResponse`.

### GET /api/sales/{id}

Response 200 `SaleResponse`.

### POST /api/sales

Request `CreateSaleRequest`: `customerId` int or null, `paymentMethod` string,
`discount` decimal, `paidAmount` decimal, `items` `CreateSaleItemRequest[]`.

`CreateSaleItemRequest`: `productId` int, `quantity` int, `unitPrice` decimal.

Response 201 `SaleResponse`.

`subtotal` is the sum of `lineTotal`. `total` is `subtotal - discount`. `changeAmount` is
`paidAmount - total`, and is 0 for `card` and `izipay`.

400 when `items` is empty, a `quantity` is not positive, a product is missing or inactive,
`discount` is larger than `subtotal`, or `paidAmount` is smaller than `total` for `cash`.
Stock is checked and decremented inside the same transaction as the insert, so two cashiers
cannot sell the last unit twice. 400 when stock is insufficient.

### POST /api/sales/{id}/return

Admin, Manager.

Request `CreateReturnRequest`: `reason` string, `items` `ReturnItemRequest[]`.

`ReturnItemRequest`: `saleItemId` int, `quantity` int.

Response 200 `SaleResponse`.

Returning all remaining quantity on every line sets `status` to `returned`, anything less sets
`partiallyReturned`. The returned quantity goes back to stock in the same transaction that
updates the sale. `refundedAmount` grows by `quantity * unitPrice` of the returned lines and
ignores the sale discount.

400 when `items` is empty, a `quantity` is not positive, a `saleItemId` does not belong to this
sale, the quantity is larger than `quantity - returnedQuantity` on that line, or the sale is
already fully returned.

## Dashboard

Admin, Manager. Every dashboard route accepts `from` date and `to` date, inclusive, defaulting to
the current day.

### GET /api/dashboard/summary

Response 200 `DashboardSummaryResponse`: `salesCount` int, `salesTotal` decimal,
`averageSale` decimal, `itemsSold` int, `lowStockCount` int.

Returned quantities are excluded from `itemsSold`, and `salesTotal` is net of `refundedAmount`.
`lowStockCount` counts active products whose `stock` is at or below `lowStockThreshold` from
settings, and ignores the date range.

### GET /api/dashboard/top-products

Query: `limit` int, default 5, max 50.

Response 200 `TopProductResponse[]`: `productId` int, `sku` string, `name` string,
`quantitySold` int, `total` decimal. Net of returns, ordered by `quantitySold` descending.

### GET /api/dashboard/sales-by-day

Response 200 `SalesByDayResponse[]`: `date` date, `salesCount` int, `total` decimal.
One entry per day in the range, including days with no sales.

## Entities

| Entity | Columns |
| --- | --- |
| `User` | `Id`, `Username` unique, `PasswordHash`, `FullName`, `Role`, `IsActive` |
| `Category` | `Id`, `Name` unique, `IsActive` |
| `Product` | `Id`, `Sku` unique, `Name`, `CategoryId`, `Price`, `Cost`, `Stock`, `IsActive` |
| `Customer` | `Id`, `FullName`, `Phone`, `Email`, `Note`, `CreatedAt` |
| `Sale` | `Id`, `Number` unique, `CreatedAt`, `CustomerId` nullable, `UserId`, `Subtotal`, `Discount`, `Total`, `PaidAmount`, `ChangeAmount`, `RefundedAmount`, `PaymentMethod`, `Status` |
| `SaleItem` | `Id`, `SaleId`, `ProductId`, `Quantity`, `UnitPrice`, `LineTotal`, `ReturnedQuantity` |
| `Setting` | `Id`, `Language`, `Theme`, `LowStockThreshold` |

`Sale.Number` is `S-yyyyMMdd-0001`, restarting each day. `Setting` holds a single row with
`Id` 1.

## Not in phase 1

The AI service exposes `GET /health` only. AI-assisted sale entry and the izi-pay payment
integration are phase 2; `izipay` is accepted as a `paymentMethod` value and records nothing else.
