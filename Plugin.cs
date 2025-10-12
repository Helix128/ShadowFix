using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Managers;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Common;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace ShadowFix;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ShadowFixPlugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public static bool DisableBlobShadow = true;
    public static bool TwoSidedShadows = true;
    public static bool EnablePlayerShadow = false;
    public static bool EnableNPCShadows = false;
    public static float ShadowDarkness = 0.8f;
    public override void Load()
    {   
        Log = base.Log;
        Log.LogInfo($"{MyPluginInfo.PLUGIN_GUID} loaded successfully!");
        ClassInjector.RegisterTypeInIl2Cpp<ShadowFixComponent>();

        ConfigDefinition disableBlobShadowDef = new ConfigDefinition("General", "DisableBlobShadow");
        ConfigEntry<bool> disableBlobShadowConfig = Config.Bind(disableBlobShadowDef, true, new ConfigDescription("Disable the player circle (blob) shadow.", null));
        DisableBlobShadow = disableBlobShadowConfig.Value;

        ConfigDefinition enablePlayerShadowDef = new ConfigDefinition("General", "EnablePlayerShadow");
        ConfigEntry<bool> enablePlayerShadowConfig = Config.Bind(enablePlayerShadowDef, true, new ConfigDescription("Enable realtime shadows for the player.", null));
        EnablePlayerShadow = enablePlayerShadowConfig.Value;

        ConfigDefinition twoSidedShadowsDef = new ConfigDefinition("General", "TwoSidedShadows");
        ConfigEntry<bool> twoSidedShadowsConfig = Config.Bind(twoSidedShadowsDef, true, new ConfigDescription("Enable two-sided shadows for mesh renderers. This setting fixes a few visual artifacts at the cost of performance.", null));
        TwoSidedShadows = twoSidedShadowsConfig.Value;

        ConfigDefinition enableNPCShadowsDef = new ConfigDefinition("General", "EnableNPCShadows");
        ConfigEntry<bool> enableNPCShadowsConfig = Config.Bind(enableNPCShadowsDef, false, new ConfigDescription("Enable shadows for NPCs. This setting may impact performance. Has some visual artifacts when killing enemies.", null));
        EnableNPCShadows = enableNPCShadowsConfig.Value;

        ConfigDefinition shadowDarknessDef = new ConfigDefinition("General", "ShadowDarkness");
        ConfigEntry<float> shadowDarknessConfig = Config.Bind(shadowDarknessDef, 0.8f, new ConfigDescription("Darkness of the shadows. Set to 1 for maximum contrast.", new AcceptableValueRange<float>(0f, 1f)));
        ShadowDarkness = shadowDarknessConfig.Value;

        GameObject shadowFixGO = new GameObject("ShadowFixGO");
        shadowFixGO.AddComponent<ShadowFixComponent>();
        Object.DontDestroyOnLoad(shadowFixGO);
    }

    public override bool Unload()
    {
        Log.LogInfo($"Unloading {MyPluginInfo.PLUGIN_GUID}!");
        return base.Unload();
    }

    public class ShadowFixComponent : MonoBehaviour
    {   
        public static ShadowFixComponent Instance { get; private set; }
        EnemyManager enemyManager;
        int lastScene = -1;
        private void Start()
        {
            if(Instance != null && Instance != this)
            {
                Log.LogInfo("Detected duplicate ShadowFixComponent. Destroying the newest one.");
                Destroy(this);
                return;
            }
            Instance = this;
            Log.LogInfo("ShadowFixComponent started.");
        }

        void Update()
        {
            try
            {
                int currentScene = SceneManager.GetActiveScene().buildIndex;
                if (currentScene != lastScene)
                {   
                    Log.LogInfo($"Detected scene change to {currentScene} ({SceneManager.GetSceneAt(0).name}).");
                    FixShadows();
                    lastScene = currentScene;
                }
                if (EnableNPCShadows && Time.frameCount % 2 >= 1)
                {
                    FixEnemyShadows();
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error in Update: {ex.Message}");
            }
        }
        public List<uint> fixedEnemies = new List<uint>();
        void FixEnemyShadows()
        {
            try
            {
                if (enemyManager == null) return;

                foreach (Enemy enemy in enemyManager.enemies.Values)
                {
                    if (enemy == null) continue;
                    if (fixedEnemies.Contains(enemy.id)) continue;
                    
                    try
                    {
                        var renderer = enemy.renderer;
                        if (renderer == null) continue;
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        fixedEnemies.Add(enemy.id);
                    }
                    catch (System.Exception ex)
                    {
                        Log.LogError($"Error fixing shadow for enemy {enemy.id}: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error in FixEnemyShadows: {ex.Message}");
            }
        }

        async void FadeShadows(Light light, float targetStrength)
        {
            try
            {
                int steps = 32;
                float duration = 0.15f;
                float initialStrength = 0;
                for (int i = 1; i <= steps; i++)
                {
                    if (light == null || light.Equals(null)) return;
                    light.shadowStrength = Mathf.Lerp(initialStrength, targetStrength, (float)i / steps);
                    await Task.Delay((int)(duration * 1000 / steps));
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error in FadeShadows: {ex.Message}");
            }
        }

        bool isFixing = false;
        async void FixShadows()
        {
            if (isFixing) { Log.LogWarning("ShadowFix is already in progress."); return; }
    
            try
            {
                int currentScene = SceneManager.GetActiveScene().buildIndex;
                if (currentScene <= 1 || currentScene == 3)
                {
                    Log.LogInfo("Non-gameplay scene detected, skipping shadow fix.");
                    return;
                }
                Log.LogInfo("Starting ShadowFix...");
                isFixing = true;
                await Task.Delay(2000);
                
                if (SceneManager.GetActiveScene().buildIndex != currentScene)
                {
                    Log.LogWarning("Scene changed during delay, aborting shadow fix.");
                    return;
                }
                
                fixedEnemies.Clear();
                var lights = FindObjectsOfType<Light>();
                foreach (var light in lights)
                {
                    if (light.type != LightType.Directional) continue;
                    if (light.shadows == LightShadows.None)
                    {
                        light.shadows = LightShadows.Soft;
                        FadeShadows(light, ShadowDarkness);
                        light.shadowConstantBias *= 0.8f;
                        light.shadowBias *= 0.8f;
                        light.shadowNormalBias *= 0.8f;
                        Log.LogInfo($"Enabled shadows for '{light.name}'.");
                    }
                }

                Log.LogInfo($"Must set two-sided shadows? {TwoSidedShadows}");
                if (TwoSidedShadows)
                {
                    Log.LogInfo("Preparing to fix mesh shadow mode...");
                    var meshes = FindObjectsOfType<MeshRenderer>();
                    foreach (var mesh in meshes)
                    {
                        Log.LogInfo($"Setting shadow casting mode for '{mesh.name}'.");
                        mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    }
                }   

        
                Log.LogInfo($"Enable player shadow? {EnablePlayerShadow}");
                var player = FindObjectOfType<PlayerRenderer>();
                if (player != null && !player.Equals(null))
                {
                    var playerGo = player.rendererObject;
                    if (playerGo == null || playerGo.Equals(null))
                    {
                        Log.LogWarning("Player GameObject not found.");
                        return;
                    }
                    var playerRenderers = playerGo.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in playerRenderers)
                    {
                        renderer.shadowCastingMode = EnablePlayerShadow? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                        Log.LogInfo($"{(EnablePlayerShadow ? "Enabled" : "Disabled")} shadows for player mesh '{renderer.name}'.");
                    }
                }
            
                Log.LogInfo($"Must destroy blob shadow? {DisableBlobShadow}");
                var blobShadowProjs = FindObjectsOfType<Projector>();
                if (blobShadowProjs != null && DisableBlobShadow)
                {
                    foreach (var blobShadowProj in blobShadowProjs)
                    {
                        Destroy(blobShadowProj);
                        Log.LogInfo("Destroyed BlobShadowProjector.");
                    }
                }
                Log.LogInfo($"Enable NPC shadows? {EnableNPCShadows}");
                if (EnableNPCShadows)
                {
                    var enemyManagers = FindObjectsByType<EnemyManager>(FindObjectsSortMode.None);
                    if (enemyManagers.Length == 0)
                    {
                        Log.LogWarning("EnemyManager not found. (Probably a menu scene)");
                    }
                    else
                    {
                        if (enemyManager != enemyManagers[0])
                        {
                            fixedEnemies.Clear();
                        }
                        enemyManager = enemyManagers[0];
                        Log.LogInfo($"Found EnemyManager.");
                    }
                }


                Log.LogInfo("ShadowFix done.");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error in FixShadows: {ex.Message}");
            }
            finally
            {
                isFixing = false;
            }
        }

    }
}
