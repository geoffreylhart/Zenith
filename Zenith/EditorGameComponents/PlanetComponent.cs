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
using Zenith.ZGame;
using Zenith.ZGeom;
using Zenith.ZGraphics;
using Zenith.ZGraphics.GraphicsBuffers;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents
{
    internal class PlanetComponent : ZGameComponent
    {
        private EditorCamera camera;
        private Dictionary<ISector, RenderTarget2D> renderTargets = new Dictionary<ISector, RenderTarget2D>();
        private OSMSectorLoader osmSectorLoader = new OSMSectorLoader();

        private static int MAX_ZOOM = 19;
        Dictionary<ISector, IGraphicsBuffer> loadedMaps = new Dictionary<ISector, IGraphicsBuffer>();
        ISector toLoad = null;

        public PlanetComponent(Game game, EditorCamera camera)
        {
            this.camera = camera;
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
#if WINDOWS
                RenderTarget2D renderTarget = new RenderTarget2D(
                     game.GraphicsDevice,
                     2560,
                     1440,
                     true,
                     GraphicsDevice.PresentationParameters.BackBufferFormat,
                     DepthFormat.Depth24);
#else
                RenderTarget2D renderTarget = new RenderTarget2D(
                     game.GraphicsDevice,
                     2560,
                     1440,
                     false,
                     game.GraphicsDevice.PresentationParameters.BackBufferFormat,
                     DepthFormat.Depth24);
#endif
                renderTargets[rootSector] = renderTarget;
            }
        }

        Dictionary<ISector, SectorBounds> allBounds = new Dictionary<ISector, SectorBounds>(); // TODO: refactor this away from static-ness
        public override void InitDraw(RenderContext renderContext)
        {
            allBounds = new Dictionary<ISector, SectorBounds>();
            // precompute this because it depends heavily on the active GraphicsDevice RenderTarget
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                allBounds[rootSector] = GetSectorBounds(renderContext.graphicsDevice, rootSector);
            }
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                InitDraw(renderContext.graphicsDevice, allBounds[rootSector], rootSector);
            }
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                DrawFlat(renderContext.graphicsDevice, allBounds[rootSector], rootSector);
            }
        }

        public override void Draw(RenderContext renderContext, GameTime gameTime)
        {
            if (renderContext.layerPass == RenderContext.LayerPass.MAIN_PASS && !Game1.DEFERRED_RENDERING)
            {
                var effect = this.GetDefaultEffect(renderContext.graphicsDevice);

                camera.ApplyMatrices(effect);
                float distance = (float)(9 * Math.Pow(0.5, camera.cameraZoom)); // TODO: this is hacky
                effect.View = CameraMatrixManager.GetWorldRelativeView(distance);

                effect.TextureEnabled = true;
                foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
                {
                    SectorBounds bounds = GetSectorBounds(renderContext.graphicsDevice, rootSector);
                    VertexIndiceBuffer sphere = SphereBuilder.MakeSphereSegExplicit(renderContext.graphicsDevice, rootSector, 2, bounds.minX, bounds.minY, bounds.maxX, bounds.maxY, camera);
                    effect.Texture = renderTargets[rootSector];
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        renderContext.graphicsDevice.Indices = sphere.indices;
                        renderContext.graphicsDevice.SetVertexBuffer(sphere.vertices);
                        renderContext.graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sphere.indices.IndexCount / 3);
                    }
                    sphere.vertices.Dispose();
                    sphere.indices.Dispose();
                }
            }
            if (renderContext.layerPass == RenderContext.LayerPass.MAIN_PASS && Game1.DEFERRED_RENDERING)
            {
                var effect = GlobalContent.DeferredBasicNormalTextureShader;
                float distance = (float)(9 * Math.Pow(0.5, camera.cameraZoom)); // TODO: this is hacky
                Matrix view = CameraMatrixManager.GetWorldRelativeView(distance);
                effect.Parameters["WVP"].SetValue(camera.world * view * camera.projection);
                foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
                {
                    SectorBounds bounds = GetSectorBounds(renderContext.graphicsDevice, rootSector);
                    VertexIndiceBuffer sphere = SphereBuilder.MakeSphereSegExplicit(renderContext.graphicsDevice, rootSector, 2, bounds.minX, bounds.minY, bounds.maxX, bounds.maxY, camera);
                    effect.Parameters["Texture"].SetValue(renderTargets[rootSector]);
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        renderContext.graphicsDevice.Indices = sphere.indices;
                        renderContext.graphicsDevice.SetVertexBuffer(sphere.vertices);
                        renderContext.graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sphere.indices.IndexCount / 3);
                    }
                    sphere.vertices.Dispose();
                    sphere.indices.Dispose();
                }
            }
            foreach (var rootSector in ZCoords.GetSectorManager().GetTopmostOSMSectors())
            {
                Draw3D(renderContext.graphicsDevice, allBounds[rootSector], rootSector, renderContext.layerPass);
            }
        }

        public override void Update(GraphicsDevice graphicsDevice, GameTime gameTime)
        {
            Vector3d circleStart = camera.GetLatLongOfCoord(graphicsDevice, Mouse.GetState().X, Mouse.GetState().Y);
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

        protected BasicEffect GetDefaultEffect(GraphicsDevice graphicsDevice)
        {
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.LightingEnabled = true;
            basicEffect.DirectionalLight0.Direction = new Vector3(-1, 1, 0);
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.8f, 0.8f, 0.8f);
            basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            return basicEffect;
        }

        private void InitDraw(GraphicsDevice graphicsDevice, SectorBounds bounds, ISector rootSector)
        {
            double relativeCameraZoom = camera.cameraZoom - Math.Log(ZCoords.GetSectorManager().GetTopmostOSMSectors().Count, 4) + (Game1.RECORDING ? 1 : 0);
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
            if (toLoad != null || Constants.TO_LOAD != null)
            {
                if (Constants.TO_LOAD != null)
                {
                    toLoad = ZCoords.GetSectorManager().FromString(Constants.TO_LOAD);
                }
                Stopwatch sw = new Stopwatch();
                sw.Start();
                foreach (var sector in toLoad.GetChildrenAtLevel(ZCoords.GetSectorManager().GetHighestOSMZoom()))
                {
                    osmSectorLoader.GetGraphicsBuffer(graphicsDevice, sector).Dispose();
                }
                Console.WriteLine($"Total load time for {toLoad} is {sw.Elapsed.TotalHours} h");
                toLoad = null;
                if (Constants.TO_LOAD != null)
                {
                    Constants.TERMINATE = true;
                    Constants.TO_LOAD = null;
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
                Matrixd projection = Matrixd.CreateOrthographicOffCenter(bounds.minX * (1 << sector.Zoom) - sector.X, bounds.maxX * (1 << sector.Zoom) - sector.X, bounds.maxY * (1 << sector.Zoom) - sector.Y, bounds.minY * (1 << sector.Zoom) - sector.Y, -1, 0.01f); // TODO: why negative?
                RenderContext context = new RenderContext(graphicsDevice, projection, bounds.minX * (1 << sector.Zoom) - sector.X, bounds.maxX * (1 << sector.Zoom) - sector.X, bounds.minY * (1 << sector.Zoom) - sector.Y, bounds.maxY * (1 << sector.Zoom) - sector.Y, camera.cameraZoom, RenderContext.LayerPass.MAIN_PASS);
                buffer.InitDraw(context);
            }
        }

        private void DrawFlat(GraphicsDevice graphicsDevice, SectorBounds bounds, ISector rootSector)
        {
            RenderTarget2D renderTarget = renderTargets[rootSector];
            // Set the render target
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };
            graphicsDevice.Clear(Pallete.OCEAN_BLUE);

            double relativeCameraZoom = camera.cameraZoom - Math.Log(ZCoords.GetSectorManager().GetTopmostOSMSectors().Count, 4) + (Game1.RECORDING ? 1 : 0);
            int zoomLevel = Math.Min(Math.Max((int)(relativeCameraZoom - 3), 0), ZCoords.GetSectorManager().GetHighestOSMZoom());
            List<ISector> containedSectors = rootSector.GetSectorsInRange(bounds.minX, bounds.maxX, bounds.minY, bounds.maxY, zoomLevel);
            List<ISector> sorted = containedSectors.Where(x => x.GetRoot().Equals(rootSector)).ToList();
            sorted.Sort((x, y) => x.Zoom.CompareTo(y.Zoom));
            foreach (var sector in sorted)
            {
                IGraphicsBuffer buffer = loadedMaps[sector];
                if (!(buffer is ImageTileBuffer)) continue;
                Matrixd projection = Matrixd.CreateOrthographicOffCenter(bounds.minX * (1 << sector.Zoom) - sector.X, bounds.maxX * (1 << sector.Zoom) - sector.X, bounds.maxY * (1 << sector.Zoom) - sector.Y, bounds.minY * (1 << sector.Zoom) - sector.Y, -1, 0.01f); // TODO: why negative?
                RenderContext context = new RenderContext(graphicsDevice, projection, bounds.minX * (1 << sector.Zoom) - sector.X, bounds.maxX * (1 << sector.Zoom) - sector.X, bounds.minY * (1 << sector.Zoom) - sector.Y, bounds.maxY * (1 << sector.Zoom) - sector.Y, camera.cameraZoom, RenderContext.LayerPass.MAIN_PASS);
                buffer.Draw(context);
            }
        }

        private void Draw3D(GraphicsDevice graphicsDevice, SectorBounds bounds, ISector rootSector, RenderContext.LayerPass layerPass)
        {

            double relativeCameraZoom = camera.cameraZoom - Math.Log(ZCoords.GetSectorManager().GetTopmostOSMSectors().Count, 4) + (Game1.RECORDING ? 1 : 0);
            int zoomLevel = Math.Min(Math.Max((int)(relativeCameraZoom - 3), 0), ZCoords.GetSectorManager().GetHighestOSMZoom());
            List<ISector> containedSectors = rootSector.GetSectorsInRange(bounds.minX, bounds.maxX, bounds.minY, bounds.maxY, zoomLevel);
            List<ISector> sorted = containedSectors.Where(x => x.GetRoot().Equals(rootSector)).ToList();
            sorted.Sort((x, y) => x.Zoom.CompareTo(y.Zoom));
            foreach (var sector in sorted)
            {
                IGraphicsBuffer buffer = loadedMaps[sector];
                if (buffer is ImageTileBuffer) continue;
                SectorBounds b = new SectorBounds(bounds.minX * (1 << sector.Zoom) - sector.X, bounds.maxX * (1 << sector.Zoom) - sector.X, bounds.minY * (1 << sector.Zoom) - sector.Y, bounds.maxY * (1 << sector.Zoom) - sector.Y);
                SectorBounds limitedB = new SectorBounds(Math.Max(0, Math.Min(1, b.minX)), Math.Max(0, Math.Min(1, b.maxX)), Math.Max(0, Math.Min(1, b.minY)), Math.Max(0, Math.Min(1, b.maxY)));
                BasicEffect basicEffect = new BasicEffect(graphicsDevice);
                camera.ApplyMatrices(basicEffect);
                // going to make it easy and assume the shape is perfectly parallel (it's not)
                // the sector plane is constructed by flattening the visible portion of the sphere, basically
                Vector3d v1 = sector.ProjectToSphereCoordinates(new Vector2d(limitedB.minX, limitedB.minY));
                Vector3d v2 = sector.ProjectToSphereCoordinates(new Vector2d(limitedB.maxX, limitedB.minY));
                Vector3d v3 = sector.ProjectToSphereCoordinates(new Vector2d(limitedB.minX, limitedB.maxY));
                Vector3d xAxis = (v2 - v1) / (limitedB.maxX - limitedB.minX);
                Vector3d yAxis = (v3 - v1) / (limitedB.maxY - limitedB.minY);
                Vector3d start = v1 - xAxis * limitedB.minX - yAxis * limitedB.minY;
                Vector3d zAxis = start * (xAxis.Length() + yAxis.Length()) / start.Length() / 2; // make this roughly the same length
                // matrixes copied over
                Matrixd world = Matrixd.CreateRotationZ(-camera.cameraRotX) * Matrixd.CreateRotationX(camera.cameraRotY); // eh.... think hard on this later
                double distance = 9 * Math.Pow(0.5, camera.cameraZoom);
                Matrixd view = CameraMatrixManager.GetWorldViewd(distance);
                Matrixd projection = CameraMatrixManager.GetWorldProjectiond(distance, graphicsDevice.Viewport.AspectRatio);
                Matrixd transformMatrix = new Matrixd(xAxis.X, xAxis.Y, xAxis.Z, 0, yAxis.X, yAxis.Y, yAxis.Z, 0, zAxis.X, zAxis.Y, zAxis.Z, 0, start.X, start.Y, start.Z, 1); // turns our local coordinates into 3d spherical coordinates, based on the sector
                Matrixd WVP = Normalize(transformMatrix * world * view * projection); // combine them all to allow for higher precision
                RenderContext context = new RenderContext(graphicsDevice, WVP, b.minX, b.maxX, b.minY, b.maxY, camera.cameraZoom, layerPass);
                buffer.Draw(context);
            }
        }

        private Matrixd Normalize(Matrixd m)
        {
            var vals = new double[] { m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44 };
            double scaleAmount = 1 / vals.Select(Math.Abs).Average();
            m.M11 *= scaleAmount;
            m.M12 *= scaleAmount;
            m.M13 *= scaleAmount;
            m.M14 *= scaleAmount;
            m.M21 *= scaleAmount;
            m.M22 *= scaleAmount;
            m.M23 *= scaleAmount;
            m.M24 *= scaleAmount;
            m.M31 *= scaleAmount;
            m.M32 *= scaleAmount;
            m.M33 *= scaleAmount;
            m.M34 *= scaleAmount;
            m.M41 *= scaleAmount;
            m.M42 *= scaleAmount;
            m.M43 *= scaleAmount;
            m.M44 *= scaleAmount;
            return m;
        }

        private SectorBounds GetSectorBounds(GraphicsDevice graphicsDevice, ISector rootSector)
        {
            // apparently we don't want to call this after changing our render target
            double w = graphicsDevice.Viewport.Width;
            double h = graphicsDevice.Viewport.Height;
            var leftArc = camera.GetArc(graphicsDevice, 0, h, 0, 0);
            var rightArc = camera.GetArc(graphicsDevice, w, 0, w, h);
            var topArc = camera.GetArc(graphicsDevice, 0, 0, w, 0);
            var bottomArc = camera.GetArc(graphicsDevice, w, h, 0, h);
            List<SphereArc> arcs = new List<SphereArc>();
            if (leftArc != null) arcs.Add(leftArc);
            if (topArc != null) arcs.Add(topArc);
            if (rightArc != null) arcs.Add(rightArc);
            if (bottomArc != null) arcs.Add(bottomArc);
            Circle3 visible = camera.GetUnitSphereVisibleCircle(graphicsDevice);
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
                        if (close1.Cross(halfway).Dot(camera.GetPosition(graphicsDevice)) > 0) halfway = -halfway;
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
                if (camera.IsUnitSpherePointVisible(graphicsDevice, new Vector3d(0, 0, 1)))
                {
                    minY = 0;
                    minX = 0;
                    maxX = 1;
                }
                if (camera.IsUnitSpherePointVisible(graphicsDevice, new Vector3d(0, 0, -1)))
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
