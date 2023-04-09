using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using GlobalSnowEffect;
using UnityEngine;

/*
   public class PrefabInstance
   {
	   public PrefabInstance(int _id, string _filename, Vector3i _position, byte _rotation, Prefab _bad, int _standaloneBlockSize)
       {
       }
   }
*/
public class SphereII_WinterProject
{
    public class SphereIIWinterProjectInit : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Log.Out(" Loading Patch: " + GetType());

            // Reduce extra logging stuff
            Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(UnityEngine.LogType.Warning, StackTraceLogType.None);

            var harmony = new HarmonyLib.Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    
    [HarmonyPatch(typeof(GameManager))]
    [HarmonyPatch("PlayerSpawnedInWorld")]
    public class CloneCameraPlayer
    {
        public static void Postfix()
        {
            var entityPlayer = GameManager.Instance.World.GetPrimaryPlayer();
            if (entityPlayer == null) return;
            var settings = entityPlayer.playerCamera.gameObject.GetOrAddComponent<GlobalSnow>();
            settings.snowAmount = 0.70f;
            settings.cameraFrost = false;
            settings.groundCoverage = 0.45f;
            
        }
    }
    [HarmonyPatch(typeof(DynamicPrefabDecorator))]
    [HarmonyPatch("AddPrefab")]
    public class SphereII_DynamicPrefabDecorator
    {
        public static bool Prefix(DynamicPrefabDecorator __instance, PrefabInstance _pi)
        {

            if (_pi.prefab.size.y < 11)
                return false;

            // Prefabs with too great of an offset should be removed.
            // Example: Size y size 30 with an offset of -25 would only be 5 above terrain; not visible.
            if (_pi.prefab.size.y - Math.Abs(_pi.prefab.yOffset) < 11)
                return false;

            // Keep the trader above.
            if (_pi.prefab.FileNameNoExtension.Contains("trader_hugh"))
                return true;

            // Check if the current thread has a name. the GenerateWorlds for RWG has a named thread; the others do not.
            if (Thread.CurrentThread.Name != null)
                return true;

            // Sink the prefab into the ground
            // This also sinks the SleeperVolumes, so they work as expected in clear quests.
            _pi.boundingBoxPosition.y -= 8;
            return true;
        }

    }
    [HarmonyPatch(typeof(Prefab))]
    [HarmonyPatch("readBlockData")]
    public class SphereII_WinterProject_readBlockData
    {
        public static bool Postfix(bool __result, ref Prefab __instance, ref List<string> ___allowedZones)
        {
            if (!__result) return __result;
            if (__instance.FileNameNoExtension.Contains("trader_hugh")) return __result;
            __instance.bTraderArea = false;
            __instance.bExcludeDistantPOIMesh = true;
            __instance.bCopyAirBlocks = true;
            return __result;
        }

    }


    // Sinks the prefabs
    [HarmonyPatch(typeof(Prefab))]
    [HarmonyPatch("CopyIntoLocal")]
    public class SphereII_WinterProject_Prefab_Prefix
    {
        public static void Postfix(Prefab __instance, Vector3i _destinationPos, ChunkCluster _cluster, QuestTags _questTags)
        {

            if (__instance.Tags.Test_AllSet(POITags.Parse("SKIP_HARMONY_COPY_INTO_LOCAL")))
                return;

            if (!__instance.FileNameNoExtension.Contains("trader_hugh"))
                WinterProjectPrefab.SetSnowPrefab(__instance, _cluster, _destinationPos, _questTags);
        }

    }



    [HarmonyPatch(typeof(PrefabInstance))]
    [HarmonyPatch("CopyIntoChunk")]
    public class SphereII_WinterProject_PrefabInstance_CopyIntoChunk
    {
        public static void Postfix(PrefabInstance __instance, Chunk _chunk)
        {
            if (!__instance.prefab.FileNameNoExtension.Contains("trader_hugh"))
                WinterProjectPrefab.SetSnowChunk(_chunk, __instance.boundingBoxPosition, __instance.boundingBoxSize);
        }
    }


}
