using System.Text.Json.Serialization;

namespace Emaill.Console.Models;

public record EmailClassification(
    [property: JsonPropertyName("summary")]
    string Summary,
    [property: JsonPropertyName("likelyTypeOfEmail")]
    string LikelyTypeOfEmail,
    [property: JsonPropertyName("mainTopics")]
    string MainTopics,
    [property: JsonPropertyName("percentageChanceOfSpam")]
    int PercentageChanceOfSpam,
    [property: JsonPropertyName("percentageChanceOfPhishing")]
    int PercentageChanceOfPhishing,
    [property: JsonPropertyName("percentageChanceOfShopping")]
    int PercentageChanceOfShopping,
    [property: JsonPropertyName("percentageChanceOfMarketing")]
    int PercentageChanceOfMarketing,
    [property: JsonPropertyName("percentageChanceOfPromotional")]
    int PercentageChanceOfPromotional,
    [property: JsonPropertyName("percentageChanceOfNewsletter")]
    int PercentageChanceOfNewsletter,
    [property: JsonPropertyName("percentageChanceOfEducational")]
    int PercentageChanceOfEducational,
    [property: JsonPropertyName("percentageChanceOfPersonal")]
    int PercentageChanceOfPersonal
);