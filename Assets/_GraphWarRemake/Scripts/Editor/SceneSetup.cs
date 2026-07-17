using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Mirror;
using Mirror.SimpleWeb;
using GraphWarRemake.Game;
using GraphWarRemake.Network;
using GraphWarRemake.LifetimeScopes;

namespace GraphWarRemake.Editor
{
    [InitializeOnLoad]
    public static class SceneSetup
    {
        [MenuItem("GraphWarRemake/Setup Scene")]
        public static void SetupScene()
        {
            if (!EditorUtility.DisplayDialog(
                "Настройка сцены",
                "Будут созданы:\n" +
                "• Префабы NetworkPlayer и NetworkProjectile\n" +
                "• NetworkManager, TurnManager, GameLifetimeScope\n" +
                "• UIDocument с GameUI\n" +
                "• Арена (пол + стены)\n\n" +
                "Продолжить?",
                "Да", "Отмена"))
                return;

            CreatePrefabs();
            SetupGameObjects();
            SetupGameUI();
            SetupArena();

            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[GraphWarRemake] Сцена успешно настроена!");
        }

        private static void CreatePrefabs()
        {
            CreatePlayerPrefab();
            CreateProjectilePrefab();
        }

        private static void CreatePlayerPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "NetworkPlayer";
            go.layer = LayerMask.NameToLayer("Player");
            go.transform.localScale = Vector3.one;
            go.transform.position = new Vector3(-8f, 0.5f, 0f);

            go.AddComponent<NetworkIdentity>();
            var player = go.AddComponent<Network.NetworkPlayer>();

            var collider = go.GetComponent<CapsuleCollider>();
            collider.isTrigger = false;

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(go.transform);
            spawnPoint.transform.localPosition = new Vector3(0f, 0f, 1.5f);

            var so = new SerializedObject(player);
            so.FindProperty("_projectileSpawnPoint").objectReferenceValue = spawnPoint.transform;

            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GraphWarRemake/Prefabs/NetworkProjectile.prefab");
            if (projectilePrefab != null)
                so.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;

            so.ApplyModifiedPropertiesWithoutUndo();

            string path = "Assets/_GraphWarRemake/Prefabs/NetworkPlayer.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"Префаб создан: {path}");
        }

        private static void CreateProjectilePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "NetworkProjectile";
            go.layer = LayerMask.NameToLayer("Projectile");
            go.transform.localScale = Vector3.one * 0.5f;

            go.AddComponent<NetworkIdentity>();
            go.AddComponent<NetworkTransformReliable>();
            go.AddComponent<NetworkProjectile>();

            var collider = go.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            string path = "Assets/_GraphWarRemake/Prefabs/NetworkProjectile.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"Префаб создан: {path}");
        }

        private static void SetupGameObjects()
        {
            SetupNetworkManager();
            SetupTurnManager();
            SetupGameLifetimeScope();
        }

        private static void SetupNetworkManager()
        {
            var go = GameObject.Find("NetworkManager") ?? new GameObject("NetworkManager");

            if (go.GetComponent<NetworkManager>() == null)
                go.AddComponent<NetworkManager>();
            if (go.GetComponent<SimpleWebTransport>() == null)
                go.AddComponent<SimpleWebTransport>();
            if (go.GetComponent<NetworkManagerHUD>() == null)
                go.AddComponent<NetworkManagerHUD>();

            var nm = go.GetComponent<NetworkManager>();
            nm.dontDestroyOnLoad = false;
            nm.runInBackground = true;
            nm.autoCreatePlayer = true;
            nm.maxConnections = 10;

            go.GetComponent<SimpleWebTransport>().port = 27777;

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GraphWarRemake/Prefabs/NetworkPlayer.prefab");
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GraphWarRemake/Prefabs/NetworkProjectile.prefab");

            if (playerPrefab != null)
                nm.playerPrefab = playerPrefab;

            if (projectilePrefab != null)
            {
                nm.spawnPrefabs.Clear();
                nm.spawnPrefabs.Add(projectilePrefab);
            }

            Debug.Log("NetworkManager настроен");
        }

        private static void SetupTurnManager()
        {
            var go = GameObject.Find("TurnManager") ?? new GameObject("TurnManager");

            if (go.GetComponent<NetworkIdentity>() == null)
                go.AddComponent<NetworkIdentity>();
            if (go.GetComponent<TurnManager>() == null)
                go.AddComponent<TurnManager>();

            Debug.Log("TurnManager настроен");
        }

        private static void SetupGameLifetimeScope()
        {
            var go = GameObject.Find("GameLifetimeScope") ?? new GameObject("GameLifetimeScope");

            if (go.GetComponent<GameLifetimeScope>() == null)
                go.AddComponent<GameLifetimeScope>();

            Debug.Log("GameLifetimeScope настроен");
        }

        private static void SetupGameUI()
        {
            // Создаём PanelSettings если нет
            var panelSettingsPath = "Assets/_GraphWarRemake/Resources/UI/PanelSettings.asset";
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            if (panelSettings == null)
            {
                // Создаём через CreateAsset с ScriptableObject
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                System.IO.Directory.CreateDirectory("Assets/_GraphWarRemake/Resources/UI");
                AssetDatabase.CreateAsset(panelSettings, panelSettingsPath);
                Debug.Log($"PanelSettings создан: {panelSettingsPath}");
            }

            var go = GameObject.Find("GameUI") ?? new GameObject("GameUI");

            var uidoc = go.GetComponent<UIDocument>();
            if (uidoc == null)
                uidoc = go.AddComponent<UIDocument>();

            uidoc.panelSettings = panelSettings;

            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_GraphWarRemake/Resources/UI/GameUI.uxml");
            if (uxmlAsset != null)
                uidoc.visualTreeAsset = uxmlAsset;

            if (go.GetComponent<UI.GameUI>() == null)
                go.AddComponent<UI.GameUI>();

            Debug.Log("UIDocument + GameUI настроен");
        }

        private static void SetupArena()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -1, 0);
            floor.transform.localScale = new Vector3(30, 0.2f, 15);
            floor.layer = LayerMask.NameToLayer("Obstacle");
            floor.GetComponent<Renderer>().sharedMaterial = CreateMaterial("FloorMat", new Color(0.2f, 0.2f, 0.25f));

            CreateWall("WallLeft", new Vector3(-15, 2, 0), new Vector3(0.2f, 4, 15));
            CreateWall("WallRight", new Vector3(15, 2, 0), new Vector3(0.2f, 4, 15));
            CreateWall("WallBack", new Vector3(0, 2, -7.5f), new Vector3(30, 4, 0.2f));
            CreateWall("WallFront", new Vector3(0, 2, 7.5f), new Vector3(30, 4, 0.2f));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Obstacle_Center";
            obstacle.transform.position = new Vector3(0, 1, 0);
            obstacle.transform.localScale = new Vector3(2, 2, 2);
            obstacle.layer = LayerMask.NameToLayer("Obstacle");
            obstacle.GetComponent<Renderer>().sharedMaterial = CreateMaterial("ObstacleMat", new Color(0.6f, 0.3f, 0.1f));

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 8, -12);
                cam.transform.rotation = Quaternion.Euler(30, 0, 0);
                cam.orthographic = false;
                cam.fieldOfView = 50;
            }

            Debug.Log("Арена создана");
        }

        private static void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.layer = LayerMask.NameToLayer("Obstacle");
            wall.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name + "Mat", new Color(0.3f, 0.3f, 0.35f));
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.name = name;
            mat.color = color;
            return mat;
        }
    }
}
