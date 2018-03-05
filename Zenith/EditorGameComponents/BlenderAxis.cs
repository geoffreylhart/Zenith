using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.PrimitiveBuilder;

namespace Zenith.EditorGameComponents
{
    // a debug class which renders 3 cubes corresponding to the Blender axis - for help setting up the blender-ish camera
    public class BlenderAxis : DrawableGameComponent
    {
        private EditorCamera camera;

        Dictionary<VertexIndiceBuffer, Color> cubes = new Dictionary<VertexIndiceBuffer, Color>();
        public BlenderAxis(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
            float size = 0.3f;
            float distance = 0.5f;
            cubes.Add(CubeBuilder.MakeBasicCube(game.GraphicsDevice, Vector3.Zero, Vector3.UnitX * size, -Vector3.UnitY * size, Vector3.UnitZ * size), Color.White);
            cubes.Add(CubeBuilder.MakeBasicCube(game.GraphicsDevice, Vector3.UnitX * distance, Vector3.UnitX * size, -Vector3.UnitY * size, Vector3.UnitZ * size), Color.Red);
            cubes.Add(CubeBuilder.MakeBasicCube(game.GraphicsDevice, Vector3.UnitY * distance, Vector3.UnitX * size, -Vector3.UnitY * size, Vector3.UnitZ * size), Color.Green);
            cubes.Add(CubeBuilder.MakeBasicCube(game.GraphicsDevice, Vector3.UnitZ * distance, Vector3.UnitX * size, -Vector3.UnitY * size, Vector3.UnitZ * size), Color.Blue);
        }

        public override void Draw(GameTime gameTime)
        {
            //GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            foreach (var cube in cubes)
            {
                BasicEffect bf3 = MakeThatBasicEffect3(cube.Value);
                foreach (EffectPass pass in bf3.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.Indices = cube.Key.indices;
                    GraphicsDevice.SetVertexBuffer(cube.Key.vertices);
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, cube.Key.indices.IndexCount / 3);
                }
            }
        }

        private BasicEffect MakeThatBasicEffect3(Color color)
        {
            var basicEffect3 = new BasicEffect(GraphicsDevice);
            basicEffect3.LightingEnabled = true;
            basicEffect3.DirectionalLight0.Direction = new Vector3(1, -1, 0);
            basicEffect3.DirectionalLight0.DiffuseColor = color.ToVector3();
            basicEffect3.AmbientLightColor = color.ToVector3()/5;
            camera.ApplyMatrices(basicEffect3);
            return basicEffect3;
        }
    }
}
