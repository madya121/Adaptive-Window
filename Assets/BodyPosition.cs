using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BodyPosition : MonoBehaviour
{
    private long _lastFrameIndex = -1;

    private const int MaxBodyCount = 1;
    private Astra.Body[] _bodies;
    // private Dictionary<int, GameObject[]> _bodySkeletons;

    // public GameObject JointPrefab;
    
    public Vector3 head = Vector3.zero;
    
    public Text text;
    
    private Queue qPos = new Queue();
    private Vector3 tPos;
    private int maximumPoint = 30;

    void Start()
    {
        // _bodySkeletons = new Dictionary<int, GameObject[]>();
        _bodies = new Astra.Body[MaxBodyCount];
        head = Vector3.zero;
        tPos = new Vector3(0, 0, 0);
    }

    public void OnNewFrame(Astra.BodyStream bodyStream, Astra.BodyFrame frame)
    {
        if (frame.Width == 0 ||
            frame.Height == 0)
        {
            return;
        }

        if (_lastFrameIndex == frame.FrameIndex)
        {
            return;
        }

        _lastFrameIndex = frame.FrameIndex;

        frame.CopyBodyData(ref _bodies);
        UpdateSkeletonsFromBodies(_bodies);
    }

    void UpdateSkeletonsFromBodies(Astra.Body[] bodies)
    {
        foreach (var body in bodies)
        {
            Vector3 maks = Vector3.zero;
            for (int i = 0; i < body.Joints.Length; i++)
            {
                var bodyJoint = body.Joints[i];
                {
                    Vector3 position =
                        new Vector3(bodyJoint.WorldPosition.X / 1000f,
                                    bodyJoint.WorldPosition.Y / 1000f,
                                    bodyJoint.WorldPosition.Z / 1000f);

                    if (maks.y < position.y)
                        maks = position;
                }
            }
            
            if (maks == Vector3.zero) {
              head = Vector3.zero;
              continue;
            }
            
            if (qPos.Count < maximumPoint) {
              qPos.Enqueue(maks);
              tPos += maks;
            } else {
              Vector3 front = (Vector3)qPos.Dequeue();
              qPos.Enqueue(maks);
              tPos = tPos - front + maks;
            }
            
            head = tPos / (float)qPos.Count;
        }
    }

    public void SetText(Vector3 v) {
      if (text)
        text.text = "X: " + v.x + "\nY: " + v.y + "\nZ: " + v.z;
    }      
}
