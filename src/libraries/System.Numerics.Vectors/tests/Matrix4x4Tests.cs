// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tests
{
    public sealed class Matrix4x4Tests
    {
        private static Matrix4x4 GenerateIncrementalMatrixNumber(float value = 0.0f)
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = value + 1.0f;
            a.M12 = value + 2.0f;
            a.M13 = value + 3.0f;
            a.M14 = value + 4.0f;
            a.M21 = value + 5.0f;
            a.M22 = value + 6.0f;
            a.M23 = value + 7.0f;
            a.M24 = value + 8.0f;
            a.M31 = value + 9.0f;
            a.M32 = value + 10.0f;
            a.M33 = value + 11.0f;
            a.M34 = value + 12.0f;
            a.M41 = value + 13.0f;
            a.M42 = value + 14.0f;
            a.M43 = value + 15.0f;
            a.M44 = value + 16.0f;
            return a;
        }

        private static Matrix4x4 GenerateTestMatrix()
        {
            Matrix4x4 m =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));
            m.Translation = new Vector3(111.0f, 222.0f, 333.0f);
            return m;
        }

        private static Matrix4x4 DefaultVarianceMatrix = GenerateFilledMatrix(1e-5f);

        private static Matrix4x4 GenerateFilledMatrix(float value) => new Matrix4x4
        {
            M11 = value,
            M12 = value,
            M13 = value,
            M14 = value,
            M21 = value,
            M22 = value,
            M23 = value,
            M24 = value,
            M31 = value,
            M32 = value,
            M33 = value,
            M34 = value,
            M41 = value,
            M42 = value,
            M43 = value,
            M44 = value
        };

        private static Vector3 InverseHandedness(Vector3 vector) => new Vector3(vector.X, vector.Y, -vector.Z);

        // The handedness-swapped matrix of matrix M is B^-1 * M * B where B is the change of handedness matrix.
        // Since only the Z coordinate is flipped when changing handedness,
        // 
        // B = [ 1  0  0  0
        //       0  1  0  0
        //       0  0 -1  0
        //       0  0  0  1 ]
        //
        // and B is its own inverse. So the handedness swap can be simplified to
        // 
        // B^-1 * M * B = [  m11  m12  -m13  m14
        //                   m21  m22  -m23  m24
        //                  -m31 -m32   m33 -m34
        //                   m41  m42  -m43  m44 ]
        private static Matrix4x4 InverseHandedness(Matrix4x4 matrix) => new Matrix4x4(
             matrix.M11,  matrix.M12, -matrix.M13,  matrix.M14,
             matrix.M21,  matrix.M22, -matrix.M23,  matrix.M24,
            -matrix.M31, -matrix.M32,  matrix.M33, -matrix.M34,
             matrix.M41,  matrix.M42, -matrix.M43,  matrix.M44);

        private static void AssertEqual(Matrix4x4 expected, Matrix4x4 actual, Matrix4x4 variance)
        {
            for (var r = 0; r < 4; r++)
                for (var c = 0; c < 4; c++)
                    AssertExtensions.Equal(expected[r, c], actual[r, c], variance[r, c], $"Values differ at Matrix4x4.M{r + 1}{c + 1}");
        }

        [Theory]
        [InlineData(0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f)]
        [InlineData(1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f)]
        [InlineData(3.1434343f, 1.1234123f, 0.1234123f, -0.1234123f, 3.1434343f, 1.1234123f, 3.1434343f, 1.1234123f, 0.1234123f, -0.1234123f, 3.1434343f, 1.1234123f, 3.1434343f, 1.1234123f, 0.1234123f, -0.1234123f)]
        [InlineData(1.0000001f, 0.0000001f, 2.0000001f, 0.0000002f, 1.0000001f, 0.0000001f, 1.0000001f, 0.0000001f, 2.0000001f, 0.0000002f, 1.0000001f, 0.0000001f, 1.0000001f, 0.0000001f, 2.0000001f, 0.0000002f)]
        public void Matrix4x4IndexerGetTest(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
        {
            var matrix = new Matrix4x4(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);

            Assert.Equal(m11, matrix[0, 0]);
            Assert.Equal(m12, matrix[0, 1]);
            Assert.Equal(m13, matrix[0, 2]);
            Assert.Equal(m14, matrix[0, 3]);

            Assert.Equal(m21, matrix[1, 0]);
            Assert.Equal(m22, matrix[1, 1]);
            Assert.Equal(m23, matrix[1, 2]);
            Assert.Equal(m24, matrix[1, 3]);

            Assert.Equal(m31, matrix[2, 0]);
            Assert.Equal(m32, matrix[2, 1]);
            Assert.Equal(m33, matrix[2, 2]);
            Assert.Equal(m34, matrix[2, 3]);

            Assert.Equal(m41, matrix[3, 0]);
            Assert.Equal(m42, matrix[3, 1]);
            Assert.Equal(m43, matrix[3, 2]);
            Assert.Equal(m44, matrix[3, 3]);
        }

        [Theory]
        [InlineData(0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f)]
        [InlineData(1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f)]
        [InlineData(3.1434343f, 1.1234123f, 0.1234123f, -0.1234123f, 3.1434343f, 1.1234123f, 3.1434343f, 1.1234123f, 0.1234123f, -0.1234123f, 3.1434343f, 1.1234123f, 3.1434343f, 1.1234123f, 0.1234123f, -0.1234123f)]
        [InlineData(1.0000001f, 0.0000001f, 2.0000001f, 0.0000002f, 1.0000001f, 0.0000001f, 1.0000001f, 0.0000001f, 2.0000001f, 0.0000002f, 1.0000001f, 0.0000001f, 1.0000001f, 0.0000001f, 2.0000001f, 0.0000002f)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/80876", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void Matrix4x4IndexerSetTest(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
        {
            var matrix = new Matrix4x4(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);

            matrix[0, 0] = m11;
            matrix[0, 1] = m12;
            matrix[0, 2] = m13;
            matrix[0, 3] = m14;

            matrix[1, 0] = m21;
            matrix[1, 1] = m22;
            matrix[1, 2] = m23;
            matrix[1, 3] = m24;

            matrix[2, 0] = m31;
            matrix[2, 1] = m32;
            matrix[2, 2] = m33;
            matrix[2, 3] = m34;

            matrix[3, 0] = m41;
            matrix[3, 1] = m42;
            matrix[3, 2] = m43;
            matrix[3, 3] = m44;

            Assert.Equal(m11, matrix[0, 0]);
            Assert.Equal(m12, matrix[0, 1]);
            Assert.Equal(m13, matrix[0, 2]);
            Assert.Equal(m14, matrix[0, 3]);

            Assert.Equal(m21, matrix[1, 0]);
            Assert.Equal(m22, matrix[1, 1]);
            Assert.Equal(m23, matrix[1, 2]);
            Assert.Equal(m24, matrix[1, 3]);

            Assert.Equal(m31, matrix[2, 0]);
            Assert.Equal(m32, matrix[2, 1]);
            Assert.Equal(m33, matrix[2, 2]);
            Assert.Equal(m34, matrix[2, 3]);

            Assert.Equal(m41, matrix[3, 0]);
            Assert.Equal(m42, matrix[3, 1]);
            Assert.Equal(m43, matrix[3, 2]);
            Assert.Equal(m44, matrix[3, 3]);
        }

        // A test for Identity
        [Fact]
        public void Matrix4x4IdentityTest()
        {
            Matrix4x4 val = new Matrix4x4();
            val.M11 = val.M22 = val.M33 = val.M44 = 1.0f;

            Assert.True(MathHelper.Equal(val, Matrix4x4.Identity), "Matrix4x4.Indentity was not set correctly.");
        }

        // A test for Determinant
        [Fact]
        public void Matrix4x4DeterminantTest()
        {
            Matrix4x4 target =
                    Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                    Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                    Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));

            float val = 1.0f;
            float det = target.GetDeterminant();

            Assert.True(MathHelper.Equal(val, det), "Matrix4x4.Determinant was not set correctly.");
        }

        // A test for Determinant
        // Determinant test |A| = 1 / |A'|
        [Fact]
        public void Matrix4x4DeterminantTest1()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 5.0f;
            a.M12 = 2.0f;
            a.M13 = 8.25f;
            a.M14 = 1.0f;
            a.M21 = 12.0f;
            a.M22 = 6.8f;
            a.M23 = 2.14f;
            a.M24 = 9.6f;
            a.M31 = 6.5f;
            a.M32 = 1.0f;
            a.M33 = 3.14f;
            a.M34 = 2.22f;
            a.M41 = 0f;
            a.M42 = 0.86f;
            a.M43 = 4.0f;
            a.M44 = 1.0f;
            Matrix4x4 i;
            Assert.True(Matrix4x4.Invert(a, out i));

            float detA = a.GetDeterminant();
            float detI = i.GetDeterminant();
            float t = 1.0f / detI;

            // only accurate to 3 precision
            Assert.True(System.Math.Abs(detA - t) < 1e-3, "Matrix4x4.Determinant was not set correctly.");
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertTest()
        {
            Matrix4x4 mtx =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.74999994f;
            expected.M12 = -0.216506317f;
            expected.M13 = 0.62499994f;
            expected.M14 = 0.0f;

            expected.M21 = 0.433012635f;
            expected.M22 = 0.87499994f;
            expected.M23 = -0.216506317f;
            expected.M24 = 0.0f;

            expected.M31 = -0.49999997f;
            expected.M32 = 0.433012635f;
            expected.M33 = 0.74999994f;
            expected.M34 = 0.0f;

            expected.M41 = 0.0f;
            expected.M42 = 0.0f;
            expected.M43 = 0.0f;
            expected.M44 = 0.99999994f;

            Matrix4x4 actual;

            Assert.True(Matrix4x4.Invert(mtx, out actual));
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Invert did not return the expected value.");

            // Make sure M*M is identity matrix
            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity), "Matrix4x4.Invert did not return the expected value.");
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertIdentityTest()
        {
            Matrix4x4 mtx = Matrix4x4.Identity;

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Assert.True(MathHelper.Equal(actual, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertTranslationTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreateTranslation(23, 42, 666);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertRotationTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreateFromYawPitchRoll(3, 4, 5);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertScaleTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreateScale(23, 42, -666);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertProjectionTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(1, 1.333f, 0.1f, 666);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertAffineTest()
        {
            Matrix4x4 mtx = Matrix4x4.CreateFromYawPitchRoll(3, 4, 5) *
                            Matrix4x4.CreateScale(23, 42, -666) *
                            Matrix4x4.CreateTranslation(17, 53, 89);

            Matrix4x4 actual;
            Assert.True(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.True(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        // A test for Invert (Matrix4x4)
        [Fact]
        public void Matrix4x4InvertRank3()
        {
            // A 4x4 Matrix having a rank of 3
            Matrix4x4 mtx = new Matrix4x4(1.0f, 2.0f, 3.0f, 0.0f,
                                          5.0f, 1.0f, 6.0f, 0.0f,
                                          8.0f, 9.0f, 1.0f, 0.0f,
                                          4.0f, 7.0f, 3.0f, 0.0f);

            Matrix4x4 actual;
            Assert.False(Matrix4x4.Invert(mtx, out actual));

            Matrix4x4 i = mtx * actual;
            Assert.False(MathHelper.Equal(i, Matrix4x4.Identity));
        }

        void DecomposeTest(float yaw, float pitch, float roll, Vector3 expectedTranslation, Vector3 expectedScales)
        {
            Quaternion expectedRotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(yaw),
                                                                            MathHelper.ToRadians(pitch),
                                                                            MathHelper.ToRadians(roll));

            Matrix4x4 m = Matrix4x4.CreateScale(expectedScales) *
                          Matrix4x4.CreateFromQuaternion(expectedRotation) *
                          Matrix4x4.CreateTranslation(expectedTranslation);

            Vector3 scales;
            Quaternion rotation;
            Vector3 translation;

            bool actualResult = Matrix4x4.Decompose(m, out scales, out rotation, out translation);
            Assert.True(actualResult, "Matrix4x4.Decompose did not return expected value.");

            bool scaleIsZeroOrNegative = expectedScales.X <= 0 ||
                                         expectedScales.Y <= 0 ||
                                         expectedScales.Z <= 0;

            if (scaleIsZeroOrNegative)
            {
                Assert.True(MathHelper.Equal(Math.Abs(expectedScales.X), Math.Abs(scales.X)), "Matrix4x4.Decompose did not return expected value.");
                Assert.True(MathHelper.Equal(Math.Abs(expectedScales.Y), Math.Abs(scales.Y)), "Matrix4x4.Decompose did not return expected value.");
                Assert.True(MathHelper.Equal(Math.Abs(expectedScales.Z), Math.Abs(scales.Z)), "Matrix4x4.Decompose did not return expected value.");
            }
            else
            {
                Assert.True(MathHelper.Equal(expectedScales, scales), string.Format("Matrix4x4.Decompose did not return expected value Expected:{0} actual:{1}.", expectedScales, scales));
                Assert.True(MathHelper.EqualRotation(expectedRotation, rotation), string.Format("Matrix4x4.Decompose did not return expected value. Expected:{0} actual:{1}.", expectedRotation, rotation));
            }

            Assert.True(MathHelper.Equal(expectedTranslation, translation), string.Format("Matrix4x4.Decompose did not return expected value. Expected:{0} actual:{1}.", expectedTranslation, translation));
        }

        // Various rotation decompose test.
        [Fact]
        public void Matrix4x4DecomposeTest01()
        {
            DecomposeTest(10.0f, 20.0f, 30.0f, new Vector3(10, 20, 30), new Vector3(2, 3, 4));

            const float step = 35.0f;

            for (float yawAngle = -720.0f; yawAngle <= 720.0f; yawAngle += step)
            {
                for (float pitchAngle = -720.0f; pitchAngle <= 720.0f; pitchAngle += step)
                {
                    for (float rollAngle = -720.0f; rollAngle <= 720.0f; rollAngle += step)
                    {
                        DecomposeTest(yawAngle, pitchAngle, rollAngle, new Vector3(10, 20, 30), new Vector3(2, 3, 4));
                    }
                }
            }
        }

        // Various scaled matrix decompose test.
        [Fact]
        public void Matrix4x4DecomposeTest02()
        {
            DecomposeTest(10.0f, 20.0f, 30.0f, new Vector3(10, 20, 30), new Vector3(2, 3, 4));

            // Various scales.
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(1, 2, 3));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(1, 3, 2));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(2, 1, 3));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(2, 3, 1));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(3, 1, 2));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(3, 2, 1));

            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(-2, 1, 1));

            // Small scales.
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(1e-4f, 2e-4f, 3e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(1e-4f, 3e-4f, 2e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(2e-4f, 1e-4f, 3e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(2e-4f, 3e-4f, 1e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(3e-4f, 1e-4f, 2e-4f));
            DecomposeTest(0, 0, 0, Vector3.Zero, new Vector3(3e-4f, 2e-4f, 1e-4f));

            // Zero scales.
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(0, 0, 0));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, 0, 0));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(0, 1, 0));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(0, 0, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(0, 1, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, 0, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, 1, 0));

            // Negative scales.
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(-1, -1, -1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, -1, -1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(-1, 1, -1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(-1, -1, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(-1, 1, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, -1, 1));
            DecomposeTest(0, 0, 0, new Vector3(10, 20, 30), new Vector3(1, 1, -1));
        }

        void DecomposeScaleTest(float sx, float sy, float sz)
        {
            Matrix4x4 m = Matrix4x4.CreateScale(sx, sy, sz);

            Vector3 expectedScales = new Vector3(sx, sy, sz);
            Vector3 scales;
            Quaternion rotation;
            Vector3 translation;

            bool actualResult = Matrix4x4.Decompose(m, out scales, out rotation, out translation);
            Assert.True(actualResult, "Matrix4x4.Decompose did not return expected value.");
            Assert.True(MathHelper.Equal(expectedScales, scales), "Matrix4x4.Decompose did not return expected value.");
            Assert.True(MathHelper.EqualRotation(Quaternion.Identity, rotation), "Matrix4x4.Decompose did not return expected value.");
            Assert.True(MathHelper.Equal(Vector3.Zero, translation), "Matrix4x4.Decompose did not return expected value.");
        }

        // Tiny scale decompose test.
        [Fact]
        public void Matrix4x4DecomposeTest03()
        {
            DecomposeScaleTest(1, 2e-4f, 3e-4f);
            DecomposeScaleTest(1, 3e-4f, 2e-4f);
            DecomposeScaleTest(2e-4f, 1, 3e-4f);
            DecomposeScaleTest(2e-4f, 3e-4f, 1);
            DecomposeScaleTest(3e-4f, 1, 2e-4f);
            DecomposeScaleTest(3e-4f, 2e-4f, 1);
        }

        [Fact]
        public void Matrix4x4DecomposeTest04()
        {
            Vector3 scales;
            Quaternion rotation;
            Vector3 translation;

            Assert.False(Matrix4x4.Decompose(GenerateIncrementalMatrixNumber(), out scales, out rotation, out translation), "decompose should have failed.");
            Assert.False(Matrix4x4.Decompose(new Matrix4x4(Matrix3x2.CreateSkew(1, 2)), out scales, out rotation, out translation), "decompose should have failed.");
        }

        // Transform by quaternion test
        [Fact]
        public void Matrix4x4TransformTest()
        {
            Matrix4x4 target = GenerateIncrementalMatrixNumber();

            Matrix4x4 m =
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(30.0f)) *
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(30.0f));

            Quaternion q = Quaternion.CreateFromRotationMatrix(m);

            Matrix4x4 expected = target * m;
            Matrix4x4 actual;
            actual = Matrix4x4.Transform(target, q);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Transform did not return the expected value.");
        }

        // A test for CreateRotationX (float)
        [Fact]
        public void Matrix4x4CreateRotationXTest()
        {
            float radians = MathHelper.ToRadians(30.0f);

            Matrix4x4 expected = new Matrix4x4();

            expected.M11 = 1.0f;
            expected.M22 = 0.8660254f;
            expected.M23 = 0.5f;
            expected.M32 = -0.5f;
            expected.M33 = 0.8660254f;
            expected.M44 = 1.0f;

            Matrix4x4 actual;

            actual = Matrix4x4.CreateRotationX(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationX did not return the expected value.");
        }

        // A test for CreateRotationX (float)
        // CreateRotationX of zero degree
        [Fact]
        public void Matrix4x4CreateRotationXTest1()
        {
            float radians = 0;

            Matrix4x4 expected = Matrix4x4.Identity;
            Matrix4x4 actual = Matrix4x4.CreateRotationX(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationX did not return the expected value.");
        }

        // A test for CreateRotationX (float, Vector3f)
        [Fact]
        public void Matrix4x4CreateRotationXCenterTest()
        {
            float radians = MathHelper.ToRadians(30.0f);
            Vector3 center = new Vector3(23, 42, 66);

            Matrix4x4 rotateAroundZero = Matrix4x4.CreateRotationX(radians, Vector3.Zero);
            Matrix4x4 rotateAroundZeroExpected = Matrix4x4.CreateRotationX(radians);
            Assert.True(MathHelper.Equal(rotateAroundZero, rotateAroundZeroExpected));

            Matrix4x4 rotateAroundCenter = Matrix4x4.CreateRotationX(radians, center);
            Matrix4x4 rotateAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateRotationX(radians) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(rotateAroundCenter, rotateAroundCenterExpected));
        }

        // A test for CreateRotationY (float)
        [Fact]
        public void Matrix4x4CreateRotationYTest()
        {
            float radians = MathHelper.ToRadians(60.0f);

            Matrix4x4 expected = new Matrix4x4();

            expected.M11 = 0.49999997f;
            expected.M13 = -0.866025448f;
            expected.M22 = 1.0f;
            expected.M31 = 0.866025448f;
            expected.M33 = 0.49999997f;
            expected.M44 = 1.0f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreateRotationY(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationY did not return the expected value.");
        }

        // A test for RotationY (float)
        // CreateRotationY test for negative angle
        [Fact]
        public void Matrix4x4CreateRotationYTest1()
        {
            float radians = MathHelper.ToRadians(-300.0f);

            Matrix4x4 expected = new Matrix4x4();

            expected.M11 = 0.49999997f;
            expected.M13 = -0.866025448f;
            expected.M22 = 1.0f;
            expected.M31 = 0.866025448f;
            expected.M33 = 0.49999997f;
            expected.M44 = 1.0f;

            Matrix4x4 actual = Matrix4x4.CreateRotationY(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationY did not return the expected value.");
        }

        // A test for CreateRotationY (float, Vector3f)
        [Fact]
        public void Matrix4x4CreateRotationYCenterTest()
        {
            float radians = MathHelper.ToRadians(30.0f);
            Vector3 center = new Vector3(23, 42, 66);

            Matrix4x4 rotateAroundZero = Matrix4x4.CreateRotationY(radians, Vector3.Zero);
            Matrix4x4 rotateAroundZeroExpected = Matrix4x4.CreateRotationY(radians);
            Assert.True(MathHelper.Equal(rotateAroundZero, rotateAroundZeroExpected));

            Matrix4x4 rotateAroundCenter = Matrix4x4.CreateRotationY(radians, center);
            Matrix4x4 rotateAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateRotationY(radians) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(rotateAroundCenter, rotateAroundCenterExpected));
        }

        // A test for CreateFromAxisAngle(Vector3f,float)
        [Fact]
        public void Matrix4x4CreateFromAxisAngleTest()
        {
            float radians = MathHelper.ToRadians(-30.0f);

            Matrix4x4 expected = Matrix4x4.CreateRotationX(radians);
            Matrix4x4 actual = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, radians);
            Assert.True(MathHelper.Equal(expected, actual));

            expected = Matrix4x4.CreateRotationY(radians);
            actual = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, radians);
            Assert.True(MathHelper.Equal(expected, actual));

            expected = Matrix4x4.CreateRotationZ(radians);
            actual = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, radians);
            Assert.True(MathHelper.Equal(expected, actual));

            expected = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.One), radians));
            actual = Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(Vector3.One), radians);
            Assert.True(MathHelper.Equal(expected, actual));

            const int rotCount = 16;
            for (int i = 0; i < rotCount; ++i)
            {
                float latitude = (2.0f * MathHelper.Pi) * ((float)i / (float)rotCount);
                for (int j = 0; j < rotCount; ++j)
                {
                    float longitude = -MathHelper.PiOver2 + MathHelper.Pi * ((float)j / (float)rotCount);

                    Matrix4x4 m = Matrix4x4.CreateRotationZ(longitude) * Matrix4x4.CreateRotationY(latitude);
                    Vector3 axis = new Vector3(m.M11, m.M12, m.M13);
                    for (int k = 0; k < rotCount; ++k)
                    {
                        float rot = (2.0f * MathHelper.Pi) * ((float)k / (float)rotCount);
                        expected = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(axis, rot));
                        actual = Matrix4x4.CreateFromAxisAngle(axis, rot);
                        Assert.True(MathHelper.Equal(expected, actual));
                    }
                }
            }
        }

        [Fact]
        public void Matrix4x4CreateFromYawPitchRollTest1()
        {
            float yawAngle = MathHelper.ToRadians(30.0f);
            float pitchAngle = MathHelper.ToRadians(40.0f);
            float rollAngle = MathHelper.ToRadians(50.0f);

            Matrix4x4 yaw = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, yawAngle);
            Matrix4x4 pitch = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, pitchAngle);
            Matrix4x4 roll = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rollAngle);

            Matrix4x4 expected = roll * pitch * yaw;
            Matrix4x4 actual = Matrix4x4.CreateFromYawPitchRoll(yawAngle, pitchAngle, rollAngle);
            Assert.True(MathHelper.Equal(expected, actual));
        }

        // Covers more numeric rigions
        [Fact]
        public void Matrix4x4CreateFromYawPitchRollTest2()
        {
            const float step = 35.0f;

            for (float yawAngle = -720.0f; yawAngle <= 720.0f; yawAngle += step)
            {
                for (float pitchAngle = -720.0f; pitchAngle <= 720.0f; pitchAngle += step)
                {
                    for (float rollAngle = -720.0f; rollAngle <= 720.0f; rollAngle += step)
                    {
                        float yawRad = MathHelper.ToRadians(yawAngle);
                        float pitchRad = MathHelper.ToRadians(pitchAngle);
                        float rollRad = MathHelper.ToRadians(rollAngle);
                        Matrix4x4 yaw = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, yawRad);
                        Matrix4x4 pitch = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, pitchRad);
                        Matrix4x4 roll = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, rollRad);

                        Matrix4x4 expected = roll * pitch * yaw;
                        Matrix4x4 actual = Matrix4x4.CreateFromYawPitchRoll(yawRad, pitchRad, rollRad);
                        Assert.True(MathHelper.Equal(expected, actual), string.Format("Yaw:{0} Pitch:{1} Roll:{2}", yawAngle, pitchAngle, rollAngle));
                    }
                }
            }
        }

        // Simple shadow test.
        [Fact]
        public void Matrix4x4CreateShadowTest01()
        {
            Vector3 lightDir = Vector3.UnitY;
            Plane plane = new Plane(Vector3.UnitY, 0);

            Matrix4x4 expected = Matrix4x4.CreateScale(1, 0, 1);

            Matrix4x4 actual = Matrix4x4.CreateShadow(lightDir, plane);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateShadow did not returned expected value.");
        }

        // Various plane projections.
        [Fact]
        public void Matrix4x4CreateShadowTest02()
        {
            // Complex cases.
            Plane[] planes = {
                new Plane( 0, 1, 0, 0 ),
                new Plane( 1, 2, 3, 4 ),
                new Plane( 5, 6, 7, 8 ),
                new Plane(-1,-2,-3,-4 ),
                new Plane(-5,-6,-7,-8 ),
            };

            Vector3[] points = {
                new Vector3( 1, 2, 3),
                new Vector3( 5, 6, 7),
                new Vector3( 8, 9, 10),
                new Vector3(-1,-2,-3),
                new Vector3(-5,-6,-7),
                new Vector3(-8,-9,-10),
            };

            foreach (Plane p in planes)
            {
                Plane plane = Plane.Normalize(p);

                // Try various direction of light directions.
                var testDirections = new Vector3[]
                {
                    new Vector3( -1.0f, 1.0f, 1.0f ),
                    new Vector3(  0.0f, 1.0f, 1.0f ),
                    new Vector3(  1.0f, 1.0f, 1.0f ),
                    new Vector3( -1.0f, 0.0f, 1.0f ),
                    new Vector3(  0.0f, 0.0f, 1.0f ),
                    new Vector3(  1.0f, 0.0f, 1.0f ),
                    new Vector3( -1.0f,-1.0f, 1.0f ),
                    new Vector3(  0.0f,-1.0f, 1.0f ),
                    new Vector3(  1.0f,-1.0f, 1.0f ),

                    new Vector3( -1.0f, 1.0f, 0.0f ),
                    new Vector3(  0.0f, 1.0f, 0.0f ),
                    new Vector3(  1.0f, 1.0f, 0.0f ),
                    new Vector3( -1.0f, 0.0f, 0.0f ),
                    new Vector3(  0.0f, 0.0f, 0.0f ),
                    new Vector3(  1.0f, 0.0f, 0.0f ),
                    new Vector3( -1.0f,-1.0f, 0.0f ),
                    new Vector3(  0.0f,-1.0f, 0.0f ),
                    new Vector3(  1.0f,-1.0f, 0.0f ),

                    new Vector3( -1.0f, 1.0f,-1.0f ),
                    new Vector3(  0.0f, 1.0f,-1.0f ),
                    new Vector3(  1.0f, 1.0f,-1.0f ),
                    new Vector3( -1.0f, 0.0f,-1.0f ),
                    new Vector3(  0.0f, 0.0f,-1.0f ),
                    new Vector3(  1.0f, 0.0f,-1.0f ),
                    new Vector3( -1.0f,-1.0f,-1.0f ),
                    new Vector3(  0.0f,-1.0f,-1.0f ),
                    new Vector3(  1.0f,-1.0f,-1.0f ),
                };

                foreach (Vector3 lightDirInfo in testDirections)
                {
                    if (lightDirInfo.Length() < 0.1f)
                        continue;
                    Vector3 lightDir = Vector3.Normalize(lightDirInfo);

                    if (Plane.DotNormal(plane, lightDir) < 0.1f)
                        continue;

                    Matrix4x4 m = Matrix4x4.CreateShadow(lightDir, plane);
                    Vector3 pp = -plane.D * plane.Normal; // origin of the plane.

                    //
                    foreach (Vector3 point in points)
                    {
                        Vector4 v4 = Vector4.Transform(point, m);

                        Vector3 sp = new Vector3(v4.X, v4.Y, v4.Z) / v4.W;

                        // Make sure transformed position is on the plane.
                        Vector3 v = sp - pp;
                        float d = Vector3.Dot(v, plane.Normal);
                        Assert.True(MathHelper.Equal(d, 0), "Matrix4x4.CreateShadow did not provide expected value.");

                        // make sure direction between transformed position and original position are same as light direction.
                        if (Vector3.Dot(point - pp, plane.Normal) > 0.0001f)
                        {
                            Vector3 dir = Vector3.Normalize(point - sp);
                            Assert.True(MathHelper.Equal(dir, lightDir), "Matrix4x4.CreateShadow did not provide expected value.");
                        }
                    }
                }
            }
        }

        void CreateReflectionTest(Plane plane, Matrix4x4 expected)
        {
            Matrix4x4 actual = Matrix4x4.CreateReflection(plane);
            Assert.True(MathHelper.Equal(actual, expected), "Matrix4x4.CreateReflection did not return expected value.");
        }

        [Fact]
        public void Matrix4x4CreateReflectionTest01()
        {
            // XY plane.
            CreateReflectionTest(new Plane(Vector3.UnitZ, 0), Matrix4x4.CreateScale(1, 1, -1));
            // XZ plane.
            CreateReflectionTest(new Plane(Vector3.UnitY, 0), Matrix4x4.CreateScale(1, -1, 1));
            // YZ plane.
            CreateReflectionTest(new Plane(Vector3.UnitX, 0), Matrix4x4.CreateScale(-1, 1, 1));

            // Complex cases.
            Plane[] planes = {
                new Plane( 0, 1, 0, 0 ),
                new Plane( 1, 2, 3, 4 ),
                new Plane( 5, 6, 7, 8 ),
                new Plane(-1,-2,-3,-4 ),
                new Plane(-5,-6,-7,-8 ),
            };

            Vector3[] points = {
                new Vector3( 1, 2, 3),
                new Vector3( 5, 6, 7),
                new Vector3(-1,-2,-3),
                new Vector3(-5,-6,-7),
            };

            foreach (Plane p in planes)
            {
                Plane plane = Plane.Normalize(p);
                Matrix4x4 m = Matrix4x4.CreateReflection(plane);
                Vector3 pp = -plane.D * plane.Normal; // Position on the plane.

                //
                foreach (Vector3 point in points)
                {
                    Vector3 rp = Vector3.Transform(point, m);

                    // Manually compute reflection point and compare results.
                    Vector3 v = point - pp;
                    float d = Vector3.Dot(v, plane.Normal);
                    Vector3 vp = point - 2.0f * d * plane.Normal;
                    Assert.True(MathHelper.Equal(rp, vp), "Matrix4x4.CreateReflection did not provide expected value.");
                }
            }
        }

        [Fact]
        public void Matrix4x4CreateReflectionTest02()
        {
            Plane plane = new Plane(0, 1, 0, 60);
            Matrix4x4 actual = Matrix4x4.CreateReflection(plane);

            AssertExtensions.Equal(1.0f, actual.M11, 0.0f);
            AssertExtensions.Equal(0.0f, actual.M12, 0.0f);
            AssertExtensions.Equal(0.0f, actual.M13, 0.0f);
            AssertExtensions.Equal(0.0f, actual.M14, 0.0f);

            AssertExtensions.Equal(0.0f, actual.M21, 0.0f);
            AssertExtensions.Equal(-1.0f, actual.M22, 0.0f);
            AssertExtensions.Equal(0.0f, actual.M23, 0.0f);
            AssertExtensions.Equal(0.0f, actual.M24, 0.0f);

            AssertExtensions.Equal(0.0f, actual.M31, 0.0f);
            AssertExtensions.Equal(0.0f, actual.M32, 0.0f);
            AssertExtensions.Equal(1.0f, actual.M33, 0.0f);
            AssertExtensions.Equal(0.0f, actual.M34, 0.0f);

            AssertExtensions.Equal(0.0f, actual.M41, 0.0f);
            AssertExtensions.Equal(-120.0f, actual.M42, 0.0f);
            AssertExtensions.Equal(0.0f, actual.M43, 0.0f);
            AssertExtensions.Equal(1.0f, actual.M44, 0.0f);
        }

        // A test for CreateRotationZ (float)
        [Fact]
        public void Matrix4x4CreateRotationZTest()
        {
            float radians = MathHelper.ToRadians(50.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.642787635f;
            expected.M12 = 0.766044438f;
            expected.M21 = -0.766044438f;
            expected.M22 = 0.642787635f;
            expected.M33 = 1.0f;
            expected.M44 = 1.0f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreateRotationZ(radians);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateRotationZ did not return the expected value.");
        }

        // A test for CreateRotationZ (float, Vector3f)
        [Fact]
        public void Matrix4x4CreateRotationZCenterTest()
        {
            float radians = MathHelper.ToRadians(30.0f);
            Vector3 center = new Vector3(23, 42, 66);

            Matrix4x4 rotateAroundZero = Matrix4x4.CreateRotationZ(radians, Vector3.Zero);
            Matrix4x4 rotateAroundZeroExpected = Matrix4x4.CreateRotationZ(radians);
            Assert.True(MathHelper.Equal(rotateAroundZero, rotateAroundZeroExpected));

            Matrix4x4 rotateAroundCenter = Matrix4x4.CreateRotationZ(radians, center);
            Matrix4x4 rotateAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateRotationZ(radians) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(rotateAroundCenter, rotateAroundCenterExpected));
        }

        [Fact]
        public void Matrix4x4CreateLookAtTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraTarget = new Vector3(3.0f, 2.0f, -4.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.979457f;
            expected.M12 = -0.0928268f;
            expected.M13 = +0.179017f;

            expected.M21 = +0.0f;
            expected.M22 = +0.887748f;
            expected.M23 = +0.460329f;

            expected.M31 = -0.201653f;
            expected.M32 = -0.450873f;
            expected.M33 = +0.869511f;

            expected.M41 = -3.74498f;
            expected.M42 = -3.30051f;
            expected.M43 = -37.0821f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookAt)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateLookAtLeftHandedTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraTarget = new Vector3(3.0f, 2.0f, -4.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = -0.979457f;
            expected.M12 = -0.0928268f;
            expected.M13 = -0.179017f;

            expected.M21 = +0.0f;
            expected.M22 = +0.887748f;
            expected.M23 = -0.460329f;

            expected.M31 = +0.201653f;
            expected.M32 = -0.450873f;
            expected.M33 = -0.869511f;

            expected.M41 = +3.74498f;
            expected.M42 = -3.30051f;
            expected.M43 = +37.0821f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = Matrix4x4.CreateLookAtLeftHanded(cameraPosition, cameraTarget, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookAtLeftHanded)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateLookToTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraDirection = new Vector3(-7.0f, -18.0f, -34.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.979457f;
            expected.M12 = -0.0928268f;
            expected.M13 = +0.179017f;

            expected.M21 = +0.0f;
            expected.M22 = +0.887748f;
            expected.M23 = +0.460329f;

            expected.M31 = -0.201653f;
            expected.M32 = -0.450873f;
            expected.M33 = +0.869511f;

            expected.M41 = -3.74498f;
            expected.M42 = -3.30051f;
            expected.M43 = -37.0821f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = Matrix4x4.CreateLookTo(cameraPosition, cameraDirection, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookTo)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateLookToLeftHandedTest()
        {
            Vector3 cameraPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 cameraDirection = new Vector3(-7.0f, -18.0f, -34.0f);
            Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = -0.979457f;
            expected.M12 = -0.0928268f;
            expected.M13 = -0.179017f;

            expected.M21 = +0.0f;
            expected.M22 = +0.887748f;
            expected.M23 = -0.460329f;

            expected.M31 = +0.201653f;
            expected.M32 = -0.450873f;
            expected.M33 = -0.869511f;

            expected.M41 = +3.74498f;
            expected.M42 = -3.30051f;
            expected.M43 = +37.0821f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = Matrix4x4.CreateLookToLeftHanded(cameraPosition, cameraDirection, cameraUpVector);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateLookToLeftHanded)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateViewportTest()
        {
            float x = 10.0f;
            float y = 20.0f;
            float width = 80.0f;
            float height = 160.0f;
            float minDepth = 1.5f;
            float maxDepth = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +40.0f;

            expected.M22 = -80.0f;

            expected.M33 = -998.5f;

            expected.M41 = +50.0f;
            expected.M42 = +100.0f;
            expected.M43 = +1.5f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = Matrix4x4.CreateViewport(x, y, width, height, minDepth, maxDepth);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateViewport)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateViewportLeftHandedTest()
        {
            float x = 10.0f, y = 20.0f;
            float width = 3.0f, height = 4.0f;
            float minDepth = 100.0f, maxDepth = 200.0f;

            Matrix4x4 expected = Matrix4x4.Identity;
            expected.M11 = width * 0.5f;
            expected.M22 = -height * 0.5f;
            expected.M33 = maxDepth - minDepth;
            expected.M41 = x + expected.M11;
            expected.M42 = y - expected.M22;
            expected.M43 = minDepth;

            Matrix4x4 actual = Matrix4x4.CreateViewportLeftHanded(x, y, width, height, minDepth, maxDepth);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateViewportLeftHanded)} did not return the expected value.");
        }

        // A test for CreateWorld (Vector3f, Vector3f, Vector3f)
        [Fact]
        public void Matrix4x4CreateWorldTest()
        {
            Vector3 objectPosition = new Vector3(10.0f, 20.0f, 30.0f);
            Vector3 objectForwardDirection = new Vector3(3.0f, 2.0f, -4.0f);
            Vector3 objectUpVector = new Vector3(0.0f, 1.0f, 0.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.799999952f;
            expected.M12 = 0;
            expected.M13 = 0.599999964f;
            expected.M14 = 0;

            expected.M21 = -0.2228344f;
            expected.M22 = 0.928476632f;
            expected.M23 = 0.297112525f;
            expected.M24 = 0;

            expected.M31 = -0.557086f;
            expected.M32 = -0.371390671f;
            expected.M33 = 0.742781341f;
            expected.M34 = 0;

            expected.M41 = 10;
            expected.M42 = 20;
            expected.M43 = 30;
            expected.M44 = 1.0f;

            Matrix4x4 actual = Matrix4x4.CreateWorld(objectPosition, objectForwardDirection, objectUpVector);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.CreateWorld did not return the expected value.");

            Assert.Equal(objectPosition, actual.Translation);
            Assert.True(Vector3.Dot(Vector3.Normalize(objectUpVector), new Vector3(actual.M21, actual.M22, actual.M23)) > 0);
            Assert.True(Vector3.Dot(Vector3.Normalize(objectForwardDirection), new Vector3(-actual.M31, -actual.M32, -actual.M33)) > 0.999f);
        }

        [Fact]
        public void Matrix4x4CreateOrthoTest()
        {
            float width = 100.0f;
            float height = 200.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.02f;

            expected.M22 = +0.01f;

            expected.M33 = -0.0010015f;

            expected.M43 = -0.00150225f;
            expected.M44 = +1.0f;

            Matrix4x4 actual;
            actual = Matrix4x4.CreateOrthographic(width, height, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateOrthographic)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateOrthoLeftHandedTest()
        {
            float width = 100.0f;
            float height = 200.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.02f;

            expected.M22 = +0.01f;

            expected.M33 = +0.0010015f;

            expected.M43 = -0.00150225f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = Matrix4x4.CreateOrthographicLeftHanded(width, height, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateOrthographicLeftHanded)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateOrthoOffCenterTest()
        {
            float left = 10.0f;
            float right = 90.0f;
            float bottom = 20.0f;
            float top = 180.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.025f;

            expected.M22 = +0.0125f;

            expected.M33 = -0.0010015f;

            expected.M41 = -1.25f;
            expected.M42 = -1.25f;
            expected.M43 = -0.00150225f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateOrthographicOffCenter)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreateOrthoOffCenterLeftHandedTest()
        {
            float left = 10.0f;
            float right = 90.0f;
            float bottom = 20.0f;
            float top = 180.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.025f;

            expected.M22 = +0.0125f;

            expected.M33 = +0.0010015f;

            expected.M41 = -1.25f;
            expected.M42 = -1.25f;
            expected.M43 = -0.00150225f;
            expected.M44 = +1.0f;

            Matrix4x4 actual = Matrix4x4.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreateOrthographicOffCenterLeftHanded)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreatePerspectiveTest()
        {
            float width = 100.0f;
            float height = 200.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.03f;

            expected.M22 = +0.015f;

            expected.M33 = -1.0015f;
            expected.M34 = -1.0f;

            expected.M43 = -1.50225f;

            Matrix4x4 actual = Matrix4x4.CreatePerspective(width, height, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreatePerspective)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreatePerspectiveLeftHandedTest()
        {
            float width = 100.0f;
            float height = 200.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.03f;

            expected.M22 = +0.015f;

            expected.M33 = +1.0015f;
            expected.M34 = +1.0f;

            expected.M43 = -1.50225f;

            Matrix4x4 actual = Matrix4x4.CreatePerspectiveLeftHanded(width, height, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreatePerspectiveLeftHanded)} did not return the expected value.");
        }

        // A test for CreatePerspective (float, float, float, float)
        // CreatePerspective test where znear = zfar
        [Fact]
        public void Matrix4x4CreatePerspectiveTest1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                float width = 100.0f;
                float height = 200.0f;
                float zNearPlane = 0.0f;
                float zFarPlane = 0.0f;

                Matrix4x4 actual = Matrix4x4.CreatePerspective(width, height, zNearPlane, zFarPlane);
            });
        }

        // A test for CreatePerspective (float, float, float, float)
        // CreatePerspective test where near plane is negative value
        [Fact]
        public void Matrix4x4CreatePerspectiveTest2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 actual = Matrix4x4.CreatePerspective(10, 10, -10, 10);
            });
        }

        // A test for CreatePerspective (float, float, float, float)
        // CreatePerspective test where far plane is negative value
        [Fact]
        public void Matrix4x4CreatePerspectiveTest3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 actual = Matrix4x4.CreatePerspective(10, 10, 10, -10);
            });
        }

        // A test for CreatePerspective (float, float, float, float)
        // CreatePerspective test where near plane is beyond far plane
        [Fact]
        public void Matrix4x4CreatePerspectiveTest4()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 actual = Matrix4x4.CreatePerspective(10, 10, 10, 1);
            });
        }

        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest()
        {
            float fieldOfView = MathHelper.ToRadians(30.0f);
            float aspectRatio = 1280.0f / 720.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +2.09928f;

            expected.M22 = +3.73205f;

            expected.M33 = -1.0015f;
            expected.M34 = -1.0f;

            expected.M43 = -1.50225f;

            Matrix4x4 actual = Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreatePerspectiveFieldOfView)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewLeftHandedTest()
        {
            float fieldOfView = MathHelper.ToRadians(30.0f);
            float aspectRatio = 1280.0f / 720.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +2.09928f;

            expected.M22 = +3.73205f;

            expected.M33 = +1.0015f;
            expected.M34 = +1.0f;

            expected.M43 = -1.50225f;

            Matrix4x4 actual = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(fieldOfView, aspectRatio, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded)} did not return the expected value.");
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where filedOfView is negative value.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(-1, 1, 1, 10);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where filedOfView is more than pi.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.Pi + 0.01f, 1, 1, 10);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where nearPlaneDistance is negative value.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, -1, 10);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where farPlaneDistance is negative value.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest4()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 1, -10);
            });
        }

        // A test for CreatePerspectiveFieldOfView (float, float, float, float)
        // CreatePerspectiveFieldOfView test where nearPlaneDistance is larger than farPlaneDistance.
        [Fact]
        public void Matrix4x4CreatePerspectiveFieldOfViewTest5()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Matrix4x4 mtx = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1, 10, 1);
            });
        }

        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterTest()
        {
            float left = 10.0f;
            float right = 90.0f;
            float bottom = 20.0f;
            float top = 180.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.0375f;

            expected.M22 = +0.01875f;

            expected.M31 = +1.25f;
            expected.M32 = +1.25f;
            expected.M33 = -1.0015f;
            expected.M34 = -1.0f;

            expected.M43 = -1.50225f;

            Matrix4x4 actual = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreatePerspectiveOffCenter)} did not return the expected value.");
        }

        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterLeftHandedTest()
        {
            float left = 10.0f;
            float right = 90.0f;
            float bottom = 20.0f;
            float top = 180.0f;
            float zNearPlane = 1.5f;
            float zFarPlane = 1000.0f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = +0.0375f;

            expected.M22 = +0.01875f;

            expected.M31 = -1.25f;
            expected.M32 = -1.25f;
            expected.M33 = +1.0015f;
            expected.M34 = +1.0f;

            expected.M43 = -1.50225f;


            Matrix4x4 actual = Matrix4x4.CreatePerspectiveOffCenterLeftHanded(left, right, bottom, top, zNearPlane, zFarPlane);
            Assert.True(MathHelper.Equal(expected, actual), $"{nameof(Matrix4x4)}.{nameof(Matrix4x4.CreatePerspectiveOffCenterLeftHanded)} did not return the expected value.");
        }

        // A test for CreatePerspectiveOffCenter (float, float, float, float, float, float)
        // CreatePerspectiveOffCenter test where nearPlaneDistance is negative.
        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterTest1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                float left = 10.0f, right = 90.0f, bottom = 20.0f, top = 180.0f;
                Matrix4x4 actual = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, -1, 10);
            });
        }

        // A test for CreatePerspectiveOffCenter (float, float, float, float, float, float)
        // CreatePerspectiveOffCenter test where farPlaneDistance is negative.
        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterTest2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                float left = 10.0f, right = 90.0f, bottom = 20.0f, top = 180.0f;
                Matrix4x4 actual = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, 1, -10);
            });
        }

        // A test for CreatePerspectiveOffCenter (float, float, float, float, float, float)
        // CreatePerspectiveOffCenter test where test where nearPlaneDistance is larger than farPlaneDistance.
        [Fact]
        public void Matrix4x4CreatePerspectiveOffCenterTest3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                float left = 10.0f, right = 90.0f, bottom = 20.0f, top = 180.0f;
                Matrix4x4 actual = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, 10, 1);
            });
        }

        // A test for Invert (Matrix4x4)
        // Non invertible matrix - determinant is zero - singular matrix
        [Fact]
        public void Matrix4x4InvertTest1()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 1.0f;
            a.M12 = 2.0f;
            a.M13 = 3.0f;
            a.M14 = 4.0f;
            a.M21 = 5.0f;
            a.M22 = 6.0f;
            a.M23 = 7.0f;
            a.M24 = 8.0f;
            a.M31 = 9.0f;
            a.M32 = 10.0f;
            a.M33 = 11.0f;
            a.M34 = 12.0f;
            a.M41 = 13.0f;
            a.M42 = 14.0f;
            a.M43 = 15.0f;
            a.M44 = 16.0f;

            float detA = a.GetDeterminant();
            Assert.True(MathHelper.Equal(detA, 0.0f), "Matrix4x4.Invert did not return the expected value.");

            Matrix4x4 actual;
            Assert.False(Matrix4x4.Invert(a, out actual));

            // all the elements in Actual is NaN
            Assert.True(
                float.IsNaN(actual.M11) && float.IsNaN(actual.M12) && float.IsNaN(actual.M13) && float.IsNaN(actual.M14) &&
                float.IsNaN(actual.M21) && float.IsNaN(actual.M22) && float.IsNaN(actual.M23) && float.IsNaN(actual.M24) &&
                float.IsNaN(actual.M31) && float.IsNaN(actual.M32) && float.IsNaN(actual.M33) && float.IsNaN(actual.M34) &&
                float.IsNaN(actual.M41) && float.IsNaN(actual.M42) && float.IsNaN(actual.M43) && float.IsNaN(actual.M44)
                , "Matrix4x4.Invert did not return the expected value.");
        }

        // A test for Lerp (Matrix4x4, Matrix4x4, float)
        [Fact]
        public void Matrix4x4LerpTest()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 11.0f;
            a.M12 = 12.0f;
            a.M13 = 13.0f;
            a.M14 = 14.0f;
            a.M21 = 21.0f;
            a.M22 = 22.0f;
            a.M23 = 23.0f;
            a.M24 = 24.0f;
            a.M31 = 31.0f;
            a.M32 = 32.0f;
            a.M33 = 33.0f;
            a.M34 = 34.0f;
            a.M41 = 41.0f;
            a.M42 = 42.0f;
            a.M43 = 43.0f;
            a.M44 = 44.0f;

            Matrix4x4 b = GenerateIncrementalMatrixNumber();

            float t = 0.5f;

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 + (b.M11 - a.M11) * t;
            expected.M12 = a.M12 + (b.M12 - a.M12) * t;
            expected.M13 = a.M13 + (b.M13 - a.M13) * t;
            expected.M14 = a.M14 + (b.M14 - a.M14) * t;

            expected.M21 = a.M21 + (b.M21 - a.M21) * t;
            expected.M22 = a.M22 + (b.M22 - a.M22) * t;
            expected.M23 = a.M23 + (b.M23 - a.M23) * t;
            expected.M24 = a.M24 + (b.M24 - a.M24) * t;

            expected.M31 = a.M31 + (b.M31 - a.M31) * t;
            expected.M32 = a.M32 + (b.M32 - a.M32) * t;
            expected.M33 = a.M33 + (b.M33 - a.M33) * t;
            expected.M34 = a.M34 + (b.M34 - a.M34) * t;

            expected.M41 = a.M41 + (b.M41 - a.M41) * t;
            expected.M42 = a.M42 + (b.M42 - a.M42) * t;
            expected.M43 = a.M43 + (b.M43 - a.M43) * t;
            expected.M44 = a.M44 + (b.M44 - a.M44) * t;

            Matrix4x4 actual;
            actual = Matrix4x4.Lerp(a, b, t);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Lerp did not return the expected value.");
        }

        // A test for operator - (Matrix4x4)
        [Fact]
        public void Matrix4x4UnaryNegationTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = -1.0f;
            expected.M12 = -2.0f;
            expected.M13 = -3.0f;
            expected.M14 = -4.0f;
            expected.M21 = -5.0f;
            expected.M22 = -6.0f;
            expected.M23 = -7.0f;
            expected.M24 = -8.0f;
            expected.M31 = -9.0f;
            expected.M32 = -10.0f;
            expected.M33 = -11.0f;
            expected.M34 = -12.0f;
            expected.M41 = -13.0f;
            expected.M42 = -14.0f;
            expected.M43 = -15.0f;
            expected.M44 = -16.0f;

            Matrix4x4 actual = -a;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator - did not return the expected value.");
        }

        // A test for operator - (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4SubtractionTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 - b.M11;
            expected.M12 = a.M12 - b.M12;
            expected.M13 = a.M13 - b.M13;
            expected.M14 = a.M14 - b.M14;
            expected.M21 = a.M21 - b.M21;
            expected.M22 = a.M22 - b.M22;
            expected.M23 = a.M23 - b.M23;
            expected.M24 = a.M24 - b.M24;
            expected.M31 = a.M31 - b.M31;
            expected.M32 = a.M32 - b.M32;
            expected.M33 = a.M33 - b.M33;
            expected.M34 = a.M34 - b.M34;
            expected.M41 = a.M41 - b.M41;
            expected.M42 = a.M42 - b.M42;
            expected.M43 = a.M43 - b.M43;
            expected.M44 = a.M44 - b.M44;

            Matrix4x4 actual = a - b;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator - did not return the expected value.");
        }

        // A test for operator * (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4MultiplyTest1()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
            expected.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
            expected.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
            expected.M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;

            expected.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
            expected.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
            expected.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
            expected.M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;

            expected.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
            expected.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
            expected.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
            expected.M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;

            expected.M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
            expected.M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
            expected.M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
            expected.M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;

            Matrix4x4 actual = a * b;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator * did not return the expected value.");
        }

        // A test for operator * (Matrix4x4, Matrix4x4)
        // Multiply with identity matrix
        [Fact]
        public void Matrix4x4MultiplyTest4()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 1.0f;
            a.M12 = 2.0f;
            a.M13 = 3.0f;
            a.M14 = 4.0f;
            a.M21 = 5.0f;
            a.M22 = -6.0f;
            a.M23 = 7.0f;
            a.M24 = -8.0f;
            a.M31 = 9.0f;
            a.M32 = 10.0f;
            a.M33 = 11.0f;
            a.M34 = 12.0f;
            a.M41 = 13.0f;
            a.M42 = -14.0f;
            a.M43 = 15.0f;
            a.M44 = -16.0f;

            Matrix4x4 b = new Matrix4x4();
            b = Matrix4x4.Identity;

            Matrix4x4 expected = a;
            Matrix4x4 actual = a * b;

            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator * did not return the expected value.");
        }

        // A test for operator + (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4AdditionTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 + b.M11;
            expected.M12 = a.M12 + b.M12;
            expected.M13 = a.M13 + b.M13;
            expected.M14 = a.M14 + b.M14;
            expected.M21 = a.M21 + b.M21;
            expected.M22 = a.M22 + b.M22;
            expected.M23 = a.M23 + b.M23;
            expected.M24 = a.M24 + b.M24;
            expected.M31 = a.M31 + b.M31;
            expected.M32 = a.M32 + b.M32;
            expected.M33 = a.M33 + b.M33;
            expected.M34 = a.M34 + b.M34;
            expected.M41 = a.M41 + b.M41;
            expected.M42 = a.M42 + b.M42;
            expected.M43 = a.M43 + b.M43;
            expected.M44 = a.M44 + b.M44;

            Matrix4x4 actual = a + b;
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.operator + did not return the expected value.");
        }

        // A test for Transpose (Matrix4x4)
        [Fact]
        public void Matrix4x4TransposeTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11;
            expected.M12 = a.M21;
            expected.M13 = a.M31;
            expected.M14 = a.M41;
            expected.M21 = a.M12;
            expected.M22 = a.M22;
            expected.M23 = a.M32;
            expected.M24 = a.M42;
            expected.M31 = a.M13;
            expected.M32 = a.M23;
            expected.M33 = a.M33;
            expected.M34 = a.M43;
            expected.M41 = a.M14;
            expected.M42 = a.M24;
            expected.M43 = a.M34;
            expected.M44 = a.M44;

            Matrix4x4 actual = Matrix4x4.Transpose(a);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Transpose did not return the expected value.");
        }

        // A test for Transpose (Matrix4x4)
        // Transpose Identity matrix
        [Fact]
        public void Matrix4x4TransposeTest1()
        {
            Matrix4x4 a = Matrix4x4.Identity;
            Matrix4x4 expected = Matrix4x4.Identity;

            Matrix4x4 actual = Matrix4x4.Transpose(a);
            Assert.True(MathHelper.Equal(expected, actual), "Matrix4x4.Transpose did not return the expected value.");
        }

        // A test for Matrix4x4 (Quaternion)
        [Fact]
        public void Matrix4x4FromQuaternionTest1()
        {
            Vector3 axis = Vector3.Normalize(new Vector3(1.0f, 2.0f, 3.0f));
            Quaternion q = Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(30.0f));

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = 0.875595033f;
            expected.M12 = 0.420031041f;
            expected.M13 = -0.2385524f;
            expected.M14 = 0.0f;

            expected.M21 = -0.38175258f;
            expected.M22 = 0.904303849f;
            expected.M23 = 0.1910483f;
            expected.M24 = 0.0f;

            expected.M31 = 0.295970082f;
            expected.M32 = -0.07621294f;
            expected.M33 = 0.952151954f;
            expected.M34 = 0.0f;

            expected.M41 = 0.0f;
            expected.M42 = 0.0f;
            expected.M43 = 0.0f;
            expected.M44 = 1.0f;

            Matrix4x4 target = Matrix4x4.CreateFromQuaternion(q);
            Assert.True(MathHelper.Equal(expected, target), "Matrix4x4.Matrix4x4(Quaternion) did not return the expected value.");
        }

        // A test for FromQuaternion (Matrix4x4)
        // Convert X axis rotation matrix
        [Fact]
        public void Matrix4x4FromQuaternionTest2()
        {
            for (float angle = 0.0f; angle < 720.0f; angle += 10.0f)
            {
                Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle);

                Matrix4x4 expected = Matrix4x4.CreateRotationX(angle);
                Matrix4x4 actual = Matrix4x4.CreateFromQuaternion(quat);
                Assert.True(MathHelper.Equal(expected, actual),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));

                // make sure convert back to quaternion is same as we passed quaternion.
                Quaternion q2 = Quaternion.CreateFromRotationMatrix(actual);
                Assert.True(MathHelper.EqualRotation(quat, q2),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));
            }
        }

        // A test for FromQuaternion (Matrix4x4)
        // Convert Y axis rotation matrix
        [Fact]
        public void Matrix4x4FromQuaternionTest3()
        {
            for (float angle = 0.0f; angle < 720.0f; angle += 10.0f)
            {
                Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

                Matrix4x4 expected = Matrix4x4.CreateRotationY(angle);
                Matrix4x4 actual = Matrix4x4.CreateFromQuaternion(quat);
                Assert.True(MathHelper.Equal(expected, actual),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));

                // make sure convert back to quaternion is same as we passed quaternion.
                Quaternion q2 = Quaternion.CreateFromRotationMatrix(actual);
                Assert.True(MathHelper.EqualRotation(quat, q2),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));
            }
        }

        // A test for FromQuaternion (Matrix4x4)
        // Convert Z axis rotation matrix
        [Fact]
        public void Matrix4x4FromQuaternionTest4()
        {
            for (float angle = 0.0f; angle < 720.0f; angle += 10.0f)
            {
                Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);

                Matrix4x4 expected = Matrix4x4.CreateRotationZ(angle);
                Matrix4x4 actual = Matrix4x4.CreateFromQuaternion(quat);
                Assert.True(MathHelper.Equal(expected, actual),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));

                // make sure convert back to quaternion is same as we passed quaternion.
                Quaternion q2 = Quaternion.CreateFromRotationMatrix(actual);
                Assert.True(MathHelper.EqualRotation(quat, q2),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));
            }
        }

        // A test for FromQuaternion (Matrix4x4)
        // Convert XYZ axis rotation matrix
        [Fact]
        public void Matrix4x4FromQuaternionTest5()
        {
            for (float angle = 0.0f; angle < 720.0f; angle += 10.0f)
            {
                Quaternion quat =
                    Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle) *
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle) *
                    Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle);

                Matrix4x4 expected =
                    Matrix4x4.CreateRotationX(angle) *
                    Matrix4x4.CreateRotationY(angle) *
                    Matrix4x4.CreateRotationZ(angle);
                Matrix4x4 actual = Matrix4x4.CreateFromQuaternion(quat);
                Assert.True(MathHelper.Equal(expected, actual),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));

                // make sure convert back to quaternion is same as we passed quaternion.
                Quaternion q2 = Quaternion.CreateFromRotationMatrix(actual);
                Assert.True(MathHelper.EqualRotation(quat, q2),
                    string.Format("Quaternion.FromQuaternion did not return the expected value. angle:{0}",
                    angle.ToString()));
            }
        }

        // A test for ToString ()
        [Fact]
        public void Matrix4x4ToStringTest()
        {
            Matrix4x4 a = new Matrix4x4();
            a.M11 = 11.0f;
            a.M12 = -12.0f;
            a.M13 = -13.3f;
            a.M14 = 14.4f;
            a.M21 = 21.0f;
            a.M22 = 22.0f;
            a.M23 = 23.0f;
            a.M24 = 24.0f;
            a.M31 = 31.0f;
            a.M32 = 32.0f;
            a.M33 = 33.0f;
            a.M34 = 34.0f;
            a.M41 = 41.0f;
            a.M42 = 42.0f;
            a.M43 = 43.0f;
            a.M44 = 44.0f;

            string expected = string.Format(CultureInfo.CurrentCulture,
                "{{ {{M11:{0} M12:{1} M13:{2} M14:{3}}} {{M21:{4} M22:{5} M23:{6} M24:{7}}} {{M31:{8} M32:{9} M33:{10} M34:{11}}} {{M41:{12} M42:{13} M43:{14} M44:{15}}} }}",
                    11.0f, -12.0f, -13.3f, 14.4f,
                    21.0f, 22.0f, 23.0f, 24.0f,
                    31.0f, 32.0f, 33.0f, 34.0f,
                    41.0f, 42.0f, 43.0f, 44.0f);

            string actual = a.ToString();
            Assert.Equal(expected, actual);
        }

        // A test for Add (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4AddTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 + b.M11;
            expected.M12 = a.M12 + b.M12;
            expected.M13 = a.M13 + b.M13;
            expected.M14 = a.M14 + b.M14;
            expected.M21 = a.M21 + b.M21;
            expected.M22 = a.M22 + b.M22;
            expected.M23 = a.M23 + b.M23;
            expected.M24 = a.M24 + b.M24;
            expected.M31 = a.M31 + b.M31;
            expected.M32 = a.M32 + b.M32;
            expected.M33 = a.M33 + b.M33;
            expected.M34 = a.M34 + b.M34;
            expected.M41 = a.M41 + b.M41;
            expected.M42 = a.M42 + b.M42;
            expected.M43 = a.M43 + b.M43;
            expected.M44 = a.M44 + b.M44;

            Matrix4x4 actual = Matrix4x4.Add(a, b);
            Assert.Equal(expected, actual);
        }

        // A test for Equals (object)
        [Fact]
        public void Matrix4x4EqualsTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber();

            // case 1: compare between same values
            object obj = b;

            bool expected = true;
            bool actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.M11 = 11.0f;
            obj = b;
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 3: compare between different types.
            obj = new Vector4();
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);

            // case 3: compare against null.
            obj = null;
            expected = false;
            actual = a.Equals(obj);
            Assert.Equal(expected, actual);
        }

        // A test for GetHashCode ()
        [Fact]
        public void Matrix4x4GetHashCodeTest()
        {
            Matrix4x4 target = GenerateIncrementalMatrixNumber();

            int expected = HashCode.Combine(
                new Vector4(target.M11, target.M12, target.M13, target.M14),
                new Vector4(target.M21, target.M22, target.M23, target.M24),
                new Vector4(target.M31, target.M32, target.M33, target.M34),
                new Vector4(target.M41, target.M42, target.M43, target.M44)
            );

            int actual = target.GetHashCode();

            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4MultiplyTest3()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
            expected.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
            expected.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
            expected.M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;

            expected.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
            expected.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
            expected.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
            expected.M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;

            expected.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
            expected.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
            expected.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
            expected.M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;

            expected.M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
            expected.M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
            expected.M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
            expected.M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;
            Matrix4x4 actual;
            actual = Matrix4x4.Multiply(a, b);

            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Matrix4x4, float)
        [Fact]
        public void Matrix4x4MultiplyTest5()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 expected = new Matrix4x4(3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48);
            Matrix4x4 actual = Matrix4x4.Multiply(a, 3);

            Assert.Equal(expected, actual);
        }

        // A test for Multiply (Matrix4x4, float)
        [Fact]
        public void Matrix4x4MultiplyTest6()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 expected = new Matrix4x4(3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48);
            Matrix4x4 actual = a * 3;

            Assert.Equal(expected, actual);
        }

        // A test for Negate (Matrix4x4)
        [Fact]
        public void Matrix4x4NegateTest()
        {
            Matrix4x4 m = GenerateIncrementalMatrixNumber();

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = -1.0f;
            expected.M12 = -2.0f;
            expected.M13 = -3.0f;
            expected.M14 = -4.0f;
            expected.M21 = -5.0f;
            expected.M22 = -6.0f;
            expected.M23 = -7.0f;
            expected.M24 = -8.0f;
            expected.M31 = -9.0f;
            expected.M32 = -10.0f;
            expected.M33 = -11.0f;
            expected.M34 = -12.0f;
            expected.M41 = -13.0f;
            expected.M42 = -14.0f;
            expected.M43 = -15.0f;
            expected.M44 = -16.0f;
            Matrix4x4 actual;

            actual = Matrix4x4.Negate(m);
            Assert.Equal(expected, actual);
        }

        // A test for operator != (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4InequalityTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber();

            // case 1: compare between same values
            bool expected = false;
            bool actual = a != b;
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.M11 = 11.0f;
            expected = true;
            actual = a != b;
            Assert.Equal(expected, actual);
        }

        // A test for operator == (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4EqualityTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber();

            // case 1: compare between same values
            bool expected = true;
            bool actual = a == b;
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.M11 = 11.0f;
            expected = false;
            actual = a == b;
            Assert.Equal(expected, actual);
        }

        // A test for Subtract (Matrix4x4, Matrix4x4)
        [Fact]
        public void Matrix4x4SubtractTest()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber(-8.0f);

            Matrix4x4 expected = new Matrix4x4();
            expected.M11 = a.M11 - b.M11;
            expected.M12 = a.M12 - b.M12;
            expected.M13 = a.M13 - b.M13;
            expected.M14 = a.M14 - b.M14;
            expected.M21 = a.M21 - b.M21;
            expected.M22 = a.M22 - b.M22;
            expected.M23 = a.M23 - b.M23;
            expected.M24 = a.M24 - b.M24;
            expected.M31 = a.M31 - b.M31;
            expected.M32 = a.M32 - b.M32;
            expected.M33 = a.M33 - b.M33;
            expected.M34 = a.M34 - b.M34;
            expected.M41 = a.M41 - b.M41;
            expected.M42 = a.M42 - b.M42;
            expected.M43 = a.M43 - b.M43;
            expected.M44 = a.M44 - b.M44;

            Matrix4x4 actual = Matrix4x4.Subtract(a, b);
            Assert.Equal(expected, actual);
        }

        private void CreateBillboardFact(Vector3 placeDirection, Vector3 cameraUpVector, Matrix4x4 expectedRotationRightHanded, Matrix4x4 expectedRotationLeftHanded)
        {
            Vector3 cameraPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 objectPosition = cameraPosition + placeDirection * 10.0f;
            Matrix4x4 expected = expectedRotationRightHanded * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualRH = Matrix4x4.CreateBillboard(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actualRH), "Matrix4x4.CreateBillboard did not return the expected value.");

            placeDirection = InverseHandedness(placeDirection);
            cameraUpVector = InverseHandedness(cameraUpVector);

            cameraPosition = new Vector3(3.0f, 4.0f, -5.0f);
            objectPosition = cameraPosition + placeDirection * 10.0f;
            expected = expectedRotationLeftHanded * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualLH = Matrix4x4.CreateBillboardLeftHanded(objectPosition, cameraPosition, cameraUpVector, Vector3.UnitZ);
            Assert.True(MathHelper.Equal(expected, actualLH), "Matrix4x4.CreateBillboardLeftHanded did not return the expected value.");

            AssertEqual(actualRH, InverseHandedness(actualLH), DefaultVarianceMatrix);
            AssertEqual(InverseHandedness(actualRH), actualLH, DefaultVarianceMatrix);
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Forward side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest01()
        {
            // Object placed at Forward of camera. result must be same as 180 degrees rotate along y-axis.
            CreateBillboardFact(
                new Vector3(0, 0, -1),
                Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Backward side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest02()
        {
            // Object placed at Backward of camera. This result must be same as 0 degrees rotate along y-axis.
            CreateBillboardFact(
                Vector3.UnitZ,
                Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(0)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(0)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Right side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest03()
        {
            // Place object at Right side of camera. This result must be same as 90 degrees rotate along y-axis.
            CreateBillboardFact(
                Vector3.UnitX,
                Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(90)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Left side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest04()
        {
            // Place object at Left side of camera. This result must be same as -90 degrees rotate along y-axis.
            CreateBillboardFact(
                new Vector3(-1, 0, 0),
                Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(90)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Up side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest05()
        {
            // Place object at Up side of camera. result must be same as 180 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateBillboardFact(
                Vector3.UnitY,
                Vector3.UnitZ,
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(180)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(180)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Down side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest06()
        {
            // Place object at Down side of camera. result must be same as 0 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateBillboardFact(
                new Vector3(0, -1, 0),
                Vector3.UnitZ,
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(0)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(0)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Right side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest07()
        {
            // Place object at Right side of camera. result must be same as 90 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateBillboardFact(
                Vector3.UnitX,
                Vector3.UnitZ,
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Left side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest08()
        {
            // Place object at Left side of camera. result must be same as -90 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateBillboardFact(
                new Vector3(-1, 0, 0),
                Vector3.UnitZ,
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Up side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest09()
        {
            // Place object at Up side of camera. result must be same as -90 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateBillboardFact(
                Vector3.UnitY,
                new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Down side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest10()
        {
            // Place object at Down side of camera. result must be same as 90 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateBillboardFact(
                new Vector3(0, -1, 0),
                new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Forward side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest11()
        {
            // Place object at Forward side of camera. result must be same as 180 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateBillboardFact(
                new Vector3(0, 0, -1),
                new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(180.0f)),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Backward side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateBillboardTest12()
        {
            // Place object at Backward side of camera. result must be same as 0 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateBillboardFact(
                Vector3.UnitZ,
                new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(0.0f)),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(0.0f)));
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Object and camera positions are too close and doesn't pass cameraForwardVector.
        [Fact]
        public void Matrix4x4CreateBillboardTooCloseTest1()
        {
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition;
            Vector3 cameraUpVector = Vector3.UnitY;

            // Doesn't pass camera face direction. CreateBillboard uses new Vector3f(0, 0, -1) direction. Result must be same as 180 degrees rotate along y-axis.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualRH = Matrix4x4.CreateBillboard(objectPosition, cameraPosition, cameraUpVector, Vector3.UnitZ);
            Assert.True(MathHelper.Equal(expected, actualRH), "Matrix4x4.CreateBillboard did not return the expected value.");

            objectPosition = new Vector3(3.0f, 4.0f, -5.0f);
            cameraPosition = objectPosition;
            cameraUpVector = Vector3.UnitY;

            expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualLH = Matrix4x4.CreateBillboardLeftHanded(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actualLH), "Matrix4x4.CreateBillboardLeftHanded did not return the expected value.");

            AssertEqual(actualRH, InverseHandedness(actualLH), DefaultVarianceMatrix);
            AssertEqual(InverseHandedness(actualRH), actualLH, DefaultVarianceMatrix);
        }

        // A test for CreateBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Object and camera positions are too close and passed cameraForwardVector.
        [Fact]
        public void Matrix4x4CreateBillboardTooCloseTest2()
        {
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition;
            Vector3 cameraUpVector = Vector3.UnitY;

            // Passes Vector3f.Right as camera face direction. Result must be same as -90 degrees rotate along y-axis.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualRH = Matrix4x4.CreateBillboard(objectPosition, cameraPosition, cameraUpVector, Vector3.UnitX);
            Assert.True(MathHelper.Equal(expected, actualRH), "Matrix4x4.CreateBillboard did not return the expected value.");

            objectPosition = new Vector3(3.0f, 4.0f, -5.0f);
            cameraPosition = objectPosition;
            cameraUpVector = Vector3.UnitY;

            expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualLH = Matrix4x4.CreateBillboardLeftHanded(objectPosition, cameraPosition, cameraUpVector, Vector3.UnitX);
            Assert.True(MathHelper.Equal(expected, actualLH), "Matrix4x4.CreateBillboardLeftHanded did not return the expected value.");
        }

        private void CreateConstrainedBillboardFact(Vector3 placeDirection, Vector3 rotateAxis, Matrix4x4 expectedRotationRightHanded, Matrix4x4 expectedRotationLeftHanded)
        {
            Vector3 cameraPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 objectPosition = cameraPosition + placeDirection * 10.0f;
            Matrix4x4 expected = expectedRotationRightHanded * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualRH = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actualRH), $"{nameof(Matrix4x4.CreateConstrainedBillboard)} did not return the expected value.");

            // When you move camera along rotateAxis, result must be same.
            cameraPosition += rotateAxis * 10.0f;
            Matrix4x4 actualTranslatedUpRH = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actualTranslatedUpRH), $"{nameof(Matrix4x4.CreateConstrainedBillboard)} did not return the expected value.");

            cameraPosition -= rotateAxis * 30.0f;
            Matrix4x4 actualTranslatedDownRH = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actualTranslatedDownRH), $"{nameof(Matrix4x4.CreateConstrainedBillboard)} did not return the expected value.");

            placeDirection = InverseHandedness(placeDirection);
            rotateAxis = InverseHandedness(rotateAxis);

            cameraPosition = new Vector3(3.0f, 4.0f, -5.0f);
            objectPosition = cameraPosition + placeDirection * 10.0f;
            expected = expectedRotationLeftHanded * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualLH = Matrix4x4.CreateConstrainedBillboardLeftHanded(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), Vector3.UnitZ);
            Assert.True(MathHelper.Equal(expected, actualLH), $"{nameof(Matrix4x4.CreateConstrainedBillboardLeftHanded)} did not return the expected value.");

            // When you move camera along rotateAxis, result must be same.
            cameraPosition += rotateAxis * 10.0f;
            Matrix4x4 actualTranslatedUpLH = Matrix4x4.CreateConstrainedBillboardLeftHanded(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), Vector3.UnitZ);
            Assert.True(MathHelper.Equal(expected, actualTranslatedUpLH), $"{nameof(Matrix4x4.CreateConstrainedBillboardLeftHanded)} did not return the expected value.");

            cameraPosition -= rotateAxis * 30.0f;
            Matrix4x4 actualTranslatedDownLH = Matrix4x4.CreateConstrainedBillboardLeftHanded(objectPosition, cameraPosition, rotateAxis, new Vector3(0, 0, -1), Vector3.UnitZ);
            Assert.True(MathHelper.Equal(expected, actualTranslatedDownLH), $"{nameof(Matrix4x4.CreateConstrainedBillboardLeftHanded)} did not return the expected value.");

            AssertEqual(actualRH, InverseHandedness(actualLH), DefaultVarianceMatrix);
            AssertEqual(InverseHandedness(actualRH), actualLH, DefaultVarianceMatrix);

            AssertEqual(actualTranslatedUpRH, InverseHandedness(actualTranslatedUpLH), DefaultVarianceMatrix);
            AssertEqual(InverseHandedness(actualTranslatedUpRH), actualTranslatedUpLH, DefaultVarianceMatrix);

            AssertEqual(actualTranslatedDownRH, InverseHandedness(actualTranslatedDownLH), DefaultVarianceMatrix);
            AssertEqual(InverseHandedness(actualTranslatedDownRH), actualTranslatedDownLH, DefaultVarianceMatrix);
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Forward side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest01()
        {
            // Object placed at Forward of camera. result must be same as 180 degrees rotate along y-axis.
            CreateConstrainedBillboardFact(
                new Vector3(0, 0, -1),
                Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Backward side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest02()
        {
            // Object placed at Backward of camera. This result must be same as 0 degrees rotate along y-axis.
            CreateConstrainedBillboardFact(
                Vector3.UnitZ,
                Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(0)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(0)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Right side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest03()
        {
            // Place object at Right side of camera. This result must be same as 90 degrees rotate along y-axis.
            CreateConstrainedBillboardFact(
                Vector3.UnitX,
                Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(90)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Left side of camera on XZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest04()
        {
            // Place object at Left side of camera. This result must be same as -90 degrees rotate along y-axis.
            CreateConstrainedBillboardFact(
                new Vector3(-1, 0, 0),
                Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(90)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Up side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest05()
        {
            // Place object at Up side of camera. result must be same as 180 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateConstrainedBillboardFact(
                Vector3.UnitY,
                Vector3.UnitZ,
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(180)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(180)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Down side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest06()
        {
            // Place object at Down side of camera. result must be same as 0 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateConstrainedBillboardFact(
                new Vector3(0, -1, 0),
                Vector3.UnitZ,
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(0)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(0)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Right side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest07()
        {
            // Place object at Right side of camera. result must be same as 90 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateConstrainedBillboardFact(
                Vector3.UnitX,
                Vector3.UnitZ,
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Left side of camera on XY-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest08()
        {
            // Place object at Left side of camera. result must be same as -90 degrees rotate along z-axis after 90 degrees rotate along x-axis.
            CreateConstrainedBillboardFact(
                new Vector3(-1, 0, 0),
                Vector3.UnitZ,
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Up side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest09()
        {
            // Place object at Up side of camera. result must be same as -90 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateConstrainedBillboardFact(
                Vector3.UnitY,
                new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Down side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest10()
        {
            // Place object at Down side of camera. result must be same as 90 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateConstrainedBillboardFact(
                new Vector3(0, -1, 0),
                new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Forward side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest11()
        {
            // Place object at Forward side of camera. result must be same as 180 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateConstrainedBillboardFact(
                new Vector3(0, 0, -1),
                new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(180.0f)),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Place object at Backward side of camera on YZ-plane
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTest12()
        {
            // Place object at Backward side of camera. result must be same as 0 degrees rotate along x-axis after 90 degrees rotate along z-axis.
            CreateConstrainedBillboardFact(
                Vector3.UnitZ,
                new Vector3(-1, 0, 0),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(0.0f)),
                Matrix4x4.CreateRotationZ(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationX(MathHelper.ToRadians(0.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Object and camera positions are too close and doesn't pass cameraForwardVector.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTooCloseTest1()
        {
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition;
            Vector3 cameraUpVector = Vector3.UnitY;

            // Doesn't pass camera face direction. CreateConstrainedBillboard uses new Vector3f(0, 0, -1) direction. Result must be same as 180 degrees rotate along y-axis.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualRH = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, cameraUpVector, Vector3.UnitZ, new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actualRH), $"{nameof(Matrix4x4.CreateConstrainedBillboard)} did not return the expected value.");

            objectPosition = new Vector3(3.0f, 4.0f, -5.0f);
            cameraPosition = objectPosition;
            cameraUpVector = Vector3.UnitY;

            expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualLH = Matrix4x4.CreateConstrainedBillboardLeftHanded(objectPosition, cameraPosition, cameraUpVector, new Vector3(0, 0, -1), Vector3.UnitZ);
            Assert.True(MathHelper.Equal(expected, actualLH), $"{nameof(Matrix4x4.CreateConstrainedBillboardLeftHanded)} did not return the expected value.");

            AssertEqual(actualRH, InverseHandedness(actualLH), DefaultVarianceMatrix);
            AssertEqual(InverseHandedness(actualRH), actualLH, DefaultVarianceMatrix);
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Object and camera positions are too close and passed cameraForwardVector.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardTooCloseTest2()
        {
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition;
            Vector3 cameraUpVector = Vector3.UnitY;

            // Passes Vector3f.Right as camera face direction. Result must be same as -90 degrees rotate along y-axis.
            Matrix4x4 expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualRH = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, cameraUpVector, Vector3.UnitX, new Vector3(0, 0, -1));
            Assert.True(MathHelper.Equal(expected, actualRH), $"{nameof(Matrix4x4.CreateConstrainedBillboard)} did not return the expected value.");

            objectPosition = new Vector3(3.0f, 4.0f, -5.0f);
            cameraPosition = objectPosition;
            cameraUpVector = Vector3.UnitY;

            expected = Matrix4x4.CreateRotationY(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualLH = Matrix4x4.CreateConstrainedBillboardLeftHanded(objectPosition, cameraPosition, cameraUpVector, Vector3.UnitX, Vector3.UnitZ);
            Assert.True(MathHelper.Equal(expected, actualLH), $"{nameof(Matrix4x4.CreateConstrainedBillboardLeftHanded)} did not return the expected value.");

            AssertEqual(actualRH, InverseHandedness(actualLH), DefaultVarianceMatrix);
            AssertEqual(InverseHandedness(actualRH), actualLH, DefaultVarianceMatrix);
        }

        private static void Matrix4x4CreateConstrainedBillboardAlongAxisFact(Vector3 rotateAxis, Vector3 cameraForward, Vector3 objectForward, Matrix4x4 expectedRotationRightHanded, Matrix4x4 expectedRotationLeftHanded)
        {
            // Place camera at up side of object.
            Vector3 objectPosition = new Vector3(3.0f, 4.0f, 5.0f);
            Vector3 cameraPosition = objectPosition + rotateAxis * 10.0f;

            Matrix4x4 expected = expectedRotationRightHanded * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualLH = Matrix4x4.CreateConstrainedBillboard(objectPosition, cameraPosition, rotateAxis, cameraForward, objectForward);
            Assert.True(MathHelper.Equal(expected, actualLH), $"{nameof(Matrix4x4.CreateConstrainedBillboard)} did not return the expected value.");

            rotateAxis = InverseHandedness(rotateAxis);
            cameraForward = InverseHandedness(cameraForward);
            objectForward = InverseHandedness(objectForward);

            objectPosition = new Vector3(3.0f, 4.0f, -5.0f);
            cameraPosition = objectPosition + rotateAxis * 10.0f;

            expected = expectedRotationLeftHanded * Matrix4x4.CreateTranslation(objectPosition);
            Matrix4x4 actualRH = Matrix4x4.CreateConstrainedBillboardLeftHanded(objectPosition, cameraPosition, rotateAxis, cameraForward, objectForward);
            Assert.True(MathHelper.Equal(expected, actualRH), $"{nameof(Matrix4x4.CreateConstrainedBillboardLeftHanded)} did not return the expected value.");

            AssertEqual(actualRH, InverseHandedness(actualLH), DefaultVarianceMatrix);
            AssertEqual(InverseHandedness(actualRH), actualLH, DefaultVarianceMatrix);
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And use doesn't passed objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest1()
        {
            // In this case, CreateConstrainedBillboard picks new Vector3f(0, 0, -1) as object forward vector.
            Matrix4x4CreateConstrainedBillboardAlongAxisFact(
                Vector3.UnitY, new Vector3(0, 0, -1), new Vector3(0, 0, -1),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And user doesn't passed objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest2()
        {
            // In this case, CreateConstrainedBillboard picks new Vector3f(1, 0, 0) as object forward vector.
            Matrix4x4CreateConstrainedBillboardAlongAxisFact(
                new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 0, -1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And user passed correct objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest3()
        {
            // User passes correct objectForwardVector.
            Matrix4x4CreateConstrainedBillboardAlongAxisFact(
                Vector3.UnitY, new Vector3(0, 0, -1), new Vector3(0, 0, -1),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And user passed incorrect objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest4()
        {
            // User passes correct objectForwardVector.
            Matrix4x4CreateConstrainedBillboardAlongAxisFact(
                Vector3.UnitY, new Vector3(0, 0, -1), Vector3.UnitY,
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)),
                Matrix4x4.CreateRotationY(MathHelper.ToRadians(180.0f)));
        }

        // A test for CreateConstrainedBillboard (Vector3f, Vector3f, Vector3f, Vector3f?)
        // Angle between rotateAxis and camera to object vector is too small. And user passed incorrect objectForwardVector parameter.
        [Fact]
        public void Matrix4x4CreateConstrainedBillboardAlongAxisTest5()
        {
            // In this case, CreateConstrainedBillboard picks Vector3f.Right as object forward vector.
            Matrix4x4CreateConstrainedBillboardAlongAxisFact(
                new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 0, -1),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)),
                Matrix4x4.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix4x4.CreateRotationZ(MathHelper.ToRadians(-90.0f)));
        }

        // A test for CreateScale (Vector3f)
        [Fact]
        public void Matrix4x4CreateScaleTest1()
        {
            Vector3 scales = new Vector3(2.0f, 3.0f, 4.0f);
            Matrix4x4 expected = new Matrix4x4(
                2.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 3.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 4.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 actual = Matrix4x4.CreateScale(scales);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (Vector3f, Vector3f)
        [Fact]
        public void Matrix4x4CreateScaleCenterTest1()
        {
            Vector3 scale = new Vector3(3, 4, 5);
            Vector3 center = new Vector3(23, 42, 666);

            Matrix4x4 scaleAroundZero = Matrix4x4.CreateScale(scale, Vector3.Zero);
            Matrix4x4 scaleAroundZeroExpected = Matrix4x4.CreateScale(scale);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix4x4 scaleAroundCenter = Matrix4x4.CreateScale(scale, center);
            Matrix4x4 scaleAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateScale (float)
        [Fact]
        public void Matrix4x4CreateScaleTest2()
        {
            float scale = 2.0f;
            Matrix4x4 expected = new Matrix4x4(
                2.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 2.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 2.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 actual = Matrix4x4.CreateScale(scale);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (float, Vector3f)
        [Fact]
        public void Matrix4x4CreateScaleCenterTest2()
        {
            float scale = 5;
            Vector3 center = new Vector3(23, 42, 666);

            Matrix4x4 scaleAroundZero = Matrix4x4.CreateScale(scale, Vector3.Zero);
            Matrix4x4 scaleAroundZeroExpected = Matrix4x4.CreateScale(scale);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix4x4 scaleAroundCenter = Matrix4x4.CreateScale(scale, center);
            Matrix4x4 scaleAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateScale (float, float, float)
        [Fact]
        public void Matrix4x4CreateScaleTest3()
        {
            float xScale = 2.0f;
            float yScale = 3.0f;
            float zScale = 4.0f;
            Matrix4x4 expected = new Matrix4x4(
                2.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 3.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 4.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 actual = Matrix4x4.CreateScale(xScale, yScale, zScale);
            Assert.Equal(expected, actual);
        }

        // A test for CreateScale (float, float, float, Vector3f)
        [Fact]
        public void Matrix4x4CreateScaleCenterTest3()
        {
            Vector3 scale = new Vector3(3, 4, 5);
            Vector3 center = new Vector3(23, 42, 666);

            Matrix4x4 scaleAroundZero = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z, Vector3.Zero);
            Matrix4x4 scaleAroundZeroExpected = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z);
            Assert.True(MathHelper.Equal(scaleAroundZero, scaleAroundZeroExpected));

            Matrix4x4 scaleAroundCenter = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z, center);
            Matrix4x4 scaleAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z) * Matrix4x4.CreateTranslation(center);
            Assert.True(MathHelper.Equal(scaleAroundCenter, scaleAroundCenterExpected));
        }

        // A test for CreateTranslation (Vector3f)
        [Fact]
        public void Matrix4x4CreateTranslationTest1()
        {
            Vector3 position = new Vector3(2.0f, 3.0f, 4.0f);
            Matrix4x4 expected = new Matrix4x4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                2.0f, 3.0f, 4.0f, 1.0f);

            Matrix4x4 actual = Matrix4x4.CreateTranslation(position);
            Assert.Equal(expected, actual);
        }

        // A test for CreateTranslation (float, float, float)
        [Fact]
        public void Matrix4x4CreateTranslationTest2()
        {
            float xPosition = 2.0f;
            float yPosition = 3.0f;
            float zPosition = 4.0f;

            Matrix4x4 expected = new Matrix4x4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                2.0f, 3.0f, 4.0f, 1.0f);

            Matrix4x4 actual = Matrix4x4.CreateTranslation(xPosition, yPosition, zPosition);
            Assert.Equal(expected, actual);
        }

        // A test for Translation
        [Fact]
        public void Matrix4x4TranslationTest()
        {
            Matrix4x4 a = GenerateTestMatrix();
            Matrix4x4 b = a;

            // Transformed vector that has same semantics of property must be same.
            Vector3 val = new Vector3(a.M41, a.M42, a.M43);
            Assert.Equal(val, a.Translation);

            // Set value and get value must be same.
            val = new Vector3(1.0f, 2.0f, 3.0f);
            a.Translation = val;
            Assert.Equal(val, a.Translation);

            // Make sure it only modifies expected value of matrix.
            Assert.True(
                a.M11 == b.M11 && a.M12 == b.M12 && a.M13 == b.M13 && a.M14 == b.M14 &&
                a.M21 == b.M21 && a.M22 == b.M22 && a.M23 == b.M23 && a.M24 == b.M24 &&
                a.M31 == b.M31 && a.M32 == b.M32 && a.M33 == b.M33 && a.M34 == b.M34 &&
                a.M41 != b.M41 && a.M42 != b.M42 && a.M43 != b.M43 && a.M44 == b.M44);
        }

        // A test for Equals (Matrix4x4)
        [Fact]
        public void Matrix4x4EqualsTest1()
        {
            Matrix4x4 a = GenerateIncrementalMatrixNumber();
            Matrix4x4 b = GenerateIncrementalMatrixNumber();

            // case 1: compare between same values
            bool expected = true;
            bool actual = a.Equals(b);
            Assert.Equal(expected, actual);

            // case 2: compare between different values
            b.M11 = 11.0f;
            expected = false;
            actual = a.Equals(b);
            Assert.Equal(expected, actual);
        }

        // A test for IsIdentity
        [Fact]
        public void Matrix4x4IsIdentityTest()
        {
            Assert.True(Matrix4x4.Identity.IsIdentity);
            Assert.True(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1).IsIdentity);
            Assert.False(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0).IsIdentity);
        }

        // A test for Matrix4x4 (Matrix3x2)
        [Fact]
        public void Matrix4x4From3x2Test()
        {
            Matrix3x2 source = new Matrix3x2(1, 2, 3, 4, 5, 6);
            Matrix4x4 result = new Matrix4x4(source);

            Assert.Equal(source.M11, result.M11);
            Assert.Equal(source.M12, result.M12);
            Assert.Equal(0f, result.M13);
            Assert.Equal(0f, result.M14);

            Assert.Equal(source.M21, result.M21);
            Assert.Equal(source.M22, result.M22);
            Assert.Equal(0f, result.M23);
            Assert.Equal(0f, result.M24);

            Assert.Equal(0f, result.M31);
            Assert.Equal(0f, result.M32);
            Assert.Equal(1f, result.M33);
            Assert.Equal(0f, result.M34);

            Assert.Equal(source.M31, result.M41);
            Assert.Equal(source.M32, result.M42);
            Assert.Equal(0f, result.M43);
            Assert.Equal(1f, result.M44);
        }

        // A test for Matrix4x4 comparison involving NaN values
        [Fact]
        public void Matrix4x4EqualsNaNTest()
        {
            Matrix4x4 a = new Matrix4x4(float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 b = new Matrix4x4(0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 c = new Matrix4x4(0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 d = new Matrix4x4(0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 e = new Matrix4x4(0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 f = new Matrix4x4(0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 g = new Matrix4x4(0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 h = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 i = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0);
            Matrix4x4 j = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0, 0);
            Matrix4x4 k = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0, 0);
            Matrix4x4 l = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0, 0);
            Matrix4x4 m = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0, 0);
            Matrix4x4 n = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0, 0);
            Matrix4x4 o = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN, 0);
            Matrix4x4 p = new Matrix4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, float.NaN);

            Assert.False(a == new Matrix4x4());
            Assert.False(b == new Matrix4x4());
            Assert.False(c == new Matrix4x4());
            Assert.False(d == new Matrix4x4());
            Assert.False(e == new Matrix4x4());
            Assert.False(f == new Matrix4x4());
            Assert.False(g == new Matrix4x4());
            Assert.False(h == new Matrix4x4());
            Assert.False(i == new Matrix4x4());
            Assert.False(j == new Matrix4x4());
            Assert.False(k == new Matrix4x4());
            Assert.False(l == new Matrix4x4());
            Assert.False(m == new Matrix4x4());
            Assert.False(n == new Matrix4x4());
            Assert.False(o == new Matrix4x4());
            Assert.False(p == new Matrix4x4());

            Assert.True(a != new Matrix4x4());
            Assert.True(b != new Matrix4x4());
            Assert.True(c != new Matrix4x4());
            Assert.True(d != new Matrix4x4());
            Assert.True(e != new Matrix4x4());
            Assert.True(f != new Matrix4x4());
            Assert.True(g != new Matrix4x4());
            Assert.True(h != new Matrix4x4());
            Assert.True(i != new Matrix4x4());
            Assert.True(j != new Matrix4x4());
            Assert.True(k != new Matrix4x4());
            Assert.True(l != new Matrix4x4());
            Assert.True(m != new Matrix4x4());
            Assert.True(n != new Matrix4x4());
            Assert.True(o != new Matrix4x4());
            Assert.True(p != new Matrix4x4());

            Assert.False(a.Equals(new Matrix4x4()));
            Assert.False(b.Equals(new Matrix4x4()));
            Assert.False(c.Equals(new Matrix4x4()));
            Assert.False(d.Equals(new Matrix4x4()));
            Assert.False(e.Equals(new Matrix4x4()));
            Assert.False(f.Equals(new Matrix4x4()));
            Assert.False(g.Equals(new Matrix4x4()));
            Assert.False(h.Equals(new Matrix4x4()));
            Assert.False(i.Equals(new Matrix4x4()));
            Assert.False(j.Equals(new Matrix4x4()));
            Assert.False(k.Equals(new Matrix4x4()));
            Assert.False(l.Equals(new Matrix4x4()));
            Assert.False(m.Equals(new Matrix4x4()));
            Assert.False(n.Equals(new Matrix4x4()));
            Assert.False(o.Equals(new Matrix4x4()));
            Assert.False(p.Equals(new Matrix4x4()));

            Assert.False(a.IsIdentity);
            Assert.False(b.IsIdentity);
            Assert.False(c.IsIdentity);
            Assert.False(d.IsIdentity);
            Assert.False(e.IsIdentity);
            Assert.False(f.IsIdentity);
            Assert.False(g.IsIdentity);
            Assert.False(h.IsIdentity);
            Assert.False(i.IsIdentity);
            Assert.False(j.IsIdentity);
            Assert.False(k.IsIdentity);
            Assert.False(l.IsIdentity);
            Assert.False(m.IsIdentity);
            Assert.False(n.IsIdentity);
            Assert.False(o.IsIdentity);
            Assert.False(p.IsIdentity);

            Assert.True(a.Equals(a));
            Assert.True(b.Equals(b));
            Assert.True(c.Equals(c));
            Assert.True(d.Equals(d));
            Assert.True(e.Equals(e));
            Assert.True(f.Equals(f));
            Assert.True(g.Equals(g));
            Assert.True(h.Equals(h));
            Assert.True(i.Equals(i));
            Assert.True(j.Equals(j));
            Assert.True(k.Equals(k));
            Assert.True(l.Equals(l));
            Assert.True(m.Equals(m));
            Assert.True(n.Equals(n));
            Assert.True(o.Equals(o));
            Assert.True(p.Equals(p));
        }

        // A test to make sure these types are blittable directly into GPU buffer memory layouts
        [Fact]
        public unsafe void Matrix4x4SizeofTest()
        {
            Assert.Equal(64, sizeof(Matrix4x4));
            Assert.Equal(128, sizeof(Matrix4x4_2x));
            Assert.Equal(68, sizeof(Matrix4x4PlusFloat));
            Assert.Equal(136, sizeof(Matrix4x4PlusFloat_2x));
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix4x4_2x
        {
            private Matrix4x4 _a;
            private Matrix4x4 _b;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix4x4PlusFloat
        {
            private Matrix4x4 _v;
            private float _f;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Matrix4x4PlusFloat_2x
        {
            private Matrix4x4PlusFloat _a;
            private Matrix4x4PlusFloat _b;
        }

        // A test to make sure the fields are laid out how we expect
        [Fact]
        public unsafe void Matrix4x4FieldOffsetTest()
        {
            Matrix4x4 mat = new Matrix4x4();

            float* basePtr = &mat.M11; // Take address of first element
            Matrix4x4* matPtr = &mat; // Take address of whole matrix

            Assert.Equal(new IntPtr(basePtr), new IntPtr(matPtr));

            Assert.Equal(new IntPtr(basePtr + 0), new IntPtr(&mat.M11));
            Assert.Equal(new IntPtr(basePtr + 1), new IntPtr(&mat.M12));
            Assert.Equal(new IntPtr(basePtr + 2), new IntPtr(&mat.M13));
            Assert.Equal(new IntPtr(basePtr + 3), new IntPtr(&mat.M14));

            Assert.Equal(new IntPtr(basePtr + 4), new IntPtr(&mat.M21));
            Assert.Equal(new IntPtr(basePtr + 5), new IntPtr(&mat.M22));
            Assert.Equal(new IntPtr(basePtr + 6), new IntPtr(&mat.M23));
            Assert.Equal(new IntPtr(basePtr + 7), new IntPtr(&mat.M24));

            Assert.Equal(new IntPtr(basePtr + 8), new IntPtr(&mat.M31));
            Assert.Equal(new IntPtr(basePtr + 9), new IntPtr(&mat.M32));
            Assert.Equal(new IntPtr(basePtr + 10), new IntPtr(&mat.M33));
            Assert.Equal(new IntPtr(basePtr + 11), new IntPtr(&mat.M34));

            Assert.Equal(new IntPtr(basePtr + 12), new IntPtr(&mat.M41));
            Assert.Equal(new IntPtr(basePtr + 13), new IntPtr(&mat.M42));
            Assert.Equal(new IntPtr(basePtr + 14), new IntPtr(&mat.M43));
            Assert.Equal(new IntPtr(basePtr + 15), new IntPtr(&mat.M44));
        }

        [Fact]
        public void PerspectiveFarPlaneAtInfinityTest()
        {
            var nearPlaneDistance = 0.125f;
            var m = Matrix4x4.CreatePerspective(1.0f, 1.0f, nearPlaneDistance, float.PositiveInfinity);
            Assert.Equal(-1.0f, m.M33);
            Assert.Equal(-nearPlaneDistance, m.M43);
        }

        [Fact]
        public void PerspectiveFieldOfViewFarPlaneAtInfinityTest()
        {
            var nearPlaneDistance = 0.125f;
            var m = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60.0f), 1.5f, nearPlaneDistance, float.PositiveInfinity);
            Assert.Equal(-1.0f, m.M33);
            Assert.Equal(-nearPlaneDistance, m.M43);
        }

        [Fact]
        public void PerspectiveOffCenterFarPlaneAtInfinityTest()
        {
            var nearPlaneDistance = 0.125f;
            var m = Matrix4x4.CreatePerspectiveOffCenter(0.0f, 0.0f, 1.0f, 1.0f, nearPlaneDistance, float.PositiveInfinity);
            Assert.Equal(-1.0f, m.M33);
            Assert.Equal(-nearPlaneDistance, m.M43);
        }

        [Fact]
        public void Matrix4x4CreateBroadcastScalarTest()
        {
            Matrix4x4 a = Matrix4x4.Create(float.Pi);

            Assert.Equal(Vector4.Create(float.Pi), a.X);
            Assert.Equal(Vector4.Create(float.Pi), a.Y);
            Assert.Equal(Vector4.Create(float.Pi), a.Z);
            Assert.Equal(Vector4.Create(float.Pi), a.W);
        }

        [Fact]
        public void Matrix4x4CreateBroadcastVectorTest()
        {
            Matrix4x4 a = Matrix4x4.Create(Vector4.Create(float.Pi, float.E, float.PositiveInfinity, float.NegativeInfinity));

            Assert.Equal(Vector4.Create(float.Pi, float.E, float.PositiveInfinity, float.NegativeInfinity), a.X);
            Assert.Equal(Vector4.Create(float.Pi, float.E, float.PositiveInfinity, float.NegativeInfinity), a.Y);
            Assert.Equal(Vector4.Create(float.Pi, float.E, float.PositiveInfinity, float.NegativeInfinity), a.Z);
            Assert.Equal(Vector4.Create(float.Pi, float.E, float.PositiveInfinity, float.NegativeInfinity), a.W);
        }

        [Fact]
        public void Matrix4x4CreateVectorsTest()
        {
            Matrix4x4 a = Matrix4x4.Create(
                Vector4.Create(11.0f, 12.0f, 13.0f, 14.0f),
                Vector4.Create(21.0f, 22.0f, 23.0f, 24.0f),
                Vector4.Create(31.0f, 32.0f, 33.0f, 34.0f),
                Vector4.Create(41.0f, 42.0f, 43.0f, 44.0f)
            );

            Assert.Equal(Vector4.Create(11.0f, 12.0f, 13.0f, 14.0f), a.X);
            Assert.Equal(Vector4.Create(21.0f, 22.0f, 23.0f, 24.0f), a.Y);
            Assert.Equal(Vector4.Create(31.0f, 32.0f, 33.0f, 34.0f), a.Z);
            Assert.Equal(Vector4.Create(41.0f, 42.0f, 43.0f, 44.0f), a.W);
        }

        [Fact]
        public void Matrix4x4GetElementTest()
        {
            Matrix4x4 a = GenerateTestMatrix();

            Assert.Equal(a.M11, a.X.X);
            Assert.Equal(a.M11, a[0, 0]);
            Assert.Equal(a.M11, a.GetElement(0, 0));

            Assert.Equal(a.M12, a.X.Y);
            Assert.Equal(a.M12, a[0, 1]);
            Assert.Equal(a.M12, a.GetElement(0, 1));

            Assert.Equal(a.M13, a.X.Z);
            Assert.Equal(a.M13, a[0, 2]);
            Assert.Equal(a.M13, a.GetElement(0, 2));

            Assert.Equal(a.M14, a.X.W);
            Assert.Equal(a.M14, a[0, 3]);
            Assert.Equal(a.M14, a.GetElement(0, 3));

            Assert.Equal(a.M21, a.Y.X);
            Assert.Equal(a.M21, a[1, 0]);
            Assert.Equal(a.M21, a.GetElement(1, 0));

            Assert.Equal(a.M22, a.Y.Y);
            Assert.Equal(a.M22, a[1, 1]);
            Assert.Equal(a.M22, a.GetElement(1, 1));

            Assert.Equal(a.M23, a.Y.Z);
            Assert.Equal(a.M23, a[1, 2]);
            Assert.Equal(a.M23, a.GetElement(1, 2));

            Assert.Equal(a.M24, a.Y.W);
            Assert.Equal(a.M24, a[1, 3]);
            Assert.Equal(a.M24, a.GetElement(1, 3));

            Assert.Equal(a.M31, a.Z.X);
            Assert.Equal(a.M31, a[2, 0]);
            Assert.Equal(a.M31, a.GetElement(2, 0));

            Assert.Equal(a.M32, a.Z.Y);
            Assert.Equal(a.M32, a[2, 1]);
            Assert.Equal(a.M32, a.GetElement(2, 1));

            Assert.Equal(a.M33, a.Z.Z);
            Assert.Equal(a.M33, a[2, 2]);
            Assert.Equal(a.M33, a.GetElement(2, 2));

            Assert.Equal(a.M34, a.Z.W);
            Assert.Equal(a.M34, a[2, 3]);
            Assert.Equal(a.M34, a.GetElement(2, 3));

            Assert.Equal(a.M41, a.W.X);
            Assert.Equal(a.M41, a[3, 0]);
            Assert.Equal(a.M41, a.GetElement(3, 0));

            Assert.Equal(a.M42, a.W.Y);
            Assert.Equal(a.M42, a[3, 1]);
            Assert.Equal(a.M42, a.GetElement(3, 1));

            Assert.Equal(a.M43, a.W.Z);
            Assert.Equal(a.M43, a[3, 2]);
            Assert.Equal(a.M43, a.GetElement(3, 2));

            Assert.Equal(a.M44, a.W.W);
            Assert.Equal(a.M44, a[3, 3]);
            Assert.Equal(a.M44, a.GetElement(3, 3));
        }

        [Fact]
        public void Matrix4x4GetRowTest()
        {
            Matrix4x4 a = GenerateTestMatrix();

            Vector4 vx = new Vector4(a.M11, a.M12, a.M13, a.M14);
            Assert.Equal(vx, a.X);
            Assert.Equal(vx, a[0]);
            Assert.Equal(vx, a.GetRow(0));

            Vector4 vy = new Vector4(a.M21, a.M22, a.M23, a.M24);
            Assert.Equal(vy, a.Y);
            Assert.Equal(vy, a[1]);
            Assert.Equal(vy, a.GetRow(1));

            Vector4 vz = new Vector4(a.M31, a.M32, a.M33, a.M34);
            Assert.Equal(vz, a.Z);
            Assert.Equal(vz, a[2]);
            Assert.Equal(vz, a.GetRow(2));

            Vector4 vw = new Vector4(a.M41, a.M42, a.M43, a.M44);
            Assert.Equal(vw, a.W);
            Assert.Equal(vw, a[3]);
            Assert.Equal(vw, a.GetRow(3));
        }

        [Fact]
        public void Matrix4x4WithElementTest()
        {
            Matrix4x4 a = Matrix4x4.Identity;

            a[0, 0] = 11.0f;
            Assert.Equal(11.5f, a.WithElement(0, 0, 11.5f).M11);
            Assert.Equal(11.0f, a.M11);

            a[0, 1] = 12.0f;
            Assert.Equal(12.5f, a.WithElement(0, 1, 12.5f).M12);
            Assert.Equal(12.0f, a.M12);

            a[0, 2] = 13.0f;
            Assert.Equal(13.5f, a.WithElement(0, 2, 13.5f).M13);
            Assert.Equal(13.0f, a.M13);

            a[0, 3] = 14.0f;
            Assert.Equal(14.5f, a.WithElement(0, 3, 14.5f).M14);
            Assert.Equal(14.0f, a.M14);

            a[1, 0] = 21.0f;
            Assert.Equal(21.5f, a.WithElement(1, 0, 21.5f).M21);
            Assert.Equal(21.0f, a.M21);

            a[1, 1] = 22.0f;
            Assert.Equal(22.5f, a.WithElement(1, 1, 22.5f).M22);
            Assert.Equal(22.0f, a.M22);

            a[1, 2] = 23.0f;
            Assert.Equal(23.5f, a.WithElement(1, 2, 23.5f).M23);
            Assert.Equal(23.0f, a.M23);

            a[1, 3] = 24.0f;
            Assert.Equal(24.5f, a.WithElement(1, 3, 24.5f).M24);
            Assert.Equal(24.0f, a.M24);

            a[2, 0] = 31.0f;
            Assert.Equal(31.5f, a.WithElement(2, 0, 31.5f).M31);
            Assert.Equal(31.0f, a.M31);

            a[2, 1] = 32.0f;
            Assert.Equal(32.5f, a.WithElement(2, 1, 32.5f).M32);
            Assert.Equal(32.0f, a.M32);

            a[2, 2] = 33.0f;
            Assert.Equal(33.5f, a.WithElement(2, 2, 33.5f).M33);
            Assert.Equal(33.0f, a.M33);

            a[2, 3] = 34.0f;
            Assert.Equal(34.5f, a.WithElement(2, 3, 34.5f).M34);
            Assert.Equal(34.0f, a.M34);

            a[3, 0] = 41.0f;
            Assert.Equal(41.5f, a.WithElement(3, 0, 41.5f).M41);
            Assert.Equal(41.0f, a.M41);

            a[3, 1] = 42.0f;
            Assert.Equal(42.5f, a.WithElement(3, 1, 42.5f).M42);
            Assert.Equal(42.0f, a.M42);

            a[3, 2] = 43.0f;
            Assert.Equal(43.5f, a.WithElement(3, 2, 43.5f).M43);
            Assert.Equal(43.0f, a.M43);

            a[3, 3] = 44.0f;
            Assert.Equal(44.5f, a.WithElement(3, 3, 44.5f).M44);
            Assert.Equal(44.0f, a.M44);
        }

        [Fact]
        public void Matrix4x4WithRowTest()
        {
            Matrix4x4 a = Matrix4x4.Identity;

            a[0] = Vector4.Create(11.0f, 12.0f, 13.0f, 14.0f);
            Assert.Equal(Vector4.Create(11.5f, 12.5f, 13.5f, 14.5f), a.WithRow(0, Vector4.Create(11.5f, 12.5f, 13.5f, 14.5f)).X);
            Assert.Equal(Vector4.Create(11.0f, 12.0f, 13.0f, 14.0f), a.X);

            a[1] = Vector4.Create(21.0f, 22.0f, 23.0f, 24.0f);
            Assert.Equal(Vector4.Create(21.5f, 22.5f, 23.5f, 24.5f), a.WithRow(1, Vector4.Create(21.5f, 22.5f, 23.5f, 24.5f)).Y);
            Assert.Equal(Vector4.Create(21.0f, 22.0f, 23.0f, 24.0f), a.Y);

            a[2] = Vector4.Create(31.0f, 32.0f, 33.0f, 34.0f);
            Assert.Equal(Vector4.Create(31.5f, 32.5f, 33.5f, 34.5f), a.WithRow(2, Vector4.Create(31.5f, 32.5f, 33.5f, 34.5f)).Z);
            Assert.Equal(Vector4.Create(31.0f, 32.0f, 33.0f, 34.0f), a.Z);

            a[3] = Vector4.Create(41.0f, 42.0f, 43.0f, 44.0f);
            Assert.Equal(Vector4.Create(41.5f, 42.5f, 43.5f, 44.5f), a.WithRow(3, Vector4.Create(41.5f, 42.5f, 43.5f, 44.5f)).W);
            Assert.Equal(Vector4.Create(41.0f, 42.0f, 43.0f, 44.0f), a.W);
        }
    }
}
