using UnityEngine;
using Unity.Mathematics;
using MathNet.Numerics.LinearAlgebra;
using static UnityEngine.GraphicsBuffer;
using static Unity.Mathematics.math;

namespace VFD {
    using V = Vector<double>;

    public abstract class TangentBundle : MonoBehaviour {
        [SerializeField] protected Material surfMat;
        [SerializeField] protected Material tngtMat;
        protected GraphicsBuffer colBuf;
        protected GraphicsBuffer tngBuf;
        protected HeGeom geom;
        protected Mesh mesh;

        protected virtual void Start() {
            var filt = GetComponentInChildren<MeshFilter>();
            var rend = GetComponentInChildren<MeshRenderer>();
            mesh = HeComp.Weld(filt.sharedMesh);
            geom = new HeGeom(mesh, transform);
            filt.sharedMesh = mesh;
            rend.material = surfMat;
            colBuf = new GraphicsBuffer(Target.Structured, geom.nVerts,     12);
            tngBuf = new GraphicsBuffer(Target.Structured, geom.nFaces * 6, 12);
        }

        protected void UpdateCol(float[] values) {
            var n = geom.nVerts;
            var x = 0f;
            var vals = new float[n];
            var lrps = new Vector3[n];
            for (var i = 0; i < n; i++) {
                var vrt = geom.Verts[i];
                var val = values[vrt.vid];
                vals[i] = val;
                x = max(abs(val), x);
            }
            x = min(PI / 8, x);
            for (var i = 0; i < n; i++) lrps[i] = ColorMap.Color(vals[i], -x, x);
            colBuf.SetData(lrps);
            surfMat.SetBuffer("_Col", colBuf);
        }

        protected void UpdateTng(float3[] omega) {
            var n = geom.nFaces;
            var tngs = new Vector3[n * 6];
            var mlen = 0.3f * geom.MeanEdgeLength();
            for(var i = 0; i < n; i++){
                var face = geom.Faces[i];
                var field = omega[i] * mlen;
                var C = (float3)geom.Centroid(face);
                var (_, N) = geom.FaceNormal(face);
                field = ClampFieldLength(field, mlen);
                var fc1 = C - field + N * 0.005f;
                var fc2 = C + field + N * 0.005f;
                var v = fc2 - fc1;
                var vT = cross(N, v);
                tngs[i * 6 + 0] = fc1;
                tngs[i * 6 + 1] = fc2;
                tngs[i * 6 + 2] = fc2;
                tngs[i * 6 + 3] = fc2 - v * 0.2f + vT * 0.2f;
                tngs[i * 6 + 4] = fc2;
                tngs[i * 6 + 5] = fc2 - v * 0.2f - vT * 0.2f;
            }
            tngBuf.SetData(tngs);
            tngtMat.SetBuffer("_Line", tngBuf);
        }

        protected void UpdateTng(V omega) {
            UpdateTng(TangentField.InterpolateWhitney(omega, geom));
        }
    
        protected Vector3 ClampFieldLength(Vector3 field, float len) {
            var m = field.magnitude;
            return m > len ? field * len / m : field;
        }

        protected virtual void OnRenderObject() {
            tngtMat.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Lines, geom.nFaces * 6);
        }

        protected virtual void OnDestroy() {
            colBuf?.Dispose();
            tngBuf?.Dispose();
        }
    }
}
