using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Geometry.Solids;

namespace Elements.Geometry
{
    /// <summary>
    /// An infinite ray starting at origin and pointing towards direction.
    /// </summary>
    public struct Ray : IEquatable<Ray>
    {
        /// <summary>
        /// The origin of the ray.
        /// </summary>
        public Vector3 Origin { get; set; }

        /// <summary>
        /// The direction of the ray.
        /// </summary>
        public Vector3 Direction { get; set; }

        /// <summary>
        /// Construct a ray.
        /// </summary>
        /// <param name="origin">The origin of the ray.</param>
        /// <param name="direction">The direction of the ray.</param>
        public Ray(Vector3 origin, Vector3 direction)
        {
            this.Origin = origin;
            this.Direction = direction;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
        /// </summary>
        /// <param name="tri">The triangle to intersect.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the type and location of intersection.</returns>
        public bool Intersects(Triangle tri, out Vector3 result)
        {
            result = default(Vector3);

            var vertex0 = tri.Vertices[0].Position;
            var vertex1 = tri.Vertices[1].Position;
            var vertex2 = tri.Vertices[2].Position;
            var edge1 = (vertex1 - vertex0);
            var edge2 = (vertex2 - vertex0);
            var h = this.Direction.Cross(edge2);
            var s = this.Origin - vertex0;
            double a, f, u, v;

            a = edge1.Dot(h);
            if (a > -Vector3.EPSILON && a < Vector3.EPSILON)
            {
                return false;    // This ray is parallel to this triangle.
            }
            f = 1.0 / a;
            u = f * (s.Dot(h));
            if (u < 0.0 || u > 1.0)
            {
                return false;
            }
            var q = s.Cross(edge1);
            v = f * this.Direction.Dot(q);
            if (v < 0.0 || u + v > 1.0)
            {
                return false;
            }
            // At this stage we can compute t to find out where the intersection point is on the line.
            double t = f * edge2.Dot(q);
            if (t > Vector3.EPSILON && t < 1 / Vector3.EPSILON) // ray intersection
            {
                result = this.Origin + this.Direction * t;
                return true;
            }
            else // This means that there is a line intersection but not a ray intersection.
            {
                return false;
            }
        }

        /// <summary>
        /// Does this ray intersect with the provided GeometricElement? Only GeometricElements with Solid Representations are currently supported, and voids will be ignored.
        /// </summary>
        /// <param name="element">The element to intersect with.</param>
        /// <param name="result">The list of intersection results.</param>
        /// <returns></returns>
        public bool Intersects(GeometricElement element, out List<Vector3> result)
        {
            if (element.Representation == null || element.Representation.SolidOperations == null || element.Representation.SolidOperations.Count == 0)
            {
                element.UpdateRepresentations();
            }
            List<Vector3> resultsOut = new List<Vector3>();
            var transformFromElement = new Transform(element.Transform);
            transformFromElement.Invert();
            var transformToElement = new Transform(element.Transform);
            var transformedRay = new Ray(transformFromElement.OfPoint(Origin), transformFromElement.OfVector(Direction));
            //TODO: extend to handle voids when void solids in Representations are supported generally
            var intersects = false;
            foreach (var solidOp in element.Representation.SolidOperations.Where(e => !e.IsVoid))
            {
                if (transformedRay.Intersects(solidOp, out List<Vector3> tempResults))
                {
                    intersects = true;
                    resultsOut.AddRange(tempResults.Select(t => transformToElement.OfPoint(t)));
                };
            }
            result = resultsOut;
            return intersects;
        }

        /// <summary>
        /// Does this ray intersect with the provided SolidOperation?
        /// </summary>
        /// <param name="solidOp">The SolidOperation to intersect with.</param>
        /// <param name="result">The list of intersection results, ordered by distance from the ray origin.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the location of the intersection.</returns>
        public bool Intersects(SolidOperation solidOp, out List<Vector3> result)
        {
            var intersects = Intersects(solidOp.Solid, out List<Vector3> tempResult);
            result = tempResult;
            return intersects;
        }

        /// <summary>
        /// Does this ray intersect with the provided Solid?
        /// </summary>
        /// <param name="solid">The Solid to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the location of the intersection.</returns>
        internal bool Intersects(Solid solid, out List<Vector3> result)
        {
            var faces = solid.Faces;
            var intersects = false;
            List<Vector3> results = new List<Vector3>();
            foreach (var face in faces)
            {
                if (Intersects(face.Value, out Vector3 tempResult))
                {
                    intersects = true;
                    results.Add(tempResult);
                }
            }
            var origin = Origin; // lambdas in structs can't refer to their properties
            result = results.OrderBy(r => r.DistanceTo(origin)).ToList();
            return intersects;
        }

        /// <summary>
        /// Does this ray intersect with the provided face?
        /// </summary>
        /// <param name="face">The Face to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the location of the intersection.</returns>
        internal bool Intersects(Face face, out Vector3 result)
        {
            var plane = face.Plane();
            if (Intersects(plane, out Vector3 intersection))
            {
                var boundaryPolygon = face.Outer.ToPolygon();
                var voids = face.Inner?.Select(v => v.ToPolygon())?.ToList();
                var transformToPolygon = new Transform(plane.Origin, plane.Normal);
                var transformFromPolygon = new Transform(transformToPolygon);
                transformFromPolygon.Invert();
                var transformedIntersection = transformFromPolygon.OfPoint(intersection);
                IEnumerable<(Vector3 from, Vector3 to)> curveList = boundaryPolygon.Edges();
                if (voids != null)
                {
                    curveList = curveList.Union(voids.SelectMany(v => v.Edges()));
                }
                curveList = curveList.Select(l => (transformFromPolygon.OfPoint(l.from), transformFromPolygon.OfPoint(l.to)));

                if (Polygon.Contains(curveList, transformedIntersection, out _))
                {
                    result = intersection;
                    return true;
                }
            }
            result = default(Vector3);
            return false;
        }

        /// <summary>
        /// Does this ray intersect the provided polygon area?
        /// </summary>
        /// <param name="polygon">The Polygon to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the location of the intersection.</returns>
        public bool Intersects(Polygon polygon, out Vector3 result)
        {
            var plane = new Plane(polygon.Vertices.First(), polygon.Vertices);
            if (Intersects(plane, out Vector3 intersection))
            {
                var transformToPolygon = new Transform(plane.Origin, plane.Normal);
                var transformFromPolygon = new Transform(transformToPolygon);
                transformFromPolygon.Invert();
                var transformedIntersection = transformFromPolygon.OfPoint(intersection);
                IEnumerable<(Vector3 from, Vector3 to)> curveList = polygon.Edges();
                curveList = curveList.Select(l => (transformFromPolygon.OfPoint(l.from), transformFromPolygon.OfPoint(l.to)));

                if (Polygon.Contains(curveList, transformedIntersection, out _))
                {
                    result = intersection;
                    return true;
                }
            }
            result = default(Vector3);
            return false;
        }

        /// <summary>
        /// Does this ray intersect the provided plane?
        /// </summary>
        /// <param name="plane">The Plane to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false — this can occur if the ray is very close to parallel to the plane.
        /// If true, check the intersection result for the location of the intersection.</returns>
        public bool Intersects(Plane plane, out Vector3 result)
        {
            var doesIntersect = Intersects(plane, out Vector3 resultVector, out _);
            result = resultVector;
            return doesIntersect;
        }

        /// <summary>
        /// Does this ray intersect the provided plane?
        /// </summary>
        /// <param name="plane">The Plane to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <param name="t"></param>
        /// <returns>True if an intersection occurs, otherwise false — this can occur if the ray is very close to parallel to the plane.
        /// If true, check the intersection result for the location of the intersection.</returns>
        public bool Intersects(Plane plane, out Vector3 result, out double t)
        {
            result = default(Vector3);
            t = double.NaN;
            var d = Direction;

            // Test for perpendicular.
            if (plane.Normal.Dot(d) == 0)
            {
                return false;
            }
            t = (plane.Normal.Dot(plane.Origin) - plane.Normal.Dot(Origin)) / plane.Normal.Dot(d);

            // If t < 0, the point of intersection is behind
            // the start of the ray.
            if (t < 0)
            {
                return false;
            }
            result = Origin + d * t;
            return true;
        }

        /// <summary>
        /// Does this ray intersect the provided topography?
        /// </summary>
        /// <param name="topo">The topography.</param>
        /// <param name="result">The location of intersection.</param>
        /// <returns>True if an intersection result occurs.
        /// False if no intersection occurs.</returns>
        public bool Intersects(Topography topo, out Vector3 result)
        {
            return Intersects(topo.Mesh, out result);
        }

        /// <summary>
        /// Does this ray intersect the provided mesh?
        /// </summary>
        /// <param name="mesh">The Mesh.</param>
        /// <param name="result">The location of intersection.</param>
        /// <returns>True if an intersection result occurs.
        /// False if no intersection occurs.</returns>

        public bool Intersects(Mesh mesh, out Vector3 result)
        {
            result = default;
            foreach (var t in mesh.Triangles)
            {
                if (this.Intersects(t, out result))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Does this ray intersect the provided ray?
        /// </summary>
        /// <param name="ray">The ray to intersect.</param>
        /// <param name="result">The location of intersection.</param>
        /// <param name="ignoreRayDirection">If true, the direction of the rays will be ignored</param>
        /// <returns>True if the rays intersect, otherwise false.</returns>
        public bool Intersects(Ray ray, out Vector3 result, bool ignoreRayDirection = false)
        {
            var p1 = this.Origin;
            var p2 = ray.Origin;
            var d1 = this.Direction;
            var d2 = ray.Direction;

            if (d1.IsParallelTo(d2))
            {
                result = default(Vector3);
                return false;
            }

            var t1 = (((p2 - p1).Cross(d2)).Dot(d1.Cross(d2))) / Math.Pow(d1.Cross(d2).Length(), 2);
            var t2 = (((p2 - p1).Cross(d1)).Dot(d1.Cross(d2))) / Math.Pow(d1.Cross(d2).Length(), 2);
            result = p1 + d1 * t1;
            if (ignoreRayDirection)
            {
                return true;
            }
            return t1 >= 0 && t2 >= 0;
        }

        /// <summary>
        /// Does this ray intersect the provided line?
        /// </summary>
        /// <param name="line">The line to intersect.</param>
        /// <param name="result">The location of intersection.</param>
        /// <returns>True if the rays intersect, otherwise false.</returns>
        public bool Intersects(Line line, out Vector3 result)
        {
            var otherRay = new Ray(line.Start, line.Direction());
            if (Intersects(otherRay, out Vector3 rayResult))
            {
                // Quick out if the result is exactly at the 
                // start or the end of the line.
                if (rayResult.IsAlmostEqualTo(line.Start) || rayResult.IsAlmostEqualTo(line.End))
                {
                    result = rayResult;
                    return true;
                }
                else if ((rayResult - line.Start).Length() > line.Length())
                {
                    result = default(Vector3);
                    return false;
                }
                else
                {
                    result = rayResult;
                    return true;
                }
            }
            result = default(Vector3);
            return false;
        }

        /// <summary>
        /// Is this ray equal to the provided ray?
        /// </summary>
        /// <param name="other">The ray to test.</param>
        /// <returns>Returns true if the two rays are equal, otherwise false.</returns>
        public bool Equals(Ray other)
        {
            return this.Origin.Equals(other.Origin) && this.Direction.Equals(other.Direction);
        }
    }
}