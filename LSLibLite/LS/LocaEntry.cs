using System.Runtime.InteropServices;
using System.Text;

namespace LSLibLite.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LocaEntry
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] Key;

    public UInt16 Version;
    public UInt32 Length;

    public string KeyString
    {
        get
        {
            var nameLen = Array.FindIndex(Key, c => c == 0) is var nullIdx and not -1
                ? nullIdx
                : Key.Length;

            return Encoding.UTF8.GetString(Key, 0, nameLen);
        }

        init
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Key = new byte[64];
            Array.Clear(Key, 0, Key.Length);
            Array.Copy(bytes, Key, bytes.Length);
        }
    }
}