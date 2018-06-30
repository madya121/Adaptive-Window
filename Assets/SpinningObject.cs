using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinningObject : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		transform.Rotate(Vector3.up, 80 * Time.deltaTime);
    //if (transform.eulerAngles.y > 90)
    //  transform.eulerAngles = new Vector3(0, -90, 0);
	}
}
