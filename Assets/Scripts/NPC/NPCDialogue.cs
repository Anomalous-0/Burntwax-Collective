using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    public string[] dialogueLines;
    public TMP_Text dialogueText;
    public GameObject npc;
    private int currentLineIndex = 1;
    InputManager input;

    void Start()
    {
        dialogueText.text = "";
    }

    void Update()
    {
        if (input.jumpIsPressed)
        {
            DisplayNextLine();
            input.jumpIsPressed = false;
        }
    }

    void DisplayNextLine()
    {
        if (currentLineIndex < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLineIndex];
            currentLineIndex++;
        }
        else
        {
            dialogueText.text = "";
            currentLineIndex = 0;
        }
    }

    public void SetupNPC(GameObject npc, string[] dialogueInput) {

        // Set up 
        this.npc = npc;
        dialogueText = npc.GetComponent<TMP_Text>();


        // Set up dialogues
        dialogueLines = new string[dialogueInput.Length];

        for (int i = 0; i < dialogueLines.Length; i++) {
            dialogueLines[i] = dialogueInput[i];
        }

        dialogueText.text = dialogueLines[0];


    }
}
