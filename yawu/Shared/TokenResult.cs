using System.Text.Json.Serialization;

namespace Shared;

public class TokenResult
{
    [JsonPropertyName("token")]
    public required string Token { get; init; }
}