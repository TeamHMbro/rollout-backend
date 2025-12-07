namespace Auth.Application.AuthContracts;

public sealed record MeResponse(Guid Id, string? Email, string? Phone);
