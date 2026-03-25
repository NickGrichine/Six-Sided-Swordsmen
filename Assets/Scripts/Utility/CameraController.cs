using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    [SerializeField] private float panSpeed;
    [SerializeField] private float zoomSpeed;
    [SerializeField] private float minZoom;
    [SerializeField] private float maxZoom;


    private float minX = int.MinValue;
    private float maxX = int.MaxValue;
    private float minY = int.MinValue;
    private float maxY = int.MaxValue;

    private const float edgeThreshold = 50f; // pixels from edge in screen space to enable panning


    [SerializeField] private new Camera camera;




    private void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    private void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        Vector3 mousePos = Input.mousePosition;
        Vector3 newPosition = transform.position;

        if (mousePos.x <= edgeThreshold)
        {
            movement.x = -1;
        }
        else if (mousePos.x >= Screen.width - edgeThreshold)
        {
            movement.x = 1;
        }

        if (mousePos.y <= edgeThreshold)
        {
            movement.y = -1;
        }
        else if (mousePos.y >= Screen.height - edgeThreshold)
        {
            movement.y = 1;
        }

        // apply movement if panning
        if (movement != Vector3.zero)
        {
            newPosition += movement.normalized * panSpeed * Time.deltaTime;
            // apply bounds
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
            
            transform.position = newPosition;
        }

    }

    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            camera.orthographicSize =
            Mathf.Clamp(
                camera.orthographicSize - scrollInput * zoomSpeed, 
                minZoom, 
                maxZoom);
        }
    }




    
    // public methods for looking at positions
    public void SetPosition(Vector2 position)
    {
        transform.position = 
        new Vector3(
            Mathf.Clamp(position.x, minX, maxX), 
            Mathf.Clamp(position.y, minY, maxY), 
            transform.position.z);
    }

    public void SetZoom(float zoom)
    {
        camera.orthographicSize = Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    // initialize bounds based on map size
    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
    }
}
