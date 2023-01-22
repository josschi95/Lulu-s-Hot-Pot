[System.Serializable]
public class RecipeStep
{
	public Ingredient ingredient; //The ingredient to add
	public float addAtPercentLowerBounds; //The percentage of recipe completion at which this ingredient needs to be added
}