using UnityEngine;

class Scaler : MonoBehaviour
{
    public float Scale { get; set; }

    [SerializeField] Transform _target = null;

    void Update()
      => _target.localScale = new Vector3(1, Scale, 1);
}
