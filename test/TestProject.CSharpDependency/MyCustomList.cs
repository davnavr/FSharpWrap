namespace CSharpDependency
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class MyCustomList<T> : IEnumerable<T>
    {
        public MyCustomList() { }

        public MyCustomList<T> Add(T item) => throw new NotImplementedException();
        public MyCustomList<T> AddRange(IEnumerable<T> items) => throw new NotImplementedException();
        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}
