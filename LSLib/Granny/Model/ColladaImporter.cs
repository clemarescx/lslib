﻿using System;
using System.Collections.Generic;
using System.Linq;
using LSLib.Granny.GR2;
using LSLib.LS;
using OpenTK;

namespace LSLib.Granny.Model;

internal class ColladaSource
{
    public string id;
    public readonly Dictionary<string, List<float>> FloatParams = new();
    public readonly Dictionary<string, List<Matrix4>> MatrixParams = new();
    public readonly Dictionary<string, List<string>> NameParams = new();

    public static ColladaSource FromCollada(source src)
    {
        var source = new ColladaSource
        {
            id = src.id
        };

        var accessor = src.technique_common.accessor;
        // TODO: check src.#ID?

        float_array floats = null;
        Name_array names = null;
        if (src.Item is float_array item)
        {
            floats = item;
            // Workaround for empty arrays being null
            if (floats.Values == null)
            {
                floats.Values = new double[] { };
            }

            if ((int)floats.count != floats.Values.Length || floats.count < accessor.stride * accessor.count + accessor.offset)
            {
                throw new ParsingException("Float source data size mismatch. Check source and accessor item counts.");
            }
        }
        else if (src.Item is Name_array array)
        {
            names = array;
            // Workaround for empty arrays being null
            if (names.Values == null)
            {
                names.Values = new string[] { };
            }

            if ((int)names.count != names.Values.Length || names.count < accessor.stride * accessor.count + accessor.offset)
            {
                throw new ParsingException("Name source data size mismatch. Check source and accessor item counts.");
            }
        }
        else
        {
            throw new ParsingException("Unsupported source data format.");
        }

        var paramOffset = 0;
        foreach (var param in accessor.param)
        {
            if (param.name == null)
            {
                param.name = "default";
            }

            if (param.type is "float" or "double")
            {
                var items = new List<float>((int)accessor.count);
                var offset = (int)accessor.offset;
                for (var i = 0; i < (int)accessor.count; i++)
                {
                    items.Add((float)floats.Values[offset + paramOffset]);
                    offset += (int)accessor.stride;
                }

                source.FloatParams.Add(param.name, items);
            }
            else if (param.type == "float4x4")
            {
                var items = new List<Matrix4>((int)accessor.count);
                var offset = (int)accessor.offset;
                for (var i = 0; i < (int)accessor.count; i++)
                {
                    var itemOff = offset + paramOffset;
                    var mat = new Matrix4(
                        (float)floats.Values[itemOff + 0], (float)floats.Values[itemOff + 1], (float)floats.Values[itemOff + 2], (float)floats.Values[itemOff + 3],
                        (float)floats.Values[itemOff + 4], (float)floats.Values[itemOff + 5], (float)floats.Values[itemOff + 6], (float)floats.Values[itemOff + 7],
                        (float)floats.Values[itemOff + 8], (float)floats.Values[itemOff + 9], (float)floats.Values[itemOff + 10], (float)floats.Values[itemOff + 11],
                        (float)floats.Values[itemOff + 12], (float)floats.Values[itemOff + 13], (float)floats.Values[itemOff + 14], (float)floats.Values[itemOff + 15]
                    );
                    items.Add(mat);
                    offset += (int)accessor.stride;
                }

                source.MatrixParams.Add(param.name, items);
            }
            else if (param.type.ToLower() == "name")
            {
                var items = new List<string>((int)accessor.count);
                var offset = (int)accessor.offset;
                for (var i = 0; i < (int)accessor.count; i++)
                {
                    items.Add(names.Values[offset + paramOffset]);
                    offset += (int)accessor.stride;
                }

                source.NameParams.Add(param.name, items);
            }
            else
            {
                throw new ParsingException($"Unsupported accessor param type: {param.type}");
            }

            paramOffset++;
        }

        return source;
    }
}

class RootBoneInfo
{
    public node Bone;
    public List<node> Parents;
}

public class ColladaImporter
{
    [Serialization(Kind = SerializationKind.None)]
    public ExporterOptions Options = new();

    private bool ZUp;

    [Serialization(Kind = SerializationKind.None)]
    public Dictionary<string, Mesh> ColladaGeometries;

    [Serialization(Kind = SerializationKind.None)]
    public HashSet<string> SkinnedMeshes;

    private ArtToolInfo ImportArtToolInfo(COLLADA collada)
    {
        ZUp = false;
        var toolInfo = new ArtToolInfo
        {
            FromArtToolName = "Unknown",
            ArtToolMajorRevision = 1,
            ArtToolMinorRevision = 0,
            ArtToolPointerSize = Options.Is64Bit ? 64 : 32,
            Origin = new float[] { 0, 0, 0 }
        };

        toolInfo.SetYUp();

        if (collada.asset != null)
        {
            if (collada.asset.unit != null)
            {
                if (collada.asset.unit.name == "meter")
                {
                    toolInfo.UnitsPerMeter = (float)collada.asset.unit.meter;
                }
                else if (collada.asset.unit.name == "centimeter")
                {
                    toolInfo.UnitsPerMeter = (float)collada.asset.unit.meter * 100;
                }
                else
                {
                    throw new NotImplementedException($"Unsupported asset unit type: {collada.asset.unit.name}");
                }
            }

            if (collada.asset.contributor is { Length: > 0 })
            {
                var contributor = collada.asset.contributor[0];
                if (contributor.authoring_tool != null)
                {
                    toolInfo.FromArtToolName = contributor.authoring_tool;
                }
            }

            switch (collada.asset.up_axis)
            {
                case UpAxisType.X_UP:
                    throw new("X-up not supported yet!");

                case UpAxisType.Y_UP:
                    toolInfo.SetYUp();
                    break;

                case UpAxisType.Z_UP:
                    ZUp = true;
                    toolInfo.SetZUp();
                    break;
            }
        }

        return toolInfo;
    }

    private ExporterInfo ImportExporterInfo(COLLADA collada)
    {
        var exporterInfo = new ExporterInfo
        {
            ExporterName = $"LSLib GR2 Exporter v{Common.LibraryVersion()}",
            ExporterMajorRevision = Common.MajorVersion,
            ExporterMinorRevision = Common.MinorVersion,
            ExporterBuildNumber = 0,
            ExporterCustomization = Common.PatchVersion
        };

        return exporterInfo;
    }

    private DivinityModelFlag DetermineSkeletonModelFlagsFromModels(Root root, Skeleton skeleton, DivinityModelFlag meshFlagOverrides)
    {
        DivinityModelFlag accumulatedFlags = 0;
        foreach (var model in root.Models ?? Enumerable.Empty<Model>())
        {
            if (model.Skeleton == skeleton && model.MeshBindings != null)
            {
                foreach (var meshBinding in model.MeshBindings)
                {
                    accumulatedFlags |= meshBinding.Mesh?.ExtendedData?.UserMeshProperties?.MeshFlags ?? meshFlagOverrides;
                }
            }
        }

        return accumulatedFlags;
    }

    private void BuildExtendedData(Root root)
    {
        if (Options.ModelInfoFormat == DivinityModelInfoFormat.None)
        {
            return;
        }

        var modelFlagOverrides = Options.ModelType;

        foreach (var mesh in root.Meshes ?? Enumerable.Empty<Mesh>())
        {
            DivinityModelFlag modelFlags = modelFlagOverrides;
            if (modelFlags == 0 && mesh.ExtendedData != null)
            {
                modelFlags = mesh.ExtendedData.UserMeshProperties.MeshFlags;
            }

            if (mesh.ExtendedData == null)
            {
                mesh.ExtendedData = DivinityMeshExtendedData.Make();
            }
            mesh.ExtendedData.UserMeshProperties.MeshFlags = modelFlags;
            mesh.ExtendedData.UpdateFromModelInfo(mesh, Options.ModelInfoFormat);
        }

        foreach (var skeleton in root.Skeletons ?? Enumerable.Empty<Skeleton>())
        {
            if (Options.ModelInfoFormat is DivinityModelInfoFormat.None or DivinityModelInfoFormat.LSMv3)
            {
                foreach (var bone in skeleton.Bones ?? Enumerable.Empty<Bone>())
                {
                    bone.ExtendedData = null;
                }
            }
            else
            {
                var accumulatedFlags = DetermineSkeletonModelFlagsFromModels(root, skeleton, modelFlagOverrides);

                foreach (var bone in skeleton.Bones ?? Enumerable.Empty<Bone>())
                {
                    if (bone.ExtendedData == null)
                    {
                        bone.ExtendedData = new();
                    }

                    var userDefinedProperties = UserDefinedPropertiesHelpers.MeshFlagsToUserDefinedProperties(accumulatedFlags);
                    bone.ExtendedData.UserDefinedProperties = userDefinedProperties;
                    bone.ExtendedData.IsRigid = accumulatedFlags.IsRigid() ? 1 : 0;
                }
            }
        }
    }

    private void FindRootBones(List<node> parents, node node, List<RootBoneInfo> rootBones)
    {
        if (node.type == NodeType.JOINT)
        {
            var root = new RootBoneInfo
            {
                Bone = node,
                Parents = parents.Select(a => a).ToList()
            };
            rootBones.Add(root);
        }
        else if (node.type == NodeType.NODE)
        {
            if (node.node1 != null)
            {
                parents.Add(node);
                foreach (var child in node.node1)
                {
                    FindRootBones(parents, child, rootBones);
                }
                parents.RemoveAt(parents.Count - 1);
            }
        }
    }

    public static technique FindExporterExtraData(extra[] extras)
    {
        if (extras != null)
        {
            foreach (var extra in extras)
            {
                if (extra.technique != null)
                {
                    foreach (var technique in extra.technique)
                    {
                        if (technique.profile == "LSTools")
                        {
                            return technique;
                        }
                    }
                }
            }
        }

        return null;
    }

    private void LoadLSLibProfileMeshType(DivinityMeshExtendedData props, string meshType)
    {
        var meshProps = props.UserMeshProperties;

        switch (meshType)
        {
            // Compatibility flag, not used anymore
            case "Normal":        break;
            case "Cloth":         meshProps.MeshFlags |= DivinityModelFlag.Cloth; props.Cloth = 1; break;
            case "Rigid":         meshProps.MeshFlags |= DivinityModelFlag.Rigid; props.Rigid = 1; break;
            case "MeshProxy":     meshProps.MeshFlags |= DivinityModelFlag.MeshProxy | DivinityModelFlag.HasProxyGeometry; props.MeshProxy = 1; break;
            case "ProxyGeometry": meshProps.MeshFlags |= DivinityModelFlag.HasProxyGeometry; break;
            case "Spring":        meshProps.MeshFlags |= DivinityModelFlag.Spring; props.Spring = 1; break;
            case "Occluder":      meshProps.MeshFlags |= DivinityModelFlag.Occluder; props.Occluder = 1; break;
            case "Cloth01":       meshProps.ClothFlags |= DivinityClothFlag.Cloth01; break;
            case "Cloth02":       meshProps.ClothFlags |= DivinityClothFlag.Cloth02; break;
            case "Cloth04":       meshProps.ClothFlags |= DivinityClothFlag.Cloth04; break;
            case "ClothPhysics":  meshProps.ClothFlags |= DivinityClothFlag.ClothPhysics; break;
            default:
                Utils.Warn($"Unrecognized model type in <DivModelType> tag: {meshType}");
                break;
        }
    }

    private void LoadLSLibProfileExportOrder(Mesh mesh, string order)
    {
        if (int.TryParse(order, out int parsedOrder))
        {
            if (parsedOrder is >= 0 and < 100)
            {
                mesh.ExportOrder = parsedOrder;
            }
        }
    }

    private void LoadLSLibProfileLOD(DivinityMeshExtendedData props, string lod)
    {
        if (int.TryParse(lod, out int parsedLod))
        {
            if (parsedLod is >= 0 and < 100)
            {
                props.LOD = parsedLod;
                if (parsedLod == 0)
                {
                    props.UserMeshProperties.Lod[0] = -1;
                }
                else
                {
                    props.UserMeshProperties.Lod[0] = parsedLod;
                }
            }
        }
    }

    private void LoadLSLibProfileImpostor(DivinityMeshExtendedData props, string impostor)
    {
        if (int.TryParse(impostor, out int isImpostor))
        {
            if (isImpostor == 1)
            {
                props.UserMeshProperties.IsImpostor[0] = 1;
            }
        }
    }

    private void LoadLSLibProfileLODDistance(DivinityMeshProperties props, string lodDistance)
    {
        if (float.TryParse(lodDistance, out float parsedLodDistance))
        {
            if (parsedLodDistance >= 0.0f)
            {
                props.LodDistance[0] = parsedLodDistance;
            }
        }
    }

    private void MakeExtendedData(mesh mesh, Mesh loaded)
    {
        var modelFlagOverrides = Options.ModelType;

        DivinityModelFlag modelFlags = modelFlagOverrides;
        if (modelFlags == 0 && loaded.ExtendedData != null)
        {
            modelFlags = loaded.ExtendedData.UserMeshProperties.MeshFlags;
        }

        loaded.ExtendedData = DivinityMeshExtendedData.Make();
        loaded.ExtendedData.UserMeshProperties.MeshFlags = modelFlags;
        loaded.ExtendedData.UpdateFromModelInfo(loaded, Options.ModelInfoFormat);
        LoadColladaLSLibProfileData(mesh, loaded);
    }

    private void LoadColladaLSLibProfileData(mesh mesh, Mesh loaded)
    {
        var technique = FindExporterExtraData(mesh.extra);
        if (technique == null || technique.Any == null)
        {
            return;
        }

        var meshProps = loaded.ExtendedData.UserMeshProperties;

        foreach (var setting in technique.Any)
        {
            switch (setting.LocalName)
            {
                case "DivModelType":
                    LoadLSLibProfileMeshType(loaded.ExtendedData, setting.InnerText.Trim());
                    break;
                        
                case "IsImpostor":
                    LoadLSLibProfileImpostor(loaded.ExtendedData, setting.InnerText.Trim());
                    break;

                case "ExportOrder":
                    LoadLSLibProfileExportOrder(loaded, setting.InnerText.Trim());
                    break;

                case "LOD":
                    LoadLSLibProfileLOD(loaded.ExtendedData, setting.InnerText.Trim());
                    break;
                        
                case "LODDistance":
                    LoadLSLibProfileLODDistance(meshProps, setting.InnerText.Trim());
                    break;

                default:
                    Utils.Warn($"Unrecognized LSLib profile attribute: {setting.LocalName}");
                    break;
            }
        }
    }

    private void ValidateLSLibProfileMetadataVersion(string ver)
    {
        if (int.TryParse(ver, out int version))
        {
            if (version > Common.ColladaMetadataVersion)
            {
                throw new ParsingException(
                    $"Collada file is using a newer LSLib metadata format than this LSLib version supports, please upgrade.\r\nFile version: {version}, exporter version: {Common.ColladaMetadataVersion}");
            }
        }
    }

    private void LoadColladaLSLibProfileData(Root root, COLLADA collada)
    {
        var technique = FindExporterExtraData(collada.extra);
        if (technique == null || technique.Any == null)
        {
            return;
        }

        foreach (var setting in technique.Any)
        {
            switch (setting.LocalName)
            {
                case "MetadataVersion":
                    ValidateLSLibProfileMetadataVersion(setting.InnerText.Trim());
                    break;

                case "LSLibMajor":
                case "LSLibMinor":
                case "LSLibPatch":
                    break;

                default:
                    Utils.Warn($"Unrecognized LSLib root profile attribute: {setting.LocalName}");
                    break;
            }
        }
    }

    private Mesh ImportMesh(geometry geom, mesh mesh, VertexDescriptor vertexFormat)
    {
        var collada = new ColladaMesh();
        bool isSkinned = SkinnedMeshes.Contains(geom.id);
        collada.ImportFromCollada(mesh, vertexFormat, isSkinned, Options);

        var m = new Mesh
        {
            VertexFormat = collada.InternalVertexType,
            Name = "Unnamed",
            PrimaryVertexData = new()
            {
                Vertices = collada.ConsolidatedVertices
            }
        };

        if (!Options.StripMetadata)
        {
            var components = m.VertexFormat.ComponentNames().Select(s => new GrannyString(s)).ToList();
            m.PrimaryVertexData.VertexComponentNames = components;
        }
        else
        {
            m.PrimaryVertexData.VertexComponentNames = null;
        }

        m.PrimaryTopology = new()
        {
            Indices = collada.ConsolidatedIndices,
            Groups = new()
        };

        var triGroup = new TriTopologyGroup
        {
            MaterialIndex = 0,
            TriFirst = 0,
            TriCount = collada.TriangleCount
        };

        m.PrimaryTopology.Groups.Add(triGroup);

        m.MaterialBindings = new();
        m.MaterialBindings.Add(new());

        // m.BoneBindings; - TODO

        m.OriginalToConsolidatedVertexIndexMap = collada.OriginalToConsolidatedVertexIndexMap;

        MakeExtendedData(mesh, m);

        Utils.Info(
            $"Imported {(m.VertexFormat.HasBoneWeights ? "skinned" : "rigid")} mesh ({m.PrimaryTopology.Groups.Count} tri groups, {collada.TriangleCount} tris)");

        return m;
    }

    private Mesh ImportMesh(Root root, string name, geometry geom, mesh mesh, VertexDescriptor vertexFormat)
    {
        var m = ImportMesh(geom, mesh, vertexFormat);
        m.Name = name;
        root.VertexDatas.Add(m.PrimaryVertexData);
        root.TriTopologies.Add(m.PrimaryTopology);
        root.Meshes.Add(m);
        return m;
    }

    private void ImportSkin(Root root, skin skin)
    {
        if (skin.source1[0] != '#')
        {
            throw new ParsingException("Only ID references are supported for skin geometries");
        }

        if (!ColladaGeometries.TryGetValue(skin.source1[1..], out var mesh))
        {
            throw new ParsingException($"Skin references nonexistent mesh: {skin.source1}");
        }

        if (!mesh.VertexFormat.HasBoneWeights)
        {
            var msg = $"Tried to apply skin to mesh ({mesh.Name}) with non-skinned vertices";
            throw new ParsingException(msg);
        }

        var sources = new Dictionary<string, ColladaSource>();
        foreach (var source in skin.source)
        {
            var src = ColladaSource.FromCollada(source);
            sources.Add(src.id, src);
        }

        List<Bone> joints = null;
        List<Matrix4> invBindMatrices = null;
        foreach (var input in skin.joints.input)
        {
            if (input.source[0] != '#')
            {
                throw new ParsingException("Only ID references are supported for joint input sources");
            }

            if (!sources.TryGetValue(input.source[1..], out var inputSource))
            {
                throw new ParsingException($"Joint input source does not exist: {input.source}");
            }

            if (input.semantic == "JOINT")
            {
                List<string> jointNames = inputSource.NameParams.Values.SingleOrDefault();
                if (jointNames == null)
                {
                    throw new ParsingException("Joint input source 'JOINT' must contain array of names.");
                }

                var skeleton = root.Skeletons[0];
                joints = new();
                foreach (var name in jointNames)
                {
                    var lookupName = name.Replace("_x0020_", " ");
                    if (!skeleton.BonesBySID.TryGetValue(lookupName, out var bone))
                    {
                        throw new ParsingException($"Joint name list references nonexistent bone: {lookupName}");
                    }

                    joints.Add(bone);
                }
            }
            else if (input.semantic == "INV_BIND_MATRIX")
            {
                invBindMatrices = inputSource.MatrixParams.Values.SingleOrDefault();
                if (invBindMatrices == null)
                {
                    throw new ParsingException("Joint input source 'INV_BIND_MATRIX' must contain a single array of matrices.");
                }
            }
            else
            {
                throw new ParsingException($"Unsupported joint semantic: {input.semantic}");
            }
        }

        if (joints == null)
        {
            throw new ParsingException("Required joint input semantic missing: JOINT");
        }

        if (invBindMatrices == null)
        {
            throw new ParsingException("Required joint input semantic missing: INV_BIND_MATRIX");
        }

        var influenceCounts = ColladaHelpers.StringsToIntegers(skin.vertex_weights.vcount);
        var influences = ColladaHelpers.StringsToIntegers(skin.vertex_weights.v);

        foreach (var count in influenceCounts)
        {
            if (count > 4)
            {
                throw new ParsingException("GR2 only supports at most 4 vertex influences");
            }
        }

        // TODO
        if (influenceCounts.Count != mesh.OriginalToConsolidatedVertexIndexMap.Count)
        {
            Utils.Warn($"Vertex influence count ({influenceCounts.Count}) differs from vertex count ({mesh.OriginalToConsolidatedVertexIndexMap.Count})");
        }

        List<float> weights = null;

        int jointInputIndex = -1, weightInputIndex = -1;
        foreach (var input in skin.vertex_weights.input)
        {
            if (input.semantic == "JOINT")
            {
                jointInputIndex = (int)input.offset;
            }
            else if (input.semantic == "WEIGHT")
            {
                weightInputIndex = (int)input.offset;

                if (input.source[0] != '#')
                {
                    throw new ParsingException("Only ID references are supported for weight input sources");
                }

                if (!sources.TryGetValue(input.source[1..], out var inputSource))
                {
                    throw new ParsingException($"Weight input source does not exist: {input.source}");
                }

                if (!inputSource.FloatParams.TryGetValue("WEIGHT", out weights))
                {
                    weights = inputSource.FloatParams.Values.SingleOrDefault();
                }

                if (weights == null)
                {
                    throw new ParsingException($"Weight input source {input.source} must have WEIGHT float attribute");
                }
            }
            else
            {
                throw new ParsingException($"Unsupported skin input semantic: {input.semantic}");
            }
        }

        if (jointInputIndex == -1)
        {
            throw new ParsingException("Required vertex weight input semantic missing: JOINT");
        }

        if (weightInputIndex == -1)
        {
            throw new ParsingException("Required vertex weight input semantic missing: WEIGHT");
        }

        // Remove bones that are not actually influenced from the binding list
        var boundBones = new HashSet<Bone>();
        int offset = 0;
        int stride = skin.vertex_weights.input.Length;
        while (offset < influences.Count)
        {
            var jointIndex = influences[offset + jointInputIndex];
            var weightIndex = influences[offset + weightInputIndex];
            var joint = joints[jointIndex];
            var weight = weights[weightIndex];
            if (!boundBones.Contains(joint))
            {
                boundBones.Add(joint);
            }

            offset += stride;
        }

        if (boundBones.Count > 127)
        {
            throw new ParsingException("D:OS supports at most 127 bound bones per mesh.");
        }

        mesh.BoneBindings = new();
        var boneToIndexMaps = new Dictionary<Bone, int>();
        for (var i = 0; i < joints.Count; i++)
        {
            if (boundBones.Contains(joints[i]))
            {
                // Collada allows one inverse bind matrix for each skin, however Granny
                // only has one matrix for one bone, even if said bone is used from multiple meshes.
                // Hopefully the Collada ones are all equal ...
                var iwt = invBindMatrices[i];
                // iwt.Transpose();
                joints[i].InverseWorldTransform = new float[] {
                    iwt[0, 0], iwt[1, 0], iwt[2, 0], iwt[3, 0],
                    iwt[0, 1], iwt[1, 1], iwt[2, 1], iwt[3, 1],
                    iwt[0, 2], iwt[1, 2], iwt[2, 2], iwt[3, 2],
                    iwt[0, 3], iwt[1, 3], iwt[2, 3], iwt[3, 3]
                };

                // Bind all bones that affect vertices to the mesh, so we can reference them
                // later from the vertexes BoneIndices.
                var binding = new BoneBinding
                {
                    BoneName = joints[i].Name,
                    // TODO
                    // Use small bounding box values, as it interferes with object placement
                    // in D:OS 2 (after the Gift Bag 2 update)
                    OBBMin = new float[] { -0.1f, -0.1f, -0.1f },
                    OBBMax = new float[] { 0.1f, 0.1f, 0.1f }
                };

                mesh.BoneBindings.Add(binding);
                boneToIndexMaps.Add(joints[i], boneToIndexMaps.Count);
            }
        }

        offset = 0;
        for (var vertexIndex = 0; vertexIndex < influenceCounts.Count; vertexIndex++)
        {
            var influenceCount = influenceCounts[vertexIndex];
            float influenceSum = 0.0f;
            for (var i = 0; i < influenceCount; i++)
            {
                var weightIndex = influences[offset + i * stride + weightInputIndex];
                influenceSum += weights[weightIndex];
            }

            for (var i = 0; i < influenceCount; i++)
            {
                var jointIndex = influences[offset + jointInputIndex];
                var weightIndex = influences[offset + weightInputIndex];
                var joint = joints[jointIndex];
                var weight = weights[weightIndex] / influenceSum;
                // Not all vertices are actually used in triangles, we may have unused verts in the
                // source list (though this is rare) which won't show up in the consolidated vertex map.
                if (mesh.OriginalToConsolidatedVertexIndexMap.TryGetValue(vertexIndex, out List<int> consolidatedIndices))
                {
                    foreach (var consolidatedIndex in consolidatedIndices)
                    {
                        var vertex = mesh.PrimaryVertexData.Vertices[consolidatedIndex];
                        vertex.AddInfluence((byte)boneToIndexMaps[joint], weight);
                    }
                }

                offset += stride;
            }
        }

        foreach (var vertex in mesh.PrimaryVertexData.Vertices)
        {
            vertex.FinalizeInfluences();
        }

        // Warn if we have vertices that are not influenced by any bone
        int notInfluenced = 0;
        foreach (var vertex in mesh.PrimaryVertexData.Vertices)
        {
            if (vertex.BoneWeights[0] == 0)
            {
                notInfluenced++;
            }
        }

        if (notInfluenced > 0)
        {
            Utils.Warn($"{notInfluenced} vertices are not influenced by any bone");
        }

        if (skin.bind_shape_matrix != null)
        {
            var bindShapeFloats = skin.bind_shape_matrix.Trim().Split(new char[] { ' ' }).Select(s => float.Parse(s)).ToArray();
            var bindShapeMat = ColladaHelpers.FloatsToMatrix(bindShapeFloats);
            bindShapeMat.Transpose();

            // Deform geometries that were affected by our bind shape matrix
            mesh.PrimaryVertexData.Transform(bindShapeMat);
        }

        if (Options.RecalculateOBBs)
        {
            UpdateOBBs(root.Skeletons.Single(), mesh);
        }
    }

    class OBB
    {
        public Vector3 Min, Max;
        public int NumVerts;
    }

    private void UpdateOBBs(Skeleton skeleton, Mesh mesh)
    {
        if (mesh.BoneBindings == null || mesh.BoneBindings.Count == 0)
        {
            return;
        }

        var obbs = new List<OBB>(mesh.BoneBindings.Count);
        for (var i = 0; i < mesh.BoneBindings.Count; i++)
        {
            obbs.Add(new()
            {
                Min = new(1000.0f, 1000.0f, 1000.0f),
                Max = new(-1000.0f, -1000.0f, -1000.0f),
            });
        }
            
        foreach (var vert in mesh.PrimaryVertexData.Vertices)
        {
            for (var i = 0; i < 4; i++)
            {
                if (vert.BoneWeights[i] > 0)
                {
                    var bi = vert.BoneIndices[i];
                    var obb = obbs[bi];
                    obb.NumVerts++;

                    var bone = skeleton.GetBoneByName(mesh.BoneBindings[bi].BoneName);
                    var invWorldTransform = ColladaHelpers.FloatsToMatrix(bone.InverseWorldTransform);
                    var transformed = Vector3.Transform(vert.Position, invWorldTransform);

                    obb.Min.X = Math.Min(obb.Min.X, transformed.X);
                    obb.Min.Y = Math.Min(obb.Min.Y, transformed.Y);
                    obb.Min.Z = Math.Min(obb.Min.Z, transformed.Z);

                    obb.Max.X = Math.Max(obb.Max.X, transformed.X);
                    obb.Max.Y = Math.Max(obb.Max.Y, transformed.Y);
                    obb.Max.Z = Math.Max(obb.Max.Z, transformed.Z);
                }
            }
        }

        for (var i = 0; i < obbs.Count; i++)
        {
            var obb = obbs[i];
            if (obb.NumVerts > 0)
            {
                mesh.BoneBindings[i].OBBMin = new float[] { obb.Min.X, obb.Min.Y, obb.Min.Z };
                mesh.BoneBindings[i].OBBMax = new float[] { obb.Max.X, obb.Max.Y, obb.Max.Z };
            }
            else
            {
                mesh.BoneBindings[i].OBBMin = new float[] { 0.0f, 0.0f, 0.0f };
                mesh.BoneBindings[i].OBBMax = new float[] { 0.0f, 0.0f, 0.0f };
            }
        }
    }

    private void LoadColladaLSLibProfileData(animation anim, TrackGroup loaded)
    {
        var technique = FindExporterExtraData(anim.extra);
        if (technique == null || technique.Any == null)
        {
            return;
        }

        foreach (var setting in technique.Any)
        {
            switch (setting.LocalName)
            {
                case "SkeletonResourceID":
                    loaded.ExtendedData = new()
                    {
                        SkeletonResourceID = setting.InnerText.Trim()
                    };
                    break;

                default:
                    Utils.Warn($"Unrecognized LSLib animation profile attribute: {setting.LocalName}");
                    break;
            }
        }
    }

    public void ImportAnimations(IEnumerable<animation> anims, Root root, Skeleton skeleton)
    {
        var trackGroup = new TrackGroup
        {
            Name = skeleton != null ? skeleton.Name : "Dummy_Root",
            TransformTracks = new(),
            InitialPlacement = new(),
            AccumulationFlags = 2,
            LoopTranslation = new float[] { 0, 0, 0 }
        };

        var animation = new Animation
        {
            Name = "Default",
            TimeStep = 0.016667f, // 60 FPS
            Oversampling = 1,
            DefaultLoopCount = 1,
            Flags = 1,
            Duration = .0f,
            TrackGroups = new() { trackGroup }
        };

        foreach (var colladaTrack in anims)
        {
            ImportAnimation(colladaTrack, animation, trackGroup, skeleton);
        }

        if (trackGroup.TransformTracks.Count > 0)
        {
            // Reorder transform tracks in lexicographic order
            // This is needed by Granny; otherwise it'll fail to find animation tracks
            trackGroup.TransformTracks.Sort((t1, t2) => string.Compare(t1.Name, t2.Name, StringComparison.Ordinal));
                
            root.TrackGroups.Add(trackGroup);
            root.Animations.Add(animation);
        }
    }

    public void ImportAnimation(animation colladaAnim, Animation animation, TrackGroup trackGroup, Skeleton skeleton)
    {
        var childAnims = 0;
        foreach (var item in colladaAnim.Items)
        {
            if (item is animation item1)
            {
                ImportAnimation(item1, animation, trackGroup, skeleton);
                childAnims++;
            }
        }

        var duration = .0f;
        if (childAnims < colladaAnim.Items.Length)
        {
            ColladaAnimation importAnim = new();
            if (importAnim.ImportFromCollada(colladaAnim, skeleton))
            {
                duration = Math.Max(duration, importAnim.Duration);
                var track = importAnim.MakeTrack(Options.RemoveTrivialAnimationKeys);
                trackGroup.TransformTracks.Add(track);
                LoadColladaLSLibProfileData(colladaAnim, trackGroup);
            }
        }

        animation.Duration = Math.Max(animation.Duration, duration);
    }

    public Root Import(string inputPath)
    {
        COLLADA collada = null;
        using (var stream = File.OpenRead(inputPath))
        {
            collada = COLLADA.Load(stream);
        }

        var root = new Root();
        LoadColladaLSLibProfileData(root, collada);
        root.ArtToolInfo = ImportArtToolInfo(collada);
        if (!Options.StripMetadata)
        {
            root.ExporterInfo = ImportExporterInfo(collada);
        }

        root.FromFileName = inputPath;

        root.Skeletons = new();
        root.VertexDatas = new();
        root.TriTopologies = new();
        root.Meshes = new();
        root.Models = new();
        root.TrackGroups = new();
        root.Animations = new();

        ColladaGeometries = new();
        SkinnedMeshes = new();

        var collGeometries = new List<geometry>();
        var collSkins = new List<skin>();
        var collNodes = new List<node>();
        var collAnimations = new List<animation>();
        var rootBones = new List<RootBoneInfo>();

        // Import skinning controllers after skeleton and geometry loading has finished, as
        // we reference both of them during skin import
        foreach (var item in collada.Items)
        {
            if (item is library_controllers controllers)
            {
                if (controllers.controller != null)
                {
                    foreach (var controller in controllers.controller)
                    {
                        if (controller.Item is skin skin)
                        {
                            collSkins.Add(skin);
                            SkinnedMeshes.Add(skin.source1[1..]);
                        }
                        else
                        {
                            Utils.Warn($"Controller {controller.Item.GetType().Name} is unsupported and will be ignored");
                        }
                    }
                }
            }
            else if (item is library_visual_scenes scenes)
            {
                if (scenes.visual_scene != null)
                {
                    foreach (var scene in scenes.visual_scene)
                    {
                        if (scene.node != null)
                        {
                            foreach (var node in scene.node)
                            {
                                collNodes.Add(node);
                                FindRootBones(new(), node, rootBones);
                            }
                        }
                    }
                }
            }
            else if (item is library_geometries geometries)
            {
                if (geometries.geometry != null)
                {
                    foreach (var geometry in geometries.geometry)
                    {
                        if (geometry.Item is mesh)
                        {
                            collGeometries.Add(geometry);
                        }
                        else
                        {
                            Utils.Warn($"Geometry type {geometry.Item.GetType().Name} is unsupported and will be ignored");
                        }
                    }
                }
            }
            else if (item is library_animations animations)
            {
                if (animations.animation != null)
                {
                    collAnimations.AddRange(animations.animation);
                }
            }
            else
            {
                Utils.Warn($"Library {item.GetType().Name} is unsupported and will be ignored");
            }
        }

        foreach (var bone in rootBones)
        {
            var skeleton = Skeleton.FromCollada(bone.Bone);
            var rootTransform = NodeHelpers.GetTransformHierarchy(bone.Parents);
            skeleton.TransformRoots(rootTransform.Inverted());
            skeleton.ReorderBones();
            root.Skeletons.Add(skeleton);
        }

        foreach (var geometry in collGeometries)
        {
            // Use the override vertex format, if one was specified
            Options.VertexFormats.TryGetValue(geometry.name, out var vertexFormat);
            var mesh = ImportMesh(root, geometry.name, geometry, geometry.Item as mesh, vertexFormat);
            ColladaGeometries.Add(geometry.id, mesh);
        }

        // Reorder meshes based on their ExportOrder
        if (root.Meshes.Any(m => m.ExportOrder > -1))
        {
            root.Meshes.Sort((a, b) => a.ExportOrder - b.ExportOrder);
        }

        // Import skinning controllers after skeleton and geometry loading has finished, as
        // we reference both of them during skin import
        if (rootBones.Count > 0)
        {
            foreach (var skin in collSkins)
            {
                ImportSkin(root, skin);
            }
        }

        if (collAnimations.Count > 0)
        {
            ImportAnimations(collAnimations, root, root.Skeletons.FirstOrDefault());
        }

        var rootModel = new Model
        {
            Name = "Unnamed" // TODO
        };

        if (root.Skeletons.Count > 0)
        {
            rootModel.Skeleton = root.Skeletons[0];
            rootModel.Name = rootModel.Skeleton.Bones[0].Name;
        }
        rootModel.InitialPlacement = new();
        rootModel.MeshBindings = new();
        foreach (var mesh in root.Meshes)
        {
            var binding = new MeshBinding
            {
                Mesh = mesh
            };

            rootModel.MeshBindings.Add(binding);
        }

        root.Models.Add(rootModel);
        // TODO: make this an option!
        if (root.Skeletons.Count > 0)
        {
            root.Skeletons[0].UpdateWorldTransforms();
        }

        root.ZUp = ZUp;
        root.PostLoad(Header.DefaultTag);

        BuildExtendedData(root);

        return root;
    }
}