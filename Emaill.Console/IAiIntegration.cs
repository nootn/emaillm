using Emaill.Console.Models;

namespace Emaill.Console;

public interface IAiIntegration
{
    Task<EmailClassification> ClassifyEmail(string fromEmail, string subject, string body, HashSet<string> classifications);
    Task<EmailAction> RecommendAction(EmailClassification classification, HashSet<string> rules);
}