﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Zenith.EditorGameComponents.FlatComponents;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZGeom;
using Zenith.ZGraphics;
using Zenith.ZGraphics.GraphicsBuffers;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents
{
    internal class PlanetComponent : DrawableGameComponent
    {
        private EditorCamera camera;
        private Dictionary<ISector, RenderTarget2D> renderTargets = new Dictionary<ISector, RenderTarget2D>();
        private OSMSectorLoader osmSectorLoader = new OSMSectorLoader();

        private static int MAX_ZOOM = 19;
        Dictionary<ISector, IGraphicsBuffer> loadedMaps = new Dictionary<ISector, IGraphicsBuffer>();
        ISector toLoad = null;

        public PlanetComponent(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                RenderTarget2D renderTarget = new RenderTarget2D(
                     GraphicsDevice,
                     2560,
                     1440,
                     true,
                     GraphicsDevice.PresentationParameters.BackBufferFormat,
                     DepthFormat.Depth24);
                renderTargets[rootSector] = renderTarget;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var allBounds = new Dictionary<ISector, SectorBounds>();
            // precompute this because it depends heavily on the active GraphicsDevice RenderTarget
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                allBounds[rootSector] = GetSectorBounds(rootSector);
            }
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                InitDraw(GraphicsDevice, allBounds[rootSector], rootSector);
            }
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                DrawFlat(GraphicsDevice, allBounds[rootSector], rootSector);
            }
            for (int i = 0; i < Game1.RENDER_TARGET_COUNT; i++)
            {
                GraphicsDevice.SetRenderTarget(Game1.renderTargets[i]);
                if (i == 2)
                {
                    var basicEffect3 = this.GetDefaultEffect();

                    camera.ApplyMatrices(basicEffect3);
                    float distance = (float)(9 * Math.Pow(0.5, camera.cameraZoom)); // TODO: this is hacky
                    basicEffect3.View = CameraMatrixManager.GetWorldRelativeView(distance);

                    basicEffect3.TextureEnabled = true;
                    foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
                    {
                        SectorBounds bounds = GetSectorBounds(rootSector);
                        VertexIndiceBuffer sphere = SphereBuilder.MakeSphereSegExplicit(GraphicsDevice, rootSector, 2, bounds.minX, bounds.minY, bounds.maxX, bounds.maxY, camera);
                        basicEffect3.Texture = renderTargets[rootSector];
                        foreach (EffectPass pass in basicEffect3.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            GraphicsDevice.Indices = sphere.indices;
                            GraphicsDevice.SetVertexBuffer(sphere.vertices);
                            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sphere.indices.IndexCount / 3);
                        }
                        sphere.vertices.Dispose();
                        sphere.indices.Dispose();
                    }
                }
                foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
                {
                    Draw3D(GraphicsDevice, allBounds[rootSector], rootSector, i);
                }
            }
            GraphicsDevice.SetRenderTarget(null);
        }

        public override void Update(GameTime gameTime)
        {
            Vector3d circleStart = camera.GetLatLongOfCoord(Mouse.GetState().X, Mouse.GetState().Y);
            if (circleStart != null)
            {
                // update in reverse order
                //if (UILayer.LeftPressed) AddImage(circleStart.X, circleStart.Y, camera.cameraZoom);
            }
        }

        private void AddImage(double mouseX, double mouseY, double cameraZoom)
        {
            ISector squareCenter = GetSector(mouseX, mouseY, cameraZoom);
            if (squareCenter == null) return;
            toLoad = squareCenter;
        }

        private ISector GetSector(double mouseX, double mouseY, double cameraZoom)
        {
            int zoom = GetRoundedZoom(cameraZoom);
            LongLat longLat = new LongLat(mouseX, mouseY);
            foreach (var sector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                if (sector.ContainsLongLat(longLat))
                {
                    var localCoord = sector.ProjectToLocalCoordinates(longLat.ToSphereVector());
                    var sectorAt = sector.GetSectorAt(localCoord.X, localCoord.Y, zoom);
                    if (sectorAt != null) return sectorAt;
                }
            }
            return null;
        }

        private int GetRoundedZoom(double cameraZoom)
        {
            return Math.Min((int)cameraZoom, MAX_ZOOM); // google only accepts integer zoom
        }

        // ------------
        private double GetZoomPortion()
        {
            return Math.Pow(0.5, camera.cameraZoom) * 2;
            //return 1;
        }

        protected BasicEffect GetDefaultEffect()
        {
            var basicEffect = new BasicEffect(Game.GraphicsDevice);
            basicEffect.LightingEnabled = true;
            basicEffect.DirectionalLight0.Direction = new Vector3(-1, 1, 0);
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.8f, 0.8f, 0.8f);
            basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            return basicEffect;
        }

        private void InitDraw(GraphicsDevice graphicsDevice, SectorBounds bounds, ISector rootSector)
        {
            double relativeCameraZoom = camera.cameraZoom - Math.Log(ZCoords.GetSectorManager().GetTopmostOSMSectors().Count, 4) + (Game1.recording ? 1 : 0);
            // autoload stuff
            // TODO: move to update step?
            int zoomLevel = Math.Min(Math.Max((int)(relativeCameraZoom - 3), 0), ZCoords.GetSectorManager().GetHighestOSMZoom());
            List<ISector> containedSectors = rootSector.GetSectorsInRange(bounds.minX, bounds.maxX, bounds.minY, bounds.maxY, zoomLevel);
            foreach (var pair in loadedMaps.Where(x => AllowUnload(x.Key, rootSector, containedSectors)).ToList())
            {
                loadedMaps[pair.Key].Dispose();
                loadedMaps.Remove(pair.Key);
            }
            // end autoload stuff
            if (toLoad != null || Program.TO_LOAD != null)
            {
                if (Program.TO_LOAD != null)
                {
                    toLoad = ZCoords.GetSectorManager().FromString(Program.TO_LOAD);
                }
                Stopwatch sw = new Stopwatch();
                sw.Start();
                foreach (var sector in toLoad.GetChildrenAtLevel(ZCoords.GetSectorManager().GetHighestOSMZoom()))
                {
                    osmSectorLoader.GetGraphicsBuffer(graphicsDevice, sector).Dispose();
                }
                Console.WriteLine($"Total load time for {toLoad} is {sw.Elapsed.TotalHours} h");
                toLoad = null;
                if (Program.TO_LOAD != null)
                {
                    Program.TERMINATE = true;
                    Program.TO_LOAD = null;
                }
            }
            bool loadCache = !(relativeCameraZoom - 4 > ZCoords.GetSectorManager().GetHighestOSMZoom());
            foreach (var l in containedSectors)
            {
                if (loadCache)
                {
                    if (!loadedMaps.ContainsKey(l))
                    {
                        loadedMaps[l] = osmSectorLoader.GetCacheBuffer(graphicsDevice, l);
                    }
                }
                else
                {
                    if (!loadedMaps.ContainsKey(l) || loadedMaps[l] is ImageTileBuffer)
                    {
                        if (loadedMaps.ContainsKey(l)) loadedMaps[l].Dispose();
                        loadedMaps[l] = osmSectorLoader.GetGraphicsBuffer(graphicsDevice, l);
                    }
                }
            }
            List<ISector> sorted = containedSectors.Where(x => x.GetRoot().Equals(rootSector)).ToList();
            sorted.Sort((x, y) => x.Zoom.CompareTo(y.Zoom));
            foreach (var sector in sorted)
            {
                IGraphicsBuffer buffer = loadedMaps[sector];
                BasicEffect basicEffect = new BasicEffect(graphicsDevice);
                basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)(bounds.minX * (1 << sector.Zoom) - sector.X), (float)(bounds.maxX * (1 << sector.Zoom) - sector.X), (float)(bounds.maxY * (1 << sector.Zoom) - sector.Y), (float)(bounds.minY * (1 << sector.Zoom) - sector.Y), -1, 0.01f); // TODO: why negative?
                buffer.InitDraw(graphicsDevice, basicEffect, bounds.minX * (1 << sector.Zoom) - sector.X, bounds.maxX * (1 << sector.Zoom) - sector.X, bounds.minY * (1 << sector.Zoom) - sector.Y, bounds.maxY * (1 << sector.Zoom) - sector.Y, camera.cameraZoom);
            }
        }

        private void DrawFlat(GraphicsDevice graphicsDevice, SectorBounds bounds, ISector rootSector)
        {
            RenderTarget2D renderTarget = renderTargets[rootSector];
            // Set the render target
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };
            graphicsDevice.Clear(Pallete.OCEAN_BLUE);

            double relativeCameraZoom = camera.cameraZoom - Math.Log(ZCoords.GetSectorManager().GetTopmostOSMSectors().Count, 4) + (Game1.recording ? 1 : 0);
            int zoomLevel = Math.Min(Math.Max((int)(relativeCameraZoom - 3), 0), ZCoords.GetSectorManager().GetHighestOSMZoom());
            List<ISector> containedSectors = rootSector.GetSectorsInRange(bounds.minX, bounds.maxX, bounds.minY, bounds.maxY, zoomLevel);
            List<ISector> sorted = containedSectors.Where(x => x.GetRoot().Equals(rootSector)).ToList();
            sorted.Sort((x, y) => x.Zoom.CompareTo(y.Zoom));
            foreach (var sector in sorted)
            {
                IGraphicsBuffer buffer = loadedMaps[sector];
                if (!(buffer is ImageTileBuffer)) continue;
                BasicEffect basicEffect = new BasicEffect(graphicsDevice);
                basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)(bounds.minX * (1 << sector.Zoom) - sector.X), (float)(bounds.maxX * (1 << sector.Zoom) - sector.X), (float)(bounds.maxY * (1 << sector.Zoom) - sector.Y), (float)(bounds.minY * (1 << sector.Zoom) - sector.Y), -1, 0.01f); // TODO: why negative?
                buffer.Draw(graphicsDevice, basicEffect, bounds.minX * (1 << sector.Zoom) - sector.X, bounds.maxX * (1 << sector.Zoom) - sector.X, bounds.minY * (1 << sector.Zoom) - sector.Y, bounds.maxY * (1 << sector.Zoom) - sector.Y, camera.cameraZoom, 0);
            }
        }

        private void Draw3D(GraphicsDevice graphicsDevice, SectorBounds bounds, ISector rootSector, int layer)
        {

            double relativeCameraZoom = camera.cameraZoom - Math.Log(ZCoords.GetSectorManager().GetTopmostOSMSectors().Count, 4) + (Game1.recording ? 1 : 0);
            int zoomLevel = Math.Min(Math.Max((int)(relativeCameraZoom - 3), 0), ZCoords.GetSectorManager().GetHighestOSMZoom());
            List<ISector> containedSectors = rootSector.GetSectorsInRange(bounds.minX, bounds.maxX, bounds.minY, bounds.maxY, zoomLevel);
            List<ISector> sorted = containedSectors.Where(x => x.GetRoot().Equals(rootSector)).ToList();
            sorted.Sort((x, y) => x.Zoom.CompareTo(y.Zoom));
            foreach (var sector in sorted)
            {
                IGraphicsBuffer buffer = loadedMaps[sector];
                if (buffer is ImageTileBuffer) continue;
                BasicEffect basicEffect = new BasicEffect(graphicsDevice);
                camera.ApplyMatrices(basicEffect);
                // going to make it easy and assume the shape is perfectly parallel (it's not)
                Vector3 start = sector.ProjectToSphereCoordinates(new Vector2d(0, 0)).ToVector3();
                Vector3 xAxis = sector.ProjectToSphereCoordinates(new Vector2d(1, 0)).ToVector3() - start;
                Vector3 yAxis = sector.ProjectToSphereCoordinates(new Vector2d(0, 1)).ToVector3() - start;
                Vector3 zAxis = start * (xAxis.Length() + yAxis.Length()) / start.Length() / 2; // make this roughly the same length
                // matrixes copied over
                Matrixd world = Matrixd.CreateRotationZ(-camera.cameraRotX) * Matrixd.CreateRotationX(camera.cameraRotY); // eh.... think hard on this later
                double distance = 9 * Math.Pow(0.5, camera.cameraZoom);
                Matrixd view = CameraMatrixManager.GetWorldViewd(distance);
                Matrixd projection = CameraMatrixManager.GetWorldProjectiond(distance, this.GraphicsDevice.Viewport.AspectRatio);
                Matrixd transformMatrix = new Matrixd(xAxis.X, xAxis.Y, xAxis.Z, 0, yAxis.X, yAxis.Y, yAxis.Z, 0, zAxis.X, zAxis.Y, zAxis.Z, 0, start.X, start.Y, start.Z, 1); // turns our local coordinates into 3d spherical coordinates, based on the sector
                basicEffect.World = (transformMatrix * world * view * projection).toMatrix(); // combine them all to allow for higher precision
                basicEffect.View = Matrix.Identity;
                basicEffect.Projection = Matrix.Identity;
                buffer.Draw(graphicsDevice, basicEffect, bounds.minX * (1 << sector.Zoom) - sector.X, bounds.maxX * (1 << sector.Zoom) - sector.X, bounds.minY * (1 << sector.Zoom) - sector.Y, bounds.maxY * (1 << sector.Zoom) - sector.Y, camera.cameraZoom, layer) ;
            }
        }

        private SectorBounds GetSectorBounds(ISector rootSector)
        {
            // apparently we don't want to call this after changing our render target
            double w = GraphicsDevice.Viewport.Width;
            double h = GraphicsDevice.Viewport.Height;
            var leftArc = camera.GetArc(0, h, 0, 0);
            var rightArc = camera.GetArc(w, 0, w, h);
            var topArc = camera.GetArc(0, 0, w, 0);
            var bottomArc = camera.GetArc(w, h, 0, h);
            List<SphereArc> arcs = new List<SphereArc>();
            if (leftArc != null) arcs.Add(leftArc);
            if (topArc != null) arcs.Add(topArc);
            if (rightArc != null) arcs.Add(rightArc);
            if (bottomArc != null) arcs.Add(bottomArc);
            Circle3 visible = camera.GetUnitSphereVisibleCircle();
            double minX, maxX, minY, maxY;
            if (arcs.Count > 0)
            {
                int cnt = arcs.Count;
                // try and construct the arc segments that connect our disconnected arcs if they are
                for (int i = 0; i < cnt; i++)
                {
                    SphereArc arc1 = arcs[i];
                    SphereArc arc2 = arcs[(i + 1) % cnt];
                    Vector3d close1 = arc1.stop;
                    Vector3d close2 = arc2.start;
                    if ((close1 - close2).Length() > 0.01)
                    {
                        Vector3d halfway = (close1 + close2).Normalized();
                        if (close1.Cross(halfway).Dot(camera.GetPosition()) > 0) halfway = -halfway;
                        arcs.Add(new SphereArc(visible, close1, halfway, true));
                        arcs.Add(new SphereArc(visible, halfway, close2, true));
                    }
                }
                minX = arcs.Min(x => x.Min(y => rootSector.ProjectToLocalCoordinates(y).X));
                maxX = arcs.Max(x => x.Max(y => rootSector.ProjectToLocalCoordinates(y).X));
                minY = arcs.Min(x => x.Min(y => rootSector.ProjectToLocalCoordinates(y).Y));
                maxY = arcs.Max(x => x.Max(y => rootSector.ProjectToLocalCoordinates(y).Y));
            }
            else
            {
                minX = visible.Min(x => rootSector.ProjectToLocalCoordinates(x).X);
                maxX = visible.Max(x => rootSector.ProjectToLocalCoordinates(x).X);
                minY = visible.Min(x => rootSector.ProjectToLocalCoordinates(x).Y);
                maxY = visible.Max(x => rootSector.ProjectToLocalCoordinates(x).Y);
            }
            if (rootSector is MercatorSector)
            {
                if (camera.IsUnitSpherePointVisible(new Vector3d(0, 0, 1)))
                {
                    minY = 0;
                    minX = 0;
                    maxX = 1;
                }
                if (camera.IsUnitSpherePointVisible(new Vector3d(0, 0, -1)))
                {
                    maxY = 1;
                    minX = 0;
                    maxX = 1;
                }
            }
            return new SectorBounds(Math.Max(minX, 0), Math.Min(maxX, 1), Math.Max(minY, 0), Math.Min(maxY, 1));
        }

        private bool AllowUnload(ISector sector, ISector rootSector, List<ISector> loadingSectors)
        {
            //if (loadedMaps[sector] is ProceduralTileBuffer) return false; // TODO: eventually unload these, maybe just have a queue
            //if (!sector.GetRoot().Equals(rootSector)) return false;
            //foreach (var s in loadingSectors)
            //{
            //    if (s.Equals(sector)) return false; // very basic: don't unload sectors immediately after loading them
            //}
            //return true;
            return false;
        }

        class SectorBounds
        {
            public double minX;
            public double maxX;
            public double minY;
            public double maxY;

            public SectorBounds(double minX, double maxX, double minY, double maxY)
            {
                this.minX = minX;
                this.maxX = maxX;
                this.minY = minY;
                this.maxY = maxY;
            }
        }
    }
}
