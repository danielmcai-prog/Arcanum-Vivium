using System;
using System.Collections.Generic;
using System.Linq;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Core
{
    public static class FeatureExtractor
    {
        public static SpellFeatures? Extract(IReadOnlyList<IReadOnlyList<Point2>> strokes)
        {
            if (strokes == null)
            {
                return null;
            }

            var allInputPoints = strokes.SelectMany(s => s).ToList();
            if (allInputPoints.Count < 2)
            {
                return null;
            }

            var simplified = strokes.Select(s => Rdp(s, 2f)).ToList();
            var normalized = Normalize(simplified);
            var all = normalized.SelectMany(s => s).ToList();

            if (all.Count < 2)
            {
                return null;
            }

            var symmetry = Symmetry(all);
            var enclosure = Enclosure(normalized);
            var spirality = Spirality(all);
            var angularity = Angularity(normalized);
            var totalLen = PathLength(normalized);
            var complexity = MathF.Min(1f, totalLen / 400f);
            var pointCount = DetectPoints(all);
            var dirBias = DirectionBias(all);
            var intersections = IntersectionCount(normalized);
            var strokeCount = normalized.Count;

            var xs = all.Select(p => p.X).ToList();
            var ys = all.Select(p => p.Y).ToList();
            var width = MathF.Max(xs.Max() - xs.Min(), 1f);
            var height = MathF.Max(ys.Max() - ys.Min(), 1f);
            var aspectRatio = width / height;
            var compactness = Compactness(all);

            return new SpellFeatures
            {
                Symmetry = symmetry,
                Enclosure = enclosure,
                Spirality = spirality,
                Angularity = angularity,
                Complexity = complexity,
                PointCount = pointCount,
                DirBias = dirBias,
                Intersections = intersections,
                StrokeCount = strokeCount,
                AspectRatio = aspectRatio,
                Compactness = compactness,
                NormalizedStrokes = normalized.Select(s => s.ToList()).ToList()
            };
        }

        public static List<Point2> Rdp(IReadOnlyList<Point2> points, float epsilon = 2f)
        {
            if (points.Count < 3)
            {
                return points.ToList();
            }

            var maxDist = 0f;
            var idx = 0;
            var end = points.Count - 1;

            for (var i = 1; i < end; i++)
            {
                var d = PerpendicularDistance(points[i], points[0], points[end]);
                if (d > maxDist)
                {
                    maxDist = d;
                    idx = i;
                }
            }

            if (maxDist <= epsilon)
            {
                return new List<Point2> { points[0], points[end] };
            }

            var left = Rdp(points.Take(idx + 1).ToList(), epsilon);
            var right = Rdp(points.Skip(idx).ToList(), epsilon);

            var merged = new List<Point2>(left.Count + right.Count - 1);
            merged.AddRange(left.Take(left.Count - 1));
            merged.AddRange(right);
            return merged;
        }

        private static float PerpendicularDistance(Point2 p, Point2 a, Point2 b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            if (MathF.Abs(dx) < 0.0001f && MathF.Abs(dy) < 0.0001f)
            {
                return Hypot(p.X - a.X, p.Y - a.Y);
            }

            var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            var px = a.X + t * dx;
            var py = a.Y + t * dy;
            return Hypot(p.X - px, p.Y - py);
        }

        private static List<List<Point2>> Normalize(IReadOnlyList<List<Point2>> strokes)
        {
            var all = strokes.SelectMany(s => s).ToList();
            if (all.Count == 0)
            {
                return strokes.Select(s => s.ToList()).ToList();
            }

            var xs = all.Select(p => p.X).ToList();
            var ys = all.Select(p => p.Y).ToList();
            var minX = xs.Min();
            var maxX = xs.Max();
            var minY = ys.Min();
            var maxY = ys.Max();
            var cx = (minX + maxX) / 2f;
            var cy = (minY + maxY) / 2f;
            var scale = MathF.Max(maxX - minX, maxY - minY);
            if (scale < 0.0001f)
            {
                scale = 1f;
            }

            return strokes
                .Select(s => s
                    .Select(p => new Point2(((p.X - cx) / scale) * 64f, ((p.Y - cy) / scale) * 64f))
                    .ToList())
                .ToList();
        }

        private static float Symmetry(IReadOnlyList<Point2> points)
        {
            if (points.Count == 0)
            {
                return 0f;
            }

            var score = 0f;
            foreach (var p in points)
            {
                var mirror = new Point2(-p.X, p.Y);
                var nearest = float.MaxValue;
                foreach (var q in points)
                {
                    var d = Hypot(q.X - mirror.X, q.Y - mirror.Y);
                    if (d < nearest)
                    {
                        nearest = d;
                    }
                }

                score += MathF.Max(0f, 1f - nearest / 20f);
            }

            return score / points.Count;
        }

        private static float Enclosure(IReadOnlyList<List<Point2>> strokes)
        {
            foreach (var stroke in strokes)
            {
                if (stroke.Count < 4)
                {
                    continue;
                }

                var first = stroke[0];
                var last = stroke[^1];
                if (Hypot(first.X - last.X, first.Y - last.Y) < 12f)
                {
                    return 1f;
                }
            }

            if (strokes.Count > 1)
            {
                var all = strokes.SelectMany(s => s).ToList();
                if (all.Count > 1)
                {
                    var first = all[0];
                    var last = all[^1];
                    if (Hypot(first.X - last.X, first.Y - last.Y) < 15f)
                    {
                        return 0.7f;
                    }
                }
            }

            return 0f;
        }

        private static float Spirality(IReadOnlyList<Point2> points)
        {
            if (points.Count < 6)
            {
                return 0f;
            }

            var angles = new List<float>(points.Count - 1);
            for (var i = 1; i < points.Count; i++)
            {
                angles.Add(MathF.Atan2(points[i].Y - points[i - 1].Y, points[i].X - points[i - 1].X));
            }

            var totalTurn = 0f;
            for (var i = 1; i < angles.Count; i++)
            {
                var d = angles[i] - angles[i - 1];
                if (d > MathF.PI)
                {
                    d -= 2f * MathF.PI;
                }

                if (d < -MathF.PI)
                {
                    d += 2f * MathF.PI;
                }

                totalTurn += d;
            }

            return MathF.Min(1f, MathF.Abs(totalTurn) / (2f * MathF.PI * 2f));
        }

        private static float Angularity(IReadOnlyList<List<Point2>> strokes)
        {
            var sharp = 0;
            var total = 0;

            foreach (var stroke in strokes)
            {
                for (var i = 1; i < stroke.Count - 1; i++)
                {
                    var a = MathF.Atan2(stroke[i].Y - stroke[i - 1].Y, stroke[i].X - stroke[i - 1].X);
                    var b = MathF.Atan2(stroke[i + 1].Y - stroke[i].Y, stroke[i + 1].X - stroke[i].X);
                    var diff = MathF.Abs(a - b);
                    if (diff > MathF.PI)
                    {
                        diff = 2f * MathF.PI - diff;
                    }

                    if (diff > 0.5f)
                    {
                        sharp++;
                    }

                    total++;
                }
            }

            return total == 0 ? 0f : (float)sharp / total;
        }

        private static float PathLength(IReadOnlyList<List<Point2>> strokes)
        {
            var total = 0f;
            foreach (var stroke in strokes)
            {
                for (var i = 1; i < stroke.Count; i++)
                {
                    total += Hypot(stroke[i].X - stroke[i - 1].X, stroke[i].Y - stroke[i - 1].Y);
                }
            }

            return total;
        }

        private static int DetectPoints(IReadOnlyList<Point2> points)
        {
            if (points.Count < 5)
            {
                return 0;
            }

            var extrema = 0;
            for (var i = 1; i < points.Count - 1; i++)
            {
                var d1 = Hypot(points[i].X, points[i].Y);
                var d0 = Hypot(points[i - 1].X, points[i - 1].Y);
                var d2 = Hypot(points[i + 1].X, points[i + 1].Y);
                if (d1 > d0 && d1 > d2 && d1 > 15f)
                {
                    extrema++;
                }
            }

            return Math.Min(extrema, 12);
        }

        private static float DirectionBias(IReadOnlyList<Point2> points)
        {
            if (points.Count == 0)
            {
                return 0f;
            }

            var cx = points.Average(p => p.X);
            var cy = points.Average(p => p.Y);
            return MathF.Atan2(cy, cx) / MathF.PI;
        }

        private static int IntersectionCount(IReadOnlyList<List<Point2>> strokes)
        {
            var count = 0;
            var segments = new List<(Point2 A, Point2 B)>();
            foreach (var stroke in strokes)
            {
                for (var i = 0; i < stroke.Count - 1; i++)
                {
                    segments.Add((stroke[i], stroke[i + 1]));
                }
            }

            for (var i = 0; i < segments.Count; i++)
            {
                for (var j = i + 2; j < segments.Count; j++)
                {
                    if (SegmentsIntersect(segments[i].A, segments[i].B, segments[j].A, segments[j].B))
                    {
                        count++;
                    }
                }
            }

            return Math.Min(count, 20);
        }

        private static bool SegmentsIntersect(Point2 a, Point2 b, Point2 c, Point2 d)
        {
            var det = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);
            if (MathF.Abs(det) < 0.001f)
            {
                return false;
            }

            var t = ((c.X - a.X) * (d.Y - c.Y) - (c.Y - a.Y) * (d.X - c.X)) / det;
            var u = -((a.X - c.X) * (b.Y - a.Y) - (a.Y - c.Y) * (b.X - a.X)) / det;
            return t > 0.01f && t < 0.99f && u > 0.01f && u < 0.99f;
        }

        private static float Compactness(IReadOnlyList<Point2> points)
        {
            if (points.Count < 3)
            {
                return 0f;
            }

            var xs = points.Select(p => p.X).ToList();
            var ys = points.Select(p => p.Y).ToList();
            var area = (xs.Max() - xs.Min()) * (ys.Max() - ys.Min());
            var perimeter = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                perimeter += Hypot(points[i].X - points[i - 1].X, points[i].Y - points[i - 1].Y);
            }

            if (perimeter <= 0.0001f)
            {
                return 0f;
            }

            return MathF.Min(1f, (4f * MathF.PI * area) / (perimeter * perimeter));
        }

        private static float Hypot(float x, float y)
        {
            return MathF.Sqrt(x * x + y * y);
        }
    }
}
