using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.ZGeom;
using Zenith.ZMath;

namespace Zenith.ZGraphics.GraphicsBuffers
{
    class HouseBuffer : IGraphicsBuffer
    {
        private ISector sector;
        List<VertexPosition> vertices;
        private VertexBuffer housePositions;

        public HouseBuffer(GraphicsDevice graphicsDevice, ISector sector)
        {
            this.sector = sector;
            vertices = new List<VertexPosition>();
            int size = 256;
            for (int i = 0; i < size * size; i++)
            {
                int x = i % size;
                int y = i / size;
                vertices.Add(new VertexPosition(new Vector3((x + 0.5f) / size, (y + 0.5f) / size, 0)));
            }
            housePositions = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Count, BufferUsage.WriteOnly);
            housePositions.SetData(vertices.ToArray());
        }

        public void Dispose()
        {
            housePositions.Dispose();
        }

        public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom, int layer)
        {
            // TODO: we've temporarily flipped the normals on the model
            if (maxX - minX > 0.1 || maxY - minY > 0.1) return;
            double radius = 6356000; // of earth, in meters
            double scale = 2 / Math.Sqrt(4 * Math.PI * radius * radius / ZCoords.GetSectorManager().GetTopmostOSMSectors().Count / (1 << (2 * ZCoords.GetSectorManager().GetHighestOSMZoom())));
            foreach (ModelMesh mesh in GlobalContent.House.Meshes)
            {
                foreach (BasicEffect eff in mesh.Effects)
                {
                    eff.EnableDefaultLighting();
                    eff.View = basicEffect.View;
                    eff.Projection = basicEffect.Projection;
                    eff.VertexColorEnabled = false;
                    eff.Alpha = 1;
                    foreach (var v in vertices)
                    {
                        if (v.Position.X < minX || v.Position.X > maxX || v.Position.Y < minY || v.Position.Y > maxY) continue;
                        eff.World =  Matrix.CreateScale((float)scale) * Matrix.CreateTranslation(v.Position) * basicEffect.World;
                        mesh.Draw();
                    }
                }
            }
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public void InitDraw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
        }
    }
}
