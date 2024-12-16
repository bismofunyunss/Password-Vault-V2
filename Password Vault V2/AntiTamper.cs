using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Secure_Password_Vault
{
    public static class AntiTamper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct CONTEXT
        {
            public uint P1Home;
            public uint P2Home;
            public uint P3Home;
            public uint P4Home;
            public uint P5Home;
            public uint P6Home;
            public long ContextFlags;
            public uint Dr0;
            public uint Dr1;
            public uint Dr2;
            public uint Dr3;
            public uint Dr4;
            public uint Dr5;
            public uint Dr6;
            public uint Dr7;
        }

        public struct PROCESS_MITIGATION_BINARY_SIGNATURE_POLICY
        {
            public uint MicrosoftSignedOnly;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct SYSTEM_CODEINTEGRITY_INFORMATION
        {
            [FieldOffset(0)]
            public ulong Length;

            [FieldOffset(4)]
            public uint CodeIntegrityOptions;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_KERNEL_DEBUGGER_INFORMATION
        {
            [MarshalAs(UnmanagedType.U1)]
            public bool KernelDebuggerEnabled;

            [MarshalAs(UnmanagedType.U1)]
            public bool KernelDebuggerNotPresent;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        public struct ANSI_STRING
        {
            public short Length;
            public short MaximumLength;
            public string Buffer;
        }

        public static class Dependencies
        {
            [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] ref bool isDebuggerPresent);

            [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsDebuggerPresent();

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string lib);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr GetProcAddress(IntPtr ModuleHandle, string Function);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteProcessMemory(SafeHandle ProcHandle, IntPtr BaseAddress, byte[] Buffer, uint size, int NumOfBytes);

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern uint NtSetInformationThread(IntPtr ThreadHandle, uint ThreadInformationClass, IntPtr ThreadInformation, int ThreadInformationLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenThread(uint DesiredAccess, bool InheritHandle, int ThreadId);

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern bool NtClose(IntPtr Handle);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteProcessMemory(IntPtr ProcHandle, IntPtr BaseAddress, byte[] Buffer, uint size, int NumOfBytes);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetProcessMitigationPolicy(int policy, ref PROCESS_MITIGATION_BINARY_SIGNATURE_POLICY lpBuffer, int size);

            [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern void RtlInitUnicodeString(out UNICODE_STRING DestinationString, string SourceString);

            [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern void RtlUnicodeStringToAnsiString(out ANSI_STRING DestinationString, UNICODE_STRING UnicodeString, bool AllocateDestinationString);

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern uint LdrGetDllHandle([MarshalAs(UnmanagedType.LPWStr)] string? DllPath, [MarshalAs(UnmanagedType.LPWStr)] string? DllCharacteristics, UNICODE_STRING LibraryName, ref IntPtr DllHandle);

            [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern uint LdrGetProcedureAddress(IntPtr Module, ANSI_STRING ProcedureName, ushort ProcedureNumber, out IntPtr FunctionHandle);

            [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
            public static extern IntPtr ZeroMemory(IntPtr addr, IntPtr size);

            [DllImport("kernel32.dll")]
            public static extern IntPtr VirtualProtect(IntPtr lpAddress, IntPtr dwSize, IntPtr flNewProtect, ref IntPtr lpflOldProtect);
        }

        public static async Task<bool> PerformChecks()
        {
            return await Task.Run(() =>
            {
                    if (CheckDebuggerManagedPresent())
                        return true;

                    if (CheckDebuggerUnmanagedPresent())
                        return true;

                    if (CheckRemoteDebugger())
                        return true;

                    if (CheckProcessList())
                        return true;

                    if (AntiDebugAttach())
                        return true;

                    if (HideThreadsAntiDebug())
                        return true;

                    if (PatchLoadLibraryA())
                        return true;

                    if (PatchLoadLibraryW())
                        return true;

                    if (DetectBadInstructionsOnCommonAntiDebuggingFunctions())
                        return true;

                    return false;
            });
        }

        /// <summary>
        /// Asks the CLR for the presence of an attached managed debugger, and never even bothers to check for the presence of a native debugger.
        /// </summary>
        private static bool CheckDebuggerManagedPresent()
        {
            if (Debugger.IsAttached)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Asks the kernel for the presence of an attached native debugger, and has no knowledge of managed debuggers.
        /// </summary>
        private static bool CheckDebuggerUnmanagedPresent()
        {
            if (Dependencies.IsDebuggerPresent())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether a process is being debugged.
        /// </summary>
        /// <remarks>
        /// The "remote" in CheckRemoteDebuggerPresent does not imply that the debugger
        /// necessarily resides on a different computer; instead, it indicates that the 
        /// debugger resides in a separate and parallel process.
        /// </remarks>
        private static bool CheckRemoteDebugger()
        {
            var isDebuggerPresent = false;

            var bApiRet = Dependencies.CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);

            if (bApiRet && isDebuggerPresent)
            {
                return true;
            }

            return false;
        }

        private static bool CheckProcessList()
        {
            var strArray = new string[41]
                   {
                    "codecracker",
                    "x32dbg",
                    "x64dbg",
                    "ollydbg",
                    "ida",
                    "charles",
                    "dnspy",
                    "simpleassembly",
                    "peek",
                    "httpanalyzer",
                    "httpdebug",
                    "fiddler",
                    "wireshark",
                    "dbx",
                    "mdbg",
                    "gdb",
                    "windbg",
                    "dbgclr",
                    "kdb",
                    "kgdb",
                    "mdb",
                    "processhacker",
                    "scylla_x86",
                    "scylla_x64",
                    "scylla",
                    "idau64",
                    "idau",
                    "idaq",
                    "idaq64",
                    "idaw",
                    "idaw64",
                    "idag",
                    "idag64",
                    "ida64",
                    "ida",
                    "ImportREC",
                    "IMMUNITYDEBUGGER",
                    "MegaDumper",
                    "CodeBrowser",
                    "reshacker",
                    "cheat engine"
                   };
            foreach (var process in Process.GetProcesses())
                if (process != Process.GetCurrentProcess())
                    for (var index = 0; index < strArray.Length; ++index)
                    {
                        if (process.ProcessName.ToLower().Contains(strArray[index]))
                            return true;

                        if (process.MainWindowTitle.ToLower().Contains(strArray[index]))
                            return true;
                    }
            return false;
        }

        public static bool AntiDebugAttach()
        {
            IntPtr NtdllModule = Dependencies.GetModuleHandle("ntdll.dll");
            if (NtdllModule == IntPtr.Zero)
            {
                MessageBox.Show("An error has occured.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }

            IntPtr DbgUiRemoteBreakinAddress = Dependencies.GetProcAddress(NtdllModule, "DbgUiRemoteBreakin");
            if (DbgUiRemoteBreakinAddress == IntPtr.Zero)
            {
                MessageBox.Show("An error has occured.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }

            IntPtr DbgBreakPointAddress = Dependencies.GetProcAddress(NtdllModule, "DbgBreakPoint");
            if (DbgBreakPointAddress == IntPtr.Zero)
            {
                MessageBox.Show("An error has occured.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            byte[] Int3InvaildCode = { 0xCC };
            byte[] RetCode = { 0xC3 };
            bool Status = Dependencies.WriteProcessMemory(Process.GetCurrentProcess().SafeHandle, DbgUiRemoteBreakinAddress, Int3InvaildCode, 1, 0);
            bool Status2 = Dependencies.WriteProcessMemory(Process.GetCurrentProcess().SafeHandle, DbgBreakPointAddress, RetCode, 1, 0);
            if (Status && Status2)
                return false;
            return true;
        }

        public static bool HideThreadsAntiDebug()
        {
            try
            {
                bool AnyThreadFailed = false;
                ProcessThreadCollection GetCurrentProcessThreads = Process.GetCurrentProcess().Threads;
                foreach (ProcessThread Threads in GetCurrentProcessThreads)
                {
                    IntPtr ThreadHandle = Dependencies.OpenThread(0x0020, false, Threads.Id);
                    if (ThreadHandle != IntPtr.Zero)
                    {
                        uint Status = Dependencies.NtSetInformationThread(ThreadHandle, 0x11, IntPtr.Zero, 0);
                        Dependencies.NtClose(ThreadHandle);
                        if (Status != 0x00000000)
                            AnyThreadFailed = true;
                    }
                }
                if (!AnyThreadFailed)
                    return false;
                return true;
            }
            catch
            {
                return true;
            }
        }

        public static bool PatchLoadLibraryA()
        {
            IntPtr KernelModule = Dependencies.GetModuleHandle("kernelbase.dll");
            IntPtr LoadLibraryA = Dependencies.GetProcAddress(KernelModule, "LoadLibraryA");

            byte[] HookedCode = { 0xC2, 0x04, 0x00 };
            bool Status = Dependencies.WriteProcessMemory(Process.GetCurrentProcess().Handle, LoadLibraryA, HookedCode, 3, 0);
            if (Status)
                return false;
            return true;
        }

        public static bool PatchLoadLibraryW()
        {
            IntPtr KernelModule = Dependencies.GetModuleHandle("kernelbase.dll");
            IntPtr LoadLibraryW = Dependencies.GetProcAddress(KernelModule, "LoadLibraryW");

            byte[] HookedCode = { 0xC2, 0x04, 0x00 };
            bool Status = Dependencies.WriteProcessMemory(Process.GetCurrentProcess().Handle, LoadLibraryW, HookedCode, 3, 0);
            if (Status)
                return false;
            return true;
        }

        public static bool BinaryImageSignatureMitigationAntiDllInjection()
        {
            PROCESS_MITIGATION_BINARY_SIGNATURE_POLICY OnlyMicrosoftBinaries = new PROCESS_MITIGATION_BINARY_SIGNATURE_POLICY();

            OnlyMicrosoftBinaries.MicrosoftSignedOnly = 1;

            if (string.Equals(Process.GetCurrentProcess().ProcessName, "MemoryManager.dll"))
            {
                OnlyMicrosoftBinaries.MicrosoftSignedOnly = 1;
            }
            if (string.Equals(Process.GetCurrentProcess().ProcessName, "libsodium.dll"))
            {
                OnlyMicrosoftBinaries.MicrosoftSignedOnly = 1;
            }
            if (string.Equals(Process.GetCurrentProcess().ProcessName, "BouncyCastle.Cryptography.dll"))
            {
                OnlyMicrosoftBinaries.MicrosoftSignedOnly = 1;
            }
            if (string.Equals(Process.GetCurrentProcess().ProcessName, "Konscious.Security.Cryptography.dll"))
            {
                OnlyMicrosoftBinaries.MicrosoftSignedOnly = 1;
            }
            if (Dependencies.SetProcessMitigationPolicy(8, ref OnlyMicrosoftBinaries, Marshal.SizeOf(typeof(PROCESS_MITIGATION_BINARY_SIGNATURE_POLICY))))
                return false;
            return true;
        }

        private static IntPtr LowLevelGetModuleHandle(string Library)
        {
            IntPtr hModule = IntPtr.Zero;
            UNICODE_STRING UnicodeString = new UNICODE_STRING();
            Dependencies.RtlInitUnicodeString(out UnicodeString, Library);
            Dependencies.LdrGetDllHandle(null, null, UnicodeString, ref hModule);
            return hModule;
        }

        private static IntPtr LowLevelGetProcAddress(IntPtr hModule, string Function)
        {
            IntPtr FunctionHandle = IntPtr.Zero;
            UNICODE_STRING UnicodeString = new UNICODE_STRING();
            ANSI_STRING AnsiString = new ANSI_STRING();
            Dependencies.RtlInitUnicodeString(out UnicodeString, Function);
            Dependencies.RtlUnicodeStringToAnsiString(out AnsiString, UnicodeString, true);
            Dependencies.LdrGetProcedureAddress(hModule, AnsiString, 0, out FunctionHandle);
            return FunctionHandle;
        }

        public static bool DetectBadInstructionsOnCommonAntiDebuggingFunctions()
        {
            string[] Libraries = { "kernel32.dll", "kernelbase.dll", "ntdll.dll", "win32u.dll" };
            string[] KernelLibAntiDebugFunctions = { "IsDebuggerPresent", "CheckRemoteDebuggerPresent", "GetThreadContext", "CloseHandle", "OutputDebugStringA", "GetTickCount", "SetHandleInformation" };
            string[] NtdllAntiDebugFunctions = { "NtQueryInformationProcess", "NtSetInformationThread", "NtClose", "NtGetContextThread", "NtQuerySystemInformation" };
            string[] Win32uAntiDebugFunctions = { "NtUserBlockInput", "NtUserFindWindowEx", "NtUserQueryWindow", "NtUserGetForegroundWindow" };
            foreach (string Library in Libraries)
            {
                IntPtr hModule = LowLevelGetModuleHandle(Library);
                if (hModule != IntPtr.Zero)
                {
                    switch (Library)
                    {
                        case "kernel32.dll":
                            {
                                try
                                {
                                    foreach (string AntiDebugFunction in KernelLibAntiDebugFunctions)
                                    {
                                        IntPtr Function = LowLevelGetProcAddress(hModule, AntiDebugFunction);
                                        byte[] FunctionBytes = new byte[1];
                                        Marshal.Copy(Function, FunctionBytes, 0, 1);
                                        if (FunctionBytes[0] == 0x90 || FunctionBytes[0] == 0xE9)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                            break;
                        case "kernelbase.dll":
                            {
                                try
                                {
                                    foreach (string AntiDebugFunction in KernelLibAntiDebugFunctions)
                                    {
                                        IntPtr Function = LowLevelGetProcAddress(hModule, AntiDebugFunction);
                                        byte[] FunctionBytes = new byte[1];
                                        Marshal.Copy(Function, FunctionBytes, 0, 1);
                                        if (FunctionBytes[0] == 255 || FunctionBytes[0] == 0x90 || FunctionBytes[0] == 0xE9)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                            break;
                        case "ntdll.dll":
                            {
                                try
                                {
                                    foreach (string AntiDebugFunction in NtdllAntiDebugFunctions)
                                    {
                                        IntPtr Function = LowLevelGetProcAddress(hModule, AntiDebugFunction);
                                        byte[] FunctionBytes = new byte[1];
                                        Marshal.Copy(Function, FunctionBytes, 0, 1);
                                        if (FunctionBytes[0] == 255 || FunctionBytes[0] == 0x90 || FunctionBytes[0] == 0xE9)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                            break;
                        case "win32u.dll":
                            {
                                try
                                {
                                    foreach (string AntiDebugFunction in Win32uAntiDebugFunctions)
                                    {
                                        IntPtr Function = LowLevelGetProcAddress(hModule, AntiDebugFunction);
                                        byte[] FunctionBytes = new byte[1];
                                        Marshal.Copy(Function, FunctionBytes, 0, 1);
                                        if (FunctionBytes[0] == 255 || FunctionBytes[0] == 0x90 || FunctionBytes[0] == 0xE9)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                            break;
                    }
                }
            }
            return false;
        }
    }
}