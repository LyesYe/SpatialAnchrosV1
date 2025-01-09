using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class SceneNameDisplayTMP : MonoBehaviour
{
	public TextMeshProUGUI sceneNameText; // Reference to the TextMeshPro component

	void Start()
	{
		// Get the name of the current scene
		string currentSceneName = SceneManager.GetActiveScene().name;

		// Display the scene name in the TextMeshPro component
		if (sceneNameText != null)
		{
			sceneNameText.text = currentSceneName;
		}
	}
}