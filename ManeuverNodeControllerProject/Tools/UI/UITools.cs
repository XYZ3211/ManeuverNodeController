using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ManeuverNodeController.UI;

public class FoldOut
{
    public delegate void onChapterUI();


    public class Chapter
    {
        public string Title;
        public onChapterUI chapterUI;
        public bool opened = false;

        public Chapter(string Title, onChapterUI chapterUI)
        {
            this.Title = Title;
            this.chapterUI = chapterUI;
        }
    }

    public List<Chapter> chapters = new List<Chapter>();
    public bool singleChapter = false;

    public void OnGui()
    {
        GUILayout.BeginVertical();

        for (int i = 0; i < chapters.Count; i++)
        {
            Chapter chapter = chapters[i];
            var style = chapter.opened ? MNCStyles.foldout_open : MNCStyles.foldout_close;
            if (GUILayout.Button(chapter.Title, style))
            {
                chapter.opened = !chapter.opened;


                if (chapter.opened && singleChapter)
                {
                    for (int j = 0; j < chapters.Count; j++)
                    {
                        if (i != j)
                            chapters[j].opened = false;
                    }
                }
            }

            if (chapter.opened)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();

                chapter.chapterUI();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

        }
        GUILayout.EndVertical();
    }

    public void addChapter(string Title, onChapterUI chapterUI)
    {
        chapters.Add(new Chapter(Title, chapterUI));
    }


    public int Count
    {
        get { return chapters.Count; }
    }

}


public class TopButtons
{
    static Rect position = Rect.zero;
    const int space = 25;

    /// <summary>
    /// Must be called before any Button call
    /// </summary>
    /// <param name="widthWindow"></param>
    static public void Init(float widthWindow)
    {
        position = new Rect(widthWindow - 5, 4, 23, 23);
    }

    static public bool Button(string txt)
    {
        position.x -= space;
        return GUI.Button(position, txt, MNCStyles.icon_button);
    }
    static public bool IconButton(Texture2D icon)
    {
        position.x -= space;
        return GUI.Button(position, icon, MNCStyles.icon_button);
    }

    static public bool Toggle(bool value, string txt)
    {
        position.x -= space;
        return GUI.Toggle(position, value, txt, MNCStyles.icon_button);
    }

    static public bool Toggle(bool value, Texture2D icon)
    {
        position.x -= space;
        return GUI.Toggle(position, value, icon, MNCStyles.icon_button);
    }
}

/// <summary>
/// A set of simple tools for UI
/// </summary>
/// TODO : remove static, make it singleton
public class UI_Tools
{

    public static int GetEnumValue<T>(T inputEnum) where T: struct, IConvertible
    {
        Type t = typeof(T);
        if (!t.IsEnum)
        {
            throw new ArgumentException("Input type must be an enum.");
        }

        return inputEnum.ToInt32(CultureInfo.InvariantCulture.NumberFormat);
    }

    public static TEnum EnumGrid<TEnum>(string label, TEnum value, string[] labels) where TEnum : struct, Enum
    {
        int int_value = value.GetHashCode();
        UI_Tools.Label(label);
        int result = GUILayout.SelectionGrid(int_value, labels, labels.Length);

        return (TEnum) Enum.ToObject(typeof(TEnum), result);
    }

    public static bool Toggle(bool is_on, string txt, string tooltip = null)
    {
        if (tooltip != null)
            return GUILayout.Toggle(is_on, new GUIContent(txt, tooltip), MNCStyles.toggle);
        else
            return GUILayout.Toggle(is_on, txt, MNCStyles.toggle);
    }

    public static bool ToggleButton(bool is_on, string txt_run, string txt_stop)
    {
        int height_bt = 30;
        int min_width_bt = 150;

        var txt = is_on ? txt_stop : txt_run;
        // GUILayout.BeginHorizontal();
        // GUILayout.FlexibleSpace();
        is_on = GUILayout.Toggle(is_on, txt, MNCStyles.big_button, GUILayout.Height(height_bt), GUILayout.MinWidth(min_width_bt));
        // GUILayout.FlexibleSpace();
        // GUILayout.EndHorizontal();
        return is_on;
    }

    public static bool Button(string txt)
    {
        return GUILayout.Button(txt, MNCStyles.big_button);
    }

    public static bool SmallButton(string txt)
    {
        return GUILayout.Button(txt, MNCStyles.small_button);
    }
     
    public static bool BigButton(string txt)
    {
        return GUILayout.Button(txt, MNCStyles.bigicon_button);
    }

    public static bool ListButton(string txt)
    {
        return GUILayout.Button(txt, MNCStyles.button, GUILayout.ExpandWidth(true));
    }

    public static bool miniToggle(bool value, string txt, string tooltip)
    {
        return GUILayout.Toggle(value, new GUIContent(txt, tooltip), MNCStyles.small_button, GUILayout.Height(20));
    }

    public static bool miniButton(string txt, string tooltip = "")
    {
        return GUILayout.Button(new GUIContent(txt, tooltip), MNCStyles.small_button, GUILayout.Height(20));
    }

    public static bool ToolTipButton(string tooltip)
    {
        return GUILayout.Button(new GUIContent("?", tooltip), MNCStyles.small_button, GUILayout.Width(16), GUILayout.Height(20));
    }

    static public bool BigIconButton(Texture2D icon)
    {
        return GUILayout.Button(icon, MNCStyles.bigicon_button);
    }


    public static void Title(string txt)
    {
        GUILayout.Label($"<b>{txt}</b>", MNCStyles.title);
    }

    public static void Label(string txt)
    {
        GUILayout.Label(txt, MNCStyles.label);
    }

    public static void OK(string txt)
    {
        GUILayout.Label(txt, MNCStyles.phase_ok);
    }

    public static void Warning(string txt)
    {
        GUILayout.Label(txt, MNCStyles.phase_warning);
    }

    public static void Error(string txt)
    {
        GUILayout.Label(txt, MNCStyles.phase_error);
    }



    public static void Console(string txt)
    {
        GUILayout.Label(txt, MNCStyles.console_text);
    }

    public static void Mid(string txt)
    {
        GUILayout.Label(txt, MNCStyles.mid_text);
    }



    public static int IntSlider(string txt, int value, int min, int max, string postfix = "", string tooltip = "")
    {
        string content = txt + $" : {value} " + postfix;

        GUILayout.Label(content, MNCStyles.slider_text);
        GUILayout.BeginHorizontal();
        value = (int) GUILayout.HorizontalSlider((int) value, min, max, MNCStyles.slider_line, MNCStyles.slider_node);
        if (value < min) value = min;
        if (value > max) value = max;

        if (!string.IsNullOrEmpty(tooltip))
        {
            UI_Tools.ToolTipButton(tooltip);
        }
        GUILayout.EndHorizontal();
        return value;
    }

    public static float HeadingSlider(string txt, float value, string tooltip = "")
    {
        string value_str = value.ToString("N"+1);
        string content =  $"{txt} : {value_str} °";
        GUILayout.Label(content, MNCStyles.slider_text);
        GUILayout.BeginHorizontal();
        value = GUILayout.HorizontalSlider( value, -180, 180, MNCStyles.slider_line, MNCStyles.slider_node);

        int step = 45;
        float precision = 5;
        int index = Mathf.RoundToInt( value / step);
        float rounded = index * step;

        float delta = Mathf.Abs( rounded - value);
        if (delta < precision)
            value = rounded;

        index = index + 4;
        string[] directions = {"S", "SW", "W", "NW", "N", "NE", "E", "SE", "S", "??" };
        GUILayout.Label(directions[index], GUILayout.Width(15));
        if (!string.IsNullOrEmpty(tooltip))
        {
            UI_Tools.ToolTipButton(tooltip);
        }

        GUILayout.EndHorizontal();
        // GUILayout.Label($"rounded {rounded} index {index}, delta {delta}");
        return value;

    }

    public static void Separator()
    {
        GUILayout.Box("", MNCStyles.separator);
    }

    public static void ProgressBar(double value, double min, double max)
    {
        ProgressBar((float) value, (float) min, (float) max);
    }

    public static void ProgressBar(float value, float min, float max)
    {
        var ratio = Mathf.InverseLerp(min, max, value);

        GUILayout.Box("", MNCStyles.progress_bar_empty, GUILayout.ExpandWidth(true));
        var lastrect = GUILayoutUtility.GetLastRect();

        lastrect.width = Mathf.Clamp(lastrect.width * ratio, 4, 10000000);
        GUI.Box(lastrect, "", MNCStyles.progress_bar_full);
    }




    public static float FloatSlider(float value, float min, float max, string tooltip = "")
    {
        // simple float slider
        GUILayout.BeginHorizontal();
        value = GUILayout.HorizontalSlider( value, min, max, MNCStyles.slider_line, MNCStyles.slider_node);

        if (!string.IsNullOrEmpty(tooltip))
        {
            UI_Tools.ToolTipButton(tooltip);
        }
        GUILayout.EndHorizontal();

        value = Mathf.Clamp(value, min, max);
        return value;
    }

    public static float FloatSliderTxt(string txt, float value, float min, float max, string postfix = "", string tooltip = "", int precision = 2)
    {
        // simple float slider with a printed value
        string value_str = value.ToString("N"+precision);

        string content =  $"{txt} : {value_str} {postfix}";

        GUILayout.Label(content, MNCStyles.slider_text);
        value = FloatSlider(value, min, max, tooltip);
        return value;
    }

    public static void Right_Left_Text(string right_txt, string left_txt)
    {
        // text aligned to right and left with a space in between
        GUILayout.BeginHorizontal();
        UI_Tools.Mid(right_txt);
        GUILayout.FlexibleSpace();
        UI_Tools.Mid(left_txt);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    public static Vector2 BeginScrollView(Vector2 scrollPos, int height)
    {
        return GUILayout.BeginScrollView(scrollPos, false, true,
            GUILayout.MinWidth(250),
            GUILayout.Height(height));
    }

}

