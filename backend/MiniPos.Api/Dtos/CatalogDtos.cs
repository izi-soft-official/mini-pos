namespace MiniPos.Api.Dtos;

public record CategoryResponse
{
    public int Guid { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public record CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
}

public record UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public record ProductResponse(
    Guid Guid,
    string Sku,
    string Name,
    Guid CategoryGuid,
    string CategoryName,
    decimal Price,
    decimal Cost,
    int Stock,
    bool IsActive
);

public record CreateProductRequest(
    string Sku,
    string Name,
    Guid CategoryGuid,
    decimal Price,
    decimal Cost,
    int Stock,
    bool IsActive
);

public record UpdateProductRequest(
    string Sku,
    string Name,
    Guid CategoryGuid,
    decimal Price,
    decimal Cost,
    int Stock,
    bool IsActive
) : CreateProductRequest(Sku, Name, CategoryGuid, Price, Cost, Stock, IsActive);
