using UnityEngine;

public class IngredientDisplay : MonoBehaviour
{
	[SerializeField] private CookingManager cookingManager;
	[SerializeField] private Ingredient ingredient;
	private Vector3 startPos;
	private bool canDrag = true;
	private bool isBeingDragged;

	private void Start()
    {
		startPos = transform.position;

		cookingManager.onRecipeComplete += delegate
		{
			canDrag = false;
			gameObject.SetActive(false);
		};
	}

    public void SetIngredient(Ingredient ingredient)
	{
		this.ingredient = ingredient;
		GetComponent<SpriteRenderer>().sprite = ingredient.icon;
		canDrag = true;
	}

    public void OnMouseDrag()
    {
		if (!canDrag) return;
		isBeingDragged = true;
		var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		pos.z = 0;
		transform.position = pos;
	}

    public void OnMouseUp()
    {
        if (isBeingDragged)
        {
			isBeingDragged = false;

			//Check if hovering over cauldron
			Collider2D[] colls = Physics2D.OverlapBoxAll(transform.position, Vector2.one, 0);
            for (int i = 0; i < colls.Length; i++)
            {
				if (colls[i].gameObject.GetComponent<Cauldron>())
                {
					cookingManager.OnAddIngredient(ingredient);
				}
            }
			transform.position = startPos;
		}
    }
}
