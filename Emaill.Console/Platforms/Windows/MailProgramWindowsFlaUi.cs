using System.Diagnostics;
using System.Runtime.InteropServices;
using Emaill.Console.Models;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using FlaUI.UIA3.Patterns;
using Spectre.Console;

namespace Emaill.Console.Platforms.Windows;

public class MailProgramWindowsFlaUi(
    IRuleStore ruleStore,
    IClassificationStore classificationStore,
    IAiIntegration aiIntegration) : IMailProgram
{
    private Application? _mailApp;
    private UIA3Automation? _mailAutomation;

    public async Task Start()
    {
        //set focus back to this app
        var thisWindow = Process.GetCurrentProcess().MainWindowHandle;

        var procInfo = new ProcessStartInfo("olk.exe");
        _mailAutomation = new UIA3Automation();
        _mailApp = Application.AttachOrLaunch(procInfo);
        if (_mailApp.WaitWhileMainHandleIsMissing())
        {
            var window = _mailApp.GetMainWindow(_mailAutomation);
            window.FindFirstDescendant(cf => cf.ByAutomationId(PageObject.LeftMenuMailButtonAutomationId)).Click();
            var navPane = window.FindFirstDescendantRequired(cf => cf.ByControlType(ControlType.Group)
                .And(cf.ByName(PageObject.NavigationPaneGroupName)));

            var emailAccountSections =
                navPane.FindAllChildren(cf => cf.ByText("@", PropertyConditionFlags.MatchSubstring)) ?? [];
            AnsiConsole.WriteLine($"Found {emailAccountSections.Length} email accounts");

            SetForegroundWindow(thisWindow);
            var selectedAccount = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Which account would you like to process?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more accounts)[/]")
                .AddChoices(emailAccountSections.Select(e => e.Name)));

            AnsiConsole.WriteLine($"Processing {selectedAccount}");

            //select the "other" tab first
            window.FindFirstDescendantRequired(cf => cf.ByControlType(ControlType.TabItem)
                .And(cf.ByName(PageObject.EmailListTabItemOtherName))).Click();
            var keepProcessing = true;
            var isFocused = false;
            while (keepProcessing)
            {
                var visibleEmails = GetEmails(window);

                if (!visibleEmails.Any())
                {
                    var tabName = isFocused ? "Focused" : "Other";
                    AnsiConsole.MarkupLine($"[salmon1]No emails found in {tabName}[/]");
                    if (!isFocused)
                    {
                        isFocused = true;
                        window.FindFirstDescendantRequired(cf => cf.ByControlType(ControlType.TabItem)
                            .And(cf.ByName(PageObject.EmailListTabItemFocusedName))).Click();
                    }
                    else
                    {
                        keepProcessing = false;
                    }
                }
                else
                {
                    var toProcess = visibleEmails.First();
                    toProcess.Click();

                    var allRules = ruleStore.GetAllRules(selectedAccount);
                    var allClassifications = classificationStore.GetAllClassifications(selectedAccount);

                    AnsiConsole.WriteLine(
                        $"Processing email from {toProcess.Name} with {allClassifications.Count} classifications and {allRules.Count} rules");

                    var from = toProcess.FindFirstChild(cf => cf.ByControlType(ControlType.Group))
                        .FindFirstChild(cf => cf.ByControlType(ControlType.Group))
                        .FindFirstChild(cf => cf.ByControlType(ControlType.Group)).Name ?? "unknown";
                    var subject = window
                        .FindFirstDescendantRequired(cf =>
                            cf.ByAutomationId(PageObject.ReadingPaneContainerAutomationId))
                        .FindFirstDescendantRequired(cf => cf.ByControlType(ControlType.Text)).Name;
                    var body = string.Join(Environment.NewLine,
                        window.FindFirstDescendantRequired(cf =>
                                cf.ByAutomationId(PageObject.ReadingPaneMessageBodyAutomationId))
                            .FindAllDescendants(cf => cf.ByControlType(ControlType.Group)).Select(e => e.Name));

                    var classification = await AnsiConsole.Status()
                        .Spinner(Spectre.Console.Spinner.Known.Shark)
                        .StartAsync("Liaising with Llamas to classify...",
                            async _ => await aiIntegration.ClassifyEmail(from, subject, body, allClassifications));

                    AnsiConsole.MarkupLine("[gold3]- CLASSIFICATION -[/]");
                    AnsiConsole.MarkupLine($"[blue]Summary    : [/] {classification.Summary}");
                    AnsiConsole.MarkupLine($"[blue]Likely Type: [/] [purple]{classification.LikelyTypeOfEmail}[/]");
                    AnsiConsole.MarkupLine($"[blue]Main Topics: [/] [purple]{classification.MainTopics}[/]");
                    var table = new Table();
                    table.AddColumn("Type");
                    table.AddColumn(new TableColumn("% Chance").Centered());

                    table.AddRow("Spam", GetMarkupValueForPercent(classification.PercentageChanceOfSpam));
                    table.AddRow("Phishing", GetMarkupValueForPercent(classification.PercentageChanceOfPhishing));
                    table.AddRow("Shopping", GetMarkupValueForPercent(classification.PercentageChanceOfShopping));
                    table.AddRow("Marketing", GetMarkupValueForPercent(classification.PercentageChanceOfMarketing));
                    table.AddRow("Promotional", GetMarkupValueForPercent(classification.PercentageChanceOfPromotional));
                    table.AddRow("Newsletter", GetMarkupValueForPercent(classification.PercentageChanceOfNewsletter));
                    table.AddRow("Educational", GetMarkupValueForPercent(classification.PercentageChanceOfEducational));
                    table.AddRow("Personal", GetMarkupValueForPercent(classification.PercentageChanceOfPersonal));
                    AnsiConsole.Write(table);

                    if (AnsiConsole.Confirm("Do you want to add a classification to improve this in future?", false))
                    {
                        var newClassification =
                            AnsiConsole.Prompt(new TextPrompt<string>("Enter a new classification"));
                        if (!string.IsNullOrWhiteSpace(newClassification))
                            classificationStore.AddClassification(newClassification, selectedAccount);
                    }

                    var action = await AnsiConsole.Status()
                        .Spinner(Spectre.Console.Spinner.Known.Monkey)
                        .StartAsync("Liaising with Llamas to determine action...",
                            async _ => await aiIntegration.RecommendAction(classification, allRules));

                    AnsiConsole.MarkupLine("[gold3]- ACTION -[/]");
                    AnsiConsole.MarkupLine($"[blue]Recommendation: [/] {action.Recommendation}");
                    AnsiConsole.MarkupLine($"[blue]Action        : [/] [purple]{action.Action}[/]");
                    if (action.Action == ActionType.MoveToFolder)
                        AnsiConsole.MarkupLine($"[blue]Folder Name   : [/] [purple]{action.FolderName}[/]");

                    if (action.Action != ActionType.Manual)
                    {
                        if (AnsiConsole.Confirm("Do you want to complete the action?", false))
                        {
                            //TODO... automate all the business!
                            toProcess.Click();
                            if (action.Action == ActionType.MoveToFolder)
                            {
                                toProcess.RightClick();
                                window.FindFirstDescendantRequired(cf => cf.ByControlType(ControlType.MenuItem)
                                    .And(cf.ByName(PageObject.RightClickMenuItemMoveName))).Click();
                                var searchBox = window.FindFirstDescendantRequired(cf => cf
                                    .ByControlType(ControlType.Edit)
                                    .And(cf.ByName(PageObject.RightClickMenuItemMoveEditSearchForFolderName)));
                                searchBox.Click();
                                AnsiConsole.WriteLine(searchBox.GetType().ToString());
                                if (searchBox.Patterns.Value.TryGetPattern(out var pattern))
                                {
                                    pattern.SetValue(action.FolderName);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unable to set value in Search Box");
                                }
                                window.FindFirstDescendantRequired(cf => cf.ByControlType(ControlType.MenuItem)
                                    .And(cf.ByName(action.FolderName))).Click();
                            }
                        }
                    }

                    if (AnsiConsole.Confirm("Do you want to add a new rule?", false))
                    {
                        var newRule = AnsiConsole.Prompt(new TextPrompt<string>("Enter a new rule"));
                        if (!string.IsNullOrWhiteSpace(newRule)) ruleStore.AddRule(newRule, selectedAccount);
                    }

                    if (!AnsiConsole.Confirm("Continue to top email?")) return;
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Unable to find main window");
        }
    }

    public void Dispose()
    {
        _mailAutomation?.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private AutomationElement[] GetEmails(Window window)
    {
        var messageGroup =
            window.FindFirstDescendantRequired(cf => cf.ByAutomationId(PageObject.EmailListGroupAutomationId));

        var messages = messageGroup.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem)) ?? [];

        return messages;
    }

    private static string GetMarkupValueForPercent(int percent)
    {
        return percent switch
        {
            < 25 => $"[green]{percent}%[/]",
            < 50 => $"[gold3]{percent}%[/]",
            < 75 => $"[darkorange]{percent}%[/]",
            _ => $"[maroon]{percent}%[/]"
        };
    }

    private static class PageObject
    {
        public static string LeftMenuMailButtonAutomationId => "ddea774c-382b-47d7-aab5-adc2139a802b";
        public static string NavigationPaneGroupName => "Navigation pane";
        public static string EmailListTabItemFocusedName => "Focused";
        public static string EmailListTabItemOtherName => "Other";
        public static string EmailListGroupAutomationId => "MailList";
        public static string ReadingPaneContainerAutomationId => "ConversationReadingPaneContainer";
        public static string ReadingPaneMessageBodyAutomationId => "UniqueMessageBody";
        public static string RightClickMenuItemMoveName = "Move";
        public static string RightClickMenuItemMoveEditSearchForFolderName = "Search for a folder";
    }
}

internal static class FlaUiAutomationExtensions
{
    public static AutomationElement FindFirstDescendantRequired(this AutomationElement parent,
        Func<ConditionFactory, ConditionBase> conditionFunc, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);

        AutomationElement? optionalElement = null;

        Retry.WhileException(() =>
            {
                optionalElement = parent.FindFirstDescendant(conditionFunc);
                if (optionalElement == null) throw new InvalidOperationException("Element not found");
            },
            timeout,
            TimeSpan.FromMilliseconds(100));

        if (optionalElement == null)
            throw new InvalidOperationException(
                $"Unable to find element after waiting for {timeout.Value.TotalSeconds} seconds");

        return optionalElement;
    }
}