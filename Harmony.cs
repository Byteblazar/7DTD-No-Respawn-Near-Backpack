using HarmonyLib;
using System.Reflection;

namespace NoRespawnNearBackpack
{
    public class NoRespawnNearBackpack : IModApi
    {
        public void InitMod(Mod mod)
        {
            new Harmony(GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(XUiC_SpawnSelectionWindow))]
    internal class XUiC_SpawnSelectionWindow_HarmonyPatches
    {

        [HarmonyPostfix]
        [HarmonyPatch(nameof(XUiC_SpawnSelectionWindow.RefreshButtons))]
        public static void Postfix_RefreshButtons()
        {
            XUiC_SpawnSelectionWindow window = XUiC_SpawnSelectionWindow.GetWindow(LocalPlayerUI.primaryUI);
            if (window.bEnteringGame || !window.showButtons) return;

            EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
            bool bedrollSet = !primaryPlayer.GetSpawnPoint().IsUndef();

            void UpdateButton(XUiC_SimpleButton button, ref SpawnMethod method, ref SpawnPosition pos)
            {
                if (method == SpawnMethod.NearBackpack || method == SpawnMethod.OnBackpack)
                {
                    if (bedrollSet)
                    {
                        button.ViewComponent.UiTransform.gameObject.SetActive(false);
                    }
                    else
                    {
                        button.Text = Localization.Get("lblRespawn");
                        method = SpawnMethod.Invalid;
                        pos = SpawnPosition.Undef;
                        EntityPlayerLocal_HarmonyPatches._teleport = true;
                    }
                }
            }

            UpdateButton(window.btnOption1, ref window.option1Method, ref window.option1Position);
            UpdateButton(window.btnOption2, ref window.option2Method, ref window.option2Position);
            UpdateButton(window.btnOption3, ref window.option3Method, ref window.option3Position);
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal))]
    internal class EntityPlayerLocal_HarmonyPatches
    {
        public static bool _teleport = false;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(EntityPlayerLocal.AfterPlayerRespawn))]
        public static void Postfix_AfterPlayerRespawn()
        {
            if (!_teleport) return;
            _teleport = false;

            EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
            GameManager gameManager = primaryPlayer.MoveController.gameManager;
            World world = GameManager.Instance.World;
            SpawnPosition spos = gameManager.GetSpawnPointList().GetRandomSpawnPosition(world);
            primaryPlayer.TeleportToPosition(spos.ToBlockPos().ToVector3());
        }
    }
}