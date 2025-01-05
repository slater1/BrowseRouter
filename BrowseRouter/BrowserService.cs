using System.Diagnostics;

namespace BrowseRouter;

public class BrowserService(IConfigService config, INotifyService notifier)
{
  public async Task LaunchAsync(string url, string windowTitle, string passThroughArgs)
  {
    try
    {
      Log.Write($"Attempting to launch \"{url}\" for \"{windowTitle}\"");

      IEnumerable<UrlPreference> urlPreferences = config.GetUrlPreferences("urls");
      IEnumerable<UrlPreference> sourcePreferences = config.GetUrlPreferences("sources");
      Uri uri = UriFactory.Get(url);

      UrlPreference? pref = null;
      if (sourcePreferences.TryGetPreference(windowTitle, out UrlPreference sourcePref))
      {
        Log.Write($"Found source preference {sourcePref}");
        pref = sourcePref;
      }

      else if (urlPreferences.TryGetPreference(uri, out UrlPreference urlPref))
      {
        Log.Write($"Found URL preference {urlPref}");
        pref = urlPref;
      }

      if (pref == null)
      {
        Log.Write($"Unable to find a browser matching \"{url}\".");
        return;
      }

      (string path, string args) = Args.SplitPathAndArgs(pref.Browser.Location);

      args = Args.Format(args, uri);
      args += passThroughArgs;

      Log.Write($"Launching {path} with args \"{args}\"");

      string name = GetAppName(path);
      
      path = Environment.ExpandEnvironmentVariables(path);

      if (!Actions.TryRun(() => Process.Start(path, args)))
      {
        await notifier.NotifyAsync($"Error", $"Could not open {name}. Please check the log for more details.");
        return;
      }

      await notifier.NotifyAsync($"Opening {name}", $"URL: {url}");
    }
    catch (Exception e)
    {
      Log.Write($"{e}");
    }
  }

  private static string GetAppName(string path)
  {
    // Get just the app name from the exe at path
    string name = Path.GetFileNameWithoutExtension(path);
    // make first letter uppercase
    name = name[0].ToString().ToUpper() + name[1..];
    return name;
  }
}
