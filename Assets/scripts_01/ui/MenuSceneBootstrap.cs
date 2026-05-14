using UnityEngine;

using UnityEngine.SceneManagement;

using UnityEngine.UI;

using System;

using System.Collections;



/// <summary>

/// Hooks the menu start button at runtime so the menu scene survives builds without hand-authored UnityEvents.

/// </summary>

[DisallowMultipleComponent]

public sealed class MenuSceneBootstrap : MonoBehaviour

{

    [SerializeField] private string mainSceneName = "scene_00_main";

    [SerializeField] private string storySceneName = "scene_02_story";

    [SerializeField] private bool loadStoryBeforeMainGameplay = true;

    [SerializeField] private Button startButton;

    [Header("Form fields (optional, auto-created if empty)")]

    [SerializeField] private InputField playerNameInput;

    [SerializeField] private Text formMessageText;

    [Header("Loading (optional)")]

    [SerializeField] private GameObject loadingPanel;

    [SerializeField] private Image loadingBarFill;

    [SerializeField] private Text loadingText;



    private const string PlayerNamePrefKey = "player_name";

    private const string BirthDatePrefKeyLegacy = "player_birth_date";

    private bool _buttonVisibilityInitialized;

    private bool _isLoadingScene;

    private Font _uiFont;



    private void Awake()

    {

        if (startButton == null)

            startButton = GetComponentInChildren<Button>(true);



        // Logo UI used an Image with no sprite: Unity draws a solid white quad. Remove leftovers from older runs.

        RemoveLegacyLogoPlaceholder();



        _uiFont = ResolveUiFont();

        EnsureForm();

        EnsureLoadingUi();

        HookEvents();

        StyleStartButton();

        RefreshStartButtonState();

        if (GetComponent<MainMenuManager>() == null)

            gameObject.AddComponent<MainMenuManager>();

    }



    private void StyleStartButton()

    {

        if (startButton == null)

            return;

        var img = startButton.GetComponent<Image>();

        if (img == null)

            img = startButton.gameObject.AddComponent<Image>();

        var tx = startButton.GetComponentInChildren<Text>(true);

        UIStyleSheet.ApplyMainMenuStartButton(img, tx);
        UIStyleSheet.ApplyButtonStates(startButton);

    }



    private void EnsureForm()

    {

        if (startButton == null)

            return;



        var canvas = startButton.GetComponentInParent<Canvas>();

        if (canvas == null)

            return;



        SetButtonLabel(startButton, "Iniciar juego");

        if (playerNameInput != null)

        {

            EnsureTitleOnExistingForm(canvas.transform);

            return;

        }



        var panel = new GameObject("StartFormPanel", typeof(RectTransform), typeof(Image));

        panel.transform.SetParent(canvas.transform, false);



        var panelRect = panel.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.22f, 0.38f);

        panelRect.anchorMax = new Vector2(0.78f, 0.78f);

        panelRect.offsetMin = Vector2.zero;

        panelRect.offsetMax = Vector2.zero;



        var panelImage = panel.GetComponent<Image>();

        UIStyleSheet.ApplyPanelImage(panelImage, 0.95f);



        CreateFormTitle(panel.transform, _uiFont);



        playerNameInput = CreateInputField(panel.transform, "NombreJugadorInput", "Nombre del jugador", new Vector2(0f, 0f), _uiFont);



        var message = CreateText(panel.transform, "InfoText", "Escribe tu nombre para continuar.", 17, new Vector2(0f, -72f), _uiFont);

        formMessageText = message;

        UIStyleSheet.ApplySecondaryText(formMessageText, 17);

    }



    private static void EnsureTitleOnExistingForm(Transform canvasRoot)

    {

        var panel = canvasRoot.Find("StartFormPanel");

        if (panel == null)

            return;

        if (panel.Find("GameTitleBanner") != null)

            return;

        CreateFormTitle(panel, UIStyleSheet.GetUiFont());

    }



    private static void CreateFormTitle(Transform panel, Font font)

    {

        var titleGo = new GameObject("GameTitleBanner", typeof(RectTransform), typeof(Text));

        titleGo.transform.SetParent(panel, false);

        var tr = titleGo.GetComponent<RectTransform>();

        tr.anchorMin = new Vector2(0.06f, 0.78f);

        tr.anchorMax = new Vector2(0.94f, 0.98f);

        tr.offsetMin = Vector2.zero;

        tr.offsetMax = Vector2.zero;

        var title = titleGo.GetComponent<Text>();

        title.font = font != null ? font : UIStyleSheet.GetUiFont();

        title.text = "DARK FARM SURVIVAL";

        title.alignment = TextAnchor.MiddleCenter;

        UIStyleSheet.ApplyPrimaryTitle(title, 30);

    }



    private void HookEvents()

    {

        if (startButton != null)

        {

            startButton.onClick.RemoveListener(LoadMain);

            startButton.onClick.AddListener(LoadMain);

        }



        if (playerNameInput != null)

        {

            playerNameInput.onValueChanged.RemoveListener(OnFieldChanged);

            playerNameInput.onValueChanged.AddListener(OnFieldChanged);

        }

    }



    private void OnFieldChanged(string _)

    {

        RefreshStartButtonState();

    }



    private void RefreshStartButtonState()

    {

        if (startButton == null)

            return;



        if (_isLoadingScene)

        {

            startButton.gameObject.SetActive(false);

            return;

        }



        var isValid = TryGetValidationError(out var validationError);

        if (!_buttonVisibilityInitialized)

        {

            startButton.gameObject.SetActive(false);

            _buttonVisibilityInitialized = true;

        }



        startButton.gameObject.SetActive(isValid);

        startButton.interactable = isValid;



        if (formMessageText == null)

            return;



        if (isValid)

        {

            formMessageText.text = "Listo. Ya puedes iniciar el juego.";

            UIStyleSheet.ApplyBodyText(formMessageText, 17);

        }

        else

        {

            formMessageText.text = validationError;

            formMessageText.color = UIStyleSheet.AccentGold;

        }

    }



    private void LoadMain()

    {

        if (_isLoadingScene)

            return;



        if (!TryGetValidationError(out _))

            return;



        PlayerPrefs.SetString(PlayerNamePrefKey, playerNameInput.text.Trim());

        PlayerPrefs.DeleteKey(BirthDatePrefKeyLegacy);

        PlayerPrefs.Save();



        if (string.IsNullOrWhiteSpace(mainSceneName))

        {

            Debug.LogError("[MenuSceneBootstrap] Main scene name is empty.");

            return;

        }

        if (loadStoryBeforeMainGameplay && string.IsNullOrWhiteSpace(storySceneName))

        {

            Debug.LogError("[MenuSceneBootstrap] Story scene name is empty while loadStoryBeforeMainGameplay is enabled.");

            return;

        }



        StartCoroutine(LoadMainAsync());

    }



    private IEnumerator LoadMainAsync()

    {

        _isLoadingScene = true;

        if (startButton != null)

            startButton.interactable = false;



        if (loadingPanel != null)

            loadingPanel.SetActive(true);

        SetLoadingProgress(0f);



        var targetScene = loadStoryBeforeMainGameplay && !string.IsNullOrWhiteSpace(storySceneName)

            ? storySceneName

            : mainSceneName;



        var operation = SceneManager.LoadSceneAsync(targetScene);

        if (operation == null)

        {

            _isLoadingScene = false;

            yield break;

        }



        operation.allowSceneActivation = false;

        while (!operation.isDone)

        {

            var progress = Mathf.Clamp01(operation.progress / 0.9f);

            SetLoadingProgress(progress);



            if (operation.progress >= 0.9f)

            {

                SetLoadingProgress(1f);

                operation.allowSceneActivation = true;

            }



            yield return null;

        }

    }



    private void SetLoadingProgress(float progress)

    {

        if (loadingBarFill != null)

            loadingBarFill.fillAmount = Mathf.Clamp01(progress);



        if (loadingText != null)

            loadingText.text = "Cargando... " + Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f) + "%";

    }



    private bool HasValidName()

    {

        return playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text);

    }



    private bool TryGetValidationError(out string validationError)

    {

        if (!HasValidName())

        {

            validationError = "Ingresa un nombre de jugador.";

            return false;

        }



        validationError = string.Empty;

        return true;

    }



    private static InputField CreateInputField(Transform parent, string objectName, string placeholderText, Vector2 anchoredPosition, Font font)

    {

        var root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(InputField));

        root.transform.SetParent(parent, false);



        var rootRect = root.GetComponent<RectTransform>();

        rootRect.anchorMin = new Vector2(0.1f, 0.5f);

        rootRect.anchorMax = new Vector2(0.9f, 0.5f);

        rootRect.sizeDelta = new Vector2(0f, 48f);

        rootRect.anchoredPosition = anchoredPosition;



        var background = root.GetComponent<Image>();



        var text = CreateText(root.transform, "Text", string.Empty, 20, Vector2.zero, font);

        text.alignment = TextAnchor.MiddleLeft;

        var textRect = text.rectTransform;

        textRect.anchorMin = Vector2.zero;

        textRect.anchorMax = Vector2.one;

        textRect.offsetMin = new Vector2(12f, 6f);

        textRect.offsetMax = new Vector2(-12f, -6f);



        var placeholder = CreateText(root.transform, "Placeholder", placeholderText, 18, Vector2.zero, font);

        placeholder.alignment = TextAnchor.MiddleLeft;

        var placeholderRect = placeholder.rectTransform;

        placeholderRect.anchorMin = Vector2.zero;

        placeholderRect.anchorMax = Vector2.one;

        placeholderRect.offsetMin = new Vector2(12f, 6f);

        placeholderRect.offsetMax = new Vector2(-12f, -6f);



        UIStyleSheet.SetupInputField(background, text, placeholder);



        var inputField = root.GetComponent<InputField>();

        inputField.textComponent = text;

        inputField.placeholder = placeholder;

        inputField.lineType = InputField.LineType.SingleLine;

        return inputField;

    }



    private static Text CreateText(Transform parent, string objectName, string content, int fontSize, Vector2 anchoredPosition, Font fallbackFont)

    {

        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));

        go.transform.SetParent(parent, false);



        var rect = go.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.1f, 0.5f);

        rect.anchorMax = new Vector2(0.9f, 0.5f);

        rect.sizeDelta = new Vector2(0f, 32f);

        rect.anchoredPosition = anchoredPosition;



        var text = go.GetComponent<Text>();

        text.font = fallbackFont != null ? fallbackFont : GetBuiltInUiFont();

        text.text = content;

        text.fontSize = fontSize;

        text.alignment = TextAnchor.MiddleCenter;

        UIStyleSheet.ApplyBodyText(text, fontSize);

        return text;

    }



    private Font ResolveUiFont()

    {

        var buttonText = startButton != null ? startButton.GetComponentInChildren<Text>(true) : null;

        if (buttonText != null && buttonText.font != null)

            return buttonText.font;



        return GetBuiltInUiFont();

    }



    private static Font GetBuiltInUiFont()

    {

        try

        {

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (font != null)

                return font;

        }

        catch (Exception)

        {

            // Keep searching fallbacks below.

        }



        try

        {

            return Resources.GetBuiltinResource<Font>("Arial.ttf");

        }

        catch (Exception)

        {

            return null;

        }

    }



    private static void SetButtonLabel(Button button, string label)

    {

        var text = button.GetComponentInChildren<Text>(true);

        if (text != null)

            text.text = label;

    }



    private void RemoveLegacyLogoPlaceholder()

    {

        if (startButton == null)

            return;



        var canvas = startButton.GetComponentInParent<Canvas>();

        if (canvas == null)

            return;



        RemoveLogoImageRecursive(canvas.transform);

    }



    private static void RemoveLogoImageRecursive(Transform parent)

    {

        for (var i = parent.childCount - 1; i >= 0; i--)

        {

            var child = parent.GetChild(i);

            RemoveLogoImageRecursive(child);

            if (child.name == "LogoImage")

                UnityEngine.Object.Destroy(child.gameObject);

        }

    }



    private void EnsureLoadingUi()

    {

        if (startButton == null)

            return;



        var canvas = startButton.GetComponentInParent<Canvas>();

        if (canvas == null)

            return;



        if (loadingPanel == null || loadingBarFill == null || loadingText == null)

            CreateLoadingUi(canvas.transform);



        if (loadingPanel != null)

            loadingPanel.SetActive(false);

    }



    private void CreateLoadingUi(Transform parent)

    {

        loadingPanel = new GameObject("LoadingPanel", typeof(RectTransform), typeof(Image));

        loadingPanel.transform.SetParent(parent, false);



        var panelRect = loadingPanel.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.2f, 0.08f);

        panelRect.anchorMax = new Vector2(0.8f, 0.2f);

        panelRect.offsetMin = Vector2.zero;

        panelRect.offsetMax = Vector2.zero;



        var panelImage = loadingPanel.GetComponent<Image>();

        UIStyleSheet.ApplyPanelImage(panelImage, 0.88f);



        var barBg = new GameObject("LoadingBarBackground", typeof(RectTransform), typeof(Image));

        barBg.transform.SetParent(loadingPanel.transform, false);

        var barBgRect = barBg.GetComponent<RectTransform>();

        barBgRect.anchorMin = new Vector2(0.08f, 0.42f);

        barBgRect.anchorMax = new Vector2(0.92f, 0.7f);

        barBgRect.offsetMin = Vector2.zero;

        barBgRect.offsetMax = Vector2.zero;

        var barBgImg = barBg.GetComponent<Image>();

        UIStyleSheet.StyleLoadingBarBackground(barBgImg);



        var barFill = new GameObject("LoadingBarFill", typeof(RectTransform), typeof(Image));

        barFill.transform.SetParent(barBg.transform, false);

        var barFillRect = barFill.GetComponent<RectTransform>();

        barFillRect.anchorMin = Vector2.zero;

        barFillRect.anchorMax = Vector2.one;

        barFillRect.offsetMin = new Vector2(2f, 2f);

        barFillRect.offsetMax = new Vector2(-2f, -2f);



        loadingBarFill = barFill.GetComponent<Image>();

        loadingBarFill.type = Image.Type.Filled;

        loadingBarFill.fillMethod = Image.FillMethod.Horizontal;

        loadingBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        loadingBarFill.fillAmount = 0f;

        UIStyleSheet.StyleLoadingBarFill(loadingBarFill);



        loadingText = CreateText(loadingPanel.transform, "LoadingText", "Cargando... 0%", 18, new Vector2(0f, -18f), _uiFont);

        loadingText.alignment = TextAnchor.MiddleCenter;

        UIStyleSheet.ApplyBodyText(loadingText, 18);

    }

}


