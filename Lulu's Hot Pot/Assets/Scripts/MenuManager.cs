using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private Cauldron cauldron;
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject stirringHint;
    [SerializeField] private Animator transitionAnim;
    [SerializeField] private Animator penguinAnim;
    [SerializeField] private Button[] levelSelectionButtons;
    [SerializeField] private Recipe[] recipes;
    [SerializeField] private Sprite[] scoreSprites;

    private CookingManager cookingManager;
    private bool levelSelected = false;
    public int recipeIndex { get; private set; }
    private Vector3 defaultPos;

    private void Start()
    {
        PlayerPrefs.DeleteAll();
        HideScore();
        defaultPos = penguinAnim.transform.position;
        cookingManager = GetComponent<CookingManager>();
        stirringHint.SetActive(false);
        stirringHint.GetComponentInChildren<Animator>().speed = 0.5f;
        for (int i = 0; i < levelSelectionButtons.Length; i++)
        {
            int level = i;
            levelSelectionButtons[level].onClick.AddListener(delegate { OnLevelSelected(level); });
        }

        StartCoroutine(DialoguePopupDelay());
    }

    private IEnumerator DialoguePopupDelay()
    {
        yield return new WaitForSeconds(2f);

        cookingManager.SetDialogue("Begin Stirring!");
        stirringHint.SetActive(true);

        while (!cauldron.isStirring) yield return null;

        OnShowLevels();
    }

    private void OnShowLevels()
    {
        penguinAnim.SetBool("isStirring", true);
        cookingManager.HideDialogue();
        stirringHint.SetActive(false);
        ShowLevelButtons();
    }

    private void OnLevelSelected(int level)
    {
        if (levelSelected == true) return;

        levelSelected = true;
        recipeIndex = level;
        transitionAnim.SetTrigger("play");

        StartCoroutine(LoadLevelDelay());
    }

    private IEnumerator LoadLevelDelay()
    {   
        yield return new WaitForSeconds(1f);

        cookingManager.SetRecipe(recipes[recipeIndex]);
        penguinAnim.SetBool("isStirring", false);
        menuCanvas.SetActive(false);
        levelSelected = false;
    }

    private IEnumerator LoadMenuDelay()
    {
        yield return new WaitForSeconds(5f);
        transitionAnim.SetTrigger("play");

        yield return new WaitForSeconds(1f);
        for (int i = 0; i < levelSelectionButtons.Length; i++)
        {
            levelSelectionButtons[i].GetComponent<Animator>().SetTrigger("hide");
        }

        HideScore();
        penguinAnim.transform.position = defaultPos;
        penguinAnim.Play("penguin_idle");
        penguinAnim.SetBool("isStirring", true);
        menuCanvas.SetActive(true);
        levelSelected = false;

        yield return new WaitForSeconds(1f);
        ShowLevelButtons();
    }

    private void ShowLevelButtons()
    {
        int unlockedLevels = PlayerPrefs.GetInt("UnlockedLevels", 0);
        for (int i = 0; i < unlockedLevels + 1; i++)
        {
            if (i > recipes.Length - 1) break;
            levelSelectionButtons[i].GetComponent<Animator>().SetTrigger("play");
            int score = PlayerPrefs.GetInt("Score" + i, 0);
            levelSelectionButtons[i].transform.GetChild(1).GetComponent<Image>().sprite = scoreSprites[score];
        }
    }

    public void ReturnToMainMenu()
    {       
        StartCoroutine(LoadMenuDelay());
    }

    public void DisplayScore(int score)
    {
        penguinAnim.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = scoreSprites[score];
        penguinAnim.transform.GetChild(0).gameObject.SetActive(true);
    }

    private void HideScore()
    {
        penguinAnim.transform.GetChild(0).gameObject.SetActive(false);
    }


}
