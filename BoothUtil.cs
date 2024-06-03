using R2API.Utils;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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
                String description = "Are you sure you want to reset this run?";
                if (pauseScreen is null)
                {
                    description += " Use info screen button (usually tab/select) to move cursor.";
                }
                confirmation.descriptionToken = new SimpleDialogBox.TokenParamsPair(description);
                confirmation.AddActionButton(() => {
                    ActuallyResetGame(pauseScreen, parent, startNewGame);
                }, "Yes");
                confirmation.AddCancelButton("Cancel");

            } else
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
            if (!(pauseScreen is null))
            {
                // Close the pause menu
                pauseScreen.InvokeMethod("OnDisable");
                UnityEngine.Object.Destroy(pauseScreen.gameObject);
            }

            if (IsMultiplayerHost())
            {
                RoR2.NetworkSession.instance.EndRun();
                if (startNewGame)
                {
                    parent.StartCoroutine(StartNewGameMultiplayer());
                }
            }
            else
            {
                // This is probably deprecated after the Anniversary update
                if (!(Run.instance is null || Run.instance.gameObject is null))
                {
                    UnityEngine.Object.Destroy(Run.instance.gameObject);//CCRunEnd
                }
                if (startNewGame)
                {
                    parent.StartCoroutine(StartNewGameSingleplayer());
                }
            }
        }

        static private IEnumerator StartNewGameSingleplayer()
        {
            while (!PreGameController.instance)
            {
                yield return new WaitForSeconds(0.1f);
            }
            PreGameController.instance.InvokeMethod("StartRun");//CCPregameStartRun
        }

        static private IEnumerator StartNewGameMultiplayer()
        {
            while (!PreGameController.instance)
            {
   
                yield return new WaitForSeconds(0.1f);
            }
            PreGameController.instance.StartLaunch();
        }
    }
}
