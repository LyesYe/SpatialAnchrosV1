using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class SaveAnchorsWhenQuit : MonoBehaviour
{
	public static SaveAnchorsWhenQuit Instance;

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
	private void SaveAnchorsToPlayerPrefs()
	{
		// Serialize the HashSet to a string (JSON, comma-separated, etc.)
		var uuids = string.Join(",", AnchorTutorialUIManager.Instance._anchorUuids.Select(g => g.ToString()).ToArray());

		// Save to PlayerPrefs
		PlayerPrefs.SetString("AnchorUuids", uuids);
		PlayerPrefs.Save(); // Make sure to save the changes
	}

	public void LoadAnchorsFromPlayerPrefs()
	{
		// Load the UUID string from PlayerPrefs
		if (PlayerPrefs.HasKey("AnchorUuids"))
		{
			var uuidsString = PlayerPrefs.GetString("AnchorUuids");

			// Deserialize the string back into a HashSet<Guid>
			AnchorTutorialUIManager.Instance._anchorUuids = new HashSet<Guid>(uuidsString.Split(',').Select(Guid.Parse));
		}
	}
	private void OnApplicationQuit()
	{
		SaveAnchorsToPlayerPrefs();
	}
}
