using System;
using System.Runtime.InteropServices;
using System.Threading;
using EasyHook;

namespace AnimalWellInjector
{
    public class EntryPoint : IEntryPoint
    {
        // TODO: rework this to scan for AOBs instead of a fixed value so this doesn't break every version
        private const long CommonVsprintfAddress = 0x14013B6CC;
        private readonly HookInterface _server;

        public EntryPoint(
            RemoteHooking.IContext context,
            string channelName
        )
        {
            _server = RemoteHooking.IpcConnectClient<HookInterface>(channelName);
            _server.Ping();
        }

        public unsafe void Run(RemoteHooking.IContext context, string channelName)
        {
            _server.ReportMessage($"Injected into: {RemoteHooking.GetCurrentProcessId()}");

            // TODO: abstract hook setup into modules
            var commonVsprintfHook = LocalHook.Create(
                new IntPtr(CommonVsprintfAddress), // TODO: try LocalHook.GetProcAddress, probably won't work because Animal Well doesn't use external library imports
                new CommonVsprintfDelegate(CommonVsprintfHook),
                this
            );

            // Activate hooks on all threads except the current thread
            commonVsprintfHook.ThreadACL.SetExclusiveACL(new[] { 0 });

            try
            {
                _server.ReportMessage($"Vsprintf hook: {commonVsprintfHook.HookBypassAddress.ToString("x")}");

                // Poll for server shutdown request. Don't try using events or tasks, as they don't get serialized.
                while (!_server.IsStoppingRequested) Thread.Sleep(1000);

                _server.ReportMessage("Disposing hooks.");
            }
            catch
            {
                // Swallow exceptions from server communication, in case it gets shutdown prematurely.
            }

            commonVsprintfHook.Dispose();
            LocalHook.Release();
        }

        private unsafe int CommonVsprintfHook(
            ulong options,
            char* buffer,
            uint bufferLength,
            string format,
            IntPtr locale,
            RuntimeArgumentHandle va
        )
        {
            var length =
                Marshal.GetDelegateForFunctionPointer<CommonVsprintfDelegate>(new IntPtr(CommonVsprintfAddress))(
                    options,
                    buffer,
                    bufferLength,
                    format,
                    locale,
                    va
                );
            try
            {
                if (length > 0) _server.ReportMessage(new string(buffer, 0, length));
            }
            catch
            {
                // Swallow exceptions from server communication
            }
            return length;
        }

        /*
         * Reference:
         *                                 unsigned __int64    const   options,
           _Out_writes_z_(buffer_count)    Character*          const   buffer,
                                           size_t              const   buffer_count,
                                           Character const*    const   format,
                                           _locale_t           const   locale,
                                           va_list             const   arglist
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Unicode)]
        private unsafe delegate int CommonVsprintfDelegate(
            ulong options,
            char* buffer, // StringBuilder instead of char* did not work well.
            uint bufferLength,
            string format,
            IntPtr locale,
            RuntimeArgumentHandle va
        );
    }
}