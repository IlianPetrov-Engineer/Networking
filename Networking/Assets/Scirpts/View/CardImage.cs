using UnityEngine;
using UnityEngine.UI;

public class CardImage : MonoBehaviour
{
    [SerializeField] private Sprite cardImage;
    private int cardId;

    public void SetCardImage(int cardId, Sprite cardImage)
    {
        this.cardImage = cardImage;
        this.cardId = cardId;
        Image image = gameObject.GetComponent<Image>();
        image.sprite = cardImage;
    }
}
