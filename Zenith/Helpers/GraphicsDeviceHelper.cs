using Microsoft.Xna.Framework.Graphics;

namespace Zenith.Helpers
{
    public static class GraphicsDeviceHelper
    {
        internal static void DrawUserPrimitives<T>(this GraphicsDevice graphicsDevice, PrimitiveType primitiveType, T[] vertexData) where T : struct, IVertexType
        {
            int primitiveCount = 0;
            switch (primitiveType)
            {
                case PrimitiveType.LineList:
                    primitiveCount = vertexData.Length / 2;
                    break;
                case PrimitiveType.LineStrip:
                    primitiveCount = vertexData.Length - 1;
                    break;
                case PrimitiveType.TriangleList:
                    primitiveCount = vertexData.Length / 3;
                    break;
                case PrimitiveType.TriangleStrip:
                    primitiveCount = vertexData.Length - 2;
                    break;
            }
            graphicsDevice.DrawUserPrimitives<T>(primitiveType, vertexData, 0, primitiveCount);
        }
    }
}
