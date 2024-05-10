using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Windows;

namespace Emaill.Console.Platforms.Windows;

public class MailProgramWindowsAppium : IMailProgram
{
    private readonly string _appId = "Microsoft.OutlookForWindows_8wekyb3d8bbwe!Microsoft.OutlookforWindows"; //Powershell: `Get-StartApps` and find "Outlook (new)"
    private readonly TimeSpan _commandTimeout = new(0, 0, 1, 0);
    private readonly TimeSpan _implicitTimeout = new(0, 0, 0, 30);
    private WindowsDriver? _mailProgramSession;

    public Task Start()
    {
        var appCapabilities = new AppiumOptions
        {
            AutomationName = "Windows",
            App = _appId,
            PlatformName = "windows"
        };

        var serverUri = AppiumServers.LocalServiceUri;
        _mailProgramSession = new WindowsDriver(serverUri, appCapabilities, _commandTimeout);
        _mailProgramSession.Manage().Timeouts().ImplicitWait = _implicitTimeout;

        _mailProgramSession.FindElement(MobileBy.Name("Mail")).Click();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _mailProgramSession?.CloseApp();
        _mailProgramSession?.Dispose();
        _mailProgramSession = null;
        AppiumServers.StopLocalService();
    }
}


/// <summary>
/// Taken from the Appium .NET client test project - will clean up later if we get it working
/// https://github.com/appium/dotnet-client/blob/main/test/integration/helpers/AppiumServers.cs
/// </summary>
internal class AppiumServers
{
    private static AppiumLocalService? _localService;
    private static Uri? _remoteAppiumServerUri;

    public static Uri? LocalServiceUri
    {
        get
        {
            if (_localService == null)
            {
                var builder =
                    new AppiumServiceBuilder()
                        .UsingPort(19191) //`appium server -p 19191`
                        .WithLogFile(new FileInfo(Path.GetTempPath() + "AppiumLog.txt"));

                _localService = builder.Build();
            }

            if (!_localService.IsRunning) _localService.Start();

            return _localService.ServiceUrl;
        }
    }

    public static Uri? RemoteServerUri
    {
        get
        {
            if (_remoteAppiumServerUri == null)
            {
                var env = Environment.GetEnvironmentVariable("RemoteAppiumServerUri");
                if (!string.IsNullOrEmpty(env) && Uri.TryCreate(env, UriKind.Absolute, out var uri))
                    _remoteAppiumServerUri = uri;
            }

            else
            {
                return _remoteAppiumServerUri;
            }

            return _remoteAppiumServerUri;
        }
    }

    public static void StopLocalService()
    {
        if (_localService != null && _localService.IsRunning)
        {
            _localService.Dispose();
            _localService = null;
        }
    }
}