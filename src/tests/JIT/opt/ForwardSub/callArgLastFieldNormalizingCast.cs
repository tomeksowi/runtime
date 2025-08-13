// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;

struct S
{
    public int I0, I1;
    public long Value;

    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static S ZeroExt(uint value) => new S {I0 = 0, I1 = 0, Value = value};
    
    [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static S SignExt(uint value) => new S {I0 = 0, I1 = 0, Value = (int)value};
}

public class ForwardSubCallArgLastFieldNormalizingCast
{
    [Fact]
    public static void Test()
    {
        uint arg = uint.MaxValue;
        Assert.Equal(S.ZeroExt(arg).Value, (long)arg);
        Assert.Equal(S.SignExt(arg).Value, (long)(int)arg);
    }
}
