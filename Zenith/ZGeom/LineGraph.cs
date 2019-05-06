using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    internal class LineGraph : IGeom
    {
        internal List<GraphNode> nodes = new List<GraphNode>();

        public BasicVertexBuffer Construct(GraphicsDevice graphicsDevice)
        {
            List<int> indices = new List<int>();
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            Dictionary<GraphNode, int> indexLookup = new Dictionary<GraphNode, int>();
            for (int i = 0; i < nodes.Count; i++) indexLookup[nodes[i]] = i;
            foreach(var node in nodes)
            {
                vertices.Add(new VertexPositionColor(new Vector3(node.pos, -10f), Color.White));
                foreach(var c in node.connections)
                {
                    int i1 = indexLookup[node];
                    int i2 = indexLookup[c];
                    if (i1 < i2) {
                        indices.Add(i1);
                        indices.Add(i2);
                    }
                }
            }
            return new BasicVertexBuffer(graphicsDevice, indices, vertices, PrimitiveType.LineList);
        }

        internal class GraphNode
        {
            internal Vector2d pos;
            internal List<GraphNode> connections = new List<GraphNode>();

            public GraphNode(Vector2d pos)
            {
                this.pos = pos;
            }
        }
    }
}
