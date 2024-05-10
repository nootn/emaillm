using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Emaill.Console.Models;
using Spectre.Console;

namespace Emaill.Console.AiIntegration;

public class AiIntegrationOllama(IHttpClientFactory _httpClientFactory) : IAiIntegration
{
    public async Task<EmailClassification> ClassifyEmail(string fromEmail, string subject, string body,
        HashSet<string> classifications)
    {
        var prompt = @$"Assess the following email and respond with a JSON result (no other text) in the following format:
`{{
    ""summary"": ""A brief 1 sentence summary of the email"",
    ""likelyTypeOfEmail"": ""spam"",
    ""mainTopics"": ""Bob's Online Casino"",
    ""percentageChanceOfSpam"": 80,
    ""percentageChanceOfPhishing"": 40,
    ""percentageChanceOfShopping"": 10,
    ""percentageChanceOfMarketing"": 0,
    ""percentageChanceOfPromotional"": 0,
    ""percentageChanceOfNewsletter"": 0,
    ""percentageChanceOfEducational"": 0,
    ""percentageChanceOfPersonal"": 1,
}}`
The value of 'likelyTypeOfEmail' is a short description of the most likely type of email out of 'spam', 'phishing', 'shopping' 'marketing' 'promotional' 'newsletter' 'educational' 'personal'  it should match the one with the highest percentage.
The value of 'mainTopics' is a few words comma separated, describing the company the email is from or the name of what it's about (E.g. if it's for a sporting team, show the name of the team).
{(classifications.Any() ? "These are some classification hints to aid the decisions, use these in addition to what you already know:" : "")}
{string.Join("\n", classifications)}

Email details:
From: `{fromEmail}`
Subject: `{subject}`
Body (substring max 1000 chars): `{new string(body.Take(1000).ToArray())}`
";


        return await GetLlamaResponse<EmailClassification>(prompt);
    }

    public async Task<EmailAction> RecommendAction(EmailClassification classification, HashSet<string> rules)
    {
        //we must have at least 1 rule otherwise the prompt doesn't make sense, so add default rules if there are none:
        if (rules.Count == 0)
        {
            rules = ["Any other classification requires manual intervention."];
        }

        var likelyWithPercentIfRelevant = classification.LikelyTypeOfEmail;
        if (likelyWithPercentIfRelevant.Contains("spam", StringComparison.InvariantCultureIgnoreCase))
        {
            likelyWithPercentIfRelevant = $"{classification.LikelyTypeOfEmail} ({classification.PercentageChanceOfSpam}% likely)";
        }
        else if (likelyWithPercentIfRelevant.Contains("phishing", StringComparison.InvariantCultureIgnoreCase))
        {
            likelyWithPercentIfRelevant = $"{classification.LikelyTypeOfEmail} ({classification.PercentageChanceOfPhishing}% likely)";
        }

        var prompt = @$"Based on the rules provided below and classification of an email being '{likelyWithPercentIfRelevant}' and main topics being '{classification.MainTopics}', return a JSON response in the following format where this has example values explaining what the values are:
`{{
    ""recommendation"": ""A brief 1 sentence summary of tha action that should be taken (E.g. Move to 'Spam' folder)"",
    ""action"": 5,
    ""folderName"": ""Spam"",
}}`
The possible values for 'action' are:
'0' - manual intervention required
'1' - delete the email
'2' - archive the email
'3' - report the email as junk
'4' - report the email as phishing
'5' - if a rule suggests it, move the email to a folder (and set the 'folderName' property to the folder name listed in the rule)

If the classification does not match a rule listed below, just return '0' for manual intervention. 
If a rule is to do with moving to a folder, the folder name will be specified within back ticks, E.g `FolderName` - those are then only valid folder names, if one does not exist, it will require manual intervention. If moving to a folder, the exact folder name must exist in the rules, do not make up new folders when suggesting to move to a folder.
Use the combination of main topics and classification to determine the best folder to move to. If there is a main topic mentioned in a rule, use that rule over a generic rule.
Here are the rules each on a new line:

{string.Join("\n", rules)}
";

        var res = await GetLlamaResponse<EmailAction>(prompt);

        if (res.Action == ActionType.MoveToFolder)
        {
            if (string.IsNullOrWhiteSpace(res.FolderName))
            {
                throw new InvalidOperationException("When moving to a folder, the folder name must be specified.");
            }

            if (!rules.Any(r =>
                    r.Contains($"`{res.FolderName}`", StringComparison.InvariantCultureIgnoreCase)))
            {
                var originalFolderName = res.FolderName;

                //try set the folder name again...
                var folderPrompt =
                    @$"Based on the rules provided below and classification of an email being '{likelyWithPercentIfRelevant}' and main topics being '{classification.MainTopics}', return a JSON response in the following format where this has example values explaining what the values are:
`{{,
    ""recommendation"": ""A brief 1 sentence summary of tha action that should be taken (E.g. Move to 'Spam' folder)"",
    ""folderName"": ""Spam"",
}}`
Use the combination of main topics and classification to determine the best folder to move to. If there is a main topic mentioned in a rule, use that rule over a generic rule.
To set 'folderName' in the response, choose a folder name from the rules below - it must match a folder that is specified in the rules, new folder names cannot be created. The folder name will be specified within back ticks, E.g `FolderName` in each rule if relevant. If no relevant folder name exists in the rules, return an empty string for 'folderName'.
Last time this ran, a folder name of '{originalFolderName}' was chosen, but that does not exist in the rules, so please specify one that does exist in the rules below:

{string.Join("\n", rules)}
";

                AnsiConsole.WriteLine("...second attempt to get response...");
                var folderRes = await GetLlamaResponse<EmailAction>(folderPrompt);
                res = res with { Recommendation = folderRes.Recommendation, FolderName = folderRes.FolderName };

                if (!rules.Any(r =>
                        r.Contains($"`{res.FolderName}`", StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        $"The folder name '{res.FolderName}' is not a valid folder name to move to on second attempt. First folder name was '{originalFolderName}'.");
                }
            }
        }

        return res;
    }

    private async Task<T> GetLlamaResponse<T>(string prompt)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync("http://localhost:11434/api/generate",
            new OllamaRequest("llama3", prompt, "json", false));

        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(content);

            if (ollamaResponse == null)
            {
                throw new InvalidOperationException($"Unable to deserialize outer response to {nameof(OllamaResponse)}");
            }

            var resultJson = JsonSerializer.Deserialize<T>(ollamaResponse.Response);

            if (resultJson == null)
            {
                throw new InvalidOperationException($"Unable to deserialize inner response to {nameof(EmailClassification)}");
            }

            return resultJson;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Unable to deserialize response: {content}", e);
        }
    }



    private record OllamaRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("stream")] bool Stream);

    private record OllamaResponse(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt,
        [property: JsonPropertyName("response")] string Response,
        [property: JsonPropertyName("done")] bool Done,
        [property: JsonPropertyName("context")] IReadOnlyList<int> Context,
        [property: JsonPropertyName("total_duration")] long TotalDuration,
        [property: JsonPropertyName("load_duration")] long LoadDuration,
        [property: JsonPropertyName("prompt_eval_count")] long PromptEvalCount,
        [property: JsonPropertyName("prompt_eval_duration")] long PromptEvalDuration,
        [property: JsonPropertyName("eval_count")] int EvalCount,
        [property: JsonPropertyName("eval_duration")] long EvalDuration
    );
}