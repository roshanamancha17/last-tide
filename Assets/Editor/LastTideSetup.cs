using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LastTide.EditorTools
{
    /// <summary>Builds the playable island scene from the exported Blender model.</summary>
    public static class LastTideSetup
    {
        private const string ScenePath = "Assets/Scenes/Island.unity";
        private const string ModelPath = "Assets/Models/Island.fbx";
        private const string ItemsPath = "Assets/Models/Items.fbx";
        private const string MarlowPath = "Assets/Models/Marlow.fbx";
        private const string QuestPath = "Assets/Models/Quest.fbx";
        private const string PlayerPath = "Assets/Models/Player.fbx";
        private const string TrailDirtTexturePath = "Assets/Models/TrailDirt.png";
        private const string TerrainTexturePath = "Assets/Models/TerrainColor.png";
        private const string TerrainTrailTexturePath = "Assets/Models/TerrainTrail.png";
        private const string PrefabFolder = "Assets/Prefabs";
        private const string MaterialFolder = "Assets/Materials";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const float IslandWidth = 150f;
        private const int IgnoreRaycastLayer = 2;

        [MenuItem("LastTide/Build Island Scene")]
        public static void BuildIslandScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureRenderAssets();
            ConfigureModelImport(ModelPath);
            ConfigureModelImport(ItemsPath);
            ConfigureModelImport(MarlowPath);
            ConfigureModelImport(QuestPath);
            ConfigureModelImport(PlayerPath);
            AllowLocalServerInEditor();
            ConfigureTerrainTexture();

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            Dictionary<string, Material> materials = CreateMaterials();
            Dictionary<string, GameObject> prefabs = CreateItemPrefabs(materials);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError("[LastTide] Could not load " + ModelPath);
                return;
            }

            GameObject island = (GameObject)PrefabUtility.InstantiatePrefab(model);
            island.name = "Island";
            Transform terrain = island.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Terrain");
            if (terrain == null)
            {
                Debug.LogError("[LastTide] The model has no object named Terrain.");
                return;
            }

            float width = ScaleIsland(island.transform, terrain);
            int[] counts = SetUpIslandObjects(island, materials);
            Vector3 spawn = FindSpawnPoint(terrain, island);
            TrailData trail = CarveTrail(terrain, island, spawn);
            CreateTrailSurface(trail, materials["Dirt"], terrain.GetComponent<MeshCollider>());
            AddTrailStones(trail, island, materials["Rock"]);

            LastTide.DayNightCycle cycle = CreateSun();
            CreateSky();
            CreateOcean();
            GameObject player = CreatePlayer(spawn, terrain.GetComponent<Renderer>().bounds.center, materials);
            CreateCamera(player.transform);
            AddPlayerSystems(player, prefabs, cycle);
            int[] itemCounts = ScatterItems(terrain, island, spawn, prefabs);
            GameObject marlow = PlaceMarlow(CreateMarlowPrefab(materials), terrain, island, spawn, player, cycle);
            BuildQuest(materials, terrain, island, spawn, player, marlow, cycle, trail);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
            AssetDatabase.SaveAssets();

            Debug.Log($"[LastTide] Island scene built. Model width {width:F1} m scaled to {IslandWidth} m; " +
                      $"palms {counts[0]}, bushes {counts[1]}, rocks {counts[2]}; spawn {spawn}; " +
                      $"wood {itemCounts[0]}, stone {itemCounts[1]}, vines {itemCounts[2]}, coconuts {itemCounts[3]}.");
        }

        private static void ConfigureRenderAssets()
        {
            foreach (string path in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    Debug.LogWarning("[LastTide] Missing render asset " + path);
                    continue;
                }

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty depth = serialized.FindProperty("m_RequireDepthTexture");
                SerializedProperty shadows = serialized.FindProperty("m_ShadowDistance");
                if (depth != null) depth.boolValue = true;
                if (shadows != null) shadows.floatValue = 160f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureModelImport(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.isReadable = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        private static void ConfigureTerrainTexture()
        {
            TextureImporter importer = AssetImporter.GetAtPath(TerrainTexturePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            Dictionary<string, Material> materials = new Dictionary<string, Material>
            {
                ["TerrainMat"] = LitMaterial("Terrain", new Color(0.85f, 0.85f, 0.85f), 0.05f, false),
                ["Bark"] = LitMaterial("Bark", Linear(0.20f, 0.12f, 0.07f), 0.1f, false),
                ["PalmLeaf"] = LitMaterial("PalmLeaf", Linear(0.13f, 0.42f, 0.07f), 0.2f, true),
                ["PalmLeafDry"] = LitMaterial("PalmLeafDry", Linear(0.30f, 0.46f, 0.08f), 0.2f, true),
                ["Coconut"] = LitMaterial("Coconut", Linear(0.10f, 0.06f, 0.03f), 0.3f, false),
                ["Bush"] = LitMaterial("Bush", Linear(0.08f, 0.30f, 0.06f), 0.15f, false),
                ["Bush2"] = LitMaterial("Bush2", Linear(0.17f, 0.38f, 0.07f), 0.15f, false),
                ["Rock"] = LitMaterial("Rock", Linear(0.24f, 0.22f, 0.20f), 0.1f, false),
                ["Player"] = LitMaterial("Player", new Color(0.95f, 0.45f, 0.15f), 0.4f, false),
                ["Wood"] = LitMaterial("Wood", Linear(0.30f, 0.17f, 0.08f), 0.1f, false),
                ["Stone"] = LitMaterial("Stone", Linear(0.45f, 0.44f, 0.42f), 0.15f, false),
                ["Vine"] = LitMaterial("Vine", Linear(0.22f, 0.60f, 0.08f), 0.2f, false),
                ["Steel"] = LitMaterial("Steel", Linear(0.55f, 0.57f, 0.60f), 0.7f, false),
                ["Flame"] = FlameMaterial(),
                ["MarlowShell"] = LitMaterial("MarlowShell", Linear(0.58f, 0.64f, 0.66f), 0.6f, false),
                ["MarlowTrim"] = LitMaterial("MarlowTrim", Linear(0.06f, 0.07f, 0.09f), 0.45f, false),
                ["MarlowRust"] = LitMaterial("MarlowRust", Linear(0.38f, 0.16f, 0.06f), 0.1f, false),
                ["MarlowEye"] = MarlowEyeMaterial(),
                ["WreckWood"] = LitMaterial("WreckWood", Linear(0.16f, 0.09f, 0.05f), 0.08f, true),
                ["CaveRock"] = LitMaterial("CaveRock", Linear(0.14f, 0.13f, 0.12f), 0.05f, true),
                ["LampOff"] = LitMaterial("LampOff", Linear(0.10f, 0.12f, 0.13f), 0.8f, false),
                ["LampOn"] = EmissiveMaterial("LampOn", new Color(1f, 0.85f, 0.45f), 5f),
                ["PartGlow"] = EmissiveMaterial("PartGlow", new Color(0.25f, 0.85f, 1f), 1.8f),
                ["Beam"] = EmissiveMaterial("Beam", new Color(0.3f, 0.9f, 1f), 3f),
                ["Dirt"] = TrailDirtMaterial(),
                ["WoodEnds"] = LitMaterial("WoodEnds", Linear(0.62f, 0.45f, 0.26f), 0.1f, false),
                ["VineLeaf"] = LitMaterial("VineLeaf", Linear(0.12f, 0.45f, 0.06f), 0.25f, true),
                ["Glint"] = EmissiveMaterial("Glint", new Color(1f, 0.95f, 0.7f), 2.5f),
                ["Marker"] = EmissiveMaterial("Marker", new Color(1f, 0.72f, 0.12f), 2.2f),
                ["PlayerSkin"] = LitMaterial("PlayerSkin", Linear(0.62f, 0.36f, 0.22f), 0.25f, false),
                ["PlayerShirt"] = LitMaterial("PlayerShirt", Linear(0.85f, 0.30f, 0.10f), 0.15f, false),
                ["PlayerShorts"] = LitMaterial("PlayerShorts", Linear(0.42f, 0.33f, 0.18f), 0.1f, false),
                ["PlayerHair"] = LitMaterial("PlayerHair", Linear(0.06f, 0.035f, 0.02f), 0.3f, false),
                ["PlayerHat"] = LitMaterial("PlayerHat", Linear(0.80f, 0.62f, 0.30f), 0.1f, true),
                ["PlayerBand"] = LitMaterial("PlayerBand", Linear(0.25f, 0.07f, 0.04f), 0.2f, false),
                ["PlayerEye"] = LitMaterial("PlayerEye", Linear(0.02f, 0.02f, 0.025f), 0.7f, false),
                ["ShipWhite"] = LitMaterial("ShipWhite", Linear(0.82f, 0.84f, 0.84f), 0.3f, false),
                ["ShipRed"] = LitMaterial("ShipRed", Linear(0.5f, 0.06f, 0.05f), 0.3f, false),
            };

            materials["TerrainMat"].SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTexturePath));
            EditorUtility.SetDirty(materials["TerrainMat"]);
            return materials;
        }

        private static Material LitMaterial(string name, Color color, float smoothness, bool doubleSided)
        {
            Material material = LoadOrCreateMaterial(name, LitShader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Cull", doubleSided ? 0f : 2f);
            material.doubleSidedGI = doubleSided;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material FlameMaterial()
        {
            Material material = LitMaterial("Flame", new Color(1f, 0.55f, 0.1f), 0f, false);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.08f) * 3f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EmissiveMaterial(string name, Color color, float strength)
        {
            Material material = LitMaterial(name, color, 0.4f, false);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material TrailDirtMaterial()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true);
            Color dirt = new Color(0.60f, 0.47f, 0.32f), damp = new Color(0.47f, 0.35f, 0.23f), grass = new Color(0.36f, 0.40f, 0.18f);
            Random.InitState(77);
            Vector2[] pebbles = new Vector2[90];
            for (int i = 0; i < pebbles.Length; i++)
            {
                pebbles[i] = new Vector2(Random.value * size, Random.value * size);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size, v = (float)y / size;
                    float large = TileNoise(u, v, 5f, 3.1f), fine = TileNoise(u, v, 22f, 8.7f);
                    Color color = Color.Lerp(dirt, damp, Mathf.SmoothStep(0.35f, 0.75f, large));
                    color *= 0.88f + 0.24f * fine;
                    float edge = Mathf.Max(Mathf.InverseLerp(0.2f, 0.02f, u), Mathf.InverseLerp(0.8f, 0.98f, u));
                    color = Color.Lerp(color, grass, Mathf.SmoothStep(0f, 1f, edge) * (0.6f + 0.4f * fine));
                    foreach (Vector2 pebble in pebbles)
                    {
                        float dx = Mathf.Abs(x - pebble.x), dy = Mathf.Abs(y - pebble.y);
                        dx = Mathf.Min(dx, size - dx);
                        dy = Mathf.Min(dy, size - dy);
                        float d = dx * dx + dy * dy;
                        if (d < 5f)
                        {
                            color = Color.Lerp(color, new Color(0.62f, 0.58f, 0.52f), 1f - d / 5f);
                        }
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            System.IO.File.WriteAllBytes(TrailDirtTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(TrailDirtTexturePath);
            TextureImporter importer = AssetImporter.GetAtPath(TrailDirtTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapModeU = TextureWrapMode.Clamp;
                importer.wrapModeV = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            Material material = LitMaterial("Dirt", Color.white, 0.05f, false);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TrailDirtTexturePath));
            EditorUtility.SetDirty(material);
            return material;
        }

        // Noise that wraps at the texture's edges, so the trail repeats without seams.
        private static float TileNoise(float u, float v, float frequency, float seed)
        {
            float a = Mathf.PerlinNoise(u * frequency + seed, v * frequency + seed);
            float b = Mathf.PerlinNoise((u - 1f) * frequency + seed, v * frequency + seed);
            float c = Mathf.PerlinNoise(u * frequency + seed, (v - 1f) * frequency + seed);
            float d = Mathf.PerlinNoise((u - 1f) * frequency + seed, (v - 1f) * frequency + seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static Material MarlowEyeMaterial()
        {
            Material material = LitMaterial("MarlowEye", new Color(0.3f, 0.9f, 1f), 0.5f, false);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.25f, 0.85f, 1f) * 2.5f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[LastTide] Shader not found: " + shaderName);
                shader = Shader.Find(LitShader);
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Color Linear(float r, float g, float b)
        {
            return new Color(r, g, b).gamma;
        }

        private static float ScaleIsland(Transform island, Transform terrain)
        {
            Bounds bounds = terrain.GetComponent<Renderer>().bounds;
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            island.localScale = Vector3.one * (IslandWidth / width);
            return width;
        }

        private static int[] SetUpIslandObjects(GameObject island, Dictionary<string, Material> materials)
        {
            GameObject trunkColliders = new GameObject("Trunk Colliders");
            int palms = 0, bushes = 0, rocks = 0;

            foreach (MeshRenderer renderer in island.GetComponentsInChildren<MeshRenderer>())
            {
                GameObject item = renderer.gameObject;
                Mesh mesh = item.GetComponent<MeshFilter>().sharedMesh;
                item.isStatic = true;

                if (item.name == "Terrain")
                {
                    renderer.sharedMaterial = materials["TerrainMat"];
                    item.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
                else if (item.name.StartsWith("Palm"))
                {
                    renderer.sharedMaterials = PalmMaterials(renderer, mesh, materials);
                    AddTrunkCollider(trunkColliders.transform, item.transform.position);
                    palms++;
                }
                else if (item.name.StartsWith("Bush"))
                {
                    bool second = renderer.sharedMaterial != null && renderer.sharedMaterial.name.StartsWith("Bush2");
                    renderer.sharedMaterial = materials[second ? "Bush2" : "Bush"];
                    // Bushes can be walked through, but the campfire needs to know they're there.
                    SphereCollider area = item.AddComponent<SphereCollider>();
                    area.isTrigger = true;
                    area.radius = 0.9f;
                    bushes++;
                }
                else if (item.name.StartsWith("Rock"))
                {
                    renderer.sharedMaterial = materials["Rock"];
                    MeshCollider collider = item.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    collider.convex = true;
                    rocks++;
                }
            }

            return new[] { palms, bushes, rocks };
        }

        // The exporter shares palm meshes between objects, so the material order on each
        // object can't be trusted. Each part is identified from its shape instead:
        // the trunk reaches lowest, and the fronds have the most triangles.
        private static Material[] PalmMaterials(MeshRenderer renderer, Mesh mesh, Dictionary<string, Material> materials)
        {
            bool dry = renderer.sharedMaterials.Any(m => m != null && m.name.StartsWith("PalmLeafDry"));
            Material leaf = materials[dry ? "PalmLeafDry" : "PalmLeaf"];
            Material[] result = new Material[mesh.subMeshCount];
            if (mesh.subMeshCount != 3)
            {
                Debug.LogWarning($"[LastTide] {renderer.name} has {mesh.subMeshCount} material slots, expected 3.");
                for (int i = 0; i < result.Length; i++) result[i] = leaf;
                return result;
            }

            Vector3[] vertices = mesh.vertices;
            int trunk = 0, fronds = 0;
            float lowest = float.MaxValue;
            int mostIndices = -1;
            for (int i = 0; i < 3; i++)
            {
                int[] indices = mesh.GetIndices(i);
                float low = indices.Min(index => renderer.transform.TransformPoint(vertices[index]).y);
                if (low < lowest)
                {
                    lowest = low;
                    trunk = i;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                int count = mesh.GetIndices(i).Length;
                if (i != trunk && count > mostIndices)
                {
                    mostIndices = count;
                    fronds = i;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                result[i] = i == trunk ? materials["Bark"] : i == fronds ? leaf : materials["Coconut"];
            }
            return result;
        }

        private static void AddTrunkCollider(Transform parent, Vector3 position)
        {
            GameObject trunk = new GameObject("Trunk");
            trunk.transform.SetParent(parent, false);
            trunk.transform.position = position;
            trunk.isStatic = true;
            trunk.AddComponent<LastTide.ChoppableTree>();
            CapsuleCollider capsule = trunk.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 10f;
            capsule.center = new Vector3(0f, 5f, 0f);
        }

        private static Vector3 FindSpawnPoint(Transform terrain, GameObject island)
        {
            Mesh mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            List<Vector3> obstacles = island.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name != "Terrain" && !r.name.StartsWith("Bush"))
                .Select(r => r.bounds.center)
                .ToList();

            Vector3 best = terrain.GetComponent<Renderer>().bounds.center;
            float bestZ = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 point = terrain.TransformPoint(vertices[i]);
                if (point.y < 1.8f || point.y > 3.4f || point.z >= bestZ)
                {
                    continue;
                }

                if (terrain.TransformDirection(normals[i]).normalized.y < 0.92f)
                {
                    continue;
                }

                if (obstacles.Any(o => (new Vector3(o.x, point.y, o.z) - point).sqrMagnitude < 16f))
                {
                    continue;
                }

                best = point;
                bestZ = point.z;
            }

            return best + Vector3.up * 0.1f;
        }

        private static LastTide.DayNightCycle CreateSun()
        {
            GameObject sun = new GameObject("Sun");
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
            return sun.AddComponent<LastTide.DayNightCycle>();
        }

        private static void CreateSky()
        {
            Material sky = LoadOrCreateMaterial("Sky", "Skybox/Procedural");
            sky.SetFloat("_SunSize", 0.04f);
            sky.SetFloat("_AtmosphereThickness", 1f);
            sky.SetFloat("_Exposure", 1.25f);
            sky.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.5f));
            sky.SetColor("_GroundColor", new Color(0.05f, 0.25f, 0.45f));
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();
        }

        private static void CreateOcean()
        {
            GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "Ocean";
            Object.DestroyImmediate(ocean.GetComponent<Collider>());
            ocean.transform.localScale = Vector3.one * 600f;
            MeshRenderer renderer = ocean.GetComponent<MeshRenderer>();
            Material water = LoadOrCreateMaterial("Ocean", "LastTide/Ocean");
            water.SetFloat("_FoamWidth", 0.09f);
            water.SetFloat("_DepthRange", 9f);
            water.SetFloat("_RippleScale", 1.8f);
            water.SetFloat("_RippleStrength", 0.12f);
            EditorUtility.SetDirty(water);
            renderer.sharedMaterial = water;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static GameObject CreatePlayer(Vector3 position, Vector3 islandCenter, Dictionary<string, Material> materials)
        {
            GameObject player = new GameObject("Player");
            player.layer = IgnoreRaycastLayer;
            player.transform.position = position;
            Vector3 look = islandCenter - position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
            {
                player.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            }

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 58f;
            controller.stepOffset = 0.4f;
            player.AddComponent<LastTide.PlayerController>();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if (model == null)
            {
                Debug.LogWarning("[LastTide] Player.fbx not found, so the player is a capsule.");
                Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Player.mat");
                AddPlayerPart(player.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.9f, 0.7f), material);
                return player;
            }

            GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(model, player.transform);
            PrefabUtility.UnpackPrefabInstance(character, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            character.name = "Castaway";
            character.transform.localPosition = Vector3.zero;
            foreach (Transform part in character.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = IgnoreRaycastLayer;
                MeshRenderer renderer = part.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    continue;
                }

                string key = PlayerMaterialFor(part.name);
                renderer.sharedMaterial = materials[key];
            }

            player.AddComponent<LastTide.CharacterAnimator>().Configure(character.transform);
            return player;
        }

        private static string PlayerMaterialFor(string part)
        {
            if (part.Contains("Torso") || part.Contains("Shirt") || part.Contains("Sleeve")) return "PlayerShirt";
            if (part.Contains("Shorts") || part.Contains("Waist")) return "PlayerShorts";
            if (part.Contains("Hair")) return "PlayerHair";
            if (part.Contains("HatBand")) return "PlayerBand";
            if (part.Contains("Hat")) return "PlayerHat";
            if (part.Contains("Eye")) return "PlayerEye";
            return "PlayerSkin";
        }

        private static void AddPlayerPart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.layer = IgnoreRaycastLayer;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = target.position - target.forward * 6f + Vector3.up * 3f;
            cameraObject.transform.LookAt(target.position + Vector3.up * 1.5f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.farClipPlane = 4000f;
            camera.nearClipPlane = 0.1f;
            cameraObject.AddComponent<AudioListener>();
            LastTide.ThirdPersonCamera orbit = cameraObject.AddComponent<LastTide.ThirdPersonCamera>();
            orbit.SetTarget(target);
            EditorUtility.SetDirty(orbit);
        }

        private static Dictionary<string, GameObject> CreateItemPrefabs(Dictionary<string, Material> materials)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ItemsPath);
            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>
            {
                ["Wood"] = PickupPrefab(model, "Wood", LastTide.ItemType.Wood, false, materials,
                    ("Wood_Bark", "Wood"), ("Wood_Ends", "WoodEnds"), ("Wood", "Wood")),
                ["Stone"] = PickupPrefab(model, "Stone", LastTide.ItemType.Stone, false, materials, ("Stone", "Stone")),
                ["Vine"] = PickupPrefab(model, "Vine", LastTide.ItemType.Vine, true, materials,
                    ("Vine", "Vine"), ("Vine_Leaves", "VineLeaf")),
                ["Coconut"] = PickupPrefab(model, "Coconut", LastTide.ItemType.Coconut, false, materials, ("Coconut", "Coconut")),
            };

            GameObject knife = new GameObject("Knife");
            AddPart(model, "Knife_Blade", knife.transform, materials["Steel"]);
            AddPart(model, "Knife_Handle", knife.transform, materials["Wood"]);
            prefabs["Knife"] = SavePrefab(knife);

            GameObject axe = new GameObject("Axe");
            AddPart(model, "Axe_Handle", axe.transform, materials["Wood"]);
            AddPart(model, "Axe_Head", axe.transform, materials["Steel"]);
            prefabs["Axe"] = SavePrefab(axe);

            GameObject campfire = new GameObject("Campfire");
            AddPart(model, "Campfire_Logs", campfire.transform, materials["Wood"]);
            AddPart(model, "Campfire_Stones", campfire.transform, materials["Stone"]);
            Transform flame = new GameObject("Flame").transform;
            flame.SetParent(campfire.transform, false);
            AddPart(model, "Campfire_Flame", flame, materials["Flame"]).shadowCastingMode = ShadowCastingMode.Off;
            GameObject lightObject = new GameObject("Fire Light");
            lightObject.transform.SetParent(campfire.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1f, 0f);
            Light fireLight = lightObject.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.25f);
            fireLight.intensity = 6f;
            fireLight.range = 10f;
            campfire.AddComponent<LastTide.FlameFlicker>().Configure(flame, fireLight);
            campfire.AddComponent<LastTide.CampfireRest>();
            prefabs["Campfire"] = SavePrefab(campfire);
            return prefabs;
        }

        // Parts missing from an older Items.fbx are skipped, so "Wood" is only used when "Wood_Bark" isn't there.
        private static GameObject PickupPrefab(GameObject model, string name, LastTide.ItemType item, bool needsKnife,
            Dictionary<string, Material> materials, params (string part, string material)[] parts)
        {
            GameObject root = new GameObject(name);
            bool hasBark = model.transform.Find("Wood_Bark") != null;
            foreach ((string part, string material) in parts)
            {
                if (model.transform.Find(part) == null || (part == "Wood" && hasBark))
                {
                    continue;
                }

                AddPart(model, part, root.transform, materials[material]);
            }

            AddGlint(model, root.transform, materials["Glint"]);
            root.AddComponent<LastTide.Pickup>().Configure(item, needsKnife);
            return SavePrefab(root);
        }

        private static void AddGlint(GameObject model, Transform parent, Material material)
        {
            GameObject glint;
            if (model.transform.Find("Glint") != null)
            {
                glint = AddPart(model, "Glint", parent, material).gameObject;
            }
            else
            {
                glint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(glint.GetComponent<Collider>());
                glint.transform.SetParent(parent, false);
                glint.transform.localScale = Vector3.one * 0.12f;
                glint.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            glint.name = "Glint";
            MeshRenderer renderer = glint.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            glint.AddComponent<LastTide.ItemGlint>();
        }

        private static MeshRenderer AddPart(GameObject model, string part, Transform parent, Material material)
        {
            Transform source = model.transform.Find(part);
            if (source == null)
            {
                Debug.LogError("[LastTide] Items.fbx has no part named " + part);
                return null;
            }

            GameObject copy = Object.Instantiate(source.gameObject, parent, false);
            copy.name = part;
            MeshRenderer renderer = copy.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static GameObject SavePrefab(GameObject root)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{root.name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddPlayerSystems(GameObject player, Dictionary<string, GameObject> prefabs, LastTide.DayNightCycle cycle)
        {
            player.AddComponent<LastTide.Inventory>();
            player.AddComponent<LastTide.GameHud>();
            player.AddComponent<LastTide.PlayerInteractor>();
            player.AddComponent<LastTide.DevTeleport>();
            LastTide.SurvivalStats stats = player.AddComponent<LastTide.SurvivalStats>();
            stats.Configure(cycle);
            EditorUtility.SetDirty(stats);
            LastTide.CraftingSystem crafting = player.AddComponent<LastTide.CraftingSystem>();
            Transform rightArm = FindChild(player.transform, "ArmR");
            Transform leftArm = FindChild(player.transform, "ArmL");
            GameObject knife = rightArm != null
                ? HandItem(prefabs["Knife"], player.transform, rightArm, new Vector3(80f, 0f, 0f), 0.04f)
                : HeldItem(prefabs["Knife"], player.transform, new Vector3(0.42f, 0.85f, 0.12f), new Vector3(70f, 0f, 0f));
            GameObject axe = leftArm != null
                ? HandItem(prefabs["Axe"], player.transform, leftArm, new Vector3(70f, 0f, 0f), 0.14f)
                : HeldItem(prefabs["Axe"], player.transform, new Vector3(-0.42f, 0.5f, 0.12f), new Vector3(35f, 0f, 0f));
            crafting.Configure(prefabs["Campfire"], knife, axe);
            EditorUtility.SetDirty(crafting);
        }

        private static Transform FindChild(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        }

        // Puts a tool in the character's hand, gripped a little way along its handle, so it swings with the arm.
        private static GameObject HandItem(GameObject prefab, Transform player, Transform arm, Vector3 rotation, float grip)
        {
            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, arm);
            Vector3 hand = arm.position + Vector3.down * 0.56f + player.forward * 0.02f;
            item.transform.rotation = player.rotation * Quaternion.Euler(rotation);
            item.transform.position = hand - item.transform.up * grip;
            foreach (Transform part in item.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = IgnoreRaycastLayer;
            }

            item.SetActive(false);
            return item;
        }

        private static GameObject HeldItem(GameObject prefab, Transform parent, Vector3 position, Vector3 rotation)
        {
            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            item.transform.localPosition = position;
            item.transform.localEulerAngles = rotation;
            item.SetActive(false);
            return item;
        }

        private static int[] ScatterItems(Transform terrain, GameObject island, Vector3 spawn, Dictionary<string, GameObject> prefabs)
        {
            Random.InitState(1984);
            Transform group = new GameObject("Items").transform;
            Mesh mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            List<KeyValuePair<Vector3, float>> ground = new List<KeyValuePair<Vector3, float>>();
            for (int i = 0; i < vertices.Length; i++)
            {
                ground.Add(new KeyValuePair<Vector3, float>(
                    terrain.TransformPoint(vertices[i]),
                    terrain.TransformDirection(normals[i]).normalized.y));
            }

            List<Vector3> palms = new List<Vector3>();
            List<Vector3> bushes = new List<Vector3>();
            foreach (MeshRenderer renderer in island.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name.StartsWith("Palm")) palms.Add(renderer.transform.position);
                if (renderer.name.StartsWith("Bush")) bushes.Add(renderer.bounds.center);
            }

            List<Vector3> used = new List<Vector3>();

            // A few of the first materials sit close to where the player starts.
            int wood = Place(prefabs["Wood"], 3, group, ground, used, 2f,
                g => g.Value > 0.85f && g.Key.y > 1.2f && Flat(g.Key, spawn) > 4f && Flat(g.Key, spawn) < 18f);
            int stone = Place(prefabs["Stone"], 3, group, ground, used, 2f,
                g => g.Value > 0.85f && g.Key.y > 1.2f && Flat(g.Key, spawn) > 4f && Flat(g.Key, spawn) < 18f);

            wood += Place(prefabs["Wood"], 24, group, ground, used, 4f, g => g.Value > 0.8f && g.Key.y > 1.8f && g.Key.y < 18f);
            stone += Place(prefabs["Stone"], 20, group, ground, used, 4f, g => g.Value > 0.75f && g.Key.y > 1.8f && g.Key.y < 25f);
            int vines = Place(prefabs["Vine"], 14, group, ground, used, 3f,
                g => g.Value > 0.75f && g.Key.y > 3f && bushes.Any(b => Flat(g.Key, b) > 2.2f && Flat(g.Key, b) < 5f));
            int coconuts = Place(prefabs["Coconut"], 22, group, ground, used, 2f,
                g => g.Value > 0.75f && g.Key.y > 1.2f && palms.Any(p => Flat(g.Key, p) > 0.8f && Flat(g.Key, p) < 4.5f));
            return new[] { wood, stone, vines, coconuts };
        }

        private static int Place(GameObject prefab, int count, Transform parent, List<KeyValuePair<Vector3, float>> ground,
            List<Vector3> used, float spacing, System.Func<KeyValuePair<Vector3, float>, bool> allowed)
        {
            List<Vector3> options = ground.Where(allowed).Select(g => g.Key).OrderBy(_ => Random.value).ToList();
            int placed = 0;
            foreach (Vector3 point in options)
            {
                if (placed >= count)
                {
                    break;
                }

                if (used.Any(u => (u - point).sqrMagnitude < spacing * spacing))
                {
                    continue;
                }

                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                item.transform.position = point;
                item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                used.Add(point);
                placed++;
            }

            return placed;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return (a - b).magnitude;
        }

        private static void AllowLocalServerInEditor()
        {
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length == 0)
            {
                return;
            }

            // 1 allows plain HTTP in the editor and development builds only; released builds need HTTPS.
            SerializedObject serialized = new SerializedObject(settings[0]);
            SerializedProperty option = serialized.FindProperty("insecureHttpOption");
            if (option != null)
            {
                option.intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject CreateMarlowPrefab(Dictionary<string, Material> materials)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(MarlowPath);
            GameObject root = new GameObject("Marlow");
            AddPart(model, "Marlow_Shell", root.transform, materials["MarlowShell"]);
            AddPart(model, "Marlow_Trim", root.transform, materials["MarlowTrim"]);
            AddPart(model, "Marlow_Eye", root.transform, materials["MarlowEye"]);
            AddPart(model, "Marlow_Rust", root.transform, materials["MarlowRust"]);
            root.AddComponent<LastTide.Marlow>();
            root.AddComponent<LastTide.MarlowChat>();
            return SavePrefab(root);
        }

        private static GameObject PlaceMarlow(GameObject prefab, Transform terrain, GameObject island, Vector3 spawn, GameObject player, LastTide.DayNightCycle cycle)
        {
            Mesh mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            List<Vector3> obstacles = island.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name != "Terrain")
                .Select(r => r.bounds.center)
                .ToList();

            Vector3 best = spawn + Vector3.right * 8f;
            float bestScore = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 point = terrain.TransformPoint(vertices[i]);
                float distance = Flat(point, spawn);
                if (distance < 7f || distance > 16f || point.y < 1.8f || point.y > 3.4f)
                {
                    continue;
                }

                if (terrain.TransformDirection(normals[i]).normalized.y < 0.9f ||
                    obstacles.Any(o => Flat(o, point) < 3f))
                {
                    continue;
                }

                float score = Mathf.Abs(distance - 10f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = point;
                }
            }

            GameObject marlow = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            marlow.transform.position = best + Vector3.up * 0.12f;
            marlow.transform.rotation = Quaternion.Euler(28f, Random.Range(0f, 360f), 18f);
            Transform beam = new GameObject("Marlow Beam").transform;
            GameObject beamShaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(beamShaft.GetComponent<Collider>());
            beamShaft.transform.SetParent(beam, false);
            beamShaft.transform.localPosition = new Vector3(0f, 45f, 0f);
            beamShaft.transform.localScale = new Vector3(0.35f, 45f, 0.35f);
            MeshRenderer beamRenderer = beamShaft.GetComponent<MeshRenderer>();
            beamRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Beam.mat");
            beamRenderer.shadowCastingMode = ShadowCastingMode.Off;
            beam.gameObject.SetActive(false);

            marlow.GetComponent<LastTide.Marlow>().Configure(player.transform, beam);
            marlow.GetComponent<LastTide.MarlowChat>().Configure(player, cycle);
            EditorUtility.SetDirty(marlow);
            Debug.Log($"[LastTide] Marlow is buried at {best}.");
            CreateGuideMarker(marlow, player);
            return marlow;
        }

        private static void CreateGuideMarker(GameObject marlow, GameObject player)
        {
            GameObject items = AssetDatabase.LoadAssetAtPath<GameObject>(ItemsPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Marker.mat");
            Transform arrow = new GameObject("Robot Arrow").transform;
            if (items.transform.Find("Marker_Arrow") != null)
            {
                AddPart(items, "Marker_Arrow", arrow, material);
            }
            else
            {
                GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.transform.SetParent(arrow, false);
                shaft.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                shaft.transform.localScale = new Vector3(0.18f, 0.25f, 0.18f);
                GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tip.transform.SetParent(arrow, false);
                tip.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                tip.transform.localScale = Vector3.one * 0.32f;
                foreach (GameObject part in new[] { shaft, tip })
                {
                    Object.DestroyImmediate(part.GetComponent<Collider>());
                    part.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
            }

            foreach (MeshRenderer renderer in arrow.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            arrow.localScale = Vector3.one * 1.5f;
            arrow.position = marlow.transform.position + Vector3.up * 2.2f;
            GameObject guide = new GameObject("Guide Marker");
            guide.AddComponent<LastTide.GuideMarker>().Configure(marlow.GetComponent<LastTide.Marlow>(), player.transform, arrow);
        }

        private static void BuildQuest(Dictionary<string, Material> materials, Transform terrain, GameObject island, Vector3 spawn,
            GameObject player, GameObject marlow, LastTide.DayNightCycle cycle, TrailData trail)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(QuestPath);
            Transform group = new GameObject("Quest").transform;
            List<WalkPoint> walkable = FindWalkableGround(terrain, spawn);
            List<Vector3> obstacles = island.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name != "Terrain")
                .Select(r => r.bounds.center)
                .ToList();
            Vector3 center = terrain.GetComponent<Renderer>().bounds.center;
            center.y = 0f;

            // The trail ends at the beacon, on the highest ground the player can walk to.
            WalkPoint top = new WalkPoint(trail.Points[trail.Points.Length - 1], 1f);
            GameObject beaconObject = new GameObject("Beacon");
            beaconObject.transform.SetParent(group, false);
            beaconObject.transform.position = top.Position;
            AddQuestPart(model, "Beacon_Base", beaconObject.transform, materials["Stone"]);
            AddQuestPart(model, "Beacon_Frame", beaconObject.transform, materials["MarlowTrim"]);
            MeshRenderer lamp = AddQuestPart(model, "Beacon_Lamp", beaconObject.transform, materials["LampOff"]);
            CapsuleCollider beaconCollider = beaconObject.AddComponent<CapsuleCollider>();
            beaconCollider.radius = 1.1f;
            beaconCollider.height = 4.5f;
            beaconCollider.center = new Vector3(0f, 2.25f, 0f);
            GameObject beaconLightObject = new GameObject("Beacon Light");
            beaconLightObject.transform.SetParent(beaconObject.transform, false);
            beaconLightObject.transform.localPosition = new Vector3(0f, 3f, 0f);
            Light beaconLight = beaconLightObject.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(1f, 0.85f, 0.5f);
            beaconLight.intensity = 25f;
            beaconLight.range = 45f;
            beaconLight.enabled = false;

            // The antenna lies beside the trail, close to the beacon.
            Vector3 peakPoint = trail.Points[trail.AntennaIndex] + trail.AntennaSide * 1.4f + Vector3.up * 0.08f;
            GameObject peakPart = CreateBeaconPart("Antenna Part", group, peakPoint, LastTide.ItemType.Antenna, false, LastTide.ItemType.Knife,
                string.Empty, "Take the beacon antenna");
            AddQuestPart(model, "Part_Antenna", peakPart.transform, materials["PartGlow"]);

            // The wreck lies on the beach farthest from the start.
            Vector3 wreckPoint = walkable
                .Where(p => p.Position.y > 1.2f && p.Position.y < 3.2f && p.Up > 0.88f && !obstacles.Any(o => Flat(o, p.Position) < 7f))
                .OrderByDescending(p => Flat(p.Position, spawn))
                .Select(p => p.Position)
                .DefaultIfEmpty(spawn + Vector3.forward * 40f)
                .First();
            Vector3 outward = wreckPoint - center;
            outward.y = 0f;
            outward.Normalize();
            GameObject wreck = new GameObject("Shipwreck");
            wreck.transform.SetParent(group, false);
            wreck.transform.position = wreckPoint - Vector3.up * 0.2f;
            wreck.transform.rotation = Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(-6f, 0f, 14f);
            AddQuestCollider(AddQuestPart(model, "Wreck_Hull", wreck.transform, materials["WreckWood"]));
            AddQuestPart(model, "Wreck_Mast", wreck.transform, materials["Wood"]);
            Vector3 crateSide = Vector3.Cross(Vector3.up, outward);
            Vector3 cratePoint = walkable
                .Where(p => Flat(p.Position, wreckPoint) > 5f && Flat(p.Position, wreckPoint) < 11f && p.Position.y > 1f && p.Up > 0.85f)
                .OrderBy(p => Vector3.Dot(p.Position - wreckPoint, outward))
                .Select(p => p.Position)
                .DefaultIfEmpty(wreckPoint + crateSide * 7f)
                .First();
            GameObject crate = CreateBeaconPart("Battery Crate", group, cratePoint, LastTide.ItemType.Battery, true, LastTide.ItemType.Axe,
                "This crate needs an axe to open", "Smash the crate and take the battery");
            AddQuestPart(model, "Crate", crate.transform, materials["WreckWood"]);
            AddQuestPart(model, "Part_Battery", crate.transform, materials["PartGlow"]);

            // The shelter faces the trail from a levelled spot on its uphill side.
            Vector3 downhill = -trail.ShelterSide;
            Vector3 shelterPoint = trail.Points[trail.ShelterIndex] + trail.ShelterSide * 3.8f;
            GameObject shelter = new GameObject("Rock Shelter");
            shelter.transform.SetParent(group, false);
            shelter.transform.position = shelterPoint;
            shelter.transform.rotation = Quaternion.LookRotation(downhill, Vector3.up);
            MeshRenderer shelterShell = AddQuestPart(model, "Shelter_Shell", shelter.transform, materials["CaveRock"]);
            AddQuestCollider(AddQuestPart(model, "Shelter_Rock", shelter.transform, materials["Rock"]));

            // The hollow must extend away from the trail, with its opening facing the path.
            if (Vector3.Dot(shelterShell.bounds.center - shelterPoint, downhill) > 0f)
            {
                shelter.transform.rotation = Quaternion.LookRotation(-downhill, Vector3.up);
            }
            GameObject lensPart = CreateBeaconPart("Lens Part", group, shelterPoint - downhill * 1.2f, LastTide.ItemType.Lens,
                true, LastTide.ItemType.Knife, "Vines hold the lens here. You need a knife", "Cut the vines and take the lens");
            AddQuestPart(model, "Part_Pedestal", lensPart.transform, materials["Stone"]);
            AddQuestPart(model, "Part_Lens", lensPart.transform, materials["PartGlow"]);

            // Loose items shouldn't overlap the new landmarks.
            RemoveItemsNear(new[] { (wreckPoint, 9f), (shelterPoint, 9f), (top.Position, 4f) });

            GameObject ship = new GameObject("Rescue Ship");
            ship.transform.SetParent(group, false);
            AddQuestPart(model, "Ship_Hull", ship.transform, materials["ShipRed"]);
            AddQuestPart(model, "Ship_Cabin", ship.transform, materials["ShipWhite"]);
            AddQuestPart(model, "Ship_Stack", ship.transform, materials["MarlowTrim"]);

            group.gameObject.AddComponent<LastTide.QuestTrail>().Configure(trail.Points, trail.ShelterIndex, trail.AntennaIndex);

            GameObject managerObject = new GameObject("Quest Manager");
            LastTide.QuestManager manager = managerObject.AddComponent<LastTide.QuestManager>();
            manager.Configure(player, marlow.GetComponent<LastTide.Marlow>(), beaconObject.transform, crate.transform,
                lensPart.transform, peakPart.transform, ship, cycle, center);
            beaconObject.AddComponent<LastTide.Beacon>().Configure(lamp, materials["LampOn"], beaconLight, manager);
            marlow.GetComponent<LastTide.MarlowChat>().SetQuest(manager);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(marlow);

            Debug.Log($"[LastTide] Quest placed. Trail {trail.Points.Length} points; beacon {top.Position}; antenna {peakPoint}; " +
                      $"crate {cratePoint}; shelter {shelterPoint}.");
        }

        private readonly struct WalkPoint
        {
            public readonly Vector3 Position;
            public readonly float Up;

            public WalkPoint(Vector3 position, float up)
            {
                Position = position;
                Up = up;
            }
        }

        private sealed class TerrainGraph
        {
            public List<Vector3> Positions = new List<Vector3>();
            public List<float> Ups = new List<float>();
            public List<HashSet<int>> Links = new List<HashSet<int>>();

            public int Nearest(Vector3 point)
            {
                int best = 0;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < Positions.Count; i++)
                {
                    float distance = (Positions[i] - point).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = i;
                    }
                }
                return best;
            }
        }

        // Connects terrain points only across surfaces the player can climb.
        private static TerrainGraph BuildTerrainGraph(Transform terrain)
        {
            const float minWalkableUp = 0.57f;
            Mesh mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            TerrainGraph graph = new TerrainGraph();
            Dictionary<Vector3Int, int> ids = new Dictionary<Vector3Int, int>();
            int[] node = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = terrain.TransformPoint(vertices[i]);
                Vector3Int key = Vector3Int.RoundToInt(world * 100f);
                if (!ids.TryGetValue(key, out int id))
                {
                    id = graph.Positions.Count;
                    ids[key] = id;
                    graph.Positions.Add(world);
                    graph.Ups.Add(terrain.TransformDirection(normals[i]).normalized.y);
                    graph.Links.Add(new HashSet<int>());
                }
                node[i] = id;
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = node[triangles[i]], b = node[triangles[i + 1]], c = node[triangles[i + 2]];
                Vector3 pa = graph.Positions[a], pb = graph.Positions[b], pc = graph.Positions[c];
                if (Mathf.Abs(Vector3.Cross(pb - pa, pc - pa).normalized.y) < minWalkableUp)
                {
                    continue;
                }

                graph.Links[a].Add(b); graph.Links[a].Add(c);
                graph.Links[b].Add(a); graph.Links[b].Add(c);
                graph.Links[c].Add(a); graph.Links[c].Add(b);
            }

            return graph;
        }

        private static List<WalkPoint> FindWalkableGround(Transform terrain, Vector3 spawn)
        {
            TerrainGraph graph = BuildTerrainGraph(terrain);
            bool[] seen = new bool[graph.Positions.Count];
            Queue<int> queue = new Queue<int>();
            int start = graph.Nearest(spawn);
            seen[start] = true;
            queue.Enqueue(start);
            List<WalkPoint> result = new List<WalkPoint>();
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                result.Add(new WalkPoint(graph.Positions[current], graph.Ups[current]));
                foreach (int next in graph.Links[current])
                {
                    if (!seen[next] && graph.Positions[next].y >= 0.3f)
                    {
                        seen[next] = true;
                        queue.Enqueue(next);
                    }
                }
            }

            return result;
        }

        private sealed class TrailData
        {
            public Vector3[] Points;
            public int ShelterIndex;
            public int AntennaIndex;
            public Vector3 ShelterSide;
            public Vector3 AntennaSide;
        }

        // Carves a climbable dirt trail from the beach to the highest walkable point,
        // so the route to the beacon is guaranteed and easy to see.
        private static TrailData CarveTrail(Transform terrain, GameObject island, Vector3 spawn)
        {
            const float maxGrade = 0.5f, sampleSpacing = 1.5f;
            // Scenery on the trail gets removed, which isn't allowed inside a prefab instance.
            PrefabUtility.UnpackPrefabInstance(island, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            MeshFilter filter = terrain.GetComponent<MeshFilter>();
            TerrainGraph graph = BuildTerrainGraph(terrain);
            List<Vector3> blockers = island.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name.StartsWith("Palm") || r.name.StartsWith("Rock"))
                .Select(r => r.transform.position)
                .ToList();
            Vector3 top = FindWalkableGround(terrain, spawn).OrderByDescending(p => p.Position.y).First().Position;
            List<Vector3> route = SmoothRoute(FindGentleRoute(graph, spawn, top, blockers), sampleSpacing);

            // Follow the ground where it's gentle, then limit the grade in both directions.
            float[] heights = route.Select(p => p.y).ToArray();
            for (int i = 1; i < heights.Length; i++)
            {
                heights[i] = Mathf.Min(heights[i], heights[i - 1] + maxGrade * sampleSpacing);
            }

            heights[heights.Length - 1] = top.y;
            for (int i = heights.Length - 2; i >= 0; i--)
            {
                heights[i] = Mathf.Max(heights[i], heights[i + 1] - maxGrade * sampleSpacing);
            }

            for (int i = 0; i < route.Count; i++)
            {
                route[i] = new Vector3(route[i].x, heights[i], route[i].z);
            }

            TrailData trail = new TrailData { Points = route.ToArray() };
            trail.ShelterIndex = Mathf.RoundToInt(route.Count * 0.55f);
            trail.AntennaIndex = Mathf.RoundToInt(route.Count * 0.85f);
            trail.ShelterSide = UphillSide(graph, route, trail.ShelterIndex);
            trail.AntennaSide = UphillSide(graph, route, trail.AntennaIndex);

            Vector3 shelterSpot = route[trail.ShelterIndex] + trail.ShelterSide * 8f;
            float shelterHeight = route[trail.ShelterIndex].y;
            Vector3 summit = route[route.Count - 1];

            float Edit(Vector3 point)
            {
                float y = point.y;
                float nearest = float.MaxValue;
                float routeHeight = y;
                for (int i = 0; i < route.Count; i++)
                {
                    float distance = Flat(point, route[i]);
                    if (distance < nearest)
                    {
                        nearest = distance;
                        routeHeight = route[i].y;
                    }
                }

                // The ground drops slightly under the trail, so it can't poke through the path surface.
                y = Mathf.Lerp(routeHeight - 0.14f, y, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3f, 8f, nearest)));
                y = Mathf.Lerp(shelterHeight, y, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9f, 13f, Flat(point, shelterSpot))));
                y = Mathf.Lerp(summit.y, y, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 8f, Flat(point, summit))));
                return y;
            }

            bool Cleared(Vector3 point)
            {
                return route.Any(r => Flat(point, r) < 3.2f) || Flat(point, shelterSpot) < 9f || Flat(point, summit) < 4.5f;
            }

            Mesh carved = Object.Instantiate(filter.sharedMesh);
            carved.name = "TerrainWithTrail";
            Vector3[] vertices = carved.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = terrain.TransformPoint(vertices[i]);
                world.y = Edit(world);
                vertices[i] = terrain.InverseTransformPoint(world);
            }

            carved.vertices = vertices;
            carved.RecalculateNormals();
            carved.RecalculateBounds();
            const string meshPath = "Assets/Models/TerrainWithTrail.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(carved, meshPath);
            PaintTrail(filter.sharedMesh, terrain, route);
            filter.sharedMesh = carved;
            terrain.GetComponent<MeshCollider>().sharedMesh = carved;

            // Move nearby scenery with the ground, and clear anything left on the trail.
            List<Transform> scenery = island.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name != "Terrain")
                .Select(r => r.transform)
                .ToList();
            GameObject trunks = GameObject.Find("Trunk Colliders");
            if (trunks != null)
            {
                scenery.AddRange(trunks.transform.Cast<Transform>());
            }

            foreach (Transform item in scenery)
            {
                if (Cleared(item.position))
                {
                    Object.DestroyImmediate(item.gameObject);
                    continue;
                }

                Vector3 position = item.position;
                position.y = Edit(position);
                item.position = position;
            }

            return trail;
        }

        // A smooth walkable path with its own collision, so the coarse terrain can't snag the player.
        // A smooth path with its own collision. Its edges slope down into the ground, and it rises out of the sand at the start.
        private static void CreateTrailSurface(TrailData trail, Material material, Collider ground)
        {
            Physics.SyncTransforms();
            float[] across = { -3.1f, -2.6f, -1.7f, 0f, 1.7f, 2.6f, 3.1f };
            float[] lift = { -0.45f, -0.12f, 0.04f, 0.08f, 0.04f, -0.12f, -0.45f };
            float[] uCoordinates = { 0f, 0.1f, 0.2f, 0.5f, 0.8f, 0.9f, 1f };
            const int startFade = 10;
            Vector3[] points = trail.Points;
            int columns = across.Length;
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            float travelled = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0)
                {
                    travelled += Flat(points[i], points[i - 1]);
                }

                Vector3 along = points[Mathf.Min(i + 1, points.Length - 1)] - points[Mathf.Max(i - 1, 0)];
                along.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, along.normalized);
                float sink = i < startFade ? 0.35f * (1f - (float)i / startFade) : 0f;
                for (int c = 0; c < columns; c++)
                {
                    // The path narrows to a rounded tip where it starts on the beach.
                    float width = i < startFade ? Mathf.Lerp(0.3f, 1f, Mathf.Sqrt((float)i / startFade)) : 1f;
                    Vector3 vertex = points[i] + side * (across[c] * width) + Vector3.up * (lift[c] - sink);
                    // The path's edges settle onto the real ground, so no raised slab edge shows.
                    if (c != 3 && ground != null && ground.Raycast(new Ray(vertex + Vector3.up * 4f, Vector3.down), out RaycastHit hit, 8f))
                    {
                        float groundY = hit.point.y;
                        if (c == 1 || c == columns - 2)
                        {
                            vertex.y = groundY + 0.03f - sink;
                        }
                        else if (c == 0 || c == columns - 1)
                        {
                            vertex.y = Mathf.Min(vertex.y, groundY - 0.35f);
                        }
                        else
                        {
                            vertex.y = Mathf.Max(vertex.y, groundY + 0.04f - sink);
                        }
                    }

                    vertices.Add(vertex);
                    uvs.Add(new Vector2(uCoordinates[c], travelled / 3f));
                }

                if (i == 0)
                {
                    continue;
                }

                int a = (i - 1) * columns, b = i * columns;
                for (int c = 0; c < columns - 1; c++)
                {
                    triangles.Add(a + c); triangles.Add(b + c); triangles.Add(a + c + 1);
                    triangles.Add(a + c + 1); triangles.Add(b + c); triangles.Add(b + c + 1);
                }
            }

            Mesh mesh = new Mesh { name = "TrailSurface" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            const string path = "Assets/Models/TrailSurface.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            GameObject surface = new GameObject("Trail Surface");
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            surface.AddComponent<MeshRenderer>().sharedMaterial = material;
            surface.AddComponent<MeshCollider>().sharedMesh = mesh;
            surface.isStatic = true;

            // Report the facing, because a path facing down would be invisible and uncollidable from above.
            Debug.Log($"[LastTide] Trail surface faces {(mesh.normals[columns * 2 + 3].y > 0f ? "up" : "DOWN")}.");
        }

        // Small stones half buried along both edges of the trail.
        private static void AddTrailStones(TrailData trail, GameObject island, Material material)
        {
            Mesh[] rocks = island.GetComponentsInChildren<MeshFilter>()
                .Where(f => f.name.StartsWith("Rock"))
                .Select(f => f.sharedMesh)
                .Distinct()
                .ToArray();
            if (rocks.Length == 0)
            {
                return;
            }

            Random.InitState(4242);
            Transform group = new GameObject("Trail Stones").transform;
            Vector3[] points = trail.Points;
            int placed = 0;
            for (int i = 3; i < points.Length - 3; i += 2)
            {
                Vector3 along = points[i + 1] - points[i - 1];
                along.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, along.normalized);
                foreach (float direction in new[] { -1f, 1f })
                {
                    if (Random.value < 0.35f)
                    {
                        continue;
                    }

                    GameObject stone = new GameObject("Trail Stone");
                    stone.transform.SetParent(group, false);
                    stone.transform.position = points[i] + side * direction * Random.Range(2.15f, 2.45f) + along.normalized * Random.Range(-0.6f, 0.6f)
                                               + Vector3.down * 0.12f;
                    stone.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                    float size = Random.Range(0.13f, 0.28f);
                    stone.transform.localScale = new Vector3(size * Random.Range(0.9f, 1.4f), size * 0.7f, size);
                    stone.AddComponent<MeshFilter>().sharedMesh = rocks[Random.Range(0, rocks.Length)];
                    MeshRenderer renderer = stone.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    stone.isStatic = true;
                    placed++;
                }
            }

            Debug.Log($"[LastTide] Placed {placed} stones along the trail.");
        }

        private static Vector3 UphillSide(TerrainGraph graph, List<Vector3> route, int index)
        {
            Vector3 along = route[Mathf.Min(index + 2, route.Count - 1)] - route[Mathf.Max(index - 2, 0)];
            along.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, along.normalized);
            float right = graph.Positions[graph.Nearest(route[index] + side * 7f)].y;
            float left = graph.Positions[graph.Nearest(route[index] - side * 7f)].y;
            return right >= left ? side : -side;
        }

        // Finds a route that prefers gentle ground and stays away from trees and rocks.
        private static List<Vector3> FindGentleRoute(TerrainGraph graph, Vector3 from, Vector3 to, List<Vector3> blockers)
        {
            int count = graph.Positions.Count;
            int start = graph.Nearest(from), goal = graph.Nearest(to);
            float[] cost = Enumerable.Repeat(float.MaxValue, count).ToArray();
            int[] previous = Enumerable.Repeat(-1, count).ToArray();
            bool[] done = new bool[count];
            bool[] nearBlocker = new bool[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 point = graph.Positions[i];
                nearBlocker[i] = blockers.Any(b => Flat(b, point) < 3.5f);
            }

            cost[start] = 0f;
            List<int> open = new List<int> { start };
            while (open.Count > 0)
            {
                int bestIndex = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (cost[open[i]] < cost[open[bestIndex]])
                    {
                        bestIndex = i;
                    }
                }

                int current = open[bestIndex];
                open[bestIndex] = open[open.Count - 1];
                open.RemoveAt(open.Count - 1);
                if (done[current])
                {
                    continue;
                }

                done[current] = true;
                if (current == goal)
                {
                    break;
                }

                foreach (int next in graph.Links[current])
                {
                    Vector3 step = graph.Positions[next] - graph.Positions[current];
                    if (done[next] || graph.Positions[next].y < 0.5f)
                    {
                        continue;
                    }

                    float run = Mathf.Max(0.05f, new Vector2(step.x, step.z).magnitude);
                    float grade = Mathf.Abs(step.y) / run;
                    float total = cost[current] + step.magnitude * (1f + 5f * grade * grade + (nearBlocker[next] ? 3f : 0f));
                    if (total < cost[next])
                    {
                        cost[next] = total;
                        previous[next] = current;
                        open.Add(next);
                    }
                }
            }

            List<Vector3> route = new List<Vector3>();
            for (int at = goal; at != -1; at = previous[at])
            {
                route.Add(graph.Positions[at]);
            }

            route.Reverse();
            return route;
        }

        private static List<Vector3> SmoothRoute(List<Vector3> raw, float spacing)
        {
            // Smoothing evenly spaced points turns tight bends into wide curves the path can follow.
            List<Vector3> points = Resample(Soften(raw, 8), spacing);
            points = Resample(Soften(points, 60), spacing);
            if (points.Count > 2 && Flat(points[points.Count - 1], points[points.Count - 2]) < spacing * 0.6f)
            {
                points.RemoveAt(points.Count - 2);
            }

            return points;
        }

        private static List<Vector3> Soften(List<Vector3> source, int passes)
        {
            List<Vector3> points = new List<Vector3>(source);
            for (int pass = 0; pass < passes; pass++)
            {
                List<Vector3> next = new List<Vector3>(points);
                for (int i = 1; i < points.Count - 1; i++)
                {
                    next[i] = (points[i - 1] + points[i] * 2f + points[i + 1]) * 0.25f;
                }
                points = next;
            }
            return points;
        }

        private static List<Vector3> Resample(List<Vector3> points, float spacing)
        {
            List<Vector3> result = new List<Vector3> { points[0] };
            float carried = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = points[i - 1], b = points[i];
                float length = Flat(a, b);
                float at = spacing - carried;
                while (at <= length && length > 0f)
                {
                    result.Add(Vector3.Lerp(a, b, at / length));
                    at += spacing;
                }
                carried = length - (at - spacing);
            }

            result.Add(points[points.Count - 1]);
            return result;
        }

        private static void PaintTrail(Mesh source, Transform terrain, List<Vector3> route)
        {
            TextureImporter importer = AssetImporter.GetAtPath(TerrainTexturePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.isReadable = true;
            importer.SaveAndReimport();
            Texture2D original = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTexturePath);
            int size = original.width;
            Color32[] pixels = original.GetPixels32();

            // UVs are a flat projection, so two far-apart vertices give the world-to-texture mapping.
            Vector3[] vertices = source.vertices;
            Vector2[] uvs = source.uv;
            int minX = 0, maxX = 0, minZ = 0, maxZ = 0;
            Vector3[] world = vertices.Select(v => terrain.TransformPoint(v)).ToArray();
            for (int i = 1; i < world.Length; i++)
            {
                if (world[i].x < world[minX].x) minX = i;
                if (world[i].x > world[maxX].x) maxX = i;
                if (world[i].z < world[minZ].z) minZ = i;
                if (world[i].z > world[maxZ].z) maxZ = i;
            }

            float uPerX = (uvs[maxX].x - uvs[minX].x) / (world[maxX].x - world[minX].x);
            float vPerZ = (uvs[maxZ].y - uvs[minZ].y) / (world[maxZ].z - world[minZ].z);
            float pixelsPerMetre = Mathf.Abs(uPerX) * size;
            int radius = Mathf.CeilToInt(2.3f * pixelsPerMetre);
            Color dirt = new Color(0.50f, 0.37f, 0.23f);
            foreach (Vector3 point in route)
            {
                float u = uvs[minX].x + (point.x - world[minX].x) * uPerX;
                float v = uvs[minZ].y + (point.z - world[minZ].z) * vPerZ;
                int cx = Mathf.RoundToInt(u * size), cy = Mathf.RoundToInt(v * size);
                for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(size - 1, cy + radius); y++)
                {
                    for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(size - 1, cx + radius); x++)
                    {
                        float distance = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radius;
                        float strength = 1f - Mathf.SmoothStep(0.55f, 1f, distance);
                        if (strength <= 0f)
                        {
                            continue;
                        }

                        float speckle = 0.92f + 0.16f * Mathf.PerlinNoise(x * 0.09f, y * 0.09f);
                        Color current = pixels[y * size + x];
                        Color target = dirt * speckle;
                        target.a = 1f;
                        pixels[y * size + x] = Color.Lerp(current, target, Mathf.Max(strength, 0f) * 0.85f);
                    }
                }
            }

            Texture2D painted = new Texture2D(size, size, TextureFormat.RGB24, false);
            painted.SetPixels32(pixels);
            painted.Apply();
            System.IO.File.WriteAllBytes(TerrainTrailTexturePath, painted.EncodeToPNG());
            Object.DestroyImmediate(painted);
            AssetDatabase.ImportAsset(TerrainTrailTexturePath);
            TextureImporter trailImporter = AssetImporter.GetAtPath(TerrainTrailTexturePath) as TextureImporter;
            if (trailImporter != null)
            {
                trailImporter.wrapMode = TextureWrapMode.Clamp;
                trailImporter.maxTextureSize = 2048;
                trailImporter.SaveAndReimport();
            }

            importer.isReadable = false;
            importer.SaveAndReimport();
            Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Terrain.mat");
            terrainMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTrailTexturePath));
            EditorUtility.SetDirty(terrainMaterial);
        }

        private static GameObject CreateBeaconPart(string name, Transform parent, Vector3 position, LastTide.ItemType item, bool needsTool,
            LastTide.ItemType tool, string lockedPrompt, string takePrompt)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.position = position;
            part.AddComponent<LastTide.BeaconPart>().Configure(item, needsTool, tool, lockedPrompt, takePrompt);
            return part;
        }

        private static MeshRenderer AddQuestPart(GameObject model, string part, Transform parent, Material material)
        {
            MeshRenderer renderer = AddPart(model, part, parent, material);
            return renderer;
        }

        private static void AddQuestCollider(MeshRenderer renderer)
        {
            MeshCollider collider = renderer.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        }

        private static void RemoveItemsNear((Vector3 point, float radius)[] areas)
        {
            GameObject items = GameObject.Find("Items");
            if (items == null)
            {
                return;
            }

            foreach (Transform item in items.transform.Cast<Transform>().ToArray())
            {
                if (areas.Any(a => Flat(item.position, a.point) < a.radius))
                {
                    Object.DestroyImmediate(item.gameObject);
                }
            }
        }

        private static void AddSceneToBuild()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(s => s.path != ScenePath && !s.path.EndsWith("SampleScene.unity"))
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
