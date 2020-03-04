using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

// Extension methods for NativeArray <-> ReadOnlySpan conversion

namespace Lasp
{
    static class SpanNativeArrayExtensions
    {
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

        public unsafe static ReadOnlySpan<T>
          GetReadOnlySpan<T>(this NativeArray<T> array) where T : unmanaged
            => new Span<T>
              (NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(array),
               array.Length);
    }
}
