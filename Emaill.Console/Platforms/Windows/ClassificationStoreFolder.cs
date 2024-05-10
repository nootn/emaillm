namespace Emaill.Console.Platforms.Windows;

public class ClassificationStoreFolder : IClassificationStore
{
    private readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    private const string ClassificationFileName = "classifications.txt";

    public HashSet<string> GetAllClassifications(string accountId)
    {
        var accountDirectory = GetAccountDirectory(accountId);
        var classificationsFilePath = Path.Combine(accountDirectory, ClassificationFileName);

        if (!File.Exists(classificationsFilePath)) return [];
        var classifications = File.ReadAllLines(classificationsFilePath);
        return [..classifications];
    }

    public void AddClassification(string classification, string accountId)
    {
        var accountDirectory = GetAccountDirectory(accountId);
        var classificationsFilePath = Path.Combine(accountDirectory, ClassificationFileName);
        if (!File.Exists(classificationsFilePath))
        {
            using var fs = File.Create(classificationsFilePath);
            fs.Close();
        }

        using var sw = File.AppendText(classificationsFilePath);
        sw.WriteLine(classification);
        sw.Close();
    }

    public void RemoveClassification(string classification, string accountId)
    {
        var accountDirectory = GetAccountDirectory(accountId);
        var classificationsFilePath = Path.Combine(accountDirectory, ClassificationFileName);

        if (!File.Exists(classificationsFilePath)) return;
        var classifications = File.ReadAllLines(classificationsFilePath).ToList();
        classifications.Remove(classification);
        File.WriteAllLines(classificationsFilePath, classifications);
    }

    private string GetAccountDirectory(string accountId)
    {
        var accountDirectory = Path.Combine(_baseDirectory, "Settings", accountId);
        Directory.CreateDirectory(accountDirectory);
        return accountDirectory;
    }
}