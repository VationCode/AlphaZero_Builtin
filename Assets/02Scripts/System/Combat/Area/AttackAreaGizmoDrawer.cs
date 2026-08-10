using UnityEngine;

namespace Alpha.Combat
{
    // AttackAreaSettings를 Scene View에서 실제 판정 크기와 동일하게 그린다.
    public static class AttackAreaGizmoDrawer
    {
        private const int CircleSegments = 48;

        public static void Draw(
            Transform p_origin,
            AttackAreaSettings p_settings,
            Color p_color)
        {
            if (p_origin == null ||
                p_settings == null ||
                !p_settings.IsValid)
            {
                return;
            }

            AttackAreaRequest request = new(
                p_origin.position,
                p_origin.forward,
                p_origin.up,
                p_origin,
                p_settings);

            Draw(request, p_color);
        }

        public static void Draw(
            in AttackAreaRequest p_request,
            Color p_color)
        {
            if (!p_request.IsValid)
                return;

            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.color = p_color;

            switch (p_request.Settings.Shape)
            {
                case EAttackAreaShape.ForwardBox:
                    DrawBox(p_request);
                    break;

                case EAttackAreaShape.ForwardSector:
                    DrawSector(p_request);
                    break;

                case EAttackAreaShape.Radial:
                    DrawRadial(p_request);
                    break;
            }

            DrawForwardArrow(p_request);

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private static void DrawBox(
            in AttackAreaRequest p_request)
        {
            AttackAreaSettings settings = p_request.Settings;
            Vector3 center = p_request.AreaOrigin +
                             p_request.Forward *
                             (settings.Length * 0.5f);

            Gizmos.matrix = Matrix4x4.TRS(
                center,
                p_request.Rotation,
                Vector3.one);

            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(
                    settings.Width,
                    settings.Height,
                    settings.Length));

            Gizmos.matrix = Matrix4x4.identity;
        }

        private static void DrawSector(
            in AttackAreaRequest p_request)
        {
            AttackAreaSettings settings = p_request.Settings;
            float halfHeight = settings.Height * 0.5f;
            float halfAngle = settings.Angle * 0.5f;

            Vector3 bottomCenter =
                p_request.AreaOrigin - p_request.Up * halfHeight;
            Vector3 topCenter =
                p_request.AreaOrigin + p_request.Up * halfHeight;

            DrawArc(
                bottomCenter,
                p_request.Forward,
                p_request.Up,
                settings.Radius,
                -halfAngle,
                halfAngle);

            DrawArc(
                topCenter,
                p_request.Forward,
                p_request.Up,
                settings.Radius,
                -halfAngle,
                halfAngle);

            Vector3 leftDirection =
                Quaternion.AngleAxis(-halfAngle, p_request.Up) *
                p_request.Forward;

            Vector3 rightDirection =
                Quaternion.AngleAxis(halfAngle, p_request.Up) *
                p_request.Forward;

            Vector3 bottomLeft =
                bottomCenter + leftDirection * settings.Radius;
            Vector3 bottomRight =
                bottomCenter + rightDirection * settings.Radius;
            Vector3 topLeft =
                topCenter + leftDirection * settings.Radius;
            Vector3 topRight =
                topCenter + rightDirection * settings.Radius;

            Gizmos.DrawLine(bottomCenter, bottomLeft);
            Gizmos.DrawLine(bottomCenter, bottomRight);
            Gizmos.DrawLine(topCenter, topLeft);
            Gizmos.DrawLine(topCenter, topRight);
            Gizmos.DrawLine(bottomCenter, topCenter);
            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(bottomRight, topRight);
        }

        private static void DrawRadial(
            in AttackAreaRequest p_request)
        {
            AttackAreaSettings settings = p_request.Settings;
            float halfHeight = settings.Height * 0.5f;

            Vector3 bottomCenter =
                p_request.AreaOrigin - p_request.Up * halfHeight;
            Vector3 topCenter =
                p_request.AreaOrigin + p_request.Up * halfHeight;

            DrawArc(
                bottomCenter,
                p_request.Forward,
                p_request.Up,
                settings.Radius,
                0f,
                360f);

            DrawArc(
                topCenter,
                p_request.Forward,
                p_request.Up,
                settings.Radius,
                0f,
                360f);

            Vector3 right = p_request.Rotation * Vector3.right;

            DrawVerticalEdge(
                bottomCenter,
                topCenter,
                p_request.Forward * settings.Radius);
            DrawVerticalEdge(
                bottomCenter,
                topCenter,
                -p_request.Forward * settings.Radius);
            DrawVerticalEdge(
                bottomCenter,
                topCenter,
                right * settings.Radius);
            DrawVerticalEdge(
                bottomCenter,
                topCenter,
                -right * settings.Radius);
        }

        private static void DrawArc(
            Vector3 p_center,
            Vector3 p_forward,
            Vector3 p_up,
            float p_radius,
            float p_startAngle,
            float p_endAngle)
        {
            Vector3 previousPoint = p_center +
                                    Quaternion.AngleAxis(
                                        p_startAngle,
                                        p_up) *
                                    p_forward * p_radius;

            for (int index = 1; index <= CircleSegments; index++)
            {
                float progress = index / (float)CircleSegments;
                float angle = Mathf.Lerp(
                    p_startAngle,
                    p_endAngle,
                    progress);

                Vector3 currentPoint = p_center +
                                       Quaternion.AngleAxis(angle, p_up) *
                                       p_forward * p_radius;

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        private static void DrawVerticalEdge(
            Vector3 p_bottomCenter,
            Vector3 p_topCenter,
            Vector3 p_offset)
        {
            Gizmos.DrawLine(
                p_bottomCenter + p_offset,
                p_topCenter + p_offset);
        }

        private static void DrawForwardArrow(
            in AttackAreaRequest p_request)
        {
            float length = p_request.Settings.Shape ==
                           EAttackAreaShape.ForwardBox
                ? p_request.Settings.Length
                : p_request.Settings.Radius;

            length = Mathf.Max(0.5f, length);

            Vector3 start = p_request.AreaOrigin;
            Vector3 end = start + p_request.Forward * length;
            Vector3 right = p_request.Rotation * Vector3.right;
            float headLength = Mathf.Min(0.3f, length * 0.2f);

            Gizmos.DrawLine(start, end);
            Gizmos.DrawLine(
                end,
                end - p_request.Forward * headLength +
                right * headLength * 0.5f);
            Gizmos.DrawLine(
                end,
                end - p_request.Forward * headLength -
                right * headLength * 0.5f);
        }
    }
}
