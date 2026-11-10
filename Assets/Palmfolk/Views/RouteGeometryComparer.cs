#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Core;

namespace Palmfolk.Views
{
    // Save round-trips can duplicate Route objects. Equal physical trunks still render once.
    internal sealed class RouteGeometryComparer : IEqualityComparer<Route>
    {
        public static readonly RouteGeometryComparer Instance = new RouteGeometryComparer();
        public bool Equals(Route a, Route b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Reversed != b.Reversed || a.Points.Count != b.Points.Count) return false;
            for (int i = 0; i < a.Points.Count; i++) if (!a.Points[i].Equals(b.Points[i])) return false;
            for (int i = 0; i < a.Widths.Count; i++) if (a.Widths[i] != b.Widths[i]) return false;
            return true;
        }
        public int GetHashCode(Route route)
        {
            unchecked
            {
                int hash = route.Reversed ? 23 : 17;
                foreach (var point in route.Points) hash = hash * 31 + point.GetHashCode();
                foreach (double width in route.Widths) hash = hash * 31 + width.GetHashCode();
                return hash;
            }
        }
    }
}
#endif
