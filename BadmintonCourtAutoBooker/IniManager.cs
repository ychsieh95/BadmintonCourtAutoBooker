using System.Runtime.InteropServices;
using System.Text;

namespace BadmintonCourtAutoBooker
{
    internal class IniManager
    {
        private readonly string filePath;
        private readonly StringBuilder lpStringBuilder;
        private readonly int bufferSize;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string lpString, string lpFileName);

        public IniManager(string iniPath)
        {
            filePath = iniPath;
            bufferSize = 512;
            lpStringBuilder = new StringBuilder(bufferSize);
        }

        public string ReadIniFile(string section, string key, string defaultValue)
        {
            lpStringBuilder.Clear();
            GetPrivateProfileString(section, key, defaultValue, lpStringBuilder, bufferSize, filePath);
            return lpStringBuilder.ToString();
        }

        public void WriteIniFile(string section, string key, object value) => _ = WritePrivateProfileString(section, key, value.ToString(), filePath);
    }
}
