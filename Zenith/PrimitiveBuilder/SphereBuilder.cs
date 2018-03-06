﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.MathHelpers;

namespace Zenith.PrimitiveBuilder
{
    public class SphereBuilder
    {
        // the poles will align with the z axis as it would in Blender
        // remember, this is designed to work with google map projections - it's not what we want to use for our map in the end
        internal static VertexIndiceBuffer MakeSphereSegLatLong(GraphicsDevice graphicsDevice, double diameter, double portion, double lat, double longi) // at 0,0, we expect the coordinates (0,-1,0)
        {
            VertexIndiceBuffer buffer = new VertexIndiceBuffer();
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();

            double radius = diameter / 2;
            double minLat = ToLat(ToY(lat) - portion / 2);
            double maxLat = ToLat(ToY(lat) + portion / 2);
            double minLong = longi - Math.PI * portion;
            double maxLong = longi + Math.PI * portion;
            int verticalSegments = Math.Max((int)((maxLat - minLat) * 10), 1);
            int horizontalSegments = Math.Max((int)((maxLong - minLong) * 10), 1);
            for (int i = 0; i <= verticalSegments; i++)
            {
                double latitude = (minLat + (maxLat - minLat) * i / (double)verticalSegments);
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    double longitude = (minLong + (maxLong - minLong) * j / (double)horizontalSegments);

                    double tx = j / (double)horizontalSegments;
                    //double ty = (ToY(latitude) - ToY(minLat)) / (ToY(maxLat) - ToY(minLat));
                    double ty = (ToY(maxLat) - ToY(latitude)) / (ToY(maxLat) - ToY(minLat)); // TODO: this works for flipping the y, now figure out if it makes sense?
                    Vector3 normal = Vector3Helper.UnitSphere(longitude, latitude);
                    Vector3 position = normal * (float)radius;
                    Vector2 texturepos = new Vector2((float)tx, (float)ty);
                    vertices.Add(new VertexPositionNormalTexture(position, normal, texturepos));
                }
            }

            List<int> indices = MakeIndices(horizontalSegments, verticalSegments);
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            buffer.indices.SetData(indices.ToArray());
            return buffer;
        }

        // NOTE: dont forget I swapped the triangle positions... try to make this make seem on purpose
        private static List<int> MakeIndices(int horizontalSegments, int verticalSegments)
        {
            List<int> indices = new List<int>();
            // Fill the sphere body with triangles joining each pair of latitude rings.
            for (int i = 0; i < verticalSegments; i++)
            {
                for (int j = 0; j < horizontalSegments; j++) // <=horizontalSegments if you really want to close the sphere for some reason
                {
                    if (i < verticalSegments || j < horizontalSegments / 2)
                    {
                        indices.Add(i * (horizontalSegments + 1) + j);
                        indices.Add((i + 1) * (horizontalSegments + 1) + j);
                        indices.Add(i * (horizontalSegments + 1) + j + 1);
                    }
                    else
                    {
                        indices.Add(i * (horizontalSegments + 1) + j);
                        indices.Add((i + 1) * (horizontalSegments + 1) + j);
                        indices.Add(i * (horizontalSegments + 1) + j + 1);
                    }

                    indices.Add(i * (horizontalSegments + 1) + j + 1);
                    indices.Add((i + 1) * (horizontalSegments + 1) + j);
                    indices.Add((i + 1) * (horizontalSegments + 1) + j + 1);
                }
            }
            return indices;
        }

        // now let's make the sphere make sense for working with - don't call ToY or ToLat ever, and rings will be evenly spaced (they already were)
        internal static VertexIndiceBuffer MakeSphereSeg(GraphicsDevice graphicsDevice, double diameter, double portion, double lat, double longi)
        {
            VertexIndiceBuffer buffer = new VertexIndiceBuffer();
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();

            double radius = diameter / 2;

            double minLat = Math.Max(lat-portion*Math.PI,-Math.PI/2);
            double maxLat = Math.Min(lat+portion * Math.PI, Math.PI/2);
            double minLong = longi - Math.PI * portion;
            double maxLong = longi + Math.PI * portion;
            int verticalSegments = Math.Max((int)((maxLat - minLat) * 10), 1);
            int horizontalSegments = Math.Max((int)((maxLong - minLong) * 10), 1);
            for (int i = 0; i <= verticalSegments; i++)
            {
                double latitude = (minLat + (maxLat - minLat) * i / (double)verticalSegments);
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    double longitude = (minLong + (maxLong - minLong) * j / (double)horizontalSegments);

                    double tx = j / (double)horizontalSegments;
                    double ty = i / (double)verticalSegments;
                    // stole this equation
                    Vector3 normal = Vector3Helper.UnitSphere(longitude, latitude);
                    Vector3 position = normal * (float) radius; // switched dy and dz here to align the poles from how we had them
                    Vector2 texturepos = new Vector2((float)tx, (float)ty);
                    vertices.Add(new VertexPositionNormalTexture(position, normal, texturepos));
                }
            }
            List<int> indices = MakeIndices(horizontalSegments, verticalSegments);
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
