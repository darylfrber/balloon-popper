using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class NeedleVisual : MonoBehaviour
{
    void Awake(){ var lr = GetComponent<LineRenderer>(); lr.positionCount=2; lr.SetPosition(0, Vector3.zero); lr.SetPosition(1, Vector3.up*0.5f); lr.startWidth=0.05f; lr.endWidth=0.01f; lr.material = new Material(Shader.Find("Sprites/Default")); lr.startColor=Color.yellow; lr.endColor=Color.red; }
}
