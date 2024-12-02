using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestingScript : MonoBehaviour
{
   public void ChangeScene()
    {
        SceneManager.LoadScene("Scene2");
    }
}
