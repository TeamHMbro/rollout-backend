namespace Auth.Application.AuthContracts;

public sealed record LoginRequest(string? Email, string? Phone, string Password);
