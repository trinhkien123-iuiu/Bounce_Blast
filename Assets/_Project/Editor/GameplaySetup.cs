#if BB_LEGACY_SETUP
using System.Collections.Generic;
using System.IO;
using BounceBlast.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.EditorTools
{
    /// <summary>Menu Tools/Bounce Blast/Setup Gameplay Scene</summary>
    public static class GameplaySetup
    {
        const string ConfigDir = "Assets/_Project/Configs";
        const string MatDir = "Assets/_Ripped/Material";
        const string SpriteDir = "Assets/_Ripped/UI/Sprite";
        const string IconDir = "Assets/_Ripped/UI/Icons";
        const string AudioDir = "Assets/_Ripped/AudioClip";

        const string ShaderCubeIsolated = "e7fad1f9215b16a44a29bd6d1ee85412";
        const string ShaderCubeOutline = "bee1af360cf57ea4aadd60c745f066dc";
        const string ShaderCube = "a5be2a8d227cdb248aa675723f78a2f9";
        const string ShaderDirectional = "b01bf42877a40e1408b9348f0478a9bb";

        static readonly (string code, string name, float min, float max)[] Colors =
        {
            ("g", "Green", -0.075f, 0.075f), ("r", "Red", -0.15f, 0f), ("b", "Blue", -0.05f, 0.1f),
            ("y", "Yellow", -0.05f, 0.05f), ("o", "Orange", -0.075f, 0.075f), ("p", "Pink", -0.1f, 0.1f),
            ("w", "White", -0.04f, 0.04f), ("bl", "Black", -0.075f, 0.1f), ("br", "Brown", -0.05f, 0.1f),
            ("pr", "Purple", -0.1f, 0.025f), ("te", "Teal", -0.05f, 0.05f), ("pe", "Peach", -0.05f, 0.05f),
            ("si", "Silver", -0.2f, -0.1f), ("gr", "Dark Green", -0.15f, 0f), ("gra", "Gray", -0.1f, 0.1f),
            ("c", "Cyan", -0.075f, 0.075f),
        };

        /// <summary>Xử lý dựng sẵn scene Gameplay</summary>
        [MenuItem("Tools/Bounce Blast/Setup Gameplay Scene")]
        public static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(ConfigDir)) AssetDatabase.CreateFolder("Assets/_Project", "Configs");

            ColorLibrary colors = LoadOrCreate<ColorLibrary>("ColorLibrary");
            FillColors(colors);
            GameConfig config = LoadOrCreate<GameConfig>("GameConfig");
            FillConfig(config);
            UiSkin skin = LoadOrCreate<UiSkin>("UiSkin");
            FillSkin(skin);
            AssetDatabase.SaveAssets();

            SetupScene(config, colors, skin);
            Debug.Log("[Setup] Xong: Configs + GameManager trong scene Gameplay.");
        }

        static T LoadOrCreate<T>(string name) where T : ScriptableObject
        {
            string path = $"{ConfigDir}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void FillColors(ColorLibrary lib)
        {
            lib.entries.Clear();
            foreach (var c in Colors)
            {
                var e = new ColorLibrary.Entry { code = c.code, name = c.name, shadeOffset = new Vector2(c.min, c.max) };
                e.cubeMaterial = FindColorMat(c.name, ShaderCubeIsolated, ShaderDirectional, ShaderCube);
                e.ammoMaterial = FindColorMat(c.name, ShaderCube, ShaderCubeOutline, ShaderDirectional);
                e.ballMaterial = FindColorMat(c.name, ShaderCube, ShaderCubeOutline, ShaderDirectional);
                Material any = e.cubeMaterial ?? e.ammoMaterial ?? e.ballMaterial;
                e.color = any != null && any.HasProperty("_BaseColor") ? any.GetColor("_BaseColor") : Color.white;
                lib.entries.Add(e);
            }
            lib.fallbackCube = Mat("Grey");
            lib.fallbackAmmo = Mat("Grey");
            lib.fallbackBall = Mat("Grey");
            EditorUtility.SetDirty(lib);
        }

        /// <summary>Tìm "Blue.mat", "Blue_0.mat"</summary>
        static Material FindColorMat(string colorName, params string[] shaderGuids)
        {
            var candidates = new List<Material>();
            foreach (string guid in AssetDatabase.FindAssets($"\"{colorName}\" t:Material", new[] { MatDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (file != colorName && !(file.StartsWith(colorName + "_") && int.TryParse(file.Substring(colorName.Length + 1), out _))) continue;
                candidates.Add(AssetDatabase.LoadAssetAtPath<Material>(path));
            }
            foreach (string sg in shaderGuids)
                foreach (Material m in candidates)
                    if (m != null && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m.shader)) == sg) return m;
            return candidates.Count > 0 ? candidates[0] : null;
        }

        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");

        static void FillConfig(GameConfig c)
        {
            c.cubePrefab = Prefab("Assets/Resources/prefabs/Cube.prefab");
            c.combinedCubePrefab = Prefab("Assets/Resources/prefabs/CombinedCubicCubes.prefab");
            c.cratePrefab = Prefab("Assets/Resources/prefabs/CombinedCubicCrate.prefab");
            c.bombPrefab = Prefab("Assets/_Project/Prefabs/Gameplay/Board/CombinedCubicBomb.prefab");
            c.curtainModularPrefab = Prefab("Assets/_Project/Prefabs/Gameplay/Board/ModularPrimitiveCurtain.prefab");
            c.curtainChainPrefab = Prefab("Assets/_Project/Prefabs/Gameplay/Board/CombinedCubicCurtain.prefab");
            c.ammoPrefab = Prefab("Assets/Resources/prefabs/Ammunation.prefab");
            c.tunnelPrefab = Prefab("Assets/Resources/prefabs/BlockerAmmoTunnel.prefab");
            c.ballPrefab = Prefab("Assets/Resources/prefabs/Ball.prefab");
            c.cubeBreakVfx = Prefab("Assets/Resources/prefabs/Block Break Particle Variant.prefab");
            c.crateBreakVfx = Prefab("Assets/_Ripped/GameObject/MetalExplosionBig.prefab") ?? c.cubeBreakVfx;
            c.ammoLaunchVfx = Prefab("Assets/_Ripped/GameObject/StarExplosion.prefab");
            c.confettiVfx = Prefab("Assets/_Ripped/GameObject/RespawnExplosion.prefab") ?? c.ammoLaunchVfx;

            c.worldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Ripped/Font/TMP/Fredoka-Bold SDF 1.asset")
                          ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Ripped/Font/TMP/Fredoka-Bold SDF.asset");
            c.wallMaterial =Mat("Grey_2") ?? Mat("Grey");
            c.crateMaterial = Mat("Crate") ?? Mat("Crate 1");
            c.ammoCellMaterial = Mat("Tunnel Super Hard") ?? Mat("Tunnel");
            c.iceMaterial = Mat("Ice");
            c.qmarkMaterial = Mat("QMark Mat Cube");
            c.trailMaterial = TrailMaterial();

            c.normalTheme = new GameConfig.Theme
            {
                useColors = true,
                backgroundTop = new Color(0.27f, 0.29f, 0.66f),
                backgroundBottom = new Color(0.17f, 0.18f, 0.42f),
                conveyorSurface = Mat("Surface_0"),
                conveyorSide = Mat("Side_0"),
                plane = Mat("t4-surface"),
                ammoCell = Mat("M-Plane"),
                ammoWall = Mat("BG"),
                groundColor = new Color(0.282f, 0.314f, 0.682f), arrowColor = new Color(0.227f, 0.243f, 0.373f) ,
                pinsColor = new Color(0.282f, 0.322f, 0.706f), backgroundMask = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Ripped/Sprite/Mask.asset"), trayWall = Mat("t4-wall"), trayPanel = Mat("t4-bg"), trayHole = Mat("Hole-1"),
            };
            c.hardTheme = new GameConfig.Theme
            {
                useColors = true,
                backgroundTop = new Color(0.62f, 0.20f, 0.17f),
                backgroundBottom = new Color(0.35f, 0.11f, 0.09f),
                conveyorSurface = Mat("Surface Hard"),
                conveyorSide = Mat("Side Hard"),
                plane = Mat("t6-surface"),
                ammoCell = Mat("M-Plane 2"),
                ammoWall = Mat("Hole-2"),
                groundColor = new Color(0.624f, 0.251f, 0.227f), arrowColor = new Color(0.553f, 0.153f, 0.180f),
                pinsColor = new Color(0.514f, 0.196f, 0.157f), backgroundMask = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Ripped/Sprite/MaskHard.asset"), trayWall = Mat("t6-wall"), trayPanel = Mat("t6-bg 2"), trayHole = Mat("Hole-2"),
            };
            c.superHardTheme = new GameConfig.Theme
            {
                useColors = true,
                backgroundTop = new Color(0.20f, 0.20f, 0.21f),
                backgroundBottom = new Color(0.10f, 0.10f, 0.11f),
                conveyorSurface = Mat("Surface Super Hard"),
                conveyorSide = Mat("Side Super Hard"),
                plane = Mat("t3-surface"),
                ammoCell = Mat("Hole-3"),
                ammoWall = Mat("M-Plane 3"),
                groundColor = new Color(0.29f, 0.29f, 0.29f), arrowColor = new Color(0.196f, 0.192f, 0.204f),
                pinsColor = new Color(0.165f, 0.165f, 0.165f), backgroundMask = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Ripped/Sprite/MaskSuperHard.asset"), trayWall = Mat("t3-wall"), trayPanel = Mat("t3-bg2"), trayHole = Mat("Hole-3"),
            };

            c.sounds = new[]
            {
                Snd(Sfx.Button, 0.7f, 0.05f, "button1"),
                Snd(Sfx.Error, 0.7f, 0.15f, "SFX Error"),
                Snd(Sfx.TapAmmo, 0.8f, 0.05f, "arabaya tıkaldığımlzda"),
                Snd(Sfx.BoxLaunch, 0.6f, 0.05f, "etfx_shoot_bubble2"),
                Snd(Sfx.BallHit, 0.30f, 0.045f, "etfx_pop_balloon"),
                Snd(Sfx.BigBlockBreak, 0.8f, 0.05f, "SFX 251025 Block cut sound"),
                Snd(Sfx.CrateBreak, 0.9f, 0.05f, "SFX 210725 crate breaked"),
                Snd(Sfx.IceCrack, 0.6f, 0.05f, "SFX 220725 hit_Pendant"),
                Snd(Sfx.IceBreak, 0.8f, 0.05f, "etfx_explosion_snow"),
                Snd(Sfx.QmarkReveal, 0.7f, 0.05f, "bubble-pop-293342 (1)"),
                Snd(Sfx.TunnelOut, 0.7f, 0.05f, "SFX 241025_tsidilin__bubble-pop-out"),
                Snd(Sfx.Win, 0.9f, 0.1f, "SFX 290725 win 1"),
                Snd(Sfx.Lose, 0.9f, 0.1f, "SFX 290725 fail 1"),
                Snd(Sfx.Confetti, 0.8f, 0.1f, "SFX 250725 confetti"),
                Snd(Sfx.Hammer, 0.9f, 0.1f, "hammer_hit"),
                Snd(Sfx.Magic, 0.8f, 0.1f, "SFX 120126 hit magic"),
            };
            foreach (SoundEntry s in c.sounds) if (s.id == Sfx.BallHit) s.pitch = new Vector2(0.9f, 1.15f);
            c.musicNormal = Clip("geoffharvey-solve-the-riddle-140001");
            c.musicHard = Clip("denis-pavlov-music-marimba-game-music-playful-tropical-jungle-puzzle-399759");
            c.musicSuperHard = Clip("denis-pavlov-music-game-music-puzzle-strategy-arcade-technology-301226");
            EditorUtility.SetDirty(c);
        }

        static Material TrailMaterial()
        {
            string path = $"{ConfigDir}/BallTrail.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameObject Prefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.ogg");

        static SoundEntry Snd(Sfx id, float volume, float interval, string clip) =>
            new SoundEntry { id = id, volume = volume, minInterval = interval, clips = new[] { Clip(clip) } };

        static void FillSkin(UiSkin s)
        {
            const string ui = "Assets/_Project/UI/Prefabs/";
            s.hudPrefab = Prefab(ui + "GameCanvas.prefab");
            s.successPrefab = Prefab(ui + "SuccessCanvas.prefab");
            s.failPrefab = Prefab(ui + "FailCanvas.prefab");
            s.settingsPrefab = Prefab(ui + "SettingsCanvas.prefab");
            s.outOfCapacityPrefab = Prefab(ui + "OutOfCapacityPopup.prefab");
            s.capacityBarPrefab = Prefab(ui + "CapacityBar.prefab");
            s.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Ripped/Font/TMP/Fredoka-Bold SDF.asset");
            s.fontOutlineMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Ripped/Font/TMP/Fredoka-Bold Atlas Material - Buttons.mat");
            s.capacityBg = Spr("CapacityBg");
            s.capacityFill = Spr("CapacityWhite") ?? Spr("CapacityGreen");
            s.overlayCanvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/UI/Prefabs/OverlayCanvas.prefab");
            s.popupNormal = Spr("Normal Pop-up (2)");
            s.popupHard = Spr("Hard Pop-up (1)_mir");
            s.popupSuperHard = Spr("Super Hard Pop-up (1)_mir");
            s.greenButton = Spr("Play Button");
            s.redButton = Spr("Give Up Button");
            s.addCapacityButton = Spr("AddCapacityButton");
            s.closeButton = Spr("Close");
            s.blastWord = Spr("blast");
            s.coin = Spr("Coin");
            s.brokenHeart = Spr("BrokenHeart Icon");
            s.sadFace = Spr("Sad_Face_Emoji");
            s.soundOn = Spr("Volume on");
            s.soundOff = Spr("Volume off");
            s.musicOn = Spr("Music on");
            s.musicOff = Spr("Music off");
            EditorUtility.SetDirty(s);
        }

        static Sprite Spr(string name)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{name}.asset");
            if (s == null) Debug.LogWarning($"[Setup] Không thấy sprite {name}");
            return s;
        }

        static Sprite Icon(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{IconDir}/{name}.asset") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{IconDir}/{name}.png");

        static void SetupScene(GameConfig config, ColorLibrary colors, UiSkin skin)
        {
            var scene = EditorSceneManager.GetActiveScene();
            GameObject board = GameObject.Find("Board - Default");
            GameObject host = GameObject.Find("GameManager") ?? new GameObject("GameManager");

            foreach (MonoBehaviour mb in host.GetComponents<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == "GameplayManager") Object.DestroyImmediate(mb);
            if (board != null)
            {
                foreach (MonoBehaviour mb in board.GetComponents<MonoBehaviour>())
                    if (mb != null && mb.GetType().Name == "BounceBlastLevelSpawner") Object.DestroyImmediate(mb);
                Transform spawned = board.transform.Find("_SpawnedLevelObjects");
                if (spawned != null) Object.DestroyImmediate(spawned.gameObject);
            }

            host.transform.position = Vector3.zero;
            GameManager gm = host.GetComponent<GameManager>();
            if (gm == null) gm = host.AddComponent<GameManager>();
            gm.config = config;
            gm.colors = colors;
            gm.skin = skin;
            gm.boardRoot = board != null ? board.transform : null;
            gm.gameCamera = Camera.main;
            EditorUtility.SetDirty(gm);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
