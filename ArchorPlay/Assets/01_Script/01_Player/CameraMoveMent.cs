using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CameraMovement>();
                if (instance == null)
                {
                    var instanceContainer = new GameObject("CameraMovement");
                    instance = instanceContainer.AddComponent<CameraMovement>();
                }
            }
            return instance;
        }
    }
    private static CameraMovement instance;

    public GameObject Player;

    public float offsetY = 45f;
    public float offsetZ = -40f;

    private Vector3 cameraPosition;

    void LateUpdate()
    {
        if (Player == null)
            return;
        cameraPosition = transform.position;

        cameraPosition.y = Player.transform.position.y + offsetY;
        cameraPosition.z = Player.transform.position.z + offsetZ;

        transform.position = cameraPosition;
    }

    public void CameraNextRoom()
    {
        if (Player == null)
            return;


        cameraPosition = transform.position;
        cameraPosition.x = Player.transform.position.x;

        transform.position = cameraPosition;
    }
}
