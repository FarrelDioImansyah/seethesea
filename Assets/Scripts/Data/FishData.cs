using UnityEngine;

public enum BiotaCategory
{
    ReefFish,       // Ikan Terumbu Karang
    NonFishBiota    // Biota Laut Non-Ikan
}

[CreateAssetMenu(fileName = "NewBiotaData", menuName = "SeeTheSea/Biota Data")]
public class FishData : ScriptableObject
{
    [Header("Identitas Biota")]
    public string biotaId;
    public string commonName;
    public string scientificName;
    public BiotaCategory category = BiotaCategory.ReefFish;

    [Header("Habitat & Kedalaman")]
    public string depthRange;
    public float minDepthMeters;
    public float maxDepthMeters;

    [Header("Informasi Edukasi")]
    [TextArea(3, 6)]
    public string funFact;

    [Header("Asset Visual & Audio")]
    public Sprite illustration;
    public AudioClip discoverySfx;
}
