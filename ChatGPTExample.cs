using System.Net.Http;
using System.Net.Http.Headers;

using UnityEngine;

public class ChatGPTExample : MonoBehaviour
{
    // Call this function and pass the text message to the ChatGPT API endpoint.
    public async void PostChatQuery(string message)
    {
        using (var httpClient = new HttpClient())
        {
            // HTTP request to ChatGPT API.
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.openai.com/v1/chat/completions"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer yourAPIkeyhere");

                // This is the POST message to Chat GPT API, there are various configs, so see OpenAI's docs for more options.
                string msg = "{\"model\": \"gpt-3.5-turbo\",\"messages\": [{\"role\": \"user\", \"content\": \"" + message + "\"}]}";

                request.Content = new StringContent(msg);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                Debug.Log(responseBody);
            }
        }
    }
}
