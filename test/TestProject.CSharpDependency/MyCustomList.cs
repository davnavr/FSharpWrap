namespace CSharpDependency
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A naive implementation for an immutable list.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    public class MyCustomList<T> : IEnumerable<T>
    {
        private readonly T[] items;

        public MyCustomList() : this(new T[0]) { }
        public MyCustomList(IEnumerable<T> items) : this(items.ToArray()) { }
        private MyCustomList(T[] items) => this.items = items;

        public MyCustomList<T> Add(T item)
        {
            var last = this.items.LongLength;
            var replacement = new T[last + 1L];
            Array.Copy(this.items, replacement, last);
            replacement[last] = item;
            return new MyCustomList<T>(replacement);
        }

        public MyCustomList<T> AddRange(IEnumerable<T> items)
        {
            var adding = items.ToArray();
            var replacement = new T[this.items.LongLength + adding.LongLength];
            Array.Copy(this.items, 0L, replacement, 0L, this.items.LongLength);
            Array.Copy(adding, 0L, replacement, this.items.LongLength, adding.LongLength);
            return new MyCustomList<T>(replacement);
        }

        public IEnumerator<T> GetEnumerator() => this.items.Cast<T>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();
    }
}
