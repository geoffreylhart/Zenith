using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ZEditorUnitTests
{
    class TestGame : Game
    {
        public GraphicsDeviceManager _graphics;

        public TestGame()
        {
            _graphics = new GraphicsDeviceManager(this);
        }
        protected override void Initialize()
        {
            Stopwatch sw = new Stopwatch();
            int TEST_COUNT = 10000;
            // create vertexbuffer normal way
            sw.Start();
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            for (int i = 0; i < TEST_COUNT; i++)
            {
                vertices.Add(new VertexPositionColor(new Vector3(0, 0, 0), Color.White));
            }
            var vertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Count, BufferUsage.None);
            vertexBuffer.SetData(vertices.ToArray());
            double time1 = sw.Elapsed.TotalSeconds;
            sw.Restart();
            // createvertexbuffer one vertex at a time
            var vertexBuffer2 = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, TEST_COUNT, BufferUsage.None);
            for (int i = 0; i < TEST_COUNT; i++)
            {
                var temp = new VertexPositionColor[] { new VertexPositionColor(new Vector3(0, 0, 0), Color.White) };
                vertexBuffer2.SetData<VertexPositionColor>(VertexPositionColor.VertexDeclaration.VertexStride * i, temp, 0, 1, VertexPositionColor.VertexDeclaration.VertexStride);
            }
            double time2 = sw.Elapsed.TotalSeconds;
            // ~3.5x slower, not acceptable
            Exit();
        }
    }
}
