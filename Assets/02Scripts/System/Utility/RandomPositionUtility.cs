using UnityEngine;

namespace Alpha.Utility
{
    // 지정된 중심과 범위 안에서 무작위 월드 좌표를 생성한다.
    public static class RandomPositionUtility
    {
        // 중심과 같은 높이를 유지하는 수평 원 내부 좌표를 반환한다.
        public static Vector3 GetPointInHorizontalCircle(
            Vector3 p_center,
            float p_radius)
        {
            float radius = Mathf.Max(0f, p_radius);

            Vector2 randomPoint =
                UnityEngine.Random.insideUnitCircle * radius;

            return p_center +
                   new Vector3(
                       randomPoint.x,
                       0f,
                       randomPoint.y);
        }
    }
}
