using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim_Build_Camera;

internal static class PickupBlockedHud
{
	private const float VisibilityCheckInterval = 0.2f;
	private const float PanelWidth = 460f;
	private const float HorizontalPadding = 18f;
	private const float TopPadding = 12f;
	private const float BottomPadding = 14f;
	private const float TextSpacing = 4f;

	private static GameObject _panel = null!;
	private static RectTransform _panelRect = null!;
	private static TextMeshProUGUI _titleText = null!;
	private static TextMeshProUGUI _bodyText = null!;
	private static float _nextVisibilityCheck;
	private static float _lastHudX = float.NaN;
	private static float _lastHudY = float.NaN;
	private static int _lastComfortLevel = -1;
	private static int _lastFontSize = -1;
	private static FieldInfo? _buildUiField;
	private static bool _buildUiFieldResolved;

	internal static void Update()
	{
		if (Time.time < _nextVisibilityCheck) return;
		_nextVisibilityCheck = Time.time + VisibilityCheckInterval;

		if (Hud.IsUserHidden() || !Utils.ShouldShowPickupWarning())
		{
			Hide();
			return;
		}

		if (!_panel)
		{
			_panelRect = null!;
			_titleText = null!;
			_bodyText = null!;
			if (!CreatePanel()) return;
		}

		GameObject panel = _panel!;
		if (!panel) return;
		bool wasHidden = !panel.activeSelf;
		if (wasHidden || NeedsRefresh()) RefreshPanel();
		if (wasHidden) panel.SetActive(true);
	}

	internal static void LanguageChanged()
	{
		_lastComfortLevel = -1;
	}

	internal static void Cleanup()
	{
		if (_panel) UnityEngine.Object.Destroy(_panel);
		_panel = null!;
		_panelRect = null!;
		_titleText = null!;
		_bodyText = null!;
		_nextVisibilityCheck = 0f;
		_lastHudX = float.NaN;
		_lastHudY = float.NaN;
		_lastComfortLevel = -1;
		_lastFontSize = -1;
	}

	private static bool CreatePanel()
	{
		MessageHud messageHud = MessageHud.instance;
		Hud hud = Hud.instance;
		if (!messageHud || !hud || !messageHud.m_messageText || !messageHud.m_messageText.font) return false;

		_panel = new GameObject("BuildCamera_PickupBlocked", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		_panel.SetActive(false);
		_panel.transform.SetParent(messageHud.transform, false);
		_panel.transform.SetAsLastSibling();
		_panelRect = (RectTransform)_panel.transform;

		Image background = _panel.GetComponent<Image>();
		background.raycastTarget = false;
		ApplyPanelStyle(background, hud);

		_titleText = CreateText("Title", messageHud.m_messageText);
		_bodyText = CreateText("Body", messageHud.m_messageText);
		_titleText.fontStyle = FontStyles.Bold;
		_titleText.alignment = TextAlignmentOptions.TopLeft;
		_bodyText.alignment = TextAlignmentOptions.TopLeft;
		_bodyText.textWrappingMode = TextWrappingModes.Normal;
		return true;
	}

	private static TextMeshProUGUI CreateText(string name, TMP_Text fontSource)
	{
		GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
		textObject.transform.SetParent(_panel.transform, false);
		TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
		text.font = fontSource.font;
		text.fontSharedMaterial = fontSource.fontSharedMaterial;
		text.color = fontSource.color;
		text.raycastTarget = false;
		text.enableAutoSizing = false;
		text.overflowMode = TextOverflowModes.Overflow;
		return text;
	}

	private static void ApplyPanelStyle(Image background, Hud hud)
	{
		Image source = FindStyleSource(hud);
		if (source)
		{
			background.sprite = source.sprite;
			background.type = source.type;
			background.color = source.color;
			background.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
			return;
		}

		background.color = new Color(0.06f, 0.055f, 0.045f, 0.92f);
	}

	private static Image FindStyleSource(Hud hud)
	{
		GameObject root = hud.m_pieceSelectionWindow;
		if (!root) root = GetBuildUiRoot(hud);
		if (!root) return null!;

		Transform panelBackground = root.transform.Find("Bkg2");
		if (panelBackground && panelBackground.GetComponent<Image>() is { } named) return named;

		foreach (Image candidate in root.GetComponentsInChildren<Image>(true))
		{
			if (candidate.sprite && candidate.name.IndexOf("bkg", StringComparison.OrdinalIgnoreCase) >= 0) return candidate;
		}

		return null!;
	}

	// Valheim 1.0 replaced m_pieceSelectionWindow with m_buildUi; read it late so one build serves both versions.
	private static GameObject GetBuildUiRoot(Hud hud)
	{
		if (!_buildUiFieldResolved)
		{
			_buildUiFieldResolved = true;
			_buildUiField = typeof(Hud).GetField("m_buildUi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		return _buildUiField?.GetValue(hud) is Component buildUi && buildUi ? buildUi.gameObject : null!;
	}

	private static bool NeedsRefresh()
	{
		if (_lastComfortLevel != Valheim_Build_CameraPlugin.minimumComfortLevel.Value) return true;
		if (_lastFontSize != Valheim_Build_CameraPlugin.pickupHudFontSize.Value) return true;
		if (!Mathf.Approximately(_lastHudX, Valheim_Build_CameraPlugin.pickupHudX.Value)) return true;
		return !Mathf.Approximately(_lastHudY, Valheim_Build_CameraPlugin.pickupHudY.Value);
	}

	private static void RefreshPanel()
	{
		_lastComfortLevel = Valheim_Build_CameraPlugin.minimumComfortLevel.Value;
		_lastFontSize = Valheim_Build_CameraPlugin.pickupHudFontSize.Value;
		_lastHudX = Valheim_Build_CameraPlugin.pickupHudX.Value;
		_lastHudY = Valheim_Build_CameraPlugin.pickupHudY.Value;

		_titleText.text = Localization.instance.Localize("$buildcamera_pickup_blocked_title");
		_bodyText.text = Localization.instance.Localize("$buildcamera_pickup_blocked", _lastComfortLevel.ToString());
		_titleText.fontSize = Mathf.Max(18, _lastFontSize + 2);
		_bodyText.fontSize = _lastFontSize;

		float titleHeight = _titleText.fontSize + 6f;
		float bodyHeight = Mathf.Max(_bodyText.fontSize * 2.4f, _bodyText.GetPreferredValues(_bodyText.text, PanelWidth - HorizontalPadding * 2f, 0f).y);
		float panelHeight = TopPadding + titleHeight + TextSpacing + bodyHeight + BottomPadding;

		Vector2 anchor = new(_lastHudX, _lastHudY);
		_panelRect.anchorMin = anchor;
		_panelRect.anchorMax = anchor;
		_panelRect.pivot = anchor;
		_panelRect.anchoredPosition = Vector2.zero;
		_panelRect.sizeDelta = new Vector2(PanelWidth, panelHeight);
		_panelRect.localScale = Vector3.one;

		RectTransform titleRect = _titleText.rectTransform;
		titleRect.anchorMin = new Vector2(0f, 1f);
		titleRect.anchorMax = new Vector2(1f, 1f);
		titleRect.pivot = new Vector2(0.5f, 1f);
		titleRect.anchoredPosition = new Vector2(0f, -TopPadding);
		titleRect.sizeDelta = new Vector2(-HorizontalPadding * 2f, titleHeight);

		RectTransform bodyRect = _bodyText.rectTransform;
		bodyRect.anchorMin = new Vector2(0f, 1f);
		bodyRect.anchorMax = new Vector2(1f, 1f);
		bodyRect.pivot = new Vector2(0.5f, 1f);
		bodyRect.anchoredPosition = new Vector2(0f, -TopPadding - titleHeight - TextSpacing);
		bodyRect.sizeDelta = new Vector2(-HorizontalPadding * 2f, bodyHeight);
	}

	private static void Hide()
	{
		if (_panel && _panel.activeSelf) _panel.SetActive(false);
	}
}
