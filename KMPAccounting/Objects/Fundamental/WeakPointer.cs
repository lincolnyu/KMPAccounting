namespace KMPAccounting.Objects.Fundamental
{
    public class WeakPointer<T> where T : WeakPointed<T>
    {
        public class Handle
        {
            public T? Target;
            public int RefCount;
        }

        public WeakPointer(T target)
        {
            var existingHandle = target.GetWeakHandle();
            if (existingHandle != null)
            {
                _handle = existingHandle;
            }
            else
            {
                _handle = new Handle { Target = target, RefCount = 1 };
                target.SetWeakHandle(_handle);
            }
        }

        public bool TryGetTarget(out T? target)
        {
            if (_handle.Target != null)
            {
                target = _handle.Target;
                return true;
            }
            target = default;
            return false;
        }

        private readonly Handle _handle;
    }
}
