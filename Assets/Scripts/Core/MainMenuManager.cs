using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject attractPanel;   // Teks "Angkat tanganmu untuk memulai"
    public GameObject menuPanel;      // Panel berisi 2 tombol (Story & Direct)

    [Header("Status")]
    public bool isMenuOpen = false;

    [Header("Tombol 3D")]
    public GameObject menuButtons3DGroup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        ShowAttractMode();
    }

    private void Update()
    {
        // Deteksi angkat tangan (Spasi di mode mouse)
        if (!isMenuOpen && KinectInputManager.Instance != null)
        {
            if (KinectInputManager.Instance.IsHandRaisedAboveShoulder)
            {
                OpenMenu();
            }
        }
    }
    public void ShowAttractMode()
    {
        isMenuOpen = false;
        if (attractPanel != null) attractPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (menuButtons3DGroup != null) menuButtons3DGroup.SetActive(false); // Sembunyi di awal
    }

    public void OpenMenu()
    {
        isMenuOpen = true;
        if (attractPanel != null) attractPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
        if (menuButtons3DGroup != null) menuButtons3DGroup.SetActive(true); // Muncul setelah Spasi
    }

    public void SelectStoryMode()
    {
        Debug.Log("[MainMenu] Memilih Story Mode -> Pindah ke Scene01_SurfaceCleanup");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartStoryMode();
        }
        else
        {
            SceneManager.LoadScene("Scene01_SurfaceCleanup");
        }
    }

    public void SelectDirectDiveMode()
    {
        Debug.Log("[MainMenu] Memilih Langsung Selam -> Pindah ke Scene02_DeepSeaDive");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartDirectDive();
        }
        else
        {
            SceneManager.LoadScene("Scene02_DeepSeaDive");
        }
    }

}