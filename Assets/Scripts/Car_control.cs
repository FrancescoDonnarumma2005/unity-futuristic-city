using UnityEngine;


public class Car_control : MonoBehaviour
{
    public GameObject car;
    int a;
    int b;

    Rigidbody rb;
    void Start()
    {
      rb = GetComponent<Rigidbody>();  
    }

    
    void Update()
    {
        car.transform.position += new Vector3(0, 0, 1) * 0.2f * Time.deltaTime;
       
    }
    
}
