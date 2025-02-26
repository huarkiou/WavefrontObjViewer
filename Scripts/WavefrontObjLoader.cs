// WavefrontObjLoader.cs
//
// Wavefront .OBJ 3d fileformat loader in C# (csharp dot net)
//
// Copyright (C) 2012 David Jeske, and given to the public domain
//
// Originally Based on DXGfx code by Guillaume Randon, Copyright (C) 2005, BSD License (See below notice)
//
// BSD License  
// DXGfx® - http://www.eteractions.com
// Copyright (c) 2005
// by Guillaume Randon
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE 
// AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Wavefront OBJ file format documentation:
//
// http://en.wikipedia.org/wiki/Wavefront_.obj_file
// http://www.fileformat.info/format/wavefrontobj/egff.htm
// http://www.fileformat.info/format/material/
// http://www.martinreddy.net/gfx/3d/OBJ.spec
//
// NOTE: OBJ uses CIE-XYZ color space...
//
// http://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1
// 
// TODO: handle 'o' object names, and 'g' object groups
// TODO: handle negative vertex indices in face specification
// TODO: handle "s" smoothing group
// TODO: handle "Tr"/"d" material transparency/alpha
//
// NOTE: OBJ puts (0,0) in the Upper Left, OpenGL Lower Left, DirectX Lower Left
// 
// http://stackoverflow.com/questions/4233152/how-to-setup-calculate-texturebuffer-in-gltexcoordpointer-when-importing-from-ob

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Numerics;


// ReSharper disable once CheckNamespace
public class WavefrontObjParseException(string reason) : Exception(reason);

public class WavefrontObjLoader
{
    public struct Face
    {
        public int[] v_idx;
        public int[] n_idx;
        public int[] tex_idx;
    }

    public int NumFaces = 0;
    public int NumIndices = 0;
    public bool HasNormals = false;

    // these are all indexed by "raw" vertex number from the OBJ file
    // NOTE: these indices are shared by the Faces in each material, so
    //       if you need per material indices, you'll need to rebuild your own
    //       vertex lists and indices.

    public List<Vector2> TexCoords = new();
    public List<Vector3> Normals = new();
    public List<Vector4> Positions = new();
    public List<Face> Faces = new();


    private void ParseObj(string basePath, string filename)
    {
        StreamReader sr = new StreamReader(Path.Combine(basePath, filename));

        //Read the first line of text
        string line = sr.ReadLine();

        //Continue to read until you reach end of file            
        while (line != null)
        {
            // handle line continuation with "\"
            if (line.Length > 0)
            {
                while (line[^1] == '\\')
                {
                    line = line.Substring(0, line.Length - 1); // remove line extender..
                    var nextline = sr.ReadLine();
                    if (!string.IsNullOrEmpty(nextline))
                    {
                        line = line + nextline; // merge with next line
                    }
                    else
                    {
                        break; // be sure to avoid infinite loop...
                    }
                }
            }

            // split the line into tokens, separated by space
            string[] tokens = line.Split(" ".ToArray(), 2);
            if (tokens.Length < 2)
            {
                goto next_line;
            }

            string firstToken = tokens[0];
            string lineContent = tokens[1];

            switch (firstToken)
            {
                /* unsupported features - fatal */
                case "cstype": // curved surface type (bmatrix, bezier, bspline, cardinal, taylor)
                case "deg": // curve attr: degree
                case "step": // curve attr: step size
                case "bmat": // curve attr: basis matrix
                case "surf": // surface
                case "parm": // curve body: paramater value
                case "trim": // curve body: outer trimming loop
                case "hole": // curve body: inner trimming loop
                case "scrv": // curve body: special curve
                case "sp": // curve body: special point
                case "end": // curve body: end
                case "con": // connection between free form surfaces
                case "vp": // paramater space vertex (for free form surfaces)

                case "bevel": // bevel interpolation
                case "c_interp": // color interpolation
                case "d_interp": // dissolve interpolation
                case "lod": // level of detail                                        
                case "ctech": // Curve approximation technique
                case "stech": // Surface approximation technique
                case "mg": // merging group (for free form surfaces)

                    throw new WavefrontObjParseException("WavefrontObjLoader.cs: fatal error, token not supported :  " +
                                                         firstToken);
                /* unsupported features - warning */
                case "o": // object name                  
                case "g": // group name
                case "s": // smoothing group
                case "shadow_obj": // shadow casting
                case "trace_obj": // ray tracing
                    Console.WriteLine("WavefrontObjLoader.cs: warning - unsupported wavefront token : " + firstToken);
                    break;

                /* supported features */
                case "#": // Nothing to read, these are comments.                        
                    break;
                case "v": // Vertex position
                    Positions.Add(WavefrontParser.ReadVector4(lineContent, null));
                    break;
                case "vn": // vertex normal direction vector
                    Normals.Add(WavefrontParser.ReadVector3(lineContent, null));
                    break;
                case "vt": // Vertex tex coordinate
                    TexCoords.Add(WavefrontParser.ReadVector2(lineContent, null));
                    break;
                case "f": // Face                    
                    string[] values = WavefrontParser.FilteredSplit(lineContent, null);
                    int numPoints = values.Length;

                    Face face = new Face
                    {
                        v_idx = new int[numPoints],
                        n_idx = new int[numPoints],
                        tex_idx =
                            new int[numPoints] // todo: how do outside clients know if there were texcoords or not?!?! 
                    };

                    for (int i = 0; i < numPoints; i++)
                    {
                        // format is "loc_index[/tex_index[/normal_index]]"  e.g. 3 ; 3/2 ; 3/2/5
                        // but middle part can me empty, e.g. 3//5
                        string[] indexes = values[i].Split('/');

                        int iPosition = int.Parse(indexes[0]) - 1; // adjust 1-based index                    
                        if (iPosition < 0)
                        {
                            iPosition += Positions.Count + 1;
                        } // adjust negative indicies

                        face.v_idx[i] = iPosition;
                        NumIndices++;

                        // initialize other indicies to not provided, in case they are missing
                        face.n_idx[i] = -1;
                        face.tex_idx[i] = -1;

                        if (indexes.Length > 1)
                        {
                            string texIndex = indexes[1];
                            if (texIndex != "")
                            {
                                int iTexCoord = int.Parse(texIndex) - 1; // adjust 1-based index
                                if (iTexCoord < 0)
                                {
                                    iTexCoord += TexCoords.Count + 1;
                                } // adjust negative indicies

                                face.tex_idx[i] = iTexCoord;
                            }

                            if (indexes.Length > 2)
                            {
                                HasNormals = true;
                                int iNormal = int.Parse(indexes[2]) - 1; // adjust 1 based index
                                if (iNormal < 0)
                                {
                                    iNormal += Normals.Count + 1;
                                } // adjust negative indicies

                                face.n_idx[i] = iNormal;
                            }
                        }
                    }

                    Faces.Add(face);
                    NumFaces++;
                    break;
                case "mtllib": // load named material file
                    break;
                case "usemtl": // use named material (from material file previously loaded)
                    break;
            }

            next_line:
            //Read the next line
            line = sr.ReadLine();
        }

        //close the file
        sr.Close();


        // debug print loaded stats
        Console.WriteLine("WavefrontObjLoader.cs: file processed...");
        Console.WriteLine("   vertex positions: {0}", Positions.Count);
        Console.WriteLine("   vertex   normals: {0}", Normals.Count);
        Console.WriteLine("   vertex texCoords: {0}", TexCoords.Count);
        Console.WriteLine("WavefrontObjLoader.cs: end.");
    }

    public WavefrontObjLoader(string path)
    {
        string basePath = Path.GetDirectoryName(path);
        string filename = Path.GetFileName(path);
        this.ParseObj(basePath, filename);
    }

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    public static System.Drawing.Color CIEXYZtoColor(Vector4 xyzColor)
    {
        if (xyzColor.X + xyzColor.Y + xyzColor.Z < 0.01f)
        {
            return System.Drawing.Color.FromArgb(150, 150, 150);
        }
        else
        {
            // this is not a proper color conversion.. just a hack approximation..
            return System.Drawing.Color.FromArgb((int)(xyzColor.X * 255), (int)(xyzColor.Y * 255),
                (int)(xyzColor.Z * 255));
        }
    }

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    public static int CIEXYZtoRGB(Vector4 xyzColor)
    {
        if (xyzColor.X + xyzColor.Y + xyzColor.Z < 0.01f)
        {
            return System.Drawing.Color.FromArgb(150, 150, 150).ToArgb();
        }
        else
        {
            // this is not a proper color conversion.. just a hack approximation..
            return System.Drawing.Color
                .FromArgb((int)(xyzColor.X * 255), (int)(xyzColor.Y * 255), (int)(xyzColor.Z * 255)).ToArgb();
        }
    }

    public static Vector3[] GenerateNormals(Vector3[] vertices, int[] triangles)
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
            Vector3 faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

            // Accumulate normals for each vertex
            normals[v0] += faceNormal;
            normals[v1] += faceNormal;
            normals[v2] += faceNormal;
        }

        // Normalize accumulated normals
        for (int i = 0; i < vertexCount; i++)
        {
            normals[i] = Vector3.Normalize(normals[i]);
        }

        return normals;
    }
}

public static class WavefrontParser
{
    public static float ParseFloat(string data)
    {
        // we have to use InvariantCulture to get the float-format parsing we expect
        return float.Parse(data, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// This method is used to split string in a list of strings based on the separator passed to hte method.
    /// </summary>
    /// <param name="strIn"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string[] FilteredSplit(string strIn, char[] separator)
    {
        string[] valuesUnfiltered = strIn.Split(separator);

        // Sometime if we have a white space at the beginning of the string, split
        // will remove an empty string. Let's remove that.
        List<string> listOfValues = new List<string>();
        foreach (string str in valuesUnfiltered)
        {
            if (str != "")
            {
                listOfValues.Add(str);
            }
        }

        string[] values = listOfValues.ToArray();

        return values;
    }

    public static Vector4 ReadVector4(string strIn, char[] separator)
    {
        string[] values = FilteredSplit(strIn, separator);

        if (values.Length == 3)
        {
            // W optional
            return new Vector4(
                ParseFloat(values[0]),
                ParseFloat(values[1]),
                ParseFloat(values[2]),
                0f);
        }
        else if (values.Length == 4)
        {
            return new Vector4(
                ParseFloat(values[0]),
                ParseFloat(values[1]),
                ParseFloat(values[2]),
                ParseFloat(values[3]));
        }
        else
        {
            throw new Exception("readVector4 found wrong number of vectors : " + strIn);
        }
    }

    public static Vector3 ReadVector3(string strIn, char[] separator)
    {
        string[] values = FilteredSplit(strIn, separator);

        if (values.Length == 3)
        {
            return new Vector3(
                ParseFloat(values[0]),
                ParseFloat(values[1]),
                ParseFloat(values[2]));
        }
        else
        {
            throw new Exception("readVector3 found wrong number of vectors : " + strIn);
        }
    }


    public static Vector2 ReadVector2(string strIn, char[] separator)
    {
        string[] values = FilteredSplit(strIn, separator);

        Assert(values.Length == 2, "readVector2 found wrong number of vectors : " + strIn);
        return new Vector2(
            ParseFloat(values[0]),
            ParseFloat(values[1]));
    }

    private static void Assert(bool testTrue, string reason)
    {
        if (!testTrue)
        {
            throw new Exception("WavefrontParser Error: " + reason);
        }
    }
}