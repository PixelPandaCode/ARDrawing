using StrokeMimicry;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Rendering;

public class TexturePaint : MonoBehaviour {

    // ======================================================================================================================
    // PARAMETERS -----------------------------------------------------------------------------------------------
    public  Texture          baseTexture;                  // used to deterimne the dimensions of the runtime texture
    public  GameObject       meshGameobject;
    public  Shader           UVShader;                     // the shader usedto draw in the texture of the mesh
    public  Shader           ilsandMarkerShader;
    public  Shader           fixIlsandEdgesShader;
    public Shader standardShader;
    public Shader ConditionalBlendShader;

    // --------------------------------
  
    private Camera           mainC;
    public RenderTexture    markedIslands;
    private CommandBuffer    cb_markingIslands;
    private int              numberOfFrames;
    public Material         fixEdgesMaterial;
    //private Mesh meshToDraw;
    //private Material meshMaterial;                 // used to bind the runtime texture as the albedo of the mesh
    private List<Mesh> meshesToDraw;
    private List<Material> materialsToDraw;
    public List<Vector4> cursorData;

    public Color BrushColor;
    public float BrushSize;
    public float BrushHardness;

    // ---------------------------------
    public PaintableTexture albedo;

    // ======================================================================================================================
    // INITIALIZE -------------------------------------------------------------------

    void Start() {

        Init();
    }

    public void Init()
    {

        // Main cam initialization ---------------------------------------------------
        mainC = Camera.main;
        if (mainC == null) mainC = this.GetComponent<Camera>();
        if (mainC == null) mainC = GameObject.FindObjectOfType<Camera>();
        mainC.RemoveAllCommandBuffers();
        numberOfFrames = 0;
        meshGameobject = FindObjectOfType<StrokeMimicryTarget>().gameObject;
        baseTexture = meshGameobject.GetComponent<StrokeMimicryTarget>().baseTexture;

        MeshRenderer[] meshRenderers = meshGameobject.GetComponentsInChildren<MeshRenderer>();
        materialsToDraw = new List<Material>();
        for (int i = 0; i < meshRenderers.Length; ++i)
        {
            // Get materials from each MeshRenderer and add them to the list
            materialsToDraw.Add(meshRenderers[i].sharedMaterial);
        }
        meshesToDraw = new List<Mesh>();
        MeshFilter[] meshFilters = meshGameobject.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < meshFilters.Length; ++i)
        {
            meshesToDraw.Add(meshFilters[i].sharedMesh);
        }


        //meshMaterial = meshGameobject.GetComponent<MeshRenderer>().material;
        //meshToDraw = meshGameobject.GetComponent<MeshFilter>().sharedMesh;
        // Main Camera depth Texture mode: very important!!!
        mainC.depthTextureMode = DepthTextureMode.Depth;

        // Texture and Mat initalization ---------------------------------------------
        markedIslands = new RenderTexture(baseTexture.width, baseTexture.height, 0, RenderTextureFormat.R8);

        // Command buffer inialzation ------------------------------------------------

        cb_markingIslands = new CommandBuffer();
        cb_markingIslands.name = "markingIlsnads";

        cb_markingIslands.SetRenderTarget(markedIslands);
        Material mIlsandMarker = new Material(ilsandMarkerShader);
        //cb_markingIslands.DrawMesh(meshToDraw, Matrix4x4.identity, mIlsandMarker);
        for (int i = 0; i < meshesToDraw.Count; ++i)
        {
            cb_markingIslands.DrawMesh(meshesToDraw[i], Matrix4x4.identity, mIlsandMarker);
        }
        mainC.AddCommandBuffer(CameraEvent.AfterDepthTexture, cb_markingIslands);

        albedo = new PaintableTexture(Color.white, baseTexture.width, baseTexture.height, "_MainTex", this);
        albedo.AddCommandBuffer();

        for (int i = 0; i < materialsToDraw.Count; ++i)
        {
            materialsToDraw[i].SetTexture(albedo.id, albedo.runTimeTexture);
        }

        Shader.SetGlobalColor("_BrushColor", BrushColor);
        Shader.SetGlobalFloat("_BrushOpacity", 1.0f);
        Shader.SetGlobalFloat("_BrushSize", BrushSize);
        Shader.SetGlobalFloat("_BrushHardness", BrushHardness);
        Shader.SetGlobalVector("_Cursor", Vector3.positiveInfinity);
    }

    public void Clear()
    {
        mainC.RemoveAllCommandBuffers();
        albedo.RemoveCommandBuffer();
    }
    // ======================================================================================================================
    // LOOP ---------------------------------------------------------------------------

    private void Update()
    {
        if (numberOfFrames > 2) mainC.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, cb_markingIslands);

        numberOfFrames++;


        // ----------------------------------------------------------------------------
        // This MUST be called to set up the painting with the mouse.
        albedo.UpdateMatrixParam(meshGameobject.transform.localToWorldMatrix);

        //CommandBuffer[] commandBuffers = mainC.GetCommandBuffers(CameraEvent.AfterDepthTexture);
        //for(int i = 0; i < commandBuffers.Length; ++i)
        //{
        //    Debug.Log(commandBuffers[i].name);
        //}
    }

    public void SetBrushColor(Color newColor)
    {
        BrushColor = newColor;
        Shader.SetGlobalColor("_BrushColor", newColor);
    }

    public void SetBrushSize(float newSize)
    {
        BrushSize = newSize;
        Shader.SetGlobalFloat("_BrushSize", BrushSize);
    }

    // ======================================================================================================================
    // HELPER FUNCTIONS ---------------------------------------------------------------------------

    public void RenderAlbedoBySprite(Sprite sprite)
    {
        albedo.RemoveCommandBuffer();
        RenderTexture.active = albedo.currentTex;
        var mat = new Material(Shader.Find("Unlit/Vector"));
        Texture tex = VectorUtils.RenderSpriteToTexture2D(sprite, albedo.currentTex.width, albedo.currentTex.height, mat);
        Graphics.Blit(tex, albedo.currentTex, mat);
        RenderTexture.active = null;
        albedo.InitPaintCommandBuffer();
        albedo.AddCommandBuffer();
    }

    public List<Mesh> GetMeshesToDraw()
    {
        return meshesToDraw;
    }

    public List<Material> GetMaterialsToDraw() { return materialsToDraw; }

    public RenderTexture GetMarkedIslands()
    {
        return markedIslands;
    }

    public void UpperLayerAction()
    {
        albedo.RemoveCommandBuffer();
        albedo.SetActiveLayer(2);
        albedo.InitPaintCommandBuffer();
        albedo.AddCommandBuffer();
    }

    public void LowerLayerAction()
    {
        albedo.RemoveCommandBuffer();
        albedo.SetActiveLayer(1);
        albedo.InitPaintCommandBuffer();
        albedo.AddCommandBuffer();
    }
}


[System.Serializable]
public class PaintableTexture
{
    public  string        id;
    public  RenderTexture runTimeTexture;
    public RenderTexture paintedTexture;

    public  CommandBuffer cb;
    public CommandBuffer cb_combine;
    public int width, height;
    public Color clearColor;

    public Material      mPaintInUV;
    public Material      mFixedEdges;
    public Material mConditionalBlend;
    public Material mDrawMesh;
    public RenderTexture fixedIlsands;
    public RenderTexture tmpTex;
    public Texture2D cursorDataTex;

    public List<RenderTexture> layers;
    public int currentLayer = 0;
    private int layerCount = 1;
    public RenderTexture currentTex;
    private TexturePaint tpManager;

    public PaintableTexture(Color clearColor, int width, int height, string id, TexturePaint manager)
    {
        this.id        = id;
        this.width = width;
        this.height = height;
        this.clearColor = clearColor;
        this.tpManager = manager;

        InitializeLayers(layerCount);
        currentLayer = 1;
        currentTex = layers[currentLayer];
        InitCommandBuffer();
    }

    public RenderTexture CreateRenderTexture()
    {
        RenderTexture newTexture = new RenderTexture(width, height, 0)
        {
            anisoLevel = 2,
            useMipMap = false,
            filterMode = FilterMode.Bilinear
        };
        Graphics.SetRenderTarget(newTexture);
        GL.Clear(false, true, clearColor);
        return newTexture;
    }

    public void UpdateUVParam(string paramName, Vector4 param)
    {
        mPaintInUV.SetVector(paramName, param);
    }

    public void SetCursorData(List<Vector4> cursorData)
    {
        Vector3 SprayDirection = tpManager.meshGameobject.transform.rotation * Vector3.forward;
        //SprayDirection = (tpManager.transform.position - Camera.main.transform.position).normalized;
        mPaintInUV.SetInt("_CursorCount", cursorData.Count);

        // Apply the changes to the texture
        cursorDataTex.SetPixels(GetColorList(cursorData));
        cursorDataTex.Apply();
        mPaintInUV.SetTexture("_CursorDataTex", cursorDataTex);
        mPaintInUV.SetVector("_SprayDirection", SprayDirection);
    }

    public void SetCursorData(List<CursorPointer> cursorData)
    {
        List<Vector4> cursorPoints = new List<Vector4>();
        for (int i = 0; i < cursorData.Count; ++i)
        {
            cursorPoints.Add(cursorData[i].cursorData.pointerPos);
        }
        SetCursorData(cursorPoints);
    }

    private Color[] GetColorList(List<Vector4> cursorData)
    {
        Color[] colorList = new Color[512 * 512];
        // Clear existing data to avoid artifacts
        for (int i = 0; i < 512 * 512; i++)
        {
            colorList[i] = new Color(0, 0, 0, 0);
        }

        // Encode cursor data into the texture
        for (int i = 0; i < cursorData.Count; i++)
        {
            if (i >= 512 * 512) break; // Prevent exceeding texture capacity

            int x = i % 512;
            int y = i / 512;
            Vector4 cursor = cursorData[i];

            // Store cursor data as a color, each component in a channel
            colorList[y * 512 + x] = new Color(cursor.x, cursor.y, cursor.z, cursor.w);
        }
        return colorList;
    }

    public void AddCommandBuffer()
    {
        Camera.main.AddCommandBuffer(CameraEvent.AfterDepthTexture, cb);
        Camera.main.AddCommandBuffer(CameraEvent.AfterDepthTexture, cb_combine);
    }

    public void RemoveCommandBuffer()
    {
        Camera.main.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, cb);
        Camera.main.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, cb_combine);
    }

    public void UpdateMatrixParam(Matrix4x4 localToWorld)
    {
        mPaintInUV.SetMatrix("mesh_Object2World", localToWorld); // Mus be updated every time the mesh moves, and also at start
        mDrawMesh.SetMatrix("mesh_Object2World", localToWorld); 
    }

    public void CaptureToJPG(string filePath)
    {
        // Create a new Texture2D with the same dimensions as the render texture
        Texture2D texture = new Texture2D(runTimeTexture.width, runTimeTexture.height, TextureFormat.RGB565, false);

        // Set the active RenderTexture to the one you want to read from
        RenderTexture.active = runTimeTexture;

        // Read the pixels from the RenderTexture and apply them to the Texture2D
        texture.ReadPixels(new Rect(0, 0, runTimeTexture.width, runTimeTexture.height), 0, 0);
        texture.Apply();

        // Encode the texture to JPG format
        byte[] bytes = texture.EncodeToJPG();

        // Write to a file in the project folder
        File.WriteAllBytes(filePath, bytes);

        // Clean up
        RenderTexture.active = null;
        Object.DestroyImmediate(texture);
    }

    public void ClearCurLayer()
    {
        Graphics.SetRenderTarget(currentTex);
        GL.Clear(false, true, clearColor);
    }

    // ======================================================================================================================
    // LAYER FUNCTIONS ---------------------------------------------------------------------------
    private void InitializeLayers(int layerCount)
    {
        layers = new List<RenderTexture>(layerCount);
        // 0 is for base texture
        RenderTexture baseLayer = CreateRenderTexture();
        if (tpManager.baseTexture != null)
        {
            Material mPaintBase = new Material(tpManager.UVShader);
            mPaintBase.SetTexture("_MainTex", tpManager.baseTexture);
            Graphics.Blit(tpManager.baseTexture, baseLayer, mPaintBase);
        }
        layers.Add(baseLayer);
        for (int i = 1; i <= layerCount; i++)
        {
            RenderTexture newLayer = CreateRenderTexture();
            layers.Add(newLayer);
        }
    }

    public void SetActiveLayer(int index)
    {
        if (index == currentLayer)
        {
            return;
        }
        if (index >= 1 && index < layers.Count)
        {
            currentLayer = index;
            currentTex = layers[index];
            InitPaintCommandBuffer();
        }
    }


    private void InitMaterial()
    {
        mPaintInUV = new Material(tpManager.UVShader);
        if (!mPaintInUV.SetPass(0))
            Debug.LogError("Invalid Shader Pass: ");

        mFixedEdges = new Material(tpManager.fixIlsandEdgesShader);
        mFixedEdges.SetTexture("_IlsandMap", tpManager.markedIslands);
        mFixedEdges.SetTexture("_MainTex", paintedTexture);

        mDrawMesh = new Material(tpManager.UVShader);
        if (!mDrawMesh.SetPass(0))
            Debug.LogError("Invalid Shader Pass: ");
        mDrawMesh.SetTexture("_MainTex", paintedTexture);
    }

    private void InitTextures()
    {
        runTimeTexture = CreateRenderTexture();
        paintedTexture = CreateRenderTexture();
        fixedIlsands = CreateRenderTexture();
        cursorDataTex = new Texture2D(512, 512, TextureFormat.RGBAFloat, false);
        cursorDataTex.filterMode = FilterMode.Point; // Prevent interpolation of texture data
        cursorDataTex.wrapMode = TextureWrapMode.Clamp; // Avoid wrapping
    }

    public void InitPaintCommandBuffer()
    {
        if (cb == null)
        {
            cb = new CommandBuffer();
            cb.name = "TexturePainting" + id;
        } else
        {
            cb.Clear();
        }

        cb.SetRenderTarget(currentTex);
        cb.Blit(currentTex, fixedIlsands, mFixedEdges);
        cb.Blit(fixedIlsands, currentTex);
        tmpTex = CreateRenderTexture();
        cb.Blit(currentTex, tmpTex);
        mPaintInUV.SetTexture("_MainTex", tmpTex);
        cb.Blit(tmpTex, currentTex, mPaintInUV);
        for (int i = 0; i < tpManager.GetMeshesToDraw().Count; ++i)
        {
            cb.DrawMesh(tpManager.GetMeshesToDraw()[i], Matrix4x4.identity, mPaintInUV);
        }
    }

    private void InitCombineCommandBuffer()
    {
        if (cb_combine == null)
        {
            cb_combine = new CommandBuffer();
            cb_combine.name = "TexturePainting_Combine" + id;
        } else
        {
            cb_combine.Clear();
        }

        cb_combine.SetRenderTarget(runTimeTexture);
        cb_combine.ClearRenderTarget(false, true, clearColor);
        for (int i = 0; i < tpManager.GetMeshesToDraw().Count; ++i)
        {
            cb_combine.DrawMesh(tpManager.GetMeshesToDraw()[i], Matrix4x4.identity, mDrawMesh);
        }
        cb_combine.Blit(runTimeTexture, fixedIlsands, mFixedEdges);
        cb_combine.Blit(fixedIlsands, runTimeTexture);
        // merge different texes
        for (int i = 0; i <= layerCount; i++)
        {
            mConditionalBlend = new Material(tpManager.ConditionalBlendShader);
            tmpTex = CreateRenderTexture();
            mConditionalBlend.SetTexture("_ResourceTex", layers[i]);
            cb_combine.Blit(runTimeTexture, tmpTex);
            mConditionalBlend.SetTexture("_TargetTex", tmpTex);
            cb_combine.Blit(tmpTex, runTimeTexture, mConditionalBlend);
        }
        cb_combine.Blit(runTimeTexture, paintedTexture);
    }

    private void InitCommandBuffer()
    {
        InitTextures();
        InitMaterial();
        InitPaintCommandBuffer();
        InitCombineCommandBuffer();

        // old procedure: 1. draw mesh using texPaint 2. fixed Edge 3. Record runtime to painted
        //cb.SetRenderTarget(runTimeTexture);
        //cb.DrawMesh(tpManager.GetMeshToDraw(), Matrix4x4.identity, mPaintInUV);
        //cb.Blit(runTimeTexture, fixedIlsands, mFixedEdges);
        //cb.Blit(fixedIlsands, runTimeTexture);
        //cb.Blit(runTimeTexture, paintedTexture);
    }

}

