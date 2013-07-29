using System;
using System.Runtime.InteropServices;

namespace XmlRpc_Wrapper
{
    public static class XmlRpcUtil
    {
        private static printint _PRINTINT;
        private static printstr _PRINTSTR;

        private static void thisishowawesomeyouare(string s)
        {
            Console.WriteLine(s);
        }

        public static void ShowOutputFromXmlRpcPInvoke()
        {
            if (_PRINTSTR == null)
            {
                _PRINTSTR = thisishowawesomeyouare;
                SetAwesomeFunctionPtr(_PRINTSTR);
            }
        }

        #region bad voodoo
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void printstr(string s);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void printint(int val);

        [DllImport("XmlRpcWin32.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int IntegerEcho(int val);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "IntegerEchoFunctionPtr", CallingConvention = CallingConvention.Cdecl
            )]
        private static extern void IntegerEchoFunctionPtr([MarshalAs(UnmanagedType.FunctionPtr)] printint callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "IntegerEchoRepeat", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IntegerEchoRepeat(int val);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "SetStringOutFunc", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetAwesomeFunctionPtr(
            [MarshalAs(UnmanagedType.FunctionPtr)] printstr callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "StringPassingTest", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StringTest([In] [Out] [MarshalAs(UnmanagedType.LPStr)] string str);
        #endregion
    }
}