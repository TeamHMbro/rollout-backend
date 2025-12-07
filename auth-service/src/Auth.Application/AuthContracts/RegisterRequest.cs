namespace Auth.Application.AuthContracts;

public sealed record RegisterRequest(string? Email, string? Phone, string Password);
