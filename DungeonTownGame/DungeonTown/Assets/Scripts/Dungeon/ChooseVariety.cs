using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseVariety : MonoBehaviour
{

    [Serializable]
    public struct variety
    {
        [SerializeField]
        public float chance;
        [SerializeField]
        public GameObject obj;
    }

    [SerializeField]
    public variety[] objectVariety;

    // Start is called before the first frame update
    void Start()
    {

        if (objectVariety == null || objectVariety.Length == 0) return;

        float total = 0;
        foreach (variety v in objectVariety)
        {
            total += v.chance;
        }

        float rng = UnityEngine.Random.Range(0, total);

        total = 0;
        foreach (variety v in objectVariety)
        {
            total += v.chance;
            if(rng < total)
            {
                GameObject obj = Instantiate(v.obj, this.transform.position, this.transform.rotation);
                obj.gameObject.transform.parent = this.transform.parent;
                Destroy(this.gameObject);
                return;
            }
        }

    }
}
