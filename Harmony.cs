﻿/*
     No Respawn Near Backpack is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    No Respawn Near Backpack is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with No Respawn Near Backpack. If not, see <https://www.gnu.org/licenses/>. 
*/

using HarmonyLib;
using System.Reflection;

namespace NoRespawnNearBackpack
{
    public class NoRespawnNearBackpack : IModApi
    {
        public void InitMod(Mod mod)
        {
            new Harmony(typeof(NoRespawnNearBackpack).FullName).PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(XUiC_SpawnSelectionWindow))]
    internal class HarmonyPatches_XUiC_SpawnSelectionWindow
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(XUiC_SpawnSelectionWindow.RefreshButtons))]
        static void Postfix_RefreshButtons(XUiC_SpawnSelectionWindow __instance)
        {
            if (__instance.bEnteringGame || !__instance.showButtons) return;

            EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
            bool bedrollSet = !primaryPlayer.GetSpawnPoint().IsUndef();

            void UpdateButton(XUiC_SimpleButton button, ref SpawnMethod method, ref SpawnPosition pos)
            {
                if (method != SpawnMethod.NearBackpack && method != SpawnMethod.OnBackpack) return;

                if (bedrollSet)
                {
                    button.ViewComponent.UiTransform.gameObject.SetActive(false);
                }
                else
                {
                    button.Text = Localization.Get("lblRespawn");
                    method = SpawnMethod.Invalid;
                    pos = SpawnPosition.Undef;
                    HarmonyPatches_EntityPlayerLocal.Teleport = true;
                }
            }

            UpdateButton(__instance.btnOption1, ref __instance.option1Method, ref __instance.option1Position);
            UpdateButton(__instance.btnOption2, ref __instance.option2Method, ref __instance.option2Position);
            UpdateButton(__instance.btnOption3, ref __instance.option3Method, ref __instance.option3Position);
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal))]
    internal class HarmonyPatches_EntityPlayerLocal
    {
        private static bool _teleport;
        internal static bool Teleport
        {
            get
            {
                if (_teleport)
                {
                    _teleport = false;
                    return true;
                }
                return false;
            }
            set => _teleport = value;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(EntityPlayerLocal.AfterPlayerRespawn))]
        static void Postfix_AfterPlayerRespawn(EntityPlayerLocal __instance)
        {
            if (!Teleport) return;

            GameManager gameManager = GameManager.Instance;
            World world = gameManager.World;
            SpawnPosition spos = gameManager.GetSpawnPointList().GetRandomSpawnPosition(world);
            __instance.TeleportToPosition(spos.ToBlockPos().ToVector3());
        }
    }
}