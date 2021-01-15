using BepInEx;
using RoR2;
using R2API;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using R2API.Utils;
using System.Threading;

namespace Booth
{

    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.IkalaGaming.QuickRestart", "QuickRestart", "1.1.1")]
    [R2APISubmoduleDependency(nameof(ResourcesAPI))]
    public class QuickRestart : BaseUnityPlugin
    {
        static public GameObject CreateButton(GameObject parent, Vector2 size, Sprite sprite)
        {
            // Set up the colors used for the button
            ColorBlock colorBlock = new ColorBlock();
            colorBlock.disabledColor = new Color(0.255f, 0.201f, 0.201f, 0.714f);
            colorBlock.highlightedColor = new Color(0.988f, 1.000f, 0.693f, 0.733f);
            colorBlock.normalColor = new Color(0.327f, 0.403f, 0.472f, 1.000f);
            colorBlock.pressedColor = new Color(0.740f, 0.755f, 0.445f, 0.984f);
            colorBlock.colorMultiplier = 1;

            // The base game object
            GameObject button = new GameObject();
            button.name = "Button";
            button.transform.parent = parent.transform;
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
            buttonTransform.pivot = new Vector2(0, 0);
            buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
            buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
            buttonTransform.anchoredPosition = new Vector2(0.5f, 0.5f);
            buttonTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            buttonTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            buttonTransform.localScale = new Vector3(1, 1, 1);

            return button;
        }

        static public void CreateText(List<TMPro.TextMeshProUGUI> texts, GameObject parent, Color colour, float size, float textOffset, Vector2 offsetMin, Vector2 offsetMax, String contents)
        {
            // The base game object for the text
            GameObject text = new GameObject();
            text.name = "Text";
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

        static public Image SpawnImage(List<Image> images, GameObject parent, Color color, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, Sprite sprite)
        {
            // The base game object for the image
            GameObject image = new GameObject();
            image.name = "Image";
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
            images.Add(actualImage);

            return actualImage;
        }

        public void Awake()
        {

            // Make our assets available to load
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("QuickRestart.booth_assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider("@Booth", bundle);
                ResourcesAPI.AddProvider(provider);
            }

            // Set up textures for the UI button
            Texture2D buttonTexture = Resources.Load<Texture2D>("@Booth:Assets/Texture2D/Booth_texUICleanButton.png");
            Texture2D buttonHighlightTexture = Resources.Load<Texture2D>("@Booth:Assets/Texture2D/Booth_texUIHighlightHeader.png");
            Texture2D buttonBorderTexture = Resources.Load<Texture2D>("@Booth:Assets/Texture2D/Booth_texUIOutlineOnly.png");
            Texture2D buttonOutlineTexture = Resources.Load<Texture2D>("@Booth:Assets/Texture2D/Booth_texUIHighlightBoxOutlineThick.png");

            // Needed to convert the textures to sprites
            Rect buttonTextureDimensions = new Rect(0, 0, buttonTexture.width, buttonTexture.height);
            Rect buttonHighlightTextureDimensions = new Rect(0, 0, buttonHighlightTexture.width, buttonHighlightTexture.height);
            Rect buttonBorderTextureeDimensions = new Rect(0, 0, buttonBorderTexture.width, buttonBorderTexture.height);
            Rect buttonOutlineTextureeDimensions = new Rect(0, 0, buttonOutlineTexture.width, buttonOutlineTexture.height);

            // Used to draw the button on UI
            Sprite buttonSprite = Sprite.Create(buttonTexture, buttonTextureDimensions, new Vector2(0, 0));
            Sprite buttonHighlightSprite = Sprite.Create(buttonHighlightTexture, buttonHighlightTextureDimensions, new Vector2(0, 0));
            Sprite buttonBorderSprite = Sprite.Create(buttonBorderTexture, buttonBorderTextureeDimensions, new Vector2(0, 0));
            Sprite buttonOutlineSprite = Sprite.Create(buttonOutlineTexture, buttonOutlineTextureeDimensions, new Vector2(0, 0));

            //Add restart button to the pause screen
            On.RoR2.UI.PauseScreenController.Awake += (orig, self) => {

                //Vector2 buttonSize = new Vector2(320, 48);
                Vector2 buttonSize = new Vector2(320, 48);
                GameObject button = CreateButton(self.mainPanel.GetChild(0).gameObject, buttonSize, buttonSprite);

                // Add in the stylized highlight/border
                List<Image> images = new List<Image>();
                SpawnImage(images, button, new Color(1, 1, 1, 1), new Vector2(0.5f, 0.5f), new Vector2(-6, -6), new Vector2(6, 6), buttonHighlightSprite);
                images[images.Count - 1].gameObject.SetActive(false);

                // Add in the sharp white border line
                SpawnImage(new List<Image>(), button, new Color(1, 1, 1, 0.286f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(0, 0), buttonBorderSprite);

                // Add in the thicker surrounding outline for when you hover on the button
                Image highlightImage = SpawnImage(new List<Image>(), button, new Color(1, 1, 1, 1), new Vector2(0.5f, 0.5f), new Vector2(-4, -12), new Vector2(14, 4), buttonOutlineSprite);
                button.GetComponent<RoR2.UI.HGButton>().imageOnHover = highlightImage;

                // Add in the restart text
                List<TMPro.TextMeshProUGUI> buttonText = new List<TMPro.TextMeshProUGUI>();
                CreateText(buttonText, button, new Color(1, 1, 1, 1), 24, 0, new Vector2(12, 4), new Vector2(-12, -4), "Restart");

                // Place our button above the pause menu buttons
                button.transform.SetAsFirstSibling();

                // Set up what to do when the button is clicked
                button.GetComponent<RoR2.UI.HGButton>().onClick.AddListener(() => {
                    // Close the pause menu
                    self.InvokeMethod("OnDisable");
                    UnityEngine.Object.Destroy(self.gameObject);

                    if (!(Run.instance is null || Run.instance.gameObject is null))
                    {
                        UnityEngine.Object.Destroy(Run.instance.gameObject);//CCRunEnd
                    }
                    
                    // Start a new game, but wait a bit before we do so the PreGameController has time to get created
                    // We do this on another thread since this method is running on the same thread as UI
                    ThreadStart work = StartNewGame;
                    Thread thread = new Thread(work);
                    thread.Start();

                });
            };
        }

        private void StartNewGame()
        {
            System.Threading.Thread.Sleep(1000);
            if (!(PreGameController.instance is null))
            {
                PreGameController.instance.InvokeMethod("StartRun");//CCPregameStartRun
            }
        }

    }

}