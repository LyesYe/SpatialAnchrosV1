using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AnchorTutorialUIManager : MonoBehaviour
{
	/// <summary>
	/// Anchor Tutorial UI manager singleton instance
	/// </summary>
	public static AnchorTutorialUIManager Instance;

	[SerializeField]
	private GameObject _saveableAnchorPrefab;

	[SerializeField]
	private GameObject _saveablePreview;

	[SerializeField]
	private Transform _saveableTransform;

	[SerializeField]
	private GameObject _nonSaveableAnchorPrefab;

	[SerializeField]
	private GameObject _nonSaveablePreview;

	[SerializeField]
	private Transform _nonSaveableTransform;

	[SerializeField]
	private GameObject _textPrefab; // Prefab pour afficher l'UUID


	private Dictionary<Guid, string> _capsuleNames = new();  // Maps UUID to Capsule N name
	private int _capsuleCount = 0; // Increment to create unique names




	private List<OVRSpatialAnchor> _anchorInstances = new(); // Active instances (red and green)

	private HashSet<Guid> _anchorUuids = new(); // Simulated external location, like PlayerPrefs

	private Action<bool, OVRSpatialAnchor.UnboundAnchor> _onLocalized;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			_onLocalized = OnLocalized;
		}
		else
		{
			Destroy(this);
		}
	}

	// This script responds to five button events:
	//
	// Left trigger: Create a saveable (green) anchor.
	// Right trigger: Create a non-saveable (red) anchor.
	// A: Load, Save and display all saved anchors (green only)
	// X: Destroy all runtime anchors (red and green)
	// Y: Erase all anchors (green only)
	// others: no action
	void Update()
	{
		if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) // Create a green capsule
		{
			// Create a green (savable) spatial anchor
			var go = Instantiate(_saveableAnchorPrefab, _saveableTransform.position, _saveableTransform.rotation); // Anchor A
			SetupAnchorAsync(go.AddComponent<OVRSpatialAnchor>(), saveAnchor: true);
		}
		else if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // Create a red capsule
		{
			// Create a red (non-savable) spatial anchor.
			var go = Instantiate(_nonSaveableAnchorPrefab, _nonSaveableTransform.position, _nonSaveableTransform.rotation); // Anchor b
			SetupAnchorAsync(go.AddComponent<OVRSpatialAnchor>(), saveAnchor: false);
		}
		else if (OVRInput.GetDown(OVRInput.Button.One)) // a button
		{
			LoadAllAnchors();
		}
		else if (OVRInput.GetDown(OVRInput.Button.Three)) // x button
		{
			// Destroy all anchors from the scene, but don't erase them from storage
			foreach (var anchor in _anchorInstances)
			{
				Destroy(anchor.gameObject);
			}

			// Clear the list of running anchors
			_anchorInstances.Clear();
		}
		else if (OVRInput.GetDown(OVRInput.Button.Four)) // y button
		{
			EraseAllAnchors();
		}
		else if (OVRInput.GetDown(OVRInput.Button.Two))
		{
                SceneManager.LoadScene("Scene2");
        }
	}

	// You need to make sure the anchor is ready to use before you save it.
	// Also, only save if specified
	private async void SetupAnchorAsync(OVRSpatialAnchor anchor, bool saveAnchor)
	{
		// Wait until the anchor is localized
		if (!await anchor.WhenLocalizedAsync())
		{
			Debug.LogError("Unable to localize anchor.");
			Destroy(anchor.gameObject);
			return;
		}

		// Add the anchor to the active list of anchors
		_anchorInstances.Add(anchor);

		// Associate this anchor with a unique "Capsule N" name
		_capsuleCount++;
		_capsuleNames[anchor.Uuid] = $"Capsule {_capsuleCount}";

		// Display the capsule name above the anchor
		DisplayAnchorName(anchor);

		// If the anchor is saveable, save it
		if (saveAnchor && (await anchor.SaveAnchorAsync()).Success)
		{
			_anchorUuids.Add(anchor.Uuid);
		}
	}

	/******************* Load Anchor Methods **********************/
	public async void LoadAllAnchors()
	{
		// Load and localize
		var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
		var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(_anchorUuids, unboundAnchors);

		if (result.Success)
		{
			foreach (var anchor in unboundAnchors)
			{
				anchor.LocalizeAsync().ContinueWith(_onLocalized, anchor);
			}
		}
		else
		{
			Debug.LogError($"Load anchors failed with {result.Status}.");
		}
	}

	private void OnLocalized(bool success, OVRSpatialAnchor.UnboundAnchor unboundAnchor)
	{
		var pose = unboundAnchor.Pose;
		var go = Instantiate(_saveableAnchorPrefab, pose.position, pose.rotation);
		var anchor = go.AddComponent<OVRSpatialAnchor>();

		unboundAnchor.BindTo(anchor);

		// Add the anchor to the active list
		_anchorInstances.Add(anchor);

		// Ensure the name is displayed based on the UUID
		DisplayAnchorName(anchor);
	}

	/******************* Erase Anchor Methods *****************/
	// If the Y button is pressed, erase all anchors saved
	// in the headset, but don't destroy them. They should remain displayed.
	public async void EraseAllAnchors()
	{
		var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: _anchorUuids);
		if (result.Success)
		{
			// Erase our reference lists
			_anchorUuids.Clear();

			Debug.Log($"Anchors erased.");
		}
		else
		{
			Debug.LogError($"Anchors NOT erased {result.Status}");
		}
	}


	private void DisplayAnchorName(OVRSpatialAnchor anchor)
	{
		// Create a new TextMeshPro object above the anchor, but offset it in the Y direction (top of the capsule)
		var textObject = Instantiate(_textPrefab, anchor.transform.position + Vector3.up * 0.2f, Quaternion.identity);

		// Set the text object as a child of the anchor, ensuring it follows its rotation
		textObject.transform.SetParent(anchor.transform);

		// Ensure the text is properly aligned and doesn't get offset due to anchor's local rotation
		textObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);  // Correct position relative to the capsule

		// Correct the rotation so the text always faces the camera (optional)
		textObject.transform.rotation = Quaternion.identity; // This keeps the text upright

		// Get the associated name from the dictionary
		var capsuleName = _capsuleNames.ContainsKey(anchor.Uuid) ? _capsuleNames[anchor.Uuid] : "Unknown Capsule";

		// Get the TextMeshPro component and update its text
		var textMeshPro = textObject.GetComponent<TextMeshPro>();
		if (textMeshPro != null)
		{
			textMeshPro.text = capsuleName;
			Debug.Log($"Displaying: {capsuleName}");
		}
		else
		{
			Debug.LogWarning("TextMeshPro component is missing on the text prefab.");
		}
	}


}