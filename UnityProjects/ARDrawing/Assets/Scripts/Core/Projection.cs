using System;
using UnityEngine;
using g3;
using ExtensionMethods;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine.InputSystem;
using MixedReality.Toolkit.Input;

namespace StrokeMimicry
{
    // Projection is the main class for stroke mimicry and other projection operations described in the paper.
    // This implementation provides three projection methods: Spraypaint, Mimicry (using Phong Projection for closest-point like operations), and Mimicry using standard closest-point operation.
    // Adding additional projection techniques that fit within the anchored projection framework is easy. See the Project() function.
    public class Projection : MonoBehaviour
    {
        // Projections can only be performed if a target object and a pen are available.
        public bool IsReady
        {
            get
            {
                if (Target is null || !Target.gameObject.activeInHierarchy)
                    return false;

                if (PenObject is null || !PenObject.gameObject.activeInHierarchy)
                    return false;

                return true;
            }
        }

        // Phong projection requires pre-processed files produced via the MATLAB code.
        public bool IsPhongProjectionAvailable
        {
            get
            {
                if (!IsReady)
                    return false;

                return !(Target.Phong.IsNull);
            }
        }

        // Target object containing a surface mesh.
        private StrokeMimicryTarget _target;
        public StrokeMimicryTarget Target
        {
            get => _target;

            set
            {
                _target = value;
                //try
                //{
                //    _target = value;
                //    string surfMeshFile = System.IO.Path.Combine(
                //        StrokeMimicryManager.Instance.PhongFilesPath,
                //        _target.Name + ".obj");

                //    if (!System.IO.File.Exists(surfMeshFile))
                //        Debug.LogError("Unable to find file " + surfMeshFile + ". This file is required for all projection functions. Perhaps you forgot to copy this file to " + StrokeMimicryManager.Instance.PhongFilesPath + " or the target model name is not set?");


                //    StandardMeshReader surfMeshReader = new StandardMeshReader();
                //    surfMeshReader.MeshBuilder = new DMesh3Builder();
                //    var surfMeshReadResult = surfMeshReader.Read(surfMeshFile, new ReadOptions());
                //    Debug.Assert(surfMeshReadResult.code == IOCode.Ok, "Unable to read target surface mesh from " + surfMeshFile);
                //    SurfMesh = ((DMesh3Builder)surfMeshReader.MeshBuilder).Meshes[0];

                //    foreach (var vidx in SurfMesh.VertexIndices())
                //    {
                //        SurfMesh.SetVertex(vidx, StrokeMimicryUtils.ChangeHandedness((Vector3)SurfMesh.GetVertex(vidx)));
                //        SurfMesh.SetVertexNormal(vidx, StrokeMimicryUtils.ChangeHandedness(SurfMesh.GetVertexNormal(vidx)));
                //    }

                //    SurfMeshTree = new DMeshAABBTree3(SurfMesh, true);
                //    // Debug.Assert(SurfMesh.IsClosed(), "Target surface mesh needs to be closed!");
                //    SurfMeshTree.FastWindingNumber(Vector3d.Zero);

                //    if (Target.Phong is null)
                //    {
                //        //Debug.LogWarning("Phong projection unavailable. Closest point queries will use vanilla version.");
                //    }
                //}
                //catch (Exception e)
                //{
                //    Debug.LogError(e.Message);
                //    _target = null;
                //}
            }
        }

        // Information about each projected point. See details of the HitInfo class for more information.
        public List<HitInfo> HitInfoFrames { get; private set; }
        // Projected stroke points. Strictly speaking, this information is redundant.
        public List<Vector3> Points { get; private set; }
        public int PointCount { get { return Points.Count; } }

        // Pen object, typically attached to a VR controller.
        private Pen _penObject;
        public Pen PenObject
        {
            get => _penObject;
            set => _penObject = value;
        }
        public Vector4 CurrentCursor;

        // This is non-null if a curve is being currentl drawn.
        //private ProjectedCurve _currentCurve = null;
        //public ProjectedCurve CurrentCurve
        //{
        //    get => _currentCurve;
        //    set => _currentCurve = value;
        //}

        // Unity's mesh data structures are not suited for gemoetry processing, so we use Ryan Schmidt's geometry3Sharp library.

        // A target 3D triangle mesh.
        private DMesh3 SurfMesh { get; set; } = null;
        // AABB tree on the above.
        private DMeshAABBTree3 SurfMeshTree { get; set; } = null;

        // The ray cast for the spraypaint technique. Note that spraypaint is also used for visualizing the projected point when no stroke is being drawn.
        private Ray SprayRay;

        // Current data frame, ray, and hit. These are updated very frame.
        private DataFrame CurrentDataFrame = null;
        private HitInfo CurrentHit;
        private Ray CurrentRay;

        public void Start()
        {
            HitInfoFrames = new List<HitInfo>();
            Points = new List<Vector3>();
            Target = FindObjectOfType<StrokeMimicryTarget>();
        }

        // Called very frame.
        public void InitFrame()
        {
            if (!IsReady)
                return;
            // All the data required from the current frame.
            CurrentDataFrame = new DataFrame(
                (DateTime.Now - StrokeMimicryManager.Instance.StartTime).TotalMilliseconds,
                Camera.main.transform.position,
                // PenObject.transform.TransformPoint(PenObject.PenTipPosition),
                PenObject.PenTipPosition,
                Camera.main.transform.up,
                Camera.main.transform.forward,
                PenObject.transform.up,
                PenObject.transform.forward
                );
            var rotation = Quaternion.LookRotation(
                CurrentDataFrame.ControllerForward,
                CurrentDataFrame.ControllerUp);

            // Current spraypaint ray
            SprayRay = new Ray(
                PenObject.PenTipPosition,
                PenObject.SprayDirection
            );
            // Debug.Log("Pentip: " + penTipGlobalPosition + " SprayRay: " + SprayRay.direction);

            // Set current ray to spraypaint. This may be updated by the Project() function.
            CurrentRay = SprayRay;

            // CurrentHit is the spraypaint raycast hit. May be updated by the Project() function.
            Raycast(CurrentRay, out CurrentHit);
        }

        //// Action button was just pressed. Try creating a new stroke.
        //public void TryCreateNewStroke()
        //{
        //    if (!IsReady)
        //        return;
        //    if (CurrentHit.Success)
        //    {
        //        GameObject strokeObject = new GameObject("Curve" + StrokeMimicryManager.Instance.NumCurve);
        //        CurrentCurve = strokeObject.AddComponent<ProjectedCurve>();
        //        strokeObject.transform.SetParent(Target.transform, false);
        //        CurrentCurve.Init(StrokeMimicryManager.Instance.ProjectionMode, Target.TargetTransform.localToWorldMatrix);
        //    }
        //}

        //// Action button released or hit did not succeed. Finish the current stroke.
        //public void TryFinishStroke()
        //{
        //    if (CurrentCurve != null)
        //        CurrentCurve.Finish();
        //}

        public bool TryDrawPoint(HitInfo hitInfo)
        {
            bool drawn = false;
            // Unsuccessful hit -> finish the current curve
            if (hitInfo.Success == true)
            {
                AddPointAndHitInfo(hitInfo);
                drawn = true;
            }
            CurrentCursor = Vector4.zero;
            CurrentCursor = Target.transform.TransformPoint(hitInfo.Point);
            CurrentCursor.w = hitInfo.Success ? 1 : 0;
            //TP.UpdateCursor(mwp);
            return drawn;
        }


        public void AddPointAndHitInfo(HitInfo hit)
        {
            Debug.Assert(hit != null);
            HitInfoFrames.Add(new HitInfo(hit));
            Points.Add(hit.Point);
            //if (Points.Count > 1 )
            //{
            //    if (Vector4.Distance(Points[Points.Count - 1], Points[Points.Count - 2]) > 0.2f)
            //    {
            //        Debug.Log(Points[Points.Count - 1] + " " + Points[Points.Count - 2]);
            //    }
            //}
        }

        // Main projection function. Only called when drawing a stroke.
        public void Project()
        {
            if (!IsReady)
                return;


            HitInfo hit;
            Ray ray;

            var penTipGlobalPosition = CurrentDataFrame.PenPosition;

            var transform = Target.TargetTransform;

            var frames = HitInfoFrames;
            Vector3 lastPointDrawn = Vector3.zero;
            Vector3 delta = Vector3.zero;

            // If we've already drawn at least one point, we can utilize information from the previous data frame for anchored projections.
            if (PointCount > 0)
            {
                var lastFrame = frames[frames.Count - 1];
                var lastUsedPenPosition = lastFrame.Frame.PenPosition;
                lastPointDrawn = lastFrame.Point;

                // convert to world coordinates
                lastUsedPenPosition = transform.TransformPoint(lastUsedPenPosition);
                lastPointDrawn = transform.TransformPoint(lastPointDrawn);

                // Δp_i = p_i - p_{i-1}
                delta = penTipGlobalPosition - lastUsedPenPosition;

                if (delta.magnitude < 1e-3f * StrokeMimicryManager.Instance.Epsilon)
                    return;
            }

            ProjectionMode projectionMode = StrokeMimicryManager.Instance.ProjectionMode;

            // For mimicry, fallback to closest-point if phong projection is unavailable
            if (projectionMode == ProjectionMode.MimicryPhong && !IsPhongProjectionAvailable)
            {
                projectionMode = ProjectionMode.MimicryClosest;
            }

            switch (projectionMode)
            {
                case ProjectionMode.Spraypaint:
                    {
                        ray = SprayRay;
                        hit = CurrentHit;
                        break;
                    }
                // anchored closest-point
                case ProjectionMode.MimicryClosest:
                    {
                        // When starting a new stroke, default to SprayRay
                        if (PointCount == 0)
                        {
                            ray = SprayRay;
                            hit = CurrentHit;
                            break;
                        }
                        // Later on, switch to "as-similar-as-possible" projection
                        // The basic idea is this: let delta = movement of the controller b/w the prev. frame and cur. frame
                        // Then, we want to find a point on the mesh which makes the segment from the prev. projectied point
                        // to it as close as possible to delta. If {p_i} and {q_i} are the 3D and projected strokes, then

                        // q_i = argmin_{q \in M} || (q - q_{i-1}) - (p_i - p_{i-1}) ||^2
                        // delta = p_i - p_{i-1}
                        // q_i = project_onto_M( q_{i-1} + delta )
                        // just use closest point projection which actually solves for
                        // q_i = argmin_{q \in M} || q - (q_{i-1} + delta) ||^2

                        hit = _ClosestHitVanilla(lastPointDrawn + delta);
                        ray = new Ray(
                            penTipGlobalPosition,
                            Target.TargetTransform.TransformPoint(hit.Point) - penTipGlobalPosition);
                        break;
                    }
                // anchored smooth closest-point (Mimicry)
                case ProjectionMode.MimicryPhong:
                    { // When starting a new stroke, default to SprayRay
                        if (PointCount == 0)
                        {
                            ray = SprayRay;
                            hit = CurrentHit;
                            break;
                        }

                        // q_i = project_onto_M( q_{i-1} + delta )
                        // Perform the projection using [Panozzo 2013] to estimate the projection on the ideal smooth
                        // surface encoded by the mesh M
                        hit = _ClosestHitPhong(lastPointDrawn + delta);
                        ray = new Ray(
                            penTipGlobalPosition,
                            Target.TargetTransform.TransformPoint(hit.Point) - penTipGlobalPosition);
                        break;
                    }
                // unreachable code, but required to prevent the compiler from complaining
                default:
                    hit = new HitInfo(CurrentDataFrame, Target.TargetTransform);
                    ray = new Ray(
                        penTipGlobalPosition,
                        Target.TargetTransform.TransformPoint(hit.Point) - penTipGlobalPosition);
                    break;
            }

            CurrentRay = ray;
            CurrentHit = hit;

            // Try drawing the point. This *always* draws a point if the hit was successful (hit.Success == true).
            TryDrawPoint(hit);
        }

        // UI updates: Projection pointer/laser transformation
        public void UpdateProjectionPointerAndLaser()
        {
            if (!IsReady)
                return;

            PenObject.UpdatePointerAndLaser(CurrentRay, CurrentHit, Target.TargetTransform);
        }

        // Phong projection (Smooth closest-point)
        private HitInfo _ClosestHitPhong(Vector3 p)
        {
            if (!IsPhongProjectionAvailable)
                return _ClosestHitVanilla(p);

            HitInfo hit = new HitInfo(CurrentDataFrame, Target.TargetTransform);

            var transform = Target.TargetTransform;

            // convert to right-handed point in model space
            // the input point `p` is left-handed and in the world space
            Vector3 positionModelSpace = StrokeMimicryUtils.ChangeHandedness(transform.InverseTransformPoint(p));

            var phong = Target.Phong;
            var res = phong.Project(
                positionModelSpace,
                out Vector3 projection,
                out int triangleIdx,
                out float[] bary,
                false,
                true);

            if (res == PhongProjectionResult.Success)
            {
                hit.Success = true;
                hit.TriangleIndex = triangleIdx;
                hit.BarycentricCoordinate = new Vector3(bary[0], bary[1], bary[2]);

                if (triangleIdx < 0)
                    Debug.LogError("Negative triangle idx!");
                else if (triangleIdx >= SurfMesh.TriangleCount)
                    Debug.LogError("Invalid triangle idx!");

                hit.Point = StrokeMimicryUtils.ChangeHandedness(projection);
                hit.Distance = (positionModelSpace - projection).magnitude;

                hit.Normal = (Vector3)SurfMesh.GetTriNormalLeftHanded(triangleIdx);
            }
            else
            {
                if (res != PhongProjectionResult.OutsideTetVolume)
                    Debug.LogWarning("Phong projection failed! Projection result: " + res.ToString());

                hit = _ClosestHitVanilla(p);
                hit.Success = true;
            }

            return hit;
        }


        // Standard closest point projection
        private HitInfo _ClosestHitVanilla(Vector3 p)
        {
            HitInfo hit = new HitInfo(CurrentDataFrame, Target.TargetTransform);

            if (!IsReady)
                return hit;

            p = Target.TargetTransform.InverseTransformPoint(p);

            if (SurfMeshTree == null)
            {
                hit.Success = false;
                return hit;
            }

            // find closest triangle
            hit.TriangleIndex = SurfMeshTree.FindNearestTriangle(p);
            g3.Vector3d ad = new Vector3d(), bd = new Vector3d(), cd = new Vector3d();
            SurfMesh.GetTriVertices(hit.TriangleIndex, ref ad, ref bd, ref cd);

            // get closest point and distance to it
            var distpt3tri3 = new DistPoint3Triangle3(p, new Triangle3d(ad, bd, cd)).Compute();
            hit.Point = (Vector3)distpt3tri3.TriangleClosest;
            hit.Distance = (float)distpt3tri3.Get();

            hit.Normal = (Vector3)SurfMesh.GetTriNormalLeftHanded(hit.TriangleIndex);
            hit.BarycentricCoordinate = (Vector3)distpt3tri3.TriangleBaryCoords;

            hit.Success = true;

            return hit;
        }


        // Raycasting
        private bool _Raycast(Ray ray, out HitInfo hitInfo, float maxDist = Mathf.Infinity)
        {
            // assign current dataframe to hitinfo using default constructor
            hitInfo = new HitInfo(CurrentDataFrame, Target.TargetTransform);

            if (!IsReady)
                return false;

            Ray3d ray3d;

            var transform = Target.TargetTransform;

            ray3d = new Ray3d(
                transform.InverseTransformPoint(ray.origin),
                transform.InverseTransformDirection(ray.direction));

            // find the first triangle intersected by `ray3d`
            var tIdx = SurfMeshTree.FindNearestHitTriangle(ray3d, maxDist);

            // no intersections
            if (tIdx == DMesh3.InvalidID)
                return false;

            Vector3d v0 = Vector3d.Zero, v1 = Vector3d.Zero, v2 = Vector3d.Zero;
            SurfMesh.GetTriVertices(tIdx, ref v0, ref v1, ref v2);

            // find actual ray-triangle intersection point
            var triangle = new Triangle3d(v0, v1, v2);
            var intr = new IntrRay3Triangle3(ray3d, triangle).Compute();
            var point = ray3d.PointAt(intr.RayParameter);

            hitInfo.Init(
                point,
                SurfMesh.GetTriNormalLeftHanded(tIdx),
                tIdx,
                (point - ray3d.Origin).Length,
                intr.TriangleBaryCoords);

            return true;
        }

        // Raycasting
        private bool Raycast(Ray ray, out HitInfo hitInfo, float maxDist = Mathf.Infinity)
        {
            // assign current dataframe to hitinfo using default constructor
            hitInfo = new HitInfo(CurrentDataFrame, Target.TargetTransform);

            if (!IsReady)
                return false;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, maxDist, 1))
            {
                 hitInfo.Success = true;
                hitInfo.Point = Target.transform.InverseTransformPoint(hit.point);
                hitInfo.Normal = hit.normal;
                hitInfo.Distance = hit.distance;
                return true;
            }

            return false;
        }

        // UI updates: Show/hide appropriate UI for draw/erase interactions
        public void TogglePenUI(InteractionMode newMode)
        {
            if (PenObject is null)
                return;

            PenObject.ToggleUI(newMode);
        }
    }
}
