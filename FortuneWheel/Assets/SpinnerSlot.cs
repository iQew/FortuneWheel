using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpinnerSlot : MonoBehaviour {

	public ItemType itemType { get; private set; }

	public Sprite coins100;
	public Sprite coins200;
	public Sprite coins500;
	public Sprite item01;
	public Sprite item02;
	public Sprite item03;

	private Image m_image;
	private RectTransform m_rectTransform;

	void Awake() {
		m_image = GetComponent<Image> ();
		m_rectTransform = GetComponent<RectTransform> ();
	}

	public void SetItemType(ItemType type) {
		itemType = type;
		switch (type) {
		case ItemType.COINS_100:
			m_image.sprite = coins100;
			break;
		case ItemType.COINS_200:
			m_image.sprite = coins200;
			break;
		case ItemType.COINS_500:
			m_image.sprite = coins500;
			break;
		case ItemType.ITEM_01:
			m_image.sprite = item01;
			break;
		case ItemType.ITEM_02:
			m_image.sprite = item02;
			break;
		case ItemType.ITEM_03:
			m_image.sprite = item03;
			break;
		}
	}

	public void SetPosition(Vector2 pos) {
		m_rectTransform.anchoredPosition = pos;
	}

	public void SetSize(Vector2 size) {
		m_rectTransform.sizeDelta = size;
	}

	public Vector2 GetPosition() {
		return m_rectTransform.anchoredPosition;
	}

}
