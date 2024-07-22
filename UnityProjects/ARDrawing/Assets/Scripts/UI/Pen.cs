using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StrokeMimicry
{
    // Pen is a UI class handling the display of the projection pointer, projection laser, and the eraser.
    public class Pen : MonoBehaviour
    {
        [Tooltip("Position of the pen tip in the local frame of the controller.")]
        public Vector3 PenTipPosition;

        [Tooltip("Spraypaint direction in the local frame of the controller.")]
        public Vector3 SprayDirection;

        public bool ShowProjectionPointer = true;
        
        private const float laserLength = 10f;
        private const float laserThickness = 0.001f;
        private const float pointerRelativeThickness = 5.0f;

        private bool _showProjectionLaser = true;
        public bool ShowProjectionLaser
        {
            get => _showProjectionLaser;
            set
            {
                ShowProjectionLaser = value;
                if (laserRenderer != null)
                    laserRenderer.enabled = value;
            }
        }

        public GameObject pointer;
        public GameObject laser;

        private MeshRenderer pointerRenderer = null;
        private MeshRenderer laserRenderer = null;

        private CustomButton currentButton = null;

        void Start()
        {
            // Create the pointer: a sphere that marks the current projected point on the target mesh
            float strokeWidth = StrokeMimicryManager.Instance.MeshThickness;
            pointerRenderer = pointer.GetComponent<MeshRenderer>();
            pointerRenderer.material = StrokeMimicryManager.Instance.PointerMaterial;
            pointerRenderer.enabled = false;
            pointer.transform.SetParent(transform, false);
            pointer.transform.localScale = new Vector3(
                pointerRelativeThickness * strokeWidth,
                pointerRelativeThickness * strokeWidth,
                pointerRelativeThickness * strokeWidth);
            Rigidbody pointerRigidbody = pointer.AddComponent<Rigidbody>();
            pointerRigidbody.isKinematic = false;
            pointerRigidbody.useGravity = false;
            pointer.tag = "PenPointer";
            //Destroy(pointer.GetComponent<MeshCollider>());

            // Create the laser: a thin cylinder that goes from the pen tip to the current projected point
            // If no projected point exists, the cylinder extends out to infinity in the spray direction
            laserRenderer = laser.GetComponent<MeshRenderer>();
            laserRenderer.material = StrokeMimicryManager.Instance.LaserMaterial;
            laser.transform.SetParent(transform, false);
            laser.transform.localScale = new Vector3(laserThickness, 0.5f*laserLength, laserThickness);
            Destroy(laser.GetComponent<MeshCollider>());
            // Add the marker script `StrokeMimicryUI` so that these objects can be deleted when needed
            pointer.AddComponent<StrokeMimicryUI>();
            laser.AddComponent<StrokeMimicryUI>();
        }

        public void UpdatePointerAndLaser(Ray ray, HitInfo hit, Transform targetTransform)
        {
            if (hit.Success)
            {
                // hit point in local coordinates of the controller, that is, of `this.gameObject`
                Vector3 hitPoint = targetTransform.TransformPoint(hit.Point);

                pointerRenderer.transform.position = hitPoint;
                pointerRenderer.transform.up = ray.direction;
                laserRenderer.transform.position = hitPoint;
                // this code will make game not run well, don't know why
                laserRenderer.transform.localScale = new Vector3(
                    laserThickness,
                    1f * (hitPoint - PenTipPosition).magnitude,
                    laserThickness);
                laserRenderer.transform.up = ray.direction;
            }
            else
            {
                pointerRenderer.enabled = false;
                laserRenderer.enabled = false;

                //laserRenderer.transform.localPosition = PenTipPosition + 0.5f * laserLength * SprayDirection;
                //// this code will make game not run well, don't know why
                //laserRenderer.transform.localScale = new Vector3(
                //    laserThickness,
                //    0.5f * laserLength,
                //    laserThickness);
                //laserRenderer.transform.up = transform.TransformDirection(SprayDirection);
            }
        }

        public bool UpdateUIPointerAndLaser(bool Press)
        {
            if (!ShowProjectionPointer)
            {
                return false;
            }
            Ray ray = new Ray(PenTipPosition, SprayDirection);
            RaycastHit hit;
            laserRenderer.enabled = ShowProjectionPointer;
            pointerRenderer.enabled = ShowProjectionPointer;
            if (Physics.Raycast(ray, out hit) && hit.collider.tag == "UI")
            {
                Vector3 hitPoint = hit.point;
                pointerRenderer.transform.position = hitPoint;
                pointerRenderer.transform.up = ray.direction;
                laserRenderer.transform.position = hitPoint;
                // this code will make game not run well, don't know why
                laserRenderer.transform.localScale = new Vector3(
                    laserThickness,
                    1f * (hitPoint - PenTipPosition).magnitude,
                    laserThickness);
                laserRenderer.transform.up = ray.direction;
                CustomButton button = hit.collider.GetComponent<CustomButton>();
                if (button != currentButton)
                {
                    if (currentButton != null)
                        currentButton.Leave();
                    currentButton = button;
                }
                if (button != null)
                {
                    if (Press)
                    {
                        button.Press();
                    }
                    else
                    {
                        button.Hover();
                    }
                }
                return true;
            }
            else
            {
                if (currentButton != null)
                    currentButton.Leave();
                return false;
            }
        }

        public void ToggleUI(InteractionMode newMode)
        {
            //if (newMode == InteractionMode.Drawing)
            //{
            //    if (ShowProjectionLaser)
            //        laserRenderer.enabled = true;

            //    eraserRenderer.enabled = false;
            //    eraserCollider.enabled = false;
            //}
            //else
            //{
            //    laserRenderer.enabled = false;
            //    pointerRenderer.enabled = false;

            //    eraserRenderer.enabled = true;
            //    eraserCollider.enabled = true;
            //}

            if (newMode == InteractionMode.Drawing)
            {
                if (ShowProjectionLaser)
                    pointerRenderer.material.color = Color.red;
            }
            else
            {
                if (ShowProjectionLaser)
                    pointerRenderer.material.color= Color.green;
            }
        }

        // Remove the objects created by this script
        public void OnDestroy()
        {
            var objs = gameObject.GetComponentsInChildren<StrokeMimicryUI>();

            foreach (var obj in objs)
                Destroy(obj.gameObject);
        }
    }

}
