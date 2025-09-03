using UnityEngine;

public class DummySlotController : MonoBehaviour
{
    [SerializeField]
    private RollingNumberController rollingNumberController;
    private int fontIndex = 0;

    void Start()
    {
        rollingNumberController.Add(100000);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            rollingNumberController.Add(250000);
        }

        if(Input.GetKeyDown(KeyCode.O))
        {
            rollingNumberController.CycleNumberFont(fontIndex++);
            rollingNumberController.Add(100000);
        }
    }
}
