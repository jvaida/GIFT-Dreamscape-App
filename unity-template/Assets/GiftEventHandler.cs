using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mil.Arl.Gift.Unity.Connectivity;
using Mil.Arl.Gift.Unity.Messaging.Incoming;
using UnityEngine.UI;

public class GiftEventHandler : MonoBehaviour
{
    public Text feedbackText;
    private AbstractGiftConnector connector = null;

    private void handleSimanMessage(Siman siman) {
    switch(siman.Siman_Type) {
        case SimanType.Load:
            connector.SendLoadedMessage();
            break;
        case SimanType.Start:
            connector.SendStartedMessage();
            break;
        case SimanType.Stop:
            connector.SendStoppedMessage();
            break;
        case SimanType.Pause:
            connector.SendPausedMessage();
            break;
        case SimanType.Resume:
            connector.SendResumedMessage();
            break;
        case SimanType.Restart:
            break;
        }
    }

    private void handleFeedbackMessage(string feedback) {
        feedbackText.text = feedback;
    }

    public void FeedbackButtonClick() {
        connector.SendSimpleExampleState("dreamscape button clicked. sending text to gift");
    }

    // Start is called before the first frame update
    void Start()
    {
        if(connector == null) {
            connector = GiftConnectorFactory.CreateGiftConnector(typeof(AbstractGiftConnector));
            connector.OnSimanReceived += handleSimanMessage;
            connector.OnFeedbackReceived += handleFeedbackMessage;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
