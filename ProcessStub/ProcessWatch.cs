using Newtonsoft.Json;
using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.NetCore.StaticTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanguard;

namespace ProcessStub
{
    public static class ProcessWatch
    {
        public static string ProcessStubVersion = "0.0.1";
        public static string currentDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static Process p;
        public static bool UseFiltering = true;
        static int CPU_STEP_Count = 0;

        public static ProgressForm progressForm;
        public static System.Timers.Timer AutoHookTimer;
        public static System.Timers.Timer AutoCorruptTimer;

        public static void Start()
        {
            AutoHookTimer = new System.Timers.Timer();
            AutoHookTimer.Interval = 5000;
            AutoHookTimer.Elapsed += AutoHookTimer_Elapsed;

            AutoCorruptTimer = new System.Timers.Timer();
            AutoCorruptTimer.Interval = 16;
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

            string disclaimerPath = Path.Combine(currentDir, "LICENSES", "DISCLAIMER.TXT");
            string disclaimerReadPath = Path.Combine(currentDir, "PARAMS", "DISCLAIMERREAD");

            if (File.Exists(disclaimerPath) && !File.Exists(disclaimerReadPath))
            {
                if(MessageBox.Show(File.ReadAllText(disclaimerPath).Replace("[ver]", ProcessWatch.ProcessStubVersion), "Process Stub", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    Environment.Exit(0);
                File.Create(disclaimerReadPath);
            }
        }

        private static void CorruptTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!VanguardCore.vanguardConnected)
            {
                AutoCorruptTimer.Start();
                return;
            }

            try
            {
                StepActions.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Corrupt Error!\n{ex.Message}\n{ex.StackTrace}");
            }

            CPU_STEP_Count++;
            bool autoCorrupt = RtcCore.AutoCorrupt;
            long errorDelay = RtcCore.ErrorDelay;
            if (autoCorrupt && CPU_STEP_Count >= errorDelay)
            {
                try
                {
                    CPU_STEP_Count = 0;
                    BlastLayer bl = RtcCore.GenerateBlastLayer((string[])AllSpec.UISpec["SELECTEDDOMAINS"]);
                    if (bl != null)
                        bl.Apply(false, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AutoCorrupt Error!\n{ex.Message}\n{ex.StackTrace}");
                }

            }

            AutoCorruptTimer.Start();
        }

        private static void AutoHookTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (p?.HasExited == false)
                    return;
                SyncObjectSingleton.FormExecute(() => S.GET<StubForm>().lbTargetStatus.Text = "Waiting...");
                var procToFind = S.GET<StubForm>().tbAutoAttach.Text;
                if (String.IsNullOrWhiteSpace(procToFind))
                    return;

                SyncObjectSingleton.FormExecute(() => S.GET<StubForm>().lbTargetStatus.Text = "Hooking...");
                var _p = Process.GetProcesses().First(x => x.ProcessName == procToFind);
                if (_p != null)
                {
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

                    p = f.RequestedProcess;
                }
            }
            else
                p = _p;

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
                S.GET<StubForm>().lbTargetStatus.Text = "Hooked!";


                //Refresh the UI
                //RefreshUIPostLoad();
            };
            S.GET<StubForm>().RunProgressBar($"Loading target...", 0, action, postAction);

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
            try
            {
                PartialSpec gameDone = new PartialSpec("VanguardSpec");
                gameDone[VSPEC.SYSTEM] = "FileSystem";
                gameDone[VSPEC.GAMENAME] = p?.ProcessName ?? "UNKNOWN";
                gameDone[VSPEC.SYSTEMPREFIX] = "ProcessStub";
                gameDone[VSPEC.SYSTEMCORE] = "ProcessStub";
                gameDone[VSPEC.OPENROMFILENAME] = p?.ProcessName ?? "UNKNOWN";
                gameDone[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS] = new string[0];
                gameDone[VSPEC.MEMORYDOMAINS_INTERFACES] = GetInterfaces();
                gameDone[VSPEC.CORE_DISKBASED] = false;
                AllSpec.VanguardSpec.Update(gameDone);

                //This is local. If the domains changed it propgates over netcore
                LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.REMOTE_EVENT_DOMAINSUPDATED, true, true);

                //Asks RTC to restrict any features unsupported by the stub
                LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.REMOTE_EVENT_RESTRICTFEATURES, true, true);

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
                        return new MemoryDomainProxy[] { };
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"GetInterfaces threw an exception!\n{e.Message}\n{e.StackTrace}");
                    return new MemoryDomainProxy[] { };
                }

                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();

                IntPtr addr = new IntPtr(0L);
                while (true)
                {
                    var _p = ProcessExtensions.GetProcessSafe(p);
                    if (_p == null)
                        return new MemoryDomainProxy[] { };

                    if (ProcessExtensions.VirtualQueryEx(_p, addr, out var mbi) == false)
                    {
                        break;
                    }

                    var state = Jupiter.MemoryProtection.ReadWrite;
                    var name = ProcessExtensions.GetModuleFileNameExW(_p.Handle, mbi.BaseAddress);

                    var filters = S.GET<StubForm>().tbFilterText.Text.Split('\n').Select(x => x.Trim()).ToArray();
                    if (mbi.State == (uint) ProcessExtensions.MemoryType.MEM_COMMIT && ((mbi.Protect & state) != 0) && filters.Any(x => name.ToUpper().Contains(x.ToUpper()))) 
                    {
                        ProcessMemoryDomain pmd = new ProcessMemoryDomain(_p, mbi.BaseAddress, (long)mbi.RegionSize);
                        interfaces.Add(new MemoryDomainProxy(pmd));
                    }

                    Console.WriteLine(ProcessExtensions.GetModuleFileNameExW(_p.Handle, mbi.BaseAddress));

                    addr = new IntPtr((long)mbi.BaseAddress + (long)mbi.RegionSize);
                }


                return interfaces.ToArray();
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();

                return new MemoryDomainProxy[] { };
            }

        }

        public static void EnableInterface()
        {
        }

        public static void DisableInterface()
        {
        }

    }


}
