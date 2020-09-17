using System;
using System.Threading;
using System.Windows.Forms;
using ProcessStub;
using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.ProcessCorrupt;
using RTCV.Vanguard;

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

                    case RTCV.NetCore.Commands.Remote.AllSpecSent:
                        {
                            //We still need to set the emulator's path
                            AllSpec.VanguardSpec.Update(VSPEC.EMUDIR, ProcessWatch.currentDir);
                            SyncObjectSingleton.FormExecute(() =>
                            {
                                ProcessWatch.UpdateDomains();
                            });
                        }
                        break;
                    case RTCV.NetCore.Commands.Basic.SaveSavestate:
                        e.setReturnValue("");
                        break;

                    case RTCV.NetCore.Commands.Basic.LoadSavestate:
                        e.setReturnValue(true);
                        break;

                    case RTCV.NetCore.Commands.Remote.PreCorruptAction:

                        SyncObjectSingleton.FormExecute(() =>
                        {
                            lock (ProcessWatch.CorruptLock)
                            {

                                if (ProcessWatch.SuspendProcess)
                                {
                                    if (!ProcessWatch.p?.Suspend() ?? true && !suspendWarned)
                                    {
                                        suspendWarned = (MessageBox.Show("Failed to suspend a thread!\nWould you like to continue to receive warnings?", "Failed to suspend thread", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes);
                                    }
                                }

                                ProcessWatch.DontChangeMemoryProtection = true;
                                var count = 0;
                                foreach (var m in MemoryDomains.MemoryInterfaces.Values)
                                {
                                    if (m.MD is ProcessMemoryDomain pmd)
                                    {
                                        if (!pmd.SetMemoryProtection(ProcessExtensions.MemoryProtection.ExecuteReadWrite))
                                            count++;
                                    }
                                }

                                Console.WriteLine($"PreCorrupt\n" +
                                                  $"Total domains: {MemoryDomains.MemoryInterfaces.Values.Count}\n" +
                                                  $"Errors: {count}");
                            }
                        });

                        break;

                    case RTCV.NetCore.Commands.Remote.PostCorruptAction:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            lock (ProcessWatch.CorruptLock)
                            {
                                foreach (var m in MemoryDomains.MemoryInterfaces.Values)
                                {
                                    if (m.MD is ProcessMemoryDomain pmd)
                                    {
                                        pmd.ResetMemoryProtection();
                                        pmd.FlushInstructionCache();
                                    }
                                }
                                ProcessWatch.DontChangeMemoryProtection = false;
                                if (ProcessWatch.SuspendProcess)
                                {
                                    if (!ProcessWatch.p?.Resume() ?? true && !suspendWarned)
                                    {
                                        suspendWarned = (MessageBox.Show("Failed to resume a thread!\nWould you like to continue to receive warnings?", "Failed to resume thread", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes);
                                    }
                                }

                            }
                        });
                        break;

                    case RTCV.NetCore.Commands.Remote.CloseGame:
                        break;

                    case RTCV.NetCore.Commands.Remote.DomainGetDomains:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            e.setReturnValue(ProcessWatch.GetInterfaces());
                        });
                        break;

                    case RTCV.NetCore.Commands.Remote.DomainRefreshDomains:
                        SyncObjectSingleton.FormExecute(() => { ProcessWatch.UpdateDomains(); });
                        break;

                    case RTCV.NetCore.Commands.Remote.EventEmuMainFormClose:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            Environment.Exit(0);
                        });
                        break;
                    case RTCV.NetCore.Commands.Remote.IsNormalAdvance:
                        e.setReturnValue(true);
                        break;

                    case RTCV.NetCore.Commands.Remote.EventCloseEmulator:
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
