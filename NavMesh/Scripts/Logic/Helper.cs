using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

namespace CCengine
{
    public static class EntityIdGenerator
    {
        private static int nextId = 1;

        public static string GetNextId()
        {
            return "hero" + nextId++;
        }
    }

    #region Serializable Vector3

    [System.Serializable]
    public struct SLVector3 : INetworkSerializable // SL = Serializable
    {
        public float x;
        public float z;

        public SLVector3(float x, float z)
        {
            this.x = x;
            this.z = z;
        }

        // Implicit conversion to Unity's Vector3
        public static implicit operator UnityEngine.Vector3(SLVector3 sv3) =>
            new UnityEngine.Vector3(sv3.x, 0f, sv3.z);

        // Implicit conversion from Unity's Vector3
        public static implicit operator SLVector3(UnityEngine.Vector3 v3) =>
            new SLVector3(v3.x, v3.z);

        // Network Serialization
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref z);
        }

        // Properties
        public float Magnitude => Mathf.Sqrt(x * x + z * z);

        public float SqrMagnitude => x * x + z * z;

        public SLVector3 normalized
        {
            get
            {
                float magnitude = Magnitude;
                return magnitude > 0 ? this / magnitude : Zero;
            }
        }

        // Static properties
        public static SLVector3 Zero => new SLVector3(0f, 0f);
        public static SLVector3 One => new SLVector3(1f, 1f);
        public static SLVector3 Forward => new SLVector3(0f, 1f);
        public static SLVector3 Backward => new SLVector3(0f, -1f);
        public static SLVector3 Left => new SLVector3(-1f, 0f);
        public static SLVector3 Right => new SLVector3(1f, 0f);

        // Vector Operations
        public static SLVector3 operator +(SLVector3 a, SLVector3 b) =>
            new SLVector3(a.x + b.x, a.z + b.z);

        public static SLVector3 operator -(SLVector3 a, SLVector3 b) =>
            new SLVector3(a.x - b.x, a.z - b.z);

        public static SLVector3 operator -(SLVector3 a) =>
            new SLVector3(-a.x, -a.z);

        public static SLVector3 operator *(SLVector3 a, float scalar) =>
            new SLVector3(a.x * scalar, a.z * scalar);

        public static SLVector3 operator *(float scalar, SLVector3 a) =>
            new SLVector3(a.x * scalar, a.z * scalar);

        public static SLVector3 operator /(SLVector3 a, float scalar)
        {
            if (scalar == 0) throw new DivideByZeroException("Division by zero.");
            return new SLVector3(a.x / scalar, a.z / scalar);
        }

        // Equality
        public static bool operator ==(SLVector3 a, SLVector3 b) =>
            Math.Abs(a.x - b.x) < Mathf.Epsilon && Math.Abs(a.z - b.z) < Mathf.Epsilon;

        public static bool operator !=(SLVector3 a, SLVector3 b) =>
            !(a == b);

        public override bool Equals(object obj) =>
            obj is SLVector3 other && this == other;

        public override int GetHashCode() => HashCode.Combine(x, z);

        // Distance between two vectors
        public static float Distance(SLVector3 a, SLVector3 b) =>
            (a - b).Magnitude;

        // Dot product
        public static float Dot(SLVector3 a, SLVector3 b) =>
            a.x * b.x + a.z * b.z;

        // Lerp (Linear Interpolation)
        public static SLVector3 Lerp(SLVector3 a, SLVector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new SLVector3(
                Mathf.Lerp(a.x, b.x, t),
                Mathf.Lerp(a.z, b.z, t)
            );
        }

        // Reflect
        public static SLVector3 Reflect(SLVector3 inDirection, SLVector3 inNormal)
        {
            float dot = Dot(inDirection, inNormal);
            return inDirection - 2f * dot * inNormal;
        }

        // ToString for debugging
        public override string ToString() => $"SLVector3({x}, 0, {z})";
    }
    #endregion

}