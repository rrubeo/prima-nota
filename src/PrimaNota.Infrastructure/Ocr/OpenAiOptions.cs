namespace PrimaNota.Infrastructure.Ocr;

/// <summary>Configuration for the OpenAI vision API.</summary>
public sealed class OpenAiOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "OpenAi";

    /// <summary>Gets or sets the API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the model to use (default: gpt-4o-mini).</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>Gets or sets the API base URL.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com";
}
