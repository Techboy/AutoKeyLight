using AutoKeyLight.Core;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace AutoKeyLight
{
    internal static class ThemeColors
    {
        public static Color ToColor(this ThemeColor color)
        {
            return Color.FromArgb(color.R, color.G, color.B);
        }
    }

    internal static class WindowsTheme
    {
        public static bool? AppsUseLightTheme()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");
            if (value is int number)
                return number != 0;

            return null;
        }
    }

    internal static class TitleBarTheme
    {
        public static void Apply(IntPtr handle, bool dark)
        {
            if (handle == IntPtr.Zero)
                return;

            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(handle, 20, ref value, sizeof(int));
            DwmSetWindowAttribute(handle, 19, ref value, sizeof(int));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
    }

    internal sealed class PaletteColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette palette;

        public PaletteColorTable(ThemePalette palette)
        {
            this.palette = palette;
        }

        public override Color ToolStripDropDownBackground => palette.MenuBackground.ToColor();
        public override Color MenuBorder => palette.Border.ToColor();
        public override Color MenuItemBorder => palette.MenuBackground.ToColor();
        public override Color MenuItemSelected => palette.MenuHighlight.ToColor();
        public override Color MenuItemSelectedGradientBegin => palette.MenuHighlight.ToColor();
        public override Color MenuItemSelectedGradientEnd => palette.MenuHighlight.ToColor();
        public override Color ImageMarginGradientBegin => palette.MenuBackground.ToColor();
        public override Color ImageMarginGradientMiddle => palette.MenuBackground.ToColor();
        public override Color ImageMarginGradientEnd => palette.MenuBackground.ToColor();
        public override Color SeparatorDark => palette.Border.ToColor();
        public override Color SeparatorLight => palette.MenuBackground.ToColor();
        public override Color MenuStripGradientBegin => palette.Background.ToColor();
        public override Color MenuStripGradientEnd => palette.Background.ToColor();
        public override Color ToolStripGradientBegin => palette.Background.ToColor();
        public override Color ToolStripGradientMiddle => palette.Background.ToColor();
        public override Color ToolStripGradientEnd => palette.Background.ToColor();
        public override Color CheckBackground => palette.MenuHighlight.ToColor();
        public override Color CheckSelectedBackground => palette.MenuHighlight.ToColor();
        public override Color CheckPressedBackground => palette.MenuHighlight.ToColor();
    }

    internal static class ThemeApplier
    {
        public static void Apply(Control root, ThemePalette palette)
        {
            ApplyControl(root, palette);
            if (root is Form form)
            {
                form.BackColor = palette.Background.ToColor();
                form.ForeColor = palette.Foreground.ToColor();
            }
        }

        public static void ApplyMenu(ContextMenuStrip menu, ThemePalette palette)
        {
            menu.Renderer = new ToolStripProfessionalRenderer(new PaletteColorTable(palette));
            menu.BackColor = palette.MenuBackground.ToColor();
            menu.ForeColor = palette.MenuForeground.ToColor();
            foreach (ToolStripItem item in menu.Items)
            {
                item.BackColor = palette.MenuBackground.ToColor();
                item.ForeColor = palette.MenuForeground.ToColor();
            }
        }

        public static void ApplySystem(Control root)
        {
            ApplySystemControl(root);
        }

        public static void ApplySystem(Control root, ContextMenuStrip menu)
        {
            ApplySystem(root);
            menu.Renderer = new ToolStripProfessionalRenderer();
            menu.BackColor = SystemColors.Control;
            menu.ForeColor = SystemColors.ControlText;
            foreach (ToolStripItem item in menu.Items)
            {
                item.BackColor = SystemColors.Control;
                item.ForeColor = SystemColors.ControlText;
            }
        }

        private static void ApplyControl(Control control, ThemePalette palette)
        {
            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.UseVisualStyleBackColor = false;
                button.BackColor = palette.ButtonBackground.ToColor();
                button.ForeColor = palette.ButtonForeground.ToColor();
                button.FlatAppearance.BorderColor = palette.Border.ToColor();
            }
            else if (control is ListView or ComboBox or NumericUpDown or TextBox)
            {
                control.BackColor = palette.InputBackground.ToColor();
                control.ForeColor = palette.InputForeground.ToColor();
            }
            else if (control is CheckBox check)
            {
                check.UseVisualStyleBackColor = false;
                check.BackColor = palette.Background.ToColor();
                check.ForeColor = palette.Foreground.ToColor();
            }
            else if (control is RadioButton radio)
            {
                radio.UseVisualStyleBackColor = false;
                radio.BackColor = palette.Background.ToColor();
                radio.ForeColor = palette.Foreground.ToColor();
            }
            else
            {
                control.BackColor = palette.Background.ToColor();
                control.ForeColor = palette.Foreground.ToColor();
            }

            foreach (Control child in control.Controls)
                ApplyControl(child, palette);
        }

        private static void ApplySystemControl(Control control)
        {
            if (control is ListView or ComboBox or NumericUpDown or TextBox)
            {
                control.BackColor = SystemColors.Window;
                control.ForeColor = SystemColors.WindowText;
            }
            else
            {
                control.BackColor = SystemColors.Control;
                control.ForeColor = SystemColors.ControlText;
            }

            if (control is Button button)
            {
                button.UseVisualStyleBackColor = true;
                button.FlatStyle = FlatStyle.Standard;
            }

            foreach (Control child in control.Controls)
                ApplySystemControl(child);
        }
    }
}
