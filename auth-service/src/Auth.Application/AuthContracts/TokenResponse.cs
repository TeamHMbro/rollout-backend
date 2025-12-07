namespace Auth.Application.AuthContracts;

public sealed record TokenResponse(string AccessToken, string RefreshToken);
