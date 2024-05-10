namespace Emaill.Console.Platforms.Windows;

public class RuleStoreFolder : IRuleStore
{
    private readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    private const string RuleFileName = "rules.txt";

    public HashSet<string> GetAllRules(string accountId)
    {
        var accountDirectory = GetAccountDirectory(accountId);
        var rulesFilePath = Path.Combine(accountDirectory, RuleFileName);

        if (!File.Exists(rulesFilePath)) return [];
        var rules = File.ReadAllLines(rulesFilePath);
        return [..rules];
    }

    public void AddRule(string rule, string accountId)
    {
        var accountDirectory = GetAccountDirectory(accountId);
        var rulesFilePath = Path.Combine(accountDirectory, RuleFileName);
        if (!File.Exists(rulesFilePath))
        {
            using var fs = File.Create(rulesFilePath);
            fs.Close();
        }

        using var sw = File.AppendText(rulesFilePath);
        sw.WriteLine(rule);
        sw.Close();
    }

    public void RemoveRule(string rule, string accountId)
    {
        var accountDirectory = GetAccountDirectory(accountId);
        var rulesFilePath = Path.Combine(accountDirectory, RuleFileName);

        if (!File.Exists(rulesFilePath)) return;
        var rules = File.ReadAllLines(rulesFilePath).ToList();
        rules.Remove(rule);
        File.WriteAllLines(rulesFilePath, rules);
    }

    private string GetAccountDirectory(string accountId)
    {
        var accountDirectory = Path.Combine(_baseDirectory, "Settings", accountId);
        Directory.CreateDirectory(accountDirectory);
        return accountDirectory;
    }
}