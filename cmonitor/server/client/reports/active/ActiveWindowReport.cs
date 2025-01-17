﻿using cmonitor.server.client.reports.system;
using common.libs;
using MemoryPack;
#if DEBUG || RELEASE
using Microsoft.Win32;
#endif
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace cmonitor.server.client.reports.active
{
    public sealed class ActiveWindowReport : IReport
    {
        public string Name => "ActiveWindow";

        private readonly ClientConfig clientConfig;
        private readonly ActiveWindowTimeManager activeWindowTimeManager = new ActiveWindowTimeManager();
        private ActiveReportInfo report = new ActiveReportInfo();

        private uint lastPid = 0;
        private string lastTitle = string.Empty;
        private int count = 0;
        public ActiveWindowReport(Config config, ClientConfig clientConfig)
        {
            this.clientConfig = clientConfig;
            if (config.IsCLient)
            {
                Timers();
                DisallowInit();

                DisallowRun(clientConfig.WindowNames);

                AppDomain.CurrentDomain.ProcessExit += (s, e) => DisallowRun(Array.Empty<string>());
                Console.CancelKeyPress += (s, e) => DisallowRun(Array.Empty<string>());
            }


        }

        long ticks = DateTime.UtcNow.Ticks;
        public object GetReports(ReportType reportType)
        {
            ticks = DateTime.UtcNow.Ticks;
            if (reportType == ReportType.Full || report.Pid != lastPid || report.Title != lastTitle || report.DisallowCount != count)
            {
                lastPid = report.Pid;
                lastTitle = report.Title;
                count = report.DisallowCount;
                return report;
            }
            return null;
        }
        public ActiveWindowTimeReportInfo GetActiveWindowTimes()
        {
            return activeWindowTimeManager.GetActiveWindowTimes();
        }
        public void ClearActiveWindowTimes()
        {
            activeWindowTimeManager.Clear();
        }


        private void Timers()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if ((DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond < 1000 || disallowNames.Length > 0)
                    {
                        try
                        {

                            GetActiveWindow();
                            report.WindowCount = GetWindowCount();
                            if (Disallow() == false)
                            {
                                //activeWindowTimeManager.Update(report);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error(ex);
                        }
                    }

                    await Task.Delay(500);
                }
            }, TaskCreationOptions.LongRunning);
        }


        const int nChars = 256;
        private StringBuilder buff = new StringBuilder(nChars);
        private void GetActiveWindow()
        {
            IntPtr handle = GetForegroundWindow();
            GetWindowThreadProcessId(handle, out uint id);
            if (GetWindowText(handle, buff, nChars) > 0)
            {
                Process p = Process.GetProcessById((int)id);
                string desc = string.Empty;
                string filename = string.Empty;

                try
                {
                    ProcessModule main = p.MainModule;
                    if (main != null)
                    {
                        filename = main.FileName;
                        desc = main.FileVersionInfo.FileDescription;
                    }
                }

                catch (Exception)
                {
                }

                report.Title = buff.ToString();
                report.FileName = filename;
                report.Desc = desc;
                report.Pid = id;
                return;
            }
            report.Title = string.Empty;
            report.FileName = string.Empty;
            report.Desc = string.Empty;
            report.Pid = 0;
        }


        private string[] disallowNames = Array.Empty<string>();
        public void DisallowRun(string[] names)
        {
            clientConfig.WindowNames = names;
            DisallowRun(false);
            DisallowRunClear();
            report.DisallowCount = names.Length;
            disallowNames = names;

            Task.Run(() =>
            {
                if (names.Length > 0)
                {
                    DisallowRun(true);
                    DisallowRunFileNames(names);
                }
                CommandHelper.Windows(string.Empty, new string[] { "gpupdate /force" });
            });
        }
        private bool Disallow()
        {
            if (disallowNames.Length > 0)
            {
                try
                {
                    ReadOnlySpan<char> filenameSpan = report.FileName.AsSpan();
                    uint pid = report.Pid;
                    foreach (string item in disallowNames)
                    {
                        ReadOnlySpan<char> nameSpan = item.AsSpan();
                        bool result = item == report.Title
                            || (filenameSpan.Length >= nameSpan.Length && filenameSpan.Slice(filenameSpan.Length - nameSpan.Length, nameSpan.Length).SequenceEqual(nameSpan));
                        if (result)
                        {
                            Task.Run(() =>
                            {
                                CommandHelper.Windows(string.Empty, new string[] { $"taskkill /f /pid {pid}" });
                            });
                        }
                    }
                }
                catch (Exception)
                {
                }
                return true;
            }
            return false;
        }
        private void DisallowInit()
        {
            CreateKey();
            DisallowRunClear();
            DisallowRun(false);
            Task.Run(() =>
            {
                CommandHelper.Windows(string.Empty, new string[] { "gpupdate /force" });
            });
        }
        private void DisallowRunClear()
        {
#if DEBUG || RELEASE
            try
            {

                if (OperatingSystem.IsWindows())
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\DisallowRun", true);
                    if (key != null)
                    {
                        string[] names = key.GetValueNames();
                        if (names != null)
                        {
                            foreach (string name in names)
                            {
                                key.DeleteValue(name, false);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"application disallow clear {ex}");
            }
#endif
        }
        private void DisallowRunFileNames(string[] filenames)
        {
#if DEBUG || RELEASE
            try
            {
                if (OperatingSystem.IsWindows())
                {

                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\DisallowRun", true);
                    if (key != null)
                    {
                        foreach (string filename in filenames)
                        {
                            key.SetValue(filename, filename, RegistryValueKind.String);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"application disallow {string.Join(",", filenames)} {ex}");
            }
#endif
        }
        private void DisallowRun(bool value)
        {
#if DEBUG || RELEASE
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", true);
                    if (key != null)
                    {
                        key.SetValue("DisallowRun", value ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception)
            {
            }
#endif
        }
        private void CreateKey()
        {
#if DEBUG || RELEASE
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", true);
                    RegistryKey disallowRun = key.OpenSubKey("DisallowRun");
                    if (disallowRun == null)
                    {
                        key.CreateSubKey("DisallowRun");
                    }
                }
            }
            catch (Exception)
            {
            }
#endif
        }

        private int GetWindowCount()
        {
            int length = 0;
            EnumWindows((IntPtr hWnd, IntPtr lParam) =>
            {
                try
                {
                    if (IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) > 0)
                    {
                        length++;
                    }
                }
                catch (Exception)
                {
                }

                return true;
            }, IntPtr.Zero);
            return length;
        }
        public Dictionary<uint, string> GetWIndows()
        {
            Dictionary<uint, string> dic = new Dictionary<uint, string>();
            StringBuilder lpString = new StringBuilder(256);
            EnumWindows((IntPtr hWnd, IntPtr lParam) =>
            {
                try
                {
                    if (IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) > 0)
                    {
                        GetWindowText(hWnd, lpString, 256);
                        GetWindowThreadProcessId(hWnd, out uint id);

                        dic[id] = lpString.ToString();
                    }
                }
                catch (Exception)
                {
                }

                return true;
            }, IntPtr.Zero);

            return dic;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);
    }

    public sealed class ActiveReportInfo
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public uint Pid { get; set; }
        public int DisallowCount { get; set; }
        public int WindowCount { get; set; }
    }

    public sealed class ActiveWindow
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;

        public uint Pid = 0;
    }
    public sealed class ActiveWindowTimeManager
    {
        private ConcurrentDictionary<string, ActiveWindowTimeInfo> dic = new ConcurrentDictionary<string, ActiveWindowTimeInfo>();
        private string lastFileName = string.Empty;
        private string lastTitle = string.Empty;
        private DateTime StartTime = DateTime.Now;
        public void Clear()
        {
            StartTime = DateTime.Now;
            dic.Clear();
            GC.Collect();
        }
        public ActiveWindowTimeReportInfo GetActiveWindowTimes()
        {
            return new ActiveWindowTimeReportInfo
            {
                StartTime = StartTime,
                List = dic.Values.ToList()
            };
        }
        public void Update(ActiveReportInfo active)
        {
            if (string.IsNullOrWhiteSpace(active.FileName)) return;

            int index = active.FileName.LastIndexOf('\\');
            string filename = active.FileName;
            if (index >= 0)
            {
                filename = filename.Substring(index + 1, filename.Length - index - 1);
            }

            if (dic.TryGetValue(filename, out ActiveWindowTimeInfo info) == false)
            {
                info = new ActiveWindowTimeInfo
                {
                    FileName = filename,
                    Desc = active.Desc,
                    StartTime = DateTime.Now,
                    Titles = new Dictionary<string, uint>()
                };
                dic.TryAdd(filename, info);
            }
            if (string.IsNullOrWhiteSpace(lastFileName) == false)
            {
                if (dic.TryGetValue(lastFileName, out ActiveWindowTimeInfo lastInfo))
                {
                    lastInfo.Time += (ulong)(DateTime.Now - lastInfo.StartTime).TotalMilliseconds;

                    if (string.IsNullOrWhiteSpace(lastTitle) == false)
                    {
                        if (info.Titles.TryGetValue(lastTitle, out uint times) == false)
                        {
                            info.Titles.TryAdd(lastTitle, 0);
                        }
                        info.Titles[lastTitle] += (uint)(DateTime.Now - lastInfo.StartTime).TotalMilliseconds;
                    }
                }
            }

            info.StartTime = DateTime.Now;

            lastFileName = filename;
            lastTitle = active.Title;

        }
    }


    [MemoryPackable]
    public sealed partial class ActiveWindowTimeReportInfo
    {
        public DateTime StartTime { get; set; } = DateTime.Now;
        public List<ActiveWindowTimeInfo> List { get; set; } = new List<ActiveWindowTimeInfo>();
    }
    [MemoryPackable]
    public sealed partial class ActiveWindowTimeInfo
    {
        public string FileName { get; set; }
        public string Desc { get; set; }
        public ulong Time { get; set; }
        public DateTime StartTime { get; set; }
        public Dictionary<string, uint> Titles { get; set; }
    }
}
