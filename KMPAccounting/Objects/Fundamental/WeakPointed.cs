using System;
using static System.GC;

namespace KMPAccounting.Objects.Fundamental
{
    public class WeakPointed<T> : IDisposable where T : WeakPointed<T>
    {
        ~WeakPointed()
        {
            Dispose(false);
        }

        public virtual void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_weakHandle != null)
            {
                _weakHandle.Target = null; // This invalidates the handle
                _weakHandle = null;
            }

            if (disposing)
            {
                SuppressFinalize(this);
            }
        }

        internal void SetWeakHandle(WeakPointer<T>.Handle handle)
        {
            _weakHandle = handle;
        }

        public WeakPointer<T>.Handle? GetWeakHandle()
        {
            return _weakHandle;
        }

        private WeakPointer<T>.Handle? _weakHandle;
    }
}