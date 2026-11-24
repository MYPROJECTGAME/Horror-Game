using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DualPlatformHUDManager : MonoBehaviour
{
    public enum PlatformMode { Auto, PC, Mobile }

    [Header("Platform Mode")]
    [Tooltip("Auto: Uses Mobile for Android/iOS, PC for Standalone/Editor. PC: Forces PC controls and shortcuts. Mobile: Forces Mobile controls with joystick.")]
    public PlatformMode platformMode = PlatformMode.Auto;

    private GameControll m_gameControll;
    private PlayerController m_player;

    private GameObject m_joystickMovement;
    private GameObject m_turnAndLookTouchpad;
    private GameObject m_hudCanvas;

    private GameObject m_wasdMoveGuide;
    private List<GameObject> m_shortcutBadges = new List<GameObject>();

    private Font m_gameFont;

    public bool IsMobile()
    {
        if (platformMode == PlatformMode.Mobile) return true;
        if (platformMode == PlatformMode.PC) return false;
        return Application.isMobilePlatform;
    }

    private void Awake()
    {
        // Maximize frame rate for ultra-smooth gameplay on both platforms
        if (Application.isMobilePlatform)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
        else
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 120;
        }

        m_gameControll = GetComponent<GameControll>();
        if (m_gameControll == null) m_gameControll = FindObjectOfType<GameControll>();

        // Load game font if available
        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        for (int i = 0; i < fonts.Length; i++)
        {
            if (fonts[i].name.Contains("Impact_Label") || fonts[i].name.Contains("Arial"))
            {
                m_gameFont = fonts[i];
                break;
            }
        }
    }

    private IEnumerator Start()
    {
        // Wait a frame to ensure all HUD canvases are instantiated
        yield return null;

        m_player = FindObjectOfType<PlayerController>();

        SetupControls();
    }

    public void SetupControls()
    {
        bool mobile = IsMobile();

        if (m_gameControll != null)
        {
            m_gameControll.m_mobileTouchInput = mobile;
        }

        // Find HUD elements in the active Canvas
        FindHUDElements();

        if (mobile)
        {
            // === MOBILE MODE ===
            if (m_joystickMovement != null)
            {
                m_joystickMovement.SetActive(true);
            }

            if (m_turnAndLookTouchpad != null)
            {
                m_turnAndLookTouchpad.SetActive(true);
            }

            // Hide all PC shortcut badges
            SetShortcutsVisible(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // === PC MODE ===
            // Hide the walking joystick completely on PC
            if (m_joystickMovement != null)
            {
                m_joystickMovement.SetActive(false);
            }

            // Hide the touch look touchpad on PC (mouse is used directly)
            if (m_turnAndLookTouchpad != null)
            {
                m_turnAndLookTouchpad.SetActive(false);
            }

            // Create and show large, visible shortcut key indicators
            CreateKeyboardShortcuts();
            SetShortcutsVisible(true);

            // Lock cursor for FPS gameplay
            LockCursor(true);
        }
    }

    private void FindHUDElements()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        for (int c = 0; c < canvases.Length; c++)
        {
            if (m_joystickMovement == null)
            {
                Transform jm = FindDeepChild(canvases[c].transform, "JoystickMovement");
                if (jm != null) m_joystickMovement = jm.gameObject;
            }

            if (m_turnAndLookTouchpad == null)
            {
                Transform tp = FindDeepChild(canvases[c].transform, "TurnAndLookTouchpad");
                if (tp != null) m_turnAndLookTouchpad = tp.gameObject;
            }

            if (m_hudCanvas == null && canvases[c].name.Contains("Canvas"))
            {
                m_hudCanvas = canvases[c].gameObject;
            }
        }

        if (m_hudCanvas == null && m_gameControll != null && m_gameControll.gameControllPanel != null)
        {
            m_hudCanvas = m_gameControll.gameControllPanel;
        }
    }

    private void CreateKeyboardShortcuts()
    {
        // Clear previous if any
        for (int i = 0; i < m_shortcutBadges.Count; i++)
        {
            if (m_shortcutBadges[i] != null) Destroy(m_shortcutBadges[i]);
        }
        m_shortcutBadges.Clear();

        if (m_hudCanvas == null) return;

        // 1. Movement Guide [W][A][S][D] in bottom-left where joystick used to be
        if (m_wasdMoveGuide == null)
        {
            GameObject wasd = new GameObject("WASD_Shortcut_Guide", typeof(RectTransform));
            wasd.transform.SetParent(m_hudCanvas.transform, false);
            RectTransform rt = wasd.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(180f, 150f);
            rt.sizeDelta = new Vector2(250f, 90f);

            Text text = wasd.AddComponent<Text>();
            if (m_gameFont != null) text.font = m_gameFont;
            text.text = "[ W A S D ]\n<color=#FFFFFF>MOVE</color>";
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.1f, 1f); // Vibrant Gold
            text.raycastTarget = false;

            Outline outline = wasd.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            m_wasdMoveGuide = wasd;
            m_shortcutBadges.Add(wasd);
        }

        // 2. Sprint / Run Button Shortcut [SHIFT]
        Transform runBtn = FindDeepChild(m_hudCanvas.transform, "RunButtonBG");
        if (runBtn != null)
        {
            AddBadgeToElement(runBtn, "[ SHIFT ]", new Vector2(-110f, 0f), 24);
        }

        // 3. Crouch Button Shortcut [C]
        Transform crouchBtn = FindDeepChild(m_hudCanvas.transform, "CrouchButton");
        if (crouchBtn != null)
        {
            AddBadgeToElement(crouchBtn, "[ C ]", new Vector2(90f, 0f), 28);
        }

        // 4. Eye Button Shortcut [E]
        Transform eyeBtn = FindDeepChild(m_hudCanvas.transform, "EyeButton");
        if (eyeBtn != null)
        {
            AddBadgeToElement(eyeBtn, "[ E ]", new Vector2(90f, 0f), 28);
        }

        // 5. Settings / Menu Button Shortcut [ESC]
        Transform menuBtn = FindDeepChild(m_hudCanvas.transform, "MenuButton");
        if (menuBtn != null)
        {
            AddBadgeToElement(menuBtn, "[ ESC ]", new Vector2(90f, 0f), 24);
        }

        // 6. Interact Button Shortcut [F]
        Transform interactBtn = FindDeepChild(m_hudCanvas.transform, "InteractButton");
        if (interactBtn != null)
        {
            AddBadgeToElement(interactBtn, "[ F ]", new Vector2(0f, -50f), 26);
        }
    }

    private void AddBadgeToElement(Transform parent, string shortcutText, Vector2 localPos, int fontSize)
    {
        GameObject badge = new GameObject("Shortcut_" + shortcutText, typeof(RectTransform));
        badge.transform.SetParent(parent, false);

        RectTransform rt = badge.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(150f, 50f);

        Text text = badge.AddComponent<Text>();
        if (m_gameFont != null) text.font = m_gameFont;
        text.text = shortcutText;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.9f, 0.2f, 1f); // Vibrant Gold Yellow
        text.raycastTarget = false;

        Outline outline = badge.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        m_shortcutBadges.Add(badge);
    }

    private void SetShortcutsVisible(bool visible)
    {
        for (int i = 0; i < m_shortcutBadges.Count; i++)
        {
            if (m_shortcutBadges[i] != null)
            {
                m_shortcutBadges[i].SetActive(visible);
            }
        }
    }

    private void Update()
    {
        if (IsMobile()) return;

        // Handle PC Cursor Lock
        UpdateCursorLockState();
    }

    private void UpdateCursorLockState()
    {
        bool isGamePaused = (m_gameControll != null && (m_gameControll.pause || (m_gameControll.pausePanel != null && m_gameControll.pausePanel.activeSelf)));
        bool isPlayerLocked = (m_player != null && (m_player.locked || m_player.lockedByDying));

        if (isGamePaused || isPlayerLocked)
        {
            LockCursor(false);
        }
        else
        {
            // If gameplay is active and player clicks into the game window, lock cursor
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    LockCursor(true);
                }
            }
        }
    }

    private void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
