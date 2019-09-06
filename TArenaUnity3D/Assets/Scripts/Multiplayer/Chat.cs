using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{

int maxMessages = 50;
public GameObject chatPanel, textObject;
public Color Master,Client,Info;

    [SerializeField]
    public List<Msg> messageList = new List<Msg>();


    public static Chat  chat;

    private void OnEnable()
    {
      
        if (Chat.chat == null)
        {
            Chat.chat = this;
        }
        else
        {
            if (Chat.chat != this)
            { Destroy(this.gameObject); }

        }
       
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
   
    }

    public void SendMessageToChat(string text, Msg.MessageType messageType)
    {

if (messageList.Count >= maxMessages){
   Destroy(messageList[0].textObject.gameObject);
   messageList.Remove(messageList[0]);
}

        Msg newMessage = new Msg();
        newMessage.text = text;

        GameObject newText = Instantiate(textObject,chatPanel.transform);
        newMessage.textObject = newText.GetComponent<Text>();
        newMessage.textObject.text = newMessage.text;
        newMessage.textObject.color = MessageTypeColor(messageType);
        
        messageList.Add(newMessage);  
    }

    Color MessageTypeColor(Msg.MessageType messageType)
    {
        Color color = Info;
        switch(messageType)
        {
            case Msg.MessageType.Master:
            color = Master;
            break;

            case Msg.MessageType.Client:
            color = Client;
            break;

        }

        return color;
    }

}

[System.Serializable]
public class Msg
{
    public Text textObject;
    public string text;
    public MessageType messageType;

    public enum MessageType
    {
        Master,
        Client,
        Info

    }
}
