using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSwitch : MonoBehaviour
{
    MeshRenderer mesh;
    [SerializeField]
    private Material switchMat;

    Material originalMat;
    bool hit;

    void Awake()
    {
        mesh = GetComponent<MeshRenderer>();
        originalMat = mesh.material;
    }

    // Update is called once per frame
    void Update()
    {
        //if(hit) return;
        //mesh.material = originalMat;
    }

    void Change()
    {
        Material currentMat = mesh.material;
        if(currentMat == originalMat)
        {
            mesh.material = switchMat;
        }
        else
        {
            mesh.material = originalMat;
        }

        //hit = true;
    }


}
