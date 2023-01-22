using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CookingManager : MonoBehaviour
{
	private const float BASE_TEMP_MULT = 0.01F;
	private float temperatureMultiplier;
	private float progressMultiplier = 0.03f;// 0.01f;

	public delegate void OnRecipeCompleteCallback();
	public OnRecipeCompleteCallback onRecipeComplete;

	[SerializeField] private GameObject gamePlayCanvas;
	[SerializeField] private Cauldron cauldron; //Script that will be on the cauldron go
	[SerializeField] private IngredientDisplay[] ingredientObjects; //This is where ingredients will be grabbed
	[SerializeField] private Slider thermometer, progressBar; //Indicator of current temperature and current progress
	[SerializeField] private RectTransform thermometerFillRect;
	[SerializeField] private Button decreaseTempButton, increaseTempButton; //For temperature adjustments
	[SerializeField] private Image thermometerGreenZone; //The fill for the thermometer
	[SerializeField] private TMP_Text dialogueText; //Text to display advice to player
	[Space]
	[SerializeField] private Ingredient[] ingredients; //Used to populate the empty IngredientObject blocks with non-included ingredients
	[SerializeField] private RectTransform[] ingredientMarkers; //Indicators for when ingredients should be added. Should be children of progress bar

    #region - Private Variables -
	private MenuManager menuManager;
	private AudioSource source;
    private Recipe recipe; //The recipe for the current level
	private RecipeStep nextStep; //the next step at which progress is halted until the correct ingredient is added
	private int currentStepIndex; //Index to mark the current step in the recipe

	private int temperatureDifference; //The modifier for the current temperature. Cannot equal zero
	private float currentTemperature; //the current temperature for the cauldron
	private float progress;
	private float timeSinceLastStir;

	private bool inRequiredTemperatureRange = false; //Is the currentTemperature within the bounds of the recipe
	private bool waitingToAddIngredient = false; //Has progress reached a step within the recipe
	private bool allIngredientsAdded = false; //Are all ingredients added
	private bool isCooking; //Set to true when a recipe is set and the game starts
	private bool wrongIngredientOverride; //bool to control dialogue text conflicts
    #endregion

    [Header("Animators")]
	[SerializeField] private Animator penguinAnim;
	[SerializeField] private Animator flameAnim;
	[SerializeField] private Animator transitionAnim;

	[Header("Audio")]
	[SerializeField] private AudioClip positive;
	[SerializeField] private AudioClip negative;
	[SerializeField] private AudioClip ingredientAdded;

	private void Start()
	{
		menuManager = GetComponent<MenuManager>();
		source = GetComponent<AudioSource>();
		decreaseTempButton.onClick.AddListener(OnTempDecrease);
		increaseTempButton.onClick.AddListener(OnTempIncrease);
		HideDialogue();
		for (int i = 0; i < ingredientMarkers.Length; i++)
        {
			ingredientMarkers[i].gameObject.SetActive(false);
			ingredientMarkers[i].transform.GetChild(1).gameObject.SetActive(false);
		}
	}

	private void Update()
	{
		if (isCooking)
		{
			HandleTemperature();
			HandleProgress();
		}
	}

    #region - Setup -
	public void SetRecipe(Recipe newRecipe)
    {
		recipe = newRecipe;
		progress = 0;
		progressBar.value = 0;
		currentStepIndex = 0;
		nextStep = recipe.steps[currentStepIndex];
		allIngredientsAdded = false;
		temperatureMultiplier = BASE_TEMP_MULT * (1 + menuManager.recipeIndex);
		currentTemperature = (recipe.minTemperatureRange + recipe.maxTemperatureRange) * 0.5f;
		temperatureDifference = -1;
		flameAnim.SetInteger("temp", temperatureDifference);

		for (int i = 0; i < ingredientMarkers.Length; i++)
		{
			ingredientMarkers[i].gameObject.SetActive(false);
			ingredientMarkers[i].transform.GetChild(1).gameObject.SetActive(false);
		}

		PlaceIngredients();
		PlaceIngredientMarkers();
		PlaceThermometerBounds();
		isCooking = true;

		gamePlayCanvas.SetActive(true);
	}

	private void PlaceIngredients()
	{
		var tempList = new List<Ingredient>();
		for (int i = 0; i < recipe.steps.Length; i++)
		{
			tempList.Add(recipe.steps[i].ingredient);
		}

		while (tempList.Count < ingredientObjects.Length)
		{
			for (int i = 0; i < ingredients.Length; i++)
			{
				if (!tempList.Contains(ingredients[i]))
				{
					tempList.Add(ingredients[i]);
					break;
				}
			}
		}
		
		for (int i = 0; i < ingredientObjects.Length; i++)
		{
			int index = Random.Range(0, tempList.Count);
			ingredientObjects[i].gameObject.SetActive(true);
			ingredientObjects[i].SetIngredient(tempList[index]);
			tempList.RemoveAt(index);
		}
	}

	private void PlaceIngredientMarkers()
	{
		var rect = progressBar.GetComponent<RectTransform>();
		var width = rect.sizeDelta.x;

		for (int i = 0; i < recipe.steps.Length; i++)
		{
			ingredientMarkers[i].gameObject.SetActive(true);
			ingredientMarkers[i].transform.GetChild(0).GetComponent<Image>().sprite = recipe.steps[i].ingredient.icon;

			float percent = recipe.steps[i].addAtPercentLowerBounds;
			var newPosition = width * percent;

			ingredientMarkers[i].anchoredPosition = 
				new Vector2(newPosition, ingredientMarkers[i].anchoredPosition.y);
		}
	}

	private void PlaceThermometerBounds()
	{
		var height = thermometerFillRect.sizeDelta.x;

		thermometerGreenZone.rectTransform.offsetMin = new Vector2(recipe.minTemperatureRange * height, 0);
		thermometerGreenZone.rectTransform.offsetMax = new Vector2(-(1 - recipe.maxTemperatureRange) * height, 0);
	}
    #endregion

	private void HandleTemperature()
	{
		currentTemperature += temperatureDifference * temperatureMultiplier * Time.deltaTime;
		currentTemperature = Mathf.Clamp(currentTemperature, 0f, 1f);
		inRequiredTemperatureRange = (currentTemperature >= recipe.minTemperatureRange && currentTemperature <= recipe.maxTemperatureRange);
		thermometer.value = currentTemperature;

		if (currentTemperature < recipe.minTemperatureRange && !wrongIngredientOverride) SetDialogue("Too Cold!");
		else if (currentTemperature > recipe.maxTemperatureRange && !wrongIngredientOverride) SetDialogue("Too Hot!");
		else if (!wrongIngredientOverride) HideDialogue();
	}

	private void HandleProgress()
	{
		penguinAnim.SetBool("isStirring", cauldron.isStirring);

		if (!inRequiredTemperatureRange) return;
		if (waitingToAddIngredient)
		{
			SetDialogue(recipe.steps[currentStepIndex].ingredient.name + "!");
			return;
		}
		else if (!HandleStirring()) return;

		progress += progressMultiplier * Time.deltaTime;
		progress = Mathf.Clamp(progress, 0f, 1f);
		progressBar.value = progress;

		if (progress >= 1) OnRecipeCompleted();

		if (allIngredientsAdded) return;
		if (progress >= nextStep.addAtPercentLowerBounds)
		{
			waitingToAddIngredient = true;
			ingredientMarkers[currentStepIndex].gameObject.transform.localScale = Vector2.one * 1.5f;
		}
	}

	private bool HandleStirring()
    {
		bool isStirring = cauldron.isStirring;

		if (isStirring) timeSinceLastStir = 0;
		else timeSinceLastStir += Time.deltaTime;

		if (timeSinceLastStir >= 0.1f && !wrongIngredientOverride) SetDialogue("Stir!");

		return isStirring;
    }

	public void OnAddIngredient(Ingredient ingredient)
	{
		source.PlayOneShot(ingredientAdded);
		if (!waitingToAddIngredient || ingredient != nextStep.ingredient)
        {
			OnWrongIngredient();
			return;
		}

		source.PlayOneShot(positive);
		waitingToAddIngredient = false;

		ingredientMarkers[currentStepIndex].gameObject.transform.localScale = Vector2.one;
		ingredientMarkers[currentStepIndex].transform.GetChild(1).gameObject.SetActive(true);

		currentStepIndex++;

		if (currentStepIndex >= recipe.steps.Length)
		{
			allIngredientsAdded = true;
			return;
		}

		nextStep = recipe.steps[currentStepIndex];
	}

	private void OnWrongIngredient()
	{
		source.PlayOneShot(negative);
		StartCoroutine(WrongIngredientWarning());
		penguinAnim.SetTrigger("mistake");
		progress -= 0.1f;

		if (progress < 0) progress = 0;
		else if (currentStepIndex > 0)
        {
			//This should prevent the progress from dipping below the last marker
			float f = recipe.steps[currentStepIndex - 1].addAtPercentLowerBounds;
			if (progress < f) progress = f;
        }

		waitingToAddIngredient = false;
	}

	public void OnRecipeCompleted()
	{
		penguinAnim.transform.position += Vector3.left;
		penguinAnim.SetTrigger("cheer");
		gamePlayCanvas.SetActive(false);
		isCooking = false;

		for (int i = 0; i < ingredientObjects.Length; i++)
		{
			ingredientObjects[i].gameObject.SetActive(false);
		}

		int unlockedLevels = PlayerPrefs.GetInt("UnlockedLevels");
		int level = menuManager.recipeIndex + 1;
		if (level > unlockedLevels) PlayerPrefs.SetInt("UnlockedLevels", level);

		menuManager.ReturnToMainMenu();
	}

	private void OnTempDecrease()
	{
		temperatureDifference--;
		if (temperatureDifference == 0) temperatureDifference = -1;
		else if (temperatureDifference < -5) temperatureDifference = -5;
		flameAnim.SetInteger("temp", temperatureDifference);
	}

	private void OnTempIncrease()
	{
		temperatureDifference++;
		if (temperatureDifference == 0) temperatureDifference = 1;
		else if (temperatureDifference > 5) temperatureDifference = 5;
		flameAnim.SetInteger("temp", temperatureDifference);
	}

	public void SetDialogue(string text)
    {
		dialogueText.transform.parent.gameObject.SetActive(true);
		dialogueText.text = text;
	}

	public void HideDialogue()
    {
		dialogueText.transform.parent.gameObject.SetActive(false);
	}

	private IEnumerator WrongIngredientWarning()
    {
		wrongIngredientOverride = true;
		SetDialogue("Wrong Ingredient!");

		yield return new WaitForSeconds(3);
		
		dialogueText.transform.parent.gameObject.SetActive(false);
		wrongIngredientOverride = false;
	}
}
