using UnityEngine;

public class CameraScaler : MonoBehaviour {

	void Awake() {
		GetComponent<Camera> ().orthographicSize = Screen.height / 2f;
	}

}
