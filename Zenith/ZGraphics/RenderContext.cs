using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Zenith.MathHelpers;

namespace Zenith.ZGraphics
{
    public class RenderContext
    {
        public GraphicsDevice graphicsDevice;
        public Matrixd WVP;
        public double minX;
        public double maxX;
        public double minY;
        public double maxY;
        public double cameraZoom;
        public LayerPass layerPass;

        public RenderContext(GraphicsDevice graphicsDevice, Matrixd WVP, double minX, double maxX, double minY, double maxY, double cameraZoom, LayerPass layerPass)
        {
            this.graphicsDevice = graphicsDevice;
            this.WVP = WVP;
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            this.cameraZoom = cameraZoom;
            this.layerPass = layerPass;
        }

        public enum LayerPass
        {
            TREE_DENSITY_PASS, GRASS_DENSITY_PASS, MAIN_PASS, UI_PASS, SELECTION_PASS
        }
    }
}
