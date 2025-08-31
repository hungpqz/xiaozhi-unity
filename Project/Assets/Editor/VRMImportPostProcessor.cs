using System;
using System.IO;
using System.Linq;
using System.Reflection;
using uLipSync;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UniVRM10;
using Object = UnityEngine.Object;

namespace XiaoZhi.Unity.Editor
{
    public static class VRMImportPreProcessor
    {
        [MenuItem("Assets/VRM10/PreProcess", isValidateFunction: false, priority: 1101)]
        private static void PreProcess()
        {
            var objects = Selection.objects;
            foreach (var o in objects)
                PreProcess(AssetDatabase.GetAssetPath(o));
        }

        private static void PreProcess(string path)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer == null) return;
            var editorGUI = UnityEditor.Editor.CreateEditor(importer);
            if (editorGUI == null) return;
            ExtraMaterial(editorGUI);
            ExtractVRM(editorGUI);
            importer.SaveAndReimport();
            AddExpressions(importer);
            CreatePrefab(importer);
        }

        private static void ExtraMaterial(UnityEditor.Editor editorGUI)
        {
            var importer = editorGUI.target;
            var editorType = editorGUI.GetType();
            var fieldCurrentTab = editorType.GetField("s_currentTab", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldCurrentTab == null) return;
            fieldCurrentTab.SetValue(null, 1);
            editorGUI.Repaint();
            var fieldMaterialEditor =
                editorType.GetField("m_materialEditor", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldMaterialEditor == null) return;
            var materialEditor = fieldMaterialEditor.GetValue(editorGUI);
            if (materialEditor is null) return;
            var methodCanExtract = fieldMaterialEditor.FieldType.BaseType!
                .GetMethod("CanExtract", BindingFlags.Instance | BindingFlags.NonPublic);
            if (methodCanExtract == null) return;
            var canExtract = (bool)methodCanExtract.Invoke(materialEditor, new object[] { importer });
            if (!canExtract) return;
            var fieldResult = editorType.GetField("m_result", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldResult == null) return;
            if (fieldResult.GetValue(editorGUI) is not Vrm10Data result) return;
            var methodExtractMaterialsAndTextures = fieldMaterialEditor.FieldType
                .GetMethod("ExtractMaterialsAndTextures", BindingFlags.Instance | BindingFlags.NonPublic);
            if (methodExtractMaterialsAndTextures == null) return;
            methodExtractMaterialsAndTextures.Invoke(materialEditor, new object[]
            {
                importer, result.Data, new Vrm10TextureDescriptorGenerator(result.Data),
                (Func<string, string>)(assetPath => $"{Path.GetFileNameWithoutExtension(assetPath)}.vrm1.Textures"),
                (Func<string, string>)(assetPath => $"{Path.GetFileNameWithoutExtension(assetPath)}.vrm1.Materials")
            });
        }

        private static void ExtractVRM(UnityEditor.Editor editorGUI)
        {
            var importer = editorGUI.target;
            var editorType = editorGUI.GetType();
            var fieldCurrentTab = editorType.GetField("s_currentTab", BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldCurrentTab == null) return;
            fieldCurrentTab.SetValue(null, 2);
            editorGUI.Repaint();
            var fieldVRMEditor =
                editorType.GetField("m_vrmEditor", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldVRMEditor == null) return;
            var vrmEditor = fieldVRMEditor.GetValue(editorGUI);
            if (vrmEditor is null) return;
            var methodCanExtract = fieldVRMEditor.FieldType.BaseType!
                .GetMethod("CanExtract", BindingFlags.Instance | BindingFlags.NonPublic);
            if (methodCanExtract == null) return;
            var canExtract = (bool)methodCanExtract.Invoke(vrmEditor, new object[] { importer });
            if (!canExtract) return;
            var fieldResult = editorType.GetField("m_result", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldResult == null) return;
            if (fieldResult.GetValue(editorGUI) is not Vrm10Data result) return;
            var methodExtract = fieldVRMEditor.FieldType
                .GetMethod("Extract", BindingFlags.Static | BindingFlags.Public);
            if (methodExtract == null) return;
            methodExtract.Invoke(null, new object[] { importer, result.Data });
        }

        private static void AddExpressions(AssetImporter importer)
        {
            var thinking = ScriptableObject.CreateInstance<VRM10Expression>();
            thinking.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
            var scene = PreviewSceneManager.GetOrCreate(thinking.Prefab);
            var pathName = "Face";
            var pathIndex = Array.FindIndex(scene.SkinnedMeshRendererPathList, x => x == pathName);
            var blendShapeNames = scene.GetBlendShapeNames(pathIndex);
            thinking.MorphTargetBindings = new[]
            {
                new MorphTargetBinding(pathName,
                    Array.FindIndex(blendShapeNames, x => x == "Fcl_EYE_Close"), 0.3f),
                new MorphTargetBinding(pathName,
                    Array.FindIndex(blendShapeNames, x => x == "Fcl_MTH_Small"), 0.5f),
            };
            EditorUtility.SetDirty(thinking);
            AssetDatabase.SaveAssets();
            AssetDatabase.CreateAsset(thinking,
                $"{Path.GetDirectoryName(importer.assetPath)}/{Path.GetFileNameWithoutExtension(importer.assetPath)}.vrm1.Assets/thinking.asset");
            var vrm10Object = AssetDatabase.LoadAssetAtPath<VRM10Object>(
                $"{Path.GetDirectoryName(importer.assetPath)}/{Path.GetFileNameWithoutExtension(importer.assetPath)}.vrm1.Assets/_vrm1_.asset");
            if (vrm10Object == null || vrm10Object.Expression.CustomClips.Any(i => i == thinking)) return;
            vrm10Object.Expression.AddClip(ExpressionPreset.custom, thinking);
            EditorUtility.SetDirty(vrm10Object);
            AssetDatabase.SaveAssets();
            Object.DestroyImmediate(scene.gameObject);
        }

        private static void CreatePrefab(AssetImporter importer)
        {
            var prefabPath =
                $"{Path.GetDirectoryName(importer.assetPath)}/prefab.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
            var prefab = (GameObject)PrefabUtility.InstantiatePrefab(model);
            const string commonAnimatorPath = "Assets/Res/VRM/Common/animator.controller";
            prefab.GetComponent<Animator>().runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(commonAnimatorPath);
            var uLipSyncExpressionVRM = prefab.AddComponent<uLipSyncExpressionVRM>();
            uLipSyncExpressionVRM.usePhonemeBlend = true;
            uLipSyncExpressionVRM.AddBlendShape("A", "aa");
            uLipSyncExpressionVRM.AddBlendShape("I", "ih");
            uLipSyncExpressionVRM.AddBlendShape("U", "ou");
            uLipSyncExpressionVRM.AddBlendShape("E", "ee");
            uLipSyncExpressionVRM.AddBlendShape("O", "oh");
            var uLipSync = prefab.AddComponent<uLipSync.uLipSync>();
            var eventMethods =
                typeof(LipSyncUpdateEvent).BaseType!.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            var methodAddPersistentListener =
                eventMethods.FirstOrDefault(x => x.Name == "AddPersistentListener" && x.GetParameters().Length == 1);
            var onLipSyncUpdate = new UnityAction<LipSyncInfo>(uLipSyncExpressionVRM.OnLipSyncUpdate);
            methodAddPersistentListener?.Invoke(uLipSync.onLipSyncUpdate, new object[] { onLipSyncUpdate });
            uLipSync.profile = AssetDatabase.LoadAssetAtPath<Profile>("Assets/Res/VRM/Common/profile.asset");
            var follower = prefab.AddComponent<TransformFollower>();
            var followerSerialObj = new SerializedObject(follower);
            followerSerialObj.FindProperty("_bounds").boundsValue =
                prefab.transform.Find("Body")?.GetComponent<SkinnedMeshRenderer>()?.bounds ?? new Bounds();
            followerSerialObj.FindProperty("_topGap").floatValue = 0.1f;
            followerSerialObj.FindProperty("_bottomGap").floatValue = 0.0f;
            followerSerialObj.ApplyModifiedProperties();
            var eyeBlinker = prefab.AddComponent<VRMEyeBlinker>();
            eyeBlinker.BlinkInterval = new Vector2(2, 8);
            eyeBlinker.BlinkEyeCloseDuration = 0.3f;
            eyeBlinker.BlinkOpeningSeconds = 0.3f;
            eyeBlinker.BlinkClosingSeconds = 0.1f;
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            addressableSettings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath),
                addressableSettings.DefaultGroup);
            Object.DestroyImmediate(prefab);
        }
    }
}