// Geometric readout only: never use collider hulls or resource scaling factors.
// The mesh must describe its inner walls for material volume to be measurable.
using System;
using System.Collections.Generic;

namespace PartSculptor
{
    internal struct PartVolumeResult
    {
        internal double Volume;
        internal int ClosedShells;
        internal bool Partial;
    }

    internal static class PartVolumeCore
    {
        private sealed class Shell
        {
            internal readonly List<int> Faces = new List<int>();
            internal MorphPoint Min, Max;
            internal double Volume;
        }

        private struct EdgeUse
        {
            internal int Face, Direction;
            internal EdgeUse(int face, int direction) { Face = face; Direction = direction; }
        }

        private static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        private static MorphPoint Sub(MorphPoint a, MorphPoint b)
        { return new MorphPoint(a.X-b.X, a.Y-b.Y, a.Z-b.Z); }
        private static double Dot(MorphPoint a, MorphPoint b) { return a.X*b.X+a.Y*b.Y+a.Z*b.Z; }
        private static MorphPoint Cross(MorphPoint a, MorphPoint b)
        { return new MorphPoint(a.Y*b.Z-a.Z*b.Y, a.Z*b.X-a.X*b.Z, a.X*b.Y-a.Y*b.X); }
        private static double Length(MorphPoint a) { return Math.Sqrt(Dot(a,a)); }
        private static long Key(int a, int b)
        { return ((long)Math.Min(a,b) << 32) | (uint)Math.Max(a,b); }

        private static void AddEdge(Dictionary<long,List<EdgeUse>> edges, int a, int b, int face)
        {
            long key = Key(a,b);
            List<EdgeUse> uses;
            if (!edges.TryGetValue(key,out uses)) edges.Add(key,uses = new List<EdgeUse>(2));
            uses.Add(new EdgeUse(face,a<b?1:-1));
        }

        // Exact positional welding reconnects normal/UV/material seams without
        // merging nearby but distinct inner and outer walls of thin shells.
        internal static PartVolumeResult Measure(IList<MorphPoint> input, IList<int> indices, bool partial = false)
        {
            var result = new PartVolumeResult { Partial = partial };
            var vertices = new List<MorphPoint>();
            var weld = new Dictionary<Tuple<double,double,double>,int>();
            var remap = new int[input.Count];
            for (int i=0;i<input.Count;i++)
            {
                MorphPoint p=input[i];
                if (!Finite(p.X)||!Finite(p.Y)||!Finite(p.Z)) { remap[i]=-1; result.Partial=true; continue; }
                var key=Tuple.Create(p.X,p.Y,p.Z);
                int id;
                if (!weld.TryGetValue(key,out id)) { id=vertices.Count; weld.Add(key,id); vertices.Add(p); }
                remap[i]=id;
            }
            var faces=new List<int>();
            var edges=new Dictionary<long,List<EdgeUse>>();
            if (indices.Count%3!=0) result.Partial=true;
            for (int i=0;i+2<indices.Count;i+=3)
            {
                int a=indices[i], b=indices[i+1], c=indices[i+2];
                if (a<0||b<0||c<0||a>=remap.Length||b>=remap.Length||c>=remap.Length)
                { result.Partial=true; continue; }
                a=remap[a]; b=remap[b]; c=remap[c];
                if (a<0||b<0||c<0) { result.Partial=true; continue; }
                if (a==b||b==c||a==c) continue;
                MorphPoint normal=Cross(Sub(vertices[b],vertices[a]),Sub(vertices[c],vertices[a]));
                if (Dot(normal,normal)==0d) continue;
                int face=faces.Count/3;
                faces.Add(a); faces.Add(b); faces.Add(c);
                AddEdge(edges,a,b,face); AddEdge(edges,b,c,face); AddEdge(edges,c,a,face);
            }
            var signs=new int[faces.Count/3];
            var shells=new List<Shell>();
            var queue=new Queue<int>();
            for (int seed=0;seed<signs.Length;seed++)
            {
                if (signs[seed]!=0) continue;
                signs[seed]=1; queue.Enqueue(seed);
                var shell=new Shell();
                MorphPoint origin=vertices[faces[3*seed]];
                shell.Min=shell.Max=origin;
                bool closed=true;
                double signed=0d;
                while (queue.Count!=0)
                {
                    int f=queue.Dequeue(); shell.Faces.Add(f);
                    MorphPoint a=vertices[faces[3*f]], b=vertices[faces[3*f+1]], c=vertices[faces[3*f+2]];
                    signed+=signs[f]*Dot(Sub(a,origin),Cross(Sub(b,origin),Sub(c,origin)))/6d;
                    for (int j=0;j<3;j++)
                    {
                        int v=faces[3*f+j], w=faces[3*f+(j+1)%3];
                        MorphPoint p=vertices[v];
                        shell.Min=new MorphPoint(Math.Min(shell.Min.X,p.X),Math.Min(shell.Min.Y,p.Y),Math.Min(shell.Min.Z,p.Z));
                        shell.Max=new MorphPoint(Math.Max(shell.Max.X,p.X),Math.Max(shell.Max.Y,p.Y),Math.Max(shell.Max.Z,p.Z));
                        List<EdgeUse> uses=edges[Key(v,w)];
                        if (uses.Count!=2) closed=false;
                        int direction=v<w?1:-1;
                        foreach (EdgeUse use in uses)
                        {
                            if (use.Face==f) continue;
                            int expected=-signs[f]*direction*use.Direction;
                            if (signs[use.Face]==0) { signs[use.Face]=expected; queue.Enqueue(use.Face); }
                            else if (signs[use.Face]!=expected) closed=false;
                        }
                    }
                }
                // Repair inconsistent triangle winding by propagation; never
                // assign a volume to an open or non-manifold component.
                if (!closed||!Finite(signed)||Math.Abs(signed)<=0d) { result.Partial=true; continue; }
                shell.Volume=Math.Abs(signed); shells.Add(shell);
            }
            result.ClosedShells=shells.Count;
            // Even/odd nesting: a closed inner shell is a cavity; a solid
            // island inside that cavity is added back. Orientation is irrelevant.
            // Separate overlapping solids are not Boolean-unioned; callers
            // label multi-shell readings approximate.
            foreach (Shell shell in shells)
            {
                int depth=0;
                foreach (Shell outer in shells)
                {
                    if (outer==shell||outer.Volume<=shell.Volume||!BoundsContain(outer,shell)) continue;
                    if (Contains(outer,shell,vertices,faces,signs)) depth++;
                }
                result.Volume+=(depth%2==0?1d:-1d)*shell.Volume;
            }
            if (!Finite(result.Volume)||result.Volume<=0d)
            { result.Volume=0d; result.ClosedShells=0; }
            return result;
        }

        private static bool BoundsContain(Shell a, Shell b)
        {
            return a.Min.X<=b.Min.X&&a.Min.Y<=b.Min.Y&&a.Min.Z<=b.Min.Z&&
                a.Max.X>=b.Max.X&&a.Max.Y>=b.Max.Y&&a.Max.Z>=b.Max.Z;
        }

        // Solid-angle winding test avoids ray/edge degeneracies. Sample inner
        // vertices, rejecting crossing candidates detectable at those points.
        // Bound the expensive work for dense models.
        private static bool Contains(Shell outer, Shell inner, List<MorphPoint> vertices, List<int> faces, int[] signs)
        {
            var seen=new HashSet<int>();
            // A bounded, evenly spaced sample for unusually dense shells.
            int stride=Math.Max(1,inner.Faces.Count/32);
            for (int i=0;i<inner.Faces.Count;i+=stride)
                for (int j=0;j<3;j++)
                {
                    int v=faces[3*inner.Faces[i]+j];
                    if (seen.Add(v)&&!Inside(outer,vertices[v],vertices,faces,signs)) return false;
                }
            return seen.Count>0;
        }
        private static bool Inside(Shell shell, MorphPoint p, List<MorphPoint> vertices, List<int> faces, int[] signs)
        {
            double angle=0d;
            foreach (int f in shell.Faces)
            {
                MorphPoint a=Sub(vertices[faces[3*f]],p),b=Sub(vertices[faces[3*f+1]],p),c=Sub(vertices[faces[3*f+2]],p);
                double la=Length(a),lb=Length(b),lc=Length(c);
                if (la==0d||lb==0d||lc==0d) return false;
                double denominator=la*lb*lc+Dot(a,b)*lc+Dot(b,c)*la+Dot(c,a)*lb;
                angle+=signs[f]*2d*Math.Atan2(Dot(a,Cross(b,c)),denominator);
            }
            return Math.Abs(angle)>2d*Math.PI;
        }
    }
}
