using UnityEngine;

public class BattleManager : MonoBehaviour
{

    public static BattleManager Instance {get; private set;} // 다른 스크립트에서 접근하기 위한 임시 싱글톤
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
