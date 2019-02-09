using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.MathHelpers;
using Zenith.PrimitiveBuilder;
using Zenith.ZGraphics;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents.FlatComponents
{
    abstract class SectorLoader : IFlatComponent, IEditorGameComponent
    {
        private static int MAX_ZOOM = 19;
        Dictionary<String, VertexIndiceBuffer> loadedMaps = new Dictionary<string, VertexIndiceBuffer>();
        List<Sector>[] imageLayers = new List<Sector>[MAX_ZOOM + 1];
        Sector previewSquare = null;

        public SectorLoader()
        {
            for (int i = 0; i <= MAX_ZOOM; i++) imageLayers[i] = new List<Sector>();
        }

        public void Draw(RenderTarget2D renderTarget, double minX, double maxX, double minY, double maxY, double cameraZoom)
        {
            GraphicsDevice graphicsDevice = renderTarget.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.TextureEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter((float)minX, (float)maxX, (float)maxY, (float)minY, 1, 1000);

            foreach (var layer in imageLayers)
            {
                foreach (var sector in layer)
                {
                    if (!loadedMaps.ContainsKey(sector.ToString()))
                    {
                        loadedMaps[sector.ToString()] = GetCachedMap(renderTarget, sector);
                    }
                    VertexIndiceBuffer buffer = loadedMaps[sector.ToString()];
                    basicEffect.Texture = buffer.texture;
                    foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphicsDevice.Indices = buffer.indices;
                        graphicsDevice.SetVertexBuffer(buffer.vertices);
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.indices.IndexCount / 3);
                    }
                    graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, graphicsDevice.Viewport.MaxDepth, 0);
                }
            }
            if (previewSquare != null)
            {
                basicEffect.TextureEnabled = false;
                basicEffect.VertexColorEnabled = true;
                basicEffect.LightingEnabled = false;
                float minLat = (float)ToLat(ToY(previewSquare.Latitude) - previewSquare.ZoomPortion / 2);
                float maxLat = (float)ToLat(ToY(previewSquare.Latitude) + previewSquare.ZoomPortion / 2);
                float minLong = (float)(previewSquare.Longitude - Math.PI * previewSquare.ZoomPortion);
                float maxLong = (float)(previewSquare.Longitude + Math.PI * previewSquare.ZoomPortion);
                float w = maxLong - minLong;
                float h = maxLat - minLat;
                Color color = CacheExists(previewSquare) ? Color.Green : Color.Red;
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat, w / 20, h, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat, w, h / 20, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong + w * 19 / 20, minLat, w / 20, h, color);
                GraphicsBasic.DrawRect(graphicsDevice, basicEffect, minLong, minLat + h * 19 / 20, w, h / 20, color);
            }
        }

        public abstract bool CacheExists(Sector sector);

        public void Update(double mouseX, double mouseY, double cameraZoom)
        {
            if (UILayer.LeftPressed) AddImage(mouseX, mouseY, cameraZoom);
            if (UILayer.LeftAvailable)
            {
                previewSquare = GetSector(mouseX, mouseY, cameraZoom);
            }
            else
            {
                previewSquare = null;
            }
        }

        private int GetRoundedZoom(double cameraZoom)
        {
            return Math.Min((int)cameraZoom, MAX_ZOOM); // google only accepts integer zoom
        }

        private Sector GetSector(double mouseX, double mouseY, double cameraZoom)
        {
            int zoom = GetRoundedZoom(cameraZoom);
            double zoomPortion = Math.Pow(0.5, zoom);
            int x = (int)((mouseX + Math.PI) / (zoomPortion * 2 * Math.PI));
            int y = (int)(ToY(mouseY) / (zoomPortion));
            y = Math.Max(y, 0);
            y = Math.Min(y, (1 << zoom) - 1);
            return new Sector(x, y, zoom);
        }

        public abstract IEnumerable<Sector> EnumerateCachedSectors();

        private void LoadAllCached(GraphicsDevice graphicsDevice)
        {
            foreach (var sector in EnumerateCachedSectors())
            {
                imageLayers[sector.zoom].Add(sector);
            }
        }

        private void AddImage(double mouseX, double mouseY, double cameraZoom)
        {
            Sector squareCenter = GetSector(mouseX, mouseY, cameraZoom);
            if (squareCenter == null) return;
            imageLayers[squareCenter.zoom].Add(squareCenter);
        }

        public abstract Texture2D GetTexture(GraphicsDevice graphicsDevice, Sector sector);

        private VertexIndiceBuffer GetCachedMap(RenderTarget2D renderTarget, Sector sector)
        {
            VertexIndiceBuffer buffer = SphereBuilder.MapMercatorToCylindrical(renderTarget.GraphicsDevice, 2, Math.Pow(0.5, sector.zoom), sector.Latitude, sector.Longitude);
            buffer.texture = GetTexture(renderTarget.GraphicsDevice, sector);
            renderTarget.GraphicsDevice.SetRenderTarget(renderTarget); // TODO: let's just assume this won't cause issues for now
            return buffer;
        }

        public class Sector
        {
            public int x; // measured 0,1,2,3 from -pi to pi (opposite left to opposite right of prime meridian)

            internal double MinDistanceFrom(LongLat longLat)
            {
                if (longLat.X >= LeftLongitude && longLat.X <= RightLongitude)
                {
                    if (longLat.Y >= BottomLatitude && longLat.Y <= TopLatitude)
                    {
                        return 0;
                    }
                    else
                    {
                        return Math.Min(Math.Abs(TopLatitude - longLat.Y), Math.Abs(BottomLatitude - longLat.Y));
                    }
                }
                else
                {
                    if (longLat.Y >= BottomLatitude && longLat.Y <= TopLatitude)
                    {
                        return Math.Min(Math.Abs(LeftLongitude - longLat.X), Math.Abs(RightLongitude - longLat.X));
                    }
                    else
                    {
                        SphereVector asSphereVec = longLat.ToSphereVector();
                        double dis = TopLeftCorner.ToSphereVector().Distance(asSphereVec);
                        dis = Math.Min(dis, TopRightCorner.ToSphereVector().Distance(asSphereVec));
                        dis = Math.Min(dis, BottomLeftCorner.ToSphereVector().Distance(asSphereVec));
                        dis = Math.Min(dis, BottomRightCorner.ToSphereVector().Distance(asSphereVec));
                        return dis;
                    }
                }
            }

            public int y; // measured 0,1,2,3 from -pi/2 (south pole) to pi/2 (north pole)
            public int zoom; // the globe is partitioned into 2^zoom vertical and horizontal sections

            public Sector(int x, int y, int zoom)
            {
                this.x = x;
                this.y = y;
                this.zoom = zoom;
            }

            public double ZoomPortion { get { return Math.Pow(0.5, zoom); } }
            public double Longitude { get { return (x + 0.5) * (ZoomPortion * 2 * Math.PI) - Math.PI; } }
            public double LeftLongitude { get { return x * (ZoomPortion * 2 * Math.PI) - Math.PI; } }
            public double RightLongitude { get { return (x + 1) * (ZoomPortion * 2 * Math.PI) - Math.PI; } }
            public double Latitude { get { return ToLat((y + 0.5) * (ZoomPortion)); } }
            public double TopLatitude { get { return ToLat((y + 1) * (ZoomPortion)); } }
            public double BottomLatitude { get { return ToLat(y * (ZoomPortion)); } }
            public LongLat TopLeftCorner { get { return new LongLat(LeftLongitude, TopLatitude); } }
            public LongLat TopRightCorner { get { return new LongLat(RightLongitude, TopLatitude); } }
            public LongLat BottomLeftCorner { get { return new LongLat(LeftLongitude, BottomLatitude); } }
            public LongLat BottomRightCorner { get { return new LongLat(RightLongitude, BottomLatitude); } }

            public double SurfaceAreaPortion // where 1 is the whole sphere
            {
                get
                {
                    // A=2pi*r^2*(1-cos0) wiki
                    double capDiff = 2 * Math.PI * (Math.Sin(TopLatitude) - Math.Sin(BottomLatitude));
                    double sa = capDiff * ZoomPortion;
                    return sa / (4 * Math.PI);
                }
            }

            public override string ToString()
            {
                return $"X={x},Y={y},Zoom={zoom}";
            }

            public bool ContainsSector(Sector sector)
            {
                if (sector.zoom < zoom) return false;
                if ((sector.x >> (sector.zoom - zoom)) != x) return false;
                if ((sector.y >> (sector.zoom - zoom)) != y) return false;
                return true;
            }

            internal List<Sector> GetChildrenAtLevel(int z)
            {
                List<Sector> answer = new List<Sector>();
                if (z > zoom)
                {
                    int diffPow = 1 << (z - zoom);
                    for (int i = 0; i < diffPow; i++)
                    {
                        for (int j = 0; j < diffPow; j++)
                        {
                            answer.Add(new Sector(x * diffPow + i, y * diffPow + j, z));
                        }
                    }
                }
                return answer;
            }

            // TODO: all of these assume a zoom lower than current right now
            internal int GetRelativeXOf(Sector s)
            {
                int diff = s.zoom - zoom;
                return s.x - x * (1 << diff);
            }

            internal int GetRelativeYOf(Sector s)
            {
                int diff = s.zoom - zoom;
                return s.y - y * (1 << diff);
            }

            internal IEnumerable<Sector> GetAllParents()
            {
                List<Sector> answer = new List<Sector>();
                for (int i = 1; i <= zoom; i++)
                {
                    answer.Add(new Sector(x >> i, y >> i, zoom - i));
                }
                return answer;
            }

            internal bool ContainsLongLat(LongLat longLat)
            {
                if (longLat.X < LeftLongitude || longLat.X > RightLongitude) return false;
                if (longLat.Y < BottomLatitude || longLat.Y > TopLatitude) return false;
                return true;
            }

            // do we treat these as straight lines or arc lines?
            // I guess lets do straight lines
            // let's return them in order of intersection
            internal LongLat[] GetIntersections(LongLat start, LongLat end)
            {
                List<LongLat> answer = new List<LongLat>();
                answer.AddRange(GetIntersections(start, end, TopLeftCorner, TopRightCorner));
                answer.AddRange(GetIntersections(start, end, TopRightCorner, BottomRightCorner));
                answer.AddRange(GetIntersections(start, end, BottomRightCorner, BottomLeftCorner));
                answer.AddRange(GetIntersections(start, end, BottomLeftCorner, TopLeftCorner));
                answer.Sort((x, y) => (Math.Pow(x.X - start.X, 2) * Math.Pow(x.Y - start.Y, 2)).CompareTo(Math.Pow(y.X - start.X, 2) * Math.Pow(y.Y - start.Y, 2)));
                return answer.ToArray();
            }

            private LongLat[] GetIntersections(LongLat A, LongLat B, LongLat C, LongLat D)
            {
                LongLat CmP = new LongLat(C.X - A.X, C.Y - A.Y);
                LongLat r = new LongLat(B.X - A.X, B.Y - A.Y);
                LongLat s = new LongLat(D.X - C.X, D.Y - C.Y);

                double CmPxr = CmP.X * r.Y - CmP.Y * r.X;
                double CmPxs = CmP.X * s.Y - CmP.Y * s.X;
                double rxs = r.X * s.Y - r.Y * s.X;

                double rxsr = 1f / rxs;
                double t = CmPxs * rxsr;
                double u = CmPxr * rxsr;

                if ((t >= 0) && (t <= 1) && (u >= 0) && (u <= 1))
                {
                    return new[] { new LongLat(A.X * (1 - t) + B.X * t, A.Y * (1 - t) + B.Y * t) };
                }
                else
                {
                    return new LongLat[0];
                }
            }
        }



        private static double ToLat(double y)
        {
            return 2 * Math.Atan(Math.Pow(Math.E, (y - 0.5) * 2 * Math.PI)) - Math.PI / 2;
        }

        // takes -pi/2 to pi/2, I assume, goes from -infinity to infinity??
        // goes from 0 to 1 in the cutoff range of 85.051129 degrees
        private static double ToY(double lat)
        {
            return Math.Log(Math.Tan(lat / 2 + Math.PI / 4)) / (Math.PI * 2) + 0.5;
        }

        public List<string> GetDebugInfo()
        {
            return new List<string>();
        }

        public List<IUIComponent> GetSettings()
        {
            return new List<IUIComponent>();
        }

        public List<IEditorGameComponent> GetSubComponents()
        {
            return new List<IEditorGameComponent>();
        }
    }
}
