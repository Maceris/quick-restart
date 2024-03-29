﻿using R2API.Utils;
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
        static public GameObject CreateButton(Transform parent, Vector2 size, Sprite sprite)
        {
            // Set up the colors used for the button
            ColorBlock colorBlock = new ColorBlock
            {
                disabledColor = new Color(0.255f, 0.201f, 0.201f, 0.714f),
                highlightedColor = new Color(0.988f, 1.000f, 0.693f, 0.733f),
                normalColor = new Color(0.327f, 0.403f, 0.472f, 1.000f),
                pressedColor = new Color(0.740f, 0.755f, 0.445f, 0.984f),
                colorMultiplier = 1
            };

            // The base game object
            GameObject button = new GameObject
            {
                name = "Button"
            };
            button.transform.parent = parent;
            button.AddComponent<RoR2.UI.MPEventSystemLocator>();

            // The graphical part
            Image buttonImage = button.AddComponent<Image>();
            buttonImage.color = new Color(1, 1, 1, 1);
            buttonImage.type = Image.Type.Sliced;
            buttonImage.sprite = sprite;
            buttonImage.raycastTarget = true;

            // The interactable part
            RoR2.UI.HGButton actualButton = button.AddComponent<RoR2.UI.HGButton>();
            actualButton.showImageOnHover = true;
            actualButton.targetGraphic = buttonImage;
            actualButton.colors = colorBlock;
            actualButton.disableGamepadClick = true;

            // Used to set the size of the button
            RectTransform buttonTransform = button.GetComponent<RectTransform>();
            buttonTransform.anchorMin = new Vector2(0f, 1f);
            buttonTransform.anchorMax = new Vector2(0f, 1f);
            buttonTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            buttonTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            return button;
        }

        static public void CreateText(List<TMPro.TextMeshProUGUI> texts, GameObject parent, Color colour, float size, float textOffset, Vector2 offsetMin, Vector2 offsetMax, String contents)
        {
            // The base game object for the text
            GameObject text = new GameObject
            {
                name = "Text"
            };
            text.transform.parent = parent.transform;

            // Actually create the text
            TMPro.TextMeshProUGUI textMesh = text.AddComponent<RoR2.UI.HGTextMeshProUGUI>();
            textMesh.color = colour;
            textMesh.fontSize = size;
            textMesh.alignment = TMPro.TextAlignmentOptions.Center;
            textMesh.text = contents;
            textMesh.raycastTarget = false;
            textMesh.lineSpacing = -25;

            // Set up the size of the text
            RectTransform textTransform = text.GetComponent<RectTransform>();
            textTransform.pivot = new Vector2(0, 1);
            textTransform.anchorMin = new Vector2(0, 0);
            textTransform.anchorMax = new Vector2(1, 1);
            textTransform.offsetMin = new Vector2(offsetMin.x, offsetMin.y + textOffset);
            textTransform.offsetMax = new Vector2(offsetMax.x, offsetMax.y + textOffset);
            textTransform.localScale = new Vector3(1, 1, 1);
            texts.Add(textMesh);
        }

        static public Image SpawnImage(GameObject parent, Color color, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, Sprite sprite)
        {
            // The base game object for the image
            GameObject image = new GameObject
            {
                name = "Image"
            };
            image.transform.parent = parent.transform;

            // Set up the size of the image
            RectTransform imageTransform = image.AddComponent<RectTransform>();
            imageTransform.pivot = pivot;
            imageTransform.anchorMin = new Vector2(0, 0);
            imageTransform.anchorMax = new Vector2(1, 1);
            imageTransform.offsetMin = offsetMin;
            imageTransform.offsetMax = offsetMax;
            imageTransform.localScale = new Vector3(1, 1, 1);

            // Create the actual image
            Image actualImage = image.AddComponent<Image>();
            actualImage.color = color;
            actualImage.sprite = sprite;
            actualImage.type = Image.Type.Sliced;
            actualImage.raycastTarget = false;

            return actualImage;
        }

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
