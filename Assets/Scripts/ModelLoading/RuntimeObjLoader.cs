using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Lightweight runtime OBJ loader for basic mesh files.
/// Supports common vertex/uv/normal face definitions.
/// </summary>
public static class RuntimeObjLoader
{
    public static GameObject LoadFromFile(string filePath)
    {
        return LoadFromText(File.ReadAllText(filePath), Path.GetFileNameWithoutExtension(filePath));
    }

    public static GameObject LoadFromText(string objText, string modelName)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();

        var meshVertices = new List<Vector3>();
        var meshUvs = new List<Vector2>();
        var meshNormals = new List<Vector3>();
        var meshTriangles = new List<int>();

        using (StringReader reader = new StringReader(objText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Trim().Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                switch (parts[0])
                {
                    case "v":
                        vertices.Add(new Vector3(
                            ParseFloat(parts, 1),
                            ParseFloat(parts, 2),
                            ParseFloat(parts, 3)));
                        break;
                    case "vt":
                        uvs.Add(new Vector2(
                            ParseFloat(parts, 1),
                            ParseFloat(parts, 2)));
                        break;
                    case "vn":
                        normals.Add(new Vector3(
                            ParseFloat(parts, 1),
                            ParseFloat(parts, 2),
                            ParseFloat(parts, 3)));
                        break;
                    case "f":
                        AddFace(parts, vertices, uvs, normals, meshVertices, meshUvs, meshNormals, meshTriangles);
                        break;
                }
            }
        }

        if (meshVertices.Count == 0)
        {
            Debug.LogError("[RuntimeObjLoader] No geometry found in OBJ file");
            return null;
        }

        var mesh = new Mesh
        {
            name = modelName + "_Mesh",
            indexFormat = meshVertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.SetVertices(meshVertices);
        mesh.SetTriangles(meshTriangles, 0);
        if (meshUvs.Count == meshVertices.Count)
            mesh.SetUVs(0, meshUvs);
        if (meshNormals.Count == meshVertices.Count)
            mesh.SetNormals(meshNormals);
        else
            mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var root = new GameObject(modelName);
        var child = new GameObject("Mesh");
        child.transform.SetParent(root.transform, false);

        var filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        var renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateDefaultMaterial();

        return root;
    }

    private static void AddFace(
        string[] parts,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Vector3> normals,
        List<Vector3> meshVertices,
        List<Vector2> meshUvs,
        List<Vector3> meshNormals,
        List<int> meshTriangles)
    {
        var faceIndices = new List<int>();

        for (int i = 1; i < parts.Length; i++)
        {
            string[] indices = parts[i].Split('/');
            int vertexIndex = ParseIndex(indices[0], vertices.Count) - 1;
            int uvIndex = indices.Length > 1 && !string.IsNullOrEmpty(indices[1])
                ? ParseIndex(indices[1], uvs.Count) - 1
                : -1;
            int normalIndex = indices.Length > 2 && !string.IsNullOrEmpty(indices[2])
                ? ParseIndex(indices[2], normals.Count) - 1
                : -1;

            meshVertices.Add(vertices[vertexIndex]);
            meshUvs.Add(uvIndex >= 0 ? uvs[uvIndex] : Vector2.zero);
            meshNormals.Add(normalIndex >= 0 ? normals[normalIndex] : Vector3.up);
            faceIndices.Add(meshVertices.Count - 1);
        }

        for (int i = 1; i < faceIndices.Count - 1; i++)
        {
            meshTriangles.Add(faceIndices[0]);
            meshTriangles.Add(faceIndices[i]);
            meshTriangles.Add(faceIndices[i + 1]);
        }
    }

    private static int ParseIndex(string value, int count)
    {
        int index = int.Parse(value, CultureInfo.InvariantCulture);
        if (index < 0)
            return count + index + 1;
        return index;
    }

    private static float ParseFloat(string[] parts, int index)
    {
        return float.Parse(parts[index], CultureInfo.InvariantCulture);
    }

    private static Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.75f, 1f));
        else
            material.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        return material;
    }
}
