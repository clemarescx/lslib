﻿using System.Collections.Generic;
using OpenTK;
using System.Diagnostics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData;

public class DaK8uC8u : AnimationCurveData
{
    [Serialization(Type = MemberType.Inline)]
    public CurveDataHeader CurveDataHeader_DaK8uC8u;
    public ushort OneOverKnotScaleTrunc;
    [Serialization(Prototype = typeof(ControlReal32), Kind = SerializationKind.UserMember, Serializer = typeof(SingleListSerializer))]
    public List<float> ControlScaleOffsets;
    [Serialization(Prototype = typeof(ControlUInt8), Kind = SerializationKind.UserMember, Serializer = typeof(UInt8ListSerializer))]
    public List<byte> KnotsControls;

    public int Components()
    {
        return ControlScaleOffsets.Count / 2;
    }

    public override int NumKnots()
    {
        return KnotsControls.Count / (Components() + 1);
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

    public override List<Matrix3> GetMatrices()
    {
        Debug.Assert(Components() == 9);
        var numKnots = NumKnots();
        var knots = new List<Matrix3>(numKnots);
        for (var i = 0; i < numKnots; i++)
        {
            var mat = new Matrix3(
                KnotsControls[numKnots + i * 9 + 0] * ControlScaleOffsets[0] + ControlScaleOffsets[9 + 0],
                KnotsControls[numKnots + i * 9 + 1] * ControlScaleOffsets[1] + ControlScaleOffsets[9 + 1],
                KnotsControls[numKnots + i * 9 + 2] * ControlScaleOffsets[2] + ControlScaleOffsets[9 + 2],
                KnotsControls[numKnots + i * 9 + 3] * ControlScaleOffsets[3] + ControlScaleOffsets[9 + 3],
                KnotsControls[numKnots + i * 9 + 4] * ControlScaleOffsets[4] + ControlScaleOffsets[9 + 4],
                KnotsControls[numKnots + i * 9 + 5] * ControlScaleOffsets[5] + ControlScaleOffsets[9 + 5],
                KnotsControls[numKnots + i * 9 + 6] * ControlScaleOffsets[6] + ControlScaleOffsets[9 + 6],
                KnotsControls[numKnots + i * 9 + 7] * ControlScaleOffsets[7] + ControlScaleOffsets[9 + 7],
                KnotsControls[numKnots + i * 9 + 8] * ControlScaleOffsets[8] + ControlScaleOffsets[9 + 8]
            );
            knots.Add(mat);
        }

        return knots;
    }

    public override List<Quaternion> GetQuaternions()
    {
        Debug.Assert(Components() == 4);
        var numKnots = NumKnots();
        var quats = new List<Quaternion>(numKnots);
        for (var i = 0; i < numKnots; i++)
        {
            var quat = new Quaternion(
                KnotsControls[numKnots + i * 4 + 0] * ControlScaleOffsets[0] + ControlScaleOffsets[4 + 0],
                KnotsControls[numKnots + i * 4 + 1] * ControlScaleOffsets[1] + ControlScaleOffsets[4 + 1],
                KnotsControls[numKnots + i * 4 + 2] * ControlScaleOffsets[2] + ControlScaleOffsets[4 + 2],
                KnotsControls[numKnots + i * 4 + 3] * ControlScaleOffsets[3] + ControlScaleOffsets[4 + 3]
            );
            quats.Add(quat);
        }

        return quats;
    }
}