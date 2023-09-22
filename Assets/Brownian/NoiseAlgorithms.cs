using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class NoiseAlgorithm
{
    // variables used in Perlin's original algorithm at their original sizes
    public const int PERM_SIZE = 256;
    public const int DIMENSIONS = 3;
    private static NativeArray<int> permutation = new NativeArray<int>(PERM_SIZE + PERM_SIZE + 2, Allocator.Persistent);
    private static NativeArray<float> randomArray = new NativeArray<float>((PERM_SIZE + PERM_SIZE + 2) * DIMENSIONS, Allocator.Persistent);

    // terrain chunk related variables
    public int width;
    public int depth;

    // the algorithm expects that all values are floating point numbers
    // and will return 0's when they aren't - so we budge
    // all indices by a consistent random number
    private float xOffset;
    private float yOffset;

    // perlin specific noise variables
    public float positionX;
    public float positionZ;
    public int octaves;
    public float frequency;
    public float amplitude;
    public float lacunarity;
    public float gain;
    public float scale;
    public float normalizeBias;

    public void InitializeNoise(int w, int d, int seed)
    {
        width = w;
        depth = d;
        UnityEngine.Random.InitState(seed);

        this.xOffset = UnityEngine.Random.Range(0.0f, 0.9999f) ;
        this.yOffset = UnityEngine.Random.Range(0.0f, 0.9999f) ;


        // since we know sizes now, create perlin arrays
        perlinCreateArrays();
    }

    public void InitializePerlinNoise()
    {
        frequency = 1;
        amplitude = 0.5f;
        octaves = 8;
        lacunarity = 2.0f;
        gain = 0.5f;
        scale = 0.01f;
        normalizeBias = 1.0f;
    }

    public void InitializePerlinNoise(float freq, float amp, int oct, float lac, float gain, float sc, float nb)
    {
        frequency = freq;
        amplitude = amp;
        octaves = oct;
        lacunarity = lac;
        this.gain = gain;
        scale = sc;
        normalizeBias = nb;
    }


    // creating the random permutation and noise arrays
    // a step that many of the perlin alg copies seem to lack
    private void perlinCreateArrays()
    {
        float s;
        Vector3 tmp;
        float[] v = new float[DIMENSIONS];
        int i;
        int j;
        int k;

        // create an array of random gradient vectors uniformly on the unit sphere
        for (i = 0; i < PERM_SIZE; i++)
        {
            do
            {
                for (j = 0; j < DIMENSIONS; j++)
                {
                    v[j] = (float)((UnityEngine.Random.Range(0, System.Int32.MaxValue) % (PERM_SIZE + PERM_SIZE)) - PERM_SIZE) / PERM_SIZE;
                }
                tmp = new Vector3(v[0], v[1], v[2]);
                s = Vector3.Dot(tmp, tmp);
            } while (s > 1.0);

            s = Mathf.Sqrt(s);
            int row = i * DIMENSIONS;
            for (j = 0; j < DIMENSIONS; j++)
            {
                randomArray[row + j] = v[j] / s;
            }


        }

        // create a pseudorandom permutation of [1 .. PERM_SIZE]
        for (i = 0; i < PERM_SIZE; i++)
        {
            permutation[i] = i;
        }

        for (i = PERM_SIZE; i > 0; i -= 2)
        {
            permutation[i] = i;
            k = permutation[i];
            j = UnityEngine.Random.Range(0, System.Int32.MaxValue) % PERM_SIZE;
            permutation[i] = permutation[j];
            permutation[j] = k;
        }

        // extend arrays to allow for faster indexing
        for (i = 0; i < PERM_SIZE + 2; i++)
        {
            permutation[PERM_SIZE + i] = permutation[i];
            int permRow = (PERM_SIZE + i) * DIMENSIONS;
            int row = i * DIMENSIONS;
            for (j = 0; j < DIMENSIONS; j++)
            {
                randomArray[permRow + j] = randomArray[row + j];
            }
        }

    }

    // assumes that InitializeBlocks has been called first
    // blocks should be in a 1D array
    public void setNoise(NativeArray<float> noiseIndices, int cx, int cy)
    {
        // set up and run noise function
        var perlin = new CalculatePerlin
        {
            heightMap = noiseIndices,
            permutation = NoiseAlgorithm.permutation,
            randomArray = NoiseAlgorithm.randomArray,
            dimensions = NoiseAlgorithm.DIMENSIONS,
            permSize = NoiseAlgorithm.PERM_SIZE,
            width = this.width,
            xOffset = this.xOffset,
            yOffset = this.yOffset,
            zOffset = 0,
            positionX = cx,
            positionZ = cy,
            scale = this.scale,
            octaves = this.octaves,
            frequency = this.frequency,
            amplitude = this.amplitude,
            lacunarity = this.lacunarity,
            gain = this.gain,
        };
        var perlinJob = perlin.Schedule(width * depth, 64);
        perlinJob.Complete();

        //normalize all those values
        var normalize = new NormalizePerlin
        {
            heightMap = noiseIndices,
            normBias = normalizeBias,
        };
        var normalizeJob = normalize.Schedule(width * depth, 64);
        normalizeJob.Complete();

    }

    public static void OnExit()
    {
        // clean up static arrays
        permutation.Dispose();
        randomArray.Dispose();
    }

}



[BurstCompile(CompileSynchronously = true)]
public struct CalculatePerlin : IJobParallelFor
{
    public NativeArray<float> heightMap;
    [ReadOnly] public NativeArray<int> permutation;
    [ReadOnly] public NativeArray<float> randomArray;
    [ReadOnly] public int dimensions;
    [ReadOnly] public int permSize;
    [ReadOnly] public int width;
    [ReadOnly] public float xOffset;
    [ReadOnly] public float yOffset;
    [ReadOnly] public float zOffset;
    [ReadOnly] public float positionX;
    [ReadOnly] public float positionZ;
    [ReadOnly] public float scale;
    [ReadOnly] public int octaves;
    [ReadOnly] public float frequency;
    [ReadOnly] public float amplitude;
    [ReadOnly] public float lacunarity;
    [ReadOnly] public float gain;

    // fill a 1D array that is actually 2D with perlin noise, representing a heightmap
    public void Execute(int index)
    {

        // since we want the noise to be consistent based on the indices
        // of the map, we scale and offset them
        int x = index / width;
        int y = index % width;
        heightMap[index] = height2d((positionX + x) * scale + xOffset, (positionZ + y) * scale + yOffset, octaves, lacunarity, gain) + zOffset;
    }

    // return a single value for a heightmap
    // apply gain 
    // does basic fractal brownian motion
    // by increasing the freq.
    private float height2d(float x, float y, int octaves,
             float lacunarity = 1.0f, float gain = 1.0f)
    {

        float freq = frequency, amp = amplitude;
        float sum = 0.0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += pnoise(x * freq, y * freq, 0) * amp;
            freq *= lacunarity; // amount we increase freq by for each loop through
            amp *= gain;
        }

        return sum;
    }

    // the Perlin noise algorithm as written by Perlin
    // lots of dot products and lerps
    private float pnoise(float x, float y, float z)
    {

        int bx0, bx1, by0, by1, bz0, bz1, b00, b10, b01, b11;
        float rx0, rx1, ry0, ry1, rz0, rz1, sx, sy, sz, a, b, c, d, t, u, v;
        int i, j;

        setup(x, out bx0, out bx1, out rx0, out rx1, out t);
        setup(y, out by0, out by1, out ry0, out ry1, out t);
        setup(z, out bz0, out bz1, out rz0, out rz1, out t);

        i = permutation[bx0];
        j = permutation[bx1];

        b00 = permutation[i + by0];
        b10 = permutation[j + by0];
        b01 = permutation[i + by1];
        b11 = permutation[j + by1];

        sx = s_curve(rx0);
        sy = s_curve(ry0);
        sz = s_curve(rz0);

        // This uses a different dropoff function that's supposed to work better.
        // uncomment to see the difference
        //sx = fade(rx0);  
        //sy = fade(ry0); 
        //sz = fade(rz0);

        int row = (b00 + bz0) * dimensions;
        u = dotProduct(randomArray[row + 0], randomArray[row + 1], randomArray[row + 2],
            rx0, ry0, rz0);
        row = (b10 + bz0) * dimensions;
        v = dotProduct(randomArray[row + 0], randomArray[row + 1], randomArray[row + 2],
            rx1, ry0, rz0);
        a = lerpP(sx, u, v);

        row = (b01 + bz0) * dimensions;
        u = dotProduct(randomArray[row + 0], randomArray[row + 1], randomArray[row + 2],
            rx0, ry1, rz0);
        row = (b11 + bz0) * dimensions;
        v = dotProduct(randomArray[row + 0], randomArray[row + 1], randomArray[row + 2],
            rx1, ry1, rz0);
        b = lerpP(sx, u, v);

        c = lerpP(sy, a, b);

        row = (b00 + bz1) * dimensions;
        u = dotProduct(randomArray[row + 0], randomArray[row + 1], randomArray[row + 2],
            rx0, ry0, rz1);
        row = (b10 + bz1) * dimensions;
        v = dotProduct(randomArray[row + 0], randomArray[row + 1], randomArray[row + 2],
            rx1, ry0, rz1);
        a = lerpP(sx, u, v);

        row = (b01 + bz1) * dimensions;
        u = dotProduct(randomArray[row + 0], randomArray[row + 1], randomArray[row + 2],
            rx0, ry1, rz1);
        row = (b11 + bz1) * dimensions;
        v = dotProduct(randomArray[row + 0], randomArray[row + 1], randomArray[row + 2],
            rx1, ry1, rz1);
        b = lerpP(sx, u, v);

        d = lerpP(sy, a, b);

        return (1.5f * lerpP(sz, c, d));
    }

    // a utility function that sets up variables used by the noise function from Perlin's paper
    private void setup(float number, out int b0, out int b1, out float r0, out float r1, out float t)
    {
        t = number + 10000.0f;
        b0 = ((int)t) & (permSize - 1);
        b1 = (b0 + 1) & (permSize - 1);
        r0 = t - (int)t;
        r1 = r0 - 1;
    }

    // a utility function in Perlin's paper 
    // gives the cubic approximation of the component dropoff
    private float s_curve(float t)
    {
        return (t * t * (3.0f - 2.0f * t));

    }

    // A dot product between two vectors represented in a bunch of individual float var's
    // from Perlin's paper
    private float dotProduct(float q1, float q2, float q3, float r1, float r2, float r3)
    {
        Vector3 tmp2 = new Vector3(q1, q2, q3);
        Vector3 tmp = new Vector3(r1, r2, r3);
        return Vector3.Dot(tmp2, tmp);
    }

    // utility function for a different dropoff function that can be tried
    private float fade(float t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }


    // utility function that interpolates data from different points on the surface
    private static float lerpP(float t, float a, float b)
    {
        return a + t * (b - a);
    }

}

 //this is written separately from the alg to
 //allow for different normalization techniques to be used
 //although it's very simple at the moment...
[BurstCompile(CompileSynchronously = true)]
public struct NormalizePerlin : IJobParallelFor
{
    public NativeArray<float> heightMap;
    [ReadOnly] public float normBias;

    // Normalize all data
    public void Execute(int index)
    {
        // max and min for perlin alg
        float highestPoint = 1.0f;
        float lowestPoint = -1f;
        // Normalised max and min we'd like to actually use for indexing into a block array with positive indices
        float normalisedHeightRange = 1.0f;
        float normaliseMin = 0.0f;
        float heightRange = highestPoint - lowestPoint;
        float normalisedHeight = ((heightMap[index] - lowestPoint) / heightRange) * normalisedHeightRange;
        heightMap[index] = (normaliseMin + normalisedHeight) * normBias;
        if (heightMap[index] > 1.0f)
        {
            heightMap[index] = 0.99f;
        }
    }


}