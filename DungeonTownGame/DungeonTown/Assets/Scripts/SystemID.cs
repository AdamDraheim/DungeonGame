using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemID : MonoBehaviour
{

    private Dictionary<string, int> idSystem;
    public static SystemID system;

    // Start is called before the first frame update
    void Awake()
    {
        if(system == null)
        {
            system = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

        this.idSystem = new Dictionary<string, int>();

    }

    public int GenerateNewID(string id_name)
    {
        if (!idSystem.ContainsKey(id_name)) { idSystem.Add(id_name, 0); return 0; }

        idSystem[id_name]++;
        return idSystem[id_name];
    }
}
