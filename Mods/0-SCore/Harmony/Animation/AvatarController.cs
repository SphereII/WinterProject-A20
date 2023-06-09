using HarmonyLib;
using UnityEngine;

namespace Harmony.Animation
{
    public class AvatarControllerSetTrig
    {
        private const string AdvFeatureClass = "AdvancedTroubleshootingFeatures";
        private const string Feature = "AnimatorMapper";

        /// <summary>
        /// Patch to the allow set a RandomIndex integer, with a range of 0 to 10, to allow more flexibility in custom animators 
        /// </summary>
        [HarmonyPatch(typeof(AvatarController))]
        [HarmonyPatch("SetTrigger")]
        [HarmonyPatch(new[] { typeof(string) })]
        public class AvatarControllerSetTrigger
        {
            public static bool Prefix(global::AvatarController __instance, string _property)
            {
                if (Configuration.CheckFeatureStatus(AdvFeatureClass, Feature))
                    AdvLogging.DisplayLog(AdvFeatureClass, "Set Trigger(): " + _property);

                // Provides a random index value to the default animator.
                __instance.SetInt("RandomIndex", Random.Range(0, 10));
                __instance.SetInt(_property, Random.Range(0, 10));
                return true;
            }
        }

     
    }
}