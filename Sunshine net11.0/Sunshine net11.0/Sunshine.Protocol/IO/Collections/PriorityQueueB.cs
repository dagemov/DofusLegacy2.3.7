using System.Collections;
using System.Collections.Generic;

namespace Sunshine.Protocol.IO.Collections
{
    public class PriorityQueueB<T> : IEnumerable<T>, IPriorityQueue<T>, IEnumerable
    {
        protected IComparer<T> Comparer;
        protected List<T> InnerList = new List<T>();
        public int Count
        {
            get
            {
                return this.InnerList.Count;
            }
        }
        public T this[int index]
        {
            get
            {
                return this.InnerList[index];
            }
            set
            {
                this.InnerList[index] = value;
                this.Update(index);
            }
        }

        public PriorityQueueB()
        {
            this.Comparer = Comparer<T>.Default;
        }

        public PriorityQueueB(IComparer<T> comparer)
        {
            this.Comparer = comparer;
        }

        public PriorityQueueB(IComparer<T> comparer, int capacity)
        {
            this.Comparer = comparer;
            this.InnerList.Capacity = capacity;
        }

        public int Push(T item)
        {
            int num = this.InnerList.Count;
            this.InnerList.Add(item);
            while (num != 0)
            {
                int num2 = (num - 1) / 2;
                if (this.OnCompare(num, num2) >= 0)
                {
                    break;
                }
                this.SwitchElements(num, num2);
                num = num2;
            }
            return num;
        }

        public T Pop()
        {
            T result = this.InnerList[0];
            int num = 0;
            this.InnerList[0] = this.InnerList[this.InnerList.Count - 1];
            this.InnerList.RemoveAt(this.InnerList.Count - 1);
            while (true)
            {
                int num2 = num;
                int num3 = 2 * num + 1;
                int num4 = 2 * num + 2;
                if (this.InnerList.Count > num3 && this.OnCompare(num, num3) > 0)
                {
                    num = num3;
                }
                if (this.InnerList.Count > num4 && this.OnCompare(num, num4) > 0)
                {
                    num = num4;
                }
                if (num == num2)
                {
                    break;
                }
                this.SwitchElements(num, num2);
            }
            return result;
        }

        public void Update(int i)
        {
            int num;
            int num2;
            for (num = i; num != 0; num = num2)
            {
                num2 = (num - 1) / 2;
                if (this.OnCompare(num, num2) >= 0)
                {
                    break;
                }
                this.SwitchElements(num, num2);
            }
            if (num >= i)
            {
                while (true)
                {
                    int num3 = num;
                    int num4 = 2 * num + 1;
                    num2 = 2 * num + 2;
                    if (this.InnerList.Count > num4 && this.OnCompare(num, num4) > 0)
                    {
                        num = num4;
                    }
                    if (this.InnerList.Count > num2 && this.OnCompare(num, num2) > 0)
                    {
                        num = num2;
                    }
                    if (num == num3)
                    {
                        break;
                    }
                    this.SwitchElements(num, num3);
                }
            }
        }

        public T Peek()
        {
            return (this.InnerList.Count > 0) ? this.InnerList[0] : default(T);
        }

        protected void SwitchElements(int i, int j)
        {
            T value = this.InnerList[i];
            this.InnerList[i] = this.InnerList[j];
            this.InnerList[j] = value;
        }

        protected virtual int OnCompare(int i, int j)
        {
            return this.Comparer.Compare(this.InnerList[i], this.InnerList[j]);
        }

        public void Clear()
        {
            this.InnerList.Clear();
        }

        public void Remove(T item)
        {
            int num = -1;
            for (int i = 0; i < this.InnerList.Count; i++)
            {
                if (this.Comparer.Compare(this.InnerList[i], item) == 0)
                {
                    num = i;
                }
            }
            if (num != -1)
            {
                this.InnerList.RemoveAt(num);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.InnerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}