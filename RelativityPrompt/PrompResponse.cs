using System.Text.Json.Serialization;

namespace RelativityPrompt
{
    /*
     * Classes that represent the JSON returned after prompt
     */
    public class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public Choice[] Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
