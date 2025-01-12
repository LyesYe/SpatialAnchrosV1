using System;
using System.Collections.Generic;
using System.Linq;
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
    private Transform _headTransform; // Reference to the VR user's head transform (center eye)

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

	[SerializeField]
	private GameObject _closestRingPrefab;

    [SerializeField] 
	private Material _closestCapsuleMaterial;

	[SerializeField]
	private TextMeshProUGUI _redCapsuleCountText; // UI text for red capsules
	[SerializeField]
	private TextMeshProUGUI _greenCapsuleCountText; // UI text for green capsules


	[SerializeField]
	private UnityEngine.UI.Toggle loadAnchorsButton; // Button to load all anchors

	[SerializeField]
	private UnityEngine.UI.Toggle destroyAnchorsButton; // Button to destroy all runtime anchors

	[SerializeField]
	private UnityEngine.UI.Toggle eraseAnchorsButton; // Button to erase all saved anchors

	public int _redCapsuleCount = 0; // Track the number of red capsules
	public int _greenCapsuleCount = 0; // Track the number of green capsules

	// Reference to the default materials for saved and non-saved capsules
	[SerializeField] private Material _savedCapsuleMaterial;
    [SerializeField] private Material _nonSavedCapsuleMaterial;
    // Keep track of the previous closest anchor to restore its material
    private OVRSpatialAnchor _previousClosestAnchor = null;


    private Dictionary<Guid, string> _capsuleNames = new();  // Maps UUID to Capsule N name
	private int _capsuleCount = 0; // Increment to create unique names
    private int _fartherCapsulesCount = 0; // Count of capsules farther than 3 meters
    private OVRSpatialAnchor _farthestCapsule = null; // The farthest anchor (capsule)
    private OVRSpatialAnchor _closestAnchor = null; // The closest anchor (capsule)
    private float _maxDistance = 0f; // The distance of the farthest anchor
    private int _localizedAnchorCount = 0; // Track how many anchors have been localized




    private List<OVRSpatialAnchor> _anchorInstances = new(); // Active instances (red and green)

	public HashSet<Guid> _anchorUuids = new(); // Simulated external location, like PlayerPrefs

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


		if (loadAnchorsButton != null)
		{
			loadAnchorsButton.onValueChanged.AddListener(OnLoadAnchorsButtonPressed);
		}
		if (destroyAnchorsButton != null)
		{
			destroyAnchorsButton.onValueChanged.AddListener(OnDestroyAnchorsButtonPressed);
		}
		if (eraseAnchorsButton != null)
		{
			eraseAnchorsButton.onValueChanged.AddListener(OnEraseAnchorsButtonPressed);
		}
	}
	private void Start()
	{
		SaveAnchorsWhenQuit.Instance.LoadAnchorsFromPlayerPrefs();

		
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
		CheckFartherAnchors();
		UpdateClosestAnchorMaterial();

        if (OVRInput.GetDown(OVRInput.Button.Three)) // Create a green capsule USING BUTTON a
		{
			// Create a green (savable) spatial anchor
			var go = Instantiate(_saveableAnchorPrefab, _saveableTransform.position, _saveableTransform.rotation); // Anchor A
			SetupAnchorAsync(go.AddComponent<OVRSpatialAnchor>(), saveAnchor: true);
		}
		else if (OVRInput.GetDown(OVRInput.Button.One)) // Create a red capsule USING BUTTON x
		{
			// Create a red (non-savable) spatial anchor.
			var go = Instantiate(_nonSaveableAnchorPrefab, _nonSaveableTransform.position, _nonSaveableTransform.rotation); // Anchor b
			SetupAnchorAsync(go.AddComponent<OVRSpatialAnchor>(), saveAnchor: false);
		}
		//else if (OVRInput.GetDown(OVRInput.Button.One)) // a button
		//{
		//	LoadAllAnchors();
		//}
		//else if (OVRInput.GetDown(OVRInput.Button.Three)) // x button
		//{
		//	// Destroy all anchors from the scene, but don't erase them from storage
		//	foreach (var anchor in _anchorInstances)
		//	{
		//		Destroy(anchor.gameObject);
		//	}

		//	_greenCapsuleCount = 0; // Reset the green capsule count

		//	_redCapsuleCount = 0; // Reset the red capsule count

		//	UpdateCapsuleCountUI();

		//	// Clear the list of running anchors
		//	_anchorInstances.Clear();
		//}
		//else if (OVRInput.GetDown(OVRInput.Button.Four)) // y button
		//{
		//	EraseAllAnchors();
		//}
		else if (OVRInput.GetDown(OVRInput.Button.Two))
		{
                SceneManager.LoadScene("Scene 2");
        }
		if(_anchorUuids.Count > 0)
		{
			Debug.Log("Anchor UUIDs: ");
			foreach (var uuid in _anchorUuids)
			{
				Debug.Log(uuid.ToString());
			}
		}
	}



	public void OnLoadAnchorsButtonPressed(bool isPressed)
	{
		if (isPressed)
		{
			LoadAllAnchors();

			// Reset the button state (optional)
			loadAnchorsButton.isOn = false;
		}
	}

	public void OnDestroyAnchorsButtonPressed(bool isPressed)
	{
		if (isPressed)
		{
			// Destroy all anchors from the scene
			for (int i = _anchorInstances.Count - 1; i >= 0; i--)
			{
				if (_anchorInstances[i] != null)
				{
					Destroy(_anchorInstances[i].gameObject); // Destroy the GameObject
				}
			}

			// Clear the list of active anchors
			_anchorInstances.Clear();

			// Reset the capsule counts
			_greenCapsuleCount = 0;
			_redCapsuleCount = 0;
			UpdateCapsuleCountUI();

			// Reset the button state (optional)
			destroyAnchorsButton.isOn = false;

			Debug.Log("All anchor instances destroyed from the scene.");
		}
	}

	public void OnEraseAnchorsButtonPressed(bool isPressed)
	{
		if (isPressed)
		{
			EraseAllAnchors();

			// Reset the button state (optional)
			eraseAnchorsButton.isOn = false;
		}
	}


	private void UpdateCapsuleCountUI()
	{
		if (_redCapsuleCountText != null)
		{
			_redCapsuleCountText.text = $"{_redCapsuleCount}";
		}
		if (_greenCapsuleCountText != null)
		{
			_greenCapsuleCountText.text = $"{_greenCapsuleCount}";
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
			_greenCapsuleCount++; // Increment green capsule count
			UpdateCapsuleCountUI(); // Update the UI
		}
		else
		{
			_redCapsuleCount++; // Increment red capsule count
			UpdateCapsuleCountUI(); // Update the UI
		}
	}

	/******************* Load Anchor Methods **********************/
	public async void LoadAllAnchors()
	{
		// Destroy all existing anchors before loading new ones
		for (int i = _anchorInstances.Count - 1; i >= 0; i--)
		{
			if (_anchorInstances[i] != null)
			{
				Destroy(_anchorInstances[i].gameObject);
			}
		}
		_anchorInstances.Clear();

		// Reset the localized anchor count
		_localizedAnchorCount = 0;

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
		if (!success)
		{
			Debug.LogError("Failed to localize anchor.");
			return;
		}

		// Check if an anchor with the same UUID already exists
		if (_anchorInstances.Any(anchor => anchor.Uuid == unboundAnchor.Uuid))
		{
			Debug.LogWarning($"Anchor with UUID {unboundAnchor.Uuid} is already bound. Skipping binding.");
			return;
		}

		var pose = unboundAnchor.Pose;
		var go = Instantiate(_saveableAnchorPrefab, pose.position, pose.rotation);
		var anchor = go.AddComponent<OVRSpatialAnchor>();

		unboundAnchor.BindTo(anchor);

		// Add the anchor to the active list
		_anchorInstances.Add(anchor);

		// Ensure the name is displayed based on the UUID
		DisplayAnchorName(anchor);

		// Check if the anchor is saved (green capsule)
		if (_anchorUuids.Contains(anchor.Uuid))
		{
			_greenCapsuleCount++; // Increment green capsule count
			UpdateCapsuleCountUI(); // Update the UI
		}

		// Process the anchor to update the closest anchor reference
		ProcessAnchorOnLoad(anchor);

		// Notify that an anchor has been localized
		OnAnchorLocalized();
	}

	// Called for each anchor when it is localized
	private void OnAnchorLocalized()
    {
        _localizedAnchorCount++;
    // If all anchors have been localized, execute the desired action
        if (_localizedAnchorCount == _anchorInstances.Count)
        {
            OnAllAnchorsLocalized();
        }
    }

	// Called after all anchors have been localized
	private void OnAllAnchorsLocalized()
	{
		// Update the material of the closest anchor
		UpdateClosestAnchorMaterial();

		// Perform additional actions on the closest anchor (e.g., instantiate a ring)
		if (_closestAnchor != null)
		{
			var go = Instantiate(_closestRingPrefab, _closestAnchor.GetComponent<Transform>().GetChild(1));
			go.transform.localRotation = Quaternion.identity;
		}
	}

	/******************* Erase Anchor Methods *****************/
	// If the Y button is pressed, erase all anchors saved
	// in the headset, but don't destroy them. They should remain displayed.
	public async void EraseAllAnchors()
	{
		// Convert the HashSet of UUIDs to a list for easier batching
		List<Guid> uuidsToErase = _anchorUuids.ToList();

		// Define the maximum number of anchors to erase in one batch
		const int batchSize = 32;

		// Process the UUIDs in batches
		for (int i = 0; i < uuidsToErase.Count; i += batchSize)
		{
			// Get the current batch of UUIDs
			var batch = uuidsToErase.Skip(i).Take(batchSize).ToList();

			// Erase the current batch of anchors
			var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: batch);
			if (!result.Success)
			{
				Debug.LogError($"Failed to erase anchors in batch {i / batchSize + 1}: {result.Status}");
				return; // Stop if any batch fails
			}

			Debug.Log($"Successfully erased batch {i / batchSize + 1} of anchors.");
		}

		// Clear the anchor UUIDs list after all batches are processed
		_anchorUuids.Clear();


		// Optionally, clear the saved anchors from PlayerPrefs
		PlayerPrefs.DeleteKey("SavedAnchors");
		PlayerPrefs.Save();

		Debug.Log("All anchors erased and removed from memory.");
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
    // This method checks for anchors farther than 3 meters from the VR user's head position
    private void CheckFartherAnchors()
    {
        if (_headTransform == null) return;

        _fartherCapsulesCount = 0; // Reset the count each time this is called
        _farthestCapsule = null; // Reset the farthest capsule
        _maxDistance = 0f; // Reset the max distance

        foreach (var anchor in _anchorInstances)
        {
            // Calculate the distance between the VR user's head position and the anchor's position
            float distance = Vector3.Distance(_headTransform.position, anchor.transform.position);

            if (distance > 3f)
            {
                _fartherCapsulesCount++; // Increment the count of farther capsules

                // Check if this is the farthest capsule found so far
                if (distance > _maxDistance)
                {
                    _maxDistance = distance; // Update the max distance
                    _farthestCapsule = anchor; // Set this anchor as the farthest one
                }
            }
        }
    }

    // This method changes the material of the closest anchor and restores the previous one
    private void UpdateClosestAnchorMaterial()
    {
        // Ensure there's a head transform to calculate the distance
        if (_headTransform == null || _anchorInstances.Count == 0) return;
		Debug.Log("LOOKING FOR THE CLOSEST ANCHOR");
        // Find the closest anchor by comparing distances
        OVRSpatialAnchor closestAnchor = null;
        float minDistance = float.MaxValue; // Start with a very large number

        foreach (var anchor in _anchorInstances)
        {
            float distance = Vector3.Distance(_headTransform.position, anchor.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestAnchor = anchor;
            }
        }
		Debug.Log(closestAnchor);

        // Check if the closest anchor is valid
        if (closestAnchor != null)
        {
            // Change the material of the closest anchor to the highlight material
            Renderer closestRenderer = closestAnchor.transform.GetChild(0).GetComponent<Renderer>();
			
            if (closestRenderer != null && _closestCapsuleMaterial != null)
            {
				Debug.Log("render is not null");
                closestRenderer.material = _closestCapsuleMaterial;
            }
        }
		else
		{
			Debug.Log("CLOSEST ANCHOR NOT BEING CALCULATED");
		}

        // If there was a previous closest anchor, restore its material based on whether it's saved or not
        if (_previousClosestAnchor != null && _previousClosestAnchor != closestAnchor)
        {
            Renderer previousRenderer = _previousClosestAnchor.transform.GetChild(0).GetComponent<Renderer>();
            if (previousRenderer != null)
            {
                // Check if the previous anchor is saved by checking if its UUID is in the _anchorUuids HashSet
                if (_anchorUuids.Contains(_previousClosestAnchor.Uuid))
                {
                    previousRenderer.material = _savedCapsuleMaterial;
                }
                else
                {
                    previousRenderer.material = _nonSavedCapsuleMaterial;
                }
            }
        }

        // Update the reference to the previous closest anchor
        _previousClosestAnchor = closestAnchor;
    }
    private void ProcessAnchorOnLoad(OVRSpatialAnchor anchor)
    {
        // Ensure that the head transform and anchor instances exist
        if (_headTransform == null || anchor == null) return;

        // Calculate the distance from the head transform to the given anchor
        float distance = Vector3.Distance(_headTransform.position, anchor.transform.position);

        // If this anchor is closer than the previously found closest anchor, update the closest anchor
        if (_closestAnchor == null || distance < Vector3.Distance(_headTransform.position, _closestAnchor.transform.position))
        {
            _closestAnchor = anchor;
        }
    }






}