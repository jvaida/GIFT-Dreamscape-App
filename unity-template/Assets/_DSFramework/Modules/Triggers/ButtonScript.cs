using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonScript : MonoBehaviour
{
    [SerializeField] private GameObject buttonCap, buttonBase;
    [SerializeField] private Color pressedColor, defaultColor;
    [SerializeField] private float pressedPosition;
    
    
    
    public UnityEvent onPressed, onReleased, onStay;

    private float defaultPosition;
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        _renderer = buttonCap.GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", defaultColor);
        _renderer.SetPropertyBlock(_propBlock);

        defaultPosition = buttonCap.transform.localPosition.y;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            onPressed.Invoke();
        }    
    }
    
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log(gameObject.name + " pressed.");
        
        onPressed.Invoke();
        
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", pressedColor);
        _renderer.SetPropertyBlock(_propBlock);
        
        buttonCap.transform.localPosition = new Vector3(buttonCap.transform.localPosition.x, pressedPosition, buttonCap.transform.localPosition.z);
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log(gameObject.name + " released.");
        
        onReleased.Invoke();
        
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", defaultColor);
        _renderer.SetPropertyBlock(_propBlock);
        
        buttonCap.transform.localPosition = new Vector3(buttonCap.transform.localPosition.x, defaultPosition, buttonCap.transform.localPosition.z);
    }

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log(gameObject.name + "stay.");
        onStay.Invoke();
    }
}
