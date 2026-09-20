using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Alpha.Boss
{
    // 표현용 복제 뼈대에서 루트 이동을 읽어 거리 진행률로 변환한다. 실제 보스나 이벤트는 실행하지 않는다.
    internal sealed class BossRootMotionSampler
    {
        private readonly Dictionary<(AnimationClip, Avatar, float, float, string), AnimationCurve> _curves = new();

        internal AnimationCurve Sample(Animator p_source, AnimationClip p_clip, float p_start, float p_duration,
            string p_bonePath = "")
        {
            if (p_source == null || p_clip == null || p_clip.legacy || !Finite(p_start) || p_start < 0f ||
                !Finite(p_duration) || p_duration <= 0f || !Finite(p_start + p_duration))
                return null;
            p_bonePath ??= string.Empty;
            var key = (p_clip, p_source.avatar, p_start, p_duration, p_bonePath);
            if (_curves.TryGetValue(key, out AnimationCurve cached))
                return cached;

            GameObject rig = null;
            PlayableGraph graph = default;
            AnimationCurve result = null;
            try
            {
                rig = new GameObject("Boss Root Motion Sample") { hideFlags = HideFlags.HideAndDontSave };
                // Instantiate를 사용하지 않아 Collider, 스크립트, Awake와 공격 이벤트가 복제되지 않는다.
                CopyChildren(p_source.transform, rig.transform);
                Transform bone = string.IsNullOrEmpty(p_bonePath) ? null : rig.transform.Find(p_bonePath);
                if (!string.IsNullOrEmpty(p_bonePath) && bone == null)
                    return null;
                Animator animator = rig.AddComponent<Animator>();
                animator.avatar = p_source.avatar;
                animator.applyRootMotion = true;
                animator.fireEvents = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                graph = PlayableGraph.Create("Boss Root Motion Sample");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, p_clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                var output = AnimationPlayableOutput.Create(graph, "Root Motion", animator);
                output.SetSourcePlayable(playable);
                graph.Play();
                float start = Mathf.Min(p_start, p_clip.length);
                playable.SetTime(start);
                graph.Evaluate(0f);
                Vector3 previousBonePosition = bone != null ? rig.transform.InverseTransformPoint(bone.position) : default;

                int count = Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(p_duration, 8f) * 120f), 2, 960);
                var keys = new Keyframe[count + 1];
                float total = 0f;
                float previousTime = start;
                for (int i = 1; i <= count; i++)
                {
                    float time = Mathf.Min(p_start + p_duration * ((float)i / count), p_clip.length);
                    float step = Mathf.Max(0f, time - previousTime);
                    if (step > 0f)
                    {
                        graph.Evaluate(step);
                        Vector3 delta = animator.deltaPosition;
                        if (bone != null)
                        {
                            // Generic 클립의 자식 뼈대 이동은 deltaPosition에 포함되지 않는다.
                            Vector3 position = rig.transform.InverseTransformPoint(bone.position);
                            delta = position - previousBonePosition;
                            previousBonePosition = position;
                        }
                        delta.y = 0f;
                        total += delta.magnitude;
                    }
                    keys[i] = new Keyframe((float)i / count, total);
                    previousTime = time;
                }
                if (Finite(total) && total > 0.0001f)
                {
                    // 직선 보간으로 정지 구간과 단조 증가를 유지하며 시작 0, 끝 1을 보장한다.
                    for (int i = 0; i <= count; i++)
                        keys[i].value /= total;
                    for (int i = 0; i <= count; i++)
                    {
                        keys[i].inTangent = i == 0 ? 0f : (keys[i].value - keys[i - 1].value) * count;
                        keys[i].outTangent = i == count ? 0f : (keys[i + 1].value - keys[i].value) * count;
                    }
                    result = new AnimationCurve(keys);
                }
            }
            finally
            {
                if (graph.IsValid()) graph.Destroy();
                if (rig != null)
                {
                    rig.SetActive(false);
                    if (Application.isPlaying) Object.Destroy(rig);
                    else Object.DestroyImmediate(rig);
                }
            }
            _curves[key] = result;
            return result;
        }

        private static void CopyChildren(Transform p_source, Transform p_parent)
        {
            foreach (Transform child in p_source)
            {
                Transform copy = new GameObject(child.name) { hideFlags = HideFlags.HideAndDontSave }.transform;
                copy.SetParent(p_parent, false);
                copy.localPosition = child.localPosition;
                copy.localRotation = child.localRotation;
                copy.localScale = child.localScale;
                CopyChildren(child, copy);
            }
        }

        private static bool Finite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
    }
}
