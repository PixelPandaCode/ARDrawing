using UnityEngine;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using MixedReality.Toolkit.Subsystems;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Diagnostics;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine.XR.ARFoundation;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEditor;
using TMPro;
using System;

namespace StrokeMimicry
{
    // InputManager processes all low-level input events and passes on high level information to the projection and UI scripts.
    // All the public members are considered to be the API and should not be modified.
    public class InputManager : MonoBehaviour
    {
        // Action button is used for drawing/erasing
        public bool ActionButtonPressed = false;
        public bool ActionButtonJustPressed = false;
        public bool ActionButtonJustReleased = false;
        public GameObject cursorPointerPrefab;
        public Vectorization vectorization;

        public bool DevicesReady { get; private set; } = false;
        public Pen pen;
        public Projection projection;
        public List<GameObject> testReferences;
        public List<GameObject> testTargets;
        public bool haveCursorAttached = false;
        public GameObject PanelUI;
        public List<CursorPointer> cursorPointers = new List<CursorPointer>();
        public int currentTaskIndex = 1;
        public MRTKRayInteractor interactor;
        public MeshRenderer penRenderer;

        private HandsAggregatorSubsystem handsAggregatorSubsystem;
        private InputAction vectorAction;
        private InputAction toggleAction;
        private InputAction upperLayerAction;
        private InputAction lowerLayerAction;
        private InputAction eraseAction;
        private InputAction penAction;
        private InputAction exportAction;
        private TexturePaint tp;
        private List<CursorData> cursorData = new List<CursorData>();
        // This is only for drawing mode
        private List<Vector4> cursorVecList = new List<Vector4>(1);
        private Vector3 fixedDirection = new Vector3(-1, 2, 1).normalized * Screen.width * 0.5f;
        private HandJointPose handJointPose = new HandJointPose();
        private HandJointPose palmJointPose = new HandJointPose();
        private bool isUIEnabled = false;
        public TextMeshProUGUI textMesh;

        public void Awake()
        {
            projection.PenObject = pen;
            handsAggregatorSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
            if (handsAggregatorSubsystem == null)
            {
                UnityEngine.Debug.LogError("No hand system");
            }
            
            vectorAction = new InputAction(binding: "<Keyboard>/v");
            vectorAction.performed += _ => VectorAction();
            vectorAction.Enable();
            upperLayerAction = new InputAction(binding: "<Keyboard>/u");
            upperLayerAction.performed += _ => UpperLayerAction();
            upperLayerAction.Enable();
            lowerLayerAction = new InputAction(binding: "<Keyboard>/l");
            lowerLayerAction.performed += _ => LowerLayerAction();
            lowerLayerAction.Enable();
            eraseAction = new InputAction(binding: "<Keyboard>/r");
            eraseAction.performed += _ => EraseAction();
            eraseAction.Enable();
            penAction = new InputAction(binding: "<Keyboard>/p");
            penAction.performed += _ => PenAction();
            penAction.Enable();
            toggleAction = new InputAction(binding: "<Keyboard>/t");
            toggleAction.performed += _ => ToggleAction();
            toggleAction.Enable();
            exportAction = new InputAction(binding: "<Keyboard>/e");
            exportAction.performed += _ => ExportAction();
            exportAction.Enable();

            tp = GetComponent<TexturePaint>();

            //UnityEngine.Debug.Log(Application.persistentDataPath);
            //UnityEngine.Debug.Log(Application.dataPath);
            //UnityEngine.Debug.Log(Application.streamingAssetsPath);

#if UNITY_EDITOR
            fixedDirection = new Vector3(-1, 2, 1).normalized * Screen.width * 0.2f;
#endif
        }

        public void Start()
        {
            if (testTargets[0].activeSelf == false)
            {
                testTargets[0].SetActive(true);
            }
            if (testReferences[0].activeSelf == false)
            {
                testReferences[0].SetActive(true);
            }
        }

        public void Update()
        {
            bool rightHandIsValid = handsAggregatorSubsystem.TryGetPinchProgress(XRNode.RightHand, out bool rightIsReadyToPinch, out bool rightIsPinching, out float rightPinchAmount);
            if (rightHandIsValid)
            {
                bool rightHandPinch = rightHandIsValid && rightIsPinching && rightPinchAmount >= 1.0f;

                handsAggregatorSubsystem.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out handJointPose);
                handsAggregatorSubsystem.TryGetJoint(TrackedHandJoint.Palm, XRNode.RightHand, out palmJointPose);
                ActionButtonHandler(rightHandPinch);
            }

            if (projection.Target != null)
            {
                pen.PenTipPosition = interactor.transform.position;
                pen.SprayDirection = interactor.transform.forward;

                //pen.PenTipPosition = palmJointPose.Position;
                //Vector3 pointOnScreen = Camera.main.WorldToScreenPoint(palmJointPose.Position);
                //pointOnScreen += fixedDirection;
                //Vector3 endPointInWorld = Camera.main.ScreenToWorldPoint(pointOnScreen);
                //pen.SprayDirection = (endPointInWorld - palmJointPose.Position).normalized;

                //Vector3 cursorPosition;
                //Vector3 cursorNormal;
                //Vector3 rayDirection;
                //Vector3 cursorOrigin;
                //if (TryGetCursorHoverInfo(projection.Target, out cursorPosition, out cursorNormal, out rayDirection, out cursorOrigin))
                //{
                //    pen.PenTipPosition = cursorPosition;
                //    pen.SprayDirection = cursorNormal;
                //}
            }
            if (!interactor.isOnUI)
            {
                Draw();
            }

            if (textMesh)
            {
                string display = "Tools\nDrawMode: ";
                if (StrokeMimicryManager.Instance.CurrentInteractionMode == InteractionMode.Drawing)
                {
                    display += "Drawing";
                }
                else
                {
                    display += "Tapline";
                }
                display += "\n";

                display += "Brush Size: " + Math.Round(tp.BrushSize * 100.0f).ToString();
                display += "  ";

                display += "Color: ";
                if (tp.BrushColor == Color.red)
                {
                    display += "red";
                }
                else if (tp.BrushColor == Color.black)
                {
                    display += "black";
                }
                else if (tp.BrushColor == Color.blue)
                {
                    display += "blue";
                }

                textMesh.text = display;
            }
        }


        public void ToggleUIAction()
        {
            if (PanelUI != null)
            {
                PanelUI.SetActive(!PanelUI.activeSelf);
            }
        }

        public bool TryGetCursorHoverInfo(MRTKBaseInteractable baseInteractable, out Vector3 cursorPosition, out Vector3 cursorNormal, out Vector3 rayDirection, out Vector3 cursorOrigin)
        {
            cursorPosition = Vector3.zero;
            cursorNormal = Vector3.zero;
            rayDirection = Vector3.zero;
            cursorOrigin = Vector3.zero;
            if (!baseInteractable.isHovered)
            {
                return false;
            }
            if (baseInteractable.HoveringRayInteractors.Count > 0)
            {
                //ray interactor hovering -> use as default
                XRRayInteractor xrRayInteractor = (XRRayInteractor)baseInteractable.HoveringRayInteractors[0];
                Vector3 position, normal;
                int positionInLine;
                bool isValidTarget;
                if (xrRayInteractor.TryGetHitInfo(out position, out normal, out positionInLine, out isValidTarget))
                {
                    cursorPosition = position;
                    cursorNormal = normal;
                    xrRayInteractor.GetLineOriginAndDirection(out cursorOrigin, out rayDirection);
                    return true;
                }
            }
            return false;
        }


        public void ExportAction()
        {
            string outputRoot = Application.persistentDataPath;
#if UNITY_EDITOR
            outputRoot = Application.dataPath;
#endif
            string outputDir = outputRoot + "/Output/Task" + currentTaskIndex.ToString() + "_" + DateTime.Now.ToString("MMddHHmm");
            // Ensure the destination directory exists, create if it doesn't
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            WriteCursorData(outputDir + "/CursorData_Before.csv");
            GenerateImageAction(outputDir + "/Texture_Before.jpg");


            VectorAction();
            WriteCursorData(outputDir + "/CursorData.csv");
            GenerateImageAction(outputDir + "/Texture.jpg");
        }

        public void WriteCursorData(string filePath)
        {
            // Using StreamWriter to write to a file
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Index,PointerPosX,PointerPosY,PointerPosZ");  // Writing the header

                // Loop through each cursor pointer and write its position to the file
                for (int i = 0; i < cursorData.Count; ++i)
                {
                    UnityEngine.Vector3 pos = cursorData[i].pointerPos;  // Get the position
                    pos = projection.Target.transform.TransformPoint(pos);
                    writer.WriteLine($"{i},{pos.x},{pos.y},{pos.z}");  // Write the index and position to the file
                }
            }
        }

        public void NextTaskAction()
        {
            string outputRoot = Application.persistentDataPath;
#if UNITY_EDITOR
            outputRoot = Application.dataPath;
#endif
            ExportAction();
            ResetAction();
            // Check if the GameObject was found
            ToggleReferenceByID(currentTaskIndex);
            if (currentTaskIndex < testReferences.Count)
            {
                currentTaskIndex += 1;
            }
            ToggleReferenceByID(currentTaskIndex);

            ToggleAction();
        }

        public bool ToggleReferenceByID(int index)
        {
            // Find the GameObject by name
            if (index > testReferences.Count)
            {
                return false;
            }
            GameObject obj = testReferences[index-1];

            if (obj != null)
            {
                obj.SetActive(!obj.activeSelf);
                return true;
            }
            return false;
        }

        public bool ToggleTargetByID(int index)
        {
            // Find the GameObject by name
            if (index > testTargets.Count)
            {
                return false;
            }
            GameObject obj = testTargets[index - 1];

            if (obj != null)
            {
                obj.SetActive(!obj.activeSelf);
                return true;
            }
            return false;
        }

        public bool MoveFileToAnotherDir(string filePath, string newFolderPath)
        {
            if (File.Exists(filePath))
            {
                // Ensure the destination directory exists, create if it doesn't
                if (!Directory.Exists(newFolderPath))
                {
                    Directory.CreateDirectory(newFolderPath);
                }

                // Define the new file path
                string fileName = Path.GetFileName(filePath);
                string newFilePath = Path.Combine(newFolderPath, fileName);

                if (File.Exists(newFilePath))
                {
                    File.Delete(newFilePath);
                }

                // Move the file to the new directory
                File.Move(filePath, newFilePath);
            }

            return false;
        }

        public void ToggleAction()
        {
            for (int i = 0; i < cursorPointers.Count; ++i)
            {
                Destroy(cursorPointers[i]);
            }
            if (StrokeMimicryManager.Instance.CurrentInteractionMode == InteractionMode.Drawing)
            {
                ResetAction();
                StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Tapline;
                penRenderer.material.SetColor("_Base_Color_", Color.green);
            }
            else
            {
                ResetAction();
                StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Drawing;
                penRenderer.material.SetColor("_Base_Color_", Color.white);
            }

            // projection.TogglePenUI(StrokeMimicryManager.Instance.CurrentInteractionMode);
        }

        public void ResetAction()
        {
            for (int i = 0; i < cursorPointers.Count; ++i)
            {
                Destroy(cursorPointers[i].gameObject);
            }
            cursorPointers.Clear();
            cursorData.Clear();
            cursorVecList.Clear();
            tp.albedo.ClearCurLayer();
            tp.albedo.SetCursorData(cursorPointers);
        }


        public void ActionButtonHandler(bool newState)
        {
            ActionButtonJustPressed = newState && !ActionButtonPressed;
            ActionButtonJustReleased = !newState && ActionButtonPressed;
            ActionButtonPressed = newState;
        }


        // Called once per frame when interaction mode is drawing
        public void Draw()
        {
            projection.InitFrame();
            bool isTapline = StrokeMimicryManager.Instance.CurrentInteractionMode == StrokeMimicry.InteractionMode.Tapline;
            bool isDrawing = StrokeMimicryManager.Instance.CurrentInteractionMode == StrokeMimicry.InteractionMode.Drawing;
            if (ActionButtonJustPressed)
            {
                cursorVecList.Clear();
                ActionButtonJustPressed = false;
                if (!haveCursorAttached && isTapline)
                {
                    projection.Project();
                    cursorPointers.Add(CreateCursorPointer());
                    tp.albedo.SetCursorData(cursorPointers);
                }
            }
            else if (ActionButtonPressed)
            {
                projection.Project();
                if (isDrawing)
                {
                    UpdateDrawingCursor();
                }
                else if (haveCursorAttached && isTapline)
                {
                    //curCursor.cursorData.pointerPos = Projection.CurrentCursor;
                    //curCursor.transform.position = Projection.CurrentCursor;
                    for (int i = 0; i < cursorPointers.Count; ++i)
                    {
                        if (cursorPointers[i].isAttached)
                        {
                            cursorPointers[i].cursorData.pointerPos = projection.CurrentCursor;
                            cursorPointers[i].transform.position = projection.CurrentCursor;
                        }
                    }
                    tp.albedo.ClearCurLayer();
                    tp.albedo.SetCursorData(cursorPointers);
                }
            }
            else if (ActionButtonJustReleased)
            {
                cursorVecList.Clear();
                projection.CurrentCursor = Vector4.positiveInfinity;
                ActionButtonJustReleased = false;
            }

            if (haveCursorAttached)
            {
                pen.ShowProjectionPointer = false;
            }
            else
            {
                pen.ShowProjectionPointer = true;
            }
            projection.UpdateProjectionPointerAndLaser();
        }

        public void UpdateDrawingCursor()
        {
            if (projection.CurrentCursor.w == 0)
            {
                cursorVecList.Clear();
                return;
            }
            cursorData.Add(CreateCursorData());
            if (cursorVecList.Count == 0)
            {
                cursorVecList.Add(projection.CurrentCursor);
            }
            else
            {
                if (Vector4.Distance(projection.CurrentCursor, cursorVecList[cursorVecList.Count - 1]) > tp.BrushSize)
                {
                    cursorVecList.Add(projection.CurrentCursor);
                }
                else
                {
                    cursorVecList.Clear();
                    cursorVecList.Add(projection.CurrentCursor);
                }

            }
            //Vector3 v = projection.CurrentCursor;
            //UnityEngine.Debug.Log((v - pen.PenTipPosition).normalized);
            tp.albedo.SetCursorData(cursorVecList);
        }

        public CursorPointer CreateCursorPointer(CursorData data)
        {
            CursorPointer pointer;
            pointer = GameObject.Instantiate(cursorPointerPrefab, projection.Target.transform).GetComponent<CursorPointer>();
            pointer.transform.position = data.pointerPos;
            pointer.cursorData.pointerPos = data.pointerPos;
            pointer.cursorData.BrushColor = data.BrushColor;
            pointer.cursorData.BrushHardness = data.BrushHardness;
            pointer.cursorData.BrushSize = data.BrushSize;
            pointer.inputManager = this;
            return pointer;
        }

        public CursorPointer CreateCursorPointer()
        {
            CursorPointer pointer;
            pointer = GameObject.Instantiate(cursorPointerPrefab, projection.Target.transform).GetComponent<CursorPointer>();
            pointer.transform.position = projection.CurrentCursor;
            pointer.cursorData.pointerPos = projection.CurrentCursor;
            pointer.cursorData.BrushColor = tp.BrushColor;
            pointer.cursorData.BrushHardness = tp.BrushHardness;
            pointer.cursorData.BrushSize = tp.BrushSize;
            pointer.inputManager = this;
            return pointer;
        }

        public CursorData CreateCursorData()
        {
            CursorData cursorData;
            cursorData = new CursorData();
            if (cursorVecList.Count == 0)
            {
                Vector4 newCursor = projection.CurrentCursor;
                newCursor.w = 0.5f;
                cursorData.pointerPos = newCursor;
            }
            else
            {
                cursorData.pointerPos = projection.CurrentCursor;
            }
            cursorData.BrushColor = tp.BrushColor;
            cursorData.BrushHardness = tp.BrushHardness;
            cursorData.BrushSize = tp.BrushSize;
            return cursorData;
        }

        public void VectorAction()
        {
            bool isTapline = StrokeMimicryManager.Instance.CurrentInteractionMode == StrokeMimicry.InteractionMode.Tapline;
            bool isDrawing = StrokeMimicryManager.Instance.CurrentInteractionMode == StrokeMimicry.InteractionMode.Drawing;
            if (isTapline)
            {
                UnityEngine.Debug.Log("Tapline don't need to vectorize");
                return;
            }
            tp.albedo.ClearCurLayer();
            vectorization.Process(ref cursorData);
            for(int i = 0; i < cursorPointers.Count; ++i)
            {
                Destroy(cursorPointers[i]);
            }
            cursorPointers.Clear();
            for (int i = 0; i < cursorData.Count; ++i)
            {
                cursorPointers.Add(CreateCursorPointer(cursorData[i]));
            }
            tp.albedo.SetCursorData(cursorPointers);

            StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Tapline;
            penRenderer.material.SetColor("_Base_Color_", Color.green);
        }

        public void GenerateImageAction(string filePath)
        {
            if (tp == null)
            {
                return;
            }
            projection.CurrentCursor = Vector4.zero;
            tp.albedo.CaptureToJPG(filePath);
        }

        public void UpperLayerAction()
        {
            if (tp == null)
            {
                return;
            }
            projection.CurrentCursor = Vector4.zero;
            tp.UpperLayerAction();
        }

        public void LowerLayerAction()
        {
            if (tp == null)
            {
                return;
            }
            projection.CurrentCursor = Vector4.zero;
            tp.LowerLayerAction();
        }

        public void PenAction()
        {
            tp.SetBrushColor(Color.red);
        }

        public void BiggerBrushAction()
        {
            cursorVecList.Clear();
            tp.albedo.SetCursorData(cursorVecList);
            tp.SetBrushSize(tp.BrushSize + 0.01f);
        }

        public void SmallerBrushAction()
        {
            if (tp.BrushSize > 0.01f)
            {
                tp.SetBrushSize(tp.BrushSize - 0.01f);
            }
        }

        public void EraseAction()
        {
            //Projection.CurrentCursor = Vector4.positiveInfinity;
            //tp.SetBrushColor(Color.white);

            for (int i = 0; i < cursorPointers.Count; ++i)
            {
                Destroy(cursorPointers[i].gameObject);
            }
            cursorPointers.Clear();
            cursorData.Clear();
            tp.albedo.ClearCurLayer();
            tp.albedo.SetCursorData(cursorPointers);
        }

        public void ChangeColorAction()
        {
            cursorVecList.Clear();
            tp.albedo.SetCursorData(cursorVecList);
            if (tp.BrushColor == Color.red)
            {
                tp.SetBrushColor(Color.black);
            }
            else if (tp.BrushColor == Color.black)
            {
                tp.SetBrushColor(Color.blue);
            }
            else if (tp.BrushColor == Color.blue)
            {
                tp.BrushColor = Color.red;
            }
        }
    }
}
