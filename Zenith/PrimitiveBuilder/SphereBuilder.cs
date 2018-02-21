using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zenith.PrimitiveBuilder
{
    public class SphereBuilder
    {
        // the poles will align with the z axis as it would in Blender
        // remember, this is designed to work with google map projections - it's not what we want to use for our map in the end
        internal static VertexIndiceBuffer MakeSphereSegLatLong(GraphicsDevice graphicsDevice, double diameter, double portion, double lat, double longi)
        {
            VertexIndiceBuffer buffer = new VertexIndiceBuffer();
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> indices = new List<int>();

            double radius = diameter / 2;
            double minLat = ToLat(ToY(-lat) - portion / 2);
            double maxLat = ToLat(ToY(-lat) + portion / 2);
            double minLong = longi - Math.PI * portion;
            double maxLong = longi + Math.PI * portion;
            int verticalSegments = Math.Max((int)((maxLat - minLat) * 5), 1);
            int horizontalSegments = Math.Max((int)((maxLong - minLong) * 5), 1);
            //cutoff google logo
            for (int i = 0; i <= verticalSegments; i++)
            {
                double latitude = (minLat + (maxLat - minLat) * i / (double)verticalSegments);
                double dy = Math.Sin(latitude);
                double dxz = Math.Cos(latitude);
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    double longitude = (minLong + (maxLong - minLong) * j / (double)horizontalSegments);

                    double dx = Math.Cos(longitude) * dxz;
                    double dz = Math.Sin(longitude) * dxz;

                    double tx = j / (double)horizontalSegments;
                    double ty = (ToY(latitude) - ToY(minLat)) / (ToY(maxLat) - ToY(minLat));
                    // stole this equation
                    Vector3 normal = new Vector3((float)dx, (float)dz, (float)dy); // forgot to switch dy and dz here too
                    // NOTE: I cheated and made the y/z negative
                    Vector3 position = new Vector3((float)(dx * radius), (float)(dz * radius), (float)(dy * radius));
                    Vector2 texturepos = new Vector2((float)tx, (float)ty);
                    vertices.Add(new VertexPositionNormalTexture(position, normal, texturepos));
                }
            }

            // Fill the sphere body with triangles joining each pair of latitude rings.
            for (int i = 0; i < verticalSegments; i++)
            {
                for (int j = 0; j < horizontalSegments; j++) // <=horizontalSegments if you really want to close the sphere for some reason
                {
                    if (i < verticalSegments || j < horizontalSegments / 2)
                    {
                        indices.Add(i * (horizontalSegments + 1) + j);
                        indices.Add(i * (horizontalSegments + 1) + j + 1);
                        indices.Add((i + 1) * (horizontalSegments + 1) + j);
                    }
                    else
                    {
                        indices.Add(i * (horizontalSegments + 1) + j);
                        indices.Add(i * (horizontalSegments + 1) + j + 1);
                        indices.Add((i + 1) * (horizontalSegments + 1) + j);
                    }

                    indices.Add(i * (horizontalSegments + 1) + j + 1);
                    indices.Add((i + 1) * (horizontalSegments + 1) + j + 1);
                    indices.Add((i + 1) * (horizontalSegments + 1) + j);
                }
            }
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            buffer.indices.SetData(indices.ToArray());
            return buffer;
        }

        // now let's make the sphere make sense for working with - don't call ToY or ToLat ever, and rings will be evenly spaced (they already were)
        internal static VertexIndiceBuffer MakeSphereSeg(GraphicsDevice graphicsDevice, double diameter, double portion, double lat, double longi)
        {
            VertexIndiceBuffer buffer = new VertexIndiceBuffer();
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> indices = new List<int>();

            double radius = diameter / 2;

            double minLat = Math.Max(lat-portion*Math.PI,-Math.PI/2);
            double maxLat = Math.Min(lat+portion * Math.PI, Math.PI/2);
            double minLong = longi - Math.PI * portion;
            double maxLong = longi + Math.PI * portion;
            int verticalSegments = Math.Max((int)((maxLat - minLat) * 10), 1);
            int horizontalSegments = Math.Max((int)((maxLong - minLong) * 10), 1);
            //cutoff google logo
            for (int i = 0; i <= verticalSegments; i++)
            {
                double latitude = (minLat + (maxLat - minLat) * i / (double)verticalSegments);
                double dy = Math.Sin(latitude);
                double dxz = Math.Cos(latitude);
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    double longitude = (minLong + (maxLong - minLong) * j / (double)horizontalSegments);

                    double dx = Math.Cos(longitude) * dxz;
                    double dz = Math.Sin(longitude) * dxz;

                    double tx = j / (double)horizontalSegments;
                    double ty = i / (double)verticalSegments;
                    // stole this equation
                    Vector3 normal = new Vector3((float)dx, (float)dz, (float)dy); // forgot to switch dy and dz here too
                    Vector3 position = new Vector3((float)(dx * radius), (float)(dz * radius), (float)(dy * radius)); // switched dy and dz here to align the poles from how we had them
                    Vector2 texturepos = new Vector2((float)tx, (float)ty);
                    vertices.Add(new VertexPositionNormalTexture(position, normal, texturepos));
                }
            }

            // Fill the sphere body with triangles joining each pair of latitude rings.
            for (int i = 0; i < verticalSegments; i++)
            {
                for (int j = 0; j < horizontalSegments; j++) // <=horizontalSegments if you really want to close the sphere for some reason
                {
                    if (i < verticalSegments || j < horizontalSegments / 2)
                    {
                        indices.Add(i * (horizontalSegments + 1) + j);
                        indices.Add(i * (horizontalSegments + 1) + j + 1);
                        indices.Add((i + 1) * (horizontalSegments + 1) + j);
                    }
                    else
                    {
                        indices.Add(i * (horizontalSegments + 1) + j);
                        indices.Add(i * (horizontalSegments + 1) + j + 1);
                        indices.Add((i + 1) * (horizontalSegments + 1) + j);
                    }

                    indices.Add(i * (horizontalSegments + 1) + j + 1);
                    indices.Add((i + 1) * (horizontalSegments + 1) + j + 1);
                    indices.Add((i + 1) * (horizontalSegments + 1) + j);
                }
            }
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            buffer.indices.SetData(indices.ToArray());
            return buffer;
        }


        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (y - 0.5) * 2 * Math.PI)) - Math.PI / 2;
        }

        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }
    }
}
