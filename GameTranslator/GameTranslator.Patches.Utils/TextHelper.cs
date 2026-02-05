using System;
using System.Globalization;
using System.Text;

namespace GameTranslator.Patches.Utils
{
    internal static class TextHelper
    {
        public static string Encode(string text)
        {
            return TextHelper.EscapeNewlines(text);
        }

        public static string[] ReadTranslationLineAndDecode(string str)
        {
            string[] array;
            if (string.IsNullOrEmpty(str))
            {
                array = null;
            }
            else
            {
                string[] array2 = new string[2];
                int num = 0;
                bool flag = false;
                int length = str.Length;
                StringBuilder stringBuilder = new StringBuilder((int)((double)length / 1.3));
                for (int i = 0; i < length; i++)
                {
                    char c = str[i];
                    if (flag)
                    {
                        char c2 = c;
                        if (c2 <= '\\')
                        {
                            if (c2 != '=' && c2 != '\\')
                            {
                                stringBuilder.Append('\\');
                                stringBuilder.Append(c);
                                flag = false;
                                continue;
                            }
                            stringBuilder.Append(c);
                        }
                        else if (c2 != 'n')
                        {
                            if (c2 != 'r')
                            {
                                if (c2 != 'u')
                                {
                                    stringBuilder.Append('\\');
                                    stringBuilder.Append(c);
                                    flag = false;
                                    continue;
                                }
                                if (i + 4 >= length)
                                {
                                    throw new Exception("Found invalid unicode in line: " + str);
                                }
                                if (i + 1 >= length || i + 2 >= length || i + 3 >= length || i + 4 >= length)
                                {
                                    throw new Exception("Invalid unicode escape sequence at position " + i + " in line: " + str);
                                }
                                int num2 = int.Parse(new string(new char[]
                                {
                                    str[i + 1],
                                    str[i + 2],
                                    str[i + 3],
                                    str[i + 4]
                                }), NumberStyles.HexNumber);
                                stringBuilder.Append((char)num2);
                                i += 4;
                            }
                            else
                            {
                                stringBuilder.Append('\r');
                            }
                        }
                        else
                        {
                            stringBuilder.Append('\n');
                        }
                        flag = false;
                        continue;
                    }
                    if (c == '\\')
                    {
                        flag = true;
                    }
                    else if (c == '=')
                    {
                        if (num > 1)
                        {
                            return null;
                        }
                        array2[num++] = stringBuilder.ToString();
                        stringBuilder.Length = 0;
                    }
                    else if (c == '%')
                    {
                        if (i + 2 < length && i + 1 < length && i + 2 < length && str[i + 1] == '3' && str[i + 2] == 'D')
                        {
                            stringBuilder.Append('=');
                            i += 2;
                        }
                        else
                        {
                            stringBuilder.Append(c);
                        }
                    }
                    else if (c == '/')
                    {
                        int num3 = i + 1;
                        if (num3 < length && num3 < length && str[num3] == '/')
                        {
                            array2[num++] = stringBuilder.ToString();
                            if (num == 2)
                            {
                                return array2;
                            }
                            return null;
                        }
                        else
                        {
                            stringBuilder.Append(c);
                        }
                    }
                    else
                    {
                        stringBuilder.Append(c);
                    }
                }
                if (num != 1)
                {
                    array = null;
                }
                else
                {
                    array2[num++] = stringBuilder.ToString();
                    array = array2;
                }
            }
            return array;
        }

        internal static string EscapeNewlines(string str)
        {
            string text;
            if (str == null || str.Length == 0)
            {
                text = "";
            }
            else
            {
                int length = str.Length;
                StringBuilder stringBuilder = new StringBuilder(length + 4);
                int i = 0;
                while (i < length)
                {
                    char c = str[i];
                    char c2 = c;
                    if (c2 <= '\r')
                    {
                        if (c2 != '\n')
                        {
                            if (c2 != '\r')
                            {
                                stringBuilder.Append(c);
                                i++;
                                continue;
                            }
                            stringBuilder.Append("\\r");
                        }
                        else
                        {
                            stringBuilder.Append("\\n");
                        }
                    }
                    else if (c2 != '/')
                    {
                        if (c2 != '=')
                        {
                            if (c2 != '\\')
                            {
                                stringBuilder.Append(c);
                                i++;
                                continue;
                            }
                            stringBuilder.Append('\\');
                            stringBuilder.Append(c);
                        }
                        else
                        {
                            stringBuilder.Append('\\');
                            stringBuilder.Append(c);
                        }
                    }
                    else
                    {
                        int num = i + 1;
                        if (num < length && str[num] == '/')
                        {
                            stringBuilder.Append('\\');
                            stringBuilder.Append(c);
                            stringBuilder.Append('\\');
                            stringBuilder.Append(c);
                            i++;
                        }
                        else
                        {
                            stringBuilder.Append(c);
                        }
                    }
                    i++;
                }
                text = stringBuilder.ToString();
            }
            return text;
        }
    }
}
