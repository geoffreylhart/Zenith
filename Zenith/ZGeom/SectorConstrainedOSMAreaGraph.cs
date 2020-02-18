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
            DoIntersections(map, blobs);
            map = map.Clone(); // now we're allowed to junk it
            DoLoops(map, blobs);
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
                            singularDelete.Add(srcNode);
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
                            singularDelete.Add(srcNode);
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
                            singularDelete.Add(srcNode);
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
            DoIntersections(map, blobs);
            map = map.Clone(); // now we're allowed to junk it
            map.Reverse();
            DoLoops(map, blobs);
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
                            singularDelete.Add(srcNode);
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
                            singularDelete.Add(srcNode);
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
                            singularDelete.Add(srcNode);
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
                            doDelete.Add(A);
                            doAdd.Add(C);
                            C.next = srcNode;
                            srcNode.prev = C;

                            doDelete.Add(B);
                            doAdd.Add(D);
                            D.prev = srcNode;
                            srcNode.next = D;
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

        // TODO: speedup later, for now use naive n^2 search
        // note, actual intersections should be exceedingly rare, like 1 in 6 sectors or something
        private void DoIntersections(SectorConstrainedOSMAreaGraph map, BlobCollection blobs)
        {
            var nodes1 = nodes.Values.ToList();
            nodes1.AddRange(startPoints);
            var nodes2 = map.nodes.Values.ToList();
            nodes2.AddRange(map.startPoints);
            foreach (var n1 in nodes1)
            {
                foreach (var n2 in nodes2)
                {
                    Vector2d A = GetPos(n1, blobs);
                    Vector2d B = GetPos(n1.next, blobs);
                    Vector2d C = GetPos(n2, blobs);
                    Vector2d D = GetPos(n2.next, blobs);
                    long randID = -(n1.id ^ n2.id); // TODO: get rid of hack
                    // TODO: we're going to treat -1 as always matching for now
                    bool ACSame = n1.id == n2.id;
                    bool ADSame = n1.id == n2.next.id;
                    bool BCSame = n1.next.id == n2.id;
                    bool BDSame = n1.next.id == n2.next.id;
                    // a subset of possible tiny angles that can cause rounding errors
                    // only thing that changes between these is the condition, line direction, and the newpoint id
                    // TODO: is this really what fixed the nonsense at 240202043? the angleDiff was only 0.009, which seems too big to cause an issue
                    if (ACSame && !ADSame && !BCSame && !BDSame)
                    {
                        Vector2d line1 = B - A;
                        Vector2d line2 = D - C;
                        double angleDiff = Math.Atan2(line1.Y, line1.X) - Math.Atan2(line2.Y, line2.X);
                        angleDiff = (angleDiff + 2 * Math.PI) % (2 * Math.PI);
                        if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;
                        if (angleDiff < 0.01)
                        {
                            if (line1.Length() < line2.Length())
                            {
                                n2.next.prev = new AreaNode { id = n1.next.id, prev = n2, next = n2.next };
                                n2.next = n2.next.prev;
                            }
                            else
                            {
                                n1.next.prev = new AreaNode { id = n2.next.id, prev = n1, next = n1.next };
                                n1.next = n1.next.prev;
                            }
                        }
                    }
                    if (!ACSame && ADSame && !BCSame && !BDSame)
                    {
                        Vector2d line1 = B - A;
                        Vector2d line2 = C - D;
                        double angleDiff = Math.Atan2(line1.Y, line1.X) - Math.Atan2(line2.Y, line2.X);
                        angleDiff = (angleDiff + 2 * Math.PI) % (2 * Math.PI);
                        if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;
                        if (angleDiff < 0.01)
                        {
                            if (line1.Length() < line2.Length())
                            {
                                n2.next.prev = new AreaNode { id = n1.next.id, prev = n2, next = n2.next };
                                n2.next = n2.next.prev;
                            }
                            else
                            {
                                n1.next.prev = new AreaNode { id = n2.id, prev = n1, next = n1.next };
                                n1.next = n1.next.prev;
                            }
                        }
                    }
                    if (!ACSame && !ADSame && BCSame && !BDSame)
                    {
                        Vector2d line1 = A - B;
                        Vector2d line2 = D - C;
                        double angleDiff = Math.Atan2(line1.Y, line1.X) - Math.Atan2(line2.Y, line2.X);
                        angleDiff = (angleDiff + 2 * Math.PI) % (2 * Math.PI);
                        if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;
                        if (angleDiff < 0.01)
                        {
                            if (line1.Length() < line2.Length())
                            {
                                n2.next.prev = new AreaNode { id = n1.id, prev = n2, next = n2.next };
                                n2.next = n2.next.prev;
                            }
                            else
                            {
                                n1.next.prev = new AreaNode { id = n2.next.id, prev = n1, next = n1.next };
                                n1.next = n1.next.prev;
                            }
                        }
                    }
                    if (!ACSame && !ADSame && !BCSame && BDSame)
                    {
                        Vector2d line1 = A - B;
                        Vector2d line2 = C - D;
                        double angleDiff = Math.Atan2(line1.Y, line1.X) - Math.Atan2(line2.Y, line2.X);
                        angleDiff = (angleDiff + 2 * Math.PI) % (2 * Math.PI);
                        if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;
                        if (angleDiff < 0.01)
                        {
                            if (line1.Length() < line2.Length())
                            {
                                n2.next.prev = new AreaNode { id = n1.id, prev = n2, next = n2.next };
                                n2.next = n2.next.prev;
                            }
                            else
                            {
                                n1.next.prev = new AreaNode { id = n2.id, prev = n1, next = n1.next };
                                n1.next = n1.next.prev;
                            }
                        }
                    }
                    if (!ACSame && !ADSame && !BCSame && !BDSame) // proper intersection
                    {
                        Vector2d intersection = Intersect(A, B, C, D);
                        if (intersection != null)
                        {
                            AreaNode newNode1 = new AreaNode() { id = randID };
                            AreaNode newNode2 = new AreaNode() { id = randID };
                            blobs.nodes[randID] = intersection;
                            nodes[randID] = newNode1;
                            newNode1.prev = n1;
                            newNode1.next = n1.next;
                            n1.next.prev = newNode1;
                            n1.next = newNode1;

                            map.nodes[randID] = newNode2;
                            newNode2.prev = n2;
                            newNode2.next = n2.next;
                            n2.next.prev = newNode2;
                            n2.next = newNode2;
                        }
                    }
                }
            }
        }

        private Vector2d Intersect(Vector2d a, Vector2d b, Vector2d c, Vector2d d)
        {
            // copied from wiki, sure
            double t = ((a.X - c.X) * (c.Y - d.Y) - (a.Y - c.Y) * (c.X - d.X)) / ((a.X - b.X) * (c.Y - d.Y) - (a.Y - b.Y) * (c.X - d.X));
            double u = -((a.X - b.X) * (a.Y - c.Y) - (a.Y - b.Y) * (a.X - c.X)) / ((a.X - b.X) * (c.Y - d.Y) - (a.Y - b.Y) * (c.X - d.X));
            if (t < 0 || t > 1 || u < 0 || u > 1) return null;
            return a + (b - a) * t;
        }

        private void DoLoops(SectorConstrainedOSMAreaGraph map, BlobCollection blobs)
        {
            // first, find those loops
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            foreach (var startPoint in map.startPoints)
            {
                AreaNode curr = startPoint;
                while (true)
                {
                    explored.Add(curr);
                    if (curr.next == null) break;
                    curr = curr.next;
                }
            }
            foreach (var node in map.nodes.Values)
            {
                if (explored.Contains(node)) continue;
                // just loops can be found at this point
                List<AreaNode> newLoop = new List<AreaNode>();
                bool loopHasConnections = false;
                AreaNode curr = node;
                while (true)
                {
                    if (nodes.ContainsKey(curr.id)) loopHasConnections = true;
                    newLoop.Add(curr);
                    explored.Add(curr);
                    if (curr.next == node) break;
                    curr = curr.next;
                }
                if (!loopHasConnections)
                {
                    // TODO: use winding rule
                    foreach (var n in newLoop) nodes[n.id] = n;
                }
            }
        }

        public void CheckValid()
        {
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            foreach (var startPoint in startPoints)
            {
                if (startPoint.prev != null || startPoint.next == null || startPoint.id != -1 || startPoint.v == null) throw new NotImplementedException();
                AreaNode curr = startPoint.next;
                while (true)
                {
                    explored.Add(curr);
                    if (curr.next == null) // endpoint
                    {
                        if (curr.next != null || curr.prev == null || curr.id != -1 || curr.v == null || curr.prev.next != curr) throw new NotImplementedException();
                        break;
                    }
                    else
                    {
                        if (curr.id == -1 || curr.v != null || curr.next == null || curr.prev == null || curr.prev.next != curr || curr.next.prev != curr) throw new NotImplementedException();
                    }
                    curr = curr.next;
                }
            }
            foreach (var node in nodes.Values)
            {
                if (explored.Contains(node)) continue;
                // just loops can be found at this point
                AreaNode curr = node;
                while (true)
                {
                    if (curr.id == -1 || curr.v != null || curr.next == null || curr.prev == null || curr.prev.next != curr || curr.next.prev != curr) throw new NotImplementedException();
                    explored.Add(curr);
                    if (curr.next == node) break;
                    curr = curr.next;
                }
            }
        }

        private void Reverse()
        {
            // fully reverse, even the bad parts
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            Queue<AreaNode> bfs = new Queue<AreaNode>();
            foreach (var startPoint in startPoints) bfs.Enqueue(startPoint);
            foreach (var node in nodes.Values) bfs.Enqueue(node);
            while (bfs.Count > 0)
            {
                var head = bfs.Dequeue();
                if (explored.Contains(head)) continue;
                explored.Add(head);
                if (head.next != null) bfs.Enqueue(head.next);
                if (head.prev != null) bfs.Enqueue(head.prev);
            }
            foreach (var x in explored)
            {
                var temp = x.next;
                x.next = x.prev;
                x.prev = temp;
            }
            startPoints = new HashSet<AreaNode>();
            foreach (var start in explored.Where(x => x.prev == null)) startPoints.Add(start);
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
            CheckValid();
            // TODO: make vector2d a struct?
            SectorConstrainedOSMAreaGraph map = new SectorConstrainedOSMAreaGraph();
            Dictionary<AreaNode, AreaNode> mapper = new Dictionary<AreaNode, AreaNode>();
            // fully clone, even the bad parts
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            Queue<AreaNode> bfs = new Queue<AreaNode>();
            foreach (var startPoint in startPoints) bfs.Enqueue(startPoint);
            foreach (var node in nodes.Values) bfs.Enqueue(node);
            while (bfs.Count > 0)
            {
                var head = bfs.Dequeue();
                if (explored.Contains(head)) continue;
                explored.Add(head);
                if (head.next != null) bfs.Enqueue(head.next);
                if (head.prev != null) bfs.Enqueue(head.prev);
            }
            foreach (var x in explored)
            {
                mapper[x] = new AreaNode() { id = x.id, v = x.v };
            }
            foreach (var x in explored)
            {
                mapper[x].prev = x.prev == null ? null : mapper[x.prev];
                mapper[x].next = x.next == null ? null : mapper[x.next];
            }
            foreach (var startPoint in startPoints)
            {
                map.startPoints.Add(mapper[startPoint]);
            }
            foreach (var pair in nodes)
            {
                map.nodes[pair.Key] = mapper[pair.Value];
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
                    if (curr.next == null || curr.next.prev != curr || curr.prev == null || curr.prev.next != curr) throw new NotImplementedException(); // validity check
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