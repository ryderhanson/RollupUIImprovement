using UnityEngine;

public class DummySlotController : MonoBehaviour
{
    public RollingNumberController RollingNumberController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RollingNumberController.Add(100000);
    }

    int fontIndex = 0;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            RollingNumberController.Add(100000);
            RollingNumberController.CycleNumberFont(fontIndex++);
        }

        if(Input.GetKeyDown(KeyCode.O))
        {
            RollingNumberController.Add(-100000);
            RollingNumberController.CycleNumberFont(fontIndex++);
        }
    }
}
