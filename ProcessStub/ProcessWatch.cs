namespace ProcessStub
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows.Forms;
    using RTCV.Common;
    using RTCV.CorruptCore;
    using RTCV.NetCore;
    using RTCV.ProcessCorrupt;
    using Vanguard;

    public static class ProcessWatch
    {
        public static string ProcessStubVersion = "0.1.6";
        public static string currentDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static Process p;
        public static bool UseFiltering = true;
        public static bool UseExceptionHandler = false;
        public static bool UseBlacklist = true;
        public static bool SuspendProcess = false;
        public static bool DontChangeMemoryProtection = false;
        public static object CorruptLock = new object();

        public static ProgressForm progressForm;
        public static volatile System.Timers.Timer AutoHookTimer;
        public static volatile System.Timers.Timer AutoCorruptTimer;
        public static ImageList ProcessIcons = new ImageList();

        public static ProcessExtensions.MemProtection ProtectMode = ProcessExtensions.MemProtection.Memory_ReadWrite;

        public static void Start()
        {
            RTCV.Common.Logging.StartLogging(VanguardCore.logPath);
            AutoHookTimer = new System.Timers.Timer();
            AutoHookTimer.Interval = 5000;
            AutoHookTimer.Elapsed += AutoHookTimer_Elapsed;

            AutoCorruptTimer = new System.Timers.Timer();
            AutoCorruptTimer.Interval = 16;
            AutoCorruptTimer.AutoReset = false;
            AutoCorruptTimer.Elapsed += CorruptTimer_Elapsed;
            AutoCorruptTimer.Start();

            if (VanguardCore.vanguardConnected)
                UpdateDomains();

            //ProcessWatch.currentFileInfo = new ProcessStubFileInfo();

            DisableInterface();
            RtcCore.EmuDirOverride = true; //allows the use of this value before vanguard is connected

            string paramsPath = Path.Combine(ProcessWatch.currentDir, "PARAMS");

            if (!Directory.Exists(paramsPath))
                Directory.CreateDirectory(paramsPath);

            if (!Params.IsParamSet("DISCLAIMERREAD"))
            {
                var disclaimer = $@"Welcome to ProcessStub
Version {ProcessWatch.ProcessStubVersion}

Disclaimer:
This program comes with absolutely ZERO warranty.
You may use it at your own risk.
Be EXTREMELY careful with what you choose to corrupt.
Be aware there is always the chance of damage.

This program inserts random data in hooked processes. There is no way to accurately predict what can happen out of this.
The developers of this software will not be held responsible for any damage caused
as the result of use of this software.

By clicking 'Yes' you agree that you have read this warning in full and are aware of any potential consequences of use of the program. If you do not agree, click 'No' to exit this software.";
                if (MessageBox.Show(disclaimer, "Process Stub", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    Environment.Exit(0);

                Params.SetParam("DISCLAIMERREAD");
            }

            var protectionMode = Params.ReadParam("PROTECTIONMODE");
            try
            {
                if (protectionMode != null)
                    ProtectMode = (ProcessExtensions.MemProtection)Enum.Parse(typeof(ProcessExtensions.MemProtection), protectionMode);
            }
            catch (Exception)
            {
                Params.RemoveParam("PROTECTIONMODE");
                ProtectMode = ProcessExtensions.MemProtection.Memory_ReadWrite;
            }

            UseExceptionHandler = Params.ReadParam("USEEXCEPTIONHANDLER") == "True";
            UseBlacklist = Params.ReadParam("USEBLACKLIST") != "False";
            SuspendProcess = Params.ReadParam("SUSPENDPROCESS") == "True";
            UseFiltering = Params.ReadParam("USEFILTERING") != "False";
        }

        private static void CorruptTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (CorruptLock)
            {
                if (!VanguardCore.vanguardConnected || AllSpec.CorruptCoreSpec == null || (p?.HasExited ?? true))
                {
                    AutoCorruptTimer.Start();
                    return;
                }

                try
                {
                    if (!DontChangeMemoryProtection)
                    {
                        foreach (var m in MemoryDomains.MemoryInterfaces?.Values ?? Enumerable.Empty<MemoryDomainProxy>())
                        {
                            if (m.MD is ProcessMemoryDomain pmd)
                            {
                                pmd.SetMemoryProtection(ProcessExtensions.MemProtection.Memory_ExecuteReadWrite);
                                if (p?.HasExited ?? false)
                                {
                                    Console.WriteLine($"Bad! {pmd.Name}");
                                }
                            }
                        }
                    }

                    try
                    {
                        RtcClock.StepCorrupt(true, true);

                        if (p?.HasExited ?? false)
                        {
                            Console.WriteLine($"Bad2!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"STEP_CORRUPT Error!\n{ex.Message}\n{ex.StackTrace}");
                    }
                }
                finally
                {
                    if (!DontChangeMemoryProtection)
                    {
                        foreach (var m in MemoryDomains.MemoryInterfaces?.Values ?? Enumerable.Empty<MemoryDomainProxy>())
                        {
                            if (m.MD is ProcessMemoryDomain pmd)
                            {
                                pmd.ResetMemoryProtection();
                                pmd.FlushInstructionCache();
                            }

                            if (p?.HasExited ?? false)
                            {
                                Console.WriteLine($"Bad3!");
                            }
                        }
                    }
                }
            }

            if (p.HasExited)
            {
                Console.WriteLine($"Bad4!");
            }
            AutoCorruptTimer.Start();
        }

        internal static bool HuntTarget(string currentTarget)
        {
            throw new NotImplementedException();
        }

        private static void AutoHookTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (p?.HasExited == false)
                    return;
                SyncObjectSingleton.FormExecute(() => S.GET<StubForm>().lbTargetStatus.Text = "Waiting...");
                var procToFind = S.GET<StubForm>().tbAutoAttach.Text;
                if (string.IsNullOrWhiteSpace(procToFind))
                    return;

                SyncObjectSingleton.FormExecute(() => S.GET<StubForm>().lbTargetStatus.Text = "Hooking...");
                var _p = Process.GetProcesses().First(x => x.ProcessName == procToFind);
                if (_p != null)
                {
                    Thread.Sleep(2000); //Give the process 2 seconds
                    SyncObjectSingleton.FormExecute(() =>
                    {
                        LoadTarget(_p);

                        if (!VanguardCore.vanguardConnected)
                            VanguardCore.Start();

                        S.GET<StubForm>().EnableTargetInterface();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AutoHook failed.\n{ex.Message}\n{ex.StackTrace}");
            }
            AutoHookTimer.Start();
        }

        internal static bool LoadTarget(Process _p = null)
        {
            lock (CorruptLock)
            {
                if (_p == null)
                {
                    using (var f = new HookProcessForm())
                    {
                        if (f.ShowDialog() != DialogResult.OK)
                            return false;

                        if (f.RequestedProcess == null || (f.RequestedProcess?.HasExited ?? true))
                        {
                            return false;
                        }

                        if (IsProcessBlacklisted(f.RequestedProcess))
                        {
                            MessageBox.Show("Blacklisted process");
                            return false;
                        }

                        p = f.RequestedProcess;
                    }
                }
                else
                    p = _p;
                /*
                if (UseExceptionHandler)
                {
                    ProcessExtensions.IsWow64Process(p.Handle, out bool is32BitProcess); //This method is stupid and returns the inverse
                    string path = is32BitProcess
                        ? Path.Combine(currentDir, "ExceptionHandler_x86.dll")
                        : Path.Combine(currentDir, "ExceptionHandler_x64.dll");
                    if (File.Exists(path))
                    {
                        try
                        {
                            using (var i = new Injector(InjectionMethod.CreateThread, p.Id, path))
                            {
                                if ((ulong) i.InjectDll() != 0)
                                {
                                    Console.WriteLine("Injected exception helper successfully");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Injection failed! {e}");
                        }
                    }
                }*/

                Action<object, EventArgs> action = (ob, ea) =>
                {
                    if (VanguardCore.vanguardConnected)
                        UpdateDomains();
                };

                Action<object, EventArgs> postAction = (ob, ea) =>
                {
                    if (p == null)
                    {
                        MessageBox.Show("Failed to load target");
                        S.GET<StubForm>().DisableTargetInterface();
                        return;
                    }

                    S.GET<StubForm>().lbTarget.Text = p.ProcessName;
                    S.GET<StubForm>().tbAutoAttach.Text = p.ProcessName;
                    S.GET<StubForm>().lbTargetStatus.Text = "Hooked!";

                    //Refresh the UI
                    //RefreshUIPostLoad();
                };
                S.GET<StubForm>().RunProgressBar($"Loading target...", 0, action, postAction);
            }

            return true;
        }

        internal static bool CloseTarget(bool updateDomains = true)
        {
            p = null;
            if (updateDomains)
                UpdateDomains();
            return true;
        }

        public static void UpdateDomains()
        {
            if (!VanguardCore.vanguardConnected)
                return;
            try
            {
                PartialSpec gameDone = new PartialSpec("VanguardSpec");
                gameDone[VSPEC.SYSTEM] = "FileSystem";
                gameDone[VSPEC.GAMENAME] = p?.ProcessName ?? "UNKNOWN";
                gameDone[VSPEC.SYSTEMPREFIX] = "ProcessStub";
                gameDone[VSPEC.SYSTEMCORE] = "ProcessStub";
                gameDone[VSPEC.OPENROMFILENAME] = p?.ProcessName ?? "UNKNOWN";
                gameDone[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS] = Array.Empty<string>();
                gameDone[VSPEC.MEMORYDOMAINS_INTERFACES] = GetInterfaces();
                gameDone[VSPEC.CORE_DISKBASED] = false;
                AllSpec.VanguardSpec.Update(gameDone);

                //This is local. If the domains changed it propgates over netcore
                LocalNetCoreRouter.Route(RTCV.NetCore.Endpoints.CorruptCore, RTCV.NetCore.Commands.Remote.EventDomainsUpdated, true, true);

                //Asks RTC to restrict any features unsupported by the stub
                LocalNetCoreRouter.Route(RTCV.NetCore.Endpoints.CorruptCore, RTCV.NetCore.Commands.Remote.EventRestrictFeatures, true, true);
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();
            }
        }

        public static MemoryDomainProxy[] GetInterfaces()
        {
            try
            {
                Console.WriteLine($"getInterfaces()");
                try
                {
                    if (ProcessExtensions.GetProcessSafe(p) == null)
                    {
                        Console.WriteLine($"p was null!");
                        return Array.Empty<MemoryDomainProxy>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"GetInterfaces threw an exception!\n{e.Message}\n{e.StackTrace}");
                    return Array.Empty<MemoryDomainProxy>();
                }

                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();

                var _p = ProcessExtensions.GetProcessSafe(p);
                if (IsProcessBlacklisted(_p))
                    return Array.Empty<MemoryDomainProxy>();

                ProcessExtensions.GetSystemInfo(out var info);
                IntPtr minAddr = info.minimumApplicationAddress;
                IntPtr maxAddr = info.maximumApplicationAddress;
                long minAddr_l = (long)info.minimumApplicationAddress;
                long maxAddr_l = (long)info.maximumApplicationAddress;
                while (minAddr_l < maxAddr_l)
                {
                    _p = ProcessExtensions.GetProcessSafe(p);
                    if (_p == null)
                        return Array.Empty<MemoryDomainProxy>();

                    if (ProcessExtensions.VirtualQueryEx(_p, minAddr, out var mbi) == false)
                    {
                        break;
                    }

                    if (mbi.State == (uint)ProcessExtensions.MemType.MEMORY_COMMIT &&
                        mbi.Protect != ProcessExtensions.MemProtection.Memory_NoAccess && //Hard blacklist
                        mbi.Protect != ProcessExtensions.MemProtection.Memory_ZeroAccess && //Hard blacklist
                        (mbi.Protect | ProtectMode) == ProtectMode)
                    {
                        var name = ProcessExtensions.GetMappedFileNameW(_p.Handle, mbi.BaseAddress);
                        if (string.IsNullOrWhiteSpace(name) || !IsPathBlacklisted(name))
                        {
                            if (string.IsNullOrWhiteSpace(name))
                                name = "UNKNOWN";
                            var filters = S.GET<StubForm>().tbFilterText.Text.Split('\n').Select(x => x.Trim()).ToArray();
                            if (!UseFiltering || filters.Any(x => name.ToUpper().Contains(x.ToUpper())))
                            {
                                Console.WriteLine($"Adding mbi {name.Split('\\').Last()}  {mbi.Protect} | {ProtectMode}");
                                ProcessMemoryDomain pmd = new ProcessMemoryDomain(_p, mbi.BaseAddress, (long)mbi.RegionSize);
                                interfaces.Add(new MemoryDomainProxy(pmd));
                            }
                        }
                    }
                    minAddr_l += (long)mbi.RegionSize;
                    minAddr = (IntPtr)minAddr_l;
                }

                Console.WriteLine("Done adding domains");
                Thread.Sleep(1000);
                return interfaces.ToArray();
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();

                return Array.Empty<MemoryDomainProxy>();
            }
        }

        public static void EnableInterface()
        {
        }

        public static void DisableInterface()
        {
        }

        public static bool IsProcessBlacklisted(Process _p)
        {
            if (!UseBlacklist)
                return false;

            try
            {
                return IsModuleBlacklisted(_p.MainModule);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine($"IsProcessBlacklisted threw exception {e}");
                return true;
            }
        }

        public static bool IsModuleBlacklisted(ProcessModule pm)
        {
            if (!UseBlacklist)
                return false;
            try
            {
                var filename = "";
                if (!string.IsNullOrWhiteSpace(pm?.FileName))
                    filename = Path.GetFileName(pm.FileName);

                if (IsExecutableNameBlacklisted(filename))
                    return true;

                if (IsPathBlacklisted(filename))
                    return true;

                if (pm?.FileVersionInfo?.ProductName != null)
                    if (IsProductNameBlacklisted(pm.FileVersionInfo?.ProductName))
                        return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"IsModuleBlacklisted failed!\n{e.Message}\n{e.StackTrace}");
                return false;
            }

            return false;
        }

        public static bool IsPathBlacklisted(string path)
        {
            if (!UseBlacklist)
                return false;

            var blacklisted = new string[]
            {
                "\\Windows\\",
                "\\windows\\",
            };
            if (blacklisted.Any(x => path.Contains(x)))
                return true;
            return false;
        }
        public static bool IsProductNameBlacklisted(string productName)
        {
            if (!UseBlacklist)
                return false;

            var blacklisted = new string[]
            {
                "Microsoft® Windows® Operating System",
            };
            if (blacklisted.Any(x => productName.Equals(x, StringComparison.OrdinalIgnoreCase)))
                return true;
            return false;
        }

        public static bool IsExecutableNameBlacklisted(string executableName)
        {
            //We have certain files we never want corrupted because we know people are going to be stupid and corrupt these online games
            var hardBlacklisted = new string[]
            {
                "r5apex", //Apex Legends
                "Roblox", //Roblox
                "RobloxPlayerLauncher", //Roblox
                "FortniteLauncher",
                "FortniteClient-Win64-Shipping_EAC",
                "FortniteClient-Win64-Shipping_BE",
                "FortniteClient-Win64-Shipping",
                "TsLGame", //pubg
                "SteamService", //Hosts VAC
                "Steam",
                "steamwebhelper",
                "Origin",
                "OriginWebHelperService",
                "Discord",
            };
            if (hardBlacklisted.Any(x => executableName.Equals(x, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (!UseBlacklist)
                return false;

            var blacklisted = Array.Empty<string>();
            if (blacklisted.Any(x => executableName.Equals(x, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }
    }
}
