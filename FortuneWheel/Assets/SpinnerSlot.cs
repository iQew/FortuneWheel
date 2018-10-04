using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpinnerSlot : MonoBehaviour {

	public ItemType itemType { get; private set; }
	public int index { get; set;}

	public ParticleSystem rewardFX;

	public Sprite coins100;
	public Sprite coins200;
	public Sprite coins500;
	public Sprite item01;
	public Sprite item02;
	public Sprite item03;

	private Image m_image;
	private RectTransform m_rectTransform;
	private bool m_showingReward;

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
		if (rewardFX != null) {
			var shape = rewardFX.shape;
			shape.scale =  new Vector3 (size.x, size.y, 0);
		}
	}

	public Vector2 GetPosition() {
		return m_rectTransform.anchoredPosition;
	}

	public void ShowRewardFX() {
		if (!m_showingReward) {
			m_showingReward = true;
			rewardFX.gameObject.SetActive (true);
		}
	}

	public void HideRewardFX() {
		if (m_showingReward) {
			m_showingReward = false;
			rewardFX.gameObject.SetActive (false);
		}
	}

}
