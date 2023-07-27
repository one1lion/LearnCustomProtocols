using Microsoft.Win32;
using System.Reflection;

namespace LearnCustomProtocols.Shared;

public class CustomProtocol
{
    private readonly string _urlProtocol;
    private readonly string _exePath;

    public CustomProtocol(string protocolName, string? manualExeFullFileName = null, string? exeNameIfVsHost = null)
    {
        _urlProtocol = protocolName;
        _exePath = manualExeFullFileName ?? Environment.ProcessPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(manualExeFullFileName) && _exePath.Contains(".vshost"))
        {
            var processWorkingDirectory = Path.GetDirectoryName(_exePath) ?? string.Empty;
            _exePath = Path.Combine(processWorkingDirectory, exeNameIfVsHost ?? Path.GetFileName(Assembly.GetExecutingAssembly().Location));
        }
    }

    public void RegisterUrlProtocol()
    {
        using RegistryKey protocolKey = Registry.ClassesRoot.CreateSubKey(_urlProtocol);
        protocolKey.SetValue(null, $"URL:{_urlProtocol} Protocol");
        protocolKey.SetValue("URL Protocol", "");

        using RegistryKey shellKey = protocolKey.CreateSubKey("shell");
        using RegistryKey openKey = shellKey.CreateSubKey("open");
        openKey.SetValue(null, $@"""{_exePath}"" %1");

        using RegistryKey commandKey = openKey.CreateSubKey("command");
        commandKey.SetValue(null, $@"""{_exePath}"" %1");

        using RegistryKey friendlyNameKey = protocolKey.CreateSubKey("FriendlyTypeName");
        friendlyNameKey.SetValue("", $"Learning to use protocols - {_urlProtocol}");

        // Flush the changes to the registry
        protocolKey.Flush();

        var accumEx = new List<Exception?>()
        {
            TryStep(() => DisableOpenWarning($"Software\\Wow6432Node\\Microsoft\\Internet Explorer\\ProtocolExecute\\{_urlProtocol}"), out var _),
            TryStep(() => AddSecurityWarningKey("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\Low Rights\\ElevationPolicy"), out var _)
        };

        if (accumEx.Any(x => x is not null))
        {
            throw new AggregateException(accumEx.Where(x => x is not null).Select(x => x!));
        }
    }

    public void UnregisterUrlProtocol()
    {
        var accumEx = new List<Exception?>()
        {
            TryStep(() => Registry.CurrentUser.DeleteSubKeyTree($"SOFTWARE\\Classes\\{_urlProtocol}", false)),
            TryStep(() => Registry.ClassesRoot.DeleteSubKeyTree(_urlProtocol)),
            TryStep(() => Registry.LocalMachine.DeleteSubKeyTree($"Software\\Microsoft\\Internet Explorer\\ProtocolExecute\\{_urlProtocol}")),
            TryStep(() => Registry.LocalMachine.DeleteSubKeyTree($"Software\\Wow6432Node\\Microsoft\\Internet Explorer\\ProtocolExecute\\{_urlProtocol}")),

            TryStep(() => RemoveSecurityWarningKey("Software\\Microsoft\\Internet Explorer\\Low Rights\\ElevationPolicy")),
            TryStep(() => RemoveSecurityWarningKey("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\Low Rights\\ElevationPolicy"))
        };

        if (accumEx.Any(x => x is not null))
        {
            throw new AggregateException(accumEx.Where(x => x is not null).Select(x => x!));
        }
    }

    private static Exception? TryStep(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    
    private static Exception? TryStep<T>(Func<T> function, out T? retVal)
    {
        try
        {
            retVal = function();
            return null;
        }
        catch (Exception ex)
        {
            retVal = default;
            return ex;
        }
    }

    private static bool DisableOpenWarning(string regPath)
    {
        RegistryKey? rKey = null;
        try
        {
            rKey = Registry.LocalMachine.OpenSubKey(regPath, true) ?? Registry.LocalMachine.CreateSubKey(regPath);
            rKey.SetValue("WarnOnOpen", 0, RegistryValueKind.DWord);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            rKey?.Dispose();
        }
    }

    private bool AddSecurityWarningKey(string regPath)
    {
        try
        {
            using var rKey = Registry.LocalMachine.OpenSubKey(regPath, true);

            var appName = Path.GetFileName(_exePath);
            var appPath = Path.GetDirectoryName(_exePath);

            string? elevationGuid = null;
            RegistryKey? elevationGuidKey = null;

            var keyExists = false;
            if (rKey is not null)
            {
                foreach (string elevationGuidVar in rKey.GetSubKeyNames())
                {
                    elevationGuid = elevationGuidVar;
                    elevationGuidKey = Registry.LocalMachine.OpenSubKey($"{regPath}\\{elevationGuid}");
                    if (elevationGuidKey == null)
                    {
                        continue;
                    }
                    if (elevationGuidKey.GetValue("AppName", string.Empty)?.ToString() == appName)
                    {
                        keyExists = true;
                        break;
                    }
                }
            }
            if (keyExists)
            {
                elevationGuidKey = Registry.LocalMachine.OpenSubKey($"{regPath}\\{elevationGuid}", true);
            }
            else
            {
                elevationGuid = $"{{{Guid.NewGuid()}}}";
                elevationGuidKey = Registry.LocalMachine.CreateSubKey($"{regPath}\\{elevationGuid}");
            }
            if (elevationGuidKey is not null)
            {
                elevationGuidKey.SetValue("AppName", appName);
                elevationGuidKey.SetValue("AppPath", appPath ?? Environment.CurrentDirectory);
                elevationGuidKey.SetValue("Policy", 3, RegistryValueKind.DWord);
                elevationGuidKey.Close();
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool RemoveSecurityWarningKey(string regPath)
    {
        try
        {
            var appName = Path.GetFileName(_exePath);
            string? elevationGuid = null;
            RegistryKey? elevationGuidKey = null;

            using var rKey = Registry.LocalMachine.OpenSubKey(regPath, true);

            if (rKey is not null)
            {
                foreach (string elevationGuidVar in rKey.GetSubKeyNames())
                {
                    elevationGuid = elevationGuidVar;
                    elevationGuidKey = Registry.LocalMachine.OpenSubKey($"{regPath}\\{elevationGuid}");
                    if (elevationGuidKey == null)
                    {
                        continue;
                    }
                    else if (elevationGuidKey.GetValue("AppName", string.Empty)?.ToString() == appName)
                    {
                        break;
                    }
                }
            }
            if (elevationGuidKey is not null && elevationGuid is not null)
            {
                elevationGuidKey.Close();
                rKey?.DeleteSubKeyTree(elevationGuid);
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


}
