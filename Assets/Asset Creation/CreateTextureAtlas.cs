using UnityEngine;


public class CreateTextureAtlas : MonoBehaviour
{
    // Name of directory to get files from
    public string mDirectoryName = "blocks";
    public string mOutputFileName = "../atlas.png";
    [Header("Buttons")]
    public bool CreateAtlas = false;

    public void OnValidate()
    {
        if (CreateAtlas)
        {

            UnityEngine.Debug.Log("Starting");

            TextureAtlas.instance.CreateAtlasComponentData(mDirectoryName, mOutputFileName);

            UnityEngine.Debug.Log("Done with creation of texture atlas.");

            CreateAtlas = false;
        }
    }
    public void Awake()
    {


    }

    
}
