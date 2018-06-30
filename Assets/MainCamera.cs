using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class MainCamera : MonoBehaviour {
  private float leftWindow = -8.0f;
  private float rightWindow = 8.0f;
  private float topWindow = 4.5f;
  private float bottomWindow = -4.5f;
  private float unityWindowWidth = 16.0f;
  
  [Header("Window Setting")]
  public float windowWidth = 16.0f;
  
  [Header("Body Position Script")]
  public BodyPosition pos;
  
  [Header("Offset Translation")]
  public Vector3 translation;
  
  [Header("Offset Rotation")]
  public Vector3 eulerAngles;
  
  void Awake() {
    
  }

  void LateUpdate() {
    float realDistance = 2000.0f;

    Camera cam = Camera.main;
    cam.farClipPlane = realDistance;

    float windowDistance = -transform.position.z;

    float top     = (topWindow - transform.position.y) / windowDistance;
    float bottom  = (bottomWindow - transform.position.y) / windowDistance;
    float left    = (leftWindow - transform.position.x) / windowDistance;
    float right   = (rightWindow - transform.position.x) / windowDistance;

    Matrix4x4 m = FrustumMatrix(left, right, bottom, top, cam.nearClipPlane, cam.farClipPlane);
    cam.projectionMatrix = m;
  }
  
  void Update() {
    // return;
    float ratio = unityWindowWidth / windowWidth;
    
    Quaternion rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
    Matrix4x4 transformationMatrix = Matrix4x4.TRS(translation, rotation, Vector3.one);
    Vector3 add = new Vector3(pos.head.x, pos.head.y, pos.head.z);
    if (add == Vector3.zero)
      add = new Vector3(0, 0, -10);
    else {
      add = transformationMatrix.MultiplyPoint3x4(add);
      /*add.x *= 7.0f;
      add.y = (add.y * 2.0f);// - 5f;
      add.z *= -7.0f;*/
      add.x *= ratio;
      add.y *= ratio;// - 5f;
      add.z *= -ratio;
    }
    
    RenderSettings.fogStartDistance = -add.z + 5;
    RenderSettings.fogEndDistance = -add.z + 15;
    
    pos.SetText(add);
    transform.position = Vector3.Lerp(transform.position, add, 0.2f);
    return;
  }

  static Matrix4x4 FrustumMatrix(float l, float r, float b, float t, float n, float f) {
    float Xc = 2.0f * n / (r - l);
    float Yc = 2.0f * n / (t - b);
    float A = (r + l) / (r - l);
    float B = (t + b) / (t - b);
    float C = -(f + n) / (f - n);
    float D = -(2.0f * f * n) / (f - n);

    Matrix4x4 m = new Matrix4x4();

    m[0, 0] = Xc; m[0, 1] = 0;  m[0, 2] = A;    m[0, 3] = 0;
    m[1, 0] = 0;  m[1, 1] = Yc; m[1, 2] = B;    m[1, 3] = 0;
    m[2, 0] = 0;  m[2, 1] = 0;  m[2, 2] = C;    m[2, 3] = D;
    m[3, 0] = 0;  m[3, 1] = 0;  m[3, 2] = -1f;  m[3, 3] = 0;

    return m;
  }
}