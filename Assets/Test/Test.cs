using System.Linq;
using UnityEngine;
using UnityEngine.UI;

class Test : MonoBehaviour
{
    [SerializeField] Text _label = null;

    void Update()
    {
        _label.text = Lasp.DeviceManager.InputDevices.
            Select(dev => $"{dev.ID} | {dev.Name}").
            Aggregate(string.Empty, (a, b) => a + "\n" + b);
    }
}
