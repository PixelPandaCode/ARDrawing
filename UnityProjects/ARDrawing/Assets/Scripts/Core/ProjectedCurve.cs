using System.Collections.Generic;
using UnityEngine;

namespace StrokeMimicry
{
    // The ProjectedCurve component contains stores the data that can be used to reconstruct a stroke.
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer))]
    public class ProjectedCurve : MonoBehaviour
    {
        // Projected stroke points. Strictly speaking, this information is redundant.
        public List<Vector3> Points { get; private set; }
        
        // Information about each projected point. See details of the HitInfo class for more information.
        public List<HitInfo> HitInfoFrames { get; private set; }

        // Projection Mode used for crating this curve.
        public ProjectionMode ProjMode { get; private set; }
        
        // Model Matrix of the Target when the stroke was created.
        public Matrix4x4 ModelMatrix = Matrix4x4.identity;

        // Internal class that creates the mesh for rendering the curve.
        //private CurveMeshBuilder MeshBuilder;

        public int PointCount { get { return Points.Count; } }

        public void Init(ProjectionMode mode, Matrix4x4 modelMat)
        {
            Points = new List<Vector3>();
            HitInfoFrames = new List<HitInfo>();
            ProjMode = mode;
            ModelMatrix = modelMat;
            //MeshBuilder = new CurveMeshBuilder(this);
            gameObject.GetComponent<MeshRenderer>().material = StrokeMimicryManager.Instance.StrokeMaterial;
        }

        public bool TryDrawPoint(HitInfo hitInfo)
        {
            bool drawn = false;
            // Unsuccessful hit -> finish the current curve
            if (hitInfo.Success == false)
            {
                Finish();
            }
            // Successful hit -> Add a point, and create the corresponding mesh segment.
            else
            {
                AddPointAndHitInfo(hitInfo);
                drawn = true;
            }
            Vector4 mwp = Vector4.positiveInfinity;
            mwp = transform.TransformPoint(hitInfo.Point);
            mwp.w = hitInfo.Success ? 1 : 0;
            //Projection.CurrentCursor = mwp;
            //TP.UpdateCursor(mwp);
            return drawn;
        }


        public void Finish()
        {
            Debug.Assert(Points.Count == HitInfoFrames.Count);
            // MeshBuilder.Finish();

            // Projection.CurrentCurve = null;
        }

        public void AddPointAndHitInfo(HitInfo hit)
        {
            Debug.Assert(hit != null);
            HitInfoFrames.Add(new HitInfo(hit));
            Points.Add(hit.Point);
        }
    }
}
