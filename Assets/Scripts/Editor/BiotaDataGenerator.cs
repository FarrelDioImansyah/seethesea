#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class BiotaDataGenerator
{
    private struct BiotaDef
    {
        public string id;
        public string commonName;
        public string scientificName;
        public BiotaCategory category;
        public string depthRange;
        public float minDepth;
        public float maxDepth;
        public string funFact;

        public BiotaDef(string id, string commonName, string scientificName, BiotaCategory category, string depthRange, float minDepth, float maxDepth, string funFact)
        {
            this.id = id;
            this.commonName = commonName;
            this.scientificName = scientificName;
            this.category = category;
            this.depthRange = depthRange;
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
            this.funFact = funFact;
        }
    }

    [MenuItem("SeeTheSea/Generate All Biota Data Assets")]
    public static void GenerateBiotaAssets()
    {
        string targetDir = "Assets/Data/Biota";
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        BiotaDef[] biotaList = new BiotaDef[]
        {
            // A. Ikan Terumbu Karang
            new BiotaDef("ikan_napoleon", "Ikan Napoleon", "Cheilinus undulatus", BiotaCategory.ReefFish, "1–60 m", 1f, 60f,
                "Ikan karang berukuran raksasa dengan punuk di dahi dan bibir tebal. Penting bagi ekosistem karena memangsa hewan beracun seperti bintang laut berduri."),
            new BiotaDef("ikan_kerapu", "Ikan Kerapu", "Epinephelus spp.", BiotaCategory.ReefFish, "5–50 m", 5f, 50f,
                "Predator penyergap ulung yang suka bersembunyi di celah karang atau gua bawah laut menunggu mangsa melintas."),
            new BiotaDef("ikan_kakap", "Ikan Kakap", "Lutjanus spp.", BiotaCategory.ReefFish, "5–50 m", 5f, 50f,
                "Perenang tangguh bergigi tajam yang hidup berkelompok di tubir karang dan aktif mencari mangsa berupa ikan kecil dan udang."),
            new BiotaDef("ikan_kuwe_sirip_biru", "Ikan Kuwe Sirip Biru", "Caranx melampygus", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Predator karang perenang cepat dengan kilau warna biru elektrik pada siripnya saat berburu di perairan jernih."),
            new BiotaDef("ikan_kepe_kepe", "Ikan Kepe-kepe", "Chaetodon spp.", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Tubuhnya pipih dengan moncong ramping untuk memakan polip karang dan invertebrata kecil di celah sempit."),
            new BiotaDef("ikan_butana", "Ikan Butana (Surgeonfish)", "Acanthurus spp.", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Memiliki duri tajam setajam pisau bedah di pangkal ekornya sebagai pertahanan diri dari pemangsa."),
            new BiotaDef("ikan_unicorn", "Ikan Unicorn", "Naso spp.", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Memiliki tonjolan seperti cula di dahinya. Pemakan alga penting yang menjaga keseimbangan karang."),
            new BiotaDef("ikan_ekor_kuning", "Ikan Ekor Kuning (Fusilier)", "Caesionidae", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Berenang lincah dalam kawanan besar di sekitar lereng karang untuk menyaring zooplankton."),
            new BiotaDef("ikan_kakatua", "Ikan Kakatua", "Scaridae", BiotaCategory.ReefFish, "1–40 m", 1f, 40f,
                "Paruhnya yang kuat mengikis alga pada karang mati, menghasilkan butiran pasir putih halus yang membentuk pantai."),
            new BiotaDef("ikan_bidadari", "Ikan Bidadari", "Pomacanthidae", BiotaCategory.ReefFish, "3–50 m", 3f, 50f,
                "Pola warna tubuhnya sangat memukau dan mencolok, penanda terumbu karang yang sehat dan subur."),
            new BiotaDef("ikan_betok_giru", "Ikan Betok/Giru", "Pomacentridae", BiotaCategory.ReefFish, "1–40 m", 1f, 40f,
                "Hidup bersimbiosis di antara tentakel anemon beracun tanpa terluka berkat lapisan lendir pelindung di tubuhnya."),
            new BiotaDef("ikan_keling", "Ikan Keling", "Labridae", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Ikan lincah beraneka warna, beberapa jenis bertindak sebagai dokter laut yang membersihkan parasit ikan lain."),
            new BiotaDef("ikan_murjan", "Ikan Murjan", "Holocentridae", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Ikan nokturnal bermata besar dan berwarna merah cerah, aktif mencari makan di malam hari."),
            new BiotaDef("ikan_pakol", "Ikan Pakol", "Balistidae", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Dapat mengunci sirip punggungnya tegak untuk mengganjal tubuhnya di celah batu karang agar tidak ditarik pemangsa."),
            new BiotaDef("ikan_capungan", "Ikan Capungan", "Apogonidae", BiotaCategory.ReefFish, "1–50 m", 1f, 50f,
                "Ikan berukuran kecil di mana ikan jantan bertugas mengerami dan melindungi telur di dalam rongga mulutnya."),
            new BiotaDef("ikan_baronang", "Ikan Baronang", "Siganus spp.", BiotaCategory.ReefFish, "1–30 m", 1f, 30f,
                "Herbivora karang pemakan alga dan lamun, memiliki duri sirip beracun ringan untuk melindungi diri."),

            // B. Biota Laut Non-Ikan
            new BiotaDef("bintang_laut", "Bintang Laut", "Asteroidea", BiotaCategory.NonFishBiota, "<200 m hingga >1.000 m", 1f, 1000f,
                "Mampu meregenerasi lengan yang hilang dan membalikkan perutnya keluar mulut untuk mencerna mangsa."),
            new BiotaDef("bintang_ular_laut", "Bintang Ular Laut", "Ophiuroidea", BiotaCategory.NonFishBiota, "<200 m hingga >1.000 m", 1f, 1000f,
                "Memiliki lengan panjang menyerupai cambuk yang dapat meliuk lincah di lantai laut dangkal hingga palung laut dalam."),
            new BiotaDef("lili_laut", "Lili Laut", "Crinoidea", BiotaCategory.NonFishBiota, "<200 m hingga >1.000 m", 1f, 1000f,
                "Fauna purba berbentuk bunga mekar yang menangkap plankton dan detritus organik menggunakan tentakel berbulu halus."),
            new BiotaDef("bulu_babi", "Bulu Babi", "Echinoidea", BiotaCategory.NonFishBiota, "<200 m hingga >1.000 m", 1f, 1000f,
                "Berbentuk bola berduri tajam pelindung. Di dasar tubuhnya terdapat struktur mulut lima gigi bernama Lentera Aristoteles."),
            new BiotaDef("teripang", "Teripang", "Holothuroidea", BiotaCategory.NonFishBiota, "<200 m hingga >1.000 m", 1f, 1000f,
                "Penyaring sedimen alami di dasar laut yang memproses pasir dan mendaur ulang bahan organik esensial bagi ekosistem.")
        };

        int createdCount = 0;
        foreach (var def in biotaList)
        {
            string assetPath = $"{targetDir}/{def.id}.asset";
            FishData asset = AssetDatabase.LoadAssetAtPath<FishData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<FishData>();
                asset.biotaId = def.id;
                asset.commonName = def.commonName;
                asset.scientificName = def.scientificName;
                asset.category = def.category;
                asset.depthRange = def.depthRange;
                asset.minDepthMeters = def.minDepth;
                asset.maxDepthMeters = def.maxDepth;
                asset.funFact = def.funFact;

                AssetDatabase.CreateAsset(asset, assetPath);
                createdCount++;
            }
            else
            {
                // Update nilai jika sudah ada
                asset.biotaId = def.id;
                asset.commonName = def.commonName;
                asset.scientificName = def.scientificName;
                asset.category = def.category;
                asset.depthRange = def.depthRange;
                asset.minDepthMeters = def.minDepth;
                asset.maxDepthMeters = def.maxDepth;
                asset.funFact = def.funFact;
                EditorUtility.SetDirty(asset);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BiotaDataGenerator] Berhasil memproses {biotaList.Length} data biota ({createdCount} baru dibuat) di {targetDir}");
    }
}
#endif
