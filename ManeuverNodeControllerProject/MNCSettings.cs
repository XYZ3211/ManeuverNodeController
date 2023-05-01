using ManeuverNodeController.Tools;


namespace ManeuverNodeController;

public class MNCSettings
{
    public static SettingsFile s_settings_file = null;
    public static string s_settings_path;

    public static void Init(string settings_path)
    {
        s_settings_file = new SettingsFile(settings_path);
    }

    public static int window_x_pos
    {
        get => s_settings_file.GetInt("window_x_pos", 70);
        set { s_settings_file.SetInt("window_x_pos", value); }
    }

    public static int window_y_pos
    {
        get => s_settings_file.GetInt("window_y_pos", 50);
        set { s_settings_file.SetInt("window_y_pos", value); }
    }

    public static double absolute_value
    {
        get => s_settings_file.GetDouble("absolute_value", 0);
        set { s_settings_file.SetDouble("absolute_value", value); }
    }

    public static double small_step
    {
        get => s_settings_file.GetDouble("small_step", 5);
        set { s_settings_file.SetDouble("small_step", value); }
    }

    public static double big_step
    {
        get => s_settings_file.GetDouble("big_step", 25);
        set { s_settings_file.SetDouble("big_step", value); }
    }

    public static double time_small_step
    {
        get => s_settings_file.GetDouble("time_small_step", 5);
        set { s_settings_file.SetDouble("time_small_step", value); }
    }

    public static double time_large_step
    {
        get => s_settings_file.GetDouble("time_large_step", 25);
        set { s_settings_file.SetDouble("time_large_step", value); }
    }
}