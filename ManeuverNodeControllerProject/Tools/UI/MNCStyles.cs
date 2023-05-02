
using UnityEngine;
using ManeuverNodeController.Tools;
using SpaceWarp.API.UI;
using UnityEngine.UIElements;
using System.Reflection.Emit;

namespace ManeuverNodeController.UI;

public class MNCStyles
{
    private static bool guiLoaded = false;


    public static GUISkin skin;

    public static void BuildStyles()
    {
        if (guiLoaded)
            return;

        skin = CopySkin(Skins.ConsoleSkin);

        BuildFrames();
        BuildSliders();
        BuildTabs();
        BuildButtons();
        BuildFoldout();
        BuildToggle();
        BuildProgressBar();
        BuildIcons();
        BuildLabels();

        guiLoaded = true;
    }

    public static GUIStyle error, warning, label, status, mid_text, console_text, phase_ok, phase_warning, phase_error;
    public static GUIStyle icons_label, title, slider_text, text_input, name_label, value_label, unit_label;
    public static GUIStyle progradeStyle, normalStyle, radialStyle, name_label_r, value_label_l;

    static void BuildLabels()
    {

        icons_label = new GUIStyle(GUI.skin.GetStyle("Label"));
        icons_label.border = new RectOffset(0, 0, 0, 0);
        icons_label.padding = new RectOffset(0, 0, 0, 0);
        icons_label.margin = new RectOffset(0, 0, 0, 0);
        icons_label.overflow = new RectOffset(0, 0, 0, 0);

        error = new GUIStyle(GUI.skin.GetStyle("Label"));
        error.alignment = TextAnchor.MiddleLeft;
        error.normal.textColor = Color.red;

        warning = new GUIStyle(GUI.skin.GetStyle("Label"));
        warning.alignment = TextAnchor.MiddleLeft;
        warning.normal.textColor = Color.yellow;
        //labelColor = GUI.skin.GetStyle("Label").normal.textColor;

        phase_ok = new GUIStyle(GUI.skin.GetStyle("Label"));
        phase_ok.alignment = TextAnchor.MiddleLeft;
        phase_ok.normal.textColor = ColorTools.parseColor("#00BC16");
        // phase_ok.fontSize = 20;

        phase_warning = new GUIStyle(GUI.skin.GetStyle("Label"));
        phase_warning.alignment = TextAnchor.MiddleLeft;
        phase_warning.normal.textColor = ColorTools.parseColor("#BC9200");
        // phase_warning.fontSize = 20;

        phase_error = new GUIStyle(GUI.skin.GetStyle("Label"));
        phase_error.alignment = TextAnchor.MiddleLeft;
        phase_error.normal.textColor = ColorTools.parseColor("#B30F0F");
        // phase_error.fontSize = 20;

        console_text = new GUIStyle(GUI.skin.GetStyle("Label"));
        console_text.alignment = TextAnchor.MiddleLeft;
        console_text.normal.textColor = ColorTools.parseColor("#B6B8FA");
        // console_text.fontSize = 15;
        console_text.padding = new RectOffset(0, 0, 0, 0);
        console_text.margin = new RectOffset(0, 0, 0, 0);

        slider_text = new GUIStyle(console_text);
        slider_text.normal.textColor = ColorTools.parseColor("#C0C1E2");

        mid_text = new GUIStyle(slider_text);

        slider_text.margin = new RectOffset(5, 0, 0, 0);
        slider_text.contentOffset = new Vector2(8, 5);

        label = new GUIStyle(GUI.skin.GetStyle("Label"));
        label.alignment = TextAnchor.MiddleLeft;
        // label.fontSize = 17;
        // label.margin = new RectOffset(0, 0, 0, 0);
        // label.padding = new RectOffset(0, 0, 0, 0);

        status = new GUIStyle(GUI.skin.GetStyle("Label"));
        status.alignment = TextAnchor.MiddleLeft;
        // status.fontSize = 17;
        // status.margin = new RectOffset(0, 0, 0, 0);
        // status.padding = new RectOffset(0, 0, 0, 0);

        title = new GUIStyle();
        title.normal.textColor = ColorTools.parseColor("#C0C1E2");
        // title.fontSize = 19;

        text_input = new GUIStyle(GUI.skin.GetStyle("textField")) // was (_spaceWarpUISkin.textField)
        {
            alignment = TextAnchor.LowerCenter,
            padding = new RectOffset(10, 10, 0, 0),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 18,
            fixedWidth = 100, //(float)(windowWidth / 4),
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        name_label = new GUIStyle(GUI.skin.GetStyle("Label")); // was (_spaceWarpUISkin.label);
        name_label.alignment = TextAnchor.MiddleLeft;
        name_label.normal.textColor = new Color(.7f, .75f, .75f, 1);

        name_label_r = new GUIStyle(name_label);
        name_label_r.alignment = TextAnchor.MiddleRight;

        value_label = new GUIStyle(GUI.skin.GetStyle("Label")); // was (_spaceWarpUISkin.label)
        value_label.alignment = TextAnchor.MiddleRight;
        value_label.normal.textColor = new Color(.6f, .7f, 1, 1);

        value_label_l = new GUIStyle(value_label);
        value_label_l.alignment = TextAnchor.MiddleLeft;

        unit_label = new GUIStyle(value_label)
        {
            fixedWidth = 24,
            alignment = TextAnchor.MiddleLeft
        };
        unit_label.normal.textColor = new Color(.7f, .75f, .75f, 1);

        progradeStyle = new GUIStyle(GUI.skin.GetStyle("Label")); // was( _spaceWarpUISkin.label);
        progradeStyle.normal.textColor = Color.green;
        progradeStyle.fixedHeight = 24;
        normalStyle = new GUIStyle(GUI.skin.GetStyle("Label")); // was( _spaceWarpUISkin.label);
        normalStyle.normal.textColor = Color.magenta;
        normalStyle.fixedHeight = 24;
        radialStyle = new GUIStyle(GUI.skin.GetStyle("Label")); // was( _spaceWarpUISkin.label);
        radialStyle.normal.textColor = Color.cyan;
        radialStyle.fixedHeight = 24;
    }

    public static GUIStyle separator;
    public static GUIStyle horizontalDivider;
    public static int spacingAfterEntry = -12;
    static void BuildFrames()
    {
        // Define the GUIStyle for the window
        GUIStyle window = new GUIStyle(skin.window);

        window.border = new RectOffset(25, 25, 35, 25);
        window.margin = new RectOffset(0, 0, 0, 0);
        window.padding = new RectOffset(20, 13, 44, 17);
        window.overflow = new RectOffset(0, 0, 0, 0);

        // window.fontSize = 20;
        window.contentOffset = new Vector2(31, -40);

        // Set the background color of the window
        window.normal.background = AssetsLoader.loadIcon("window");
        window.normal.textColor = Color.black;
        setAllFromNormal(window);
        window.alignment = TextAnchor.UpperLeft;
        window.stretchWidth = true;
        // window.fontSize = 20;
        window.contentOffset = new Vector2(31, -40);
        skin.window = window;

        // Define the GUIStyle for the box
        GUIStyle box = new GUIStyle(window);
        box.normal.background = AssetsLoader.loadIcon("Box");
        setAllFromNormal(box);
        box.border = new RectOffset(10, 10, 10, 10);
        box.margin = new RectOffset(0, 0, 0, 0);
        box.padding = new RectOffset(10, 10, 10, 10);
        box.overflow = new RectOffset(0, 0, 0, 0);
        skin.box = box;
        skin.scrollView = box;


        // define the V scrollbar
        GUIStyle verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar);
        
        verticalScrollbar.normal.background = AssetsLoader.loadIcon("VerticalScroll");
        setAllFromNormal(verticalScrollbar);
        verticalScrollbar.border = new RectOffset(5, 5, 5, 5);
        verticalScrollbar.fixedWidth = 10;
   
        skin.verticalScrollbar = verticalScrollbar;

        GUIStyle verticalScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb);

        verticalScrollbarThumb.normal.background = AssetsLoader.loadIcon("VerticalScroll_thumb");
        setAllFromNormal(verticalScrollbarThumb);
        verticalScrollbarThumb.border = new RectOffset(5, 5, 5, 5);
        verticalScrollbarThumb.fixedWidth = 10;

        skin.verticalScrollbarThumb = verticalScrollbarThumb;

        // separator
        separator = new GUIStyle(GUI.skin.box);
        separator.normal.background = AssetsLoader.loadIcon("line");
        separator.border = new RectOffset(2, 2, 0, 0);
        separator.margin = new RectOffset(10, 10, 5, 5);
        separator.fixedHeight = 3;
        setAllFromNormal(separator);

        horizontalDivider = new GUIStyle();
        horizontalDivider.fixedHeight = 2;
        horizontalDivider.margin = new RectOffset(0, 0, 4, 4);
    }

    public static GUIStyle slider_line, slider_node;

    static void BuildSliders()
    {
        slider_line = new GUIStyle(GUI.skin.horizontalSlider);
        slider_line.normal.background = AssetsLoader.loadIcon("Slider");
        setAllFromNormal(slider_line);
        slider_line.border = new RectOffset(5, 5, 0, 0);

        slider_line.border = new RectOffset(12, 14, 0, 0);
        slider_line.fixedWidth = 0;
        slider_line.fixedHeight = 21;
        slider_line.margin = new RectOffset(0, 0, 2, 5);

        slider_node = new GUIStyle(GUI.skin.horizontalSliderThumb);
        slider_node.normal.background = AssetsLoader.loadIcon("SliderNode");
        setAllFromNormal(slider_node);
        slider_node.border = new RectOffset(0, 0, 0, 0);
        slider_node.fixedWidth = 21;
        slider_node.fixedHeight = 21;

    }

    // icons
    public static Texture2D gear, icon, mnc_icon, cross;

    static void BuildIcons()
    {
        // icons
        gear = AssetsLoader.loadIcon("gear");
        icon = AssetsLoader.loadIcon("icon");
        mnc_icon = AssetsLoader.loadIcon("icon_white_50"); //  mnc_icon_bw_50
        cross = AssetsLoader.loadIcon("Cross");
    }

    public static GUIStyle progress_bar_empty, progress_bar_full;

    static void BuildProgressBar()
    {
        // progress bar
        progress_bar_empty = new GUIStyle(GUI.skin.box);
        progress_bar_empty.normal.background = AssetsLoader.loadIcon("progress_empty");
        progress_bar_empty.border = new RectOffset(2, 2, 2, 2);
        progress_bar_empty.margin = new RectOffset(5, 5, 5, 5);
        progress_bar_empty.fixedHeight = 20;
        setAllFromNormal(progress_bar_empty);

        progress_bar_full = new GUIStyle(progress_bar_empty);
        progress_bar_full.normal.background = AssetsLoader.loadIcon("progress_full");
        setAllFromNormal(progress_bar_empty);
    }


    public static GUIStyle tab_normal, tab_active;
    static void BuildTabs()
    {
        tab_normal = new GUIStyle(button);
        tab_normal.border = new RectOffset(5, 5, 5, 5);
        tab_normal.padding = new RectOffset(4, 3, 4, 3);
        tab_normal.overflow = new RectOffset(0, 0, 0, 0);
        // big_button.fontSize = 20;
        tab_normal.alignment = TextAnchor.MiddleCenter;

        tab_normal.normal.background = AssetsLoader.loadIcon("Tab_Normal");
        setAllFromNormal(tab_normal);

        tab_normal.hover.background = AssetsLoader.loadIcon("Tab_Hover");
        tab_normal.active.background = AssetsLoader.loadIcon("Tab_Active");
        tab_normal.onNormal = tab_normal.active;
        setFromOn(tab_normal);


        tab_active = new GUIStyle(tab_normal);
        tab_active.normal.background = AssetsLoader.loadIcon("Tab_On_normal");
        setAllFromNormal(tab_active);

        tab_active.hover.background = AssetsLoader.loadIcon("Tab_On_hover");
        tab_active.active.background = AssetsLoader.loadIcon("Tab_On_Active");
        tab_active.onNormal = tab_active.active;
        setFromOn(tab_active);
    }

    public static GUIStyle bigicon_button, icon_button, small_button, big_button, button, ctrl_button, small_btn, snap_button;

    static void BuildButtons()
    {
        // button std
        button = new GUIStyle(GUI.skin.GetStyle("Button"));
        button.normal.background = AssetsLoader.loadIcon("BigButton_Normal");
        button.normal.textColor = ColorTools.parseColor("#FFFFFF");
        setAllFromNormal(button);

        button.hover.background = AssetsLoader.loadIcon("BigButton_hover");
        button.active.background = AssetsLoader.loadIcon("BigButton_hover");
        // button.active.background = AssetsLoader.loadIcon("BigButton_on");
        // button.onNormal = button.active;
        // setFromOn(button);

        button.border = new RectOffset(5, 5, 5, 5);
        button.padding = new RectOffset(4, 3, 4, 3);
        button.overflow = new RectOffset(0, 0, 0, 0);
        // button.fontSize = 20;
        button.alignment = TextAnchor.MiddleCenter;
        skin.button = button;

        // Small Button
        small_button = new GUIStyle(GUI.skin.GetStyle("Button"));
        small_button.normal.background = AssetsLoader.loadIcon("Small_Button");
        setAllFromNormal(small_button);
        small_button.hover.background = AssetsLoader.loadIcon("Small_Button_hover");
        small_button.active.background = AssetsLoader.loadIcon("Small_Button_active");
        small_button.onNormal = small_button.active;
        setFromOn(small_button);

        small_button.border = new RectOffset(5, 5, 5, 5);
        small_button.padding = new RectOffset(0, 0, 0, 0);
        small_button.overflow = new RectOffset(0, 0, 0, 0);
        small_button.alignment = TextAnchor.MiddleCenter;

        big_button = new GUIStyle(GUI.skin.GetStyle("Button"));
        big_button.normal.background = AssetsLoader.loadIcon("BigButton_Normal");
        big_button.normal.textColor = ColorTools.parseColor("#FFFFFF");
        setAllFromNormal(big_button);

        big_button.hover.background = AssetsLoader.loadIcon("BigButton_Hover");
        big_button.active.background = AssetsLoader.loadIcon("BigButton_Active");
        big_button.onNormal = big_button.active;
        setFromOn(big_button);

        big_button.border = new RectOffset(5, 5, 5, 5);
        big_button.padding = new RectOffset(4, 3, 4, 3);
        big_button.overflow = new RectOffset(0, 0, 0, 0);
        // big_button.fontSize = 20;
        big_button.alignment = TextAnchor.MiddleCenter;

        // Small Button
        icon_button = new GUIStyle(small_button);
        icon_button.padding = new RectOffset(4, 4, 4, 4);

        bigicon_button = new GUIStyle(icon_button);
        bigicon_button.fixedWidth = 50;
        bigicon_button.fixedHeight = 50;
        bigicon_button.fontStyle = FontStyle.Bold;

        small_btn = new GUIStyle(small_button) // GUI.skin.GetStyle("Button")) // was (_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 0, 3),
            // contentOffset = new Vector2(0, 2),
            fixedHeight = 25, // 16,
            // fixedWidth = 95,
            fontSize = 16,
            clipping = TextClipping.Overflow,
            border = new RectOffset(5, 5, 5, 5),
            // padding = new RectOffset(0, 0, 0, 0),
            overflow = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 10, 0)
        };
        small_btn.normal.background = AssetsLoader.loadIcon("BigButton_Normal");
        setAllFromNormal(small_btn);
        small_btn.hover.background = AssetsLoader.loadIcon("BigButton_Hover");
        small_btn.active.background = AssetsLoader.loadIcon("BigButton_Active");
        small_btn.onNormal = small_btn.active;
        setFromOn(small_btn);


        ctrl_button = new GUIStyle(GUI.skin.GetStyle("Button")); // was (_spaceWarpUISkin.button)
        ctrl_button.fixedHeight = 16;
        ctrl_button.fixedWidth = 32;
        ctrl_button.clipping = TextClipping.Overflow;
        ctrl_button.normal.background = AssetsLoader.loadIcon("BigButton_Normal");
        setAllFromNormal(ctrl_button);
        ctrl_button.hover.background = AssetsLoader.loadIcon("BigButton_Hover");
        ctrl_button.active.background = AssetsLoader.loadIcon("BigButton_Active");
        ctrl_button.onNormal = ctrl_button.active;
        setFromOn(ctrl_button);

        ctrl_button.border = new RectOffset(5, 5, 5, 5);
        ctrl_button.padding = new RectOffset(0, 0, 0, 0);
        ctrl_button.overflow = new RectOffset(0, 0, 0, 0);
        ctrl_button.alignment = TextAnchor.MiddleCenter;

        //{
        //    alignment = TextAnchor.MiddleCenter,
        //    // padding = new RectOffset(0, 0, 0, 3),
        //    // contentOffset = new Vector2(0, 2),
        //    fixedHeight = 16,
        //    fixedWidth = 32,
        //    // fontSize = 16,
        //    clipping = TextClipping.Overflow,
        //    border = new RectOffset(5, 5, 5, 5),
        //    padding = new RectOffset(0, 0, 0, 0),
        //    overflow = new RectOffset(0, 0, 0, 0),
        //    margin = new RectOffset(0, 0, 10, 0)
        //};

        snap_button = new GUIStyle(small_button) // GUI.skin.GetStyle("Button")) // was (_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            // padding = new RectOffset(0, 0, 0, 3),
            // contentOffset = new Vector2(0, 2),
            fixedHeight = 20,
            fixedWidth = 40, // (float)(windowWidth / 8) - 5,
            // fontSize = 16,
            clipping = TextClipping.Overflow,
            border = new RectOffset(5, 5, 5, 5),
            padding = new RectOffset(0, 0, 0, 0),
            overflow = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 10, 0)
        };
        snap_button.normal.background = AssetsLoader.loadIcon("BigButton_Normal");
        setAllFromNormal(snap_button);
        snap_button.hover.background = AssetsLoader.loadIcon("BigButton_Hover");
        snap_button.active.background = AssetsLoader.loadIcon("BigButton_Active");
        snap_button.onNormal = snap_button.active;
        setFromOn(snap_button);
    }
    public static GUIStyle foldout_close, foldout_open;
    
    static void BuildFoldout()
    {

        foldout_close = new GUIStyle(small_button);
        foldout_close.fixedHeight = 30;
        foldout_close.padding = new RectOffset(23, 2, 2, 2);
        foldout_close.border = new RectOffset(23, 7, 27, 3);

        foldout_close.normal.background = AssetsLoader.loadIcon("Chapter_Off_Normal");
        foldout_close.normal.textColor = ColorTools.parseColor("#D4D4D4");
        foldout_close.alignment = TextAnchor.MiddleLeft;
        setAllFromNormal(foldout_close);
        foldout_close.hover.background = AssetsLoader.loadIcon("Chapter_Off_Hover");
        foldout_close.active.background = AssetsLoader.loadIcon("Chapter_Off_Active");

        foldout_open = new GUIStyle(foldout_close);
        foldout_open.normal.background = AssetsLoader.loadIcon("Chapter_On_Normal");
        foldout_open.normal.textColor = ColorTools.parseColor("#8BFF95");
        setAllFromNormal(foldout_open);

        foldout_open.hover.background = AssetsLoader.loadIcon("Chapter_On_Hover");
        foldout_open.active.background = AssetsLoader.loadIcon("Chapter_On_Active");
    }


    public static GUIStyle toggle;
    static void BuildToggle()
    {
        // Toggle Button
        toggle = new GUIStyle(GUI.skin.GetStyle("Button"));
        toggle.normal.background = AssetsLoader.loadIcon("Toggle_Off");
        toggle.normal.textColor = ColorTools.parseColor("#C0C1E2");


        setAllFromNormal(toggle);
        toggle.onNormal.background = AssetsLoader.loadIcon("Toggle_On");
        toggle.onNormal.textColor = ColorTools.parseColor("#C0E2DC");
        setFromOn(toggle);
        toggle.fixedHeight = 32;
        toggle.stretchWidth = false;

        toggle.border = new RectOffset(45, 5, 5, 5);
        toggle.padding = new RectOffset(34, 16, 0, 0);
        toggle.overflow = new RectOffset(0, 0, 0, 2);
    }

    public static void Init()
    {
        if (!guiLoaded)
        {
            BuildStyles();
        }
    }

    /// <summary>
    /// copy all styles from normal state to others
    /// </summary>
    /// <param name="style"></param>
    private static void setAllFromNormal(GUIStyle style)
    {
        style.hover = style.normal;
        style.active = style.normal;
        style.focused = style.normal;
        style.onNormal = style.normal;
        style.onHover = style.normal;
        style.onActive = style.normal;
        style.onFocused = style.normal;
    }

    /// <summary>
    /// copy all styles from onNormal state to on others
    /// </summary>
    /// <param name="style"></param>
    private static void setFromOn(GUIStyle style)
    {
        style.onHover = style.onNormal;
        style.onActive = style.onNormal;
        style.onFocused = style.onNormal;
    }

    /// <summary>
    /// do a full copy of a skin
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    private static GUISkin CopySkin(GUISkin source)
    {
        var copy = new GUISkin();
        copy.font = source.font;
        copy.box = new GUIStyle(source.box);
        copy.label = new GUIStyle(source.label);
        copy.textField = new GUIStyle(source.textField);
        copy.textArea = new GUIStyle(source.textArea);
        copy.button = new GUIStyle(source.button);
        copy.toggle = new GUIStyle(source.toggle);
        copy.window = new GUIStyle(source.window);

        copy.horizontalSlider = new GUIStyle(source.horizontalSlider);
        copy.horizontalSliderThumb = new GUIStyle(source.horizontalSliderThumb);    
        copy.verticalSlider = new GUIStyle(source.verticalSlider);
        copy.verticalSliderThumb = new GUIStyle(source.verticalSliderThumb);

        copy.horizontalScrollbar = new GUIStyle(source.horizontalScrollbar);
        copy.horizontalScrollbarThumb = new GUIStyle(source.horizontalScrollbarThumb);
        copy.horizontalScrollbarLeftButton = new GUIStyle(source.horizontalScrollbarLeftButton);
        copy.horizontalScrollbarRightButton = new GUIStyle(source.horizontalScrollbarRightButton);

        copy.verticalScrollbar = new GUIStyle(source.verticalScrollbar);
        copy.verticalScrollbarThumb = new GUIStyle(source.verticalScrollbarThumb);
        copy.verticalScrollbarUpButton = new GUIStyle(source.verticalScrollbarUpButton);
        copy.verticalScrollbarDownButton = new GUIStyle(source.verticalScrollbarDownButton);

        copy.scrollView = new GUIStyle(source.scrollView);

        return copy;
    }
}

