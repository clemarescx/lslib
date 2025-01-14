﻿using System.Collections.Generic;
using System.Linq;
using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using OpenTK;

namespace LSLib.Granny;

public class ColladaAnimation
{
    private animation Animation;
    private Dictionary<string, ColladaSource> Sources;
    private List<Matrix4> Transforms;
    private List<float> Times;
    private string BoneName;

    public float Duration => Times[^1];

    private void ImportSources()
    {
        Sources = new();
        foreach (var item in Animation.Items)
        {
            if (item is source source)
            {
                var src = ColladaSource.FromCollada(source);
                Sources.Add(src.id, src);
            }
        }
    }

    private void ImportSampler()
    {
        sampler sampler = null;
        foreach (var item in Animation.Items)
        {
            if (item is sampler item1)
            {
                sampler = item1;
                break;
            }
        }

        if (sampler == null)
        {
            throw new ParsingException($"Animation {Animation.id} has no sampler!");
        }

        ColladaSource inputSource = null, outputSource = null, interpolationSource = null;
        foreach (var input in sampler.input)
        {
            if (input.source[0] != '#')
            {
                throw new ParsingException("Only ID references are supported for animation input sources");
            }

            if (!Sources.TryGetValue(input.source[1..], out var source))
            {
                throw new ParsingException($"Animation sampler {input.semantic} references nonexistent source: {input.source}");
            }

            switch (input.semantic)
            {
                case "INPUT":
                    inputSource = source;
                    break;

                case "OUTPUT":
                    outputSource = source;
                    break;

                case "INTERPOLATION":
                    interpolationSource = source;
                    break;

                default:
                    break;
            }
        }

        if (inputSource == null || outputSource == null || interpolationSource == null)
        {
            throw new ParsingException($"Animation {Animation.id} must have an INPUT, OUTPUT and INTERPOLATION sampler input!");
        }

        if (!inputSource.FloatParams.TryGetValue("TIME", out Times))
        {
            Times = inputSource.FloatParams.Values.SingleOrDefault();
        }

        if (Times == null)
        {
            throw new ParsingException($"Animation {Animation.id} INPUT must have a TIME parameter!");
        }

        if (!outputSource.MatrixParams.TryGetValue("TRANSFORM", out Transforms))
        {
            Transforms = outputSource.MatrixParams.Values.SingleOrDefault();
        }

        if (Transforms == null)
        {
            throw new ParsingException($"Animation {Animation.id} OUTPUT must have a TRANSFORM parameter!");
        }

        if (Transforms.Count != Times.Count)
        {
            throw new ParsingException($"Animation {Animation.id} has different time and transform counts!");
        }

        for (var i = 0; i < Transforms.Count; i++ )
        {
            var m = Transforms[i];
            m.Transpose();
            Transforms[i] = m;
        }
    }

    private void ImportChannel(Skeleton skeleton)
    {
        channel channel = null;
        foreach (var item in Animation.Items)
        {
            if (item is channel item1)
            {
                channel = item1;
                break;
            }
        }

        if (channel == null)
        {
            throw new ParsingException($"Animation {Animation.id} has no channel!");
        }

        var parts = channel.target.Split(new char[] { '/' });
        if (parts.Length != 2)
        {
            throw new ParsingException($"Unsupported channel target format: {channel.target}");
        }

        if (skeleton != null)
        {
            if (!skeleton.BonesByID.TryGetValue(parts[0], out var bone))
            {
                throw new ParsingException($"Animation channel references nonexistent bone: {parts[0]}");
            }

            if (bone.TransformSID != parts[1])
            {
                throw new ParsingException($"Animation channel references nonexistent transform or transform is not float4x4: {channel.target}");
            }

            BoneName = bone.Name;
        }
        else
        {
            BoneName = parts[0];
        }
    }

    public bool ImportFromCollada(animation colladaAnim, Skeleton skeleton)
    {
        Animation = colladaAnim;
        ImportSources();
        ImportSampler();

        // Avoid importing empty animations
        if (Transforms.Count == 0)
        {
            return false;
        }

        ImportChannel(skeleton);
        return true;
    }
        
    public TransformTrack MakeTrack(bool removeTrivialKeys)
    {
        var keyframes = KeyframeTrack.FromMatrices(Times, Transforms);

        if (removeTrivialKeys)
        {
            keyframes.RemoveTrivialTranslations();
            keyframes.RemoveTrivialRotations();
            keyframes.RemoveTrivialScales();
            keyframes.RemoveTrivialFrames();
        }

        var track = TransformTrack.FromKeyframes(keyframes);
        track.Flags = 0;
        track.Name = BoneName;

        return track;
    }
}