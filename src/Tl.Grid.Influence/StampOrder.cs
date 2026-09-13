using System.Collections.Generic;

namespace Tl.Grid.Influence;

public sealed class StampOrder : IComparer<Stamp>
{
    public static readonly StampOrder Instance = new();

    private StampOrder()
    {
    }

    public int Compare(Stamp a, Stamp b)
    {
        var c = CompareOrigin(in a, in b);
        if (c != 0) return c;

        c = CompareShapeIdentity(in a, in b);
        return c != 0 ? c : CompareShapeExtents(in a, in b);
    }

    private static int CompareOrigin(in Stamp a, in Stamp b)
    {
        var c = a.Origin.X.CompareTo(b.Origin.X);
        return c != 0 ? c : a.Origin.Y.CompareTo(b.Origin.Y);
    }

    private static int CompareShapeIdentity(in Stamp a, in Stamp b)
    {
        var c = ((byte)a.Shape.Kind).CompareTo((byte)b.Shape.Kind);
        if (c != 0) return c;

        c = a.Shape.Weight.CompareTo(b.Shape.Weight);
        if (c != 0) return c;

        c = a.Shape.RectMin.X.CompareTo(b.Shape.RectMin.X);
        return c != 0 ? c : a.Shape.RectMin.Y.CompareTo(b.Shape.RectMin.Y);
    }

    private static int CompareShapeExtents(in Stamp a, in Stamp b)
    {
        var c = a.Shape.RectSize.X.CompareTo(b.Shape.RectSize.X);
        if (c != 0) return c;

        c = a.Shape.RectSize.Y.CompareTo(b.Shape.RectSize.Y);
        if (c != 0) return c;

        c = a.Shape.ShellThickness.CompareTo(b.Shape.ShellThickness);
        return c != 0 ? c : CompareShapeAngles(in a, in b);
    }

    private static int CompareShapeAngles(in Stamp a, in Stamp b)
    {
        var c = a.Shape.AnnulusInnerRadius.CompareTo(b.Shape.AnnulusInnerRadius);
        if (c != 0) return c;

        c = a.Shape.SectorDir1.X.CompareTo(b.Shape.SectorDir1.X);
        return c != 0 ? c : a.Shape.SectorDir1.Y.CompareTo(b.Shape.SectorDir1.Y);
    }
}
