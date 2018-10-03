using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Spinner : MonoBehaviour {

	public int targetFPS;
	public Button spinStopButton;

	public float speed;

	// Reward chances (0 - 1.0)
	private const double COINS_CHANCE = 0.90;
	private const double ITEM_CHANCE = 1 - COINS_CHANCE;

	private const double COINS_100_CHANCE = 0.585;
	private const double COINS_200_CHANCE = 0.225;
	private const double COINS_500_CHANCE = 0.09;

	private const double ITEM_01_CHANCE = 0.0475;
	private const double ITEM_02_CHANCE = 0.0475;
	private const double ITEM_03_CHANCE = 0.005;

	private const int REWARD_POOL_SIZE = 1000;

	public GameObject slotPrefab;
	private SpinnerSlot m_spinnerSlotCache;
	private GameObject m_gameObjectCache;
	private List<SpinnerSlot> m_spinnerSlots = new List<SpinnerSlot>();
	private List<ItemType> m_rewardPool = new List<ItemType> ();

	private int m_spinnerSlotsCount;
	private float m_spinnerSlotWidth;
	private float m_spinnerSlotHeight;

	private SpinnerSlot m_leftMostSpinnerSlot;

	private Vector2 m_targetDestination;
	private Vector2 m_positionCache;

	private float m_currentSpeed;
	private bool m_spinning;
	private bool m_braking;

	private int m_rewardIndex = 0;
	private ItemType m_itemTypeCache;

	void Awake() {
		Application.targetFrameRate = targetFPS;
		spinStopButton.onClick.AddListener (Spin);
		CreateRewardPool ();
		CreateSlots ();
	}

	void FixedUpdate() {
		if (m_spinning) {
			for (int i = 0; i < m_spinnerSlotsCount; i++) {
				m_spinnerSlotCache = m_spinnerSlots [i];

				m_positionCache = m_spinnerSlotCache.GetPosition();
				m_positionCache.x -= m_currentSpeed * Time.fixedDeltaTime;
				m_spinnerSlotCache.SetPosition(m_positionCache);
			}
			if (m_leftMostSpinnerSlot.GetPosition ().x <= m_targetDestination.x) {
				int leftMostSlotIndex = m_spinnerSlots.IndexOf (m_leftMostSpinnerSlot);

				int indexOfLastSlot = leftMostSlotIndex - 1;
				int indexOfNextSlot = leftMostSlotIndex + 1;

				if (indexOfLastSlot == -1) {
					indexOfLastSlot = m_spinnerSlotsCount - 1;
				}
				if (indexOfNextSlot == m_spinnerSlotsCount) {
					indexOfNextSlot = 0;
				}

				m_leftMostSpinnerSlot.SetPosition (new Vector2(m_spinnerSlots [indexOfLastSlot].GetPosition ().x + m_spinnerSlotWidth, 0));
				m_leftMostSpinnerSlot.SetItemType (GetNextRandomReward());
				m_leftMostSpinnerSlot = m_spinnerSlots [indexOfNextSlot];
			}

			if (m_braking && m_currentSpeed <= 5f) {
				iTween.Stop ();
				m_spinning = false;
				m_braking = false;

				Debug.Log ("Winner: " + FindWinnerSlot().name + "!");
				m_currentSpeed = 0;
				spinStopButton.onClick.RemoveListener (Brake);
				spinStopButton.GetComponentInChildren<Text>().text = "Spin";
				spinStopButton.onClick.AddListener (Spin);


			}
		}
	}

	private SpinnerSlot FindWinnerSlot() {
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

	private void CreateRewardPool() {
		int amountCoins100 = (int)(REWARD_POOL_SIZE * COINS_100_CHANCE);
		int amountCoins200 = (int)(REWARD_POOL_SIZE * COINS_200_CHANCE);
		int amountCoins500 = (int)(REWARD_POOL_SIZE * COINS_500_CHANCE);

		int amountItem01 = (int)(REWARD_POOL_SIZE * ITEM_01_CHANCE);
		int amountItem02 = (int)(REWARD_POOL_SIZE * ITEM_02_CHANCE);
		int amountItem03 = (int)(REWARD_POOL_SIZE * ITEM_03_CHANCE);
		System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch ();
		watch.Start ();
		for (int i = 0; i < amountCoins100; i++) {
			m_rewardPool.Add (ItemType.COINS_100);
		}
		Debug.Log ("Created coins100...");
		for (int i = 0; i < amountCoins200; i++) {
			m_rewardPool.Add (ItemType.COINS_200);
		}
		Debug.Log ("Created coins200...");
		for (int i = 0; i < amountCoins500; i++) {
			m_rewardPool.Add (ItemType.COINS_500);
		}
		Debug.Log ("Created coins500...");
		for (int i = 0; i < amountItem01; i++) {
			m_rewardPool.Add (ItemType.ITEM_01);
		}
		Debug.Log ("Created item01...");
		for (int i = 0; i < amountItem02; i++) {
			m_rewardPool.Add (ItemType.ITEM_02);
		}
		Debug.Log ("Created item02...");
		for (int i = 0; i < amountItem03; i++) {
			m_rewardPool.Add (ItemType.ITEM_03);
		}
		Debug.Log ("Created item03...");

		watch.Stop ();
		Debug.Log ("reward pool completed in " + watch.ElapsedMilliseconds + "ms");
		watch.Start ();
		//ExtensionClasses.Shuffle (m_rewardPool);
		watch.Stop ();
		Debug.Log ("shuffle completed in " + watch.ElapsedMilliseconds + "ms");
	}

	private void CreateSlots() {
		m_spinnerSlotsCount = (int)(Screen.width / (Screen.height / 4f)) + 5; // adding enough slots to fill the screen on both sides
		m_spinnerSlotWidth = (Screen.height / 4f);
		m_spinnerSlotHeight = m_spinnerSlotWidth;
		m_targetDestination = new Vector2 (((Screen.width / 2f * -1) - 3 * m_spinnerSlotWidth), 0); // higher offset to the left for the start of the spinning animation

		for (int i = 0; i < m_spinnerSlotsCount; i++) {
			m_gameObjectCache = Instantiate (slotPrefab);
			m_spinnerSlotCache = m_gameObjectCache.GetComponent<SpinnerSlot> ();
			m_spinnerSlotCache.name = "SpinnerSlot_" + i;
			m_spinnerSlotCache.transform.SetParent (transform);


			m_spinnerSlotCache.SetItemType (GetNextRandomReward());

			m_spinnerSlotCache.SetSize (new Vector2 (m_spinnerSlotWidth, m_spinnerSlotHeight));
			m_spinnerSlotCache.SetPosition (new Vector2 (m_targetDestination.x + i * m_spinnerSlotWidth, 0));

			m_spinnerSlots.Add (m_spinnerSlotCache);
		}

		m_leftMostSpinnerSlot = m_spinnerSlots [0];
	}

	private ItemType GetNextRandomReward() {
		Debug.Log ("reward pool size: " + m_rewardPool.Count + " reward index: " + m_rewardIndex);
		m_itemTypeCache = m_rewardPool [m_rewardIndex];
		m_rewardIndex++;
		if (m_rewardIndex == REWARD_POOL_SIZE) {
			m_rewardIndex = 0;
			//ExtensionClasses.Shuffle (m_rewardPool);
		}
		return m_itemTypeCache;
	}

	public void Spin() {
		if (!m_braking) {
			m_spinning = true;
			spinStopButton.onClick.RemoveListener (Spin);
			spinStopButton.GetComponentInChildren<Text>().text = "Stop";
			spinStopButton.onClick.AddListener (Brake);

			iTween.ValueTo (gameObject, iTween.Hash(
				"from", 0,
				"to", speed,
				"time", 1.5f,
				"onupdate", "OnSpeedUpdate",
				"easetype", iTween.EaseType.easeInBack));
		}
	}

	public void Brake() {
		if (m_spinning) {
			m_braking = true;
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
