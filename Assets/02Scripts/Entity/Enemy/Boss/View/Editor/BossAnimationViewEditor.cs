using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alpha.Boss.Editor
{
    // Animator의 실제 경로와 에셋 ID를 함께 보관해 이름 변경을 추적한다.
    public static class BossAnimationStateCatalog
    {
        public sealed class Entry
        {
            public string Path;
            public string Id;
        }

        public static List<Entry> Read(RuntimeAnimatorController p_controller)
        {
            while (p_controller is AnimatorOverrideController overrides)
                p_controller = overrides.runtimeAnimatorController;

            List<Entry> entries = new();
            if (p_controller is AnimatorController controller && controller.layers.Length > 0)
            {
                AnimatorControllerLayer layer = controller.layers[0];
                Collect(layer.stateMachine, layer.name, entries);
            }
            return entries;
        }

        private static void Collect(AnimatorStateMachine p_machine, string p_path, List<Entry> p_entries)
        {
            foreach (ChildAnimatorState child in p_machine.states)
            {
                bool hasId = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(child.state, out string guid, out long id);
                p_entries.Add(new Entry { Path = p_path + "." + child.state.name,
                    Id = hasId ? guid + ":" + id : string.Empty });
            }
            foreach (ChildAnimatorStateMachine child in p_machine.stateMachines)
                Collect(child.stateMachine, p_path + "." + child.stateMachine.name, p_entries);
        }

        // 최초 연결은 기존 경로를 사용하고, 이후에는 이름과 무관한 ID를 우선한다.
        public static bool Synchronize(BossAnimationView p_view)
        {
            if (p_view == null || p_view.Animator == null)
                return false;

            List<Entry> entries = Read(p_view.Animator.runtimeAnimatorController);
            SerializedObject serialized = new(p_view);
            bool changed = SyncPair(serialized.FindProperty("_idleStatePath"), serialized.FindProperty("_idleStateId"), entries);
            changed |= SyncPair(serialized.FindProperty("_chaseStatePath"), serialized.FindProperty("_chaseStateId"), entries);
            changed |= SyncPair(serialized.FindProperty("_deathStatePath"), serialized.FindProperty("_deathStateId"), entries);

            if (changed)
                serialized.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        private static bool SyncPair(SerializedProperty p_path, SerializedProperty p_id, List<Entry> p_entries)
        {
            Entry entry = string.IsNullOrEmpty(p_id.stringValue) ? null :
                p_entries.Find(item => item.Id == p_id.stringValue);
            // Controller 교체 시 동일 경로를 새 Controller의 상태에 연결한다.
            entry ??= p_entries.Find(item => item.Path == p_path.stringValue);
            if (entry == null || (p_path.stringValue == entry.Path && p_id.stringValue == entry.Id))
                return false;
            p_path.stringValue = entry.Path;
            p_id.stringValue = entry.Id;
            return true;
        }
    }

    [CustomEditor(typeof(BossAnimationView))]
    public sealed class BossAnimationViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BossAnimationView view = (BossAnimationView)target;
            BossAnimationStateCatalog.Synchronize(view);
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_animator"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_rootMotionBonePath"));
            List<BossAnimationStateCatalog.Entry> entries = BossAnimationStateCatalog.Read(view.Animator?.runtimeAnimatorController);
            if (entries.Count == 0)
                EditorGUILayout.HelpBox("Animator Controller의 첫 번째 Layer에 상태를 추가하세요.", MessageType.Warning);

            DrawState(serializedObject.FindProperty("_idleStatePath"), serializedObject.FindProperty("_idleStateId"), "Idle", entries);
            DrawState(serializedObject.FindProperty("_chaseStatePath"), serializedObject.FindProperty("_chaseStateId"), "Chase", entries);
            DrawState(serializedObject.FindProperty("_deathStatePath"), serializedObject.FindProperty("_deathStateId"), "Death", entries);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_logCrossFadeRequests"));
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawState(SerializedProperty p_path, SerializedProperty p_id, string p_label,
            List<BossAnimationStateCatalog.Entry> p_entries)
        {
            string[] labels = new string[p_entries.Count + 1];
            labels[0] = string.IsNullOrEmpty(p_path.stringValue) ? "상태 선택" : "연결 없음: " + p_path.stringValue;
            int selected = 0;
            for (int index = 0; index < p_entries.Count; index++)
            {
                labels[index + 1] = p_entries[index].Path;
                if (p_entries[index].Path == p_path.stringValue)
                    selected = index + 1;
            }
            int next = EditorGUILayout.Popup(p_label, selected, labels);
            if (next > 0 && next != selected)
            {
                p_path.stringValue = p_entries[next - 1].Path;
                p_id.stringValue = p_entries[next - 1].Id;
            }
            if (selected == 0 && !string.IsNullOrEmpty(p_path.stringValue))
                EditorGUILayout.HelpBox("연결된 상태가 없습니다. 실제 상태를 다시 선택하세요.", MessageType.Warning);
        }
    }

    // Inspector가 닫혀 있어도 Play 직전·씬 로드·빌드 시 최신 상태 이름을 반영한다.
    [InitializeOnLoad]
    public sealed class BossAnimationStateSynchronizer : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        static BossAnimationStateSynchronizer()
        {
            EditorApplication.delayCall += SynchronizeLoadedScenes;
            EditorApplication.projectChanged += SynchronizeLoadedScenes;
            EditorSceneManager.sceneOpened += (scene, mode) => SynchronizeScene(scene);
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    SynchronizeLoadedScenes();
            };
        }

        private static void SynchronizeLoadedScenes()
        {
            if (EditorApplication.isPlaying)
                return;
            for (int index = 0; index < SceneManager.sceneCount; index++)
                SynchronizeScene(SceneManager.GetSceneAt(index));
        }

        private static void SynchronizeScene(Scene p_scene)
        {
            if (!p_scene.IsValid() || !p_scene.isLoaded)
                return;
            foreach (GameObject root in p_scene.GetRootGameObjects())
                foreach (BossAnimationView view in root.GetComponentsInChildren<BossAnimationView>(true))
                    BossAnimationStateCatalog.Synchronize(view);
        }

        public void OnProcessScene(Scene p_scene, BuildReport p_report)
        {
            SynchronizeScene(p_scene);
        }
    }
}
