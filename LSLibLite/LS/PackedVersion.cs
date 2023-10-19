namespace LSLibLite.LS;

public struct PackedVersion
{
    public uint Major;
    public uint Minor;
    public uint Revision;
    public uint Build;

    public static PackedVersion FromInt64(long packed)
    {
        return new PackedVersion
        {
            Major = (uint)(packed >> 55 & 0x7f),
            Minor = (uint)(packed >> 47 & 0xff),
            Revision = (uint)(packed >> 31 & 0xffff),
            Build = (uint)(packed & 0x7fffffff)
        };
    }

    public static PackedVersion FromInt32(int packed)
    {
        return new PackedVersion
        {
            Major = (uint)(packed >> 28 & 0x0f),
            Minor = (uint)(packed >> 24 & 0x0f),
            Revision = (uint)(packed >> 16 & 0xff),
            Build = (uint)(packed & 0xffff)
        };
    }

    public readonly int ToVersion32()
    {
        return (int)((Major & 0x0f) << 28 | (Minor & 0x0f) << 24 | (Revision & 0xff) << 16 | (Build & 0xffff) << 0);
    }

    public readonly long ToVersion64()
    {
        return ((long)Major & 0x7f) << 55 | ((long)Minor & 0xff) << 47 | ((long)Revision & 0xffff) << 31 | ((long)Build & 0x7fffffff) << 0;
    }
}