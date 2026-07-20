using System;

namespace GameTranslator.Patches.Utils
{
    internal class StringBuffer
    {
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
            for (int i = this.IndexOfWord(oldValue, 0, this.length); i >= 0; i = this.IndexOfWord(oldValue, i + num2, this.length - (i + num2)))
            {
                this.Remove(i, num);
                this.Insert(i, newValue);
            }
            return this;
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

            static bool IsWordChar(char c)
            {
                return char.IsLetterOrDigit(c) || c == '_';
            }

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
                    if ((i - num2 == 0 || !IsWordChar(this.value[i - num2 - 1])) && (i == this.length || !IsWordChar(this.value[i])))
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
    }
}
