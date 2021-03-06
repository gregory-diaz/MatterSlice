/*
Copyright (C) 2013 David Braam
Copyright (c) 2014, Lars Brubaker

This file is part of MatterSlice.

MatterSlice is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MatterSlice is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MatterSlice.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using MatterSlice.ClipperLib;

namespace MatterHackers.MatterSlice
{
    // A SimpleFace is a 3 dimensional model triangle with 3 points. These points are already converted to integers
    public class SimpleFace
    {
        public Point3[] v = new Point3[3];

        public SimpleFace(Point3 v0, Point3 v1, Point3 v2) { v[0] = v0; v[1] = v1; v[2] = v2; }
    };

    // A SimpleVolume is the most basic reprisentation of a 3D model. It contains all the faces as SimpleTriangles, with nothing fancy.
    public class SimpleVolume
    {
        public List<SimpleFace> faceTriangles = new List<SimpleFace>();

        void SET_MIN(ref int n, int m)
        {
            if ((m) < (n))
                n = m;
        }

        void SET_MAX(ref int n, int m)
        {
            if ((m) > (n))
                n = m;
        }

        public void addFaceTriangle(Point3 v0, Point3 v1, Point3 v2)
        {
            faceTriangles.Add(new SimpleFace(v0, v1, v2));
        }

        public Point3 minXYZ()
        {
            if (faceTriangles.Count < 1)
            {
                return new Point3(0, 0, 0);
            }

            Point3 ret = faceTriangles[0].v[0];
            for (int faceIndex = 0; faceIndex < faceTriangles.Count; faceIndex++)
            {
                SET_MIN(ref ret.x, faceTriangles[faceIndex].v[0].x);
                SET_MIN(ref ret.y, faceTriangles[faceIndex].v[0].y);
                SET_MIN(ref ret.z, faceTriangles[faceIndex].v[0].z);
                SET_MIN(ref ret.x, faceTriangles[faceIndex].v[1].x);
                SET_MIN(ref ret.y, faceTriangles[faceIndex].v[1].y);
                SET_MIN(ref ret.z, faceTriangles[faceIndex].v[1].z);
                SET_MIN(ref ret.x, faceTriangles[faceIndex].v[2].x);
                SET_MIN(ref ret.y, faceTriangles[faceIndex].v[2].y);
                SET_MIN(ref ret.z, faceTriangles[faceIndex].v[2].z);
            }
            return ret;
        }

        public Point3 maxXYZ()
        {
            if (faceTriangles.Count < 1)
            {
                return new Point3(0, 0, 0);
            }

            Point3 ret = faceTriangles[0].v[0];
            for (int i = 0; i < faceTriangles.Count; i++)
            {
                SET_MAX(ref ret.x, faceTriangles[i].v[0].x);
                SET_MAX(ref ret.y, faceTriangles[i].v[0].y);
                SET_MAX(ref ret.z, faceTriangles[i].v[0].z);
                SET_MAX(ref ret.x, faceTriangles[i].v[1].x);
                SET_MAX(ref ret.y, faceTriangles[i].v[1].y);
                SET_MAX(ref ret.z, faceTriangles[i].v[1].z);
                SET_MAX(ref ret.x, faceTriangles[i].v[2].x);
                SET_MAX(ref ret.y, faceTriangles[i].v[2].y);
                SET_MAX(ref ret.z, faceTriangles[i].v[2].z);
            }
            return ret;
        }
    }

    //A SimpleModel is a 3D model with 1 or more 3D volumes.
    public class SimpleModel
    {
        public static StreamWriter binaryMeshBlob;

        public List<SimpleVolume> volumes = new List<SimpleVolume>();

        void SET_MIN(ref int n, int m)
        {
            if ((m) < (n))
            {
                n = m;
            }
        }

        void SET_MAX(ref int n, int m)
        {
            if ((m) > (n))
            {
                n = m;
            }
        }

        public Point3 minXYZ()
        {
            if (volumes.Count < 1)
            {
                return new Point3(0, 0, 0);
            }

            Point3 ret = volumes[0].minXYZ();
            for (int volumeIndex = 0; volumeIndex < volumes.Count; volumeIndex++)
            {
                Point3 v = volumes[volumeIndex].minXYZ();
                SET_MIN(ref ret.x, v.x);
                SET_MIN(ref ret.y, v.y);
                SET_MIN(ref ret.z, v.z);
            }
            return ret;
        }

        public Point3 maxXYZ()
        {
            if (volumes.Count < 1)
            {
                return new Point3(0, 0, 0);
            }

            Point3 ret = volumes[0].maxXYZ();
            for (int i = 0; i < volumes.Count; i++)
            {
                Point3 v = volumes[i].maxXYZ();
                SET_MAX(ref ret.x, v.x);
                SET_MAX(ref ret.y, v.y);
                SET_MAX(ref ret.z, v.z);
            }
            return ret;
        }

        public static SimpleModel loadModelSTL_ascii(string filename, FMatrix3x3 matrix)
        {
            SimpleModel m = new SimpleModel();
            m.volumes.Add(new SimpleVolume());
            SimpleVolume vol = m.volumes[0];
            using (StreamReader f = new StreamReader(filename))
            {
                // check for "SOLID"

                FPoint3 vertex = new FPoint3();
                int n = 0;
                Point3 v0 = new Point3(0, 0, 0);
                Point3 v1 = new Point3(0, 0, 0);
                Point3 v2 = new Point3(0, 0, 0);
                string line = f.ReadLine();
                Regex onlySingleSpaces = new Regex("\\s+", RegexOptions.Compiled);
                while (line != null)
                {
                    line = onlySingleSpaces.Replace(line, " ");
                    var parts = line.Trim().Split(' ');
                    if (parts[0].Trim() == "vertex")
                    {
                        vertex.x = Convert.ToDouble(parts[1]);
                        vertex.y = Convert.ToDouble(parts[2]);
                        vertex.z = Convert.ToDouble(parts[3]);

                        // change the scale from mm to micrometers
                        vertex *= 1000.0;

                        n++;
                        switch (n)
                        {
                            case 1:
                                v0 = matrix.apply(vertex);
                                break;
                            case 2:
                                v1 = matrix.apply(vertex);
                                break;
                            case 3:
                                v2 = matrix.apply(vertex);
                                vol.addFaceTriangle(v0, v1, v2);
                                n = 0;
                                break;
                        }
                    }
                    line = f.ReadLine();
                }

                return m;
            }
        }

        static SimpleModel loadModelSTL_binary(string filename, FMatrix3x3 matrix)
        {
            SimpleModel m = new SimpleModel();

            using (FileStream stlStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // load it as a binary stl
                // skip the first 80 bytes
                // read in the number of triangles
                stlStream.Position = 0;
                BinaryReader br = new BinaryReader(stlStream);
                byte[] fileContents = br.ReadBytes((int)stlStream.Length);
                int currentPosition = 80;
                uint numTriangles = System.BitConverter.ToUInt32(fileContents, currentPosition);
                long bytesForNormals = numTriangles * 3 * 4;
                long bytesForVertices = numTriangles * 3 * 4;
                long bytesForAttributs = numTriangles * 2;
                currentPosition += 4;
                long numBytesRequiredForVertexData = currentPosition + bytesForNormals + bytesForVertices + bytesForAttributs;
                if (fileContents.Length < numBytesRequiredForVertexData || numTriangles < 4)
                {
                    stlStream.Close();
                    return null;
                }

                m.volumes.Add(new SimpleVolume());
                SimpleVolume vol = m.volumes[0];
                Point3[] vector = new Point3[3];
                for (int i = 0; i < numTriangles; i++)
                {
                    // skip the normal 
                    currentPosition += 3 * 4;
                    for (int j = 0; j < 3; j++)
                    {
                        vector[j] = new Point3(
                            System.BitConverter.ToSingle(fileContents, currentPosition + 0 * 4) * 1000,
                            System.BitConverter.ToSingle(fileContents, currentPosition + 1 * 4) * 1000,
                            System.BitConverter.ToSingle(fileContents, currentPosition + 2 * 4) * 1000);
                        currentPosition += 3 * 4;
                    }
                    currentPosition += 2; // skip the attribute

                    vol.addFaceTriangle(vector[2], vector[1], vector[0]);
                }
            }

            return m;
        }

        public static SimpleModel loadModelFromFile(string filename, FMatrix3x3 matrix)
        {
            SimpleModel fromAsciiModel = loadModelSTL_ascii(filename, matrix);
            if (fromAsciiModel.volumes[0].faceTriangles.Count == 0)
            {
                return loadModelSTL_binary(filename, matrix);
            }

            return fromAsciiModel;
        }
    }
}