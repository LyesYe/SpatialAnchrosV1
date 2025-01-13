using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField]
    private TextMeshProUGUI _redCapsuleCountText;
    [SerializeField]
    private TextMeshProUGUI _greenCapsuleCountText;
    [SerializeField]
    private TextMeshProUGUI _farCapsuleCountText;

    private void Awake()
    {
        Instance = this;
    }
    private void Update()
    {
        UpdateCapsuleCountUI();
    }
    private void UpdateCapsuleCountUI()
    {
        print("adsa");
        //print(AnchorTutorialUIManager.Instance._redCapsuleCount);
        if (_redCapsuleCountText != null)
        {
            _redCapsuleCountText.text = $"{AnchorTutorialUIManager.Instance._redCapsuleCount}";
        }
        if (_greenCapsuleCountText != null)
        {
            _greenCapsuleCountText.text = $"{AnchorTutorialUIManager.Instance._greenCapsuleCount}";
        }        
        if (_farCapsuleCountText != null)
        {
            _farCapsuleCountText.text = $"Number of Capsules Farther than 3 Meters: {AnchorTutorialUIManager.Instance._fartherCapsulesCount}";
        }
    }

}
