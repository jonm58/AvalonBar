﻿using System;
using System.Reflection;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Sidebar.Host
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string logPath = Path.Combine(Settings.Current.path, "Logs");
            string logFile = string.Format(@"{0}\{1}.{2}.{3}.log",
                logPath, DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);

            Directory.CreateDirectory(logPath);

            try
            {
                File.AppendAllText(logFile, string.Format("{0}\r\n{1}\r\n----\r\n",
                    DateTime.UtcNow.ToString(), e.Exception));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to write exception log.\n\nDetails: " + ex.Message,
                    null, MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }

            if (Settings.Current.showErrors)
                TaskDialogs.ErrorDialog.ShowDialog((string)Application.Current.TryFindResource("ErrorOccured1"), String.Format("Error: {0}\nSource: {1}\nSee log for detailed info.", e.Exception.Message, e.Exception.Source), e.Exception);

            e.Handled = true;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
#if !DEBUG
            DispatcherUnhandledException += App_DispatcherUnhandledException;
#endif
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
            LocaleManager.LoadLocale(Settings.Current.path, Settings.Current.locale);

            if (Utils.PriorProcess() != null && e.Args.Length == 0)
            {
                MessageBox.Show(
                    (string)Current.TryFindResource("AlreadyRunning"),
                    "AvalonBar", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
            }

            if (e.Args.Length > 0)
            {
                switch (e.Args[0])
                {
                    case @"/regext":
                        try
                        {
                            RegistryKey key;
                            key = Registry.ClassesRoot;
                            key = key.CreateSubKey(".tile", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            key.SetValue(null, "LongBar.Tile", RegistryValueKind.String);
                            key = Registry.ClassesRoot;
                            key = key.CreateSubKey("LongBar.Tile", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            key.SetValue(null, "LongBar Tile", RegistryValueKind.String);
                            key = key.CreateSubKey("DefaultIcon", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            key.SetValue(null, Settings.Current.path + @"\Slate.dll,0", RegistryValueKind.ExpandString);
                            key = Registry.ClassesRoot;
                            key = key.OpenSubKey("LongBar.Tile", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            key = key.CreateSubKey("shell", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            key = key.CreateSubKey("Install", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            key = key.CreateSubKey("command", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            key.SetValue(null, Settings.Current.path + @"\" + Assembly.GetExecutingAssembly().GetName().Name + @".exe %1", RegistryValueKind.String);
                            key.Close();
                        }
                        catch { }
                        break;

                    case @"/unregext":
                        try
                        {
                            RegistryKey key;
                            key = Registry.ClassesRoot.OpenSubKey(".tile", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            if (key != null)
                                Registry.ClassesRoot.DeleteSubKeyTree(".tile");
                            key = Registry.ClassesRoot.OpenSubKey("LongBar.Tile", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            if (key != null)
                                Registry.ClassesRoot.DeleteSubKeyTree("LongBar.Tile");
                            key.Close();
                        }
                        catch { }
                        break;

                    case "/debug":
                        if (e.Args.Length > 1 && e.Args[1].EndsWith(".dll") && File.Exists(e.Args[1]))
                        {
                            Settings.Current.debug = true;
                            Settings.Current.tileToDebug = e.Args[1];
                        }
                        break;


                    default:
                        foreach (string file in e.Args)
                        {
                            try
                            {
                                string longFile = Path.GetFullPath(file);
                                if (File.Exists(longFile) && Path.GetExtension(longFile) == ".tile")
                                {
                                    FileInfo info = new FileInfo(longFile);
                                    TaskDialogs.TileInstallDialog.ShowDialog(null, info.Name, longFile);
                                }
                            }
                            catch (Exception ex) { MessageBox.Show(ex.Message); }
                        }
                        break;
                }
            }

            var mainWindow = new SidebarWindow();
            mainWindow.Show();
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            AppBar.ResizeBar();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= new EventHandler(SystemEvents_DisplaySettingsChanged);
        }
    }
}