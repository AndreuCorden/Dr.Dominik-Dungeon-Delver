#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
static class MainMenuSceneMigration
{
    static MainMenuSceneMigration()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path != MainMenuSetupEditor.MainMenuScenePath)
            return;

        EditorApplication.delayCall += () =>
        {
            if (!MainMenuSetupEditor.NeedsScenePlacement())
                return;

            GameObject display = GameObject.Find("MainMenuDisplay");
            bool replace = display != null && display.transform.childCount > 0;
            MainMenuSetupEditor.PlaceDisplayInScene(replaceExistingChildren: replace);
            Debug.Log("Main menu: colocados MainMenuCampfire y MainMenuAdventurer en la escena (editables en Hierarchy).");
        };
    }
}

public static class MainMenuSetupEditor
{
    internal const string CampfirePrefabPath = "Assets/_Prefabs/_MainMenu/MainMenuCampfire.prefab";
    internal const string AdventurerPrefabPath = "Assets/_Prefabs/_MainMenu/MainMenuAdventurer.prefab";
    internal const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string MainMenuSkyboxPath = "Assets/_Materials/_MainMenu/MainMenuSkybox.mat";
    const string FloorPrefabPath = "Assets/_Prefabs/_LevelPrefabs/NGF_Env_Floor_01.prefab";
    const string ColumnPrefabPath = "Assets/_Prefabs/_EnvironmentPrefabs/NGF_Env_Column_01.prefab";
    const string MenuBgSpritePath = "Assets/SoftTouch_UI/SoftTouch/Sprites/PopupAndPanels/MenuBG.png";
    const string BackgroundCameraName = "MainMenuBackgroundCamera";
    const string EnvironmentRootName = "MainMenuEnvironment";
    const string RightPanelBackdropName = "RightPanelBackdrop";
    const string CampfireFbxPath = "Assets/Polygonal Particles/Assets/Meshes/Assets_Campfire.fbx";
    const string FirePrefabPath = "Assets/Polygonal Particles/Assets/Prefabs/Fire/Fire02.prefab";
    const string MedievalFbxPath = "Assets/Hooded Adventurer/Medieval.fbx";
    const string MenuAnimatorPath = "Assets/_Animations/Menu/MenuAdventurer.controller";

    [MenuItem("Tools/Dungeon Delver/Place Main Menu Display In Scene")]
    public static void PlaceDisplayInSceneFromMenu()
    {
        EnsurePrefabsExist();
        PlaceDisplayInScene(replaceExistingChildren: false);
        Debug.Log("Main menu display placed in scene. Move MainMenuCampfire and MainMenuAdventurer under MainMenuDisplay in the Hierarchy.");
    }

    [MenuItem("Tools/Dungeon Delver/Reset Main Menu Display In Scene")]
    public static void ResetDisplayInSceneFromMenu()
    {
        EnsurePrefabsExist();
        PlaceDisplayInScene(replaceExistingChildren: true);
        Debug.Log("Main menu display reset to default layout.");
    }

    [MenuItem("Tools/Dungeon Delver/Setup Main Menu Background")]
    public static void SetupBackgroundFromMenu()
    {
        SetupBackgroundBatchInternal(saveAndExit: false);
        Debug.Log("Main menu background configured.");
    }

    public static void SetupBackgroundBatch()
    {
        SetupBackgroundBatchInternal(saveAndExit: true);
    }

    static void SetupBackgroundBatchInternal(bool saveAndExit)
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        ConfigureCamera();
        EnsureMainMenuBackground();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (saveAndExit)
            EditorApplication.Exit(0);
    }

    [MenuItem("Tools/Dungeon Delver/Rebuild Main Menu Prefabs")]
    public static void RebuildPrefabsFromMenu()
    {
        EnsureFolder("Assets/_Prefabs/_MainMenu");
        EnsureFolder("Assets/_Animations/Menu");
        BuildCampfirePrefab();
        BuildAdventurerPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("Main menu prefabs rebuilt.");
    }

    public static void RunSetupBatch()
    {
        EnsurePrefabsExist();
        PlaceDisplayInScene(replaceExistingChildren: true);
        EnsureMainMenuBackground();
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
    }

    internal static bool NeedsScenePlacement()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        Scene active = SceneManager.GetActiveScene();
        if (active.path != MainMenuScenePath)
            return false;

        GameObject display = GameObject.Find("MainMenuDisplay");
        if (display == null)
            return true;

        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(display) > 0)
            return true;

        if (display.transform.childCount == 0)
            return true;

        GameObject campfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampfirePrefabPath);
        GameObject adventurerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdventurerPrefabPath);
        bool hasCampfire = FindPrefabInstanceChild(display.transform, campfirePrefab) != null;
        bool hasAdventurer = FindPrefabInstanceChild(display.transform, adventurerPrefab) != null;
        return !hasCampfire || !hasAdventurer;
    }

    static void EnsurePrefabsExist()
    {
        if (!File.Exists(CampfirePrefabPath) || !File.Exists(AdventurerPrefabPath))
        {
            EnsureFolder("Assets/_Prefabs/_MainMenu");
            EnsureFolder("Assets/_Animations/Menu");
            if (!File.Exists(CampfirePrefabPath))
                BuildCampfirePrefab();
            if (!File.Exists(AdventurerPrefabPath))
                BuildAdventurerPrefab();
        }
    }

    internal static void PlaceDisplayInScene(bool replaceExistingChildren)
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

        RemoveLegacyInstaller();

        Transform displayRoot = GetOrCreateDisplayRoot();

        if (replaceExistingChildren)
            ClearDisplayChildren(displayRoot);

        GameObject campfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampfirePrefabPath);
        GameObject adventurerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdventurerPrefabPath);

        GameObject campfire = FindPrefabInstanceChild(displayRoot, campfirePrefab)
            ?? InstantiateDisplayChild(campfirePrefab, displayRoot, new Vector3(0.4f, 0f, 0f), Quaternion.identity);

        GameObject adventurer = FindPrefabInstanceChild(displayRoot, adventurerPrefab)
            ?? InstantiateDisplayChild(adventurerPrefab, displayRoot, new Vector3(-1.1f, 0f, 0.3f), Quaternion.Euler(0f, 55f, 0f));

        if (campfire != null && adventurer != null)
            LinkAdventurerToCampfire(adventurer, campfire);

        ConfigureCamera();
        ConfigureBackgroundCamera();
        ConfigureLighting();
        EnsureMainMenuBackground();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void EnsureMainMenuBackground()
    {
        EnsureMainMenuSkyboxMaterial();
        ConfigureRenderSettingsSkybox();
        ConfigureBackgroundCamera();
        EnsureMainMenuEnvironment();
        EnsureRightPanelBackdrop();
    }

    static void RemoveLegacyInstaller()
    {
        GameObject display = GameObject.Find("MainMenuDisplay");
        if (display != null)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(display);
    }

    static Transform GetOrCreateDisplayRoot()
    {
        GameObject existing = GameObject.Find("MainMenuDisplay");
        if (existing != null)
            return existing.transform;

        var displayRoot = new GameObject("MainMenuDisplay");
        displayRoot.transform.position = new Vector3(-2.2f, 0f, 0f);
        Undo.RegisterCreatedObjectUndo(displayRoot, "Create MainMenuDisplay");
        return displayRoot.transform;
    }

    static void ClearDisplayChildren(Transform displayRoot)
    {
        for (int i = displayRoot.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(displayRoot.GetChild(i).gameObject);
    }

    static GameObject FindPrefabInstanceChild(Transform parent, GameObject prefabAsset)
    {
        if (prefabAsset == null)
            return null;

        string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
        foreach (Transform child in parent)
        {
            GameObject childRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject);
            if (childRoot == null)
                continue;

            string childPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(childRoot);
            if (childPath == prefabPath)
                return childRoot;
        }

        return null;
    }

    static GameObject InstantiateDisplayChild(
        GameObject prefabAsset,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        if (prefabAsset == null)
            return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(instance, "Place " + prefabAsset.name);
        return instance;
    }

    static void LinkAdventurerToCampfire(GameObject adventurer, GameObject campfire)
    {
        var display = adventurer.GetComponent<MainMenuAdventurerDisplay>();
        if (display == null)
            return;

        var serialized = new SerializedObject(display);
        serialized.FindProperty("fireTarget").objectReferenceValue = campfire.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(display);
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    static GameObject BuildCampfirePrefab()
    {
        var root = new GameObject("MainMenuCampfire");

        GameObject campfireModel = LoadModelRoot(CampfireFbxPath);
        if (campfireModel != null)
        {
            campfireModel.transform.SetParent(root.transform, false);
            campfireModel.transform.localPosition = Vector3.zero;
            campfireModel.transform.localRotation = Quaternion.identity;
            campfireModel.transform.localScale = Vector3.one;
        }

        GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirePrefabPath);
        if (firePrefab != null)
        {
            GameObject fire = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab, root.transform);
            fire.transform.localPosition = new Vector3(0f, 0.05f, 0.45f);
            fire.transform.localRotation = Quaternion.identity;
            fire.transform.localScale = Vector3.one;
        }

        var lightGo = new GameObject("CampfireLight");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.6f, 0.2f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.65f, 0.35f);
        light.intensity = 2.5f;
        light.range = 6f;
        light.shadows = LightShadows.None;

        return SavePrefab(root, CampfirePrefabPath);
    }

    static GameObject BuildAdventurerPrefab()
    {
        var root = new GameObject("MainMenuAdventurer");

        GameObject model = LoadModelRoot(MedievalFbxPath);
        if (model == null)
            return SavePrefab(root, AdventurerPrefabPath);

        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        var animator = model.GetComponent<Animator>();
        if (animator == null)
            animator = model.AddComponent<Animator>();

        RuntimeAnimatorController menuController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MenuAnimatorPath);
        if (menuController != null)
            animator.runtimeAnimatorController = menuController;

        animator.applyRootMotion = false;

        var display = root.AddComponent<MainMenuAdventurerDisplay>();
        var serialized = new SerializedObject(display);
        serialized.FindProperty("animator").objectReferenceValue = animator;
        serialized.FindProperty("poseNormalizedTime").floatValue = 0.9f;
        serialized.FindProperty("handAimSpeed").floatValue = 10f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, AdventurerPrefabPath);
    }

    static GameObject LoadModelRoot(string assetPath)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (source == null)
        {
            Debug.LogError($"Missing asset: {assetPath}");
            return null;
        }

        return (GameObject)PrefabUtility.InstantiatePrefab(source);
    }

    static GameObject SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static void ConfigureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.orthographic = true;
        cam.orthographicSize = 2.2f;
        cam.transform.position = new Vector3(-1.5f, 1.4f, -8f);
        cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.Depth;
        cam.depth = -1f;
        cam.rect = new Rect(0f, 0f, 0.58f, 1f);
    }

    static void ConfigureBackgroundCamera()
    {
        GameObject bgGo = GameObject.Find(BackgroundCameraName);
        if (bgGo == null)
        {
            bgGo = new GameObject(BackgroundCameraName);
            bgGo.AddComponent<Camera>();
            Undo.RegisterCreatedObjectUndo(bgGo, "Create MainMenuBackgroundCamera");
        }

        Camera bgCam = bgGo.GetComponent<Camera>();
        bgCam.depth = -10f;
        bgCam.clearFlags = CameraClearFlags.Skybox;
        bgCam.cullingMask = 0;
        bgCam.rect = new Rect(0f, 0f, 1f, 1f);
        bgCam.orthographic = true;
        bgCam.orthographicSize = 1f;
        bgCam.nearClipPlane = 0.3f;
        bgCam.farClipPlane = 1000f;

        AudioListener listener = bgGo.GetComponent<AudioListener>();
        if (listener != null)
            Object.DestroyImmediate(listener);

        EditorUtility.SetDirty(bgGo);
    }

    static void ConfigureRenderSettingsSkybox()
    {
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(MainMenuSkyboxPath);
        if (skybox != null)
            RenderSettings.skybox = skybox;
    }

    static void EnsureMainMenuSkyboxMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(MainMenuSkyboxPath) != null)
            return;

        EnsureFolder("Assets/_Materials/_MainMenu");

        Shader shader = Shader.Find("Polygonal/GradientBackgroundShader");
        if (shader == null)
        {
            shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/Polygonal Particles/Assets/Shaders/GradientBackgroundShader.shader");
        }

        if (shader == null)
        {
            Debug.LogError("Main menu skybox shader not found.");
            return;
        }

        var material = new Material(shader)
        {
            name = "MainMenuSkybox"
        };
        material.SetColor("_ColorTop", new Color(0.04f, 0.03f, 0.1f));
        material.SetColor("_ColorBottom", new Color(0.32f, 0.12f, 0.08f));
        AssetDatabase.CreateAsset(material, MainMenuSkyboxPath);
        AssetDatabase.SaveAssets();
    }

    static void EnsureMainMenuEnvironment()
    {
        GameObject envRoot = GameObject.Find(EnvironmentRootName);
        if (envRoot == null)
        {
            envRoot = new GameObject(EnvironmentRootName);
            Undo.RegisterCreatedObjectUndo(envRoot, "Create MainMenuEnvironment");
        }

        GameObject display = GameObject.Find("MainMenuDisplay");
        if (display != null)
            envRoot.transform.position = display.transform.position;
        else
            envRoot.transform.position = new Vector3(-2.849f, 0f, -2.56f);

        envRoot.transform.rotation = Quaternion.identity;
        envRoot.transform.localScale = Vector3.one;

        GameObject floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefabPath);
        if (floorPrefab != null)
        {
            Vector3[] floorPositions =
            {
                new Vector3(0f, -0.05f, 2.8f),
                new Vector3(1.15f, -0.05f, 2.8f),
                new Vector3(-1.15f, -0.05f, 2.8f)
            };

            foreach (Vector3 localPos in floorPositions)
            {
                if (FindEnvironmentChild(envRoot.transform, floorPrefab, localPos) != null)
                    continue;

                GameObject floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, envRoot.transform);
                floor.transform.localPosition = localPos;
                floor.transform.localRotation = Quaternion.identity;
                floor.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(floor, "Place menu floor");
            }
        }

        GameObject columnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ColumnPrefabPath);
        if (columnPrefab != null)
        {
            Vector3 columnPos = new Vector3(2.6f, 0f, 3.2f);
            if (FindEnvironmentChild(envRoot.transform, columnPrefab, columnPos) == null)
            {
                GameObject column = (GameObject)PrefabUtility.InstantiatePrefab(columnPrefab, envRoot.transform);
                column.transform.localPosition = columnPos;
                column.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
                column.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(column, "Place menu column");
            }
        }

        EditorUtility.SetDirty(envRoot);
    }

    static GameObject FindEnvironmentChild(Transform parent, GameObject prefabAsset, Vector3 localPosition)
    {
        if (prefabAsset == null)
            return null;

        string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
        foreach (Transform child in parent)
        {
            GameObject childRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject);
            if (childRoot == null)
                continue;

            string childPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(childRoot);
            if (childPath != prefabPath)
                continue;

            if (Vector3.Distance(child.localPosition, localPosition) < 0.15f)
                return childRoot;
        }

        return null;
    }

    static void EnsureRightPanelBackdrop()
    {
        GameObject canvasGo = GameObject.Find("MainMenuCanvas");
        if (canvasGo == null)
            return;

        Transform canvas = canvasGo.transform;
        Transform existing = canvas.Find(RightPanelBackdropName);
        if (existing == null)
        {
            var backdropGo = new GameObject(RightPanelBackdropName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdropGo.transform.SetParent(canvas, false);
            backdropGo.transform.SetAsFirstSibling();
            existing = backdropGo.transform;
            Undo.RegisterCreatedObjectUndo(backdropGo, "Create RightPanelBackdrop");
        }

        var rect = existing.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.58f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = existing.GetComponent<Image>();
        Sprite menuBg = AssetDatabase.LoadAssetAtPath<Sprite>(MenuBgSpritePath);
        if (menuBg != null)
            image.sprite = menuBg;

        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0.42f);

        existing.SetAsFirstSibling();
        EditorUtility.SetDirty(existing.gameObject);
    }

    static void ConfigureLighting()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light.type != LightType.Directional)
                continue;

            light.intensity = 0.35f;
            light.color = new Color(0.55f, 0.6f, 0.75f);
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.12f, 0.15f);
    }
}
#endif
