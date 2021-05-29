using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsNav.Core
{
    [Serializable]
    unsafe struct IntPtr : ISerializable, IEquatable<IntPtr>
    {
        [NativeDisableUnsafePtrRestriction]
        private void* m_value;
        /// <summary><para>A read-only field that represents a pointer or handle that has been initialized to zero.</para></summary>
        public static readonly IntPtr Zero;

        /// <summary><para>Initializes a new instance of <see cref="T:System.IntPtr" /> using the specified 32-bit pointer or handle.</para></summary>
        /// <param name="value">A pointer or handle contained in a 32-bit signed integer. </param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public IntPtr(int value)
        {
            this.m_value = (void*) value;
        }

        /// <summary><para>Initializes a new instance of <see cref="T:System.IntPtr" /> using the specified 32-bit pointer or handle.</para></summary>
        /// <param name="value">A pointer or handle contained in a 32-bit signed integer. </param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public IntPtr(long value)
        {
            this.m_value = (void*) value;
        }

        /// <summary><para>Initializes a new instance of <see cref="T:System.IntPtr" /> using the specified 32-bit pointer or handle.</para></summary>
        /// <param name="value">A pointer or handle contained in a 32-bit signed integer. </param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public IntPtr(void* value)
        {
            this.m_value = value;
        }

        /// <summary><para>Initializes a new instance of <see cref="T:System.IntPtr" /> using the specified 32-bit pointer or handle.</para></summary>
        /// <param name="value">A pointer or handle contained in a 32-bit signed integer. </param>
        private IntPtr(SerializationInfo info, StreamingContext context)
        {
            this.m_value = (void*) info.GetInt64("value");
        }

        /// <summary><para>Gets the size of this instance.</para></summary>
        public static int Size
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] get
            {
                return sizeof (void*);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof (info));
            info.AddValue("value", this.ToInt64());
        }

        public bool Equals(IntPtr other) => m_value == other.m_value;

        /// <summary><para>Returns a value indicating whether this instance is equal to a specified object.</para></summary>
        /// <param name="obj">An object to compare with this instance or null. </param>
        public override bool Equals(object obj)
        {
            return obj is IntPtr num && num.m_value == this.m_value;
        }

        /// <summary><para>Returns the hash code for this instance.</para></summary>
        public override int GetHashCode()
        {
            return (int) this.m_value;
        }

        /// <summary><para>Converts the value of this instance to a 32-bit signed integer.</para></summary>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public int ToInt32()
        {
            return (int) this.m_value;
        }

        /// <summary><para>Converts the value of this instance to a 64-bit signed integer.</para></summary>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public long ToInt64()
        {
            return IntPtr.Size == 4 ? (long) (int) this.m_value : (long) this.m_value;
        }

        /// <summary><para>Converts the value of this instance to a pointer to an unspecified type.</para></summary>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void* ToPointer()
        {
            return this.m_value;
        }

        /// <summary><para>Converts the numeric value of the current <see cref="T:System.IntPtr" /> object to its equivalent string representation.</para></summary>
        public override string ToString()
        {
            return this.ToString((string) null);
        }

        /// <summary><para>Converts the numeric value of the current <see cref="T:System.IntPtr" /> object to its equivalent string representation.</para></summary>
        public string ToString(string format)
        {
            return IntPtr.Size == 4 ? ((int) this.m_value).ToString(format, (IFormatProvider) null) : ((long) this.m_value).ToString(format, (IFormatProvider) null);
        }

        /// <summary><para>Determines whether two specified instances of <see cref="T:System.IntPtr" /> are equal.</para></summary>
        /// <param name="value1">The first pointer or handle to compare.</param>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static bool operator ==(IntPtr value1, IntPtr value2)
        {
            return value1.m_value == value2.m_value;
        }

        /// <summary><para>Determines whether two specified instances of <see cref="T:System.IntPtr" /> are not equal.</para></summary>
        /// <param name="value1">The first pointer or handle to compare. </param>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static bool operator !=(IntPtr value1, IntPtr value2)
        {
            return value1.m_value != value2.m_value;
        }

        /// <summary>To be added.</summary>
        /// <param name="value">To be added.</param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static explicit operator IntPtr(int value)
        {
            return new IntPtr(value);
        }

        /// <summary>To be added.</summary>
        /// <param name="value">To be added.</param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static explicit operator IntPtr(long value)
        {
            return new IntPtr(value);
        }

        /// <summary>To be added.</summary>
        /// <param name="value">To be added.</param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static explicit operator IntPtr(void* value)
        {
            return new IntPtr(value);
        }

        /// <summary>To be added.</summary>
        /// <param name="value">To be added.</param>
        public static explicit operator int(IntPtr value)
        {
            return (int) value.m_value;
        }

        /// <summary>To be added.</summary>
        /// <param name="value">To be added.</param>
        public static explicit operator long(IntPtr value)
        {
            return value.ToInt64();
        }

        /// <summary>To be added.</summary>
        /// <param name="value">To be added.</param>
        public static explicit operator void*(IntPtr value)
        {
            return value.m_value;
        }

        /// <summary><para>Adds an offset to the value of a pointer.</para></summary>
        /// <param name="pointer">The pointer to add the offset to.</param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static IntPtr Add(IntPtr pointer, int offset)
        {
            return (IntPtr) (void*) ((IntPtr) (void*) pointer + offset);
        }

        /// <summary><para>Subtracts an offset from the value of a pointer.</para></summary>
        /// <param name="pointer">The pointer to subtract the offset from.</param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static IntPtr Subtract(IntPtr pointer, int offset)
        {
            return (IntPtr) (void*) ((IntPtr) (void*) pointer - offset);
        }

        /// <summary><para>Adds an offset to the value of a pointer.</para></summary>
        /// <param name="pointer">The pointer to add the offset to.</param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static IntPtr operator +(IntPtr pointer, int offset)
        {
            return (IntPtr) (void*) ((IntPtr) (void*) pointer + offset);
        }

        /// <summary><para>Subtracts an offset from the value of a pointer.</para></summary>
        /// <param name="pointer">The pointer to subtract the offset from.</param>
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static IntPtr operator -(IntPtr pointer, int offset)
        {
            return (IntPtr) (void*) ((IntPtr) (void*) pointer - offset);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal bool IsNull()
        {
            return (IntPtr) this.m_value == IntPtr.Zero;
        }
    }
}