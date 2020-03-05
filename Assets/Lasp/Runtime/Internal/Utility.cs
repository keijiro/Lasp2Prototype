using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Lasp
{
    // Extension methods for converting NativeArray <-> ReadOnlySpan
    static class SpanNativeArrayExtensions
    {
        // ReadOnlySpan -> NativeArray
        public unsafe static NativeArray<T>
          GetNativeArray<T>(this ReadOnlySpan<T> span) where T : unmanaged
        {
            fixed (void* ptr = &span.GetPinnableReference())
            {
                var array = NativeArrayUnsafeUtility.
                  ConvertExistingDataToNativeArray<T>
                    (ptr, span.Length, Allocator.None);

              #if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle
                  (ref array, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
              #endif

                return array;
            }
        }

        // NativeArray -> ReadOnlySpan
        public unsafe static ReadOnlySpan<T>
          GetReadOnlySpan<T>(this NativeArray<T> array) where T : unmanaged
            => new Span<T>
              (NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(array),
               array.Length);
    }

    // Extension methods for List<T>
    static class ListExtensions
    {
        // Find and retrieve an entry with removing it
        public static T FindAndRemove<T>(this List<T> list, Predicate<T> match)
        {
            var index = list.FindIndex(match);
            if (index < 0) return default(T);
            var res = list[index];
            list.RemoveAt(index);
            return res;
        }
    }
}
