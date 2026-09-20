using System.Collections.Generic;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Boss
{
    // 한 공격의 피해 중복 방지와 연속 영역 검색에 필요한 이전 위치를 보관한다.
    public sealed class BossDamageContext
    {
        internal bool HasPosition { get; set; }
        internal Vector3 PreviousPosition { get; set; }
        internal HashSet<IDamageable> DamagedTargets { get; } = new();

        internal void Clear()
        {
            HasPosition = false;
            PreviousPosition = Vector3.zero;
            DamagedTargets.Clear();
        }
    }
}
