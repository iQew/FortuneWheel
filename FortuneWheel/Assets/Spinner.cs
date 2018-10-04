using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Security.Cryptography;

public class Spinner : MonoBehaviour {

	public bool showDebug;
	public int targetFPS;
	public Button spinStopButton;

	// a higher speed than 5000f is not recommended, because at a certain point, the elements move too fast for spawning/dying (e.g. m_maxSpeed = 10000f)
	private const float m_maxSpeed = 5000f;

	// value at which point the human-eye cannot detect any visible speed difference of the slot elements
	private const float BRAKING_SPEED_THRESHOLD = 5f;

	// the reward pool size directly influences the available decimal precision for the probabilities of each single item
	// a size of 100 does not allow any decimals for probability
	// a size of 200 allows only decimals to be .0 or .5
	// a size higher than 200 does not make sense, because the shuffle functionality only works for lists with size <= 255
	private const int REWARD_POOL_SIZE = 100;

	// Reward chances (0 - 1.0)
	private const float COINS_100_CHANCE = 0.58f;
	private const float COINS_200_CHANCE = 0.22f;
	private const float COINS_500_CHANCE = 0.1f;

	private const float ITEM_01_CHANCE = 0.05f;
	private const float ITEM_02_CHANCE = 0.04f;
	private const float ITEM_03_CHANCE = 0.01f;

	public GameObject slotPrefab;
	private SpinnerSlot m_spinnerSlotCache;
	private GameObject m_gameObjectCache;
	private List<SpinnerSlot> m_spinnerSlots = new List<SpinnerSlot>();
	private List<ItemType> m_rewardPool = new List<ItemType> ();

	private int m_spinnerSlotsCount;
	private float m_spinnerSlotWidth;
	private float m_spinnerSlotHeight;

	private SpinnerSlot m_leftMostSpinnerSlot;

	private Vector2 m_deathPosition;
	private Vector2 m_spawnPosition;
	private Vector2 m_positionCache;

	private float m_predeterminedDistance;
	private float m_currentSpeed;
	private bool m_spinning;
	private bool m_braking;

	private ItemType m_itemTypeCache;

	private SpinnerSlot m_winningSlot;

	// for debugging
	private int m_coins100, m_coins200, m_coins500, m_item01, m_item02, m_item03, m_items;

	void Awake() {
		Application.targetFrameRate = targetFPS;
		spinStopButton.onClick.AddListener (Spin);
		CreateRewardPool ();
		CreateSlots ();

	}

	void FixedUpdate() {
		if (m_spinning) {
			if (!m_braking && iTween.Count (gameObject) == 0) {
				// spinner finished start animation and is now at desired speed
				// state of the button is handled
				spinStopButton.onClick.RemoveListener (Spin);
				spinStopButton.GetComponentInChildren<Text>().text = "Stop";
				spinStopButton.onClick.AddListener (Brake);
				spinStopButton.interactable = true;
			}
			// moving all spinner slots synchronized
			for (int i = 0; i < m_spinnerSlotsCount; i++) {
				m_spinnerSlotCache = m_spinnerSlots [i];

				m_positionCache = m_spinnerSlotCache.GetPosition();
				m_positionCache.x -= m_currentSpeed * Time.fixedDeltaTime;
				m_spinnerSlotCache.SetPosition(m_positionCache);
			}
			// when a spinner has reached the spot marked for death, it will be respawned at the right-most end of the spinner slots
			// no fixed spawn position is computed, because the offset between the right-most spinner and the spawn point
			// varies depending on the current speed of the spinner
			if (m_leftMostSpinnerSlot.GetPosition ().x  <= m_deathPosition.x) {
				int leftMostSlotIndex = m_spinnerSlots.IndexOf (m_leftMostSpinnerSlot);
				int indexOfNextSlot = leftMostSlotIndex + 1;
				if (indexOfNextSlot == m_spinnerSlotsCount) {
					indexOfNextSlot = 0;
				}

				m_leftMostSpinnerSlot.SetPosition (new Vector2(GetLastSpinnerSlot().GetPosition ().x + m_spinnerSlotWidth, 0));
				m_leftMostSpinnerSlot.SetItemType (GetNextRandomReward());
				m_leftMostSpinnerSlot = m_spinnerSlots [indexOfNextSlot];
			}
			// when the spinner is slowed down to a certain point at which the movement
			// is not visible to the human eye, it will stop interpolating between values
			if (m_braking && m_currentSpeed <= BRAKING_SPEED_THRESHOLD) {
				m_spinning = false;
				m_braking = false;
				iTween.Stop (gameObject);
				m_winningSlot = FindCenterSlot ();
				m_winningSlot.ShowRewardFX ();

				m_currentSpeed = 0;

				// state of the button is handled
				spinStopButton.onClick.RemoveListener (Brake);
				spinStopButton.GetComponentInChildren<Text>().text = "Spin";
				spinStopButton.onClick.AddListener (Spin);
				spinStopButton.interactable = true;
			}
		}
	}

	/**
	 * Returns the spinner slot, which is furthest to the right.
	 * **/
	private SpinnerSlot GetLastSpinnerSlot() {
		int leftMostSlotIndex = m_spinnerSlots.IndexOf (m_leftMostSpinnerSlot);

		int indexOfLastSlot = leftMostSlotIndex - 1;

		if (indexOfLastSlot == -1) {
			indexOfLastSlot = m_spinnerSlotsCount - 1;
		}
		return m_spinnerSlots [indexOfLastSlot];
	}

	/**
	 * Finds the slot which is closest to the center point of the spinner.
	 * **/
	private SpinnerSlot FindCenterSlot() {
		float smallestDelta = int.MaxValue;
		int winningIndex = int.MaxValue;
		for (int i = 0; i < m_spinnerSlotsCount; i++) {
			if (Mathf.Abs (m_spinnerSlots [i].GetPosition ().x) < smallestDelta) {
				smallestDelta = Mathf.Abs (m_spinnerSlots [i].GetPosition ().x);
				winningIndex = i;
			}
		}
		return m_spinnerSlots [winningIndex];
	}

	/**
	 * Creating a pool of possible rewards. The amount of items in the list directly represents the probability of the item.
	 * **/
	private void CreateRewardPool() {
		// saving values as floats first and casting to int later to prevent errors due to float rounding 6.2f * 10 = 61.9999..8
		float amountCoins100 = REWARD_POOL_SIZE * COINS_100_CHANCE;
		float amountCoins200 = REWARD_POOL_SIZE * COINS_200_CHANCE;
		float amountCoins500 = REWARD_POOL_SIZE * COINS_500_CHANCE;

		float amountItem01 = REWARD_POOL_SIZE * ITEM_01_CHANCE;
		float amountItem02 = REWARD_POOL_SIZE * ITEM_02_CHANCE;
		float amountItem03 = REWARD_POOL_SIZE * ITEM_03_CHANCE;

		for (int i = 0; i < (int)amountCoins100; i++) {
			m_rewardPool.Add (ItemType.COINS_100);
		}
		for (int i = 0; i < (int)amountCoins200; i++) {
			m_rewardPool.Add (ItemType.COINS_200);
		}
		for (int i = 0; i < (int)amountCoins500; i++) {
			m_rewardPool.Add (ItemType.COINS_500);
		}
		for (int i = 0; i < (int)amountItem01; i++) {
			m_rewardPool.Add (ItemType.ITEM_01);
		}
		for (int i = 0; i < (int)amountItem02; i++) {
			m_rewardPool.Add (ItemType.ITEM_02);
		}
		for (int i = 0; i < (int)amountItem03; i++) {
			m_rewardPool.Add (ItemType.ITEM_03);
		}
	}

	/**
	 * Fills the screen with slots and adds more slots at both ends in order to create
	 * the illusion of an endless stream of slots. The left-hand side has more slots,
	 * because the start animation of the spinner first moves the slots to the right
	 * before spinning at the desired speed to the left.
	 * **/
	private void CreateSlots() {
		m_spinnerSlotsCount = (int)(Screen.width / (Screen.height / 4f)) + 5; // adding enough slots to fill the screen on both sides
		m_spinnerSlotWidth = (Screen.height / 4f);
		m_spinnerSlotHeight = m_spinnerSlotWidth;
		m_deathPosition = new Vector2 (((Screen.width / 2f * -1) - 3 * m_spinnerSlotWidth), 0); // higher offset to the left for the start of the spinning animation
		m_spawnPosition = new Vector2 (((Screen.width / 2f * -1) - 3 * m_spinnerSlotWidth) + m_spinnerSlotsCount * m_spinnerSlotWidth, 0);
		for (int i = 0; i < m_spinnerSlotsCount; i++) {
			m_gameObjectCache = Instantiate (slotPrefab);
			m_spinnerSlotCache = m_gameObjectCache.GetComponent<SpinnerSlot> ();
			m_spinnerSlotCache.name = "SpinnerSlot_" + i;
			m_spinnerSlotCache.transform.SetParent (transform);


			m_spinnerSlotCache.SetItemType (GetNextRandomReward());
			m_spinnerSlotCache.index = i;

			m_spinnerSlotCache.SetSize (new Vector2 (m_spinnerSlotWidth, m_spinnerSlotHeight));
			m_spinnerSlotCache.SetPosition (new Vector2 (m_deathPosition.x + i * m_spinnerSlotWidth, 0));

			m_spinnerSlots.Add (m_spinnerSlotCache);
		}

		m_leftMostSpinnerSlot = m_spinnerSlots [0];
	}

	/**
	 * Shuffles the list of rewards and return the value at index 0, which will thus be a randomly chosen reward.
	 * **/
	private ItemType GetNextRandomReward() {
		ExtensionClasses.Shuffle (m_rewardPool);
		m_itemTypeCache = m_rewardPool [0];
		switch (m_itemTypeCache) {
		case ItemType.COINS_100:
			m_coins100++;
			break;
		case ItemType.COINS_200:
			m_coins200++;
			break;
		case ItemType.COINS_500:
			m_coins500++;
			break;
		case ItemType.ITEM_01:
			m_item01++;
			break;
		case ItemType.ITEM_02:
			m_item02++;
			break;
		case ItemType.ITEM_03:
			m_item03++;
			break;
		}
		m_items++;
		if (showDebug) {
			Debug.Log (
				"COINS100: " + m_coins100 + " (" + Math.Round(m_coins100/(float)m_items * 100f, 2) + "%) " + 
				"COINS200: " + m_coins200 + " (" + Math.Round(m_coins200/(float)m_items * 100f, 2) + "%) " +
				"COINS500: " + m_coins500 + " (" + Math.Round(m_coins500/(float)m_items * 100f, 2) + "%) " +
				"ITEM01: " + m_item01 + " (" + Math.Round(m_item01/(float)m_items * 100f, 2) + "%) " +
				"ITEM02: " + m_item02 + " (" + Math.Round(m_item02/(float)m_items * 100f, 2) + "%) " +
				"ITEM03: " + m_item03 + " (" + Math.Round(m_item03/(float)m_items * 100f, 2) + "%) ");
		}
		return m_itemTypeCache;
	}

	public void Spin() {
		if (!m_braking && !m_spinning) {
			m_spinning = true;
			spinStopButton.interactable = false;
			iTween.ValueTo (gameObject, iTween.Hash(
				"from", 0,
				"to", m_maxSpeed,
				"time", 1.5f,
				"onupdate", "OnSpeedUpdate",
				"easetype", iTween.EaseType.easeInBack));

			if (m_winningSlot != null) {
				m_winningSlot.HideRewardFX ();
			}
		}
	}

	public void Brake() {
		if (m_spinning && !m_braking) {
			m_braking = true;
			spinStopButton.interactable = false;
			iTween.ValueTo (gameObject, iTween.Hash(
				"from", m_currentSpeed,
				"to", 0,
				"time", 8f,
				"onupdate", "OnSpeedUpdate",
				"easetype", iTween.EaseType.easeOutQuart));
		}
	}

	private void OnSpeedUpdate(float value) {
		m_currentSpeed = value;
	}
}
