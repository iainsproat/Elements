using Elements.Validators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elements.Geometry
{
    /// <summary>
    /// A linear curve between two points.
    /// </summary>
    /// <example>
    /// [!code-csharp[Main](../../Elements/test/LineTests.cs?name=example)]
    /// </example>
    public class Line : Curve, IEquatable<Line>
    {
        /// <summary>The start of the line.</summary>
        [Newtonsoft.Json.JsonProperty("Start", Required = Newtonsoft.Json.Required.AllowNull)]
        public Vector3 Start { get; set; }

        /// <summary>The end of the line.</summary>
        [Newtonsoft.Json.JsonProperty("End", Required = Newtonsoft.Json.Required.AllowNull)]
        public Vector3 End { get; set; }

        /// <summary>
        /// Create a line.
        /// </summary>
        /// <param name="start">The start of the line.</param>
        /// <param name="end">The end of the line.</param>
        [Newtonsoft.Json.JsonConstructor]
        public Line(Vector3 @start, Vector3 @end) : base()
        {
            if (!Validator.DisableValidationOnConstruction)
            {
                if (start.IsAlmostEqualTo(end))
                {
                    throw new ArgumentException($"The line could not be created. The start and end points of the line cannot be the same: start {start}, end {end}");
                }
            }

            this.Start = @start;
            this.End = @end;
        }

        /// <summary>
        /// Calculate the length of the line.
        /// </summary>
        public override double Length()
        {
            return this.Start.DistanceTo(this.End);
        }

        /// <summary>
        /// Create a line of one unit length along the X axis.
        /// </summary>
        public Line()
        {
            this.Start = Vector3.Origin;
            this.End = new Vector3(1, 0, 0);
        }

        /// <summary>
        /// Construct a line of length from a start along direction.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        public Line(Vector3 start, Vector3 direction, double length)
        {
            this.Start = start;
            this.End = start + direction.Unitized() * length;
        }

        /// <summary>
        /// Get a transform whose XY plane is perpendicular to the curve, and whose
        /// positive Z axis points along the curve.
        /// </summary>
        /// <param name="u">The parameter along the Line, between 0.0 and 1.0, at which to calculate the Transform.</param>
        /// <returns>A transform.</returns>
        public override Transform TransformAt(double u)
        {
            return new Transform(PointAt(u), (this.Start - this.End).Unitized());
        }

        /// <summary>
        /// Get a point along the line at parameter u.
        /// </summary>
        /// <param name="u"></param>
        /// <returns>A point on the curve at parameter u.</returns>
        public override Vector3 PointAt(double u)
        {
            if (u > 1.0 || u < 0.0)
            {
                throw new Exception("The parameter t must be between 0.0 and 1.0.");
            }
            var offset = this.Length() * u;
            return this.Start + offset * this.Direction();
        }

        /// <summary>
        /// Construct a transformed copy of this Curve.
        /// </summary>
        /// <param name="transform">The transform to apply.</param>
        public override Curve Transformed(Transform transform)
        {
            return TransformedLine(transform);
        }

        /// <summary>
        /// Construct a transformed copy of this Line.
        /// </summary>
        /// <param name="transform">The transform to apply.</param>
        public Line TransformedLine(Transform transform)
        {
            return new Line(transform.OfPoint(this.Start), transform.OfPoint(this.End));
        }

        /// <summary>
        /// Get a new line that is the reverse of the original line.
        /// </summary>
        public Line Reversed()
        {
            return new Line(End, Start);
        }

        /// <summary>
        /// Thicken a line by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to thicken the line.</param>
        public Polygon Thicken(double amount)
        {
            if (!Start.Z.ApproximatelyEquals(End.Z))
            {
                throw new Exception("The line could not be thickened. Only lines with their start and end at the same elevation can be thickened.");
            }

            var offsetN = this.Direction().Cross(Vector3.ZAxis);
            var a = this.Start + (offsetN * (amount / 2));
            var b = this.End + (offsetN * (amount / 2));
            var c = this.End - (offsetN * (amount / 2));
            var d = this.Start - (offsetN * (amount / 2));
            return new Polygon(new[] { a, b, c, d });
        }

        /// <summary>
        /// Is this line equal to the provided line?
        /// </summary>
        /// <param name="other">The target line.</param>
        /// <returns>True if the start and end points of the lines are equal, otherwise false.</returns>
        public bool Equals(Line other)
        {
            if (other == null)
            {
                return false;
            }
            return this.Start.Equals(other.Start) && this.End.Equals(other.End);
        }

        /// <summary>
        /// Get the hash code for the line.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.Start.GetHashCode();
                hash = hash * 23 + this.End.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Are the two lines almost equal?
        /// </summary>
        public bool IsAlmostEqualTo(Line other, bool directionDependent, double tolerance = Vector3.EPSILON)
        {
            return (Start.IsAlmostEqualTo(other.Start, tolerance) && End.IsAlmostEqualTo(other.End, tolerance))
                    || (!directionDependent
                        && (Start.IsAlmostEqualTo(other.End, tolerance) && End.IsAlmostEqualTo(other.Start, tolerance)));
        }

        /// <summary>
        /// Intersect this line with the specified plane
        /// </summary>
        /// <param name="p">The plane.</param>
        /// <param name="result">The location of intersection.</param>
        /// <param name="infinite">If true, line will be treated as infinite. (False by default)</param>
        /// <returns>True if the line intersects the plane, false if no intersection occurs.</returns>
        public bool Intersects(Plane p, out Vector3 result, bool infinite = false)
        {
            return Intersects(p, this.Start, this.End, out result, infinite);
        }

        /// <summary>
        /// Intersect a segment defined by two points with a plane.
        /// </summary>
        /// <param name="p">The plane.</param>
        /// <param name="start">The start of the segment.</param>
        /// <param name="end">The end of the segment.</param>
        /// <param name="result">The location of intersection.</param>
        /// <param name="infinite">Whether the segment should instead be considered infinite.</param>
        /// <returns>True if an intersection is found, otherwise false.</returns>
        public static bool Intersects(Plane p, Vector3 start, Vector3 end, out Vector3 result, bool infinite = false)
        {
            var d = (end - start).Unitized();
            var rayIntersects = new Ray(start, d).Intersects(p, out Vector3 location, out double t);
            if (rayIntersects)
            {
                var l = start.DistanceTo(end);
                if (infinite || t.ApproximatelyEquals(l) || t < l)
                {
                    result = location;
                    return true;
                }
            }
            else if (infinite)
            {
                var rayIntersectsBackwards = new Ray(end, d.Negate()).Intersects(p, out Vector3 location2, out double t2);
                if (rayIntersectsBackwards)
                {
                    result = location2;
                    return true;
                }
            }
            result = default(Vector3);
            return false;
        }

        /// <summary>
        /// Does this line intersect the provided line in 2D?
        /// </summary>
        /// <param name="l"></param>
        /// <returns>Return true if the lines intersect,
        /// false if the lines have coincident vertices or do not intersect.</returns>
        public bool Intersects2D(Line l)
        {
            return Intersects2d(Start, End, l.Start, l.End);
        }

        /// <summary>
        /// Does the first line intersect with the second line in 2D?
        /// </summary>
        /// <param name="start1">Start point of the first line</param>
        /// <param name="end1">End point of the first line</param>
        /// <param name="start2">Start point of the second line</param>
        /// <param name="end2">End point of the second line</param>
        /// <returns>Return true if the lines intersect,
        /// false if the lines have coincident vertices or do not intersect.</returns>
        public static bool Intersects2d(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
        {
            var a = Vector3.CCW(start1, end1, start2) * Vector3.CCW(start1, end1, end2);
            var b = Vector3.CCW(start2, end2, start1) * Vector3.CCW(start2, end2, end1);
            if (IsAlmostZero(a) || a > Vector3.EPSILON) return false;
            if (IsAlmostZero(b) || b > Vector3.EPSILON) return false;
            return true;
        }

        /// <summary>
        /// Does this line intersect the provided line in 3D?
        /// </summary>
        /// <param name="l"></param>
        /// <param name="result"></param>
        /// <param name="infinite">Treat the lines as infinite?</param>
        /// <param name="includeEnds">If the end of one line lies exactly on the other, count it as an intersection?</param>
        /// <returns>True if the lines intersect, false if they are fully collinear or do not intersect.</returns>
        public bool Intersects(Line l, out Vector3 result, bool infinite = false, bool includeEnds = false)
        {
            return Line.Intersects(this.Start, this.End, l.Start, l.End, out result, infinite, includeEnds);
        }

        /// <summary>
        /// Do two lines intersect in 3d?
        /// </summary>
        /// <param name="start1">Start point of the first line</param>
        /// <param name="end1">End point of the first line</param>
        /// <param name="start2">Start point of the second line</param>
        /// <param name="end2">End point of the second line</param>
        /// <param name="result"></param>
        /// <param name="infinite">Treat the lines as infinite?</param>
        /// <param name="includeEnds">If the end of one line lies exactly on the other, count it as an intersection?</param>
        /// <returns>True if the lines intersect, false if they are fully collinear or do not intersect.</returns>
        public static bool Intersects(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out Vector3 result, bool infinite = false, bool includeEnds = false)
        {
            // check if two lines are parallel
            var direction1 = Direction(start1, end1);
            var direction2 = Direction(start2, end2);
            if (direction1.IsParallelTo(direction2))
            {
                result = default(Vector3);
                return false;
            }
            // construct a plane through this line and the start or end of the other line
            Plane plane;
            Vector3 testpoint;
            if (!(new[] { start1, end1, start2 }).AreCollinear())
            {
                plane = new Plane(start1, end1, start2);
                testpoint = end2;

            } // this only occurs in the rare case that the start point of the other line is collinear with this line (still need to generate a plane)
            else if (!(new[] { start1, end1, end2 }).AreCollinear())
            {
                plane = new Plane(start1, end1, end2);
                testpoint = start2;
            }
            else // they're collinear (this shouldn't occur since it should be caught by the parallel test)
            {
                result = default(Vector3);
                return false;
            }

            // check if the fourth point is in the same plane as the other 3
            if (Math.Abs(plane.SignedDistanceTo(testpoint)) > Vector3.EPSILON)
            {
                result = default(Vector3);
                return false;
            }

            // at this point they're not parallel, and they lie in the same plane, so we know they intersect, we just don't know where.
            // construct a plane
            var normal = direction2.Cross(plane.Normal);
            Plane intersectionPlane = new Plane(start2, normal);
            if (Intersects(intersectionPlane, start1, end1, out Vector3 planeIntersectionResult, true)) // does the line intersect the plane?
            {
                if (infinite || (PointOnLine(planeIntersectionResult, start2, end2, includeEnds) && PointOnLine(planeIntersectionResult, start1, end1, includeEnds)))
                {
                    result = planeIntersectionResult;
                    return true;
                }

            }
            result = default(Vector3);
            return false;
        }

        /// <summary>
        /// Does this line touches or intersects the provided box in 3D?
        /// </summary>
        /// <param name="box"></param>
        /// <param name="results"></param>
        /// <param name="infinite">Treat the line as infinite?</param>
        /// <returns>True if the line touches or intersects the  box at least at one point, false otherwise.</returns>
        public bool Intersects(BBox3 box, out List<Vector3> results, bool infinite = false)
        {
            var d = End - Start;
            results = new List<Vector3>();

            // Solving the t parameter on line were it intersects planes of box in different coordinates.
            // If vector has no change in particular coordinate - just skip it as infinity.
            var t0x = double.NegativeInfinity;
            var t1x = double.PositiveInfinity;
            if (Math.Abs(d.X) > 1e-6)
            {
                t0x = (box.Min.X - Start.X) / d.X;
                t1x = (box.Max.X - Start.X) / d.X;
                // Line can reach min plane of box before reaching max.
                if (t1x < t0x)
                {
                    (t0x, t1x) = (t1x, t0x);
                }
            }

            var t0y = double.NegativeInfinity;
            var t1y = double.PositiveInfinity;
            if (Math.Abs(d.Y) > 1e-6)
            {
                t0y = (box.Min.Y - Start.Y) / d.Y;
                t1y = (box.Max.Y - Start.Y) / d.Y;
                if (t1y < t0y)
                {
                    (t0y, t1y) = (t1y, t0y);
                }
            }

            // If max hit of one coordinate is smaller then min hit of other - line hits planes outside the box.
            // In other words line just goes by.
            if (t0x > t1y || t0y > t1x)
            {
                return false;
            }

            var tMin = Math.Max(t0x, t0y);
            var tMax = Math.Min(t1x, t1y);

            if (Math.Abs(d.Z) > 1e-6)
            {
                var t0z = (box.Min.Z - Start.Z) / d.Z;
                var t1z = (box.Max.Z - Start.Z) / d.Z;

                if (t1z < t0z)
                {
                    (t0z, t1z) = (t1z, t0z);
                }

                if (t0z > tMax || t1z < tMin)
                {
                    return false;
                }

                tMin = Math.Max(t0z, tMin);
                tMax = Math.Min(t1z, tMax);
            }

            if (tMin == double.NegativeInfinity || tMin == double.PositiveInfinity)
            {
                return false;
            }

            // Check if found parameters are within normalized line range.
            if (infinite || (tMin > -Vector3.EPSILON && tMin < 1 + Vector3.EPSILON))
            {
                results.Add(Start + d * tMin);
            }

            if (Math.Abs(tMax - tMin) > Vector3.EPSILON &&
                (infinite || (tMax > -Vector3.EPSILON && tMax < 1 + Vector3.EPSILON)))
            {
                results.Add(Start + d * tMax);
            }

            return results.Any();
        }

        private static bool IsAlmostZero(double a)
        {
            return Math.Abs(a) < Vector3.EPSILON;
        }

        /// <summary>
        /// Get the bounding box for this line.
        /// </summary>
        /// <returns>A bounding box for this line.</returns>
        public override BBox3 Bounds()
        {
            if (this.Start < this.End)
            {
                return new BBox3(this.Start, this.End);
            }
            else
            {
                return new BBox3(this.End, this.Start);
            }
        }

        /// <summary>
        /// A normalized vector representing the direction of the line.
        /// </summary>
        public Vector3 Direction()
        {
            return Direction(Start, End);
        }

        /// <summary>
        /// A normalized vector representing the direction of a line, represented by a start and end point.
        /// <param name="start">The start point of the line.</param>
        /// <param name="end">The end point of the line.</param>
        /// </summary>
        public static Vector3 Direction(Vector3 start, Vector3 end)
        {
            return (end - start).Unitized();
        }

        /// <summary>
        /// Test if a point lies within this line segment
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="includeEnds">Consider a point at the endpoint as on the line.</param>
        public bool PointOnLine(Vector3 point, bool includeEnds = false)
        {
            return Line.PointOnLine(point, Start, End, includeEnds);
        }

        /// <summary>
        /// Test if a point lies within a given line segment
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="start">The start point of the line segment.</param>
        /// <param name="end">The end point of the line segment.</param>
        /// <param name="includeEnds">Consider a point at the endpoint as on the line.</param>
        public static bool PointOnLine(Vector3 point, Vector3 start, Vector3 end, bool includeEnds = false)
        {
            if (includeEnds && (point.DistanceTo(start) < Vector3.EPSILON || point.DistanceTo(end) < Vector3.EPSILON))
            {
                return true;
            }
            return (start - point).Unitized().Dot((end - point).Unitized()) < (Vector3.EPSILON - 1);
        }

        /// <summary>
        /// Divide the line into as many segments of the provided length as possible.
        /// </summary>
        /// <param name="l">The length.</param>
        /// <param name="removeShortSegments">A flag indicating whether segments shorter than l should be removed.</param>
        public List<Line> DivideByLength(double l, bool removeShortSegments = false)
        {
            var len = this.Length();
            if (l > len)
            {
                return new List<Line>() { new Line(this.Start, this.End) };
            }

            var total = 0.0;
            var d = this.Direction();
            var lines = new List<Line>();
            while (total + l <= len)
            {
                var a = this.Start + d * total;
                var b = a + d * l;
                lines.Add(new Line(a, b));
                total += l;
            }
            if (total < len && !removeShortSegments)
            {
                var a = this.Start + d * total;
                if (!a.IsAlmostEqualTo(End))
                {
                    lines.Add(new Line(a, End));
                }
            }
            return lines;
        }


        /// <summary>
        /// Divide the line into as many segments of the provided length as possible.
        /// Divisions will be centered along the line.
        /// </summary>
        /// <param name="l">The length.</param>
        /// <returns></returns>
        public List<Line> DivideByLengthFromCenter(double l)
        {
            var lines = new List<Line>();

            var localLength = this.Length();
            if (localLength <= l)
            {
                lines.Add(this);
                return lines;
            }

            var divs = (int)(localLength / l);
            var span = divs * l;
            var halfSpan = span / 2;
            var mid = this.PointAt(0.5);
            var dir = this.Direction();
            var start = mid - dir * halfSpan;
            var end = mid + dir * halfSpan;
            if (!this.Start.IsAlmostEqualTo(start))
            {
                lines.Add(new Line(this.Start, start));
            }
            for (var i = 0; i < divs; i++)
            {
                var p1 = start + (i * l) * dir;
                var p2 = p1 + dir * l;
                lines.Add(new Line(p1, p2));
            }
            if (!this.End.IsAlmostEqualTo(end))
            {
                lines.Add(new Line(end, this.End));
            }

            return lines;
        }

        /// <summary>
        /// Divide the line into n equal segments.
        /// </summary>
        /// <param name="n">The number of segments.</param>
        public List<Line> DivideIntoEqualSegments(int n)
        {
            if (n <= 0)
            {
                throw new ArgumentException($"The number of divisions must be greater than 0.");
            }
            var lines = new List<Line>();
            var div = 1.0 / n;
            var a = Start;
            var t = div;
            for (var i = 0; i < n - 1; i++)
            {
                var b = PointAt(t);
                lines.Add(new Line(a, b));

                t += div;
                a = b;
            }
            lines.Add(new Line(a, End));
            return lines;
        }

        /// <summary>
        /// Offset the line. The offset direction will be defined by
        /// Direction X Vector3.ZAxis.
        /// </summary>
        /// <param name="distance">The distance to offset.</param>
        /// <param name="flip">Flip the offset direction.</param>
        /// <returns></returns>
        public Line Offset(double distance, bool flip)
        {
            var offsetVector = this.Direction().Cross(Vector3.ZAxis);
            if (flip)
            {
                offsetVector = offsetVector.Negate();
            }
            var a = Start + offsetVector * distance;
            var b = End + offsetVector * distance;
            return new Line(a, b);
        }

        /// <summary>
        /// Trim this line to the trimming curve.
        /// </summary>
        /// <param name="line">The curve to which to trim.</param>
        /// <param name="flip">Should the trim direction be reversed?</param>
        /// <returns>A new line, or null if this line does not intersect the trimming line.</returns>
        public Line TrimTo(Line line, bool flip = false)
        {
            if (this.Intersects(line, out Vector3 result, true))
            {
                if (flip)
                {
                    return new Line(result, this.End);
                }
                else
                {
                    return new Line(this.Start, result);
                }
            }

            return null;
        }

        /// <summary>
        /// Extend this line to the trimming curve.
        /// </summary>
        /// <param name="line">The curve to which to extend.</param>
        /// <returns>A new line, or null if these lines would never intersect if extended infinitely.</returns>
        public Line ExtendTo(Line line)
        {
            if (!Intersects(line, out var intersection, true))
            {
                return null;
            }
            if (intersection.DistanceTo(Start) > intersection.DistanceTo(End))
            {
                return new Line(this.Start, intersection);
            }
            else
            {
                return new Line(this.End, intersection);
            }
        }

        /// <summary>
        /// Extend this line to its (nearest, by default) intersection with any other line.
        /// If optional `extendToFurthest` is true, extends to furthest intersection with any other line.
        /// </summary>
        /// <param name="otherLines">The other lines to intersect with</param>
        /// <param name="bothSides">Optional — if false, will only extend in the line's direction; if true will extend in both directions.</param>
        /// <param name="extendToFurthest">Optional — if true, will extend line as far as it will go, rather than stopping at the closest intersection.</param>
        /// <param name="tolerance">Optional — The amount of tolerance to include in the extension method.</param>
        public Line ExtendTo(IEnumerable<Line> otherLines, bool bothSides = true, bool extendToFurthest = false, double tolerance = Vector3.EPSILON)
        {
            return ExtendTo(otherLines, double.MaxValue, bothSides, extendToFurthest, tolerance);
        }

        /// <summary>
        /// Extend this line to its (nearest, by default) intersection with any other line, but no further than maxDistance.
        /// If optional `extendToFurthest` is true, extends to furthest intersection with any other line, but no further than maxDistance.
        /// If the distance to the intersection with the lines is greater than the maximum, the line will be returned unchanged.
        /// </summary>
        /// <param name="otherLines">The other lines to intersect with.</param>
        /// <param name="maxDistance">Maximum extension distance.</param>
        /// <param name="bothSides">Optional — if false, will only extend in the line's direction; if true will extend in both directions.</param>
        /// <param name="extendToFurthest">Optional — if true, will extend line as far as it will go, rather than stopping at the closest intersection.</param>
        /// <param name="tolerance">Optional — The amount of tolerance to include in the extension method.</param>
        public Line ExtendTo(IEnumerable<Line> otherLines, double maxDistance, bool bothSides = true, bool extendToFurthest = false, double tolerance = Vector3.EPSILON)
        {
            // this test line — inset slightly from the line — helps treat the ends as valid intersection points, to prevent
            // extension beyond an immediate intersection.
            var testLine = new Line(this.PointAt(0.001), this.PointAt(0.999));
            var segments = otherLines;
            var intersectionsForLine = new List<Vector3>();
            foreach (var segment in segments)
            {
                bool pointAdded = false;
                // Special case for parallel + collinear lines:
                // ____   |__________
                // We want to extend only to the first corner of the other lines,
                // not all the way through to the other end
                if (segment.Direction().IsParallelTo(testLine.Direction(), tolerance) && // if the two lines are parallel
                    (new[] { segment.End, segment.Start, testLine.Start, testLine.End }).AreCollinear())// and collinear
                {
                    if (!this.PointOnLine(segment.End, true))
                    {
                        intersectionsForLine.Add(segment.End);
                        pointAdded = true;
                    }

                    if (!this.PointOnLine(segment.Start, true))
                    {
                        intersectionsForLine.Add(segment.Start);
                        pointAdded = true;
                    }
                }
                if (extendToFurthest || !pointAdded)
                {
                    var intersects = testLine.Intersects(segment, out Vector3 intersection, true, true);

                    // if the intersection lies on the obstruction, but is beyond the segment, we collect it
                    if (intersects && segment.PointOnLine(intersection, true) && !testLine.PointOnLine(intersection, true))
                    {
                        intersectionsForLine.Add(intersection);
                    }
                }
            }

            var dir = this.Direction();
            var intersectionsOrdered = intersectionsForLine.OrderBy(i => (testLine.Start - i).Dot(dir));

            var start = this.Start;
            var end = this.End;

            var startCandidates = intersectionsOrdered
                    .Where(i => (testLine.Start - i).Dot(dir) > 0)
                    .Cast<Vector3?>();

            var endCandidates = intersectionsOrdered
                .Where(i => (testLine.Start - i).Dot(dir) < testLine.Length() * -1)
                .Reverse().Cast<Vector3?>();

            (Vector3? Start, Vector3? End) startEndCandidates = extendToFurthest ?
                (startCandidates.LastOrDefault(p => p.GetValueOrDefault().DistanceTo(start) < maxDistance), endCandidates.LastOrDefault(p => p.GetValueOrDefault().DistanceTo(end) < maxDistance)) :
                (startCandidates.FirstOrDefault(p => p.GetValueOrDefault().DistanceTo(start) < maxDistance), endCandidates.FirstOrDefault(p => p.GetValueOrDefault().DistanceTo(end) < maxDistance));

            if (bothSides && startEndCandidates.Start != null)
            {
                start = (Vector3)startEndCandidates.Start;
            }
            if (startEndCandidates.End != null)
            {
                end = (Vector3)startEndCandidates.End;
            }

            return new Line(start, end);
        }

        /// <summary>
        /// Extend this line to its (nearest, by default) intersection with a polyline.
        /// </summary>
        /// <param name="polyline">The polyline to intersect with</param>
        /// <param name="bothSides">Optional — if false, will only extend in the line's direction; if true will extend in both directions.</param>
        /// <param name="extendToFurthest">Optional — if true, will extend line as far as it will go, rather than stopping at the closest intersection.</param>
        public Line ExtendTo(Polyline polyline, bool bothSides = true, bool extendToFurthest = false)
        {
            return ExtendTo(polyline.Segments(), bothSides, extendToFurthest);
        }

        /// <summary>
        /// Extend this line to its (nearest, by default) intersection with a polyline, but no further than maxDistance.
        /// </summary>
        /// <param name="polyline">The polyline to intersect with</param>
        /// <param name="maxDistance">Maximum extension distance.</param>
        /// <param name="bothSides">Optional — if false, will only extend in the line's direction; if true will extend in both directions.</param>
        /// <param name="extendToFurthest">Optional — if true, will extend line as far as it will go, rather than stopping at the closest intersection.</param>
        public Line ExtendTo(Polyline polyline, double maxDistance, bool bothSides = true, bool extendToFurthest = false)
        {
            return ExtendTo(polyline.Segments(), maxDistance, bothSides, extendToFurthest);
        }

        /// <summary>
        /// Extend this line to its (nearest, by default) intersection with a profile.
        /// </summary>
        /// <param name="profile">The profile to intersect with</param>
        /// <param name="bothSides">Optional — if false, will only extend in the line's direction; if true will extend in both directions.</param>
        /// <param name="extendToFurthest">Optional — if true, will extend line as far as it will go, rather than stopping at the closest intersection.</param>
        public Line ExtendTo(Profile profile, bool bothSides = true, bool extendToFurthest = false)
        {
            return ExtendTo(profile.Segments(), bothSides, extendToFurthest);
        }

        /// <summary>
        /// Extend this line to its (nearest, by default) intersection with a profile, but no further than maxDistance.
        /// </summary>
        /// <param name="profile">The profile to intersect with</param>
        /// <param name="maxDistance">Maximum extension distance.</param>
        /// <param name="bothSides">Optional — if false, will only extend in the line's direction; if true will extend in both directions.</param>
        /// <param name="extendToFurthest">Optional — if true, will extend line as far as it will go, rather than stopping at the closest intersection.</param>
        public Line ExtendTo(Profile profile, double maxDistance, bool bothSides = true, bool extendToFurthest = false)
        {
            return ExtendTo(profile.Segments(), maxDistance, bothSides, extendToFurthest);
        }

        /// <summary>
        /// Extend this line to its (nearest, by default) intersection with a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to intersect with</param>
        /// <param name="bothSides">Optional — if false, will only extend in the line's direction; if true will extend in both directions.</param>
        /// <param name="extendToFurthest">Optional — if true, will extend line as far as it will go, rather than stopping at the closest intersection.</param>
        /// <param name="tolerance">Optional — The amount of tolerance to include in the extension method.</param>
        public Line ExtendTo(Polygon polygon, bool bothSides = true, bool extendToFurthest = false, double tolerance = Vector3.EPSILON)
        {
            return ExtendTo(polygon.Segments(), bothSides, extendToFurthest, tolerance);
        }

        /// <summary>
        /// Extend this line to its (nearest, by default) intersection with a polygon, but no further than maxDistance.
        /// </summary>
        /// <param name="polygon">The polygon to intersect with</param>
        /// <param name="maxDistance">Maximum extension distance.</param>
        /// <param name="bothSides">Optional — if false, will only extend in the line's direction; if true will extend in both directions.</param>
        /// <param name="extendToFurthest">Optional — if true, will extend line as far as it will go, rather than stopping at the closest intersection.</param>
        /// <param name="tolerance">Optional — The amount of tolerance to include in the extension method.</param>
        public Line ExtendTo(Polygon polygon, double maxDistance, bool bothSides = true, bool extendToFurthest = false, double tolerance = Vector3.EPSILON)
        {
            return ExtendTo(polygon.Segments(), maxDistance, bothSides, extendToFurthest, tolerance);
        }

        /// <summary>
        /// Trim a line with a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to trim with.</param>
        /// <param name="outsideSegments">A list of the segment(s) of the line outside of the supplied polygon.</param>
        /// <param name="includeCoincidenceAtEdge">Include coincidence at edge as inner segment.</param>
        /// <returns>A list of the segment(s) of the line within the supplied polygon.</returns>
        public List<Line> Trim(Polygon polygon, out List<Line> outsideSegments, bool includeCoincidenceAtEdge = false)
        {
            // adapted from http://csharphelper.com/blog/2016/01/clip-a-line-segment-to-a-polygon-in-c/
            // Make lists to hold points of intersection
            var intersections = new List<Vector3>();

            // Add the segment's starting point.
            intersections.Add(this.Start);
            polygon.Contains(this.Start, out var containment);
            var StartsOutsidePolygon = containment == Containment.Outside;

            var hasVertexIntersections = containment == Containment.CoincidesAtVertex;

            // Examine the polygon's edges.
            for (int i1 = 0; i1 < polygon.Vertices.Count; i1++)
            {
                // Get the end points for this edge.
                int i2 = (i1 + 1) % polygon.Vertices.Count;

                // See where the edge intersects the segment.
                var segment = new Line(polygon.Vertices[i1], polygon.Vertices[i2]);
                var segmentsIntersect = Intersects(segment, out Vector3 intersection); // This will return false for intersections exactly at an end

                // See if the segment intersects the edge.
                if (segmentsIntersect)
                {
                    // Record this intersection.
                    intersections.Add(intersection);
                }
                // see if the segment intersects at a vertex
                else if (this.PointOnLine(polygon.Vertices[i1]))
                {
                    intersections.Add(polygon.Vertices[i1]);
                    hasVertexIntersections = true;
                }
            }

            // Add the segment's ending point.
            intersections.Add(End);

            var intersectionsOrdered = intersections.OrderBy(v => v.DistanceTo(Start)).ToArray();
            var inSegments = new List<Line>();
            outsideSegments = new List<Line>();
            var currentlyIn = !StartsOutsidePolygon;
            for (int i = 0; i < intersectionsOrdered.Length - 1; i++)
            {
                var A = intersectionsOrdered[i];
                var B = intersectionsOrdered[i + 1];
                if (A.IsAlmostEqualTo(B)) // skip duplicate points
                {
                    // it's possible that A is outside, but B is at an edge, even 
                    // if they are within tolerance of each other. 
                    // This can happen due to floating point error when the point is almost exactly
                    // epsilon distance from the edge.
                    // so if we have duplicate points, we have to update the containment value.
                    polygon.Contains(B, out containment);
                    continue;
                }
                var segment = new Line(A, B);
                if (hasVertexIntersections || containment == Containment.CoincidesAtEdge) // if it passed through a vertex, or started at an edge or vertex, we can't rely on alternating, so check each midpoint
                {
                    polygon.Contains((A + B) / 2, out var containmentInPolygon);
                    currentlyIn = containmentInPolygon == Containment.Inside;
                    if (includeCoincidenceAtEdge)
                    {
                        currentlyIn = currentlyIn || containmentInPolygon == Containment.CoincidesAtEdge;
                    }
                }
                if (currentlyIn)
                {
                    inSegments.Add(segment);
                }
                else
                {
                    outsideSegments.Add(segment);
                }
                currentlyIn = !currentlyIn;
            }

            return inSegments;
        }

        /// <summary>
        /// Create a fillet arc between this line and the target.
        /// </summary>
        /// <param name="target">The line with which to fillet.</param>
        /// <param name="radius">The radius of the fillet.</param>
        /// <returns>An arc, or null if no fillet can be calculated.</returns>
        public Arc Fillet(Line target, double radius)
        {
            var d1 = this.Direction();
            var d2 = target.Direction();
            if (d1.IsParallelTo(d2))
            {
                throw new Exception("The fillet could not be created. The lines are parallel");
            }

            var r1 = new Ray(this.Start, d1);
            var r2 = new Ray(target.Start, d2);
            if (!r1.Intersects(r2, out Vector3 result, true))
            {
                return null;
            }

            // Construct new vectors that both
            // point away from the projected intersection
            var newD1 = (this.PointAt(0.5) - result).Unitized();
            var newD2 = (target.PointAt(0.5) - result).Unitized();

            var theta = newD1.AngleTo(newD2) * Math.PI / 180.0;
            var halfTheta = theta / 2.0;
            var h = radius / Math.Sin(halfTheta);
            var centerVec = newD1.Average(newD2).Unitized();
            var arcCenter = result + centerVec * h;

            // Find the closest points from the arc
            // center to the adjacent curves.
            var p1 = arcCenter.ClosestPointOn(this);
            var p2 = arcCenter.ClosestPointOn(target);

            // Find the angle of both segments relative to the fillet arc.
            // ATan2 assumes the origin, so correct the coordinates
            // by the offset of the center of the arc.
            var angle1 = Math.Atan2(p1.Y - arcCenter.Y, p1.X - arcCenter.X) * 180.0 / Math.PI;
            var angle2 = Math.Atan2(p2.Y - arcCenter.Y, p2.X - arcCenter.X) * 180.0 / Math.PI;

            // ATan2 will provide negative angles in the "lower" quadrants
            // Ensure that these values are 180d -> 360d
            angle1 = (angle1 + 360) % 360;
            angle2 = (angle2 + 360) % 360;
            angle2 = angle2 == 0.0 ? 360.0 : angle2;

            // We only support CCW wound arcs.
            // For arcs that with start angles <1d, convert
            // the arc back to a negative value.
            var arc = new Arc(arcCenter,
                           radius,
                           angle1 > angle2 ? angle1 - 360.0 : angle1,
                           angle2);

            // Get the complimentary arc and choose
            // the shorter of the two arcs.
            var complement = arc.Complement();
            if (arc.Length() < complement.Length())
            {
                return arc;
            }
            else
            {
                return complement;
            }
        }

        /// <summary>
        /// A list of vertices describing the arc for rendering.
        /// </summary>
        internal override IList<Vector3> RenderVertices()
        {
            return new[] { this.Start, this.End };
        }
    }
}