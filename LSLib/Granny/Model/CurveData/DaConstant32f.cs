﻿using System.Collections.Generic;
using OpenTK;
using LSLib.Granny.GR2;
using System.Diagnostics;

namespace LSLib.Granny.Model.CurveData;

public class DaConstant32f : AnimationCurveData
{
    [Serialization(Type = MemberType.Inline)]
    public CurveDataHeader CurveDataHeader_DaConstant32f;
    public short Padding;
    [Serialization(Prototype = typeof(ControlReal32), Kind = SerializationKind.UserMember, Serializer = typeof(SingleListSerializer))]
    public List<float> Controls;

    public override int NumKnots()
    {
        return 1;
    }

    public override List<float> GetKnots()
    {
        return new() { 0.0f };
    }

    public override List<Matrix3> GetMatrices()
    {
        Debug.Assert(Controls.Count == 9);
        var m = Controls;
        Matrix3 mat = new(
            m[0], m[1], m[2],
            m[3], m[4], m[5],
            m[6], m[7], m[8]
        );

        return new() { mat };
    }
}