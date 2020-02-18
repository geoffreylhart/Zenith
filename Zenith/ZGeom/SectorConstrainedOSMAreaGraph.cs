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
        public Dictionary<long, List<AreaNode>> nodes = new Dictionary<long, List<AreaNode>>();
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
            List<long> singularDelete = new List<long>();
            foreach (var pair in map.nodes)
            {
                if (nodes.ContainsKey(pair.Key))
                {
                    List<AreaNode> srcInitialLines = new List<AreaNode>();
                    List<AreaNode> mapInitialLines = new List<AreaNode>();
                    List<AreaNode> initialLines = new List<AreaNode>();
                    foreach (var n in nodes[pair.Key])
                    {
                        srcInitialLines.Add(n.prev);
                        srcInitialLines.Add(n.next);
                    }
                    foreach (var n in map.nodes[pair.Key])
                    {
                        mapInitialLines.Add(n.prev);
                        mapInitialLines.Add(n.next);
                    }
                    initialLines.AddRange(srcInitialLines);
                    initialLines.AddRange(mapInitialLines);
                    List<AreaNode> finalLines = initialLines.ToList();
                    // sort clockwise
                    finalLines = finalLines.OrderBy(x => ComputeInnerAngle(new Vector2d(1, 0), (x.prev.id == pair.Key) ? (GetPos(x, blobs) - blobs.nodes[pair.Key]) : (blobs.nodes[pair.Key] - GetPos(x, blobs)))).ToList();
                    bool[] sectors = new bool[finalLines.Count]; // first sector means the area just ccw of the first finalLine
                    foreach (var n in srcInitialLines)
                    {
                        int from = finalLines.IndexOf(n.prev);
                        int to = finalLines.IndexOf(n.next);
                        if (to <= from) to += finalLines.Count;
                        for (int i = from + 1; i <= to; i++)
                        {
                            sectors[i % sectors.Length] = true;
                        }
                    }
                    foreach (var n in mapInitialLines)
                    {
                        int from = finalLines.IndexOf(n.prev);
                        int to = finalLines.IndexOf(n.next);
                        if (to <= from) to += finalLines.Count;
                        for (int i = from + 1; i <= to; i++)
                        {
                            sectors[i % sectors.Length] = true;
                        }
                    }
                    // make sure zero-width areas match one of their neighbors so they get cleaned up
                    bool[] zeroWidth = new bool[sectors.Length];
                    for (int i = 0; i < sectors.Length; i++)
                    {
                        AreaNode p = finalLines[(i - 1 + finalLines.Count) % finalLines.Count];
                        AreaNode n = finalLines[i];
                        if (p.prev.id == pair.Key ? (p.next.id == n.prev.id) : (p.prev.id == n.next.id))
                        {
                            zeroWidth[i] = true;
                        }
                    }
                    int start = zeroWidth.ToList().IndexOf(false);
                    if (start < 0) throw new NotImplementedException();
                    for (int i = 0; i < zeroWidth.Length; i++)
                    {
                        if (zeroWidth[(i + start) % zeroWidth.Length])
                        {
                            sectors[(i + start) % sectors.Length] = sectors[(i - 1 + start) % sectors.Length];
                        }
                    }
                    List<int> remove = new List<int>();
                    for (int i = 0; i < finalLines.Count; i++)
                    {
                        // i and i + 1 surround this line
                        // get rid of any line that doesn't act as a border
                        if (sectors[i] == sectors[(i + 1) % sectors.Length])
                        {
                            remove.Add(i);
                        }
                    }
                    remove.Reverse();
                    foreach (var i in remove) finalLines.RemoveAt(i);
                    // we should only have an even number of things in finalLine now
                    if (finalLines.Count % 2 != 0) throw new NotImplementedException();
                    // let's make sure the first one points in
                    if (finalLines.Count > 0)
                    {
                        if (finalLines.First().next.id != pair.Key)
                        {
                            finalLines.Add(finalLines.First());
                            finalLines.RemoveAt(0);
                        }
                    }
                    foreach (var line in srcInitialLines)
                    {
                        if (!finalLines.Contains(line)) doDelete.Add(line);
                    }
                    foreach (var line in mapInitialLines)
                    {
                        if (finalLines.Contains(line)) doAdd.Add(line);
                    }
                    List<AreaNode> newSrcNodes = new List<AreaNode>();
                    for (int i = 0; i < finalLines.Count % 2; i++)
                    {
                        AreaNode into = finalLines[i * 2];
                        AreaNode outof = finalLines[i * 2 + 1];
                        AreaNode newSrcNode = new AreaNode() { id = pair.Key };
                        into.next = newSrcNode;
                        outof.prev = newSrcNode;
                        newSrcNode.prev = into;
                        newSrcNode.next = outof;
                        newSrcNodes.Add(newSrcNode);
                    }
                    if (newSrcNodes.Count > 0)
                    {
                        nodes[pair.Key] = newSrcNodes;
                    }
                    else
                    {
                        singularDelete.Add(pair.Key);
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
                        nodes[temp.id] = new List<AreaNode>() { temp };
                    }
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            foreach (var d in singularDelete) nodes.Remove(d);
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
            List<long> singularDelete = new List<long>();
            foreach (var pair in map.nodes)
            {
                if (nodes.ContainsKey(pair.Key))
                {
                    List<AreaNode> srcInitialLines = new List<AreaNode>();
                    List<AreaNode> mapInitialLines = new List<AreaNode>();
                    List<AreaNode> initialLines = new List<AreaNode>();
                    foreach (var n in nodes[pair.Key])
                    {
                        srcInitialLines.Add(n.prev);
                        srcInitialLines.Add(n.next);
                    }
                    foreach (var n in map.nodes[pair.Key])
                    {
                        mapInitialLines.Add(n.prev);
                        mapInitialLines.Add(n.next);
                    }
                    initialLines.AddRange(srcInitialLines);
                    initialLines.AddRange(mapInitialLines);
                    List<AreaNode> finalLines = initialLines.ToList();
                    // sort clockwise
                    finalLines = finalLines.OrderBy(x => ComputeInnerAngle(new Vector2d(1, 0), (x.prev.id == pair.Key) ? (GetPos(x, blobs) - blobs.nodes[pair.Key]) : (blobs.nodes[pair.Key] - GetPos(x, blobs)))).ToList();
                    bool[] sectors = new bool[finalLines.Count]; // first sector means the area just ccw of the first finalLine
                    foreach (var n in srcInitialLines)
                    {
                        int from = finalLines.IndexOf(n.prev);
                        int to = finalLines.IndexOf(n.next);
                        if (to <= from) to += finalLines.Count;
                        for (int i = from + 1; i <= to; i++)
                        {
                            sectors[i % sectors.Length] = true;
                        }
                    }
                    foreach (var n in mapInitialLines)
                    {
                        int from = finalLines.IndexOf(n.prev);
                        int to = finalLines.IndexOf(n.next);
                        if (to <= from) to += finalLines.Count;
                        for (int i = from + 1; i <= to; i++)
                        {
                            sectors[i % sectors.Length] = false; // this is the only difference for a subtraction, as it should be
                        }
                    }
                    // make sure zero-width areas match one of their neighbors so they get cleaned up
                    bool[] zeroWidth = new bool[sectors.Length];
                    for (int i = 0; i < sectors.Length; i++)
                    {
                        AreaNode p = finalLines[(i - 1 + finalLines.Count) % finalLines.Count];
                        AreaNode n = finalLines[i];
                        if (p.prev.id == pair.Key ? (p.next.id == n.prev.id) : (p.prev.id == n.next.id))
                        {
                            zeroWidth[i] = true;
                        }
                    }
                    int start = zeroWidth.ToList().IndexOf(false);
                    if (start < 0) throw new NotImplementedException();
                    for (int i = 0; i < zeroWidth.Length; i++)
                    {
                        if (zeroWidth[(i + start) % zeroWidth.Length])
                        {
                            sectors[(i + start) % sectors.Length] = sectors[(i - 1 + start) % sectors.Length];
                        }
                    }
                    List<int> remove = new List<int>();
                    for (int i = 0; i < finalLines.Count; i++)
                    {
                        // i and i + 1 surround this line
                        // get rid of any line that doesn't act as a border
                        if (sectors[i] == sectors[(i + 1) % sectors.Length])
                        {
                            remove.Add(i);
                        }
                    }
                    remove.Reverse();
                    foreach (var i in remove) finalLines.RemoveAt(i);
                    // we should only have an even number of things in finalLine now
                    if (finalLines.Count % 2 != 0) throw new NotImplementedException();
                    // let's make sure the first one points in
                    if (finalLines.Count > 0)
                    {
                        if (finalLines.First().next.id != pair.Key)
                        {
                            finalLines.Add(finalLines.First());
                            finalLines.RemoveAt(0);
                        }
                    }
                    foreach (var line in srcInitialLines)
                    {
                        if (!finalLines.Contains(line)) doDelete.Add(line);
                    }
                    foreach (var line in mapInitialLines)
                    {
                        if (finalLines.Contains(line)) doAdd.Add(line);
                    }
                    List<AreaNode> newSrcNodes = new List<AreaNode>();
                    for (int i = 0; i < finalLines.Count % 2; i++)
                    {
                        AreaNode into = finalLines[i * 2];
                        AreaNode outof = finalLines[i * 2 + 1];
                        AreaNode newSrcNode = new AreaNode() { id = pair.Key };
                        into.next = newSrcNode;
                        outof.prev = newSrcNode;
                        newSrcNode.prev = into;
                        newSrcNode.next = outof;
                        newSrcNodes.Add(newSrcNode);
                    }
                    if (newSrcNodes.Count > 0)
                    {
                        nodes[pair.Key] = newSrcNodes;
                    }
                    else
                    {
                        singularDelete.Add(pair.Key);
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
                        nodes[temp.id] = new List<AreaNode>() { temp };
                    }
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            foreach (var d in singularDelete) nodes.Remove(d);
            return this;
        }

        // TODO: speedup later, for now use naive n^2 search
        // note, actual intersections should be exceedingly rare, like 1 in 6 sectors or something
        private void DoIntersections(SectorConstrainedOSMAreaGraph map, BlobCollection blobs)
        {
            var nodes1 = nodes.Values.Where(x => x.Count == 1).Select(x => x.Single()).ToList();
            nodes1.AddRange(startPoints);
            var nodes2 = map.nodes.Values.Where(x => x.Count == 1).Select(x => x.Single()).ToList();
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
                            nodes[randID] = new List<AreaNode>() { newNode1 };
                            newNode1.prev = n1;
                            newNode1.next = n1.next;
                            n1.next.prev = newNode1;
                            n1.next = newNode1;

                            map.nodes[randID] = new List<AreaNode>() { newNode2 };
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

        // TODO: apparently we've been adding loops twice this entire time, basically
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
            foreach (var nodeList in map.nodes.Values)
            {
                foreach (var node in nodeList)
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
                        foreach (var n in newLoop)
                        {
                            if (!nodes.ContainsKey(n.id)) nodes[n.id] = new List<AreaNode>();
                            nodes[n.id].Add(n);
                        }
                    }
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
            foreach (var nodeList in nodes.Values)
            {
                foreach (var node in nodeList)
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
        }

        private void Reverse()
        {
            // fully reverse, even the bad parts
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            Queue<AreaNode> bfs = new Queue<AreaNode>();
            foreach (var startPoint in startPoints) bfs.Enqueue(startPoint);
            foreach (var nodeList in nodes.Values)
            {
                foreach (var node in nodeList) bfs.Enqueue(node);
            }
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
            foreach (var nodeList in nodes.Values)
            {
                foreach (var node in nodeList) bfs.Enqueue(node);
            }
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
                map.nodes[pair.Key] = pair.Value.Select(x => mapper[x]).ToList();
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
            foreach (var nodeList in nodes.Values)
            {
                foreach (var node in nodeList)
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
            foreach (var nodeList in nodes.Values)
            {
                foreach (var node in nodeList)
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