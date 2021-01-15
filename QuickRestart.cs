using BepInEx;
using RoR2;
using R2API;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using R2API.Utils;
using BepInEx.Configuration;
using QuickRestart;

namespace Booth
{

    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.IkalaGaming.QuickRestart", "QuickRestart", "1.2.2")]
    [R2APISubmoduleDependency(nameof(ResourcesAPI))]
    public class QuickRestart : BaseUnityPlugin
    {
       
        public void SetupConfig()
        {
            ConfigButtonPosition = Config.Bind<String>(
            "Graphics",
            "ButtonPosition",
            "bottom",
            "The position of the button in the pause menu. Options are 'top', 'bottom', or the number of positions away from the top, so '1' would be 1 below the top item and thus second in the list"
            );

            ConfigConfirmationDialog = Config.Bind<bool>(
            "Graphics",
            "ConfirmationDialogEnabled",
            false,
            "Enables a confirmation dialog when trying to reset so it is not done accidentally"
            );

            ConfigResetKeyEnabled = Config.Bind<bool>(
            "Keybind",
            "ResetKeyEnabled",
            false,
            "Allows a key press to be used to reset runs in addition to the menu"
            );

            ConfigResetKeyBind = Config.Bind<String>(
            "Keybind",
            "ResetKeyBind",
            "T",
            "The key that has to be pressed to reset"
            );

            ConfigResetKeyHoldTime = Config.Bind<int>(
            "Keybind",
            "ResetKeyHoldTime",
            1000,
            "The number of ms that the reset key has to be held in order to reset. Divide by 1000 to get seconds"
            );
        }

        public void Awake()
        {
            SetupConfig();

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
                orig(self);
                //Vector2 buttonSize = new Vector2(320, 48);
                Vector2 buttonSize = new Vector2(320, 48);
                GameObject button = BoothUtil.CreateButton(self.mainPanel.GetChild(0).gameObject, buttonSize, buttonSprite);

                // Add in the stylized highlight/border
                List<Image> images = new List<Image>();
                BoothUtil.SpawnImage(images, button, new Color(1, 1, 1, 1), new Vector2(0.5f, 0.5f), new Vector2(-6, -6), new Vector2(6, 6), buttonHighlightSprite);
                images[images.Count - 1].gameObject.SetActive(false);

                // Add in the sharp white border line
                BoothUtil.SpawnImage(new List<Image>(), button, new Color(1, 1, 1, 0.286f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(0, 0), buttonBorderSprite);

                // Add in the thicker surrounding outline for when you hover on the button
                Image highlightImage = BoothUtil.SpawnImage(new List<Image>(), button, new Color(1, 1, 1, 1), new Vector2(0.5f, 0.5f), new Vector2(-4, -12), new Vector2(14, 4), buttonOutlineSprite);
                button.GetComponent<RoR2.UI.HGButton>().imageOnHover = highlightImage;

                // Add in the restart text
                List<TMPro.TextMeshProUGUI> buttonText = new List<TMPro.TextMeshProUGUI>();
                BoothUtil.CreateText(buttonText, button, new Color(1, 1, 1, 1), 24, 0, new Vector2(12, 4), new Vector2(-12, -4), "Restart");

                // Place our button above the pause menu buttons
                button.transform.SetAsLastSibling();

                if (PlayerCharacterMasterController.instances.Count > 1)

                {
                    // Disable on multiplayer, as it's broken there and I don't have a fix yet.instances
                    button.SetActive(false);
                }

                // Set up what to do when the button is clicked
                button.GetComponent<RoR2.UI.HGButton>().onClick.AddListener(() => {
                    BoothUtil.ResetGame(self);
                });
            };
        }

        
        public static ConfigEntry<String> ConfigButtonPosition { get; set; }
        public static ConfigEntry<bool> ConfigResetKeyEnabled { get; set; }
        public static ConfigEntry<String> ConfigResetKeyBind { get; set; }
        public static ConfigEntry<int> ConfigResetKeyHoldTime { get; set; }
        public static ConfigEntry<bool> ConfigConfirmationDialog { get; set; }

    }

}