namespace Emaill.Console;

public interface IClassificationStore
{
    HashSet<string> GetAllClassifications(string accountId);
    void AddClassification(string Classification, string accountId);
    void RemoveClassification(string Classification, string accountId);
}