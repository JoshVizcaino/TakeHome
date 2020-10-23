//Store the contents for widgets to display if using them as buttons.
using UnityEngine;

// The base class of the scroll content container. Create the individual ScrollBank by inheriting this class
public abstract class BaseScrollBank: MonoBehaviour
{
	public abstract string GetScrollContent(int index);
	public abstract int GetScrollLength();
}

// The example of the ScrollBank
public class ScrollBank : BaseScrollBank
{
	private int[] contents = {
		1, 2, 3, 4, 5, 6, 7, 8, 9, 10
	};

	public override string GetScrollContent(int index)
	{
		return contents[index].ToString();
	}

	public override int GetScrollLength()
	{
		return contents.Length;
	}
}
