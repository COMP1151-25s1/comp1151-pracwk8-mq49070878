using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class WorldHandler : MonoBehaviour
{
    private int seed;
    public Transform spawnAroundPoint;
    public int minimapRenderRadius = 5; // Set this to change the radius of chunks rendered on the minimap

    
    public List<WorldBiome> worldBiomes;
    //public WorldBiome currentBiome;
    public float biomeNoiseScale = 0.05f; // Lower = larger biomes

    
    public List<Chunk> chunks;
    public int chunkSize = 16;
    public int chunkRenderRadiusDistance = 4;

    public Material spaceBackground;
    private Color lerpColor;
    
    private int updateIndex = 0;

    private void Start()
    {
        seed = Random.Range(-9999, 9999);
        lerpColor = worldBiomes[0].biomeColor;
        SpawnChunksAroundPlayer();
        
        Texture2D minimap = GenerateBiomeMinimapTexture();
        if (minimapImage != null)
        {
            minimapImage.texture = minimap;
        }
    }

    public void SpawnChunksAroundPlayer()
    {
        Vector2Int centerChunk = GetChunkFromWorldPosition(spawnAroundPoint.position);

        // Use minimapRenderRadius to control the radius of chunks generated around the player
        for (int x = -minimapRenderRadius; x <= minimapRenderRadius; x++)
        {
            for (int y = -minimapRenderRadius; y <= minimapRenderRadius; y++)
            {
                Vector2Int currentChunkPosition = new Vector2Int(centerChunk.x + x, centerChunk.y + y);

                if (!DoesChunkExist(currentChunkPosition))
                {
                    WorldBiome newChunkBiome = GetBiomeFromNoise(currentChunkPosition);
                    Chunk newChunk = new Chunk(currentChunkPosition, newChunkBiome, chunkSize);
                    chunks.Add(newChunk);
                }
            }
        }
    }

    
    private WorldBiome GetBiomeFromNoise(Vector2Int chunkPosition)
    {
        float noiseX = chunkPosition.x * biomeNoiseScale;
        float noiseY = chunkPosition.y * biomeNoiseScale;

        float noiseValue = Mathf.PerlinNoise(noiseX+seed, noiseY-seed); // Range [0, 1]

        int biomeIndex = Mathf.FloorToInt(noiseValue * worldBiomes.Count);
        biomeIndex = Mathf.Clamp(biomeIndex, 0, worldBiomes.Count - 1);

        return worldBiomes[biomeIndex];
    }
    
    public RawImage minimapImage;
    
    public Texture2D GenerateBiomeMinimapTexture()
    {
        if (chunks.Count == 0) return null;

        Vector2Int centerChunk = GetChunkFromWorldPosition(spawnAroundPoint.position);
        int minX = centerChunk.x - minimapRenderRadius;
        int minY = centerChunk.y - minimapRenderRadius;
        int maxX = centerChunk.x + minimapRenderRadius;
        int maxY = centerChunk.y + minimapRenderRadius;

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        Texture2D minimapTexture = new Texture2D(width, height);
        minimapTexture.filterMode = FilterMode.Point; // Pixelated minimap appearance

        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.black;
        minimapTexture.SetPixels(clearPixels);

        foreach (Chunk chunk in chunks)
        {
            if (chunk.chunkPosition.x >= minX && chunk.chunkPosition.x <= maxX &&
                chunk.chunkPosition.y >= minY && chunk.chunkPosition.y <= maxY)
            {
                int x = chunk.chunkPosition.x - minX;
                int y = chunk.chunkPosition.y - minY;

                Color biomeColor = chunk.chunkBiome.biomeColor;
                minimapTexture.SetPixel(x, y, biomeColor);
            }
        }

        minimapTexture.Apply();
        return minimapTexture;
    }




    private void Update()
    {
        int loopThroughValue = (int)Mathf.Pow(chunkRenderRadiusDistance * 2, 2);
        updateIndex++;
        
        if (updateIndex >= loopThroughValue) updateIndex = 0;
        
        Vector2Int centerChunk = GetChunkFromWorldPosition(spawnAroundPoint.position);
        

        int renderRadius = chunkRenderRadiusDistance * 2;
    
        int chunkIndexX = updateIndex % renderRadius;
        int chunkIndexY = updateIndex / renderRadius;
    
        Vector2Int chunkPositionToCheck = new Vector2Int(centerChunk.x + chunkIndexX - chunkRenderRadiusDistance, centerChunk.y + chunkIndexY - chunkRenderRadiusDistance);

        if (!DoesChunkExist(chunkPositionToCheck))
        {
            WorldBiome newChunkBiome = worldBiomes[Random.Range(0, worldBiomes.Count)];
            Chunk newChunk = new Chunk(chunkPositionToCheck, newChunkBiome, chunkSize);
            chunks.Add(newChunk);
        }
        
        // space bg color
        Debug.Log("currentchunkbiome: " + GetChunk(centerChunk).chunkBiome.biomeName);
        lerpColor = Color.Lerp(lerpColor, GetChunk(centerChunk).chunkBiome.biomeColor, Time.deltaTime);
        spaceBackground.SetColor("_SpaceColor", lerpColor);
    }

    public Vector2Int GetChunkFromWorldPosition(Vector2 worldPosition)
    {
        Vector2 divPos = new Vector2(worldPosition.x / chunkSize, worldPosition.y / chunkSize);
        return Vector2Int.RoundToInt(divPos);
    }

    public bool DoesChunkExist(Vector2Int chunkPosition)
    {
        foreach (Chunk chunk in chunks)
        {
            if (chunk.chunkPosition == chunkPosition) return true;
        }
        
        return false;
    }
    
    public Chunk GetChunk(Vector2Int chunkPosition)
    {
        foreach (Chunk chunk in chunks)
        {
            if (chunk.chunkPosition == chunkPosition) return chunk;
        }
        
        return null;
    }
    
}

[System.Serializable]
public class Chunk
{
    public Vector2Int chunkPosition;
    public List<ChunkObjectData> chunkObjects;
    public WorldBiome chunkBiome;
    private float chunkSize;

    public Chunk(Vector2Int _pos, WorldBiome _biome, float _chunkSize)
    {
        chunkPosition = _pos;
        chunkBiome = _biome;
        chunkSize = _chunkSize;

        SpawnObjectsInChunk();
    }

    public void SpawnObjectsInChunk()
    {
        float sampleSize = 1f;
        float sampleMulti = sampleSize/(chunkSize*chunkSize);

        for (float x = (-chunkSize / 2); x < (chunkSize / 2); x += sampleSize)
        {
            for (float y = (-chunkSize / 2); y < (chunkSize / 2); y += sampleSize)
            {
                Vector2 perlinNoisePos = ((Vector2)chunkPosition * chunkSize) + new Vector2(x, y);
                //float noiseValue = Mathf.PerlinNoise(perlinNoisePos.x, perlinNoisePos.y);

                foreach (BiomeObjectsData biomeObject in chunkBiome.biomeObjects)
                {
                    float noiseValue = Random.Range(0f, 1f);

                    if (noiseValue <= biomeObject.density * sampleMulti)
                    {
                        GameObject newObj = GameObject.Instantiate(biomeObject.densityObject, perlinNoisePos, Quaternion.identity);
                        newObj.transform.rotation = biomeObject.GetRandomRot();
                        newObj.transform.localScale = biomeObject.GetRandomScl();
                        if (newObj.GetComponent<Rigidbody2D>() != null) newObj.GetComponent<Rigidbody2D>().linearVelocity = biomeObject.GetRandomVel();
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class ChunkObjectData
{
    public Vector2 _position;
    public Quaternion _rotation;
    public Vector2 _scale;
    
    public GameObject _gameObject;
}
