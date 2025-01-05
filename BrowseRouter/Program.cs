using BrowseRouter.Interop.Win32;

namespace BrowseRouter;

public static class Program
{
  private static async Task Main(string[] args)
  {
    Kernel32.AttachToParentConsole();

    if (args.Length == 0)
    {
      await new DefaultBrowserService(new NotifyService(false)).RegisterOrUnregisterAsync();
      return;
    }

    (string passThroughArgs, args) = GetPassthru(args);

    // Process each URL in the arguments list.
    foreach (string arg in args)
    {
      await RunAsync(arg.Trim(), passThroughArgs);
    }
  }

  private static (string passThroughArgs, string[] remainder) GetPassthru(string[] args)
  {
    const string flag = "--passthru";

    bool hasFlag = args.Any(arg => arg == flag);

    if (!hasFlag)
      return ("", args);

    // Get all text between the first "--passthru" and the next "-"
    // e.g. "--passthru arg1 arg2 --flag" => "arg1 arg2"
    string[] passthruArgs = args
      .SkipWhile(a => a != flag).Skip(1)
      .TakeWhile(a => !a.StartsWith("-"))
      .ToArray();

      return (string.Join(" ", passthruArgs), args.TakeWhile(a => a != flag).ToArray());
  }

  private static async Task RunAsync(string arg, string passThroughArgs)
  {
    Func<bool> getIsOption = () => arg.StartsWith('-') || arg.StartsWith('/');

    bool isOption = getIsOption();
    while (getIsOption())
    {
      arg = arg[1..];
    }

    if (isOption)
    {
      await RunOption(arg);
      return;
    }

    await LaunchUrlAsyc(arg, passThroughArgs);
  }

  private static async Task<bool> RunOption(string arg)
  {
    if (string.Equals(arg, "h") || string.Equals(arg, "help"))
    {
      ShowHelp();
      return true;
    }

    if (string.Equals(arg, "r") || string.Equals(arg, "register"))
    {
      await new DefaultBrowserService(new NotifyService(false)).RegisterAsync();
      return true;
    }

    if (string.Equals(arg, "u") || string.Equals(arg, "unregister"))
    {
      await new DefaultBrowserService(new NotifyService(false)).UnregisterAsync();
      return true;
    }

    return false;
  }

  private static async Task LaunchUrlAsyc(string url, string passThroughArgs)
  {
    // Get the window title for whichever application is opening the URL.
    string windowTitle = User32.GetActiveWindowTitle();

    var configService = new ConfigService();
    Log.Preference = configService.GetLogPreference();

    NotifyPreference notifyPref = configService.GetNotifyPreference();
    INotifyService notifier = notifyPref.IsEnabled switch
    {
      true => new NotifyService(notifyPref.IsSilent),
      false => new EmptyNotifyService()
    };

    await new BrowserService(configService, notifier).LaunchAsync(url, windowTitle, passThroughArgs);
  }

  private static void ShowHelp()
  {
    Console.WriteLine
    (
$@"{nameof(BrowseRouter)}: In Windows, launch a different browser depending on the URL.

   https://github.com/nref/BrowseRouter

   Usage:

    BrowseRouter.exe [-h | --help]
        Show help.

    BrowseRouter.exe
        Automatic registration. 
        Same as --register if not already registered, otherwise --unregister.
        If the app has moved or been renamed, updates the existing registration.

    BrowseRouter.exe [-r | --register]
        Register as a web browser, then open Settings. 
        The user must choose BrowseRouter as the default browser.
        No need to run as admin.

    BrowseRouter.exe [-u | --unregister]
        Unregister as a web browser. 

    BrowseRouter.exe https://example.org/ [...more URLs]
        Launch one or more URLs"
    );
  }
}
