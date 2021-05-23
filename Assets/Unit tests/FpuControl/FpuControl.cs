using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RobustArithmetic.Test.FpuControl
{

    // The FpuControl class allows for experimentation with different settings for the x87 FPU.
    // I have no idea when / if the .NET runtime explicitly sets or overrides these settings.
    // To use in an implementation (for example to do floating-point interval testing be switching rounding modes)
    // one should use the IDisposable FpuControlContext to wrap code in.

    // For now I just use it for testing and understanding the effects of different settings.

    // Info for the C runtime call: http://msdn.microsoft.com/en-us/library/c9676k6h.aspx
    // IA-32 reference:http://flint.cs.yale.edu/cs422/doc/24547012.pdf
    class FpuControl
    {
        // P/Invoke declare for the FPU control helper in the C runtime
        // errno_t __cdecl _controlfp_s(_Out_opt_ unsigned int *_CurrentState, _In_ unsigned int _NewValue, _In_ unsigned int _Mask);
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        static extern int _controlfp_s(out uint currentState, uint newValue, uint mask);

        [Flags]
        public enum Mask : uint
        {
            DenormalControl         = 0x03000000,
            InterruptExceptionMask  = 0x0008001F,
            InfinityControl         = 0x00040000,
            RoundingControl         = 0x00000300,
            PrecisionControl        = 0x00030000
        }

        public enum DenormalControl : uint
        {
            Save    = 0x00000000,
            Flush   = 0x01000000
        }

        // the control bits actually turn exceptions OFF, not ON !
        // "For the _MCW_EM mask, clearing the mask sets the exception, which allows the hardware exception; setting the mask hides the exception."
        [Flags]
        public enum ExceptionMask : uint
        {
            Invalid     = 0x00000010,
            Denormal    = 0x00080000,
            ZeroDivide  = 0x00000008,
            Overflow    = 0x00000004,
            Underflow   = 0x00000002,
            Inexact     = 0x00000001
        }

        // Wikipedia says a bit about this (http://en.wikipedia.org/wiki/IEEE_754-1985).
        // The Affine option has +infinity and -infinity, and is the only IEEE 754 option.
        // The Projective option has only -infinity.
        public enum InfinityControl : uint
        {
            Affine      = 0x00040000,
            Projective  = 0x00000000,
        }

        public enum RoundingControl : uint
        {
            Chop = 0x00000300,
            Up   = 0x00000200,
            Down = 0x00000100,
            Near = 0x00000000
        }

        public enum PrecisionControl : uint
        {
            Single24Bits    = 0x00020000,
            Double53Bits    = 0x00010000,
            Extended64Bits  = 0x00000000
        }

        public const uint ErrorAmbiguous = 0x80000000;

        // Returns the new state
        public static uint SetState(uint newValue, Mask mask)
        {
            uint state;
            int error = _controlfp_s(out state, newValue, (uint)mask);
            if (error == 0)
            {
                return state;
            }
            throw new Win32Exception(error);
        }

        public static uint GetState()
        {
            uint state;
            int error = _controlfp_s(out state, 0, 0);
            if (error == 0)
            {
                return state;
            }
            throw new Win32Exception(error);
        }

        public struct State
        {
            readonly uint _state;
            public State(uint state)
            {
                _state = state;
            }

            public PrecisionControl PrecisionControl
            {
                get
                {
                    return (PrecisionControl)(_state & (uint)Mask.PrecisionControl);
                }
            }

            public InfinityControl InfinityControl
            {
                get
                {
                    return (InfinityControl)(_state & (uint)Mask.InfinityControl);
                }
            }

            public RoundingControl RoundingControl
            {
                get
                {
                    return (RoundingControl)(_state & (uint)Mask.RoundingControl);
                }
            }

            public DenormalControl DenormalControl
            {
                get
                {
                    return (DenormalControl)(_state & (uint)Mask.DenormalControl);
                }
            }

            public ExceptionMask ExceptionMask
            {
                get
                {
                    return (ExceptionMask)(_state & (uint)Mask.InterruptExceptionMask);
                }
            }
        }
    }
}
