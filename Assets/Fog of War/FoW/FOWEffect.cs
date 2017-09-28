using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
///     Fog of War system needs 3 components in order to work:
///     - Fog of War system that will create a height map of your scene and perform all the updates.
///     - Fog of War Image Effect on the camera that will be displaying the fog of war (this class).
///     - Fog of War Revealer on one or more game objects in the world.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[AddComponentMenu("Fog of War/Image Effect")]
public class FOWEffect : MonoBehaviour
{
    /// <summary>
    ///     Color tint given to unexplored pixels.
    /// </summary>
    public Color mUnexploredColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    public FOWSystem mFOWSystem;

    /// <summary>
    ///     Color tint given to explored (but not visible) pixels.
    /// </summary>
    public Color mExploredColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    public Vector3[] mCorners;
    public Vector3[] mVertexs;
    private FOWSystem mFog;
    public Camera mCam;
    private Material mMat;
    private Mesh mMesh;
    private Plane mPlane;
    private CommandBuffer mBuf;

    /// <summary>
    ///     The camera we're working with needs depth.
    /// </summary>
    void OnEnable()
    {
        mCorners = new Vector3[4];
        mVertexs = new Vector3[4];
        mPlane = new Plane(Vector3.up, Vector3.zero);
        mCorners[0] = new Vector3(0, 0, 0);
        mCorners[1] = new Vector3(mCam.pixelWidth, 0, 0);
        mCorners[2] = new Vector3(0, mCam.pixelHeight, 0);
        mCorners[3] = new Vector3(mCam.pixelWidth, mCam.pixelHeight, 0);
        mMesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        float halfSize = mFOWSystem.worldSize / 2.0f;
        vertices[0] = new Vector3(-halfSize, 0, -halfSize);
        vertices[1] = new Vector3(halfSize, 0, -halfSize);
        vertices[2] = new Vector3(-halfSize, 0, halfSize);
        vertices[3] = new Vector3(halfSize, 0, halfSize);
        mMesh.vertices = vertices;
        Vector2[] uvs = new Vector2[4];
        uvs[0] = Vector2.zero;
        uvs[1] = Vector2.right;
        uvs[2] = Vector2.up;
        uvs[3] = Vector2.one;
        mMesh.uv = uvs;
        int[] indices = {0, 2, 1, 1, 2, 3};
        mMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        if (Application.isPlaying)
            GetComponent<MeshRenderer>().enabled = true;
        else
            GetComponent<MeshRenderer>().enabled = false;
        GetComponent<MeshFilter>().sharedMesh = mMesh;
    }

    private void CreateVertexs()
    {
        for (int i = 0; i < mCorners.Length; i++)
        {
            Ray ray = mCam.ScreenPointToRay(mCorners[i]);
            float dis;
            mPlane.Raycast(ray, out dis);
            mVertexs[i] = ray.GetPoint(dis);
        }
    }

    private void Start()
    {
        mMat = GetComponent<MeshRenderer>().sharedMaterial;
    }

    private void OnDrawGizmos()
    {
        CreateVertexs();
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(mVertexs[0], mVertexs[1]);
        Gizmos.DrawLine(mVertexs[3], mVertexs[1]);
        Gizmos.DrawLine(mVertexs[2], mVertexs[3]);
        Gizmos.DrawLine(mVertexs[0], mVertexs[2]);
    }


    private void LateUpdate()
    {
        if (mFog == null)
        {
            mFog = FOWSystem.Instance;
            if (mFog == null) mFog = FindObjectOfType(typeof(FOWSystem)) as FOWSystem;
        }

        if (mFog == null || !mFog.enabled)
        {
            enabled = false;
            return;
        }
        float invScale = 1f / mFog.worldSize;
        Transform t = mFog.transform;
        float x = t.position.x - mFog.worldSize * 0.5f;
        float z = t.position.z - mFog.worldSize * 0.5f;
        Vector4 p = new Vector4(x * invScale, z * invScale, invScale, mFog.blendFactor);
        mMat.SetColor("_Unexplored", mUnexploredColor);
        mMat.SetColor("_Explored", mExploredColor);
        mMat.SetVector("_Params", p);
        mMat.SetTexture("_FogTex0", mFog.texture0);
        mMat.SetTexture("_FogTex1", mFog.texture1);
    }

    private void CleanUp()
    {
        mCam.RemoveCommandBuffer(CameraEvent.AfterSkybox, mBuf);
    }

    public void OnWillRenderObject()
    {
        if (mBuf == null)
        {
            mBuf = new CommandBuffer();
            mBuf.name = "Grab screen and blur";

            // copy screen into temporary RT
            int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
            mBuf.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
            mBuf.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);
            mBuf.SetGlobalTexture("_GrabBlurTexture", screenCopyID);
            mCam.AddCommandBuffer(CameraEvent.AfterSkybox, mBuf);
        }
    }
}