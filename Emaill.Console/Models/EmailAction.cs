using System.Text.Json.Serialization;

namespace Emaill.Console.Models;

public record EmailAction(
    [property: JsonPropertyName("recommendation")]
    string Recommendation,
    [property: JsonPropertyName("action")]
    ActionType? Action,
    [property: JsonPropertyName("folderName")]
    string? FolderName
);

public enum ActionType
{
    Manual = 0,
    Delete = 1,
    Archive = 2,
    ReportJunk = 3,
    ReportPhishing = 4,
    MoveToFolder = 5
}