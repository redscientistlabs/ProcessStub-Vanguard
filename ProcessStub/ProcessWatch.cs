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

        public static ProgressForm progressForm;

        public static void Start()
        {
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
                MessageBox.Show(File.ReadAllText(disclaimerPath).Replace("[ver]", ProcessWatch.ProcessStubVersion), "Process Stub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.Create(disclaimerReadPath);
            }

            //If we can't load the dictionary, quit the wgh to prevent the loss of backups
            if (!FileInterface.LoadCompositeFilenameDico(ProcessWatch.currentDir))
                Application.Exit();

        }


        internal static bool LoadTarget()
        {
            using (var f = new HookProcessForm())
            {
                if (f.ShowDialog() != DialogResult.OK)
                    return false;

                if (f.RequestedProcess == null || (f.RequestedProcess?.HasExited ?? true))
                {
                    return false;
                }

                Action<object, EventArgs> action = (ob, ea) =>
                {
                    p = f.RequestedProcess;


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
                    var name = ProcessExtensions.GetModuleFileNameExW(p.Handle, mbi.BaseAddress);
                    if (mbi.State == (uint) ProcessExtensions.MemoryType.MEM_COMMIT && ((mbi.Protect & state) != 0) && (name.Contains(".exe") || name.Contains(".dll"))) 
                    {
                        ProcessMemoryDomain p = new ProcessMemoryDomain(_p, mbi.BaseAddress, (long)mbi.RegionSize);
                        interfaces.Add(new MemoryDomainProxy(p));
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
