using System;

namespace UniGetUI.Core
{

    public abstract class SingletonBase<T> where T : SingletonBase<T>
    {
        private static readonly Lazy<T> Lazy =
            new(() => (Activator.CreateInstance(typeof(T), true) as T)!);

        public static T Instance => Lazy.Value;
    }
}
