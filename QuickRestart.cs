using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using R2API.Utils;
using BepInEx.Configuration;
using QuickRestart;
using RoR2.UI;
using BepInEx.Logging;
using TMPro;

namespace Booth
{

    internal static class Log
    {
        private static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void Debug(object data) => _logSource.LogDebug(data);
        internal static void Error(object data) => _logSource.LogError(data);
        internal static void Fatal(object data) => _logSource.LogFatal(data);
        internal static void Info(object data) => _logSource.LogInfo(data);
        internal static void Message(object data) => _logSource.LogMessage(data);
        internal static void Warning(object data) => _logSource.LogWarning(data);
    }

    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin("com.IkalaGaming.QuickRestart", "QuickRestart", "1.5.0")]
    public class QuickRestart : BaseUnityPlugin
    {
       
        public void SetupConfig()
        {
            ConfigRestartButtonPosition = Config.Bind<String>(
            "Graphics",
            "RestartPosition",
            "bottom",
            "The position of the restart button in the pause menu. Options are 'top', 'bottom', or the number of positions away from the top, so '1' would be 1 below the top item and thus second in the list. Falls back to default if you give weird values."
            );

            ConfigCharacterButtonPosition = Config.Bind<String>(
            "Graphics",
            "CharacterPosition",
            "bottom",
            "The position of the character select button in the pause menu. Options are 'top', 'bottom', or the number of positions away from the top, so '1' would be 1 below the top item and thus second in the list. Falls back to default if you give weird values. Evaluated after the restart button is placed."
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
            "The key that has to be pressed to reset. Falls back to default if you give weird values."
            );

            // Convert the keybind to a real key code, falling back to default if there are issues
            try
            {
                ResetKeyCode = (KeyCode) Enum.Parse(typeof(KeyCode), ConfigResetKeyBind.Value);
            }
            catch (Exception)
            {
                ResetKeyCode = KeyCode.T;
            }

            ConfigResetKeyHoldTime = Config.Bind<float>(
            "Keybind",
            "ResetKeyHoldTime",
            1.0f,
            "The number of seconds that the reset key has to be held in order to reset. Falls back to default if you give weird values."
            );

            if (ConfigResetKeyHoldTime.Value >= 0)
            {
                ResetKeyThreshold = ConfigResetKeyHoldTime.Value;
            }
        }

        void HandleResetKey()
        {
            if (Input.GetKey(ResetKeyCode))
            {
                TimeSpentHoldingKey += Time.deltaTime;
                if (TimeSpentHoldingKey > ResetKeyThreshold && !ResetAlready)
                {
                    /*
                     * I would remember that the game has been reset and not allow
                     * it to reset again until the key is released, except for some
                     * reason, when resetting the game it thinks the key has been
                     * released, so GetKeyUp gets called anyways. Oh well, I guess
                     * we can just hold down the key and continuously reset.
                     */
                    PauseScreenController PauseScreen = null;
                    if (PauseScreenController.instancesList.Count > 0)
                    {
                        PauseScreen = PauseScreenController.instancesList[0];
                    }
                    TimeSpentHoldingKey = 0f;
                    ResetAlready = true;
                    BoothUtil.ResetGame(PauseScreen, ConfigConfirmationDialog.Value, this, true);
                }
            }
            if (Input.GetKeyUp(ResetKeyCode))
            {
                TimeSpentHoldingKey = 0f;
                ResetAlready = false;
            }
        }

        void Update()
        {
            if (ConfigResetKeyEnabled.Value)
            {
                bool InRun = !(Run.instance is null);
                bool Multiplayer = PlayerCharacterMasterController.instances.Count > 1 && !BoothUtil.IsMultiplayerHost();
                if (InRun && !Multiplayer && !IsInChatBox)
                {
                    // Done this way so we can have other things in the update function
                    HandleResetKey();
                }
            }
        }

        public void Awake()
        {
            Log.Init(Logger);
            SetupConfig();

            AssetBundle bundle;
            // Make our assets available to load
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("QuickRestart.booth_assets"))
            {
                bundle = AssetBundle.LoadFromStream(stream);
            }

            // Set up textures for the UI button
            Texture2D buttonTexture = bundle.LoadAsset<Texture2D>("Assets/Texture2D/Booth_texUICleanButton.png");
            Texture2D buttonBorderTexture = bundle.LoadAsset<Texture2D>("Assets/Texture2D/Booth_texUIOutlineOnly.png");
            Texture2D buttonOutlineTexture = bundle.LoadAsset<Texture2D>("Assets/Texture2D/Booth_texUIHighlightBoxOutlineThick.png");

            // Needed to convert the textures to sprites
            Rect buttonTextureDimensions = new Rect(0, 0, buttonTexture.width, buttonTexture.height);
            Rect buttonBorderTextureeDimensions = new Rect(0, 0, buttonBorderTexture.width, buttonBorderTexture.height);
            Rect buttonOutlineTextureeDimensions = new Rect(0, 0, buttonOutlineTexture.width, buttonOutlineTexture.height);

            // Used to draw the button on UI
            Sprite buttonSprite = Sprite.Create(buttonTexture, buttonTextureDimensions, new Vector2(0, 0));
            Sprite buttonBorderSprite = Sprite.Create(buttonBorderTexture, buttonBorderTextureeDimensions, new Vector2(0, 0));
            Sprite buttonOutlineSprite = Sprite.Create(buttonOutlineTexture, buttonOutlineTextureeDimensions, new Vector2(0, 0));

            On.RoR2.UI.ChatBox.FocusInputField += (orig, self) => { orig(self); IsInChatBox = true; };
            On.RoR2.UI.ChatBox.UnfocusInputField += (orig, self) => { orig(self); IsInChatBox = false; };

            On.RoR2.UI.PauseScreenController.Awake += (orig, self) => {
                orig(self);
                if (Run.instance is null || PreGameController.instance)
                {
                    // Don't show in lobby
                    return;
                }
                Transform firstButton = self.mainPanel.GetChild(0).GetChild(0);

                //Add restart button to the pause screen

                Transform restartButton = Instantiate(firstButton, self.mainPanel.GetChild(0));

                TextMeshProUGUI originalRestartText = restartButton.GetComponentInChildren<HGTextMeshProUGUI>();
                originalRestartText.text = "Restart";
                //The GUI refuses to update without literally replacing the component
                Transform originalRestartTextParent = originalRestartText.transform.parent;
                TextMeshProUGUI newRestartText = Instantiate(originalRestartText, originalRestartTextParent);
                Destroy(originalRestartText);

                restartButton.GetComponentInChildren<HGButton>().onClick.RemoveAllListeners();
                restartButton.GetComponentInChildren<HGButton>().onClick.AddListener(() => {
                    BoothUtil.ResetGame(self, ConfigConfirmationDialog.Value, this, true);
                });

                if ("top".Equals(ConfigRestartButtonPosition.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    restartButton.transform.SetAsFirstSibling();
                }
                else if ("bottom".Equals(ConfigRestartButtonPosition.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    restartButton.transform.SetAsLastSibling();
                }
                else
                {
                    try
                    {
                        int position = Convert.ToInt32(ConfigRestartButtonPosition.Value);
                        if (position < 0)
                        {
                            position = 0;
                        }
                        else if (position >= restartButton.transform.parent.childCount)
                        {
                            position = restartButton.transform.parent.childCount - 1;
                        }
                        restartButton.transform.SetSiblingIndex(position);
                    }
                    catch (FormatException)
                    {
                        //default to bottom
                        restartButton.transform.SetAsLastSibling();
                    }
                }
                
                if (PlayerCharacterMasterController.instances.Count > 1 && !BoothUtil.IsMultiplayerHost())
                {
                    // Disable on multiplayer, unless they are the host
                    restartButton.gameObject.SetActive(false);
                }

                // Set up what to do when the button is clicked
                restartButton.GetComponent<RoR2.UI.HGButton>().onClick.AddListener(() => {
                    BoothUtil.ResetGame(self, ConfigConfirmationDialog.Value, this, true);
                });


                //Add Back to Character Select to the pause screen

                Transform characterSelectButton = Instantiate(firstButton, self.mainPanel.GetChild(0));

                TextMeshProUGUI originalCharacterSelectText = characterSelectButton.GetComponentInChildren<HGTextMeshProUGUI>();
                originalCharacterSelectText.text = "Character Select";
                //The GUI refuses to update without literally replacing the component
                Transform originalCharacterSelectTextParent = originalCharacterSelectText.transform.parent;
                TextMeshProUGUI newCharacterSelectText = Instantiate(originalCharacterSelectText, originalCharacterSelectTextParent);
                Destroy(originalCharacterSelectText);

                if ("top".Equals(ConfigCharacterButtonPosition.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    characterSelectButton.transform.SetAsFirstSibling();
                }
                else if ("bottom".Equals(ConfigCharacterButtonPosition.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    characterSelectButton.transform.SetAsLastSibling();
                }
                else
                {
                    try
                    {
                        int position = Convert.ToInt32(ConfigCharacterButtonPosition.Value);
                        if (position < 0)
                        {
                            position = 0;
                        }
                        else if (position >= characterSelectButton.transform.parent.childCount)
                        {
                            position = characterSelectButton.transform.parent.childCount - 1;
                        }
                        characterSelectButton.transform.SetSiblingIndex(position);
                    }
                    catch (FormatException)
                    {
                        //default to bottom
                        characterSelectButton.transform.SetAsLastSibling();
                    }
                }

                if (PlayerCharacterMasterController.instances.Count > 1 && !BoothUtil.IsMultiplayerHost())
                {
                    // Disable on multiplayer, unless they are the host
                    characterSelectButton.gameObject.SetActive(false);
                }

                characterSelectButton.GetComponentInChildren<HGButton>().onClick.RemoveAllListeners();
                characterSelectButton.GetComponent<HGButton>().onClick.AddListener(() => {
                    BoothUtil.ResetGame(self, ConfigConfirmationDialog.Value, this, false);
                });


            };
        }
        
        public static ConfigEntry<String> ConfigRestartButtonPosition { get; set; }
        public static ConfigEntry<String> ConfigCharacterButtonPosition { get; set; }
        public static ConfigEntry<bool> ConfigResetKeyEnabled { get; set; }
        public static ConfigEntry<String> ConfigResetKeyBind { get; set; }
        public static ConfigEntry<float> ConfigResetKeyHoldTime { get; set; }
        public static ConfigEntry<bool> ConfigConfirmationDialog { get; set; }

        private static KeyCode ResetKeyCode = KeyCode.T;
        private float TimeSpentHoldingKey = 0f;
        private float ResetKeyThreshold = 1f;
        private bool ResetAlready = false;
        private bool IsInChatBox = false;
    }
}
