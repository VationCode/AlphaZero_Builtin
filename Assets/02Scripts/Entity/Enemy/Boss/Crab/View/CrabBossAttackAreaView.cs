using System.Collections.Generic;
using Alpha.Detection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.Enemy.Boss
{
    // 공격 Flow가 전달한 선택 범위와 타격 범위를 ID별 지면 표시로 그린다.
    [DisallowMultipleComponent]
    public sealed class CrabBossAttackAreaView : MonoBehaviour
    {
        private const int ArcSegments = 32;
        private const int RadialSegments = 3;
        private const int BoxWidthSegments = 4;
        private const int BoxLengthSegments = 6;
        private const int RingVertices = ArcSegments + 1;
        private const float ProbeHeight = 50f;
        private const float ProbeDistance = 200f;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int RevealProgressProperty = Shader.PropertyToID("_RevealProgress");
        private static readonly int RevealExtentProperty = Shader.PropertyToID("_RevealExtent");
        private static readonly int RevealBoxProperty = Shader.PropertyToID("_RevealBox");

        private enum MeshShape
        {
            SolidPolar,
            HollowPolar,
            Box
        }

        private sealed class Preview
        {
            public GameObject Object;
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock PropertyBlock;
            public Vector3[] Vertices;
            public MeshShape Topology;
            public EDetectionAreaShape SourceShape;
            public bool HasTopology;
            public bool HasGeometry;
            public Vector3 Origin;
            public Quaternion Rotation;
            public float Radius;
            public float Angle;
            public float Width;
            public float Length;
            public float InnerRadius;
            public float FallbackY;
            public int GroundMask;
            public float GroundOffset;
        }

        [SerializeField] private Material _material;
        [SerializeField] private LayerMask _groundMask = 1 << 0;
        [SerializeField, Min(0f)] private float _groundOffset = 0.05f;

        private readonly Dictionary<int, Preview> _previews = new();

        // 실제 타격 예고와 상시 선택 범위를 같은 형태 데이터로 그린다.
        // innerRadius를 주면 원형 진동파처럼 안쪽이 빈 장판을 그린다.
        public bool Show(int id, in DetectionAreaRequest request, float innerRadius = 0f,
            Color? color = null, float revealProgress = 1f, int sortingOrder = 0)
        {
            if (!isActiveAndEnabled || _material == null || !request.IsValid)
                return false;

            DetectionAreaSettings settings = request.Settings;
            if (settings.Shape != EDetectionAreaShape.ForwardSector &&
                settings.Shape != EDetectionAreaShape.Radial &&
                settings.Shape != EDetectionAreaShape.ForwardBox)
            {
                return false;
            }

            if (innerRadius < 0f ||
                (settings.Shape == EDetectionAreaShape.ForwardBox && innerRadius > 0f) ||
                (settings.Shape != EDetectionAreaShape.ForwardBox &&
                 innerRadius >= settings.Radius))
            {
                return false;
            }

            if (!_previews.TryGetValue(id, out Preview preview))
            {
                preview = CreatePreview(id);
                _previews.Add(id, preview);
            }

            UpdatePreview(preview, request, innerRadius);
            preview.Renderer.sortingOrder = sortingOrder;
            revealProgress = Mathf.Clamp01(revealProgress);
            if (color.HasValue || revealProgress < 1f)
            {
                preview.PropertyBlock.Clear();
                if (color.HasValue)
                    preview.PropertyBlock.SetColor(ColorProperty, color.Value);
                preview.PropertyBlock.SetFloat(RevealProgressProperty, revealProgress);
                preview.PropertyBlock.SetFloat(RevealExtentProperty,
                    request.Settings.Shape == EDetectionAreaShape.ForwardBox
                        ? request.Settings.Length : request.Settings.Radius);
                preview.PropertyBlock.SetFloat(RevealBoxProperty,
                    request.Settings.Shape == EDetectionAreaShape.ForwardBox ? 1f : 0f);
                preview.Renderer.SetPropertyBlock(preview.PropertyBlock);
            }
            else
            {
                preview.Renderer.SetPropertyBlock(null);
            }

            preview.Object.SetActive(true);
            return true;
        }

        public void Hide(int id)
        {
            if (!_previews.TryGetValue(id, out Preview preview))
                return;

            preview.Object.SetActive(false);
            preview.HasGeometry = false;
        }

        public void HideAll()
        {
            foreach (Preview preview in _previews.Values)
            {
                preview.Object.SetActive(false);
                preview.HasGeometry = false;
            }
        }

        private Preview CreatePreview(int id)
        {
            Preview preview = new()
            {
                Object = new GameObject($"Crab Attack Area {id}"),
                Mesh = new Mesh { name = $"Crab Attack Area {id}" },
                PropertyBlock = new MaterialPropertyBlock()
            };

            preview.Mesh.MarkDynamic();
            MeshFilter filter = preview.Object.AddComponent<MeshFilter>();
            MeshRenderer renderer = preview.Object.AddComponent<MeshRenderer>();
            preview.Renderer = renderer;
            filter.sharedMesh = preview.Mesh;
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            preview.Object.SetActive(false);
            return preview;
        }

        private void UpdatePreview(Preview preview, in DetectionAreaRequest request,
            float innerRadius)
        {
            Vector3 forward = Vector3.ProjectOnPlane(request.Forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            Vector3 origin = request.AreaOrigin;
            DetectionAreaSettings settings = request.Settings;
            MeshShape topology = settings.Shape == EDetectionAreaShape.ForwardBox
                ? MeshShape.Box
                : innerRadius > 0f ? MeshShape.HollowPolar : MeshShape.SolidPolar;

            if (preview.HasGeometry && preview.Topology == topology &&
                preview.SourceShape == settings.Shape &&
                preview.Origin == origin && preview.Rotation == rotation &&
                preview.Radius == settings.Radius && preview.Angle == settings.Angle &&
                preview.Width == settings.Width && preview.Length == settings.Length &&
                preview.InnerRadius == innerRadius && preview.FallbackY == request.Origin.y &&
                preview.GroundMask == _groundMask.value &&
                preview.GroundOffset == _groundOffset)
            {
                return;
            }

            if (!preview.HasTopology || preview.Topology != topology)
                BuildTopology(preview, topology);

            preview.Object.transform.SetPositionAndRotation(origin, rotation);

            if (topology == MeshShape.Box)
                UpdateBoxVertices(preview, request, rotation);
            else
                UpdatePolarVertices(preview, request, rotation, innerRadius,
                    topology == MeshShape.HollowPolar);

            preview.Mesh.vertices = preview.Vertices;
            preview.Mesh.RecalculateBounds();
            preview.Origin = origin;
            preview.SourceShape = settings.Shape;
            preview.Rotation = rotation;
            preview.Radius = settings.Radius;
            preview.Angle = settings.Angle;
            preview.Width = settings.Width;
            preview.Length = settings.Length;
            preview.InnerRadius = innerRadius;
            preview.FallbackY = request.Origin.y;
            preview.GroundMask = _groundMask.value;
            preview.GroundOffset = _groundOffset;
            preview.HasGeometry = true;
        }

        private static void BuildTopology(Preview preview, MeshShape topology)
        {
            preview.Mesh.Clear();
            preview.Topology = topology;
            preview.HasTopology = true;
            preview.HasGeometry = false;

            if (topology == MeshShape.Box)
            {
                int rowSize = BoxWidthSegments + 1;
                preview.Vertices = new Vector3[rowSize * (BoxLengthSegments + 1)];
                int[] triangles = new int[BoxWidthSegments * BoxLengthSegments * 6];
                int next = 0;

                for (int row = 0; row < BoxLengthSegments; row++)
                {
                    for (int column = 0; column < BoxWidthSegments; column++)
                    {
                        int nearLeft = row * rowSize + column;
                        int farLeft = nearLeft + rowSize;
                        triangles[next++] = nearLeft;
                        triangles[next++] = farLeft;
                        triangles[next++] = farLeft + 1;
                        triangles[next++] = nearLeft;
                        triangles[next++] = farLeft + 1;
                        triangles[next++] = nearLeft + 1;
                    }
                }

                preview.Mesh.vertices = preview.Vertices;
                preview.Mesh.triangles = triangles;
                return;
            }

            bool hollow = topology == MeshShape.HollowPolar;
            int ringCount = hollow ? RadialSegments + 1 : RadialSegments;
            preview.Vertices = new Vector3[ringCount * RingVertices + (hollow ? 0 : 1)];
            int triangleCount = hollow
                ? RadialSegments * ArcSegments * 2
                : ArcSegments * (1 + (RadialSegments - 1) * 2);
            int[] polarTriangles = new int[triangleCount * 3];
            int nextTriangle = 0;

            if (!hollow)
            {
                for (int index = 0; index < ArcSegments; index++)
                {
                    polarTriangles[nextTriangle++] = 0;
                    polarTriangles[nextTriangle++] = index + 1;
                    polarTriangles[nextTriangle++] = index + 2;
                }
            }

            int bandCount = hollow ? RadialSegments : RadialSegments - 1;
            for (int band = 0; band < bandCount; band++)
            {
                int inner = (hollow ? 0 : 1) + band * RingVertices;
                int outer = inner + RingVertices;

                for (int index = 0; index < ArcSegments; index++)
                {
                    int innerLeft = inner + index;
                    int outerLeft = outer + index;
                    polarTriangles[nextTriangle++] = innerLeft;
                    polarTriangles[nextTriangle++] = outerLeft;
                    polarTriangles[nextTriangle++] = outerLeft + 1;
                    polarTriangles[nextTriangle++] = innerLeft;
                    polarTriangles[nextTriangle++] = outerLeft + 1;
                    polarTriangles[nextTriangle++] = innerLeft + 1;
                }
            }

            preview.Mesh.vertices = preview.Vertices;
            preview.Mesh.triangles = polarTriangles;
        }

        private void UpdatePolarVertices(Preview preview,
            in DetectionAreaRequest request, Quaternion rotation,
            float innerRadius, bool hollow)
        {
            Vector3 origin = request.AreaOrigin;
            float fallbackY = request.Origin.y;
            float centerY = ProjectGroundY(origin, fallbackY);
            int firstRing = hollow ? 0 : 1;
            int ringCount = hollow ? RadialSegments + 1 : RadialSegments;
            float angle = request.Settings.Shape == EDetectionAreaShape.Radial
                ? 360f : request.Settings.Angle;

            if (!hollow)
                preview.Vertices[0] = new Vector3(0f, centerY + _groundOffset - origin.y, 0f);

            for (int ring = 0; ring < ringCount; ring++)
            {
                float radius = hollow
                    ? Mathf.Lerp(innerRadius, request.Settings.Radius,
                        ring / (float)RadialSegments)
                    : request.Settings.Radius * (ring + 1) / RadialSegments;
                int ringStart = firstRing + ring * RingVertices;

                for (int index = 0; index <= ArcSegments; index++)
                {
                    float degrees = Mathf.Lerp(-angle * 0.5f, angle * 0.5f,
                        index / (float)ArcSegments);
                    float radians = degrees * Mathf.Deg2Rad;
                    float x = Mathf.Sin(radians) * radius;
                    float z = Mathf.Cos(radians) * radius;
                    Vector3 worldPoint = origin + rotation * new Vector3(x, 0f, z);
                    float groundY = ProjectGroundY(worldPoint, centerY);
                    preview.Vertices[ringStart + index] = new Vector3(
                        x, groundY + _groundOffset - origin.y, z);
                }
            }
        }

        private void UpdateBoxVertices(Preview preview,
            in DetectionAreaRequest request, Quaternion rotation)
        {
            Vector3 origin = request.AreaOrigin;
            float fallbackY = ProjectGroundY(origin, request.Origin.y);
            int rowSize = BoxWidthSegments + 1;

            for (int row = 0; row <= BoxLengthSegments; row++)
            {
                float z = request.Settings.Length * row / BoxLengthSegments;
                for (int column = 0; column <= BoxWidthSegments; column++)
                {
                    float x = Mathf.Lerp(-request.Settings.Width * 0.5f,
                        request.Settings.Width * 0.5f,
                        column / (float)BoxWidthSegments);
                    Vector3 worldPoint = origin + rotation * new Vector3(x, 0f, z);
                    float groundY = ProjectGroundY(worldPoint, fallbackY);
                    preview.Vertices[row * rowSize + column] = new Vector3(
                        x, groundY + _groundOffset - origin.y, z);
                }
            }
        }

        private float ProjectGroundY(Vector3 point, float fallbackY)
        {
            Vector3 rayOrigin = point + Vector3.up * ProbeHeight;
            return Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                ProbeDistance, _groundMask, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : fallbackY;
        }

        private void OnDisable() => HideAll();

        private void OnDestroy()
        {
            foreach (Preview preview in _previews.Values)
            {
                if (preview.Object != null)
                    Destroy(preview.Object);
                if (preview.Mesh != null)
                    Destroy(preview.Mesh);
            }

            _previews.Clear();
        }
    }
}
