using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI testText;
    int testCount = 0;  
    private void Awake()
    {
        Instance = this;
    }
    public void TestFunction()
    {
        testText.text = testCount++.ToString();
    }

}
