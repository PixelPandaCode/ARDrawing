using StrokeMimicry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CursorData
{
    public Vector4 pointerPos;
    public Color BrushColor;
    public float BrushSize;
    public float BrushHardness;
}

public class CursorPointer : MonoBehaviour
{
    public CursorData cursorData;
    public Renderer currentRenderer;
    public InputManager inputManager;
    public bool isAttached = false;

    // Start is called before the first frame update
    void Start()
    {
        currentRenderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "PenPointer")
        {
            currentRenderer.material.color = Color.white;
            inputManager.haveCursorAttached = true;
            isAttached = true;
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject.tag == "PenPointer")
        {
            currentRenderer.material.color = new Color(0, 154.0f/255.0f, 1.0f);
            isAttached = false;
            for (int i = 0; i < inputManager.cursorPointers.Count; ++i)
            {
                if (inputManager.cursorPointers[i].isAttached)
                {
                    return;
                }
            }
            inputManager.haveCursorAttached = false;
        }
    }
}
