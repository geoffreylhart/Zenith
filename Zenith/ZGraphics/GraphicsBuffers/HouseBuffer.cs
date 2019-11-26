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
        List<Matrix> matrices;

        public HouseBuffer(GraphicsDevice graphicsDevice, List<Matrix> matrices, ISector sector)
        {
            this.sector = sector;
            this.matrices = matrices;
        }

        public void Dispose()
        {
        }

        public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect, double minX, double maxX, double minY, double maxY, double cameraZoom, int layer)
        {
            // TODO: we've temporarily flipped the normals on the model
            if (maxX - minX > 0.3 || maxY - minY > 0.3) return;
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
                    foreach (var m in matrices)
                    {
                        Vector3 pos = m.Translation;
                        if (pos.X < minX || pos.X > maxX || pos.Y < minY || pos.Y > maxY) continue;
                        eff.World = Matrix.CreateScale((float)scale) * m * basicEffect.World;
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
