using System;
using System.Collections.Generic;

namespace MediaArchiver.Storage
{
    public class SingletonFactory<T>
    {
        private static readonly Lazy<SingletonFactory<T>>
            lazyInstance = new Lazy<SingletonFactory<T>>();
        private Dictionary<string, T> _singletonDictionary;

        public static SingletonFactory<T> Instance => lazyInstance.Value;


        public SingletonFactory()
        {
            _singletonDictionary = new Dictionary<string, T>();
        }
        public static T GetSingleton(string key, Func<T> getInstance)
        {
            var internalInstance = Instance;
            var dic = internalInstance._singletonDictionary;
            if (dic.TryGetValue(key, out var instance))
                return instance;
            var newInstance = getInstance.Invoke();
            dic.Add(key,newInstance);
            return newInstance;
        }
    }
}