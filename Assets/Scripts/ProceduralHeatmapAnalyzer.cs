using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ProceduralHeatmapAnalyzer : MonoBehaviour
{
    [Header("Setup")]
    public SkinnedMeshRenderer sourceSkinnedMesh;

    public GameObject [] ObjectsToDisable;
    public Shader heatmapShader;

    [Header("Controls")]
   
    private Material originalMaterial;
    private Material heatmapMaterialInstance;

    private Vector3[] baseVertices;
    private float[] maxDisplacements;
    private ComputeBuffer displacementBuffer;

    // This tracks the actual, internal state of the heatmap
    bool isHeatmapActive = false;

    private Mesh deformedMesh;
    private Mesh bakeMesh;

    bool justEnabled = false;

    

    private void Awake() {
        deformedMesh = new Mesh();

        bakeMesh = new Mesh();
    }
    //private void Start() {
    //    if(sourceSkinnedMesh == null || heatmapShader == null) return;

    //}



    public void ResetHeatmapData() {
        Debug.Log("Resetting heatmap data arrays and buffers.");

        if(maxDisplacements != null) {            
            System.Array.Clear(maxDisplacements, 0, maxDisplacements.Length);
        }

        // Initialize a fresh array for displacement data, automatically filled with zeros.
        maxDisplacements = new float[baseVertices.Length];

        // Ensure any old buffer is released before creating a new one.
        if(displacementBuffer != null) {
            displacementBuffer.Release();
        }

        // Create a new compute buffer and set it on the material.
        displacementBuffer = new ComputeBuffer(baseVertices.Length, sizeof(float));
        if(heatmapMaterialInstance != null) {
            heatmapMaterialInstance.SetBuffer("_DisplacementBuffer", displacementBuffer);
        }
    }


    public void EnableHeatmap() {

        if(isHeatmapActive) return;

        justEnabled = true;
        if(sourceSkinnedMesh == null || heatmapShader == null) return;
        Debug.Log("enabled");

        

        originalMaterial = sourceSkinnedMesh.material;
        heatmapMaterialInstance = new Material(originalMaterial);

        // Store original material and create an instance for the heatmap
        heatmapMaterialInstance.shader = heatmapShader;
        sourceSkinnedMesh.material = heatmapMaterialInstance;



        sourceSkinnedMesh.BakeMesh(bakeMesh, true);
        baseVertices = bakeMesh.vertices;
        //Destroy(tempMesh);

        // Explicitly reset all data and buffers
        ResetHeatmapData();


        
        isHeatmapActive = true;
        


        foreach(GameObject o in ObjectsToDisable)
            o.SetActive(false);
        Debug.Log("Live heatmap ENABLED.");
    }
    

    public void DisableHeatmap() {
        if(originalMaterial != null) {
            sourceSkinnedMesh.material = originalMaterial;
        }

        if(displacementBuffer != null) {
            displacementBuffer.Release();
            displacementBuffer = null;
        }

        if(maxDisplacements!=null)
            System.Array.Clear(maxDisplacements, 0, maxDisplacements.Length);

        isHeatmapActive = false;

        Destroy(heatmapMaterialInstance);
        heatmapMaterialInstance = null;




        foreach(GameObject o in ObjectsToDisable)
            o.SetActive(true);
        Debug.Log("Live heatmap DISABLED.");
    }

    void LateUpdate() {
        if(!isHeatmapActive) return;

        if(justEnabled) {
            // first frame after enabling → capture true neutral pose
            bakeMesh.Clear();
            sourceSkinnedMesh.BakeMesh(bakeMesh, true);
            baseVertices = bakeMesh.vertices;

            ResetHeatmapData();   // zero out maxDisplacements & buffer
            justEnabled = false;
            return;               // skip displacement pass this frame
        }

        
        sourceSkinnedMesh.BakeMesh(deformedMesh, true);
        Vector3[] currentVertices = deformedMesh.vertices;

        for(int i = 0; i < baseVertices.Length; i++) {
            float displacement = Vector3.Distance(baseVertices[i], currentVertices[i]);
            if(displacement > maxDisplacements[i]) {
                maxDisplacements[i] = displacement;
            }
        }
        heatmapMaterialInstance.SetFloat("_MaxDisplacement", 0f);

        float currentGlobalMax = maxDisplacements.Max();

        displacementBuffer.SetData(maxDisplacements);
        heatmapMaterialInstance.SetFloat("_MaxDisplacement", currentGlobalMax);
    }

    void OnDestroy() {
        // Ensure resources are cleaned up when play mode stops
        if(isHeatmapActive) {
            DisableHeatmap();
        }
    }
}