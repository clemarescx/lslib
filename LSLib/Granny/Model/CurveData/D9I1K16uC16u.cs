using System;
using System.Collections.Generic;
using OpenTK;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData;

public class D9I1K16uC16u : AnimationCurveData
{
    [Serialization(Type = MemberType.Inline)]
    public CurveDataHeader CurveDataHeader_D9I1K16uC16u;
    public ushort OneOverKnotScaleTrunc;
    public float ControlScale;
    public float ControlOffset;
    [Serialization(Prototype = typeof(ControlUInt16), Kind = SerializationKind.UserMember, Serializer = typeof(UInt16ListSerializer))]
    public List<ushort> KnotsControls;

    public override int NumKnots()
    {
        return KnotsControls.Count / 2;
    }

    public override List<float> GetKnots()
    {
        var scale = ConvertOneOverKnotScaleTrunc(OneOverKnotScaleTrunc);
        var numKnots = NumKnots();
        var knots = new List<float>(numKnots);
        for (var i = 0; i < numKnots; i++)
        {
            knots.Add(KnotsControls[i] / scale);
        }

        return knots;
    }

    public override List<Quaternion> GetQuaternions()
    {
        throw new InvalidOperationException("D9I1K16uC16u is not a rotation curve!");
    }

    public override List<Matrix3> GetMatrices()
    {
        var numKnots = NumKnots();
        var knots = new List<Matrix3>(numKnots);
        for (var i = 0; i < numKnots; i++)
        {
            var scale = KnotsControls[numKnots + i] * ControlScale + ControlOffset;
            var mat = new Matrix3(
                scale, 0, 0,
                0, scale, 0,
                0, 0, scale
            );
            knots.Add(mat);
        }

        return knots;
    }
}