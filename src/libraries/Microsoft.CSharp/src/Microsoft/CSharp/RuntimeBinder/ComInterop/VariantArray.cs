// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VariantArray1
    {
        public ComVariant Element0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VariantArray2
    {
        public ComVariant Element0, Element1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VariantArray4
    {
        public ComVariant Element0, Element1, Element2, Element3;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VariantArray8
    {
        public ComVariant Element0, Element1, Element2, Element3, Element4, Element5, Element6, Element7;
    }

    //
    // Helper for getting the right VariantArray struct for a given number of
    // arguments. Will generate a struct if needed.
    //
    // We use this because we don't have stackalloc or pinning in Expression
    // Trees, so we can't create an array of Variants directly.
    //
    internal static class VariantArray
    {
        // Don't need a dictionary for this, it will have very few elements
        // (guaranteed less than 28, in practice 0-2)
        private static readonly List<Type> s_generatedTypes = new List<Type>(0);

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields, typeof(VariantArray1))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields, typeof(VariantArray2))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields, typeof(VariantArray4))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields, typeof(VariantArray8))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Types are either dynamically created or have dynamic dependency.")]
        internal static MemberExpression GetStructField(ParameterExpression variantArray, int field)
        {
            return Expression.Field(variantArray, "Element" + field);
        }

        [RequiresDynamicCode(Binder.DynamicCodeWarning)]
        internal static Type GetStructType(int args)
        {
            Debug.Assert(args >= 0);
            if (args <= 1) return typeof(VariantArray1);
            if (args <= 2) return typeof(VariantArray2);
            if (args <= 4) return typeof(VariantArray4);
            if (args <= 8) return typeof(VariantArray8);

            int size = 1;
            while (args > size)
            {
                size *= 2;
            }

            lock (s_generatedTypes)
            {
                // See if we can find an existing type
                foreach (Type t in s_generatedTypes)
                {
                    int arity = int.Parse(t.Name.AsSpan("VariantArray".Length), provider: CultureInfo.InvariantCulture);
                    if (size == arity)
                    {
                        return t;
                    }
                }

                // Else generate a new type
                Type type = CreateCustomType(size);
                s_generatedTypes.Add(type);
                return type;
            }
        }

        [RequiresDynamicCode(Binder.DynamicCodeWarning)]
        private static Type CreateCustomType(int size)
        {
            TypeAttributes attrs = TypeAttributes.NotPublic | TypeAttributes.SequentialLayout;
            TypeBuilder type = UnsafeMethods.DynamicModule.DefineType("VariantArray" + size, attrs, typeof(ValueType));
            for (int i = 0; i < size; i++)
            {
                type.DefineField("Element" + i, typeof(ComVariant), FieldAttributes.Public);
            }
            return type.CreateType();
        }
    }
}
