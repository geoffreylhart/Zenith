using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.LibraryWrappers.OSM
{
    class OSMLineBufferGenerator
    {
        // generates a zero-width quad for each line in the blob collection, with texture and color
        internal static BasicVertexBuffer GenerateDebugLines(GraphicsDevice graphicsDevice, BlobCollection blobs)
        {
            List<int> indices = new List<int>();
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            foreach (Way way in blobs.EnumerateWays())
            {
                long? prev = null;
                foreach (var nodeRef in way.refs)
                {
                    long? v = blobs.nodes.ContainsKey(nodeRef) ? nodeRef : (long?)null;
                    if (prev != null && v != null)
                    {
                        Vector2d pos1 = blobs.nodes[prev.Value];
                        Vector2d pos2 = blobs.nodes[v.Value];
                        Vector3 normalLeft = new Vector3((pos2 - pos1).RotateCCW90(), 0);
                        Vector3 normalRight = new Vector3((pos2 - pos1).RotateCW90(), 0);
                        var topLeft = new VertexPositionNormalTexture(new Vector3(pos2, 0), normalLeft, new Vector2(0, 0));
                        var topRight = new VertexPositionNormalTexture(new Vector3(pos2, 0), normalRight, new Vector2(0, 1));
                        var bottomLeft = new VertexPositionNormalTexture(new Vector3(pos1, 0), normalLeft, new Vector2(1, 0));
                        var bottomRight = new VertexPositionNormalTexture(new Vector3(pos1, 0), normalRight, new Vector2(1, 1));
                        vertices.Add(topLeft);
                        vertices.Add(topRight);
                        vertices.Add(bottomLeft);
                        vertices.Add(bottomRight);
                        // TODO: undo the bad flipping when we're ready
                        int i = vertices.Count - 4;
                        indices.Add(i);
                        indices.Add(i + 3);
                        indices.Add(i + 1);
                        indices.Add(i);
                        indices.Add(i + 2);
                        indices.Add(i + 3);
                    }
                    prev = v;
                }
            }
            return new BasicVertexBuffer(graphicsDevice, indices, vertices, GlobalContent.Road, true, PrimitiveType.TriangleList);
        }
    }
}
