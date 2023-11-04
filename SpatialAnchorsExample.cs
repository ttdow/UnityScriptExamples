using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

// Include the Spatial anchors packages in MRTK for these.
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;

public class SpatialAnchorsExample : MonoBehaviour
{
    // Need to have a Spatial Anchor Manager script on an object instantiated in the scene.
    // This script is a part of the MRTK spatial anchors package.
    // It tracks your API key, region info, etc. for the Azure Spatial Anchors Service.
    SpatialAnchorManager spatialAnchorManager; 

    // A cube object representing the spatial anchor.
    GameObject anchorGameObject;

    public void Start()
    {
        // Get reference to the spatial anchor manager in the scene.
        spatialAnchorManager = GetComponent<SpatialAnchorManager>();

        // Define the callback function when an anchor is located.
        spatialAnchorManager.AnchorLocated += SpatialAnchorManager_AnchorLocated;
    }

    // ---------------------------------- Load Existing Anchor ------------------------------------

    // Callback function for when a spatial anchor is located.
    private void SpatialAnchorManager_AnchorLocated(object sender, AnchorLocatedEventArgs args)
    {
        if (args.Status == LocateAnchorStatus.Located)
        {
            Debug.Log("Anchor was successfully located.");
            CreateAnchorGameObject(args);
        }
        else if (args.Status == LocateAnchorStatus.AlreadyTracked)
        {
            Debug.Log("Anchor was rediscovered successfully.");
            CreateAnchorGameObject(args);
        }
        else
        {
            Debug.Log("Failed to located anchor: " + args.Status);
        }
    }

    private void CreateAnchorGameObject(AnchorLocatedEventArgs args)
    {
        // Need to call this on main Unity app thread.
        UnityDispatcher.InvokeOnAppThread(() =>
        {
            CloudSpatialAnchor cloudSpatialAnchor = args.Anchor;

            this.anchorGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anchorGameObject.transform.localScale = Vector3.one * 0.05f;
            anchorGameObject.GetComponent<MeshRenderer>().material.shader = Shader.Find("Standard");
            anchorGameObject.GetComponent<MeshRenderer>().material.color = Color.blue;

            anchorGameObject.AddComponent<CloudNativeAnchor>().CloudToNative(cloudSpatialAnchor);

            // Change anchor game object to green once it's saved successfully.
            anchorGameObject.GetComponent<MeshRenderer>().material.color = Color.green;
        });
    }

    // Call this function to locate the anchors in anchorId in the scene using
    // the HL2 spatial awarness data.
    public async void LocateAnchor(List<string> anchorIds)
    {
            // Start an Azure Spatial Anchors session if it doesn't exist.
            if (!spatialAnchorManager.IsSessionStarted)
            {
                await spatialAnchorManager.StartSessionAsync();
            }

            // Create criteria for anchor location.
            AnchorLocateCriteria anchorLocateCriteria = new AnchorLocateCriteria();
            anchorLocateCriteria.Identifiers = anchorIds.ToArray();

            if (spatialAnchorManager.Session.GetActiveWatchers().Count > 0)
            {
                Debug.Log("Spatial anchor watcher already exists.");
            }

            // Create watcher to locate anchors with given criteria.
            spatialAnchorManager.Session.CreateWatcher(anchorLocateCriteria);
    }

    // ----------------------------------- Create New Anchor --------------------------------------

    private async Task CreateAnchor(Vector3 position, Quaternion rotation)
    {
        // Create a local game object to represent the spatial anchor.
        this.anchorGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        anchorGameObject.transform.localScale = Vector3.one;
        anchorGameObject.GetComponent<MeshRenderer>().material.shader = Shader.Find("Standard");
        anchorGameObject.GetComponent<MeshRenderer>().material.color = Color.blue;
        anchorGameObject.transform.position = position;
        anchorGameObject.transform.rotation = rotation;

        // Attach the spatial anchor to the game object.
        CloudNativeAnchor cloudNativeAnchor = anchorGameObject.AddComponent<CloudNativeAnchor>();
        await cloudNativeAnchor.NativeToCloud();
        CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;
        cloudSpatialAnchor.Expiration = DateTimeOffset.Now.AddDays(10); // Keep the anchor alive in the cloud for 10 days.

        // Collect spatial data for the anchor.
        while (!spatialAnchorManager.IsReadyForCreate)
        {
            float createProgress = spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
            Debug.Log($"Move your device to capture more environment data: {createProgress:0%}");
        }

        // Create the anchor in the cloud.
        try
        {
            await spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);

            bool saveSucceeded = cloudSpatialAnchor != null;
            if (!saveSucceeded)
            {
                Debug.Log("Failed to save spatial anchor to the cloud, but no exception was thrown.");
                return;
            }

            Debug.Log($"Saved anchor to cloud with ID: {cloudSpatialAnchor.Identifier}");

            // Change anchor game object to green once it's saved successfully.
            anchorGameObject.GetComponent<MeshRenderer>().material.color = Color.green;
        }
        catch (Exception exception)
        {
            Debug.Log("Failed to save anchor to cloud: " + exception.ToString());
            Debug.LogException(exception);
        }
    }
    
    // Call this function to create a new anchor on the cloud and in your scene.
    public async void StartCreateAnchor()
    {
        // Start a Azure Spatial Anchors session if not already started.
        if (!spatialAnchorManager.IsSessionStarted)
        {
            await spatialAnchorManager.StartSessionAsync();
        }

        // Get the direction the user is currently facing.
        Vector3 headDirection = Camera.main.transform.forward;

        // Create a spatial anchor with vec3 world positon and quaternion rotation.
        await CreateAnchor(Vector3.zero, Quaternion.identity);
    }
}
