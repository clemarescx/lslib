﻿using System.Collections.Generic;
using OpenTK;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData;

public class D3I1K8uC8u : AnimationCurveData
{
    [Serialization(Type = MemberType.Inline)]
    public CurveDataHeader CurveDataHeader_D3I1K8uC8u;
    public ushort OneOverKnotScaleTrunc;
    [Serialization(ArraySize = 3)]
    public float[] ControlScales;
    [Serialization(ArraySize = 3)]
    public float[] ControlOffsets;
    [Serialization(Prototype = typeof(ControlUInt8), Kind = SerializationKind.UserMember, Serializer = typeof(UInt8ListSerializer))]
    public List<byte> KnotsControls;

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

    public override List<Vector3> GetPoints()
    {
        var numKnots = NumKnots();
        var knots = new List<Vector3>(numKnots);
        for (var i = 0; i < numKnots; i++)
        {
            var vec = new Vector3(
                KnotsControls[numKnots + i] * ControlScales[0] + ControlOffsets[0],
                KnotsControls[numKnots + i] * ControlScales[1] + ControlOffsets[1],
                KnotsControls[numKnots + i] * ControlScales[2] + ControlOffsets[2]
            );
            knots.Add(vec);
        }

        return knots;
    }
}