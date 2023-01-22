using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Scriptable Objects/Recipe")]
public class Recipe : ScriptableObject
{
	new public string name;
	public Sprite icon;
	[Space]
	public float minTemperatureRange;
	public float maxTemperatureRange;

	public RecipeStep[] steps;
}