using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.EditorGameComponents.UIComponents;

namespace Zenith.EditorGameComponents
{
    internal class ShipComponent : DrawableGameComponent
    {
        private EditorCamera camera;

        public ShipComponent(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Draw(GameTime gameTime)
        {
            float pixelsPerUnit = 32;
            Matrix world = Matrix.Identity;
            Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 5), new Vector3(0, 0, 0), Vector3.UnitY);
            Matrix projection = Matrix.CreateOrthographicOffCenter(-GraphicsDevice.Viewport.Width / pixelsPerUnit / 2, GraphicsDevice.Viewport.Width / pixelsPerUnit / 2, GraphicsDevice.Viewport.Height / pixelsPerUnit / 2, -GraphicsDevice.Viewport.Height / pixelsPerUnit / 2, 1, 1000);
            foreach (ModelMesh mesh in GlobalContent.StartingShuttle.Meshes)
            {
                foreach (BasicEffect eff in mesh.Effects)
                {
                    eff.EnableDefaultLighting();
                    eff.World = world;
                    eff.View = view;
                    eff.Projection = projection;
                    eff.VertexColorEnabled = false;
                    eff.Alpha = 1;
                }

                mesh.Draw();
            }
        }
    }
}
