using UnityEngine;

public class SlideBarText : MonoBehaviour
{

    public void setText(float value)
    {
        gameObject.GetComponent<UnityEngine.UI.Text>().text = value.ToString();
    }

}
