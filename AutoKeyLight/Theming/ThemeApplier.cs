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
        public override Color MenuItemPressedGradientBegin => palette.MenuHighlight.ToColor();
        public override Color MenuItemPressedGradientMiddle => palette.MenuHighlight.ToColor();
        public override Color MenuItemPressedGradientEnd => palette.MenuHighlight.ToColor();
        public override Color ButtonSelectedHighlight => palette.MenuHighlight.ToColor();
        public override Color ButtonSelectedGradientBegin => palette.MenuHighlight.ToColor();
        public override Color ButtonSelectedGradientMiddle => palette.MenuHighlight.ToColor();
        public override Color ButtonSelectedGradientEnd => palette.MenuHighlight.ToColor();
        public override Color ButtonPressedHighlight => palette.MenuHighlight.ToColor();
        public override Color ButtonPressedGradientBegin => palette.MenuHighlight.ToColor();
        public override Color ButtonPressedGradientMiddle => palette.MenuHighlight.ToColor();
        public override Color ButtonPressedGradientEnd => palette.MenuHighlight.ToColor();
    }

    internal sealed class PaletteMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemePalette palette;

        public PaletteMenuRenderer(ThemePalette palette)
            : base(new PaletteColorTable(palette))
        {
            this.palette = palette;
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            Color back = e.ToolStrip is ToolStripDropDown
                ? palette.MenuBackground.ToColor()
                : palette.Background.ToColor();
            using SolidBrush brush = new SolidBrush(back);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            bool highlighted = e.Item.Selected || e.Item.Pressed;
            Color back = highlighted
                ? palette.MenuHighlight.ToColor()
                : e.Item.IsOnDropDown
                    ? palette.MenuBackground.ToColor()
                    : palette.Background.ToColor();
            using SolidBrush brush = new SolidBrush(back);
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Color text = e.Item.Selected || e.Item.Pressed
                ? palette.MenuHighlightText.ToColor()
                : palette.MenuForeground.ToColor();
            TextRenderer.DrawText(
                e.Graphics,
                e.Text,
                e.TextFont,
                e.TextRectangle,
                text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
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

        public static void ApplyMenu(ToolStrip menu, ThemePalette palette)
        {
            PaletteMenuRenderer renderer = new PaletteMenuRenderer(palette);
            menu.Renderer = renderer;
            Color back = menu is ToolStripDropDown ? palette.MenuBackground.ToColor() : palette.Background.ToColor();
            Color fore = palette.MenuForeground.ToColor();
            menu.BackColor = back;
            menu.ForeColor = fore;
            PaintMenuItems(menu.Items, palette, renderer);
        }

        private static void PaintMenuItems(ToolStripItemCollection items, ThemePalette palette, PaletteMenuRenderer renderer)
        {
            foreach (ToolStripItem item in items)
            {
                item.BackColor = item.IsOnDropDown ? palette.MenuBackground.ToColor() : palette.Background.ToColor();
                item.ForeColor = palette.MenuForeground.ToColor();
                if (item is ToolStripDropDownItem dropDown)
                {
                    dropDown.DropDown.Renderer = renderer;
                    dropDown.DropDown.BackColor = palette.MenuBackground.ToColor();
                    dropDown.DropDown.ForeColor = palette.MenuForeground.ToColor();
                    PaintMenuItems(dropDown.DropDownItems, palette, renderer);
                }
            }
        }

        public static void ApplySystem(Control root)
        {
            ApplySystemControl(root);
        }

        public static void ApplySystem(Control root, ContextMenuStrip menu)
        {
            ApplySystem(root);
            ApplySystemMenu(menu);
        }

        public static void ApplySystemMenu(ContextMenuStrip menu)
        {
            menu.Renderer = new ToolStripProfessionalRenderer();
            menu.BackColor = SystemColors.Control;
            menu.ForeColor = SystemColors.ControlText;
            PaintSystemItems(menu.Items);
        }

        private static void PaintSystemItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                item.BackColor = SystemColors.Control;
                item.ForeColor = SystemColors.ControlText;
                if (item is ToolStripDropDownItem dropDown)
                {
                    dropDown.DropDown.Renderer = new ToolStripProfessionalRenderer();
                    dropDown.DropDown.BackColor = SystemColors.Control;
                    dropDown.DropDown.ForeColor = SystemColors.ControlText;
                    PaintSystemItems(dropDown.DropDownItems);
                }
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
            else if (control is ListView or ListBox or ComboBox or NumericUpDown or TextBox)
            {
                control.BackColor = palette.InputBackground.ToColor();
                control.ForeColor = palette.InputForeground.ToColor();
            }
            else if (control is LinkLabel link)
            {
                link.LinkColor = palette.Link.ToColor();
                link.ActiveLinkColor = palette.Link.ToColor();
                link.VisitedLinkColor = palette.Link.ToColor();
                link.BackColor = palette.Background.ToColor();
                link.ForeColor = palette.Link.ToColor();
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
            if (control is ListView or ListBox or ComboBox or NumericUpDown or TextBox)
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
