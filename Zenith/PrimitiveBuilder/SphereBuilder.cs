using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.EditorGameComponents;
using Zenith.MathHelpers;
using Zenith.ZMath;

namespace Zenith.PrimitiveBuilder
{
    public class SphereBuilder
    {
        private static List<int> MakeIndices(int horizontalSegments, int verticalSegments)
        {
            List<int> indices = new List<int>();
            // Fill the sphere body with triangles joining each pair of latitude rings.
            for (int y = 0; y < verticalSegments; y++)
            {
                for (int x = 0; x < horizontalSegments; x++) // <=horizontalSegments if you really want to close the sphere for some reason
                {
                    indices.Add(y * (horizontalSegments + 1) + x);
                    indices.Add(y * (horizontalSegments + 1) + x + 1);
                    indices.Add((y + 1) * (horizontalSegments + 1) + x + 1);

                    indices.Add(y * (horizontalSegments + 1) + x);
                    indices.Add((y + 1) * (horizontalSegments + 1) + x + 1);
                    indices.Add((y + 1) * (horizontalSegments + 1) + x);
                }
            }
            return indices;
        }

        internal static VertexIndiceBuffer MakeSphereSegExplicit(GraphicsDevice graphicsDevice, ISector root, double diameter, double minX, double minY, double maxX, double maxY, EditorCamera camera)
        {
            Vector3d cameraVector = new LongLat(camera.cameraRotX, camera.cameraRotY).ToSphereVector(); // TODO: this is hacky

            VertexIndiceBuffer buffer = new VertexIndiceBuffer();
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();

            double radius = diameter / 2;
            int verticalSegments = Math.Max((int)((maxY - minY) * 50), 1);
            int horizontalSegments = Math.Max((int)((maxX - minX) * 50), 1);
            for (int i = 0; i <= verticalSegments; i++)
            {
                double y = (minY + (maxY - minY) * i / (double)verticalSegments);
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    double x = (minX + (maxX - minX) * j / (double)horizontalSegments);

                    double tx = j / (double)horizontalSegments;
                    double ty = i / (double)verticalSegments;
                    // stole this equation
                    Vector3d normal = root.ProjectToSphereCoordinates(new Vector2d(x, y));
                    Vector3d position = normal * (float)radius; // switched dy and dz here to align the poles from how we had them
                    Vector2d texturepos = new Vector2d((float)tx, (float)ty);
                    vertices.Add(new VertexPositionNormalTexture((position-cameraVector).ToVector3(), normal.ToVector3(), texturepos));
                }
            }
            List<int> indices = MakeIndices(horizontalSegments, verticalSegments);
            buffer.vertices = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            buffer.vertices.SetData(vertices.ToArray());
            buffer.indices = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            buffer.indices.SetData(indices.ToArray());
            return buffer;
        }
    }
}
