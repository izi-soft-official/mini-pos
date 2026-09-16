namespace MiniPos.Api.Dtos;

public record CategoryResponse(int Id, string Name, bool IsActive);

public record CreateCategoryRequest(string Name);

public record UpdateCategoryRequest(string Name, bool IsActive);

public record ProductResponse(
    int Id,
    string Sku,
    string Name,
    int CategoryId,
    string CategoryName,
    decimal Price,
    decimal Cost,
    int Stock,
    bool IsActive
);

public record CreateProductRequest(
    string Sku,
    string Name,
    int CategoryId,
    decimal Price,
    decimal Cost,
    int Stock,
    bool IsActive
);

public record UpdateProductRequest(
    string Sku,
    string Name,
    int CategoryId,
    decimal Price,
    decimal Cost,
    int Stock,
    bool IsActive
) : CreateProductRequest(Sku, Name, CategoryId, Price, Cost, Stock, IsActive);
