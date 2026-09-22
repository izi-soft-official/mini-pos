using System.Collections.Concurrent;
using System.Security.Claims;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniPos.Api.Data;
using MiniPos.Api.Dtos;

namespace MiniPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;

    // Pending sale per authenticated cashier/user.
    private static readonly ConcurrentDictionary<int, PendingSaleState>
        PendingSales = new();

    public AiController(
        AppDbContext db,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _http = httpClientFactory.CreateClient("ai-service");
    }


    // ============================================================
    // EXISTING PARSE SALE
    // ============================================================

    [HttpPost("parse-sale")]
    public async Task<ActionResult<ParseSaleResponse>> ParseSale(
        ParseSaleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new
            {
                error = "Text is required."
            });

        var catalog = await _db.Products
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Sku,
                p.Name
            })
            .ToListAsync();

        HttpResponseMessage aiResponse;

        try
        {
            aiResponse = await _http.PostAsJsonAsync(
                "/parse-sale",
                new
                {
                    text = request.Text,
                    catalog
                });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                502,
                new
                {
                    error = "AI service is unavailable."
                });
        }

        if (!aiResponse.IsSuccessStatusCode)
        {
            return StatusCode(
                502,
                new
                {
                    error = "AI service is unavailable."
                });
        }

        var parsed =
            await aiResponse.Content.ReadFromJsonAsync<AiParsedResult>();

        if (parsed is null)
        {
            return StatusCode(
                502,
                new
                {
                    error = "AI service returned an unexpected response."
                });
        }

        var products = await _db.Products
            .Where(p =>
                parsed.Items
                    .Select(i => i.ProductId)
                    .Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var result = new ParseSaleResponse
        {
            Unmatched = parsed.Unmatched
        };

        foreach (var item in parsed.Items)
        {
            if (!products.TryGetValue(
                    item.ProductId,
                    out var product))
                continue;

            var quantity = item.Quantity < 1
                ? 1
                : item.Quantity;

            var insufficientStock =
                quantity > product.Stock;

            result.Items.Add(new ParsedSaleItem
            {
                ProductId = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Quantity = quantity,
                AvailableStock = product.Stock,
                InsufficientStock = insufficientStock
            });

            if (insufficientStock)
            {
                result.StockWarnings.Add(
                    $"{product.Name}: requested {quantity}, " +
                    $"only {product.Stock} in stock.");
            }
        }

        return result;
    }


    // ============================================================
    // MAIN AI ASSISTANT
    // ============================================================

    [HttpPost("assistant")]
    public async Task<ActionResult<AiAssistantResponse>> Assistant(
        AiAssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new
            {
                error = "Text is required."
            });
        }

        var userId = CurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new
            {
                error = "Invalid authentication token."
            });
        }

        var pending =
            PendingSales.TryGetValue(
                userId.Value,
                out var existingPending)
                ? existingPending
                : null;


        // --------------------------------------------------------
        // LOAD PRODUCTS
        // --------------------------------------------------------

        var catalog = await _db.Products
            .Select(p => new AiCatalogProduct
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Category = p.Category != null ? p.Category.Name : "Uncategorized",
                IsActive = p.IsActive
            })
            .ToListAsync();


        // --------------------------------------------------------
        // LOAD CUSTOMERS
        // --------------------------------------------------------

        var customers = await _db.Customers
            .Select(c => new AiCatalogCustomer
            {
                Id = c.Id,
                Name = c.FullName
            })
            .ToListAsync();


        // --------------------------------------------------------
        // SEND REQUEST TO PYTHON
        // --------------------------------------------------------

        var aiRequest = new
        {
            text = request.Text,

            catalog = catalog,

            customers = customers,

            pendingItems = existingPending?.Items
                .Select(x => new
                {
                    productId = x.ProductId,
                    quantity = x.Quantity
                })
                .ToList() ?? new(),

            pendingCustomerId =
                existingPending?.CustomerId,

            pendingPaymentMethod =
                existingPending?.PaymentMethod,

            pendingDiscount =
                existingPending?.Discount ?? 0m,

            pendingPaidAmount =
                existingPending?.PaidAmount
        };

        HttpResponseMessage aiResponse;

        try
        {
            aiResponse = await _http.PostAsJsonAsync(
                "/assistant",
                aiRequest);
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                502,
                new
                {
                    error = "AI service is unavailable."
                });
        }

        if (!aiResponse.IsSuccessStatusCode)
        {
            var errorBody =
                await aiResponse.Content.ReadAsStringAsync();

            return StatusCode(
                502,
                new
                {
                    error = "AI service failed.",
                    details = errorBody
                });
        }

        var parsed =
            await aiResponse.Content
                .ReadFromJsonAsync<AiPythonResponse>();

        if (parsed is null)
        {
            return StatusCode(
                502,
                new
                {
                    error =
                        "AI service returned an unexpected response."
                });
        }


        // ========================================================
        // CANCEL
        // ========================================================

        if (parsed.Intent == "cancel_sale")
        {
            PendingSales.TryRemove(
                userId.Value,
                out _);

            return Ok(new AiAssistantResponse
            {
                Intent = "cancel_sale",
                Message =
                    "The pending sale has been cancelled. " +
                    "Nothing was added to the database and stock " +
                    "was not changed.",
                RequiresConfirmation = false,
                SaleCreated = false
            });
        }


        // ========================================================
        // CONFIRM
        // ========================================================

        if (parsed.Intent == "confirm_sale")
        {
            if (pending is null)
            {
                return Ok(new AiAssistantResponse
                {
                    Intent = "confirm_sale",
                    Message =
                        "There is no pending sale to confirm.",
                    RequiresConfirmation = false
                });
            }

            return await ConfirmPendingSale(
                userId.Value,
                pending);
        }

        // ========================================================
        // PRODUCT / STOCK ASSISTANT
        // ========================================================

        // Generic natural-language product questions.
        // The Python service interprets the user's wording, while
        // this controller performs the authoritative PostgreSQL query.
        if (parsed.Intent == "product_query")
        {
            return await ProductQuery(parsed);
        }

        if (parsed.Intent == "check_product")
        {
            return await CheckProduct(parsed);
        }


        if (parsed.Intent == "low_stock")
        {
            return await GetLowStockProducts();
        }


        if (parsed.Intent == "search_products")
        {
            return await SearchProducts(parsed);
        }


        // ========================================================
        // UNKNOWN
        // ========================================================

        if (parsed.Intent == "unknown")
        {
            return Ok(new AiAssistantResponse
            {
                Intent = "unknown",
                Message =
                    "I can help you prepare a sale. " +
                    "For example: \"Sell 2 Coca Cola and 1 Chips " +
                    "to Ahmed, cash.\"",
                RequiresConfirmation =
                    pending is not null
            });
        }


        // ========================================================
        // CREATE / MODIFY
        // ========================================================

        if (parsed.Intent != "create_sale" &&
            parsed.Intent != "modify_sale")
        {
            return Ok(new AiAssistantResponse
            {
                Intent = "unknown",
                Message =
                    "I couldn't understand that request."
            });
        }


        // --------------------------------------------------------
        // VALIDATE PRODUCTS
        // --------------------------------------------------------

        if (parsed.Items.Count == 0)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = parsed.Intent,
                Message =
                    "I couldn't identify any products " +
                    "in the request.",
                RequiresConfirmation = false
            });
        }


        var requestedIds = parsed.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await _db.Products
            .Where(p =>
                requestedIds.Contains(p.Id) &&
                p.IsActive)
            .ToDictionaryAsync(p => p.Id);


        var unmatched = new List<string>();

        foreach (var item in parsed.Items)
        {
            if (!products.ContainsKey(item.ProductId))
            {
                unmatched.Add(
                    $"Product ID {item.ProductId}");
            }
        }

        if (parsed.Unmatched.Count > 0)
            unmatched.AddRange(parsed.Unmatched);


        if (unmatched.Count > 0)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = parsed.Intent,
                Message =
                    "I couldn't confidently identify " +
                    "some of the requested products.",
                Unmatched = unmatched,
                RequiresConfirmation = false
            });
        }


        // --------------------------------------------------------
        // MERGE DUPLICATE PRODUCTS
        // --------------------------------------------------------

        var mergedItems = parsed.Items
            .GroupBy(x => x.ProductId)
            .Select(g => new PendingSaleItem
            {
                ProductId = g.Key,
                Quantity = g.Sum(x =>
                    Math.Max(1, x.Quantity))
            })
            .ToList();


        // --------------------------------------------------------
        // CUSTOMER
        // --------------------------------------------------------

        int? customerId = parsed.CustomerId;

        if (customerId is not null)
        {
            var customerExists =
                await _db.Customers
                    .AnyAsync(c =>
                        c.Id == customerId.Value);

            if (!customerExists)
            {
                return Ok(new AiAssistantResponse
                {
                    Intent = parsed.Intent,
                    Message =
                        "I couldn't find that customer.",
                    Warnings =
                    {
                        $"Customer ID {customerId} does not exist."
                    }
                });
            }
        }
        else if (existingPending is not null)
        {
            customerId =
                existingPending.CustomerId;
        }


        // --------------------------------------------------------
        // PAYMENT METHOD
        // --------------------------------------------------------

        var paymentMethod =
            parsed.PaymentMethod?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            paymentMethod =
                existingPending?.PaymentMethod ?? "cash";
        }

        if (paymentMethod != "cash" &&
            paymentMethod != "card" &&
            paymentMethod != "izipay")
        {
            return Ok(new AiAssistantResponse
            {
                Intent = parsed.Intent,
                Message =
                    "Payment method must be cash, card or izipay."
            });
        }


        // --------------------------------------------------------
        // DISCOUNT
        // --------------------------------------------------------

        var discount = parsed.Discount;

        if (parsed.Intent == "modify_sale" &&
            parsed.Discount == 0 &&
            existingPending is not null)
        {
            discount = existingPending.Discount;
        }

        if (discount < 0)
            discount = 0;


        // --------------------------------------------------------
        // PAID AMOUNT
        // --------------------------------------------------------

        decimal? paidAmount =
            parsed.PaidAmount;

        if (paidAmount is null &&
            existingPending is not null)
        {
            paidAmount =
                existingPending.PaidAmount;
        }


        // --------------------------------------------------------
        // BUILD PREVIEW
        // --------------------------------------------------------

        var previewResult =
            await BuildPreview(
                mergedItems,
                customerId,
                paymentMethod,
                discount,
                paidAmount);

        if (!previewResult.Success)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = parsed.Intent,
                Message = previewResult.Error!,
                Warnings = previewResult.Warnings
            });
        }


        // --------------------------------------------------------
        // SAVE PENDING SALE
        // --------------------------------------------------------

        var pendingSale = new PendingSaleState
        {
            CustomerId = customerId,
            PaymentMethod = paymentMethod,
            Discount = discount,
            PaidAmount = previewResult.PaidAmount,
            Items = previewResult.AdjustedItems
        };

        PendingSales[userId.Value] =
            pendingSale;


        // --------------------------------------------------------
        // RESPONSE
        // --------------------------------------------------------

        var message =
            parsed.Intent == "modify_sale"
                ? "I've updated the sale. Review the new preview and send \"confirm\" to complete it."
                : "I've prepared the sale. Review the preview and send \"confirm\" to complete it.";

        return Ok(new AiAssistantResponse
        {
            Intent = parsed.Intent,
            Message = message,
            RequiresConfirmation = true,
            SaleCreated = false,
            Preview = previewResult.Preview,
            Warnings = previewResult.Warnings
        });
    }

    // ============================================================
    //  - GENERIC PRODUCT QUERY
    // ============================================================

    private async Task<ActionResult<AiAssistantResponse>>
        ProductQuery(AiPythonResponse parsed)
    {
        var queryType =
            parsed.QueryType?.Trim().ToLowerInvariant() ?? "search";

        var operation =
            parsed.Operation?.Trim().ToLowerInvariant();

        var field =
            parsed.Field?.Trim().ToLowerInvariant();

        var status =
            parsed.Status?.Trim().ToLowerInvariant();

        var category =
            parsed.Category?.Trim();

        // Start from the real PostgreSQL product table.
        // The AI service only interprets the user's wording;
        // it does not provide prices or stock values.
        var query = _db.Products
            .Include(p => p.Category)
            .AsQueryable();

        // Inactive is explicitly requested.
        // Otherwise product questions operate on active products.
        if (status == "inactive")
        {
            query = query.Where(p => !p.IsActive);
        }
        else
        {
            query = query.Where(p => p.IsActive);
        }

        // Low-stock products.
        if (queryType == "low_stock")
        {
            const int lowStockThreshold = 5;

            var lowStock = await query
                .Where(p => p.Stock <= lowStockThreshold)
                .OrderBy(p => p.Stock)
                .ThenBy(p => p.Name)
                .Take(50)
                .Select(p => new AiProductInfo
                {
                    Id = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    Category = p.Category != null
                        ? p.Category.Name
                        : "Uncategorized",
                    Price = p.Price,
                    Stock = p.Stock,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return ProductListResponse(
                lowStock,
                lowStock.Count == 0
                    ? "No products are currently low in stock."
                    : $"I found {lowStock.Count} product" +
                      $"{(lowStock.Count == 1 ? "" : "s")} with low stock.");
        }

        // Out-of-stock products.
        if (queryType == "out_of_stock")
        {
            var outOfStock = await query
                .Where(p => p.Stock <= 0)
                .OrderBy(p => p.Name)
                .Take(50)
                .Select(p => new AiProductInfo
                {
                    Id = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    Category = p.Category != null
                        ? p.Category.Name
                        : "Uncategorized",
                    Price = p.Price,
                    Stock = p.Stock,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return ProductListResponse(
                outOfStock,
                outOfStock.Count == 0
                    ? "No active products are currently out of stock."
                    : $"I found {outOfStock.Count} out-of-stock product" +
                      $"{(outOfStock.Count == 1 ? "" : "s")}.");
        }

        // One specific product.
        if (queryType == "single" && parsed.ProductId is not null)
        {
            var product = await query
                .Where(p => p.Id == parsed.ProductId.Value)
                .Select(p => new AiProductInfo
                {
                    Id = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    Category = p.Category != null
                        ? p.Category.Name
                        : "Uncategorized",
                    Price = p.Price,
                    Stock = p.Stock,
                    IsActive = p.IsActive
                })
                .FirstOrDefaultAsync();

            if (product is null)
            {
                return Ok(new AiAssistantResponse
                {
                    Intent = "product_query",
                    Message = "I couldn't find that product.",
                    RequiresConfirmation = false,
                    SaleCreated = false,
                    Unmatched = parsed.Unmatched
                });
            }

            return Ok(new AiAssistantResponse
            {
                Intent = "product_query",
                Message = product.Stock > 0
                    ? $"Yes. {product.Name} is currently in stock."
                    : $"{product.Name} is currently out of stock.",
                RequiresConfirmation = false,
                SaleCreated = false,
                Product = product
            });
        }

        // Numeric stock filters, for example:
        // "products with more than 10 in stock".
        if (queryType == "filter" &&
            field == "stock" &&
            parsed.Value is not null)
        {
            var value = parsed.Value.Value;

            query = parsed.Operator?.Trim() switch
            {
                ">" => query.Where(p => p.Stock > value),
                ">=" => query.Where(p => p.Stock >= value),
                "<" => query.Where(p => p.Stock < value),
                "<=" => query.Where(p => p.Stock <= value),
                "=" or "==" => query.Where(p => p.Stock == value),
                _ => query.Where(p => p.Stock == value)
            };
        }

        // Category filter, for example:
        // "show me snacks".
        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryPattern = $"%{category}%";

            query = query.Where(p =>
                p.Category != null &&
                EF.Functions.ILike(
                    p.Category.Name,
                    categoryPattern));
        }

        // Product IDs can be supplied by the AI service after
        // matching a product name from the real catalog.
        if (parsed.ProductIds.Count > 0 &&
            queryType == "search")
        {
            var ids = parsed.ProductIds
                .Distinct()
                .ToList();

            query = query.Where(p => ids.Contains(p.Id));
        }

        // Aggregate queries such as:
        // "most expensive snack"
        // "cheapest product"
        // "product with the most stock"
        if (queryType == "aggregate")
        {
            query = field switch
            {
                "price" when operation == "max" =>
                    query
                        .OrderByDescending(p => p.Price)
                        .ThenBy(p => p.Name),

                "price" when operation == "min" =>
                    query
                        .OrderBy(p => p.Price)
                        .ThenBy(p => p.Name),

                "stock" when operation == "max" =>
                    query
                        .OrderByDescending(p => p.Stock)
                        .ThenBy(p => p.Name),

                "stock" when operation == "min" =>
                    query
                        .OrderBy(p => p.Stock)
                        .ThenBy(p => p.Name),

                _ => query.OrderBy(p => p.Name)
            };
        }
        else if (queryType != "filter")
        {
            query = query.OrderBy(p => p.Name);
        }

        var products = await query
            .Take(queryType == "aggregate" ? 1 : 50)
            .Select(p => new AiProductInfo
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Category = p.Category != null
                    ? p.Category.Name
                    : "Uncategorized",
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive
            })
            .ToListAsync();

        if (products.Count == 0)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = "product_query",
                Message = parsed.Unmatched.Count > 0
                    ? $"I couldn't find a product matching \"{parsed.Unmatched[0]}\"."
                    : "I couldn't find any products matching that request.",
                RequiresConfirmation = false,
                SaleCreated = false,
                Products = new List<AiProductInfo>()
            });
        }

        var message = queryType == "aggregate"
            ? $"I found the matching product: {products[0].Name}."
            : $"I found {products.Count} matching product" +
              $"{(products.Count == 1 ? "" : "s")}.";

        return ProductListResponse(products, message);
    }

    private ActionResult<AiAssistantResponse> ProductListResponse(
        List<AiProductInfo> products,
        string message)
    {
        return Ok(new AiAssistantResponse
        {
            Intent = "product_query",
            Message = message,
            RequiresConfirmation = false,
            SaleCreated = false,
            Products = products
        });
    }


    // ============================================================
    //  - CHECK PRODUCT
    // ============================================================

    private async Task<ActionResult<AiAssistantResponse>>
        CheckProduct(AiPythonResponse parsed)
    {
        if (parsed.ProductId is null)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = "check_product",
                Message =
                    parsed.Unmatched.Count > 0
                        ? $"I couldn't find a product matching \"{parsed.Unmatched[0]}\"."
                        : "I couldn't identify the product you are asking about.",
                RequiresConfirmation = false,
                SaleCreated = false,
                Unmatched = parsed.Unmatched
            });
        }


        var product = await _db.Products
            .Where(p =>
                p.Id == parsed.ProductId.Value &&
                p.IsActive)
            .Select(p => new AiProductInfo
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Category = p.Category != null ? p.Category.Name : "Uncategorized",
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync();


        if (product is null)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = "check_product",
                Message =
                    "I couldn't find that product in the active product catalog.",
                RequiresConfirmation = false,
                SaleCreated = false
            });
        }


        var stockMessage =
            product.Stock > 0
                ? $"Yes. {product.Name} is currently in stock."
                : $"No. {product.Name} is currently out of stock.";


        return Ok(new AiAssistantResponse
        {
            Intent = "check_product",

            Message = stockMessage,

            RequiresConfirmation = false,

            SaleCreated = false,

            Product = product
        });
    }


    // ============================================================
    //  - LOW STOCK
    // ============================================================

    private async Task<ActionResult<AiAssistantResponse>>
        GetLowStockProducts()
    {
        // Phase 1 low-stock threshold.
        // Products with stock <= 5 are considered low stock.
        const int lowStockThreshold = 5;


        var products = await _db.Products
            .Where(p =>
                p.IsActive &&
                p.Stock <= lowStockThreshold)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Name)
            .Select(p => new AiProductInfo
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Category = p.Category != null ? p.Category.Name : "Uncategorized",
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive
            })
            .ToListAsync();


        if (products.Count == 0)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = "low_stock",

                Message =
                    "No products are currently low in stock.",

                RequiresConfirmation = false,

                SaleCreated = false,

                Products = new List<AiProductInfo>()
            });
        }


        return Ok(new AiAssistantResponse
        {
            Intent = "low_stock",

            Message =
                $"I found {products.Count} product" +
                $"{(products.Count == 1 ? "" : "s")} with low stock.",

            RequiresConfirmation = false,

            SaleCreated = false,

            Products = products
        });
    }


    // ============================================================
    // SEARCH PRODUCTS
    // ============================================================

    private async Task<ActionResult<AiAssistantResponse>>
        SearchProducts(AiPythonResponse parsed)
    {
        var requestedIds = parsed.ProductIds
            .Distinct()
            .ToList();


        if (requestedIds.Count == 0)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = "search_products",

                Message =
                    parsed.Unmatched.Count > 0
                        ? $"I couldn't find a product matching \"{parsed.Unmatched[0]}\"."
                        : "I couldn't identify the product you are looking for.",

                RequiresConfirmation = false,

                SaleCreated = false,

                Unmatched = parsed.Unmatched
            });
        }


        var products = await _db.Products
            .Where(p =>
                requestedIds.Contains(p.Id) &&
                p.IsActive)
                .Select(p => new AiProductInfo
                {
                    Id = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    Category = p.Category != null ? p.Category.Name : "Uncategorized",
                    Price = p.Price,
                    Stock = p.Stock,
                    IsActive = p.IsActive
                })
            .ToListAsync();


        if (products.Count == 0)
        {
            return Ok(new AiAssistantResponse
            {
                Intent = "search_products",

                Message =
                    "I couldn't find any matching active products.",

                RequiresConfirmation = false,

                SaleCreated = false
            });
        }


        return Ok(new AiAssistantResponse
        {
            Intent = "search_products",

            Message =
                $"I found {products.Count} matching product" +
                $"{(products.Count == 1 ? "" : "s")}.",

            RequiresConfirmation = false,

            SaleCreated = false,

            Products = products
        });
    }


    // ============================================================
    // BUILD PREVIEW
    // ============================================================

    private async Task<PreviewBuildResult> BuildPreview(
        List<PendingSaleItem> items,
        int? customerId,
        string paymentMethod,
        decimal discount,
        decimal? paidAmount)
    {
        var productIds =
            items
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

        var products =
            await _db.Products
                .Where(p =>
                    productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

        var preview =
            new AiSalePreview
            {
                CustomerId = customerId,
                PaymentMethod = paymentMethod,
                Discount = discount
            };

        if (customerId is not null)
        {
            var customer =
                await _db.Customers
                    .FirstOrDefaultAsync(c =>
                        c.Id == customerId.Value);

            if (customer is null)
            {
                return PreviewBuildResult.Fail(
                    "The selected customer does not exist.");
            }

            preview.CustomerName =
                customer.FullName;
        }


        decimal subtotal = 0;

        var warnings = new List<string>();
        var adjustedItems = new List<PendingSaleItem>();

        foreach (var item in items)
        {
            if (!products.TryGetValue(
                    item.ProductId,
                    out var product))
            {
                return PreviewBuildResult.Fail(
                    $"Product {item.ProductId} was not found.");
            }

            if (!product.IsActive)
            {
                return PreviewBuildResult.Fail(
                    $"{product.Name} is inactive.");
            }

            if (item.Quantity <= 0)
            {
                return PreviewBuildResult.Fail(
                    $"Quantity for {product.Name} must be greater than zero.");
            }

            // Quantity originally requested by the cashier/AI.
            var requestedQuantity = item.Quantity;

            // Quantity that can actually be sold.
            var adjustedQuantity = Math.Min(
                requestedQuantity,
                product.Stock);

            // No stock available: warn and do not include this item
            // in the pending sale.
            if (adjustedQuantity <= 0)
            {
                warnings.Add(
                    $"{product.Name}: requested {requestedQuantity}, " +
                    "but there is no stock available.");

                continue;
            }

            // Requested quantity is greater than available stock.
            if (requestedQuantity > product.Stock)
            {
                warnings.Add(
                    $"{product.Name}: requested {requestedQuantity}, " +
                    $"only {product.Stock} in stock.");
            }

            // IMPORTANT: save the adjusted quantity in the pending sale.
            adjustedItems.Add(
                new PendingSaleItem
                {
                    ProductId = product.Id,
                    Quantity = adjustedQuantity
                });

            var lineTotal =
                product.Price * adjustedQuantity;

            subtotal += lineTotal;

            preview.Items.Add(
                new AiSalePreviewItem
                {
                    ProductId = product.Id,
                    Sku = product.Sku,
                    Name = product.Name,
                    Quantity = adjustedQuantity,
                    UnitPrice = product.Price,
                    LineTotal = lineTotal,
                    AvailableStock = product.Stock,
                    InsufficientStock =
                        requestedQuantity > product.Stock
                });
        }

        if (discount < 0)
            discount = 0;

        if (discount > subtotal)
        {
            return PreviewBuildResult.Fail(
                $"Discount cannot be larger than the subtotal ({subtotal:0.00}).");
        }


        var total =
            subtotal - discount;



        // --------------------------------------------------------
        // PAID AMOUNT
        // --------------------------------------------------------

        var finalPaidAmount =
            paymentMethod == "cash"
                ? paidAmount ?? total
                : 0m;


        if (paymentMethod == "cash" &&
            finalPaidAmount < total)
        {
            warnings.Add(
                $"Paid amount ({finalPaidAmount:0.00}) " +
                $"is less than the total ({total:0.00}).");
        }


        var change =
            paymentMethod == "cash"
                ? Math.Max(0, finalPaidAmount - total)
                : 0m;


        preview.Subtotal = subtotal;
        preview.Discount = discount;
        preview.Total = total;
        preview.PaidAmount = finalPaidAmount;
        preview.ChangeAmount = change;


        return new PreviewBuildResult
        {
            Success = true,
            Preview = preview,
            Warnings = warnings,
            PaidAmount = finalPaidAmount,
            AdjustedItems = adjustedItems

        };
    }


    // ============================================================
    // CONFIRM PENDING SALE
    // ============================================================

    private Task<ActionResult<AiAssistantResponse>>
        ConfirmPendingSale(
            int userId,
            PendingSaleState pending)
    {
        var createRequest =
            new CreateSaleRequest
            {
                CustomerId =
                    pending.CustomerId,

                PaymentMethod =
                    pending.PaymentMethod,

                Discount =
                    pending.Discount,

                PaidAmount =
                    pending.PaidAmount ?? 0m,

                Items =
                    pending.Items
                        .Select(item => new CreateSaleItemRequest
                        {
                            ProductId =
                                item.ProductId,

                            Quantity =
                                item.Quantity,

                            // IMPORTANT:
                            // SalesController validates stock,
                            // but its current implementation
                            // receives UnitPrice from the request.
                            //
                            // We use the real database price here.
                            UnitPrice =
                                GetProductPrice(item.ProductId)
                        })
                        .ToList()
            };


        // ========================================================
        // IMPORTANT
        // ========================================================
        //
        // Reuse the existing authoritative SalesController.
        //
        // This means:
        //
        // - stock is locked
        // - stock is checked again
        // - sale number is generated
        // - SaleItem is created
        // - stock is decremented
        // - authenticated user is recorded
        //
        // Nothing is actually created before this point.
        // ========================================================

        var controller =
            new SalesController(_db)
            {
                ControllerContext =
                    ControllerContext
            };

        var result =
            controller.CreateSale(createRequest);

        if (result.Result is ObjectResult objectResult &&
            objectResult.StatusCode is >= 400)
        {
            return Task.FromResult<
                ActionResult<AiAssistantResponse>>(
                new ObjectResult(
                    new AiAssistantResponse
                    {
                        Intent = "confirm_sale",
                        Message =
                            "The sale could not be completed.",
                        RequiresConfirmation = true,
                        Warnings =
                        {
                            ExtractError(objectResult.Value)
                        }
                    })
                {
                    StatusCode =
                            objectResult.StatusCode
                });
        }


        // --------------------------------------------------------
        // SALE CREATED
        // --------------------------------------------------------

        PendingSales.TryRemove(
            userId,
            out _);


        SaleResponse? sale = null;

        if (result.Result is CreatedAtActionResult created)
        {
            sale =
                created.Value as SaleResponse;
        }

        var response =
            new AiAssistantResponse
            {
                Intent = "confirm_sale",
                Message =
                    sale is null
                        ? "The sale was completed successfully."
                        : $"Sale {sale.Number} was completed successfully.",

                RequiresConfirmation = false,

                SaleCreated = true,

                SaleId =
                    sale?.Id,

                SaleNumber =
                    sale?.Number,

                Preview =
                    sale is null
                        ? null
                        : new AiSalePreview
                        {
                            CustomerId =
                                sale.CustomerId,

                            CustomerName =
                                sale.CustomerName,

                            PaymentMethod =
                                sale.PaymentMethod,

                            Subtotal =
                                sale.Subtotal,

                            Discount =
                                sale.Discount,

                            Total =
                                sale.Total,

                            PaidAmount =
                                sale.PaidAmount,

                            ChangeAmount =
                                sale.ChangeAmount,

                            Items =
                                sale.Items
                                    .Select(x =>
                                        new AiSalePreviewItem
                                        {
                                            ProductId =
                                                x.ProductId,

                                            Sku =
                                                x.Sku,

                                            Name =
                                                x.ProductName,

                                            Quantity =
                                                x.Quantity,

                                            UnitPrice =
                                                x.UnitPrice,

                                            LineTotal =
                                                x.LineTotal
                                        })
                                    .ToList()
                        }
            };

        return Task.FromResult<
            ActionResult<AiAssistantResponse>>(
            Ok(response));
    }


    // ============================================================
    // GET REAL PRODUCT PRICE
    // ============================================================

    private decimal GetProductPrice(int productId)
    {
        return _db.Products
            .Where(p => p.Id == productId)
            .Select(p => p.Price)
            .First();
    }


    // ============================================================
    // CURRENT USER
    // ============================================================

    private int? CurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return int.TryParse(
            value,
            out var id)
            ? id
            : null;
    }


    private static string ExtractError(object? value)
    {
        if (value is null)
            return "Unknown error.";

        var property =
            value.GetType()
                .GetProperty("error");

        if (property?.GetValue(value) is string error)
            return error;

        return value.ToString()
               ?? "Unknown error.";
    }


    // ============================================================
    // INTERNAL TYPES
    // ============================================================

    private class AiParsedResult
    {
        public List<AiParsedItem> Items { get; set; } = new();

        public List<string> Unmatched { get; set; } = new();
    }

    private class AiParsedItem
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }


    private class AiPythonResponse
    {
        public string Intent { get; set; } = "unknown";

        public List<AiPythonItem> Items { get; set; } = new();

        public int? CustomerId { get; set; }

        public string? PaymentMethod { get; set; }

        public decimal Discount { get; set; }

        public decimal? PaidAmount { get; set; }

        public List<string> Unmatched { get; set; } = new();

        public string Message { get; set; } = string.Empty;

        public int? ProductId { get; set; }

        public List<int> ProductIds { get; set; } = new();

        // Generic product-query fields returned by the Python AI service.
        public string? QueryType { get; set; }

        public string? Operation { get; set; }

        public string? Field { get; set; }

        public string? Operator { get; set; }

        public decimal? Value { get; set; }

        public string? Category { get; set; }

        public string? Status { get; set; }
    }


    private class AiPythonItem
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }


    private class AiCatalogProduct
    {
        public int Id { get; set; }

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = "Uncategorized";

        public bool IsActive { get; set; }
    }


    private class AiCatalogCustomer
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }


    private class PendingSaleState
    {
        public int? CustomerId { get; set; }

        public string PaymentMethod { get; set; } = "cash";

        public decimal Discount { get; set; }

        public decimal? PaidAmount { get; set; }

        public List<PendingSaleItem> Items { get; set; } = new();
    }


    private class PendingSaleItem
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }


    private class PreviewBuildResult
    {
        public bool Success { get; set; }

        public string? Error { get; set; }

        public AiSalePreview? Preview { get; set; }

        public List<string> Warnings { get; set; } = new();

        public decimal PaidAmount { get; set; }

        public List<PendingSaleItem> AdjustedItems { get; set; } = new();

        public static PreviewBuildResult Fail(
            string error)
        {
            return new PreviewBuildResult
            {
                Success = false,
                Error = error
            };
        }
    }
}