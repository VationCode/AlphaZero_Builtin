using System.IO;
using Alpha.Player.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alpha.Player.Editor
{
    // 이전 RequireComponent가 카테고리에 추가한 중복을 열린 Main Scene에서 한 번 정리한다.
    [InitializeOnLoad]
    internal static class PlayerCombatCategoryCleanup
    {
        private const string CompletedKey = "Alpha.PlayerCombatCategoryCleanup.v1";
        private const string ScenePath = "Assets/01Scenes/Main.unity";
        private const string ReportPath = "Library/player-combat-category-cleanup-v1.txt";

        static PlayerCombatCategoryCleanup()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            if (SessionState.GetBool(CompletedKey, false) || File.Exists(ReportPath))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += RunOnce;
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            int removed = 0;
            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (PlayerCore player in root.GetComponentsInChildren<PlayerCore>(true))
            {
                Transform combatOwner = player.transform.Find("Combat");
                CombatModule combat = combatOwner != null
                    ? combatOwner.GetComponent<CombatModule>() : null;
                if (combat == null || combat.GetComponent<WeaponSwapModule>() == null)
                    continue;

                foreach (string category in new[] { "Melee", "Range", "Special" })
                {
                    Transform owner = combatOwner.Find(category);
                    if (owner == null)
                        continue;

                    // View의 명시적 참조도 공통 Combat으로 돌린 뒤 중복 객체를 제거한다.
                    foreach (MonoBehaviour component in owner.GetComponents<MonoBehaviour>())
                    {
                        if (component == null)
                            continue;
                        SerializedObject serialized = new(component);
                        SerializedProperty reference = serialized.FindProperty("_combatModule");
                        if (reference != null &&
                            reference.propertyType == SerializedPropertyType.ObjectReference &&
                            reference.objectReferenceValue != combat)
                        {
                            reference.objectReferenceValue = combat;
                            serialized.ApplyModifiedProperties();
                            changed = true;
                        }
                    }

                    foreach (CombatModule duplicate in owner.GetComponents<CombatModule>())
                    {
                        Undo.DestroyObjectImmediate(duplicate);
                        removed++;
                    }
                    foreach (WeaponSwapModule duplicate in owner.GetComponents<WeaponSwapModule>())
                    {
                        Undo.DestroyObjectImmediate(duplicate);
                        removed++;
                    }
                }
            }

            if (removed > 0 || changed)
                EditorSceneManager.MarkSceneDirty(scene);

            // 현재 열려 있는 Scene의 사용자 변경도 유지한 채 결과를 저장한다.
            bool saved = !(removed > 0 || changed) || EditorSceneManager.SaveScene(scene);
            if (saved)
            {
                File.WriteAllText(ReportPath,
                    $"Removed={removed}; Saved={saved}; Scene={scene.path}");
                SessionState.SetBool(CompletedKey, true);
            }
            Debug.Log($"[Combat Category Cleanup] Removed={removed}, Saved={saved}");
        }
    }
}
