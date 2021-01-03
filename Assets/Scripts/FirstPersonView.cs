using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonView : MonoBehaviour
{
    public Controller MyController;
    public Head MyHead;
    public float SmoothFactor = 0.1f;
    private const float SmoothFactorDivisor = 1e6f;

    void FixedUpdate()
    {
        //On tourne la tête dans le sens de la vue
        float t = 1 - Mathf.Pow(SmoothFactor / SmoothFactorDivisor, Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, MyHead.transform.position, t);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(MyController.WantedDirectionLook), t);
    }
    
}
