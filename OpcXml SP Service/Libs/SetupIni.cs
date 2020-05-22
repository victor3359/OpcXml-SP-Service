using System.Text;
using System.Runtime.InteropServices;

namespace OpcXml_SP_Service.Libs
{
    class SetupIni
    {
        [DllImport(@"kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section,
               string key, string val, string filePath);
        [DllImport(@"kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section,
            string key, string def, StringBuilder retVal,
            int size, string filePath);

        private static string IniFilePath;  //INI檔案名稱
        private static StringBuilder lpReturnedString;
        private static int BufferSize;

        public SetupIni(string iniFilePath, int bufferSize)
        {
            IniFilePath = iniFilePath;
            BufferSize = bufferSize;
            lpReturnedString = new StringBuilder(BufferSize);
        }
        public void IniWriteValue(string Section, string Key, string Value) //INI寫入函式
        {
            WritePrivateProfileString(Section, Key, Value, System.Environment.CurrentDirectory + "\\" + IniFilePath);
        }

        public string IniReadValue(string Section, string Key)  //INI讀取函式
        {
            lpReturnedString.Clear();
            int i = GetPrivateProfileString(Section, Key, "", lpReturnedString, BufferSize, 
                System.Environment.CurrentDirectory + "\\" + IniFilePath);
            if (i.Equals(0))
            {
                return @"NotFound";
            }
            return lpReturnedString.ToString();
        }
    }
}
