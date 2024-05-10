namespace Emaill.Console;

public interface IRuleStore
{
    HashSet<string> GetAllRules(string accountId);
    void AddRule(string rule, string accountId);
    void RemoveRule(string rule, string accountId);
}