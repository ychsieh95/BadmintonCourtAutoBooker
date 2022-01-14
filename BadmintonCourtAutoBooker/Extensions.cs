using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BadmintonCourtAutoBooker
{
    public static class FormExtensions
    {
        public static void SetAttributes(
            this Form form,
            ContentAlignment contentAlignment = ContentAlignment.MiddleCenter,
            FormBorderStyle formBorderStyle = FormBorderStyle.Fixed3D,
            Icon icon = null,
            bool maximizeBox = false,
            bool minimizeBox = false,
            Size size = default,
            string text = "")
        {
            form.Visible = false;

            if (!string.IsNullOrEmpty(text))
                form.Text = text;

            form.FormBorderStyle = formBorderStyle;

            if (size.Width > 0)
                form.Width = size.Width;
            if (size.Height > 0)
                form.Height = size.Height;

            if (icon != null)
                form.Icon = icon;

            form.MaximizeBox = maximizeBox;
            form.MinimizeBox = minimizeBox;

            SetLocation(form: form, contentAlignment: contentAlignment);
        }

        public static void SetLocation(this Form form, ContentAlignment contentAlignment)
        {
            int workArea_width = Screen.PrimaryScreen.WorkingArea.Width,
                workArea_height = Screen.PrimaryScreen.WorkingArea.Height,
                width = form.Width,
                height = form.Height;

            switch (contentAlignment)
            {
                case ContentAlignment.BottomCenter:
                    form.Location = new Point((workArea_width - width) / 2, workArea_height - height);
                    break;
                case ContentAlignment.BottomLeft:
                    form.Location = new Point(0, workArea_height - height);
                    break;
                case ContentAlignment.BottomRight:
                    form.Location = new Point(workArea_width - width, workArea_height - height);
                    break;
                case ContentAlignment.MiddleCenter:
                    form.Location = new Point((workArea_width - width) / 2, (workArea_height - height) / 2);
                    break;
                case ContentAlignment.MiddleLeft:
                    form.Location = new Point(0, (workArea_height - height) / 2);
                    break;
                case ContentAlignment.MiddleRight:
                    form.Location = new Point(workArea_width - width, (workArea_height - height) / 2);
                    break;
                case ContentAlignment.TopCenter:
                    form.Location = new Point((workArea_width - width) / 2, 0);
                    break;
                case ContentAlignment.TopLeft:
                    form.Location = new Point(0, 0);
                    break;
                case ContentAlignment.TopRight:
                    form.Location = new Point(workArea_width - width, 0);
                    break;
                default:
                    break;
            }
        }
    }

    public static class TextBoxExtensions
    {

        [DllImport("user32.dll")]
        private static extern int SendMessageA(IntPtr hwnd, int wMsg, IntPtr wParam, byte[] lParam);
        private const int EM_SETCUEBANNER = 5377;

        public static void SetWaterMark(this TextBox textBox, string wmText) =>
            SendMessageA(
                hwnd: textBox.Handle,
                wMsg: EM_SETCUEBANNER,
                wParam: IntPtr.Zero,
                lParam: Encoding.Unicode.GetBytes(wmText));
        public static int ToTimePartCode(this int timeCode)
        {
            switch (timeCode)
            {
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                    return 1;
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                    return 2;
                case 18:
                case 19:
                case 20:
                case 21:
                    return 3;
                default:
                    throw new ArgumentException("Incorrect time code, must be 6-21.");
            }
        }
    }

    public static class CheckedListBoxExtensions
    {
        public static List<int> GetCheckedItemIndexs(this CheckedListBox checkedListBox)
        {
            List<int> checkedIndexs = new List<int>();
            if (checkedListBox.Items.Count > 0)
            {
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    if (checkedListBox.GetItemChecked(i))
                    {
                        checkedIndexs.Add(i);
                    }
                }
            }
            return checkedIndexs;
        }

        public static void SetItemsChecked(this CheckedListBox checkedListBox, string checkedIndexsStr)
        {
            if (!string.IsNullOrEmpty(checkedIndexsStr) && checkedIndexsStr.Contains(','))
            {
                foreach (string indexStr in checkedIndexsStr.Split(','))
                {
                    if (int.TryParse(indexStr, out int index))
                    {
                        checkedListBox.SetItemChecked(index, true);
                    }
                }
            }
        }
    }

    public static class BookingExtensions
    {
        public static List<int> ToTimePartCodes(this List<int> timeCodes)
        {
            List<int> timePartCodes = new List<int>();
            foreach (int timeCode in timeCodes)
            {
                int timePartCode = timeCode.ToTimePartCode();
                if (!timePartCodes.Contains(timePartCode))
                {
                    timePartCodes.Add(timePartCode);
                }
            }
            return timePartCodes;
        }
    }

    public static class StringExtensions
    {
        public static string Repeat(this string s, int n) => new StringBuilder(s.Length * n).Insert(0, s, n).ToString();
    }
}
