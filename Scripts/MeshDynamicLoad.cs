using System;
using System.IO;
using System.Linq;
using Godot;
using Environment = System.Environment;

namespace WavefrontObjViewer.Scripts;

public partial class MeshDynamicLoad : MeshInstance3D
{
    [Export(PropertyHint.GlobalFile, "*.obj")] private string _objFilePath = @"D:\Users\huarkiou\Downloads\test4.obj";

    private void _ready()
    {
        var commandLineArgs = Environment.GetCommandLineArgs();
        var files = commandLineArgs[1..].Where(File.Exists).ToList();
        if (files.Count == 0)
        {
            files.Add(_objFilePath);
        }

        foreach (var file in files)
        {
            try
            {
                SetMeshByObjFile(file);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error when reading obj {file}\n", e);
            }
        }
    }

    private void SetMeshByObjFile(string objFile)
    {
        WavefrontObjLoader objLoader = new(objFile);
        float maxX = objLoader.Positions.Max(v => v.X);
        float minX = objLoader.Positions.Min(v => v.X);
        float length = maxX - minX;

        var vertices = new Vector3[objLoader.Positions.Count];
        for (var i = 0; i < objLoader.Positions.Count; i++)
        {
            vertices[i] = new Vector3(objLoader.Positions[i].X - length / 2.0f, objLoader.Positions[i].Y,
                              objLoader.Positions[i].Z) /
                          length;
        }

        var triangles = new int[objLoader.NumIndices];
        for (var i = 0; i < objLoader.NumFaces; i++)
        {
            triangles[3 * i + 0] = objLoader.Faces[i].v_idx[0];
            triangles[3 * i + 1] = objLoader.Faces[i].v_idx[1];
            triangles[3 * i + 2] = objLoader.Faces[i].v_idx[2];
        }

        var normals = objLoader.HasNormals
            ? objLoader.Normals.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray()
            : GenerateNormals(vertices, triangles);

        // Initialize the ArrayMesh.
        var arrMesh = new ArrayMesh();
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices;
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals;
        surfaceArray[(int)Mesh.ArrayType.Index] = triangles;
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        Mesh = arrMesh;
    }

    private static Vector3[] GenerateNormals(Vector3[] vertices, int[] triangles)
    {
        int vertexCount = vertices.Length;
        var normals = new Vector3[vertexCount];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];
            Vector3 p0 = vertices[v0];
            Vector3 p1 = vertices[v1];
            Vector3 p2 = vertices[v2];

            // Compute face normal using cross product
            Vector3 edge1 = p1 - p0;
            Vector3 edge2 = p2 - p0;
            Vector3 faceNormal = edge1.Cross(edge2).Normalized();

            // Accumulate normals for each vertex
            normals[v0] += faceNormal;
            normals[v1] += faceNormal;
            normals[v2] += faceNormal;
        }

        // Normalize accumulated normals
        for (int i = 0; i < vertexCount; i++)
        {
            normals[i] = normals[i].Normalized();
        }

        return normals;
    }
}