using System;

namespace Alexitech.Scoping
{
    public static class ScopingExtensions
    {
        public static bool TryGet<T>(this IScope scope, out T value)
        {
            return scope.Data.TryGet<T>(out value);
        }

        public static bool TryGet<T>(this IScope scope, string key, out T value)
        {
            return scope.Data.TryGet<T>(key, out value);
        }

        public static T Get<T>(this IScope scope)
        {
            return scope.Data.Get<T>();
        }

        public static T Get<T>(this IScope scope, string key)
        {
            return scope.Data.Get<T>(key);
        }

        public static void Set<T>(this IScope scope, T value)
        {
            scope.Data.Set<T>(value);
        }

        public static void Set<T>(this IScope scope, string key, T value)
        {
            scope.Data.Set<T>(key, value);
        }

        public static T Ensure<T>(this IScope scope)
        {
            return scope.Data.Ensure<T>();
        }

        public static T Ensure<T>(this IScope scope, string key)
        {
            return scope.Data.Ensure<T>(key);
        }

        public static T Ensure<T>(this IScope scope, Func<T> getter)
        {
            return scope.Data.Ensure<T>(getter);
        }

        public static T Ensure<T>(this IScope scope, string key, Func<T> getter)
        {
            return scope.Data.Ensure<T>(key, getter);
        }
    }
}