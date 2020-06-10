using NetTopologySuite.Index.Strtree;
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
        static bool PERTURB_AT_JOINTS = false; // ex: a triforce is currently alway 3 shapes, but this would make sure they don't overlap
        static double PERTURB_AMOUNT = 0;

        // TODO: that thing going on near way 553880275, node 5534127050 still doesn't look resolved
        public Dictionary<long, List<AreaNode>> nodes = new Dictionary<long, List<AreaNode>>();

        // TODO: for now we're assuming no arbitrary intersections or multiple branching intersections - this is probably naive
        public SectorConstrainedOSMAreaGraph Add(SectorConstrainedOSMAreaGraph map, BlobCollection blobs)
        {
            map = map.Clone(); // now we're allowed to junk it
            var loopNodes = DoLoops(map, blobs);
            // remember, counterclockwise makes an island
            List<AreaNode> doAdd = new List<AreaNode>();
            List<AreaNode> doDelete = new List<AreaNode>();
            List<long> singularDelete = new List<long>();
            HashSet<long> criticalPoints = new HashSet<long>();
            foreach (var pair in map.nodes)
            {
                if (nodes.ContainsKey(pair.Key))
                {
                    criticalPoints.Add(pair.Key);
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
                    finalLines = finalLines.OrderBy(x => ComputeInnerAngle(new Vector2d(1, 0), GetPos(x, blobs) - blobs.nodes[pair.Key])).ToList();
                    bool[] sectors = new bool[finalLines.Count]; // first sector means the area just ccw of the first finalLine
                    // NOTE: this logic only works assuming multiple polygons are disjoint at intersections
                    foreach (var n in nodes[pair.Key])
                    {
                        int from = finalLines.IndexOf(n.prev);
                        int to = finalLines.IndexOf(n.next);
                        if (to <= from) to += finalLines.Count;
                        for (int i = from + 1; i <= to; i++)
                        {
                            sectors[i % sectors.Length] = true;
                        }
                    }
                    foreach (var n in map.nodes[pair.Key])
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
                    // correction: make sure zero-width somehow prioritize getting rid of new map lines like our original code
                    // turns out, even in our first test case, being inconsistent with this is just wrong (to see, treat this test case as one of two non-degenerate cases)
                    int[] makeMatch = new int[sectors.Length]; // -1 means, make match left, etc. TODO: I'm really winging it here, no clue if this makes any sense at all (basically, just test it!)
                    for (int i = 0; i < sectors.Length; i++)
                    {
                        AreaNode p = finalLines[(i - 1 + finalLines.Count) % finalLines.Count];
                        AreaNode n = finalLines[i];
                        if (p.id == n.id)
                        {
                            makeMatch[i] = mapInitialLines.Contains(p) ? -1 : 1; // make sector before this match this one to eliminate p
                        }
                    }
                    if (!makeMatch.Contains(0)) throw new NotImplementedException();
                    for (int i = 0; i < makeMatch.Length; i++)
                    {
                        HashSet<int> explored = new HashSet<int>();
                        int curr = i;
                        while (true)
                        {
                            if (makeMatch[curr] == 0)
                            {
                                sectors[i] = sectors[curr];
                                break;
                            }
                            if (explored.Contains(curr)) throw new NotImplementedException(); // some kind of rare loop
                            explored.Add(curr);
                            curr = (curr + makeMatch[curr] + makeMatch.Length) % makeMatch.Length;
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
                        if (finalLines.First().next == null || finalLines.First().next.id != pair.Key)
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
                    for (int i = 0; i < finalLines.Count / 2; i++)
                    {
                        AreaNode into = finalLines[i * 2];
                        AreaNode outof = finalLines[i * 2 + 1];
                        AreaNode newSrcNode;
                        if (i == 0) // mimic our original code for debugging?
                        {
                            newSrcNode = nodes[pair.Key].First();
                        }
                        else
                        {
                            newSrcNode = new AreaNode() { id = pair.Key };
                        }
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
                bool forwards = d.next != null && (!criticalPoints.Contains(d.next.id));
                var temp = d;
                while (temp != null && !criticalPoints.Contains(temp.id))
                {
                    if (!ContainsNode(temp)) break;
                    nodes[temp.id].Remove(temp);
                    if (nodes[temp.id].Count == 0) nodes.Remove(temp.id);
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            // add until you reach the next critical point
            foreach (var d in doAdd)
            {
                bool forwards = d.next != null && (!criticalPoints.Contains(d.next.id));
                var temp = d;
                while (temp != null && !criticalPoints.Contains(temp.id))
                {
                    if (ContainsNode(temp)) break;
                    if (!nodes.ContainsKey(temp.id)) nodes[temp.id] = new List<AreaNode>();
                    nodes[temp.id].Add(temp);
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            foreach (var d in singularDelete) nodes.Remove(d);
            foreach (var n in loopNodes)
            {
                if (!nodes.ContainsKey(n.id)) nodes[n.id] = new List<AreaNode>();
                nodes[n.id].Add(n);
            }
            return this;
        }

        // aka, zero-width areas
        // also, the sections of multipolygons with multiple way sections (not supposed to be allowed, but ex: relation 534928 ie Mississippi River)
        internal void RemoveDuplicateLines()
        {
            List<long> removeAltogether = new List<long>();
            List<AreaNode> remove = new List<AreaNode>();
            List<AreaNode> newNodes = new List<AreaNode>();
            foreach (var node in nodes)
            {
                // TODO: for now, lets individually fix each possible case
                for (int i = 0; i < node.Value.Count; i++)
                {
                    var matches = node.Value.Take(i + 1).Where(x => (x.prev == null ? node.Value[i].next == null : node.Value[i].next != null && x.prev.id == node.Value[i].next.id) || (x.next == null ? node.Value[i].prev == null : node.Value[i].prev != null && x.next.id == node.Value[i].prev.id));
                    if (matches.Count() > 1) throw new NotImplementedException(); // seriously?
                    if (matches.Count() == 1)
                    {
                        var v1 = node.Value[i];
                        var v2 = matches.Single();
                        if ((v1.prev == null ? v2.next == null : v2.next != null && v1.prev.id == v2.next.id) && (v1.next == null ? v2.prev == null : v2.prev != null && v1.next.id == v2.prev.id))
                        {
                            removeAltogether.Add(node.Key);
                        }
                        else
                        {
                            remove.Add(v1);
                            remove.Add(v2);
                            AreaNode newNode;
                            if (v1.prev.id == v2.next.id)
                            {
                                newNode = new AreaNode() { id = node.Key, next = v1.next, prev = v2.prev };
                            }
                            else
                            {
                                newNode = new AreaNode() { id = node.Key, prev = v1.prev, next = v2.next };
                            }
                            newNodes.Add(newNode);
                        }
                    }
                }
            }
            // do the actual graph edits outside the loop to make debugging easier
            foreach (var r in removeAltogether)
            {
                nodes.Remove(r);
            }
            foreach (var r in remove)
            {
                nodes[r.id].Remove(r);
            }
            foreach (var newNode in newNodes)
            {
                nodes[newNode.id].Add(newNode);
                newNode.prev.next = newNode;
                newNode.next.prev = newNode;
            }
        }

        public SectorConstrainedOSMAreaGraph Subtract(SectorConstrainedOSMAreaGraph map, BlobCollection blobs)
        {
            map = map.Clone(); // now we're allowed to junk it
            map.Reverse();
            var loopNodes = DoLoops(map, blobs);
            // remember, counterclockwise makes an island
            List<AreaNode> doAdd = new List<AreaNode>();
            List<AreaNode> doDelete = new List<AreaNode>();
            List<long> singularDelete = new List<long>();
            HashSet<long> criticalPoints = new HashSet<long>();
            foreach (var pair in map.nodes)
            {
                if (nodes.ContainsKey(pair.Key))
                {
                    criticalPoints.Add(pair.Key);
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
                    finalLines = finalLines.OrderBy(x => ComputeInnerAngle(new Vector2d(1, 0), GetPos(x, blobs) - blobs.nodes[pair.Key])).ToList();
                    bool[] sectors = new bool[finalLines.Count]; // first sector means the area just ccw of the first finalLine
                    // NOTE: this logic only works assuming multiple polygons are disjoint at intersections
                    foreach (var n in nodes[pair.Key])
                    {
                        int from = finalLines.IndexOf(n.prev);
                        int to = finalLines.IndexOf(n.next);
                        if (to <= from) to += finalLines.Count;
                        for (int i = from + 1; i <= to; i++)
                        {
                            sectors[i % sectors.Length] = true;
                        }
                    }
                    foreach (var n in map.nodes[pair.Key])
                    {
                        int from = finalLines.IndexOf(n.next); // swapped because subtraction
                        int to = finalLines.IndexOf(n.prev);
                        if (to <= from) to += finalLines.Count;
                        for (int i = from + 1; i <= to; i++)
                        {
                            sectors[i % sectors.Length] = false; // this is the only difference for a subtraction, as it should be
                        }
                    }
                    // make sure zero-width areas match one of their neighbors so they get cleaned up
                    // correction: make sure zero-width somehow prioritize getting rid of new map lines like our original code
                    // turns out, even in our first test case, being inconsistent with this is just wrong (to see, treat this test case as one of two non-degenerate cases)
                    int[] makeMatch = new int[sectors.Length]; // -1 means, make match left, etc. TODO: I'm really winging it here, no clue if this makes any sense at all (basically, just test it!)
                    for (int i = 0; i < sectors.Length; i++)
                    {
                        AreaNode p = finalLines[(i - 1 + finalLines.Count) % finalLines.Count];
                        AreaNode n = finalLines[i];
                        if (p.id == n.id)
                        {
                            makeMatch[i] = mapInitialLines.Contains(p) ? -1 : 1; // make sector before this match this one to eliminate p
                        }
                    }
                    if (!makeMatch.Contains(0)) throw new NotImplementedException();
                    for (int i = 0; i < makeMatch.Length; i++)
                    {
                        HashSet<int> explored = new HashSet<int>();
                        int curr = i;
                        while (true)
                        {
                            if (makeMatch[curr] == 0)
                            {
                                sectors[i] = sectors[curr];
                                break;
                            }
                            if (explored.Contains(curr)) throw new NotImplementedException(); // some kind of rare loop
                            explored.Add(curr);
                            curr = (curr + makeMatch[curr] + makeMatch.Length) % makeMatch.Length;
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
                        if (finalLines.First().next == null || finalLines.First().next.id != pair.Key)
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
                    for (int i = 0; i < finalLines.Count / 2; i++)
                    {
                        AreaNode into = finalLines[i * 2];
                        AreaNode outof = finalLines[i * 2 + 1];
                        AreaNode newSrcNode;
                        if (i == 0) // mimic our original code for debugging?
                        {
                            newSrcNode = nodes[pair.Key].First();
                        }
                        else
                        {
                            newSrcNode = new AreaNode() { id = pair.Key };
                        }
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
                bool forwards = d.next != null && (!criticalPoints.Contains(d.next.id));
                var temp = d;
                while (temp != null && !criticalPoints.Contains(temp.id))
                {
                    if (!ContainsNode(temp)) break;
                    nodes[temp.id].Remove(temp);
                    if (nodes[temp.id].Count == 0) nodes.Remove(temp.id);
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            // add until you reach the next critical point
            foreach (var d in doAdd)
            {
                bool forwards = d.next != null && (!criticalPoints.Contains(d.next.id));
                var temp = d;
                while (temp != null && !criticalPoints.Contains(temp.id))
                {
                    if (ContainsNode(temp)) break;
                    if (!nodes.ContainsKey(temp.id)) nodes[temp.id] = new List<AreaNode>();
                    nodes[temp.id].Add(temp);
                    temp = forwards ? temp.next : temp.prev;
                }
            }
            foreach (var d in singularDelete) nodes.Remove(d);
            foreach (var n in loopNodes)
            {
                if (!nodes.ContainsKey(n.id)) nodes[n.id] = new List<AreaNode>();
                nodes[n.id].Add(n);
            }
            return this;
        }

        private bool ContainsNode(AreaNode node)
        {
            if (node.id == -1) throw new NotImplementedException();
            return nodes.ContainsKey(node.id) && nodes[node.id].Contains(node);
        }

        // TODO: apparently we've been adding loops twice this entire time, basically
        // I see, I never did loops at the end before because map was modified at that point
        private List<AreaNode> DoLoops(SectorConstrainedOSMAreaGraph map, BlobCollection blobs)
        {
            // first, find those loops
            // actually, non-loops should be found as well!
            List<AreaNode> loopNodes = new List<AreaNode>();
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
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
                            loopNodes.Add(n);
                        }
                    }
                }
            }
            return loopNodes;
        }

        public void CheckValid()
        {
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            foreach (var nodeList in nodes.Values)
            {
                foreach (var node in nodeList)
                {
                    if (explored.Contains(node)) continue;
                    // just loops can be found at this point
                    AreaNode curr = node;
                    while (true)
                    {
                        if (nodes[curr.id].Count == 0 || !nodes[curr.id].All(x => x.id == curr.id)) throw new NotImplementedException();
                        if (curr.id == -1 || curr.next == null || curr.prev == null || curr.next.prev != curr || curr.prev.next != curr) throw new NotImplementedException();
                        explored.Add(curr);
                        if (curr.next == node) break;
                        if (curr.next == null) break;
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
            // TODO: make vector2d a struct?
            SectorConstrainedOSMAreaGraph map = new SectorConstrainedOSMAreaGraph();
            Dictionary<AreaNode, AreaNode> mapper = new Dictionary<AreaNode, AreaNode>();
            // fully clone, even the bad parts
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            Queue<AreaNode> bfs = new Queue<AreaNode>();
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
                mapper[x] = new AreaNode() { id = x.id };
            }
            foreach (var x in explored)
            {
                mapper[x].prev = x.prev == null ? null : mapper[x.prev];
                mapper[x].next = x.next == null ? null : mapper[x.next];
            }
            foreach (var pair in nodes)
            {
                map.nodes[pair.Key] = pair.Value.Select(x => mapper[x]).ToList();
            }
            return map;
        }

        internal void CloseLines(BlobCollection blobs)
        {
            if (nodes.Count == 0) return;
            var startEndPoints = new List<QuickRef>();
            foreach (var pair in nodes)
            {
                foreach (var node in pair.Value)
                {
                    if (node.next == null || node.prev == null)
                    {
                        startEndPoints.Add(new QuickRef() { node = node, isBorder = false });
                        if (blobs.nodes[node.id].X != 0 && blobs.nodes[node.id].X != 1 && blobs.nodes[node.id].Y != 0 && blobs.nodes[node.id].Y != 1) throw new NotImplementedException();
                    }
                }
            }
            if (startEndPoints.Count == 0) return; // usually hit when trying to close the lines on relations
            foreach (var r in blobs.borderWay.refs.Skip(1))
            {
                if (!nodes.ContainsKey(r))
                {
                    startEndPoints.Add(new QuickRef() { node = new AreaNode() { id = r }, isBorder = true });
                }
            }
            startEndPoints = startEndPoints.OrderBy(x => -Math.Atan2(blobs.nodes[x.node.id].Y - 0.5, blobs.nodes[x.node.id].X - 0.5)).ToList();
            int offset = startEndPoints.FindIndex(x => !x.isBorder);
            if (offset < 0) throw new NotImplementedException();
            for (int i = 0; i < offset; i++) // rotate it so that the first one is always a non-corner
            {
                var temp = startEndPoints[0];
                startEndPoints.RemoveAt(0);
                startEndPoints.Add(temp);
            }
            bool recentlyConnected = false;
            for (int i = 0; i < startEndPoints.Count; i++)
            {
                AreaNode prev = startEndPoints[i].node;
                AreaNode next = startEndPoints[(i + 1) % startEndPoints.Count].node;
                bool prevIsCorner = startEndPoints[i].isBorder;
                bool nextIsCorner = startEndPoints[(i + 1) % startEndPoints.Count].isBorder;
                if (!prevIsCorner && !nextIsCorner && prev.next == null && next.prev != null) throw new NotImplementedException(); // two exit nodes in a row
                if ((prevIsCorner && nextIsCorner && recentlyConnected) || (!prevIsCorner && nextIsCorner && prev.next == null) || (prevIsCorner && !nextIsCorner && next.prev == null) || (!prevIsCorner && !nextIsCorner && prev.next == null && next.prev == null))
                {
                    prev.next = next;
                    next.prev = prev;
                    if (prevIsCorner)
                    {
                        if (!nodes.ContainsKey(prev.id)) nodes[prev.id] = new List<AreaNode>();
                        nodes[prev.id].Add(prev);
                    }
                    recentlyConnected = true;
                }
                else
                {
                    recentlyConnected = false;
                }
            }
        }

        public class QuickRef
        {
            public AreaNode node;
            public bool isBorder;
        }

        private static Vector2d GetPos(AreaNode node, BlobCollection blobs)
        {
            return blobs.nodes[node.id];
        }

        public SectorConstrainedAreaMap Finalize(BlobCollection blobs)
        {
            HashSet<AreaNode> explored = new HashSet<AreaNode>();
            SectorConstrainedAreaMap map = new SectorConstrainedAreaMap();
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
                        Vector2d v = blobs.nodes[curr.id];
                        // ex: node 5534127050
                        if (PERTURB_AT_JOINTS && nodes[curr.id].Count > 1)
                        {
                            Vector2d v1 = blobs.nodes[curr.next.id];
                            Vector2d v2 = blobs.nodes[curr.prev.id];
                            v += (v1 + v2 - blobs.nodes[curr.id] * 2).Normalized() * PERTURB_AMOUNT;
                        }
                        newLoop.Add(v);
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
        public long id = -1;
    }
}