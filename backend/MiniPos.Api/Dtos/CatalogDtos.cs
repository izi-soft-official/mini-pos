namespace MiniPos.Api.Dtos;

public record CategoryResponse
{
    public int Id { get; set; }
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

public record ProductResponse
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

public record CreateProductRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

public record UpdateProductRequest : CreateProductRequest
{
}
