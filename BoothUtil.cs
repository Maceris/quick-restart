using R2API.Utils;
using RoR2;
using RoR2.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace QuickRestart
{
    class BoothUtil
    {
        static public void ResetGame(PauseScreenController pauseScreen, bool AskConfirmation, Booth.QuickRestart parent, bool startNewGame)
        {
            if (AskConfirmation)
            {
                if (SimpleDialogBox.instancesList.Count > 0)
                {
                    // Don't create more than one window.
                    return;
                }
                SimpleDialogBox confirmation = SimpleDialogBox.Create();
                confirmation.headerToken = new SimpleDialogBox.TokenParamsPair("Are you sure?");
                string description = "Are you sure you want to reset this run?";
                if (pauseScreen is null)
                {
                    description += " Use info screen button (usually tab/select) to move cursor.";
                }
                confirmation.descriptionToken = new SimpleDialogBox.TokenParamsPair(description);
                confirmation.AddActionButton(() => {
                    ActuallyResetGame(pauseScreen, parent, startNewGame);
                }, "Yes");
                confirmation.AddCancelButton("Cancel");

            }
            else
            {
                // Avoid duplicate code but allow the dialog shenanigans
                ActuallyResetGame(pauseScreen, parent, startNewGame);
            }
        }

        static public bool IsMultiplayerHost()
        {
            return RoR2.NetworkSession.instance && NetworkServer.active;
        }

        static private void ActuallyResetGame(PauseScreenController pauseScreen, Booth.QuickRestart parent, bool startNewGame)
        {
            // Close the pause menu
            pauseScreen?.DestroyPauseScreen(true);

            if (!(Run.instance is null || Run.instance.gameObject is null))
            {
                UnityEngine.Object.Destroy(Run.instance.gameObject);//CCRunEnd
                UnityEngine.Object.Destroy(Run.instance);
            }
            if (startNewGame)
            {
                parent.StartCoroutine(StartNewGame());
            }
        }

        static private IEnumerator StartNewGame()
        {
            while (!PreGameController.instance)
            {
                yield return new WaitForSeconds(0.1f);
            }
            PreGameController.instance.InvokeMethod("StartRun");//CCPregameStartRun
        }
    }
}
