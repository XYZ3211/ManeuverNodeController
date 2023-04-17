using SpaceWarp.API.UI;
using UnityEngine;

namespace ManeuverNodeController;

public static class Styles
{
    public static int WindowWidth = 290;
    public static int WindowHeight = 1440;
    public static int WindowWidthStageOAB = 645;
    public static int WindowWidthSettingsOAB = 300;

    public static GUISkin SpaceWarpUISkin;
    public static GUIStyle MainWindowStyle;
    public static GUIStyle PopoutWindowStyle;
    public static GUIStyle EditWindowStyle;
    public static GUIStyle StageOABWindowStyle;
    public static GUIStyle CelestialSelectionStyle;
    public static GUIStyle SettingsOabStyle;
    public static GUIStyle PopoutBtnStyle;
    public static GUIStyle SectionToggleStyle;
    public static GUIStyle NameLabelStyle;
    public static GUIStyle ValueLabelStyle;
    public static GUIStyle BlueLabelStyle;
    public static GUIStyle UnitLabelStyle;
    public static GUIStyle UnitLabelStyleStageOAB;
    public static GUIStyle NormalLabelStyle;
    public static GUIStyle TitleLabelStyle;
    public static GUIStyle NormalCenteredLabelStyle;
    public static GUIStyle WindowSelectionTextFieldStyle;
    public static GUIStyle WindowSelectionAbbrevitionTextFieldStyle;
    public static GUIStyle CloseBtnStyle;
    public static GUIStyle SettingsBtnStyle;
    public static GUIStyle CloseBtnStageOABStyle;
    public static GUIStyle NormalBtnStyle;
    public static GUIStyle CelestialBodyBtnStyle;
    public static GUIStyle CelestialSelectionBtnStyle;
    public static GUIStyle OneCharacterBtnStyle;
    public static GUIStyle TableHeaderLabelStyle;
    public static GUIStyle TableHeaderCenteredLabelStyle;

    public static string UnitColorHex { get => ColorUtility.ToHtmlStringRGBA(UnitLabelStyle.normal.textColor); }

    public static int SpacingAfterHeader = -12;
    public static int SpacingAfterEntry = -12;
    public static int SpacingAfterSection = 5;
    public static float SpacingBelowPopout = 10;

    public static float PoppedOutX = Screen.width * 0.6f;
    public static float PoppedOutY = Screen.height * 0.2f;
    public static float MainGuiX = Screen.width * 0.8f;
    public static float MainGuiY = Screen.height * 0.2f;

    public static Rect CloseBtnRect = new Rect(Styles.WindowWidth - 23, 6, 16, 16);
    public static Rect CloseBtnStagesOABRect = new Rect(Styles.WindowWidthStageOAB - 23, 6, 16, 16);
    public static Rect CloseBtnSettingsOABRect = new Rect(Styles.WindowWidthSettingsOAB - 23, 6, 16, 16);
    public static Rect SettingsOABRect = new Rect(Styles.WindowWidthStageOAB - 50, 6, 16, 16);
    public static Rect EditWindowRect = new Rect(Screen.width * 0.5f - Styles.WindowWidth / 2, Screen.height * 0.2f, Styles.WindowWidth, 0);

    public static void InitializeStyles()
    {
        SpaceWarpUISkin = Skins.ConsoleSkin;

        MainWindowStyle = new GUIStyle(SpaceWarpUISkin.window)
        {
            padding = new RectOffset(8, 8, 20, 8),
            contentOffset = new Vector2(0, -22),
            fixedWidth = WindowWidth
        };

        PopoutWindowStyle = new GUIStyle(MainWindowStyle)
        {
            padding = new RectOffset(MainWindowStyle.padding.left, MainWindowStyle.padding.right, 0, MainWindowStyle.padding.bottom - 5),
            fixedWidth = WindowWidth
        };

        EditWindowStyle = new GUIStyle(PopoutWindowStyle)
        {
            padding = new RectOffset(8, 8, 30, 8)
        };

        StageOABWindowStyle = new GUIStyle(SpaceWarpUISkin.window)
        {
            padding = new RectOffset(8, 8, 0, 8),
            contentOffset = new Vector2(0, -22),
            fixedWidth = WindowWidthStageOAB
        };

        CelestialSelectionStyle = new GUIStyle(SpaceWarpUISkin.window)
        {
            padding = new RectOffset(8, 8, 0, 8),
            contentOffset = new Vector2(0, -22)
        };

        SettingsOabStyle = new GUIStyle(SpaceWarpUISkin.window)
        {
            padding = new RectOffset(8, 8, 0, 16),
            contentOffset = new Vector2(0, -22),
            fixedWidth = WindowWidthSettingsOAB
        };

        PopoutBtnStyle = new GUIStyle(SpaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            contentOffset = new Vector2(0, 2),
            fixedHeight = 15,
            fixedWidth = 15,
            fontSize = 28,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        SectionToggleStyle = new GUIStyle(SpaceWarpUISkin.toggle)
        {
            padding = new RectOffset(0, 18, -5, 0),
            contentOffset= new Vector2(17, 8)
        };

        NameLabelStyle = new GUIStyle(SpaceWarpUISkin.label);
        NameLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);

        ValueLabelStyle = new GUIStyle(SpaceWarpUISkin.label)
        {
            alignment = TextAnchor.MiddleRight
        };
        ValueLabelStyle.normal.textColor = new Color(.6f, .7f, 1, 1);

        UnitLabelStyle = new GUIStyle(SpaceWarpUISkin.label)
        {
            fixedWidth = 24,
            alignment = TextAnchor.MiddleLeft
        };
        UnitLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);

        UnitLabelStyleStageOAB = new GUIStyle(SpaceWarpUISkin.label)
        {
            alignment = TextAnchor.MiddleRight
        };
        UnitLabelStyleStageOAB.normal.textColor = new Color(.7f, .75f, .75f, 1);

        NormalLabelStyle = new GUIStyle(SpaceWarpUISkin.label)
        {
            fixedWidth = 120
        };

        TitleLabelStyle = new GUIStyle(SpaceWarpUISkin.label)
        {
            fontSize = 18,
            fixedWidth = 100,
            fixedHeight = 50,
            contentOffset = new Vector2(0, -20),
        };

        NormalCenteredLabelStyle = new GUIStyle(SpaceWarpUISkin.label)
        {
            fixedWidth = 80,
            alignment = TextAnchor.MiddleCenter
        };

        BlueLabelStyle = new GUIStyle(SpaceWarpUISkin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        BlueLabelStyle.normal.textColor = new Color(.6f, .7f, 1, 1);

        WindowSelectionTextFieldStyle = new GUIStyle(SpaceWarpUISkin.textField)
        {
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = 80
        };

        WindowSelectionAbbrevitionTextFieldStyle = new GUIStyle(SpaceWarpUISkin.textField)
        {
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = 40
        };

        CloseBtnStyle = new GUIStyle(SpaceWarpUISkin.button)
        {
            fontSize = 8
        };

        SettingsBtnStyle = new GUIStyle(SpaceWarpUISkin.button)
        {
            fontSize = 24
        };

        NormalBtnStyle = new GUIStyle(SpaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter
        };

        CelestialBodyBtnStyle = new GUIStyle(SpaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = 80,
            fixedHeight = 20
        };

        CelestialSelectionBtnStyle = new GUIStyle(SpaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fixedWidth = 120
        };

        OneCharacterBtnStyle = new GUIStyle(SpaceWarpUISkin.button)
        {
            fixedWidth = 20,
            alignment = TextAnchor.MiddleCenter
        };

        TableHeaderLabelStyle = new GUIStyle(NameLabelStyle)
        {
            alignment = TextAnchor.MiddleRight
        };
        TableHeaderCenteredLabelStyle = new GUIStyle(NameLabelStyle)
        {
            alignment = TextAnchor.MiddleCenter
        };
    }

    /// <summary>
    /// Draws a white horizontal line accross the container it's put in
    /// </summary>
    /// <param name="height">Height/thickness of the line</param>
    public static void DrawHorizontalLine(float height)
    {
        Texture2D horizontalLineTexture = new Texture2D(1, 1);
        horizontalLineTexture.SetPixel(0, 0, Color.white);
        horizontalLineTexture.Apply();
        GUI.DrawTexture(GUILayoutUtility.GetRect(Screen.width, height), horizontalLineTexture);
    }

    /// <summary>
    /// Draws a white horizontal line accross the container it's put in with height of 1 px
    /// </summary>
    public static void DrawHorizontalLine() { Styles.DrawHorizontalLine(1); }

    internal static void SetStylesForOldSpaceWarpSkin()
    {
        SectionToggleStyle = new GUIStyle(SpaceWarpUISkin.toggle)
        {
            margin = new RectOffset(0, 30, 0, 5)
        };
    }
}
