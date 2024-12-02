using UnityEngine;

public class Scene2Manager : MonoBehaviour
{
    public static Scene2Manager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    void Start()
    {
        AnchorTutorialUIManager.Instance.LoadAllAnchors();
    }


}
