using UnityEngine;

public class Cauldron : MonoBehaviour
{
	private bool canStir = true;
	public bool isStirring { get; private set; }
	Vector3 lastMousePosition;
    
	//Call this method when the penguin's mistake animation is playing
    public void ToggleCanStir(bool canStir)
	{
		this.canStir = canStir;
		if (!canStir) isStirring = false;
	}

    public void OnMouseDrag()
    {
		if (!canStir)
		{
			isStirring = false;
			return;
		}

		if (Input.mousePosition != lastMousePosition)
		{
			lastMousePosition = Input.mousePosition;
			isStirring = true;
		}
		else isStirring = false;
    }

    public void OnMouseUp()
    {
		isStirring = false;
	}
}
