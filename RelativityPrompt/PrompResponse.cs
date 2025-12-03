using System.Text.Json.Serialization;

namespace RelativityPrompt
{
    /*
     * Classes that represent the JSON returned after prompt
     */
    public class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public OpenAIChoice[] Choices { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class ClaudeResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("content")]
        public ClaudeContent[] Content { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("usage")]
        public ClaudeUsage Usage { get; set; }
    }

    public class ClaudeContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }

        [JsonPropertyName("cache_creation_input_tokens")]
        public int CacheCreationInputTokens { get; set; }

        [JsonPropertyName("cache_read_input_tokens")]
        public int CacheReadInputTokens { get; set; }

        [JsonPropertyName("cache_creation")]
        public CacheCreation CacheCreation { get; set; }

        [JsonPropertyName("service_tier")]
        public string ServiceTier { get; set; }
    }

    public class CacheCreation
    {
        [JsonPropertyName("ephemeral_5m_input_tokens")]
        public int Ephemeral5mInputTokens { get; set; }

        [JsonPropertyName("ephemeral_1h_input_tokens")]
        public int Ephemeral1hInputTokens { get; set; }
    }
}
