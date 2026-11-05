using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public const string SCENE_MAIN_MENU = "Scene00_MainMenu";
    public const string SCENE_SURFACE_CLEANUP = "Scene01_SurfaceCleanup";
    public const string SCENE_DEEP_SEA_DIVE = "Scene02_DeepSeaDive";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartStoryMode()
    {
        Debug.Log("[GameFlowManager] Memulai Story Mode -> Pindah ke " + SCENE_SURFACE_CLEANUP);
        SceneManager.LoadScene(SCENE_SURFACE_CLEANUP);
    }

    public void StartDirectDive()
    {
        Debug.Log("[GameFlowManager] Memulai Langsung Selam -> Pindah ke " + SCENE_DEEP_SEA_DIVE);
        SceneManager.LoadScene(SCENE_DEEP_SEA_DIVE);
    }

    public void GoToDeepSeaDive()
    {
        Debug.Log("[GameFlowManager] Transisi dari Surface Cleanup ke " + SCENE_DEEP_SEA_DIVE);
        SceneManager.LoadScene(SCENE_DEEP_SEA_DIVE);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("[GameFlowManager] Selesai / Reset -> Kembali ke " + SCENE_MAIN_MENU);
        SceneManager.LoadScene(SCENE_MAIN_MENU);
    }
}
