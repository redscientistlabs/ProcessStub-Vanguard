using System;
using System.Threading;
using System.Windows.Forms;
using ProcessStub;
using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.Vanguard;
using static RTCV.NetCore.NetcoreCommands;

namespace Vanguard
{
    public static class VanguardImplementation
    {
        public static VanguardConnector connector;
        private static bool suspendWarned = false;

        public static void StartClient()
        {
            try
            {
                ConsoleEx.WriteLine("Starting Vanguard Client");
                Thread.Sleep(500); //When starting in Multiple Startup Project, the first try will be uncessful since
                                   //the server takes a bit more time to start then the client.

                var spec = new NetCoreReceiver();
                spec.Attached = VanguardCore.attached;
                spec.MessageReceived += OnMessageReceived;

                connector = new VanguardConnector(spec);
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new AbortEverythingException();
            }
        }

        public static void RestartClient()
        {
            connector?.Kill();
            connector = null;
            StartClient();
        }

        private static void OnMessageReceived(object sender, NetCoreEventArgs e)
        {
            try
            {
                // This is where you implement interaction.
                // Warning: Any error thrown in here will be caught by NetCore and handled by being displayed in the console.

                var message = e.message;
                var simpleMessage = message as NetCoreSimpleMessage;
                var advancedMessage = message as NetCoreAdvancedMessage;

                ConsoleEx.WriteLine(message.Type);
                switch (message.Type) //Handle received messages here
                {

                    case REMOTE_ALLSPECSSENT:
                        {
                            //We still need to set the emulator's path
                            AllSpec.VanguardSpec.Update(VSPEC.EMUDIR, ProcessWatch.currentDir);
                            SyncObjectSingleton.FormExecute(() =>
                            {
                                ProcessWatch.UpdateDomains();
                            });
                        }
                        break;
                    case SAVESAVESTATE:
                        e.setReturnValue("");
                        break;

                    case LOADSAVESTATE:
                        e.setReturnValue(true);
                        break;

                    case REMOTE_PRECORRUPTACTION:
                        if (ProcessWatch.SuspendProcess)
                        {
                            if (!ProcessWatch.p?.Suspend() ?? true && !suspendWarned)
                            {
                                suspendWarned = (MessageBox.Show("Failed to suspend a thread!\nWould you like to continue to receive warnings?", "Failed to suspend thread", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes);
                            }
                        }

                        foreach (var m in MemoryDomains.MemoryInterfaces.Values)
                        {
                            if (m.MD is ProcessMemoryDomain pmd)
                            {
                                pmd.SetMemoryProtection(Jupiter.MemoryProtection.ExecuteReadWrite);
                            }
                        }

                        break;

                    case REMOTE_POSTCORRUPTACTION:

                        foreach (var m in MemoryDomains.MemoryInterfaces.Values)
                        {
                            if (m.MD is ProcessMemoryDomain pmd)
                            {
                                pmd.ResetMemoryProtection();
                            }
                        }

                        if (ProcessWatch.SuspendProcess)
                        {
                            if (!ProcessWatch.p?.Resume() ?? true && !suspendWarned)
                            {
                                suspendWarned = (MessageBox.Show("Failed to resume a thread!\nWould you like to continue to receive warnings?", "Failed to resume thread", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes);
                            }
                        }

                        break;

                    case REMOTE_CLOSEGAME:
                        break;

                    case REMOTE_DOMAIN_GETDOMAINS:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            e.setReturnValue(ProcessWatch.GetInterfaces());
                        });
                        break;

                    case REMOTE_DOMAIN_REFRESHDOMAINS:
                        SyncObjectSingleton.FormExecute(() => { ProcessWatch.UpdateDomains(); });
                        break;

                    case REMOTE_EVENT_EMU_MAINFORM_CLOSE:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            Environment.Exit(0);
                        });
                        break;
                    case REMOTE_ISNORMALADVANCE:
                        e.setReturnValue(true);
                        break;

                    case REMOTE_EVENT_CLOSEEMULATOR:
                        Environment.Exit(-1);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new AbortEverythingException();
            }
        }
    }
}
