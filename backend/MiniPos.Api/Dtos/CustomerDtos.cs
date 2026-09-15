namespace MiniPos.Api.Dtos;

public record CustomerResponse(Guid Id, string FullName, string Phone, string Email, string Note, DateTime CreatedAt);

public record CreateCustomerRequest(string FullName, string Phone, string Email, string Note);

public record UpdateCustomerRequest(string FullName, string Phone, string Email, string Note) : CreateCustomerRequest(FullName, Phone, Email, Note);
