using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zenith.LibraryWrappers.OSM;
using Zenith.ZMath;

namespace Zenith.ZGeom
{
    public class SectorConstrainedOSMAreaGraph
    {
        public Dictionary<long, AreaNode> nodes = new Dictionary<long, AreaNode>();
        public HashSet<AreaNode> startPoints = new HashSet<AreaNode>();

        // TODO: for now we're assuming no arbitrary intersections or multiple branching intersections - this is probably naive
        public SectorConstrainedOSMAreaGraph Add(SectorConstrainedOSMAreaGraph map, BlobCollection blobs)
        {
            map = map.Clone(); // now we're allowed to junk it
            // remember, counterclockwise makes an island
            List<AreaNode> doAdd = new List<AreaNode>();
            List<AreaNode> doDelete = new List<AreaNode>();
            List<AreaNode> singularDelete = new List<AreaNode>();
            foreach (var pair in map.nodes)
            {
                if (nodes.ContainsKey(pair.Key))
                {
                    AreaNode srcNode = nodes[pair.Key];
                    AreaNode mapNode = map.nodes[pair.Key];
                    Vector2d src1 = GetPos(srcNode.prev, blobs);
                    Vector2d src2 = GetPos(srcNode, blobs);
                    Vector2d src3 = GetPos(srcNode.next, blobs);
                    Vector2d new1 = GetPos(mapNode.prev, blobs);
                    Vector2d new2 = GetPos(mapNode, blobs);
                    Vector2d new3 = GetPos(mapNode.next, blobs);
                    Vector2d srcInto = src2 - src1;
                    Vector2d srcOutof = src3 - src2;
                    Vector2d newInto = new2 - new1;
                    Vector2d newOutof = new3 - new2;
                    AreaNode A = srcNode.prev;
                    AreaNode B = srcNode.next;
                    AreaNode C = mapNode.prev;
                    AreaNode D = mapNode.next;
                    double AtoBAngle = ComputeInnerAngle(srcInto, srcOutof);
                    double AtoCAngle = ComputeInnerAngle(srcInto, -newInto);
                    double AtoDAngle = ComputeInnerAngle(srcInto, newOutof);
                    bool ACSame = !srcNode.prev.IsEdge() && srcNode.prev.id == mapNode.prev.id;
                    bool BDSame = !srcNode.next.IsEdge() && srcNode.next.id == mapNode.next.id;
                    bool ADSame = !srcNode.prev.IsEdge() && srcNode.prev.id == mapNode.next.id;
                    bool BCSame = !srcNode.next.IsEdge() && srcNode.next.id == mapNode.prev.id;
                    // Note: I've drawn all these on pen & pad
                    // first, deal with super degenerate cases
                    if (ACSame && BDSame)
                    {
                    }
                    else if (ADSame && BCSame)
                    {
                        singularDelete.Add(srcNode);
                    } // now, slightly less degenerate
                    else if (ACSame)
                    {
                        if (AtoBAngle < AtoDAngle)
                        {
                            doDelete.Add(B);
                            doAdd.Add(D);
                            D.prev = srcNode;
                            srcNode.next = D;
                        }
                        else
                        {
                        }
                    }
                    else if (BDSame)
                    {
                        if (AtoBAngle < AtoCAngle)
                        {
                            doDelete.Add(A);
                            doAdd.Add(C);
                            C.next = srcNode;
                            srcNode.prev = C;
                        }
                        else
                        {
                        }
                    }
                    else if (ADSame)
                    {
                        if (AtoBAngle < AtoCAngle)
                        {
                            doDelete.Add(A);
                            doAdd.Add(C);
                            C.next = srcNode;
                            srcNode.prev = C;
                        }
                        else
                        {
                            doDelete.Add(A);
                            doDelete.Add(B);
                        }
                    }
                    else if (BCSame)
                    {
                        if (AtoBAngle < AtoDAngle)
                        {
                            doDelete.Add(B);
                            doAdd.Add(D);
                            D.prev = srcNode;
                            srcNode.next = D;
                        }
                        else
                        {
                            doDelete.Add(A);
                            doDelete.Add(B);
                        }
                    } // now, non-degenerate
                    else
                    {
                        if (AtoBAngle < AtoCAngle && AtoCAngle < AtoDAngle)
                        {
                            throw new NotImplementedException();
                        }
                        else if (AtoCAngle < AtoBAngle && AtoBAngle < AtoDAngle)
                        {
                            doDelete.Add(B);
                            doAdd.Add(D);
                            D.prev = srcNode;
                            srcNode.next = D;
                        }
                        else if (AtoCAngle < AtoDAngle && AtoDAngle < AtoBAngle)
                        {
                        }
                        else if (AtoBAngle < AtoDAngle && AtoDAngle < AtoCAngle)
                        {
                            doDelete.Add(B);
                            doAdd.Add(D);
                            D.prev = srcNode;
                            srcNode.next = D;

                            doDelete.Add(A);
                            doAdd.Add(C);
                            C.next = srcNode;
                            srcNode.prev = C;
                        }
                        else if (AtoDAngle < AtoBAngle && AtoBAngle < AtoCAngle)
                        {
                            doDelete.Add(A);
                            doAdd.Add(C);
                            C.next = srcNode;
                            srcNode.prev = C;
                        }
                        else if (AtoDAngle < AtoCAngle && AtoCAngle < AtoBAngle)
                        {
                            doDelete.Add(A);
                            doDelete.Add(B);
                        }
                    }
                }
            }
            // delete until you reach the next critical point
            foreach (var d in doDelete)
            {
                bool forwards = d.next != null && (!nodes.ContainsKey(d.next.id) || !map.nodes.ContainsKey(d.next.id));
                var temp = d;
                while (temp != null && (temp.IsEdge() || !nodes.ContainsKey(temp.id) || !map.nodes.ContainsKey(temp.id)))
                {
                    if (!nodes.ContainsKey(temp.id)) break;
                    if (temp.IsEdge())
                    {
                        if (!forwards) startPoints.Remove(temp);
                    }
                    else
                    {
                        nodes.Remove(temp.id);
                    }
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            // add until you reach the next critical point
            foreach (var d in doAdd)
            {
                bool forwards = d.next != null && (!nodes.ContainsKey(d.next.id) || !map.nodes.ContainsKey(d.next.id));
                var temp = d;
                while (temp != null && (temp.IsEdge() || !nodes.ContainsKey(temp.id) || !map.nodes.ContainsKey(temp.id)))
                {
                    if (nodes.ContainsKey(temp.id)) break;
                    if (temp.IsEdge())
                    {
                        if (!forwards) startPoints.Add(temp);
                    }
                    else
                    {
                        nodes[temp.id] = temp;
                    }
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            foreach (var d in singularDelete) nodes.Remove(d.id);
            return this;
        }

        public SectorConstrainedOSMAreaGraph Subtract(SectorConstrainedOSMAreaGraph map, BlobCollection blobs)
        {
            map = map.Clone(); // now we're allowed to junk it
            map.Reverse();
            // remember, counterclockwise makes an island
            List<AreaNode> doAdd = new List<AreaNode>();
            List<AreaNode> doDelete = new List<AreaNode>();
            List<AreaNode> singularDelete = new List<AreaNode>();
            foreach (var pair in map.nodes)
            {
                if (nodes.ContainsKey(pair.Key))
                {
                    AreaNode srcNode = nodes[pair.Key];
                    AreaNode mapNode = map.nodes[pair.Key];
                    Vector2d src1 = GetPos(srcNode.prev, blobs);
                    Vector2d src2 = GetPos(srcNode, blobs);
                    Vector2d src3 = GetPos(srcNode.next, blobs);
                    Vector2d new1 = GetPos(mapNode.prev, blobs);
                    Vector2d new2 = GetPos(mapNode, blobs);
                    Vector2d new3 = GetPos(mapNode.next, blobs);
                    Vector2d srcInto = src2 - src1;
                    Vector2d srcOutof = src3 - src2;
                    Vector2d newInto = new2 - new1;
                    Vector2d newOutof = new3 - new2;
                    AreaNode A = srcNode.prev;
                    AreaNode B = srcNode.next;
                    AreaNode C = mapNode.prev;
                    AreaNode D = mapNode.next;
                    double AtoBAngle = ComputeInnerAngle(srcInto, srcOutof);
                    double AtoCAngle = ComputeInnerAngle(srcInto, -newInto);
                    double AtoDAngle = ComputeInnerAngle(srcInto, newOutof);
                    bool ACSame = !srcNode.prev.IsEdge() && srcNode.prev.id == mapNode.prev.id;
                    bool BDSame = !srcNode.next.IsEdge() && srcNode.next.id == mapNode.next.id;
                    bool ADSame = !srcNode.prev.IsEdge() && srcNode.prev.id == mapNode.next.id;
                    bool BCSame = !srcNode.next.IsEdge() && srcNode.next.id == mapNode.prev.id;
                    // Note: I've drawn all these on pen & pad
                    // first, deal with super degenerate cases
                    if (ACSame && BDSame)
                    {
                    }
                    else if (ADSame && BCSame)
                    {
                        singularDelete.Add(srcNode);
                    } // now, slightly less degenerate
                    else if (ACSame)
                    {
                        if (AtoBAngle < AtoDAngle)
                        {
                        }
                        else
                        {
                            doDelete.Add(B);
                            doAdd.Add(D);
                            D.prev = srcNode;
                            srcNode.next = D;
                        }
                    }
                    else if (BDSame)
                    {
                        if (AtoBAngle < AtoCAngle)
                        {
                        }
                        else
                        {
                            doDelete.Add(A);
                            doAdd.Add(C);
                            C.next = srcNode;
                            srcNode.prev = C;
                        }
                    }
                    else if (ADSame)
                    {
                        if (AtoBAngle < AtoCAngle)
                        {
                            doDelete.Add(A);
                            doDelete.Add(B);
                        }
                        else
                        {
                            doDelete.Add(A);
                            doAdd.Add(C);
                            C.next = srcNode;
                            srcNode.prev = C;
                        }
                    }
                    else if (BCSame)
                    {
                        if (AtoBAngle < AtoDAngle)
                        {
                            doDelete.Add(A);
                            doDelete.Add(B);
                        }
                        else
                        {
                            doDelete.Add(B);
                            doAdd.Add(D);
                            D.prev = srcNode;
                            srcNode.next = D;
                        }
                    } // now, non-degenerate
                    else
                    {
                        if (AtoBAngle < AtoCAngle && AtoCAngle < AtoDAngle)
                        {
                            doDelete.Add(A);
                            doDelete.Add(B);
                        }
                        else if (AtoCAngle < AtoBAngle && AtoBAngle < AtoDAngle)
                        {
                            doDelete.Add(A);
                            doAdd.Add(C);
                            C.next = srcNode;
                            srcNode.prev = C;
                        }
                        else if (AtoCAngle < AtoDAngle && AtoDAngle < AtoBAngle)
                        {
                            throw new NotImplementedException();
                        }
                        else if (AtoBAngle < AtoDAngle && AtoDAngle < AtoCAngle)
                        {
                        }
                        else if (AtoDAngle < AtoBAngle && AtoBAngle < AtoCAngle)
                        {
                            doDelete.Add(B);
                            doAdd.Add(D);
                            D.prev = srcNode;
                            srcNode.next = D;
                        }
                        else if (AtoDAngle < AtoCAngle && AtoCAngle < AtoBAngle)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }
            // delete until you reach the next critical point
            foreach (var d in doDelete)
            {
                bool forwards = d.next != null && (!nodes.ContainsKey(d.next.id) || !map.nodes.ContainsKey(d.next.id));
                var temp = d;
                while (temp != null && (temp.IsEdge() || !nodes.ContainsKey(temp.id) || !map.nodes.ContainsKey(temp.id)))
                {
                    if (!nodes.ContainsKey(temp.id)) break;
                    if (temp.IsEdge())
                    {
                        if (!forwards) startPoints.Remove(temp);
                    }
                    else
                    {
                        nodes.Remove(temp.id);
                    }
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            // add until you reach the next critical point
            foreach (var d in doAdd)
            {
                bool forwards = d.next != null && (!nodes.ContainsKey(d.next.id) || !map.nodes.ContainsKey(d.next.id));
                var temp = d;
                while (temp != null && (temp.IsEdge() || !nodes.ContainsKey(temp.id) || !map.nodes.ContainsKey(temp.id)))
                {
                    if (nodes.ContainsKey(temp.id)) break;
                    if (temp.IsEdge())
                    {
                        if (!forwards) startPoints.Add(temp);
                    }
                    else
                    {
                        nodes[temp.id] = temp;
                    }
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            foreach (var d in singularDelete) nodes.Remove(d.id);
            return this;
        }

        private void Reverse()
        {
            startPoints = new HashSet<AreaNode>();
            foreach (var node in nodes.Values)
            {
                var temp = node.next;
                node.next = node.prev;
                node.prev = temp;
                if (node.prev.IsEdge()) startPoints.Add(node.prev);
            }
        }

        private static double ComputeInnerAngle(Vector2d v1, Vector2d v2)
        {
            // ex: we'd expect a square island to have 4 inner angles of pi/2
            // ex: we'd expect a square pond to have 4 inner angles of 3pi/2
            // we're returning this according to our unusual coordinate system
            double cos = (v1.X * v2.X + v1.Y * v2.Y) / v1.Length() / v2.Length();
            double sin = (v2.X * v1.Y - v2.Y * v1.X) / v1.Length() / v2.Length();
            return Math.Atan2(-sin, cos) + Math.PI;
        }

        public SectorConstrainedOSMAreaGraph Clone()
        {
            SectorConstrainedOSMAreaGraph map = new SectorConstrainedOSMAreaGraph();
            foreach (var node in nodes.Values)
            {
                map.nodes[node.id] = new AreaNode() { id = node.id };
            }
            foreach (var node in nodes.Values)
            {
                if (node.prev.IsEdge())
                {
                    map.nodes[node.id].prev = new AreaNode() { v = node.prev.v, next = map.nodes[node.id] }; // TODO: make vector2d a struct?
                    map.startPoints.Add(map.nodes[node.id].prev);
                }
                else
                {
                    map.nodes[node.id].prev = map.nodes[node.prev.id];
                }
                if (node.next.IsEdge())
                {
                    map.nodes[node.id].next = new AreaNode() { v = node.next.v, prev = map.nodes[node.id] }; // TODO: make vector2d a struct?
                }
                else
                {
                    map.nodes[node.id].next = map.nodes[node.next.id];
                }
            }
            return map;
        }

        private Vector2d GetPos(AreaNode node, BlobCollection blobs)
        {
            return node.IsEdge() ? node.v : blobs.nodes[node.id];
        }

        public SectorConstrainedAreaMap Finalize(BlobCollection blobs)
        {
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            SectorConstrainedAreaMap map = new SectorConstrainedAreaMap();
            foreach (var startPoint in startPoints)
            {
                List<Vector2d> newPath = new List<Vector2d>();
                AreaNode curr = startPoint;
                while (true)
                {
                    if (curr.v == null)
                    {
                        newPath.Add(blobs.nodes[curr.id]);
                    }
                    else
                    {
                        newPath.Add(curr.v);
                    }
                    explored.Add(curr);
                    if (curr.next == null) break;
                    curr = curr.next;
                }
                map.paths.Add(newPath);
            }
            foreach (var node in nodes.Values)
            {
                if (explored.Contains(node)) continue;
                // just loops can be found at this point
                List<Vector2d> newLoop = new List<Vector2d>();
                AreaNode curr = node;
                while (true)
                {
                    newLoop.Add(blobs.nodes[curr.id]);
                    explored.Add(curr);
                    if (curr.next == node) break;
                    curr = curr.next;
                }
                newLoop.Add(blobs.nodes[node.id]); // finish off the loop;
                if (ApproximateCW(newLoop))
                {
                    map.inners.Add(newLoop);
                }
                else
                {
                    map.outers.Add(newLoop);
                }
            }
            return map;
        }

        public double Area(BlobCollection blobs)
        {
            double area = 0;
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            foreach (var startPoint in startPoints)
            {
                List<Vector2d> newPath = new List<Vector2d>();
                AreaNode curr = startPoint;
                while (true)
                {
                    if (curr.v == null)
                    {
                        newPath.Add(blobs.nodes[curr.id]);
                    }
                    else
                    {
                        newPath.Add(curr.v);
                    }
                    explored.Add(curr);
                    if (curr.next == null) break;
                    curr = curr.next;
                }
                area += AreaOf(newPath); // TODO: this is fake area, but sure
            }
            foreach (var node in nodes.Values)
            {
                if (explored.Contains(node)) continue;
                // just loops can be found at this point
                List<Vector2d> newLoop = new List<Vector2d>();
                AreaNode curr = node;
                while (true)
                {
                    newLoop.Add(blobs.nodes[curr.id]);
                    explored.Add(curr);
                    if (curr.next == node) break;
                    curr = curr.next;
                }
                newLoop.Add(blobs.nodes[node.id]); // finish off the loop;
                area += AreaOf(newLoop);
            }
            return area;
        }

        private static bool ApproximateCW(List<Vector2d> loop)
        {
            return AreaOf(loop) < 0; // based on the coordinate system we're using, with X right and Y down
        }

        private static double AreaOf(List<Vector2d> loop)
        {
            double area = 0;
            // calculate that area
            Vector2d basePoint = loop.First();
            for (int i = 1; i < loop.Count; i++)
            {
                Vector2d prev = loop[i - 1];
                Vector2d next = loop[i];
                Vector2d line1 = prev - basePoint;
                Vector2d line2 = next - prev;
                area += (line2.X * line1.Y - line2.Y * line1.X) / 2; // random cross-product logic
            }
            // based on the coordinate system we're using, with X right and Y down
            return area;
        }
    }

    public class AreaNode
    {
        public AreaNode next;
        public AreaNode prev;
        public long id = -1; // -1 when at edge
        public Vector2d v = null; // null when has an id
        public bool IsEdge() { return v != null; }
    }
}