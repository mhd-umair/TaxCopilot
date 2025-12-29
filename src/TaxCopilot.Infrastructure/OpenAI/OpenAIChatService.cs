using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using TaxCopilot.Application.Configuration;
using TaxCopilot.Application.DTOs;
using TaxCopilot.Application.Interfaces;

namespace TaxCopilot.Infrastructure.OpenAI;

/// <summary>
/// OpenAI chat service implementation (non-Azure).
/// </summary>
public class OpenAIChatService : IChatService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAIChatService> _logger;

    private const string SystemPrompt = @"You are a tax law expert assistant. Your task is to answer questions based ONLY on the provided document context.

STRICT RULES:
1. Answer ONLY based on the information in the provided context
2. If the answer is not found in the context, respond with: ""Not found in provided documents.""
3. Always cite your sources with document title, page number, and section heading
4. Be precise and accurate - do not speculate or add information not in the context
5. Assess your confidence level based on how directly the context answers the question

OUTPUT FORMAT:
You must respond with a valid JSON object in exactly this format:
{
  ""answer"": ""Your detailed answer here"",
  ""citations"": [
    {""documentTitle"": ""Title"", ""pageNumber"": 1, ""sectionHeading"": ""Section"", ""chunkId"": ""id""}
  ],
  ""confidence"": ""high|medium|low""
}

CONFIDENCE LEVELS:
- high: The context directly and completely answers the question
- medium: The context partially answers the question or requires some interpretation
- low: The context only tangentially relates to the question";

    public OpenAIChatService(IOptions<OpenAIOptions> options, ILogger<OpenAIChatService> logger)
    {
        var openAIOptions = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(openAIOptions.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key not configured");
        }

        var client = new OpenAIClient(openAIOptions.ApiKey);
        _chatClient = client.GetChatClient(openAIOptions.ChatModel);

        _logger.LogInformation("OpenAI Chat service initialized with model: {Model}", openAIOptions.ChatModel);
    }

    public async Task<AskResponse> GenerateAnswerAsync(
        string question,
        List<RetrievedChunk> context,
        CancellationToken cancellationToken = default)
    {
        if (context.Count == 0)
        {
            return new AskResponse
            {
                Answer = "Not found in provided documents.",
                Citations = new List<Citation>(),
                Confidence = "low"
            };
        }

        // Build context string
        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("DOCUMENT CONTEXT:");
        contextBuilder.AppendLine("=================");

        foreach (var chunk in context)
        {
            contextBuilder.AppendLine($"\n--- Document: {chunk.DocumentTitle} | Page: {chunk.PageNumber} | Section: {chunk.SectionHeading ?? "N/A"} | ChunkId: {chunk.ChunkId} ---");
            contextBuilder.AppendLine(chunk.ChunkText);
        }

        var userMessage = $@"{contextBuilder}

QUESTION: {question}

Provide your answer in the required JSON format.";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(userMessage)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                MaxOutputTokenCount = 2000
            };

            var response = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

            var responseText = response.Value.Content[0].Text;

            _logger.LogDebug("Raw chat response: {Response}", responseText);

            // Parse JSON response
            return ParseResponse(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate chat response");
            throw;
        }
    }

    private AskResponse ParseResponse(string responseText)
    {
        try
        {
            // Try to extract JSON from the response (in case there's extra text)
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var response = JsonSerializer.Deserialize<AskResponse>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (response != null)
                {
                    return response;
                }
            }

            // Fallback if JSON parsing fails
            _logger.LogWarning("Failed to parse JSON response, returning raw text");
            return new AskResponse
            {
                Answer = responseText,
                Citations = new List<Citation>(),
                Confidence = "low"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse chat response as JSON");
            return new AskResponse
            {
                Answer = responseText,
                Citations = new List<Citation>(),
                Confidence = "low"
            };
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new UserChatMessage("test")
            };

            var chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 5
            };

            await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI Chat service not available");
            return false;
        }
    }
}

