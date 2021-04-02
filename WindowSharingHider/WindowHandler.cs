using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowSharingHider
{
    public static class WindowHandler
    {
        [DllImport("user32")] static extern Boolean EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32")] static extern Boolean IsWindowVisible(IntPtr hWnd);
        [DllImport("dwmapi.dll")] static extern Int32 DwmGetWindowAttribute(IntPtr hwnd, Int32 dwAttribute, out Int32 pvAttribute, Int32 cbAttribute);

        [DllImport("user32")] static extern IntPtr GetWindowText(IntPtr hWnd, StringBuilder lpString, Int32 nMaxCount);
        [DllImport("user32")] static extern Int32 GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32")] static extern Boolean GetWindowDisplayAffinity(IntPtr hWnd, out Int32 dwAffinity);
        [DllImport("user32")] static extern UInt32 GetWindowThreadProcessId(IntPtr hWnd, out Int32 processId);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("kernel32")] static extern IntPtr OpenProcess(Int32 dwDesiredAccess, Boolean bInheritHandle, Int32 dwProcessId);
        [DllImport("kernel32")] static extern IntPtr GetModuleHandle(String lpModuleName);
        [DllImport("kernel32")] static extern IntPtr GetProcAddress(IntPtr hModule, String procName);
        [DllImport("kernel32")] static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, Int32 dwSize, UInt32 flAllocationType, UInt32 flProtect);
        [DllImport("kernel32")] static extern Int32 ReadProcessMemory(IntPtr hProcess, UInt64 lpBaseAddress, [In, Out] Byte[] buffer, Int32 size, out Int32 lpNumberOfBytesRead);
        [DllImport("kernel32")] static extern Boolean WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, Byte[] lpBuffer, Int32 nSize, out Int32 lpNumberOfBytesWritten);
        [DllImport("kernel32")] static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, UInt32 dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, UInt32 dwCreationFlags, IntPtr lpThreadId);
        [DllImport("kernel32")] static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
        [DllImport("kernel32")] static extern Int32 CloseHandle(IntPtr hObject);
        [DllImport("kernel32")] static extern Boolean VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, Int32 dwSize, Int32 dwFreeType);
        [DllImport("kernel32")] static extern Boolean IsWow64Process(IntPtr processHandle, out Boolean wow64Process);
        [DllImport("psapi")] static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, UInt32 cb);
        [DllImport("psapi")] static extern bool EnumProcessModulesEx(IntPtr hProcess, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)][In][Out] IntPtr[] lphModule, UInt32 cb, out UInt32 lpcbNeeded, UInt32 dwFilterFlag);
        [DllImport("psapi")] static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, UInt32 nSize);
        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public UInt32 SizeOfImage;
            public IntPtr EntryPoint;
        }
        public static Dictionary<IntPtr, String> GetVisibleWindows()
        {
            var windows = new Dictionary<IntPtr, String>();
            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;
                DwmGetWindowAttribute(hWnd, 14, out Int32 pvAttribute, 4);
                if (pvAttribute == 2) return true;
                var length = GetWindowTextLength(hWnd);
                if (length == 0) return true;
                var windowTextBuilder = new StringBuilder(length + 1);
                GetWindowText(hWnd, windowTextBuilder, windowTextBuilder.Capacity);
                windows[hWnd] = windowTextBuilder.ToString();
                return true;
            }, IntPtr.Zero);
            return windows;
        }
        public static Int32 GetWindowDisplayAffinity(IntPtr hWnd)
        {
            GetWindowDisplayAffinity(hWnd, out Int32 dwAffinity);
            return dwAffinity;
        }
        public static Int32 ReadInt32(IntPtr procHandle, UInt64 addr, Boolean is32Bit)
        {
            var buffer = new Byte[8];
            ReadProcessMemory(procHandle, (UInt64)addr, buffer, 8, out _);
            return BitConverter.ToInt32(buffer, 0);
        }
        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity current process has to own hWnd.. but does it really matter??
        public static void SetWindowDisplayAffinity(IntPtr hWnd, Int32 dwAffinity)
        {
            GetWindowThreadProcessId(hWnd, out Int32 procId);
            var procHandle = OpenProcess(0x38, true, procId);
            if (!IsWow64Process(procHandle, out bool is32Bit)) is32Bit = IntPtr.Size == 4;

            var ptrs = new IntPtr[0];
            EnumProcessModulesEx(procHandle, ptrs, 0, out UInt32 bytesNeeded, 3);
            var size = is32Bit ? 4 : 8;
            var moduleCount = bytesNeeded / size;
            ptrs = new IntPtr[moduleCount];
            EnumProcessModulesEx(procHandle, ptrs, bytesNeeded, out _, 3);

            var SetWindowDisplayAffinityAddr = 0ul;
            for (var i = 0; i < moduleCount && SetWindowDisplayAffinityAddr == 0; i++)
            {
                var path = new StringBuilder(260);
                GetModuleFileNameEx(procHandle, ptrs[i], path, 260);

                if (path.ToString().ToLower().Contains("user32.dll"))
                {
                    GetModuleInformation(procHandle, ptrs[i], out MODULEINFO info, (uint)(size * ptrs.Length));
                    var e_lfanew = ReadInt32(procHandle, (UInt64)info.lpBaseOfDll + 0x3C, is32Bit);
                    var ntHeaders = info.lpBaseOfDll + e_lfanew;
                    var optionalHeader = ntHeaders + 0x18;
                    var dataDirectory = optionalHeader + (is32Bit ? 0x60 : 0x70);
                    var exportDirectory = info.lpBaseOfDll + ReadInt32(procHandle, (UInt64)dataDirectory, is32Bit);
                    var names = info.lpBaseOfDll + ReadInt32(procHandle, (UInt64)exportDirectory + 0x20, is32Bit);
                    var ordinals = info.lpBaseOfDll + ReadInt32(procHandle, (UInt64)exportDirectory + 0x24, is32Bit);
                    var functions = info.lpBaseOfDll + ReadInt32(procHandle, (UInt64)exportDirectory + 0x1C, is32Bit);
                    var numFuncs = ReadInt32(procHandle, (UInt64)exportDirectory + 0x18, is32Bit);

                    for (var j = 0u; j < numFuncs && SetWindowDisplayAffinityAddr == 0; j++)
                    {
                        var offset = (UInt64)ReadInt32(procHandle, (UInt64)names + j * 4, is32Bit);
                        var buffer = new Byte[32];
                        ReadProcessMemory(procHandle, (UInt64)info.lpBaseOfDll + offset, buffer, 32, out _);
                        var name = Encoding.UTF8.GetString(buffer);
                        var ordinal = (UInt64)(ReadInt32(procHandle, (UInt64)ordinals + j * 2, is32Bit) & 0xFFFF);
                        var address = (UInt64)info.lpBaseOfDll + (UInt64)ReadInt32(procHandle, (UInt64)functions + ordinal * 4, is32Bit);
                        if (name.StartsWith("SetWindowDisplayAffinity")) SetWindowDisplayAffinityAddr = address;
                    }
                }
            }

            var asm = new List<Byte>();
            if (is32Bit)
            {
                asm.Add(0x68); // push
                asm.AddRange(BitConverter.GetBytes((UInt32)dwAffinity));
                asm.Add(0x68); // push
                asm.AddRange(BitConverter.GetBytes((UInt32)hWnd));
                asm.Add(0xB8); // mov eax
                asm.AddRange(BitConverter.GetBytes((UInt32)SetWindowDisplayAffinityAddr));
                asm.AddRange(new Byte[] { 0xFF, 0xD0 }); // call eax
            }
            else
            {
                asm.AddRange(new Byte[] { 0x48, 0x83, 0xEC, 0x30 }); // sub rsp 0x30

                asm.AddRange(new Byte[] { 0x48, 0xB9 }); // mov rcx
                asm.AddRange(BitConverter.GetBytes((UInt64)hWnd));

                asm.AddRange(new Byte[] { 0x48, 0xBA }); // mov rdx
                asm.AddRange(BitConverter.GetBytes((UInt64)dwAffinity));

                asm.AddRange(new Byte[] { 0x48, 0xB8 }); // mov rax
                asm.AddRange(BitConverter.GetBytes((UInt64)SetWindowDisplayAffinityAddr));

                asm.AddRange(new Byte[] { 0xFF, 0xD0 }); // call rax
                asm.AddRange(new Byte[] { 0x48, 0x83, 0xC4, 0x30 }); // add rsp 0x30
            }
            asm.Add(0xC3); // ret
            var codePtr = VirtualAllocEx(procHandle, IntPtr.Zero, asm.Count, 0x1000, 0x40);
            WriteProcessMemory(procHandle, codePtr, asm.ToArray(), asm.Count, out Int32 bytesWritten);

            var thread = CreateRemoteThread(procHandle, IntPtr.Zero, 0, codePtr, IntPtr.Zero, 0, IntPtr.Zero);
            WaitForSingleObject(thread, 10000);
            VirtualFreeEx(procHandle, codePtr, 0, 0x8000);
            CloseHandle(procHandle);
        }
    }
}
