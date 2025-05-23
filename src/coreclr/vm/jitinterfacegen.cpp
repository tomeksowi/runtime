// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===========================================================================
// File: JITinterfaceGen.CPP
//
// This contains the AMD64 version of InitJITHelpers1().
//
// ===========================================================================


#include "common.h"
#include "clrtypes.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "comdelegate.h"
#include "field.h"
#include "ecall.h"
#include "writebarriermanager.h"

#ifdef HOST_64BIT

// These are the single-processor-optimized versions of the allocation helpers.
EXTERN_C Object* JIT_TrialAllocSFastSP(CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C Object* AllocateStringFastUP (CLR_I4 cch);

EXTERN_C Object* JIT_NewArr1OBJ_UP (CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);
EXTERN_C Object* JIT_NewArr1VC_UP (CORINFO_CLASS_HANDLE arrayMT, INT_PTR size);

#endif // HOST_64BIT

/*********************************************************************/
// Initialize the part of the JIT helpers that require very little of
// EE infrastructure to be in place.
/*********************************************************************/
#ifndef TARGET_X86

void InitJITHelpers1()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(g_SystemInfo.dwNumberOfProcessors != 0);

#if defined(TARGET_AMD64)

    g_WriteBarrierManager.Initialize();

    // Allocation helpers, faster but non-logging
    if (!((TrackAllocationsEnabled()) ||
        (LoggingOn(LF_GCALLOC, LL_INFO10))
#ifdef _DEBUG
        || (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP) != 0)
#endif // _DEBUG
        ))
    {
#ifdef TARGET_UNIX
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_NewS_MP_FastPortable);
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, JIT_NewS_MP_FastPortable);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_MP_FastPortable);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_MP_FastPortable);

        ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateString_MP_FastPortable), ECall::FastAllocateString);
#else // TARGET_UNIX
        // if (multi-proc || server GC)
        if (GCHeapUtilities::UseThreadAllocationContexts())
        {
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_NewS_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, JIT_NewS_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_MP_FastPortable);

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateString_MP_FastPortable), ECall::FastAllocateString);
        }
        else
        {
            // Replace the 1p slow allocation helpers with faster version
            //
            // When we're running Workstation GC on a single proc box we don't have
            // InlineGetThread versions because there is no need to call GetThread
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_TrialAllocSFastSP);
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, JIT_TrialAllocSFastSP);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_UP);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_UP);

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateStringFastUP), ECall::FastAllocateString);
        }
#endif // TARGET_UNIX
    }
#endif // TARGET_AMD64
}

#endif // !TARGET_X86
