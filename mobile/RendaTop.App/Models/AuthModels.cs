using System.Text.Json.Serialization;

namespace RendaTop.App.Models;

public sealed record LoginRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);

public sealed record TotpLoginRequest(
    [property: JsonPropertyName("challenge_id")] string ChallengeId,
    [property: JsonPropertyName("code")] string Code);

public sealed record SignupRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);

public sealed record SignupVerificationRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("code")] string Code);

public sealed record SignupVerificationResendRequest(
    [property: JsonPropertyName("email")] string Email);

public sealed record LoginStartResponse(
    [property: JsonPropertyName("requires_totp")] bool RequiresTotp,
    [property: JsonPropertyName("challenge_id")] string? ChallengeId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("user_type")] string? UserType);

public sealed record LoginResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("user_type")] string UserType);

public sealed record SignupPendingResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("email_sent")] bool EmailSent);

public sealed record MessageResponse(
    [property: JsonPropertyName("message")] string Message);

public sealed record ErrorResponse(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("statusCode")] int? StatusCode);
