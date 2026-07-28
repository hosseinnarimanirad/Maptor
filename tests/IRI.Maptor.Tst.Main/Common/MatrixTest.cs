using IRI.Maptor.Sta.Mathematics;

namespace IRI.Maptor.Tst.Main.Common;

/// <summary>
/// Characterization tests for the Matrix class. They lock in the current numeric
/// behavior (including exact values where geometry predicates depend on them)
/// so the internals can be optimized safely.
/// </summary>
public class MatrixTest
{
    #region Determinant

    [Fact]
    public void Determinant_1x1_ReturnsElement()
    {
        var m = new Matrix(new double[,] { { 7 } });

        Assert.Equal(7.0, m.Determinant);
    }

    [Fact]
    public void Determinant_2x2_KnownValue()
    {
        var m = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });

        Assert.Equal(-2.0, m.Determinant);
    }

    [Fact]
    public void Determinant_3x3_KnownValue_Exact()
    {
        var m = new Matrix(new double[,] { { 6, 1, 1 }, { 4, -2, 5 }, { 2, 8, 7 } });

        // exact equality on purpose: 3x3 determinants back sign-sensitive
        // geometry predicates (TopologyUtility.GetPointCircleRelation)
        Assert.Equal(-306.0, m.Determinant);
    }

    [Fact]
    public void Determinant_3x3_Singular_IsExactlyZero()
    {
        var m = new Matrix(new double[,] { { 1, 2, 3 }, { 1, 2, 3 }, { 4, 5, 6 } });

        Assert.Equal(0.0, m.Determinant);
    }

    [Fact]
    public void Determinant_4x4_KnownValue()
    {
        var m = new Matrix(new double[,]
        {
            { 1, 0, 2, -1 },
            { 3, 0, 0, 5 },
            { 2, 1, 4, -3 },
            { 1, 0, 5, 0 }
        });

        Assert.Equal(30.0, m.Determinant, 9);
    }

    [Fact]
    public void Determinant_NonSquare_Throws()
    {
        var m = new Matrix(2, 3);

        Assert.Throws<NonSquareMatrixException>(() => m.Determinant);
    }

    [Fact]
    public void Determinant_InCirclePredicate_SignContract()
    {
        // mirrors TopologyUtility.GetPointCircleRelation: circle through
        // (1,0), (0,1), (-1,0) counter-clockwise; sightly point (x0,y0)
        static double InCircleDet(double x0, double y0)
        {
            double x1 = 1, y1 = 0, x2 = 0, y2 = 1, x3 = -1, y3 = 0;

            var m = new Matrix(
            [
                [x1 - x0, x2 - x0, x3 - x0],
                [y1 - y0, y2 - y0, y3 - y0],
                [ x1 * x1 - x0 * x0 + y1 * y1 - y0 * y0,
                  x2 * x2 - x0 * x0 + y2 * y2 - y0 * y0,
                  x3 * x3 - x0 * x0 + y3 * y3 - y0 * y0 ]
            ]);

            return m.Determinant;
        }

        Assert.True(InCircleDet(0, 0) > 0);       // inside  -> In
        Assert.True(InCircleDet(2, 0) < 0);       // outside -> Out
        Assert.Equal(0.0, InCircleDet(0, -1));    // on circle -> On (exact zero)
    }

    #endregion

    #region Inverse / Adjoint / Cofactors

    [Fact]
    public void Inverse_2x2_KnownValue()
    {
        var m = new Matrix(new double[,] { { 4, 7 }, { 2, 6 } });

        var inv = m.Inverse();

        Assert.Equal(0.6, inv[0, 0], 12);
        Assert.Equal(-0.7, inv[0, 1], 12);
        Assert.Equal(-0.2, inv[1, 0], 12);
        Assert.Equal(0.4, inv[1, 1], 12);

        Assert.True(m * inv == Matrix.Identity(2));
    }

    [Fact]
    public void Inverse_3x3_UnitDeterminant_ExactIntegerInverse()
    {
        var m = new Matrix(new double[,] { { 1, 2, 3 }, { 0, 1, 4 }, { 5, 6, 0 } });

        var inv = m.Inverse();

        var expected = new double[,] { { -24, 18, 5 }, { 20, -15, -4 }, { -5, 4, 1 } };

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                Assert.Equal(expected[i, j], inv[i, j]);

        Assert.True(m * inv == Matrix.Identity(3));
    }

    [Fact]
    public void Inverse_Singular_DoesNotThrow_ProducesInfinityOrNaN()
    {
        var m = new Matrix(new double[,] { { 1, 2, 3 }, { 1, 2, 3 }, { 4, 5, 6 } });

        var inv = m.Inverse();

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                Assert.True(double.IsInfinity(inv[i, j]) || double.IsNaN(inv[i, j]));
    }

    [Fact]
    public void Inverse_NonSquare_Throws()
    {
        var m = new Matrix(2, 3);

        Assert.Throws<NonSquareMatrixException>(() => m.Inverse());
    }

    [Fact]
    public void Adjoint_2x2_KnownValue()
    {
        var m = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });

        var adj = m.Adjoint();

        Assert.Equal(4.0, adj[0, 0]);
        Assert.Equal(-2.0, adj[0, 1]);
        Assert.Equal(-3.0, adj[1, 0]);
        Assert.Equal(1.0, adj[1, 1]);
    }

    [Fact]
    public void CofactorOf_MinorOf_3x3_KnownValues()
    {
        var m = new Matrix(new double[,] { { 6, 1, 1 }, { 4, -2, 5 }, { 2, 8, 7 } });

        Assert.Equal(-54.0, m.CofactorOf(0, 0));   // det {{-2,5},{8,7}}
        Assert.Equal(-18.0, m.CofactorOf(0, 1));   // -det {{4,5},{2,7}}
        Assert.Equal(1.0, m.CofactorOf(1, 0));     // -det {{1,1},{8,7}}
        Assert.Equal(18.0, m.MinorOf(0, 1));
    }

    [Fact]
    public void CofactorMatrixOf_ReturnsSubmatrix_DoesNotMutateSource()
    {
        var m = new Matrix(new double[,] { { 6, 1, 1 }, { 4, -2, 5 }, { 2, 8, 7 } });

        var sub = m.CofactorMatrixOf(0, 1);

        Assert.Equal(2, sub.NumberOfRows);
        Assert.Equal(2, sub.NumberOfColumns);
        Assert.Equal(4.0, sub[0, 0]);
        Assert.Equal(5.0, sub[0, 1]);
        Assert.Equal(2.0, sub[1, 0]);
        Assert.Equal(7.0, sub[1, 1]);

        // source untouched
        Assert.Equal(3, m.NumberOfRows);
        Assert.Equal(1.0, m[0, 1]);
    }

    #endregion

    #region Element-wise operations

    [Fact]
    public void Multiply_2x2_KnownValue()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var b = new Matrix(new double[,] { { 5, 6 }, { 7, 8 } });

        var c = a * b;

        Assert.Equal(19.0, c[0, 0]);
        Assert.Equal(22.0, c[0, 1]);
        Assert.Equal(43.0, c[1, 0]);
        Assert.Equal(50.0, c[1, 1]);
    }

    [Fact]
    public void Multiply_NonConformable_Throws()
    {
        var a = new Matrix(2, 3);
        var b = new Matrix(2, 3);

        Assert.Throws<ImproperMatrixSizeForMultiplicationException>(() => a * b);
    }

    [Fact]
    public void Multiply_RectangularShapes_KnownValue()
    {
        var a = new Matrix(new double[,] { { 1, 2, 3 }, { 4, 5, 6 } });          // 2x3
        var b = new Matrix(new double[,] { { 7, 8 }, { 9, 10 }, { 11, 12 } });   // 3x2

        var c = a * b;   // 2x2

        Assert.Equal(58.0, c[0, 0]);
        Assert.Equal(64.0, c[0, 1]);
        Assert.Equal(139.0, c[1, 0]);
        Assert.Equal(154.0, c[1, 1]);
    }

    [Fact]
    public void Add_Subtract_DotMultiply_KnownValues()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var b = new Matrix(new double[,] { { 5, 6 }, { 7, 8 } });

        var sum = a + b;
        var diff = b - a;
        var dot = a.DotMultiply(b);

        Assert.Equal(6.0, sum[0, 0]);
        Assert.Equal(12.0, sum[1, 1]);
        Assert.Equal(4.0, diff[0, 0]);
        Assert.Equal(4.0, diff[1, 1]);
        Assert.Equal(5.0, dot[0, 0]);
        Assert.Equal(32.0, dot[1, 1]);
    }

    [Fact]
    public void Add_Subtract_DotMultiply_SizeMismatch_Throw()
    {
        var a = new Matrix(2, 2);
        var b = new Matrix(2, 3);

        Assert.Throws<UnequalMatrixSizeException>(() => a + b);
        Assert.Throws<UnequalMatrixSizeException>(() => a - b);
        Assert.Throws<UnequalMatrixSizeException>(() => a.DotMultiply(b));
    }

    [Fact]
    public void ScalarMultiply_KnownValue()
    {
        var a = new Matrix(new double[,] { { 1, -2 }, { 3, 4 } });

        var c = 2 * a;

        Assert.Equal(2.0, c[0, 0]);
        Assert.Equal(-4.0, c[0, 1]);
        Assert.Equal(6.0, c[1, 0]);
        Assert.Equal(8.0, c[1, 1]);
    }

    [Fact]
    public void Transpose_NonSquare_KnownValue()
    {
        var a = new Matrix(new double[,] { { 1, 2, 3 }, { 4, 5, 6 } });

        var t = a.Transpose();

        Assert.Equal(3, t.NumberOfRows);
        Assert.Equal(2, t.NumberOfColumns);
        Assert.Equal(1.0, t[0, 0]);
        Assert.Equal(4.0, t[0, 1]);
        Assert.Equal(3.0, t[2, 0]);
        Assert.Equal(6.0, t[2, 1]);
    }

    [Fact]
    public void Clone_IsDeep()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });

        var clone = a.Clone();
        clone[0, 0] = 99;

        Assert.Equal(1.0, a[0, 0]);
        Assert.Equal(99.0, clone[0, 0]);
    }

    [Fact]
    public void SubMatrix_InteriorRange_KnownValue()
    {
        var a = new Matrix(new double[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } });

        var s = a.SubMatrix(0, 1, 1, 2);

        Assert.Equal(2, s.NumberOfRows);
        Assert.Equal(2, s.NumberOfColumns);
        Assert.Equal(2.0, s[0, 0]);
        Assert.Equal(3.0, s[0, 1]);
        Assert.Equal(5.0, s[1, 0]);
        Assert.Equal(6.0, s[1, 1]);
    }

    [Fact]
    public void SubMatrix_StartAfterEnd_Throws()
    {
        var a = new Matrix(3, 3);

        Assert.Throws<IllegalInputException>(() => a.SubMatrix(2, 0, 1, 2));
    }

    [Fact]
    public void Negate_MutatesInPlace()
    {
        var a = new Matrix(new double[,] { { 1, -2 }, { 3, 4 } });

        var result = a.Negate();

        // current contract: in-place negation returning the same instance
        Assert.Same(a, result);
        Assert.Equal(-1.0, a[0, 0]);
        Assert.Equal(2.0, a[0, 1]);
        Assert.Equal(-3.0, a[1, 0]);
        Assert.Equal(-4.0, a[1, 1]);
    }

    [Fact]
    public void UnaryMinus_DoesNotMutateOperand()
    {
        var a = new Matrix(new double[,] { { 1, -2 }, { 3, 4 } });

        var neg = -a;

        Assert.Equal(-1.0, neg[0, 0]);
        Assert.Equal(2.0, neg[0, 1]);
        Assert.Equal(1.0, a[0, 0]);
        Assert.Equal(-2.0, a[0, 1]);
    }

    [Fact]
    public void Trace_KnownValue_And_NonSquareThrows()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });

        Assert.Equal(5.0, a.Trace);
        Assert.Throws<NonSquareMatrixException>(() => new Matrix(2, 3).Trace);
    }

    [Fact]
    public void SumOfElements_KnownValue()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });

        Assert.Equal(10.0, a.SumOfElements);
    }

    #endregion

    #region Rows and columns

    [Fact]
    public void GetRow_GetColumn_KnownValues()
    {
        var a = new Matrix(new double[,] { { 1, 2, 3 }, { 4, 5, 6 } });

        Assert.Equal(new double[] { 1, 2, 3 }, a.GetRow(0));
        Assert.Equal(new double[] { 4, 5, 6 }, a.GetRow(1));
        Assert.Equal(new double[] { 2, 5 }, a.GetColumn(1));
    }

    [Fact]
    public void SetRow_SetColumn_KnownValues()
    {
        var a = new Matrix(2, 2);

        a.SetRow(0, new double[] { 1, 2 });
        a.SetColumn(1, new double[] { 7, 8 });

        Assert.Equal(1.0, a[0, 0]);
        Assert.Equal(7.0, a[0, 1]);
        Assert.Equal(8.0, a[1, 1]);
    }

    [Fact]
    public void SetRow_WrongLength_Throws()
    {
        var a = new Matrix(2, 2);

        Assert.Throws<NumberOfElementsException>(() => a.SetRow(0, new double[] { 1, 2, 3 }));
        Assert.Throws<NumberOfElementsException>(() => a.SetColumn(0, new double[] { 1, 2, 3 }));
    }

    [Fact]
    public void SwapRows_SwapColumns_KnownValues()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        a.SwapRows(0, 1);

        Assert.Equal(3.0, a[0, 0]);
        Assert.Equal(4.0, a[0, 1]);
        Assert.Equal(1.0, a[1, 0]);
        Assert.Equal(2.0, a[1, 1]);

        var b = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        b.SwapColumns(0, 1);

        Assert.Equal(2.0, b[0, 0]);
        Assert.Equal(1.0, b[0, 1]);
        Assert.Equal(4.0, b[1, 0]);
        Assert.Equal(3.0, b[1, 1]);
    }

    #endregion

    #region Eigenvalues / eigenvectors (Jacobi)

    [Fact]
    public void Eigen_2x2_EqualDiagonal_TerminatesWithKnownValues()
    {
        var m = new Matrix(new double[,] { { 2, 1 }, { 1, 2 } });

        var eig = (double[])m.GetEigenvaluesEigenvectors().Eigenvalues.Clone();
        Array.Sort(eig);

        Assert.Equal(1.0, eig[0], 6);
        Assert.Equal(3.0, eig[1], 6);
    }

    [Fact]
    public void Eigen_3x3_KnownValues_AndPairing()
    {
        var m = new Matrix(new double[,] { { 2, 0, 0 }, { 0, 3, 4 }, { 0, 4, 9 } });

        var result = m.GetEigenvaluesEigenvectors();

        var sorted = (double[])result.Eigenvalues.Clone();
        Array.Sort(sorted);

        Assert.Equal(1.0, sorted[0], 6);
        Assert.Equal(2.0, sorted[1], 6);
        Assert.Equal(11.0, sorted[2], 6);

        // pairing contract: A * v_i == lambda_i * v_i for eigenvector column i
        for (int i = 0; i < 3; i++)
        {
            var v = new Matrix(3, 1);
            v.SetColumn(0, result.EigenvectorMatrix.GetColumn(i));

            var av = m * v;

            for (int j = 0; j < 3; j++)
                Assert.Equal(result.Eigenvalues[i] * v[j, 0], av[j, 0], 6);
        }
    }

    [Fact]
    public void Eigen_SumIsTrace_ProductIsDeterminant()
    {
        var m = new Matrix(new double[,] { { 4, 1, 1 }, { 1, 3, 0 }, { 1, 0, 2 } });

        var eig = m.GetEigenvaluesEigenvectors().Eigenvalues;

        Assert.Equal(m.Trace, eig[0] + eig[1] + eig[2], 6);
        Assert.Equal(m.Determinant, eig[0] * eig[1] * eig[2], 6);
    }

    [Fact]
    public void Eigen_NonSymmetric_Throws()
    {
        var m = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });

        Assert.Throws<NotImplementedException>(() => m.GetEigenvaluesEigenvectors());
    }

    #endregion

    #region Predicates

    [Fact]
    public void IsSymmetric_TrueFalse_AndToleranceBoundary()
    {
        Assert.True(new Matrix(new double[,] { { 1, 2 }, { 2, 1 } }).IsSymmetric());
        Assert.False(new Matrix(new double[,] { { 1, 2 }, { 3, 1 } }).IsSymmetric());

        // off-symmetry below the 10-decimal rounding threshold counts as symmetric
        Assert.True(new Matrix(new double[,] { { 1, 2 }, { 2 + 1e-11, 1 } }).IsSymmetric());
        Assert.False(new Matrix(new double[,] { { 1, 2 }, { 2 + 1e-9, 1 } }).IsSymmetric());

        // non-square: returns false, does not throw
        Assert.False(new Matrix(2, 3).IsSymmetric());
    }

    [Fact]
    public void IsDiagonal_TrueFalse_AndNonSquareThrows()
    {
        Assert.True(Matrix.DiagonalMatrix(new double[] { 2, 3 }).IsDiagonal());
        Assert.False(new Matrix(new double[,] { { 1, 1 }, { 0, 1 } }).IsDiagonal());
        Assert.Throws<NonSquareMatrixException>(() => new Matrix(2, 3).IsDiagonal());
    }

    [Fact]
    public void IsOrthogonal_TrueFalse_AndNonSquareThrows()
    {
        Assert.True(Matrix.RotateAboutZ(0.3).IsOrthogonal());
        Assert.False(new Matrix(new double[,] { { 1, 1 }, { 0, 1 } }).IsOrthogonal());
        Assert.Throws<NonSquareMatrixException>(() => new Matrix(2, 3).IsOrthogonal());
    }

    #endregion

    #region ToString / equality

    [Fact]
    public void ToString_KnownFormat()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });

        Assert.Equal("1,2;3,4;", a.ToString());
    }

    [Fact]
    public void EqualityOperator_RoundsTo10Decimals()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var b = new Matrix(new double[,] { { 1 + 1e-11, 2 }, { 3, 4 } });
        var c = new Matrix(new double[,] { { 1 + 1e-9, 2 }, { 3, 4 } });

        Assert.True(a == b);
        Assert.False(a == c);
        Assert.True(a != c);
    }

    [Fact]
    public void EqualityOperator_SizeMismatch_IsFalse()
    {
        Assert.False(new Matrix(2, 2) == new Matrix(2, 3));
    }

    [Fact]
    public void EqualityOperator_NullHandling()
    {
        Matrix nullMatrix = null;

        Assert.True(nullMatrix == null);
        Assert.False(new Matrix(2, 2) == null);
        Assert.False(null == new Matrix(2, 2));
    }

    #endregion
}
