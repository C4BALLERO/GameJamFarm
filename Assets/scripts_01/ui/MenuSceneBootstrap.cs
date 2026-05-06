using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Globalization;

/// <summary>
/// Hooks the menu start button at runtime so the menu scene survives builds without hand-authored UnityEvents.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuSceneBootstrap : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "scene_00_main";
    [SerializeField] private Button startButton;
    [Header("Form fields (optional, auto-created if empty)")]
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private InputField birthDateInput;
    [SerializeField] private Text formMessageText;
    [Header("Loading (optional)")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Image loadingBarFill;
    [SerializeField] private Text loadingText;

    private const string PlayerNamePrefKey = "player_name";
    private const string BirthDatePrefKey = "player_birth_date";
    private const int MinAllowedAge = 13;
    private static readonly string[] BirthDateFormats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
    private bool _isFormattingBirthDate;
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
        RefreshStartButtonState();
    }

    private void EnsureForm()
    {
        if (startButton == null)
            return;

        var canvas = startButton.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        SetButtonLabel(startButton, "Iniciar juego");
        if (playerNameInput != null && birthDateInput != null)
            return;

        var panel = new GameObject("StartFormPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.46f);
        panelRect.anchorMax = new Vector2(0.75f, 0.76f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);

        playerNameInput = CreateInputField(panel.transform, "NombreJugadorInput", "Nombre del jugador", new Vector2(0f, 50f), _uiFont);
        birthDateInput = CreateInputField(panel.transform, "FechaNacimientoInput", "Fecha de nacimiento (dd/MM/yyyy)", new Vector2(0f, -10f), _uiFont);

        var message = CreateText(panel.transform, "InfoText", "Completa ambos campos para continuar.", 18, new Vector2(0f, -72f), _uiFont);
        formMessageText = message;
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

        if (birthDateInput != null)
        {
            birthDateInput.onValueChanged.RemoveListener(OnBirthDateChanged);
            birthDateInput.onValueChanged.AddListener(OnBirthDateChanged);
        }
    }

    private void OnFieldChanged(string _)
    {
        RefreshStartButtonState();
    }

    private void OnBirthDateChanged(string value)
    {
        if (_isFormattingBirthDate)
            return;

        var formatted = FormatBirthDateInput(value);
        if (!string.Equals(formatted, value, StringComparison.Ordinal))
        {
            _isFormattingBirthDate = true;
            birthDateInput.SetTextWithoutNotify(formatted);
            birthDateInput.caretPosition = formatted.Length;
            _isFormattingBirthDate = false;
        }

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
            formMessageText.text = "Datos correctos. Ya puedes iniciar el juego.";
        else
            formMessageText.text = validationError;
    }

    private void LoadMain()
    {
        if (_isLoadingScene)
            return;

        if (!TryGetValidationError(out _))
            return;

        PlayerPrefs.SetString(PlayerNamePrefKey, playerNameInput.text.Trim());
        PlayerPrefs.SetString(BirthDatePrefKey, birthDateInput.text.Trim());
        PlayerPrefs.Save();

        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogError("[MenuSceneBootstrap] Main scene name is empty.");
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

        var operation = SceneManager.LoadSceneAsync(mainSceneName);
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

    private bool HasValidBirthDate()
    {
        return TryGetBirthDate(out _);
    }

    private bool TryGetValidationError(out string validationError)
    {
        if (!HasValidName())
        {
            validationError = "Ingresa un nombre de jugador.";
            return false;
        }

        if (!TryGetBirthDate(out var birthDate))
        {
            validationError = "Fecha invalida. Usa formato dd/MM/yyyy.";
            return false;
        }

        if (!HasMinimumAge(birthDate, MinAllowedAge))
        {
            validationError = "Debes tener al menos 13 anos para jugar.";
            return false;
        }

        validationError = string.Empty;
        return true;
    }

    private bool TryGetBirthDate(out DateTime birthDate)
    {
        birthDate = default;
        if (birthDateInput == null || string.IsNullOrWhiteSpace(birthDateInput.text))
            return false;

        var value = birthDateInput.text.Trim();
        if (DateTime.TryParseExact(value, BirthDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate))
            return true;

        return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out birthDate);
    }

    private static bool HasMinimumAge(DateTime birthDate, int minAge)
    {
        var today = DateTime.Today;
        if (birthDate > today)
            return false;

        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;

        return age >= minAge;
    }

    private static string FormatBirthDateInput(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var digits = ExtractDigits(value, 8);
        if (digits.Length <= 2)
            return digits;
        if (digits.Length <= 4)
            return digits.Substring(0, 2) + "/" + digits.Substring(2);

        return digits.Substring(0, 2) + "/" + digits.Substring(2, 2) + "/" + digits.Substring(4);
    }

    private static string ExtractDigits(string source, int maxDigits)
    {
        var chars = new char[maxDigits];
        var count = 0;

        for (var i = 0; i < source.Length && count < maxDigits; i++)
        {
            var c = source[i];
            if (!char.IsDigit(c))
                continue;

            chars[count] = c;
            count++;
        }

        return new string(chars, 0, count);
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
        background.color = Color.white;

        var text = CreateText(root.transform, "Text", string.Empty, 20, Vector2.zero, font);
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.black;
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 6f);
        textRect.offsetMax = new Vector2(-12f, -6f);

        var placeholder = CreateText(root.transform, "Placeholder", placeholderText, 18, Vector2.zero, font);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color(0.45f, 0.45f, 0.45f, 0.8f);
        var placeholderRect = placeholder.rectTransform;
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(12f, 6f);
        placeholderRect.offsetMax = new Vector2(-12f, -6f);

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
        text.color = Color.white;
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
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        var barBg = new GameObject("LoadingBarBackground", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(loadingPanel.transform, false);
        var barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.08f, 0.42f);
        barBgRect.anchorMax = new Vector2(0.92f, 0.7f);
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;
        barBg.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 1f);

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
        loadingBarFill.color = new Color(0.25f, 0.78f, 0.28f, 1f);

        loadingText = CreateText(loadingPanel.transform, "LoadingText", "Cargando... 0%", 18, new Vector2(0f, -18f), _uiFont);
        loadingText.alignment = TextAnchor.MiddleCenter;
    }
}
