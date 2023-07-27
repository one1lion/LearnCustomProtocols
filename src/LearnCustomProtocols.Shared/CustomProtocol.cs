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

        DisableOpenWarning($"Software\\Wow6432Node\\Microsoft\\Internet Explorer\\ProtocolExecute\\{_urlProtocol}");
        AddSecurityWarningKey("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\Low Rights\\ElevationPolicy");
    }

    public void UnregisterUrlProtocol()
    {
        Registry.CurrentUser.DeleteSubKeyTree($"SOFTWARE\\Classes\\{_urlProtocol}", false);
        Registry.ClassesRoot.DeleteSubKeyTree(_urlProtocol);
        Registry.LocalMachine.DeleteSubKeyTree("Software\\Microsoft\\Internet Explorer\\ProtocolExecute\\" + _urlProtocol);
        Registry.LocalMachine.DeleteSubKeyTree("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\ProtocolExecute\\" + _urlProtocol);

        RemoveSecurityWarningKey("Software\\Microsoft\\Internet Explorer\\Low Rights\\ElevationPolicy");
        RemoveSecurityWarningKey("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\Low Rights\\ElevationPolicy");
    }

    private static bool DisableOpenWarning(string regPath)
    {
        try
        {
            var rKey = Registry.LocalMachine.OpenSubKey(regPath, true) ?? Registry.LocalMachine.CreateSubKey(regPath);
            rKey.SetValue("WarnOnOpen", 0, RegistryValueKind.DWord);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool AddSecurityWarningKey(string regPath)
    {
        try
        {
            var appName = _exePath[(_exePath.LastIndexOf("\\") + 1)..];
            var appPath = _exePath[.._exePath.LastIndexOf("\\")];
            using var rKey = Registry.LocalMachine.OpenSubKey(regPath, true);
            string? elevationGuid = null;
            RegistryKey? elevationGuidKey = null;
            bool keyExists = false;
            if (rKey != null)
                foreach (string elevationGuidVar in rKey.GetSubKeyNames())
                {
                    elevationGuid = elevationGuidVar;
                    elevationGuidKey = Registry.LocalMachine.OpenSubKey(regPath + "\\" + elevationGuid);
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
            if (keyExists)
            {
                elevationGuidKey = Registry.LocalMachine.OpenSubKey(regPath + "\\" + elevationGuid, true);
            }
            else
            {
                elevationGuid = "{" + (Guid.NewGuid()).ToString() + "}";
                elevationGuidKey = Registry.LocalMachine.CreateSubKey(regPath + "\\" + elevationGuid);
            }
            if (elevationGuidKey != null)
            {
                elevationGuidKey.SetValue("AppName", appName);
                elevationGuidKey.SetValue("AppPath", appPath);
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
            var appName = _exePath[(_exePath.LastIndexOf("\\") + 1)..];
            string? elevationGuid = null;
            RegistryKey? elevationGuidKey = null;

            using var rKey = Registry.LocalMachine.OpenSubKey(regPath, true);

            if (rKey != null)
            {
                foreach (string elevationGuidVar in rKey.GetSubKeyNames())
                {
                    elevationGuid = elevationGuidVar;
                    elevationGuidKey = Registry.LocalMachine.OpenSubKey(regPath + "\\" + elevationGuid);
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
            if (elevationGuidKey != null && elevationGuid != null)
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
