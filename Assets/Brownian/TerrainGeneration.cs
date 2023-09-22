using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using System.IO;


[System.Serializable]
public class StriationTexture
{
    public float startHeight;
    public float endHeight;
    public int textureIndex;
    public GameObject spawn;
    public float spawnChance;

}
[System.Serializable]
public class StriationHeight
{
    public float startHeight;
    public float endHeight;
    public AnimationCurve terrainCurve;
}

public class TerrainGeneration : MonoBehaviour
{
    public int mRandomSeed;
    public int xChunks;
    public int yChunks;
    public int mWidth;
    public int mDepth;
    public int mMaxHeight;
    public Material mTerrainMaterial;

    public float snowfallNormal;
    public int snowfallIndex;
    public float snowfallMinHeight;

    public List<StriationTexture> striationTextures;
    public List<StriationHeight> StriationHeightCurves;


    private GameObject realTerrain;
    private NoiseAlgorithm terrainNoise;
    
    // code to get rid of fog from: https://forum.unity.com/threads/how-do-i-turn-off-fog-on-a-specific-camera-using-urp.1373826/
    // Unity calls this method automatically when it enables this component
    private void OnEnable()
    {
        // Add WriteLogMessage as a delegate of the RenderPipelineManager.beginCameraRendering event
        RenderPipelineManager.beginCameraRendering += BeginRender;
        RenderPipelineManager.endCameraRendering += EndRender;
    }
 
    // Unity calls this method automatically when it disables this component
    private void OnDisable()
    {
        // Remove WriteLogMessage as a delegate of the  RenderPipelineManager.beginCameraRendering event
        RenderPipelineManager.beginCameraRendering -= BeginRender;
        RenderPipelineManager.endCameraRendering -= EndRender;
    }
 
    // When this method is a delegate of RenderPipeline.beginCameraRendering event, Unity calls this method every time it raises the beginCameraRendering event
    void BeginRender(ScriptableRenderContext context, Camera camera)
    {
        // Write text to the console
        //Debug.Log($"Beginning rendering the camera: {camera.name}");
 
        if(camera.name == "Main Camera No Fog")
        {
            //Debug.Log("Turn fog off");
           // RenderSettings.fog = false;
        }
         
    }
 
    void EndRender(ScriptableRenderContext context, Camera camera)
    {
        //Debug.Log($"Ending rendering the camera: {camera.name}");
        if (camera.name == "Main Camera No Fog")
        {
            //Debug.Log("Turn fog on");
            //RenderSettings.fog = true;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        // create a height map using perlin noise and fractal brownian motion


        // create the mesh and set it to the terrain variable
        for(int i = 0; i < xChunks; i++)
        {
            for(int j = 0; j < yChunks; j++)
            {
                CreateChunk( ((int)(i - 0.5*xChunks)) * mWidth, ((int)(j - 0.5*yChunks)) * mDepth);
            }
        }



        
        NoiseAlgorithm.OnExit();
    }

    private void CreateChunk(int xOffset, int yOffset)
    {
        terrainNoise = new NoiseAlgorithm();
        terrainNoise.InitializeNoise(mWidth + 1, mDepth + 1, mRandomSeed);
        terrainNoise.InitializePerlinNoise(1.0f, 0.5f, 8, 2.0f, 0.5f, 0.01f, 1.0f);
        NativeArray<float> terrainHeightMap = new NativeArray<float>((mWidth + 1) * (mDepth + 1), Allocator.Persistent);
        terrainNoise.setNoise(terrainHeightMap, xOffset, yOffset);
        realTerrain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        realTerrain.transform.position = new Vector3(xOffset, 0, yOffset) ;
        MeshRenderer meshRenderer = realTerrain.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = realTerrain.GetComponent<MeshFilter>();
        meshRenderer.material = mTerrainMaterial;
        UnityEngine.Random.InitState(mRandomSeed + xOffset + (yOffset * 100));
        meshFilter.mesh = GenerateTerrainMesh(terrainHeightMap, xOffset, yOffset);

        terrainHeightMap.Dispose();
    }



    // create a new mesh with
    // perlin noise done blankly from Mathf.PerlinNoise in Unity
    // without any other features
    // makes a quad and connects it with the next quad
    // uses whatever texture the material is given
    public Mesh GenerateTerrainMesh(NativeArray<float> heightMap, int xOffset, int yOffset)
    {
        int width = mWidth + 1, depth = mDepth + 1;
        int height = mMaxHeight;
        int indicesIndex = 0;
        int vertexIndex = 0;
        int vertexMultiplier = 4; // create quads to fit uv's to so we can use more than one uv (4 vertices to a quad)

        Mesh terrainMesh = new Mesh();
        List<Vector3> vert = new List<Vector3>(width * depth * vertexMultiplier);
        List<int> indices = new List<int>(width * depth * 6);
        List<Vector2> uvs = new List<Vector2>(width * depth);
        TextureData textureData;


        string path = "UVData.txt";//Application.persistentDataPath + "/UVData.txt";

        StreamReader reader = new StreamReader(path, false);
        

        string json = reader.ReadToEnd();
        textureData = JsonUtility.FromJson<TextureData>(json);

    
        reader.Close();


        for (int x = 0; x < width-1; x++)
        {
            for (int z = 0; z < depth-1; z++)
            {
                //if (x < width - 1 && z < depth - 1)
                {

                    // note: since perlin goes up to 1.0 multiplying by a height will tend to set
                    // the average around maxheight/2. We remove most of that extra by subtracting maxheight/2
                    // so our ground isn't always way up in the air
                    float y = heightMap[(x) * (width) + (z)] * height - (mMaxHeight/2.0f);
                    float useAltXPlusY = heightMap[(x + 1) * (width) + (z)] * height - (mMaxHeight/2.0f);
                    float useAltZPlusY = heightMap[(x) * (width) + (z + 1)] * height- (mMaxHeight/2.0f);
                    float useAltXAndZPlusY = heightMap[(x + 1) * (width) + (z + 1)] * height- (mMaxHeight/2.0f);

                    float3 vert1 = new float3(x, y, z);
                    float3 vert2 = new float3(x, useAltZPlusY, z + 1);
                    float3 vert3 = new float3(x + 1, useAltXPlusY, z);
                    float3 vert4 = new float3(x + 1, useAltXAndZPlusY, z + 1);


                    Vector3 edge1= vert1 - vert2;
                    Vector3 edge2= vert1 - vert3;
                    Vector3 normal = Vector3.Cross(edge1.normalized, edge2.normalized);
                    float h = (vert1.y + vert2.y + vert3.y + vert4.y) / 4;
                    


                    foreach(StriationHeight s in StriationHeightCurves)
                    {
                        if(vert1.y>s.startHeight&& vert1.y < s.endHeight)
                        {
                            float striationSize = s.endHeight - s.startHeight;
                            vert1.y = s.terrainCurve.Evaluate((vert1.y-s.startHeight)/striationSize)*striationSize + s.startHeight;

                        }
                        if (vert2.y > s.startHeight && vert2.y < s.endHeight)
                        {
                            float striationSize = s.endHeight - s.startHeight;
                            vert2.y = s.terrainCurve.Evaluate((vert2.y - s.startHeight) / striationSize) * striationSize +s.startHeight;

                        }
                        if (vert3.y > s.startHeight && vert3.y < s.endHeight)
                        {
                            float striationSize = s.endHeight - s.startHeight;
                            vert3.y = s.terrainCurve.Evaluate((vert3.y - s.startHeight) / striationSize) * striationSize + s.startHeight;

                        }
                        if (vert4.y > s.startHeight && vert4.y < s.endHeight)
                        {
                            float striationSize = s.endHeight - s.startHeight;
                            vert4.y = s.terrainCurve.Evaluate((vert4.y - s.startHeight) / striationSize) * striationSize + s.startHeight;
                        }

                    }
                    vert.Add(vert1);
                    vert.Add(vert2); 
                    vert.Add(vert3);  
                    vert.Add(vert4);





                    // add uv's
                    // remember to give it all 4 sides of the image coords
                    //the closer y is to 1 the flatter the surface
                    int index = 0;
                    foreach (StriationTexture s in striationTextures)
                    {
                        if(h>s.startHeight&& h < s.endHeight)
                        {
                            index = s.textureIndex;
                            if (UnityEngine.Random.value*100 < s.spawnChance)
                            {
                                Instantiate(s.spawn, vert1 + new float3(xOffset, 0, yOffset), Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
                            }
                        }

                    }
                    
                    if (normal.y > snowfallNormal && h>snowfallMinHeight)
                    {
                       
                        index = snowfallIndex;
                    }
   

                    
                    uvs.Add(new Vector2(textureData.textureUVs[index].pixelStartX + 0.00f, textureData.textureUVs[index].pixelStartY + 0.00f));
                    uvs.Add(new Vector2(textureData.textureUVs[index].pixelEndX2 + -0.00f, textureData.textureUVs[index].pixelStartY2 + 0.00f));
                    uvs.Add(new Vector2(textureData.textureUVs[index].pixelStartX2 + 0.00f, textureData.textureUVs[index].pixelEndY+  -0.00f));
                    uvs.Add(new Vector2(textureData.textureUVs[index].pixelEndX + -0.00f, textureData.textureUVs[index].pixelEndY2 + -0.00f));


                    


                    // front or top face indices for a quad
                    //0,2,1,0,3,2
                    indices.Add(vertexIndex);
                    indices.Add(vertexIndex + 1);
                    indices.Add(vertexIndex + 2);
                    indices.Add(vertexIndex + 3);
                    indices.Add(vertexIndex + 2);
                    indices.Add(vertexIndex + 1);
                    indicesIndex += 6;
                    vertexIndex += vertexMultiplier;
                }
            }

        }
        
        // set the terrain var's for the mesh
        terrainMesh.vertices = vert.ToArray();
        terrainMesh.triangles = indices.ToArray();
        terrainMesh.SetUVs(0, uvs);
        
        // reset the mesh
        terrainMesh.RecalculateNormals();
        terrainMesh.RecalculateBounds();
       
        return terrainMesh;
    }

}
