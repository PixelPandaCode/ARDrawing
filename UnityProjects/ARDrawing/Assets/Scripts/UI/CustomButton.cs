using MixedReality.Toolkit;
using StrokeMimicry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class CustomButton : StatefulInteractable
{
    public Color normalColor;
    public Color hoverColor;
    public Color pressColor;

    public GameObject receiver;
    public string message;

    // most action only act once
    private bool actioned = false;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.tag = "UI";
        gameObject.layer = LayerMask.NameToLayer("UI");
        // gameObject.GetComponent<Renderer>().material.color = normalColor;
        receiver = FindObjectOfType<InputManager>().gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Action()
    {
        if (receiver != null && !actioned)
        {
            receiver.gameObject.SendMessage(message);
            actioned = true;
        }
    }

    public void Hover()
    {
        // gameObject.GetComponent<Renderer>().material.color = hoverColor;
        actioned = false;
        HoverEnterEventArgs args = new HoverEnterEventArgs();
        hoverEntered.Invoke(args);
    }

    public void Press()
    {
        // gameObject.GetComponent<Renderer>().material.color = pressColor;
        Action();
    }

    public void Leave()
    {
        // gameObject.GetComponent <Renderer>().material.color = normalColor;
        actioned = false;
    }
}
