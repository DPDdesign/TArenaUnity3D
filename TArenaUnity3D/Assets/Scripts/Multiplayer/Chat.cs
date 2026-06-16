using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Chat : MonoBehaviour
{

int maxMessages = 50;
public GameObject chatPanel;
public TMP_Text textObject;
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
        AddMessageToChat(text, MessageTypeColor(messageType));
    }

    public void SendSkillUseMessage(TosterHexUnit caster, string skillName)
    {
        if (caster == null)
        {
            SendMessageToChat(skillName, Msg.MessageType.Info);
            return;
        }

        string text = UnitText(caster) + " " + SystemText("użył skilla") + " " + TeamText(skillName, caster) + SystemText(".");
        AddMessageToChat(text, Info);
    }

    public void SendDamageMessage(TosterHexUnit attacker, int damage, TosterHexUnit defender)
    {
        string text =
            UnitText(attacker) +
            SystemText(" zadał " + damage + " obrażeń ") +
            UnitText(defender);

        AddMessageToChat(text, Info);
    }

    public void SendUnitLossMessage(TosterHexUnit unit, int amountLost)
    {
        string text = UnitText(unit) + SystemText(" stracił " + amountLost + " jednostek");
        AddMessageToChat(text, Info);
    }

    public void SendUnitActionMessage(TosterHexUnit unit, string actionText)
    {
        string text = UnitText(unit) + SystemText(" " + actionText);
        AddMessageToChat(text, Info);
    }

    public void SendUnitTextMessage(TosterHexUnit unit, string text)
    {
        string richText = SystemText(text);
        if (unit != null && string.IsNullOrEmpty(unit.Name) == false)
        {
            richText = richText.Replace(EscapeRichText(unit.Name), UnitText(unit));
        }

        AddMessageToChat(richText, Info);
    }

    public void SendTargetedSkillMessage(TosterHexUnit caster, string skillName, TosterHexUnit target)
    {
        string text =
            UnitText(caster) +
            SystemText(" rzucił ") +
            TeamText(skillName, caster) +
            SystemText(" na ") +
            UnitText(target);

        AddMessageToChat(text, Info);
    }

    public void SendTrapTriggeredMessage(TosterHexUnit unit, string trapName, TosterHexUnit trapOwner)
    {
        TosterHexUnit colorSource = trapOwner != null ? trapOwner : unit;
        string text = UnitText(unit) + SystemText(" wszedł w ") + TeamText(trapName, colorSource);
        AddMessageToChat(text, Info);
    }

    void AddMessageToChat(string text, Color baseColor)
    {
        if (chatPanel == null)
        {
            Debug.LogWarning("Chat message skipped because chatPanel is not assigned.");
            return;
        }

        if (messageList.Count >= maxMessages)
        {
            if (messageList[0].textObject != null)
            {
                Destroy(messageList[0].textObject.gameObject);
            }

            messageList.Remove(messageList[0]);
        }

        Msg newMessage = new Msg();
        newMessage.text = text;

        TMP_Text newText = CreateChatText();
        newMessage.textObject = newText;
        newMessage.textObject.richText = true;
        newMessage.textObject.text = newMessage.text;
        newMessage.textObject.color = baseColor;
        
        messageList.Add(newMessage);  
    }

    TMP_Text CreateChatText()
    {
        if (textObject != null)
        {
            return Instantiate(textObject, chatPanel.transform);
        }

        GameObject messageObject = new GameObject("Chat Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        messageObject.transform.SetParent(chatPanel.transform, false);

        TMP_Text messageText = messageObject.GetComponent<TMP_Text>();
        messageText.raycastTarget = false;
        return messageText;
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

    string UnitText(TosterHexUnit unit)
    {
        if (unit == null)
        {
            return SystemText("Unknown");
        }

        return TeamText(unit.Name, unit);
    }

    string TeamText(string text, TosterHexUnit unit)
    {
        Msg.MessageType messageType = unit != null && unit.teamN ? Msg.MessageType.Master : Msg.MessageType.Client;
        return ColorText(text, MessageTypeColor(messageType));
    }

    string SystemText(string text)
    {
        return EscapeRichText(text);
    }

    string ColorText(string text, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + EscapeRichText(text) + "</color>";
    }

    string EscapeRichText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

}

[System.Serializable]
public class Msg
{
    public TMP_Text textObject;
    public string text;
    public MessageType messageType;

    public enum MessageType
    {
        Master,
        Client,
        Info

    }
}
