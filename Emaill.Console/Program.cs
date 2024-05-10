using Emaill.Console;
using Emaill.Console.AiIntegration;
using Emaill.Console.Platforms.Windows;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

try
{
    var services = new ServiceCollection();
    services.AddHttpClient();
    services.AddSingleton<IRuleStore, RuleStoreFolder>();
    services.AddSingleton<IClassificationStore, ClassificationStoreFolder>();
    services.AddSingleton<IMailProgram, MailProgramWindowsFlaUi>();
    services.AddSingleton<IAiIntegration, AiIntegrationOllama>();
    var serviceProvider = services.BuildServiceProvider();


    AnsiConsole.Write(
        new FigletText("Process Emails")
            .LeftJustified()
            .Color(Color.Blue));

    using IMailProgram mailProgram = serviceProvider.GetRequiredService<IMailProgram>();
    await mailProgram.Start();
}
catch (Exception ex)
{
    AnsiConsole.Write(
        new FigletText("Error")
            .LeftJustified()
            .Color(Color.Red));
    AnsiConsole.WriteException(ex);
}
finally
{
    AnsiConsole.WriteLine("Press any key to exit...");
    Console.ReadKey();
}