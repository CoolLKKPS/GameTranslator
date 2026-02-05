using GameTranslator.Patches.Translatons;
using System;

namespace GameTranslator.Patches.Utils
{
    public class StringBuffer
    {
        public StringBuffer()
        {
            this.value = new char[16];
            this.length = 0;
            this.capacity = 16;
        }

        public StringBuffer(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            this.value = new char[capacity];
            this.length = 0;
            this.capacity = capacity;
        }

        public StringBuffer(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            this.value = new char[str.Length + 16];
            str.CopyTo(0, this.value, 0, str.Length);
            this.length = str.Length;
            this.capacity = str.Length + 16;
        }

        public int Length
        {
            get
            {
                return this.length;
            }
            set
            {
                if (value < 0 || value > this.capacity)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (value < this.length)
                {
                    Array.Clear(this.value, value, this.length - value);
                }
                this.length = value;
            }
        }

        public int Capacity
        {
            get
            {
                return this.capacity;
            }
            set
            {
                if (value < this.length)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (value != this.capacity)
                {
                    char[] array = new char[value];
                    Array.Copy(this.value, 0, array, 0, this.length);
                    this.value = array;
                    this.capacity = value;
                }
            }
        }

        public void EnsureCapacity(int minimumCapacity)
        {
            if (minimumCapacity < 0)
            {
                throw new ArgumentOutOfRangeException("minimumCapacity");
            }
            if (minimumCapacity > this.capacity)
            {
                int num = this.capacity * 2;
                if (num < minimumCapacity)
                {
                    num = minimumCapacity;
                }
                this.Capacity = num;
            }
        }

        public StringBuffer Append(object obj)
        {
            StringBuffer stringBuffer;
            if (obj == null)
            {
                stringBuffer = this;
            }
            else
            {
                stringBuffer = this.Append(obj.ToString());
            }
            return stringBuffer;
        }

        public StringBuffer Append(string str)
        {
            StringBuffer stringBuffer;
            if (str == null)
            {
                stringBuffer = this;
            }
            else
            {
                int num = str.Length;
                this.EnsureCapacity(this.length + num);
                str.CopyTo(0, this.value, this.length, num);
                this.length += num;
                stringBuffer = this;
            }
            return stringBuffer;
        }

        public StringBuffer Append(char c)
        {
            this.EnsureCapacity(this.length + 1);
            this.value[this.length] = c;
            this.length++;
            return this;
        }

        public StringBuffer Insert(int index, object obj)
        {
            StringBuffer stringBuffer;
            if (obj == null)
            {
                stringBuffer = this;
            }
            else
            {
                stringBuffer = this.Insert(index, obj.ToString());
            }
            return stringBuffer;
        }

        public StringBuffer Insert(int index, string str)
        {
            if (index < 0 || index > this.length)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            StringBuffer stringBuffer;
            if (str == null)
            {
                stringBuffer = this;
            }
            else
            {
                int num = str.Length;
                this.EnsureCapacity(this.length + num);
                Array.Copy(this.value, index, this.value, index + num, this.length - index);
                str.CopyTo(0, this.value, index, num);
                this.length += num;
                stringBuffer = this;
            }
            return stringBuffer;
        }

        public StringBuffer Insert(int index, char c)
        {
            if (index < 0 || index > this.length)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            this.EnsureCapacity(this.length + 1);
            Array.Copy(this.value, index, this.value, index + 1, this.length - index);
            this.value[index] = c;
            this.length++;
            return this;
        }

        public StringBuffer Remove(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex > this.length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (length < 0 || startIndex + length > this.length)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            Array.Copy(this.value, startIndex + length, this.value, startIndex, this.length - startIndex - length);
            Array.Clear(this.value, this.length - length, length);
            this.length -= length;
            return this;
        }

        public StringBuffer Replace(char oldChar, char newChar)
        {
            for (int i = 0; i < this.length; i++)
            {
                if (this.value[i] == oldChar)
                {
                    this.value[i] = newChar;
                }
            }
            return this;
        }

        public StringBuffer Replace(string oldValue, string newValue)
        {
            if (oldValue == null)
            {
                throw new ArgumentNullException("oldValue");
            }
            if (oldValue.Length == 0)
            {
                throw new ArgumentException("oldValue cannot be empty");
            }
            if (newValue == null)
            {
                newValue = string.Empty;
            }
            int num = oldValue.Length;
            int num2 = newValue.Length;
            for (int i = this.IndexOf(oldValue); i >= 0; i = this.IndexOf(oldValue, i + num2))
            {
                this.Remove(i, num);
                this.Insert(i, newValue);
            }
            return this;
        }

        public int IndexOf(char c)
        {
            return this.IndexOf(c, 0, this.length);
        }

        public int IndexOf(char c, int startIndex)
        {
            return this.IndexOf(c, startIndex, this.length - startIndex);
        }

        public int IndexOf(char c, int startIndex, int count)
        {
            if (startIndex < 0 || startIndex > this.length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (count < 0 || startIndex + count > this.length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return Array.IndexOf<char>(this.value, c, startIndex, count);
        }

        public int IndexOf(string str)
        {
            return this.IndexOf(str, 0, this.length);
        }

        public int IndexOf(string str, int startIndex)
        {
            return this.IndexOf(str, startIndex, this.length - startIndex);
        }

        public int IndexOf(string str, int startIndex, int count)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            if (startIndex < 0 || startIndex > this.length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (count < 0 || startIndex + count > this.length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return this.value.ToString(startIndex, count).IndexOf(str);
        }

        public string Substring(int startIndex)
        {
            return this.Substring(startIndex, this.length - startIndex);
        }

        public string Substring(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex > this.length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (length < 0 || startIndex + length > this.length)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            return new string(this.value, startIndex, length);
        }

        public bool Contains(string text)
        {
            return this.IndexOf(text) >= 0;
        }

        public StringBuffer ReplaceFull(string oldValue, string newValue)
        {
            if (oldValue == null)
            {
                throw new ArgumentNullException("oldValue");
            }
            if (oldValue.Length == 0)
            {
                throw new ArgumentException("oldValue cannot be empty");
            }
            if (newValue == null)
            {
                newValue = string.Empty;
            }
            int num = oldValue.Length;
            int num2 = newValue.Length;
            for (int i = this.IndexOfWord(oldValue); i >= 0; i = this.IndexOfWord(oldValue, i + num2))
            {
                this.Remove(i, num);
                this.Insert(i, newValue);
            }
            return this;
        }

        private int IndexOfWord(string str)
        {
            return this.IndexOfWord(str, 0, this.length);
        }

        private int IndexOfWord(string str, int startIndex)
        {
            return this.IndexOfWord(str, startIndex, this.length - startIndex);
        }

        public int IndexOfWord(string str, int startIndex, int count)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            if (startIndex < 0 || startIndex > this.length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (count < 0 || startIndex + count > this.length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            int num = str.Length;
            int[] array = new int[num];
            int num2 = 0;
            this.computeLPSArray(str, num, array);
            int i = startIndex;
            while (i < startIndex + count)
            {
                if (str[num2] == this.value[i])
                {
                    num2++;
                    i++;
                }
                if (num2 == num)
                {
                    if ((i - num2 == 0 || !char.IsLetter(this.value[i - num2 - 1])) && (i == this.length || !char.IsLetter(this.value[i])))
                    {
                        return i - num2;
                    }
                    num2 = array[num2 - 1];
                }
                else if (i < startIndex + count && str[num2] != this.value[i])
                {
                    if (num2 != 0)
                    {
                        num2 = array[num2 - 1];
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            return -1;
        }

        private void computeLPSArray(string str, int M, int[] lps)
        {
            int num = 0;
            int i = 1;
            lps[0] = 0;
            while (i < M)
            {
                if (str[i] == str[num])
                {
                    num++;
                    lps[i] = num;
                    i++;
                }
                else if (num != 0)
                {
                    num = lps[num - 1];
                }
                else
                {
                    lps[i] = num;
                    i++;
                }
            }
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        public StringBuffer Clear()
        {
            this.Length = 0;
            return this;
        }

        public override string ToString()
        {
            return new string(this.value, 0, this.length);
        }

        private char[] value;

        private int length;

        private int capacity;

        private const int DEFAULT_CAPACITY = 16;
    }
}
