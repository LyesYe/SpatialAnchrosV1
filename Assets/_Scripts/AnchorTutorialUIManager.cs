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
		ProcessAnchorOnLoad(anchor);
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

            // Perform actions on the closest anchor
            Instantiate(_closestRingPrefab, _closestAnchor.GetComponent<Transform>());
            Debug.Log("ON LOAD: CLOSEST ANCHOR Found");
        }
    }






}