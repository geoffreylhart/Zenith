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
        List<VertexBufferBinding[]> bindings;
        VertexBuffer instanceBuffer;

        public HouseBuffer(GraphicsDevice graphicsDevice, List<Matrix> matrices, ISector sector)
        {
            this.sector = sector;
            this.matrices = matrices;

            double radius = 6356000; // of earth, in meters
            double scale = 2 / Math.Sqrt(4 * Math.PI * radius * radius / ZCoords.GetSectorManager().GetTopmostOSMSectors().Count / (1 << (2 * ZCoords.GetSectorManager().GetHighestOSMZoom())));

            VertexDeclaration instanceVertexDeclaration = GenerateInstanceVertexDeclaration();
            instanceBuffer = new VertexBuffer(graphicsDevice, instanceVertexDeclaration, matrices.Count, BufferUsage.WriteOnly);
            instanceBuffer.SetData(matrices.Select(x => Matrix.CreateScale((float)scale) * x).ToArray());
            bindings = new List<VertexBufferBinding[]>();

            foreach (ModelMesh mesh in GlobalContent.House.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    var meshPartBindings = new VertexBufferBinding[2];
                    meshPartBindings[0] = new VertexBufferBinding(meshPart.VertexBuffer, 0);
                    meshPartBindings[1] = new VertexBufferBinding(instanceBuffer, 0, 1);
                    bindings.Add(meshPartBindings);
                }
            }
        }

        public void Dispose()
        {
        }

        public void Draw(RenderContext context)
        {
            bool actuallyDeferred = context.deferred.HasValue ? context.deferred.Value : Game1.DEFERRED_RENDERING;
            if (context.layerPass != RenderContext.LayerPass.MAIN_PASS) return;
            if (!actuallyDeferred)
            {
                // TODO: we've temporarily flipped the normals on the model
                var effect = GlobalContent.InstancingShader;
                effect.Parameters["WVP"].SetValue(context.WVP.toMatrix());
                int i = 0;
                foreach (ModelMesh mesh in GlobalContent.House.Meshes)
                {
                    foreach (var meshPart in mesh.MeshParts)
                    {
                        effect.Parameters["Texture"].SetValue(((BasicEffect)meshPart.Effect).Texture);
                        context.graphicsDevice.Indices = meshPart.IndexBuffer;
                        effect.CurrentTechnique.Passes[0].Apply();
                        context.graphicsDevice.SetVertexBuffers(bindings[i]);
                        context.graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, meshPart.VertexOffset, meshPart.StartIndex, meshPart.PrimitiveCount, matrices.Count);
                        i++;
                    }
                }
            }
            if (actuallyDeferred)
            {
                // TODO: we've temporarily flipped the normals on the model
                var effect = GlobalContent.DeferredInstancingShader;
                effect.Parameters["WVP"].SetValue(context.WVP.toMatrix());
                int i = 0;
                foreach (ModelMesh mesh in GlobalContent.House.Meshes)
                {
                    foreach (var meshPart in mesh.MeshParts)
                    {
                        effect.Parameters["Texture"].SetValue(((BasicEffect)meshPart.Effect).Texture);
                        context.graphicsDevice.Indices = meshPart.IndexBuffer;
                        effect.CurrentTechnique.Passes[0].Apply();
                        context.graphicsDevice.SetVertexBuffers(bindings[i]);
                        context.graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, meshPart.VertexOffset, meshPart.StartIndex, meshPart.PrimitiveCount, matrices.Count);
                        i++;
                    }
                }
            }
        }

        public Texture2D GetImage(GraphicsDevice graphicsDevice)
        {
            throw new NotImplementedException();
        }

        public void InitDraw(RenderContext context)
        {
        }

        private VertexDeclaration GenerateInstanceVertexDeclaration()
        {
            VertexElement[] instanceStreamElements = new VertexElement[5];
            instanceStreamElements[0] = new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1);
            instanceStreamElements[1] = new VertexElement(sizeof(float) * 4, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2);
            instanceStreamElements[2] = new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3);
            instanceStreamElements[3] = new VertexElement(sizeof(float) * 12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4);
            return new VertexDeclaration(instanceStreamElements);
        }
    }
}
