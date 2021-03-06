﻿using Microsoft.Xna.Framework;
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
        // arrow texture will point in the direction the nodes are given in
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
                        float length = (float)(pos2 - pos1).Length();
                        // unfortunately calculating the normal from the next/prev vertex in the vertexshader has issues, due to WVP inaccuracies of offscreen vertices
                        Vector2d normalRight = (pos2 - pos1).RotateCW90().Normalized();
                        Vector2d normalLeft = (pos2 - pos1).RotateCCW90().Normalized();
                        // the top of the image will be at the end of the path
                        var topLeft = new VertexPositionNormalTexture(new Vector3(pos2, 0), new Vector3(normalLeft, 0), new Vector2(0, 0));
                        var topRight = new VertexPositionNormalTexture(new Vector3(pos2, 0), new Vector3(normalRight, 0), new Vector2(1, 0));
                        var bottomLeft = new VertexPositionNormalTexture(new Vector3(pos1, 0), new Vector3(normalLeft, 0), new Vector2(0, length));
                        var bottomRight = new VertexPositionNormalTexture(new Vector3(pos1, 0), new Vector3(normalRight, 0), new Vector2(1, length));
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
            return new BasicVertexBuffer(graphicsDevice, indices, vertices, GlobalContent.CCWArrows, true, PrimitiveType.TriangleList);
        }
    }
}
