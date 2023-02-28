using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace  VectorField {
    public class GeomContainer : MonoBehaviour {
        public enum Surface { blackBase, whiteBase }

        [SerializeField] protected Surface surface;
        [SerializeField] protected Material lineMat;
        [SerializeField] protected Material arrowMat;
        [SerializeField] protected bool showVertArrow;
        [SerializeField] protected bool showFaceArrow;
        [SerializeField] protected bool showFaceRibbon;
        public HeGeom geom { get; private set; }
        public Mesh mesh { get; private set; }
        public Material LineMat => lineMat;
        TangentRibbon faceRibbon;
        TangentVertArrow  vertArrow;
        TangentFaceArrow  faceArrow;
        Color arrowColor;
        bool flag;

        void OnValidate() {
            if (flag) SwitchSurface();
        }

        void Awake() {
            var f = GetComponentInChildren<MeshFilter>();
            mesh = HeComp.Weld(f.sharedMesh);
            geom = new HeGeom(mesh, transform);
            f.sharedMesh = mesh;
            SwitchSurface();
            flag = true;
        }

        void OnRenderObject() {
            if (showVertArrow && vertArrow != null) DrawArrows(vertArrow.buff, geom.nVerts);
            if (showFaceArrow && faceArrow != null) DrawArrows(faceArrow.buff, geom.nFaces);
            if (showFaceRibbon) DrawRibbons();
        }

        void SwitchSurface() {
            switch (surface) {
                case Surface.blackBase:
                    arrowColor = Color.white;
                    mesh.SetColors(Enumerable.Repeat(Color.black, geom.nVerts).ToArray());
                    break;
                case Surface.whiteBase:
                    arrowColor = Color.black;
                    mesh.SetColors(Enumerable.Repeat(Color.white, geom.nVerts).ToArray());
                    break;
            }
        }

        public void PutSingularityPoint(int vid) {
            var o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            o.transform.position = geom.Pos[vid];
            o.transform.localScale *= geom.MeanEdgeLength() * 0.3f;
        }

        public void BuildVertArrowBuffer(float3[] vecs) { vertArrow = new TangentVertArrow(vecs, geom); }
        public void BuildFaceArrowBuffer(float3[] vecs) { faceArrow = new TangentFaceArrow(vecs, geom); }
        
        
        public void BuildRibbonBuffer(float3[] faceVector, Gradient colScheme) {
            var n = math.min((int)(geom.nFaces * 0.5f), 2000);
            var l = math.min((int)(geom.nFaces * 0.1f), 400);
            faceRibbon = new TangentRibbon(faceVector, geom, n, l, colScheme);
        }
        
        void DrawArrows(GraphicsBuffer buff, int count) {
            arrowMat.SetBuffer("_Lines", buff);
            arrowMat.SetVector("_Color", arrowColor);
            arrowMat.SetPass(1);
            Graphics.DrawProceduralNow(MeshTopology.Lines, count * 6);
        }
        void DrawRibbons() {
            lineMat.SetBuffer("_Line", faceRibbon.tracerBuff);
            lineMat.SetBuffer("_Norm", faceRibbon.normalBuff);
            lineMat.SetBuffer("_Col",  faceRibbon.colourBuff);
            lineMat.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Lines, faceRibbon.nTracers);
        }

        void OnDestroy() {
            vertArrow?.Dispose();
            faceArrow?.Dispose();
            faceRibbon?.Dispose();
        }
    }
}
