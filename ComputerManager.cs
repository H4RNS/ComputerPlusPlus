using System;
using System.Collections.Generic;
using ComputerPlusPlus.Screens;
using ComputerPlusPlus.Tools;
using GorillaNetworking;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ComputerPlusPlus
{
    public class ComputerManager : MonoBehaviour
    {
        public static ComputerManager Instance;

        // Screens
        public List<IScreen> Screens = new List<IScreen>();
        public int currentScreenIndex = 0, currentPage = 0;
        public IScreen currentScreen;

        // Background
        public Transform backgroundPlane;
        public Material backgroundMaterial;

        // Text
        public TMP_Text screenText, originalScreenText;
        public TMP_Text functionsText, originalFunctionText;

        public const string Divider = "==========================================\n";
        string screenContent, functionsContent;
        const int maxLines = 13;
        const int screenWidth = 43;
        Font font;

        private static Dictionary<string, GorillaKeyboardButton> keys
            = new Dictionary<string, GorillaKeyboardButton>();
        public static Traverse ComputerTraverse;

        void Awake() => Instance = this;

        public void RegisterScreen(IScreen screen)
        {
            Screens.Add(screen);
            Logging.Debug($"Registered Screen: {screen.Title}");
        }

        public void UnregisterScreen(IScreen screen)
        {
            Screens.Remove(screen);
        }

        public void Initialize()
        {
            // guard: ensure there's at least one screen
            if (Screens == null) Screens = new List<IScreen>();
            if (Screens.Count == 0)
            {
                Logging.Warning("No screens registered in ComputerManager.Initialize()");
            }

            currentScreen = Screens.Count > 0 ? Screens[currentScreenIndex] : null;

            // Guard: GorillaComputer.instance may not be present (avoid null access)
            if (GorillaComputer.instance != null)
            {
                try
                {
                    ComputerTraverse = Traverse.Create(GorillaComputer.instance);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    ComputerTraverse = null;
                }
            }
            else
            {
                ComputerTraverse = null;
                Logging.Warning("GorillaComputer.instance is null in ComputerManager.Initialize()");
            }

            try
            {
                foreach (var button in FindObjectsOfType<GorillaKeyboardButton>())
                {
                    if (button == null) continue;
                    var keyStr = button.characterString;
                    if (string.IsNullOrEmpty(keyStr)) continue;

                    if (!keys.ContainsKey(keyStr))
                    {
                        keys.Add(keyStr, button);
                    }
                    else
                    {
                        Logging.Debug($"Duplicate keyboard key found, skipping: {keyStr}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }

            // If GorillaComputer.instance is available, try to get and clone text fields
            if (GorillaComputer.instance != null && ComputerTraverse != null)
            {
                try
                {
                    var origScreenTextObj = ComputerTraverse.Field("screenText").GetValue<TMP_Text>();
                    originalScreenText = origScreenTextObj;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    originalScreenText = null;
                }

                try
                {
                    var origFuncTextObj = ComputerTraverse.Field("functionSelectText").GetValue<TMP_Text>();
                    originalFunctionText = origFuncTextObj;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    originalFunctionText = null;
                }

                try
                {
                    if (originalScreenText != null)
                        screenText = CloneAndScale(originalScreenText);
                    if (originalFunctionText != null)
                        functionsText = CloneAndScale(originalFunctionText);
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                }
            }
            else
            {
                // If we couldn't find the computer/text, avoid using them later
                originalScreenText = null;
                originalFunctionText = null;
                screenText = null;
                functionsText = null;
            }

            UpdateFunctions();

            try
            {
                if (Tools.AssetUtils.LoadImageFromFile() is Texture2D texture)
                {
                    if (backgroundMaterial == null)
                    {
                        var bundle = Tools.AssetUtils.LoadAssetBundle("ComputerPlusPlus/cppbundle");
                        if (bundle != null)
                        {
                            font = bundle.LoadAsset<Font>("Font");
                            backgroundMaterial = Instantiate(bundle.LoadAsset<Material>("m_Unlit"));
                            backgroundMaterial.color = new Color(1 / 9f, 1 / 9f, 1 / 9f);
                        }
                    }

                    if (backgroundMaterial != null)
                    {
                        backgroundMaterial.mainTexture = texture;
                        backgroundPlane = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
                        backgroundPlane.name = "C++ Background Plane";
                        if (screenText != null)
                            backgroundPlane.SetParent(screenText.transform.parent);
                        backgroundPlane.localPosition = new Vector3(0, -0.286f, 0.5f);
                        backgroundPlane.localScale = new Vector3(.7f, .4f, 1f);
                        backgroundPlane.localRotation = screenText != null ? screenText.transform.localRotation : Quaternion.identity;
                        backgroundPlane.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
                        var mr = backgroundPlane.GetComponent<MeshRenderer>();
                        if (mr != null)
                            mr.material = backgroundMaterial;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }

            foreach (var screen in Screens)
            {
                try
                {
                    screen.Start();
                }
                catch (Exception e)
                {
                    Logging.Exception(e);
                }
            }
        }

        public TMP_Text CloneAndScale(TMP_Text original, float scaleDelta = -0.1f)
        {
            TMP_Text clone = Instantiate(original);
            clone.transform.SetParent(original.transform.parent);
            clone.transform.localPosition = original.transform.localPosition;
            clone.transform.localScale = original.transform.localScale;
            clone.transform.localRotation = original.transform.localRotation;
            clone.rectTransform.sizeDelta = new Vector2(
                clone.rectTransform.sizeDelta.x * (1 - scaleDelta),
                clone.rectTransform.sizeDelta.y * (1 - scaleDelta));
            clone.rectTransform.localScale = new Vector3(
                clone.rectTransform.localScale.x * (1 + scaleDelta),
                clone.rectTransform.localScale.y * (1 + scaleDelta),
                clone.rectTransform.localScale.z * (1 + scaleDelta));
            original.enabled = false;
            clone.fontSize = 10;
            clone.enableWordWrapping = false;
            clone.richText = true;
            clone.name = "C++ " + original.name;
            return clone;
        }

        public void OnKeyPressed(GorillaKeyboardButton button)
        {
            if (button == null) return;

            if (button.characterString == "up")
            {
                // Decrement the current screen index
                currentScreenIndex--;
                // If the current screen index is less than 0, set it to the last screen
                if (currentScreenIndex < 0)
                {
                    currentScreenIndex = Screens.Count - 1;
                    currentPage = Screens.Count / maxLines;
                }
                // If the current screen index is less than the current page, decrement the current page
                if (currentScreenIndex < currentPage * maxLines)
                {
                    currentPage--;
                }
                currentScreen = Screens[currentScreenIndex];
                UpdateFunctions();
            }
            else if (button.characterString == "down")
            {
                // Increment the current screen index
                currentScreenIndex++;
                // If the current screen index is greater than the last screen, set it to the first screen
                if (currentScreenIndex >= Screens.Count)
                {
                    currentScreenIndex = 0;
                    currentPage = 0;
                }
                // If the current screen index is greater than the current page, increment the current page
                if (currentScreenIndex >= (currentPage + 1) * maxLines)
                {
                    currentPage++;
                }
                currentScreen = Screens[currentScreenIndex];
                UpdateFunctions();
            }

            try
            {
                currentScreen?.OnKeyPressed(button);
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }
        }

        void UpdateFunctions()
        {
            FunctionsText = "";
            int maxLength = 9;

            for (int i = 0; i < maxLines; i++)
            {
                if (Screens.Count <= i + currentPage * maxLines)
                    break;
                var screen = Screens[i + currentPage * maxLines];
                string title = screen.Title.ToUpper().Trim();
                if (title.Length > maxLength)
                    title = title.Substring(0, maxLength);
                if (screen == currentScreen)
                    FunctionsText += ">";
                else
                    FunctionsText += " ";
                FunctionsText += title + "\n";
            }
            if (Screens.Count > (currentPage + 1) * maxLines)
                FunctionsText += " ...";
        }

        /* string Template =
             "{0}\n" +
             Divider +
             "{1}\n" +
             Divider +
             "\n" +
             "{2}\n";*/

        void FixedUpdate()
        {
            if (currentScreen == null)
                return;

            if (originalScreenText)
                originalScreenText.enabled = false;
            if (originalFunctionText)
                originalFunctionText.enabled = false;
            var text = "";

            try
            {
                if (!string.IsNullOrEmpty(currentScreen?.Title))
                {
                    text += Center(currentScreen.Title.ToUpper()) + "\n";
                    text += Divider;
                }
                if (!string.IsNullOrEmpty(currentScreen?.Description))
                {
                    text += currentScreen.Description.ToUpper() + "\n";
                    text += Divider;
                }

                // SAFE: protect against null content from screens
                var content = currentScreen?.GetContent();
                if (content == null) content = "";
                text += content.ToUpper();

                ScreenText = text;
            }
            catch (Exception ex)
            {
                // Catch and log any screen content formatting errors to prevent spam/crash
                Logging.Exception(ex);
            }
        }



        void OnDisable()
        {
            if (screenText != null) screenText.enabled = false;
            if (functionsText != null) functionsText.enabled = false;
            if (originalFunctionText != null) originalFunctionText.enabled = true;
            if (originalScreenText != null) originalScreenText.enabled = true;
        }

        public static string Center(string text, char padWith = ' ')
        {
            int width = Divider.Length;
            int padding = (width - text.Length) / 2;
            string result = "";
            for (int i = 0; i < padding; i++)
                result += padWith;
            result += text;
            for (int i = 0; i < padding; i++)
                result += padWith;
            return result;
        }

        public static string Scrolling(string text, int speed = 5, int width = screenWidth)
        {
            if (text.Length < width)
                return text;
            text += " --- ";
            int start = (int)(Time.time * speed) % text.Length;
            return
                text.Substring(start, Mathf.Min(width, text.Length - start))
                + text.Substring(0, Mathf.Max(0, width - (text.Length - start)));
        }

        public string ScreenText
        {
            get
            {
                return screenContent;
            }
            set
            {
                screenContent = value;
                if (screenText)
                    screenText.text = screenContent;
            }
        }

        public string FunctionsText
        {
            get
            {
                return functionsContent;
            }
            set
            {
                functionsContent = value;
                if (functionsText)
                    functionsText.text = functionsContent;
            }
        }

        public string EnabledColor =>
            "#" + ColorUtility.ToHtmlStringRGB(screenText.color.Lighter(.3f));

        public string DisabledColor =>
            "#" + ColorUtility.ToHtmlStringRGB(screenText.color.Darker(.3f));

        public static Dictionary<string, GorillaKeyboardButton> Keys
        {
            get { return keys; }
        }

        public static GorillaKeyboardButton GetKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (keys.TryGetValue(key, out var btn)) return btn;
            return null;
        }

        public static GorillaKeyboardButton GetKey(int key)
        {
            var s = key.ToString();
            if (keys.TryGetValue(s, out var btn)) return btn;
            return null;
        }

        public static Traverse Field(string fieldName)
        {
            return ComputerTraverse.Field(fieldName);
        }
    }
}
