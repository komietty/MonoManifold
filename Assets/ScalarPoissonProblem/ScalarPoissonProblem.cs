using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;
using ddg;

public class ScalarPoissonProblem : MonoMfdViewer {
    protected Matrix<double> phi;

    protected override void Start() {
        base.Start();
        SolveScalarPoissonProblem(geom, new int[] { 0, 5 });
        UpdateColor();
    }

    /*
     * Computes the solution of the poisson problem Ax = -M(rho - rhoBar),
     * where A is the positive definite laplace matrix and M is the mass matrix.
	 * rho: A scalar density of vertices of the input mesh.
    */
    Matrix<double> Solve(HalfEdgeGeom geom, DenseMatrix rho){
        var M = Operator.Mass(geom);
        var A = Operator.Laplace(geom);
        var T = geom.TotalArea();
        var rhoSum = (M * rho).RowSums().Sum();
        var rhoBar = DenseMatrix.Create(M.RowCount, 1, rhoSum / T);
        var rhoDif = rho - rhoBar;
        var B = - M * rhoDif;
        //var LLT = A.Cholesky(); // must be very naive chol decomp
        //return LLT.Solve(B);
        var LLT = A.LU();
        return LLT.Solve(B);
    }

    void SolveScalarPoissonProblem(HalfEdgeGeom geom, int[] vertexIds) {
        var rho = DenseMatrix.Create(geom.nVerts, 1, 0);
        foreach(var i in vertexIds) rho[i, 0] = 1;
        phi = Solve(geom, rho);
    }

    protected override float GetValueOnSurface(Vert v) {
        return (float)phi[v.vid, 0];
    }

}
