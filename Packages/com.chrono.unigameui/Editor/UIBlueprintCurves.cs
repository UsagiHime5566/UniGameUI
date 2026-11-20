using System.Collections.Generic;
using UnityEngine;

namespace UniGameUI.Editor
{
    /// <summary>一段三次貝茲曲線：起點、兩個控制點、終點。</summary>
    internal readonly struct UIBlueprintSegment
    {
        public UIBlueprintSegment(Vector2 start, Vector2 control1, Vector2 control2, Vector2 end)
        {
            Start = start;
            Control1 = control1;
            Control2 = control2;
            End = end;
        }

        public Vector2 Start { get; }
        public Vector2 Control1 { get; }
        public Vector2 Control2 { get; }
        public Vector2 End { get; }

        public Vector2 Evaluate(float t)
        {
            var u = 1f - t;
            return u * u * u * Start + 3f * u * u * t * Control1 + 3f * u * t * t * Control2 + t * t * t * End;
        }
    }

    /// <summary>
    /// 藍圖上的線。線從來源節點右側的連接點出發，依序穿過每一個中繼點，進入目的地節點的左側；
    /// 每一段是一條三次貝茲曲線，經過中繼點時兩邊的切線方向相同，所以沒有尖角。
    /// 這裡的座標單位都一樣（藍圖用畫布座標），和縮放無關。
    /// </summary>
    internal static class UIBlueprintCurves
    {
        /// <summary>沒有中繼點時，控制點離兩端的最短距離。</summary>
        public const float MinReach = 60f;

        /// <summary>
        /// 回頭線（目的地在來源左邊）的控制點距離上限。沒有上限時距離跟著水平距離變大，
        /// 線會往右拉出很大的弧再繞回來，跑出範圍框。
        /// </summary>
        public const float BackwardReach = 120f;

        /// <summary>判斷距離與找線長時，每一段切成幾小段。</summary>
        public const int SamplesPerSegment = 32;

        // 線離開連接點、進入目的地時，至少水平走這麼遠，線才會從連接點直直出去。
        private const float EndHandle = 30f;

        /// <summary>沒有中繼點時，控制點離兩端的距離。</summary>
        public static float Reach(Vector2 from, Vector2 to)
        {
            var reach = Mathf.Max(MinReach, Mathf.Abs(from.x - to.x) * 0.45f);
            return to.x < from.x ? Mathf.Min(reach, BackwardReach) : reach;
        }

        /// <summary>從 <paramref name="from"/> 依序穿過 <paramref name="waypoints"/> 到 <paramref name="to"/> 的整條線。</summary>
        public static List<UIBlueprintSegment> Path(Vector2 from, Vector2 to, IReadOnlyList<Vector2> waypoints)
        {
            var path = new List<UIBlueprintSegment>();
            if (waypoints == null || waypoints.Count == 0)
            {
                var reach = Reach(from, to);
                path.Add(new UIBlueprintSegment(from, from + Vector2.right * reach, to - Vector2.right * reach, to));
                return path;
            }

            var points = new List<Vector2> { from };
            points.AddRange(waypoints);
            points.Add(to);
            // 兩端水平；中繼點的切線是前後兩點連線的方向（Catmull-Rom），兩邊相同才不會有尖角。
            var tangents = new Vector2[points.Count];
            tangents[0] = Vector2.right;
            tangents[points.Count - 1] = Vector2.right;
            for (var index = 1; index < points.Count - 1; index++)
            {
                var direction = points[index + 1] - points[index - 1];
                tangents[index] = direction.sqrMagnitude < 1e-6f ? Vector2.right : direction.normalized;
            }

            for (var index = 0; index < points.Count - 1; index++)
            {
                var start = points[index];
                var end = points[index + 1];
                var distance = Vector2.Distance(start, end);
                var startHandle = index == 0 ? Mathf.Max(distance * 0.4f, Mathf.Min(EndHandle, distance)) : distance / 3f;
                var endHandle = index == points.Count - 2
                    ? Mathf.Max(distance * 0.4f, Mathf.Min(EndHandle, distance))
                    : distance / 3f;
                path.Add(new UIBlueprintSegment(start, start + tangents[index] * startHandle,
                    end - tangents[index + 1] * endHandle, end));
            }
            return path;
        }

        /// <summary>把線切成折線：每一段 <see cref="SamplesPerSegment"/> 小段，包含兩端。</summary>
        public static List<Vector2> Sample(IReadOnlyList<UIBlueprintSegment> path, int perSegment = SamplesPerSegment)
        {
            var points = new List<Vector2>();
            if (path.Count == 0)
                return points;
            points.Add(path[0].Start);
            foreach (var segment in path)
            {
                for (var step = 1; step <= perSegment; step++)
                    points.Add(segment.Evaluate(step / (float)perSegment));
            }
            return points;
        }

        /// <summary>一點到線的最短距離。<paramref name="segment"/> 是最近的那一段（第幾段）。</summary>
        public static float DistanceTo(IReadOnlyList<UIBlueprintSegment> path, Vector2 point, out int segment)
        {
            segment = -1;
            var best = float.MaxValue;
            for (var index = 0; index < path.Count; index++)
            {
                var previous = path[index].Start;
                for (var step = 1; step <= SamplesPerSegment; step++)
                {
                    var next = path[index].Evaluate(step / (float)SamplesPerSegment);
                    var distance = DistanceToSegment(point, previous, next);
                    if (distance < best)
                    {
                        best = distance;
                        segment = index;
                    }
                    previous = next;
                }
            }
            return best;
        }

        /// <summary>
        /// 在線上的這一點新增中繼點時，它在中繼點清單裡的位置：第 k 段介於第 k - 1 與第 k 個中繼點之間，所以插在 k。
        /// </summary>
        public static int InsertionIndex(IReadOnlyList<UIBlueprintSegment> path, Vector2 point)
        {
            DistanceTo(path, point, out var segment);
            return Mathf.Max(0, segment);
        }

        /// <summary>整條線的長度。</summary>
        public static float Length(IReadOnlyList<UIBlueprintSegment> path)
        {
            var points = Sample(path);
            var length = 0f;
            for (var index = 1; index < points.Count; index++)
                length += Vector2.Distance(points[index - 1], points[index]);
            return length;
        }

        /// <summary>從起點沿著線走 <paramref name="distance"/> 之後的位置。</summary>
        public static Vector2 PointAt(IReadOnlyList<UIBlueprintSegment> path, float distance)
        {
            var points = Sample(path);
            if (points.Count == 0)
                return Vector2.zero;
            var travelled = 0f;
            for (var index = 1; index < points.Count; index++)
            {
                var length = Vector2.Distance(points[index - 1], points[index]);
                if (travelled + length >= distance && length > 0f)
                    return Vector2.Lerp(points[index - 1], points[index], (distance - travelled) / length);
                travelled += length;
            }
            return points[points.Count - 1];
        }

        /// <summary>整條線長度的中間：線上的動作標籤放在這裡。</summary>
        public static Vector2 Midpoint(IReadOnlyList<UIBlueprintSegment> path)
        {
            return PointAt(path, Length(path) * 0.5f);
        }

        public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var segment = b - a;
            var length = segment.sqrMagnitude;
            var t = length <= 0f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, segment) / length);
            return Vector2.Distance(point, a + segment * t);
        }
    }
}
