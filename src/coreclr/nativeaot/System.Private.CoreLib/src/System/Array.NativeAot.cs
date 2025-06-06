// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.IntrinsicSupport;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using EETypeElementType = Internal.Runtime.EETypeElementType;
using MethodTable = Internal.Runtime.MethodTable;

namespace System
{
    // Note that we make a T[] (single-dimensional w/ zero as the lower bound) implement both
    // IList<U> and IReadOnlyList<U>, where T : U dynamically.  See the SZArrayHelper class for details.
    public abstract partial class Array : ICollection, IEnumerable, IList, IStructuralComparable, IStructuralEquatable, ICloneable
    {
        // CS0169: The field 'Array._numComponents' is never used
        // CA1823: Unused field '_numComponents'
#pragma warning disable 0169
#pragma warning disable CA1823
        // This field should be the first field in Array as the runtime/compilers depend on it
        [NonSerialized]
        private int _numComponents;
#pragma warning restore

        public int Length => checked((int)Unsafe.As<RawArrayData>(this).Length);

        // This could return a length greater than int.MaxValue
        internal nuint NativeLength => Unsafe.As<RawArrayData>(this).Length;

        public long LongLength => (long)NativeLength;

        internal unsafe bool IsSzArray
        {
            get
            {
                return this.GetMethodTable()->IsSzArray;
            }
        }

        // This is the classlib-provided "get array MethodTable" function that will be invoked whenever the runtime
        // needs to know the base type of an array.
        [RuntimeExport("GetSystemArrayEEType")]
        private static unsafe MethodTable* GetSystemArrayEEType()
        {
            return MethodTable.Of<Array>();
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        private static unsafe Array InternalCreate(RuntimeType elementType, int rank, int* pLengths, int* pLowerBounds)
        {
            if (elementType.IsByRef || elementType.IsByRefLike)
                throw new NotSupportedException(SR.NotSupported_ByRefLikeArray);
            if (elementType == typeof(void))
                throw new NotSupportedException(SR.NotSupported_VoidArray);
            if (elementType.ContainsGenericParameters)
                throw new NotSupportedException(SR.NotSupported_OpenType);

            if (pLowerBounds != null)
            {
                for (int i = 0; i < rank; i++)
                {
                    if (pLowerBounds[i] != 0)
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_NonZeroLowerBound);
                }
            }

            if (rank == 1)
            {
                return RuntimeAugments.NewArray(elementType.MakeArrayType().TypeHandle, pLengths[0]);
            }
            else
            {
                Type arrayType = elementType.MakeArrayType(rank);

                // Create a local copy of the lengths that cannot be modified by the caller
                int* pImmutableLengths = stackalloc int[rank];
                for (int i = 0; i < rank; i++)
                    pImmutableLengths[i] = pLengths[i];

                return NewMultiDimArray(arrayType.TypeHandle.ToMethodTable(), pImmutableLengths, rank);
            }
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures that if we have a TypeHandle of a Rank-1 MdArray, we also generated the SzArray.")]
        private static unsafe Array InternalCreateFromArrayType(RuntimeType arrayType, int rank, int* pLengths, int* pLowerBounds)
        {
            Debug.Assert(arrayType.IsArray);
            Debug.Assert(arrayType.GetArrayRank() == rank);

            if (arrayType.ContainsGenericParameters)
                throw new NotSupportedException(SR.NotSupported_OpenType);

            if (pLowerBounds != null)
            {
                for (int i = 0; i < rank; i++)
                {
                    if (pLowerBounds[i] != 0)
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_NonZeroLowerBound);
                }
            }

            if (rank == 1)
            {
                // Multidimensional array of rank 1 with 0 lower bounds gets actually allocated
                // as an SzArray. SzArray is castable to MdArray rank 1.
                RuntimeTypeHandle arrayTypeHandle = arrayType.IsSZArray
                    ? arrayType.TypeHandle
                    : arrayType.GetElementType().MakeArrayType().TypeHandle;

                return RuntimeAugments.NewArray(arrayTypeHandle, pLengths[0]);
            }
            else
            {
                // Create a local copy of the lengths that cannot be modified by the caller
                int* pImmutableLengths = stackalloc int[rank];
                for (int i = 0; i < rank; i++)
                    pImmutableLengths[i] = pLengths[i];

                MethodTable* eeType = arrayType.TypeHandle.ToMethodTable();
                return NewMultiDimArray(eeType, pImmutableLengths, rank);
            }
        }

        public unsafe void Initialize()
        {
            MethodTable* pElementEEType = ElementMethodTable;
            if (!pElementEEType->IsValueType)
                return;

            IntPtr constructorEntryPoint = RuntimeAugments.TypeLoaderCallbacks.TryGetDefaultConstructorForType(new RuntimeTypeHandle(pElementEEType));
            if (constructorEntryPoint == IntPtr.Zero)
                return;

            IntPtr constructorFtn = RuntimeAugments.TypeLoaderCallbacks.ConvertUnboxingFunctionPointerToUnderlyingNonUnboxingPointer(constructorEntryPoint, new RuntimeTypeHandle(pElementEEType));

            ref byte arrayRef = ref MemoryMarshal.GetArrayDataReference(this);
            nuint elementSize = ElementSize;

            for (nuint i = 0; i < NativeLength; i++)
            {
                RawCalliHelper.CallDefaultStructConstructor(constructorFtn, ref arrayRef);
                arrayRef = ref Unsafe.Add(ref arrayRef, elementSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetRawMultiDimArrayBounds()
        {
            Debug.Assert(!IsSzArray);
            return ref Unsafe.As<byte, int>(ref Unsafe.As<RawArrayData>(this).Data);
        }

        // Provides a strong exception guarantee - either it succeeds, or
        // it throws an exception with no side effects.  The arrays must be
        // compatible array types based on the array element type - this
        // method does not support casting, boxing, or primitive widening.
        // It will up-cast, assuming the array types are correct.
        public static void ConstrainedCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            CopyImpl(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable: true);
        }


        //
        // Funnel for all the Array.Copy() overloads. The "reliable" parameter indicates whether the caller for ConstrainedCopy()
        // (must leave destination array unchanged on any exception.)
        //
        private static unsafe void CopyImpl(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            if (sourceArray is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sourceArray);
            if (destinationArray is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);

            if (sourceArray.GetType() != destinationArray.GetType() && sourceArray.Rank != destinationArray.Rank)
                throw new RankException(SR.Rank_MustMatch);

            ArgumentOutOfRangeException.ThrowIfNegative(length);

            const int srcLB = 0;
            if (sourceIndex < srcLB || sourceIndex - srcLB < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_ArrayLB);
            sourceIndex -= srcLB;

            const int dstLB = 0;
            if (destinationIndex < dstLB || destinationIndex - dstLB < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), SR.ArgumentOutOfRange_ArrayLB);
            destinationIndex -= dstLB;

            if ((uint)(sourceIndex + length) > sourceArray.NativeLength)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));
            if ((uint)(destinationIndex + length) > destinationArray.NativeLength)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            MethodTable* sourceElementEEType = sourceArray.ElementMethodTable;
            MethodTable* destinationElementEEType = destinationArray.ElementMethodTable;

            if (!destinationElementEEType->IsValueType && !destinationElementEEType->IsPointer && !destinationElementEEType->IsFunctionPointer)
            {
                if (!sourceElementEEType->IsValueType && !sourceElementEEType->IsPointer && !sourceElementEEType->IsFunctionPointer)
                {
                    CopyImplGcRefArray(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable);
                }
                else if (RuntimeImports.AreTypesAssignable(sourceElementEEType, destinationElementEEType))
                {
                    CopyImplValueTypeArrayToReferenceArray(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable);
                }
                else
                {
                    throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);
                }
            }
            else
            {
                if (sourceElementEEType == destinationElementEEType)
                {
                    if (sourceElementEEType->ContainsGCPointers)
                    {
                        CopyImplValueTypeArrayWithInnerGcRefs(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable);
                    }
                    else
                    {
                        CopyImplValueTypeArrayNoInnerGcRefs(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
                    }
                }
                else if ((sourceElementEEType->IsPointer || sourceElementEEType->IsFunctionPointer) && (destinationElementEEType->IsPointer || destinationElementEEType->IsFunctionPointer))
                {
                    // CLR compat note: CLR only allows Array.Copy between pointee types that would be assignable
                    // to using array covariance rules (so int*[] can be copied to uint*[], but not to float*[]).
                    if (RuntimeImports.AreTypesAssignable(sourceElementEEType, destinationElementEEType))
                    {
                        CopyImplValueTypeArrayNoInnerGcRefs(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
                    }
                    else
                    {
                        throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);
                    }
                }
                else if (IsSourceElementABaseClassOrInterfaceOfDestinationValueType(sourceElementEEType, destinationElementEEType))
                {
                    CopyImplReferenceArrayToValueTypeArray(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable);
                }
                else if (sourceElementEEType->IsPrimitive && destinationElementEEType->IsPrimitive)
                {
                    if (RuntimeImports.AreTypesAssignable(sourceArray.GetMethodTable(), destinationArray.GetMethodTable()))
                    {
                        // If we're okay casting between these two, we're also okay blitting the values over
                        CopyImplValueTypeArrayNoInnerGcRefs(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
                    }
                    else
                    {
                        // The only case remaining is that primitive types could have a widening conversion between the source element type and the destination
                        // If a widening conversion does not exist we are going to throw an ArrayTypeMismatchException from it.
                        CopyImplPrimitiveTypeWithWidening(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable);
                    }
                }
                else
                {
                    throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);
                }
            }
        }

        private static unsafe bool IsSourceElementABaseClassOrInterfaceOfDestinationValueType(MethodTable* sourceElementEEType, MethodTable* destinationElementEEType)
        {
            if (sourceElementEEType->IsValueType || sourceElementEEType->IsPointer || sourceElementEEType->IsFunctionPointer)
                return false;

            // It may look like we're passing the arguments to AreTypesAssignable in the wrong order but we're not. The source array is an interface or Object array, the destination
            // array is a value type array. Our job is to check if the destination value type implements the interface - which is what this call to AreTypesAssignable does.
            // The copy loop still checks each element to make sure it actually is the correct valuetype.
            if (!RuntimeImports.AreTypesAssignable(destinationElementEEType, sourceElementEEType))
                return false;
            return true;
        }

        //
        // Array.CopyImpl case: Gc-ref array to gc-ref array copy.
        //
        private static unsafe void CopyImplGcRefArray(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            // For mismatched array types, the desktop Array.Copy has a policy that determines whether to throw an ArrayTypeMismatch without any attempt to copy
            // or to throw an InvalidCastException in the middle of a copy. This code replicates that policy.
            MethodTable* sourceElementEEType = sourceArray.ElementMethodTable;
            MethodTable* destinationElementEEType = destinationArray.ElementMethodTable;

            Debug.Assert(!sourceElementEEType->IsValueType && !sourceElementEEType->IsPointer && !sourceElementEEType->IsFunctionPointer);
            Debug.Assert(!destinationElementEEType->IsValueType && !destinationElementEEType->IsPointer && !destinationElementEEType->IsFunctionPointer);

            bool attemptCopy = RuntimeImports.AreTypesAssignable(sourceElementEEType, destinationElementEEType);
            bool mustCastCheckEachElement = !attemptCopy;
            if (reliable)
            {
                if (mustCastCheckEachElement)
                    throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_ConstrainedCopy);
            }
            else
            {
                attemptCopy = attemptCopy || RuntimeImports.AreTypesAssignable(destinationElementEEType, sourceElementEEType);

                // If either array is an interface array, we allow the attempt to copy even if the other element type does not statically implement the interface.
                // We don't have an "IsInterface" property in EETypePtr so we instead check for a null BaseType. The only the other MethodTable with a null BaseType is
                // System.Object but if that were the case, we would already have passed one of the AreTypesAssignable checks above.
                attemptCopy = attemptCopy || sourceElementEEType->BaseType == null;
                attemptCopy = attemptCopy || destinationElementEEType->BaseType == null;

                if (!attemptCopy)
                    throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);
            }

            bool reverseCopy = ((object)sourceArray == (object)destinationArray) && (sourceIndex < destinationIndex);
            ref object? refDestinationArray = ref Unsafe.As<byte, object?>(ref MemoryMarshal.GetArrayDataReference(destinationArray));
            ref object? refSourceArray = ref Unsafe.As<byte, object?>(ref MemoryMarshal.GetArrayDataReference(sourceArray));
            if (reverseCopy)
            {
                sourceIndex += length - 1;
                destinationIndex += length - 1;
                for (int i = 0; i < length; i++)
                {
                    object? value = Unsafe.Add(ref refSourceArray, sourceIndex - i);
                    if (mustCastCheckEachElement && value != null && TypeCast.IsInstanceOfAny(destinationElementEEType, value) == null)
                        throw new InvalidCastException(SR.InvalidCast_DownCastArrayElement);
                    Unsafe.Add(ref refDestinationArray, destinationIndex - i) = value;
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    object? value = Unsafe.Add(ref refSourceArray, sourceIndex + i);
                    if (mustCastCheckEachElement && value != null && TypeCast.IsInstanceOfAny(destinationElementEEType, value) == null)
                        throw new InvalidCastException(SR.InvalidCast_DownCastArrayElement);
                    Unsafe.Add(ref refDestinationArray, destinationIndex + i) = value;
                }
            }
        }

        //
        // Array.CopyImpl case: Value-type array to Object[] or interface array copy.
        //
        private static unsafe void CopyImplValueTypeArrayToReferenceArray(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            Debug.Assert(sourceArray.ElementMethodTable->IsValueType);
            Debug.Assert(!destinationArray.ElementMethodTable->IsValueType && !destinationArray.ElementMethodTable->IsPointer && !destinationArray.ElementMethodTable->IsFunctionPointer);

            // Caller has already validated this.
            Debug.Assert(RuntimeImports.AreTypesAssignable(sourceArray.ElementMethodTable, destinationArray.ElementMethodTable));

            if (reliable)
                throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_ConstrainedCopy);

            MethodTable* sourceElementEEType = sourceArray.ElementMethodTable;
            nuint sourceElementSize = sourceArray.ElementSize;

            fixed (byte* pSourceArray = &MemoryMarshal.GetArrayDataReference(sourceArray))
            {
                byte* pElement = pSourceArray + (nuint)sourceIndex * sourceElementSize;
                ref object refDestinationArray = ref Unsafe.As<byte, object>(ref MemoryMarshal.GetArrayDataReference(destinationArray));
                for (int i = 0; i < length; i++)
                {
                    object boxedValue = RuntimeExports.RhBox(sourceElementEEType, ref *pElement);
                    Unsafe.Add(ref refDestinationArray, destinationIndex + i) = boxedValue;
                    pElement += sourceElementSize;
                }
            }
        }

        //
        // Array.CopyImpl case: Object[] or interface array to value-type array copy.
        //
        private static unsafe void CopyImplReferenceArrayToValueTypeArray(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            Debug.Assert(!sourceArray.ElementMethodTable->IsValueType && !sourceArray.ElementMethodTable->IsPointer && !sourceArray.ElementMethodTable->IsFunctionPointer);
            Debug.Assert(destinationArray.ElementMethodTable->IsValueType);

            if (reliable)
                throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);

            MethodTable* destinationElementEEType = destinationArray.ElementMethodTable;
            nuint destinationElementSize = destinationArray.ElementSize;
            bool isNullable = destinationElementEEType->IsNullable;

            fixed (byte* pDestinationArray = &MemoryMarshal.GetArrayDataReference(destinationArray))
            {
                ref object refSourceArray = ref Unsafe.As<byte, object>(ref MemoryMarshal.GetArrayDataReference(sourceArray));
                byte* pElement = pDestinationArray + (nuint)destinationIndex * destinationElementSize;

                for (int i = 0; i < length; i++)
                {
                    object boxedValue = Unsafe.Add(ref refSourceArray, sourceIndex + i);
                    if (boxedValue == null)
                    {
                        if (!isNullable)
                            throw new InvalidCastException(SR.InvalidCast_DownCastArrayElement);
                    }
                    else
                    {
                        MethodTable* eeType = boxedValue.GetMethodTable();
                        if (!(RuntimeImports.AreTypesAssignable(eeType, destinationElementEEType)))
                            throw new InvalidCastException(SR.InvalidCast_DownCastArrayElement);
                    }

                    RuntimeImports.RhUnbox(boxedValue, ref *pElement, destinationElementEEType);
                    pElement += destinationElementSize;
                }
            }
        }


        //
        // Array.CopyImpl case: Value-type array with embedded gc-references.
        //
        private static unsafe void CopyImplValueTypeArrayWithInnerGcRefs(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            Debug.Assert(sourceArray.GetMethodTable() == destinationArray.GetMethodTable());
            Debug.Assert(sourceArray.ElementMethodTable->IsValueType);

            MethodTable* sourceElementEEType = sourceArray.GetMethodTable()->RelatedParameterType;
            bool reverseCopy = ((object)sourceArray == (object)destinationArray) && (sourceIndex < destinationIndex);

            // Copy scenario: ValueType-array to value-type array with embedded gc-refs.
            object[]? boxedElements = null;
            if (reliable)
            {
                boxedElements = new object[length];
                reverseCopy = false;
            }

            fixed (byte* pDstArray = &MemoryMarshal.GetArrayDataReference(destinationArray), pSrcArray = &MemoryMarshal.GetArrayDataReference(sourceArray))
            {
                nuint cbElementSize = sourceArray.ElementSize;
                byte* pSourceElement = pSrcArray + (nuint)sourceIndex * cbElementSize;
                byte* pDestinationElement = pDstArray + (nuint)destinationIndex * cbElementSize;
                if (reverseCopy)
                {
                    pSourceElement += (nuint)length * cbElementSize;
                    pDestinationElement += (nuint)length * cbElementSize;
                }

                for (int i = 0; i < length; i++)
                {
                    if (reverseCopy)
                    {
                        pSourceElement -= cbElementSize;
                        pDestinationElement -= cbElementSize;
                    }

                    object boxedValue = RuntimeExports.RhBox(sourceElementEEType, ref *pSourceElement);
                    if (boxedElements != null)
                        boxedElements[i] = boxedValue;
                    else
                        RuntimeImports.RhUnbox(boxedValue, ref *pDestinationElement, sourceElementEEType);

                    if (!reverseCopy)
                    {
                        pSourceElement += cbElementSize;
                        pDestinationElement += cbElementSize;
                    }
                }
            }

            if (boxedElements != null)
            {
                fixed (byte* pDstArray = &MemoryMarshal.GetArrayDataReference(destinationArray))
                {
                    nuint cbElementSize = sourceArray.ElementSize;
                    byte* pDestinationElement = pDstArray + (nuint)destinationIndex * cbElementSize;
                    for (int i = 0; i < length; i++)
                    {
                        RuntimeImports.RhUnbox(boxedElements[i], ref *pDestinationElement, sourceElementEEType);
                        pDestinationElement += cbElementSize;
                    }
                }
            }
        }

        //
        // Array.CopyImpl case: Value-type array without embedded gc-references.
        //
        private static unsafe void CopyImplValueTypeArrayNoInnerGcRefs(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            Debug.Assert((sourceArray.ElementMethodTable->IsValueType && !sourceArray.ElementMethodTable->ContainsGCPointers) ||
                sourceArray.ElementMethodTable->IsPointer || sourceArray.ElementMethodTable->IsFunctionPointer);
            Debug.Assert((destinationArray.ElementMethodTable->IsValueType && !destinationArray.ElementMethodTable->ContainsGCPointers) ||
                destinationArray.ElementMethodTable->IsPointer || destinationArray.ElementMethodTable->IsFunctionPointer);

            // Copy scenario: ValueType-array to value-type array with no embedded gc-refs.
            nuint elementSize = sourceArray.ElementSize;

            SpanHelpers.Memmove(
                ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(destinationArray), (nuint)destinationIndex * elementSize),
                ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(sourceArray), (nuint)sourceIndex * elementSize),
                elementSize * (nuint)length);
        }

        //
        // Array.CopyImpl case: Primitive types that have a widening conversion
        //
        private static unsafe void CopyImplPrimitiveTypeWithWidening(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            MethodTable* sourceElementEEType = sourceArray.ElementMethodTable;
            MethodTable* destinationElementEEType = destinationArray.ElementMethodTable;

            Debug.Assert(sourceElementEEType->IsPrimitive && destinationElementEEType->IsPrimitive); // Caller has already validated this.

            EETypeElementType sourceElementType = sourceElementEEType->ElementType;
            EETypeElementType destElementType = destinationElementEEType->ElementType;

            nuint srcElementSize = sourceArray.ElementSize;
            nuint destElementSize = destinationArray.ElementSize;

            if ((sourceElementEEType->IsEnum || destinationElementEEType->IsEnum) && sourceElementType != destElementType)
                throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);

            if (reliable)
            {
                // ConstrainedCopy() cannot even widen - it can only copy same type or enum to its exact integral subtype.
                if (sourceElementType != destElementType)
                    throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_ConstrainedCopy);
            }

            ref byte srcData = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(sourceArray), (nuint)sourceIndex * srcElementSize);
            ref byte dstData = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(destinationArray), (nuint)destinationIndex * destElementSize);

            if (sourceElementType == destElementType)
            {
                // Multidim arrays and enum->int copies can still reach this path.
                SpanHelpers.Memmove(ref dstData, ref srcData, (nuint)length * srcElementSize);
                return;
            }

            if (!InvokeUtils.CanPrimitiveWiden(destElementType, sourceElementType))
            {
                throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);
            }

            for (int i = 0; i < length; i++)
            {
                InvokeUtils.PrimitiveWiden(destElementType, sourceElementType, ref dstData, ref srcData);
                srcData = ref Unsafe.AddByteOffset(ref srcData, srcElementSize);
                dstData = ref Unsafe.AddByteOffset(ref dstData, destElementSize);
            }
        }

        public static unsafe void Clear(Array array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            MethodTable* mt = array.GetMethodTable();
            nuint totalByteLength = mt->ComponentSize * array.NativeLength;
            ref byte pStart = ref MemoryMarshal.GetArrayDataReference(array);

            if (!mt->ContainsGCPointers)
            {
                SpanHelpers.ClearWithoutReferences(ref pStart, totalByteLength);
            }
            else
            {
                Debug.Assert(totalByteLength % (nuint)sizeof(IntPtr) == 0);
                SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref pStart), totalByteLength / (nuint)sizeof(IntPtr));
            }
        }

        public static unsafe void Clear(Array array, int index, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            ref byte p = ref Unsafe.As<RawArrayData>(array).Data;
            int lowerBound = 0;

            MethodTable* mt = array.GetMethodTable();
            if (!mt->IsSzArray)
            {
                int rank = mt->ArrayRank;
                lowerBound = Unsafe.Add(ref Unsafe.As<byte, int>(ref p), rank);
                p = ref Unsafe.Add(ref p, 2 * sizeof(int) * rank); // skip the bounds
            }

            int offset = index - lowerBound;

            if (index < lowerBound || offset < 0 || length < 0 || (uint)(offset + length) > array.NativeLength)
                ThrowHelper.ThrowIndexOutOfRangeException();

            nuint elementSize = mt->ComponentSize;

            ref byte ptr = ref Unsafe.AddByteOffset(ref p, (uint)offset * elementSize);
            nuint byteLength = (uint)length * elementSize;

            if (mt->ContainsGCPointers)
            {
                Debug.Assert(byteLength % (nuint)sizeof(IntPtr) == 0);
                SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref ptr), byteLength / (uint)sizeof(IntPtr));
            }
            else
            {
                SpanHelpers.ClearWithoutReferences(ref ptr, byteLength);
            }

            // GC.KeepAlive(array) not required. pMT kept alive via `ptr`
        }

        [Intrinsic]
        public int GetLength(int dimension)
        {
            int length = GetUpperBound(dimension) + 1;
            // We don't support non-zero lower bounds so don't incur the cost of obtaining it.
            Debug.Assert(GetLowerBound(dimension) == 0);
            return length;
        }

        public unsafe int Rank
        {
            get
            {
                return this.GetMethodTable()->ArrayRank;
            }
        }

        // Allocate new multidimensional array of given dimensions. Assumes that pLengths is immutable.
        internal static unsafe Array NewMultiDimArray(MethodTable* eeType, int* pLengths, int rank)
        {
            Debug.Assert(eeType->IsArray && !eeType->IsSzArray);
            Debug.Assert(rank == eeType->ArrayRank);

            // Code below assumes 0 lower bounds. MdArray of rank 1 with zero lower bounds should never be allocated.
            // The runtime always allocates an SzArray for those:
            // * newobj instance void int32[0...]::.ctor(int32)" actually gives you int[]
            // * int[] is castable to int[*] to make it mostly transparent
            // The callers need to check for this.
            Debug.Assert(rank != 1);

            ulong totalLength = 1;
            bool maxArrayDimensionLengthOverflow = false;

            for (int i = 0; i < rank; i++)
            {
                int length = pLengths[i];
                if (length < 0)
                    throw new OverflowException();
                if (length > MaxLength)
                    maxArrayDimensionLengthOverflow = true;
                totalLength *= (ulong)length;
                if (totalLength > int.MaxValue)
                    throw new OutOfMemoryException(); // "Array dimensions exceeded supported range."
            }

            // Throw this exception only after everything else was validated for backward compatibility.
            if (maxArrayDimensionLengthOverflow)
                throw new OutOfMemoryException(); // "Array dimensions exceeded supported range."

            Debug.Assert(eeType->NumVtableSlots != 0, "Compiler enforces we never have unconstructed MTs for multi-dim arrays since those can be template-constructed anytime");
            Array ret = RuntimeImports.RhNewVariableSizeObject(eeType, (int)totalLength);

            ref int bounds = ref ret.GetRawMultiDimArrayBounds();
            for (int i = 0; i < rank; i++)
            {
                Unsafe.Add(ref bounds, i) = pLengths[i];
            }

            return ret;
        }

        [Intrinsic]
        public int GetLowerBound(int dimension)
        {
            if (!IsSzArray)
            {
                int rank = Rank;
                if ((uint)dimension >= rank)
                    throw new IndexOutOfRangeException();

                return Unsafe.Add(ref GetRawMultiDimArrayBounds(), rank + dimension);
            }

            if (dimension != 0)
                throw new IndexOutOfRangeException();
            return 0;
        }

        [Intrinsic]
        public int GetUpperBound(int dimension)
        {
            if (!IsSzArray)
            {
                int rank = Rank;
                if ((uint)dimension >= rank)
                    throw new IndexOutOfRangeException();

                ref int bounds = ref GetRawMultiDimArrayBounds();

                int length = Unsafe.Add(ref bounds, dimension);
                int lowerBound = Unsafe.Add(ref bounds, rank + dimension);
                return length + lowerBound - 1;
            }

            if (dimension != 0)
                throw new IndexOutOfRangeException();
            return Length - 1;
        }

        private unsafe nint GetFlattenedIndex(int rawIndex)
        {
            // Checked by the caller
            Debug.Assert(Rank == 1);

            if (!IsSzArray)
            {
                ref int bounds = ref GetRawMultiDimArrayBounds();
                rawIndex -= Unsafe.Add(ref bounds, 1);
            }

            if ((uint)rawIndex >= NativeLength)
                ThrowHelper.ThrowIndexOutOfRangeException();
            return rawIndex;
        }

        internal unsafe nint GetFlattenedIndex(ReadOnlySpan<int> indices)
        {
            // Checked by the caller
            Debug.Assert(indices.Length == Rank);

            if (!IsSzArray)
            {
                ref int bounds = ref GetRawMultiDimArrayBounds();
                nint flattenedIndex = 0;
                for (int i = 0; i < indices.Length; i++)
                {
                    int index = indices[i] - Unsafe.Add(ref bounds, indices.Length + i);
                    int length = Unsafe.Add(ref bounds, i);
                    if ((uint)index >= (uint)length)
                        ThrowHelper.ThrowIndexOutOfRangeException();
                    flattenedIndex = (length * flattenedIndex) + index;
                }
                Debug.Assert((nuint)flattenedIndex < NativeLength);
                return flattenedIndex;
            }
            else
            {
                int index = indices[0];
                if ((uint)index >= NativeLength)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                return index;
            }
        }

        internal unsafe object? InternalGetValue(nint flattenedIndex)
        {
            Debug.Assert((nuint)flattenedIndex < NativeLength);

            if (ElementMethodTable->IsPointer || ElementMethodTable->IsFunctionPointer)
                throw new NotSupportedException(SR.NotSupported_Type);

            ref byte element = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(this), (nuint)flattenedIndex * ElementSize);

            MethodTable* pElementEEType = ElementMethodTable;
            if (pElementEEType->IsValueType)
            {
                return RuntimeExports.RhBox(pElementEEType, ref element);
            }
            else
            {
                Debug.Assert(!pElementEEType->IsPointer && !pElementEEType->IsFunctionPointer);
                return Unsafe.As<byte, object>(ref element);
            }
        }

        private unsafe void InternalSetValue(object? value, nint flattenedIndex)
        {
            Debug.Assert((nuint)flattenedIndex < NativeLength);

            ref byte element = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(this), (nuint)flattenedIndex * ElementSize);

            MethodTable* pElementEEType = ElementMethodTable;
            if (pElementEEType->IsValueType)
            {
                // Unlike most callers of InvokeUtils.ChangeType(), Array.SetValue() does *not* permit conversion from a primitive to an Enum.
                if (value != null && !(value.GetMethodTable() == pElementEEType) && pElementEEType->IsEnum)
                    throw new InvalidCastException(SR.Format(SR.Arg_ObjObjEx, value.GetType(), Type.GetTypeFromHandle(new RuntimeTypeHandle(pElementEEType))));

                value = InvokeUtils.CheckArgument(value, pElementEEType, InvokeUtils.CheckArgumentSemantics.ArraySet, binderBundle: null);
                Debug.Assert(value == null || RuntimeImports.AreTypesAssignable(value.GetMethodTable(), pElementEEType));

                RuntimeImports.RhUnbox(value, ref element, pElementEEType);
            }
            else if (pElementEEType->IsPointer || pElementEEType->IsFunctionPointer)
            {
                throw new NotSupportedException(SR.NotSupported_Type);
            }
            else
            {
                try
                {
                    RuntimeImports.RhCheckArrayStore(this, value);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new InvalidCastException(SR.InvalidCast_StoreArrayElement);
                }
                Unsafe.As<byte, object?>(ref element) = value;
            }
        }

        internal unsafe MethodTable* ElementMethodTable => this.GetMethodTable()->RelatedParameterType;

        internal unsafe CorElementType GetCorElementTypeOfElementType()
        {
            return new EETypePtr(ElementMethodTable).CorElementType;
        }

        internal unsafe bool IsValueOfElementType(object o)
        {
            return ElementMethodTable == o.GetMethodTable();
        }

        //
        // Return storage size of an individual element in bytes.
        //
        internal unsafe nuint ElementSize
        {
            get
            {
                return this.GetMethodTable()->ComponentSize;
            }
        }

        private static int IndexOfImpl<T>(T[] array, T value, int startIndex, int count)
        {
            // See comment in EqualityComparerHelpers.GetComparerForReferenceTypesOnly for details
            EqualityComparer<T> comparer = EqualityComparerHelpers.GetComparerForReferenceTypesOnly<T>();

            int endIndex = startIndex + count;
            if (comparer != null)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (comparer.Equals(array[i], value))
                        return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (EqualityComparerHelpers.StructOnlyEquals<T>(array[i], value))
                        return i;
                }
            }

            return -1;
        }

        private static int LastIndexOfImpl<T>(T[] array, T value, int startIndex, int count)
        {
            // See comment in EqualityComparerHelpers.GetComparerForReferenceTypesOnly for details
            EqualityComparer<T> comparer = EqualityComparerHelpers.GetComparerForReferenceTypesOnly<T>();

            int endIndex = startIndex - count + 1;
            if (comparer != null)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (comparer.Equals(array[i], value))
                        return i;
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (EqualityComparerHelpers.StructOnlyEquals<T>(array[i], value))
                        return i;
                }
            }

            return -1;
        }
    }

    //
    // Note: the declared base type and interface list also determines what Reflection returns from TypeInfo.BaseType and TypeInfo.ImplementedInterfaces for array types.
    //
    internal class Array<T> : Array, IEnumerable<T>, ICollection<T>, IList<T>, IReadOnlyList<T>
    {
        // Prevent the C# compiler from generating a public default constructor
        private Array() { }

        [Intrinsic]
        public new IEnumerator<T> GetEnumerator()
        {
            T[] @this = Unsafe.As<T[]>(this);
            // get length so we don't have to call the Length property again in ArrayEnumerator constructor
            // and avoid more checking there too.
            int length = @this.Length;
            return length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(@this, length);
        }

        public int Count
        {
            get
            {
                return Unsafe.As<T[]>(this).Length;
            }
        }

        //
        // Fun fact:
        //
        //  ((int[])a).IsReadOnly returns false.
        //  ((IList<int>)a).IsReadOnly returns true.
        //
        public new bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public void Add(T item)
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        public void Clear()
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        public bool Contains(T item)
        {
            T[] @this = Unsafe.As<T[]>(this);
            return Array.IndexOf(@this, item, 0, @this.Length) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            T[] @this = Unsafe.As<T[]>(this);
            Array.Copy(@this, 0, array, arrayIndex, @this.Length);
        }

        public bool Remove(T item)
        {
            ThrowHelper.ThrowNotSupportedException();
            return false; // unreachable
        }

        public T this[int index]
        {
            get
            {
                try
                {
                    return Unsafe.As<T[]>(this)[index];
                }
                catch (IndexOutOfRangeException)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
                    return default; // unreachable
                }
            }
            set
            {
                try
                {
                    Unsafe.As<T[]>(this)[index] = value;
                }
                catch (IndexOutOfRangeException)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
                }
            }
        }

        public int IndexOf(T item)
        {
            T[] @this = Unsafe.As<T[]>(this);
            return Array.IndexOf(@this, item, 0, @this.Length);
        }

        public void Insert(int index, T item)
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        public void RemoveAt(int index)
        {
            ThrowHelper.ThrowNotSupportedException();
        }
    }

    public class MDArray
    {
        public const int MinRank = 1;
        public const int MaxRank = 32;
    }
}
