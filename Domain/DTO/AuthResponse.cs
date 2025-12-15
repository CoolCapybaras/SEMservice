namespace Domain.DTO;

public record AuthResponse(string AccessToken, string RefreshToken, DateTime RefreshExpiresAt);