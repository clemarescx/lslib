using LSLib.Granny.GR2;

namespace LSLib.Granny.Model;

public class ArtToolInfo
{
    public string FromArtToolName;
    public int ArtToolMajorRevision;
    public int ArtToolMinorRevision;
    [Serialization(MinVersion = 0x80000011)]
    public int ArtToolPointerSize;
    public float UnitsPerMeter;
    [Serialization(ArraySize = 3)]
    public float[] Origin;
    [Serialization(ArraySize = 3)]
    public float[] RightVector;
    [Serialization(ArraySize = 3)]
    public float[] UpVector;
    [Serialization(ArraySize = 3)]
    public float[] BackVector;
    [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000011)]
    public object ExtendedData;

    public void SetYUp()
    {
        RightVector = new float[] { 1, 0, 0 };
        UpVector = new float[] { 0, 1, 0 };
        BackVector = new float[] { 0, 0, -1 };
    }

    public void SetZUp()
    {
        RightVector = new float[] { 1, 0, 0 };
        UpVector = new float[] { 0, 0, 1 };
        BackVector = new float[] { 0, 1, 0 };
    }
}

public class ExporterInfo
{
    public string ExporterName;
    public int ExporterMajorRevision;
    public int ExporterMinorRevision;
    public int ExporterCustomization;
    public int ExporterBuildNumber;
    [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000011)]
    public object ExtendedData;
}