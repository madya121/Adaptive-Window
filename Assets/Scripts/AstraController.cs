#if UNITY_ANDROID && !UNITY_EDITOR
#define ASTRA_UNITY_ANDROID_NATIVE
#endif

using Astra;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class NewDepthFrameEvent : UnityEvent<DepthFrame> { }

[System.Serializable]
public class NewColorFrameEvent : UnityEvent<ColorFrame> { }

[System.Serializable]
public class NewBodyFrameEvent : UnityEvent<BodyStream, BodyFrame> { }

[System.Serializable]
public class NewMaskedColorFrameEvent : UnityEvent<MaskedColorFrame> { }

[System.Serializable]
public class NewColorizedBodyFrameEvent : UnityEvent<ColorizedBodyFrame> { }

[System.Serializable]
public class NewBodyMaskEvent : UnityEvent<BodyMask> { }

public class AstraController : MonoBehaviour
{
    public bool AutoRequestAndroidUsbPermission = true;

    private Astra.StreamSet _streamSet;
    private Astra.StreamReader _reader1;
    private Astra.StreamReader _reader2;
    private Astra.StreamReader _reader3;
    private Astra.StreamReader _reader4;
    private Astra.StreamReader _reader5;

    private DepthStream _depthStream;
    private ColorStream _colorStream;
    private BodyStream _bodyStream;
    private MaskedColorStream _maskedColorStream;
    private ColorizedBodyStream _colorizedBodyStream;

    private long _lastBodyFrameIndex = -1;
    private long _lastDepthFrameIndex = -1;
    private long _lastColorFrameIndex = -1;
    private long _lastMaskedColorFrameIndex = -1;
    private long _lastColorizedBodyFrameIndex = -1;

    private int _lastWidth = 0;
    private int _lastHeight = 0;
    private short[] _buffer;
    private int _frameCount = 0;
    private bool _areStreamsInitialized = false;

    private bool _frameReadyDirty = false;

    private TimerHistory frameReadyTime = new TimerHistory();
    private TimerHistory astraUpdateTime = new TimerHistory();
    private TimerHistory totalFrameTime = new TimerHistory();

    public Text TimeText = null;

    public NewDepthFrameEvent NewDepthFrameEvent = new NewDepthFrameEvent();
    public NewColorFrameEvent NewColorFrameEvent = new NewColorFrameEvent();
    public NewBodyFrameEvent NewBodyFrameEvent = new NewBodyFrameEvent();
    public NewMaskedColorFrameEvent NewMaskedColorFrameEvent = new NewMaskedColorFrameEvent();
    public NewColorizedBodyFrameEvent NewColorizedBodyFrameEvent = new NewColorizedBodyFrameEvent();
    public NewBodyMaskEvent NewBodyMaskEvent = new NewBodyMaskEvent();

    public Toggle ToggleDepth = null;
    public Toggle ToggleColor = null;
    public Toggle ToggleBody = null;
    public Toggle ToggleMaskedColor = null;
    public Toggle ToggleColorizedBody = null;

    private void Awake()
    {
        Debug.Log("AstraUnityContext.Awake");
        AstraUnityContext.Instance.Initializing += OnAstraInitializing;
        AstraUnityContext.Instance.Terminating += OnAstraTerminating;
        AstraUnityContext.Instance.Initialize();
    }

    void Start()
    {
        if (TimeText != null)
        {
            TimeText.text = "";
        }
    }

    private void OnAstraInitializing(object sender, AstraInitializingEventArgs e)
    {
        Debug.Log("AstraController is initializing");

#if ASTRA_UNITY_ANDROID_NATIVE
        if (AutoRequestAndroidUsbPermission)
        {
            Debug.Log("Auto-requesting usb device access.");
            AstraUnityContext.Instance.RequestUsbDeviceAccessFromAndroid();
        }
#endif
        InitializeStreams();
    }

    private void InitializeStreams()
    {
        try
        {
            _streamSet = Astra.StreamSet.Open();
            _reader1 = _streamSet.CreateReader();
            _reader1.FrameReady += FrameReady;

            _reader2 = _streamSet.CreateReader();
            _reader2.FrameReady += FrameReady;

            _reader3 = _streamSet.CreateReader();
            _reader3.FrameReady += FrameReady;

            _reader4 = _streamSet.CreateReader();
            _reader4.FrameReady += FrameReady;

            _reader5 = _streamSet.CreateReader();
            _reader5.FrameReady += FrameReady;

            _depthStream = _reader1.GetStream<DepthStream>();

            var depthModes = _depthStream.AvailableModes;
            ImageMode selectedDepthMode = depthModes[0];

    #if ASTRA_UNITY_ANDROID_NATIVE
            int targetDepthWidth = 160;
            int targetDepthHeight = 120;
            int targetDepthFps = 30;
    #else
            int targetDepthWidth = 320;
            int targetDepthHeight = 240;
            int targetDepthFps = 60;
    #endif

            foreach (var m in depthModes)
            {
                if (m.Width == targetDepthWidth &&
                    m.Height == targetDepthHeight &&
                    m.FramesPerSecond == targetDepthFps)
                {
                    selectedDepthMode = m;
                    break;
                }
            }

            _depthStream.SetMode(selectedDepthMode);

            _colorStream = _reader2.GetStream<ColorStream>();

            var colorModes = _colorStream.AvailableModes;
            ImageMode selectedColorMode = colorModes[0];

    #if ASTRA_UNITY_ANDROID_NATIVE
            int targetColorWidth = 320;
            int targetColorHeight = 240;
            int targetColorFps = 30;
    #else
            int targetColorWidth = 640;
            int targetColorHeight = 480;
            int targetColorFps = 60;
    #endif

            foreach (var m in colorModes)
            {
                if (m.Width == targetColorWidth &&
                    m.Height == targetColorHeight &&
                    m.FramesPerSecond == targetColorFps)
                {
                    selectedColorMode = m;
                    break;
                }
            }

            _colorStream.SetMode(selectedColorMode);

            _bodyStream = _reader3.GetStream<BodyStream>();

            _maskedColorStream = _reader4.GetStream<MaskedColorStream>();

            _colorizedBodyStream = _reader5.GetStream<ColorizedBodyStream>();

            _areStreamsInitialized = true;
        }
        catch (AstraException e)
        {
            Debug.Log("AstraController: Couldn't initialize streams: " + e.ToString());
            UninitializeStreams();
        }
    }

    private void OnAstraTerminating(object sender, AstraTerminatingEventArgs e)
    {
        Debug.Log("AstraController is tearing down");
        UninitializeStreams();
    }

    private void UninitializeStreams()
    {
        Debug.Log("AstraController: Uninitializing streams");
        if (_reader1 != null)
        {
            _reader1.FrameReady -= FrameReady;
            _reader2.FrameReady -= FrameReady;
            _reader3.FrameReady -= FrameReady;
            _reader4.FrameReady -= FrameReady;
            _reader5.FrameReady -= FrameReady;
            _reader1.Dispose();
            _reader2.Dispose();
            _reader3.Dispose();
            _reader4.Dispose();
            _reader5.Dispose();
            _reader1 = null;
            _reader2 = null;
            _reader3 = null;
            _reader4 = null;
            _reader5 = null;
        }

        if (_streamSet != null)
        {
            _streamSet.Dispose();
            _streamSet = null;
        }
    }

    private void FrameReady(object sender, FrameReadyEventArgs e)
    {
        frameReadyTime.Start();

        //Debug.Log("FrameReady " + _frameCount);
        DepthFrame depthFrame = e.Frame.GetFrame<DepthFrame>();

        if (depthFrame != null)
        {
            if(_lastDepthFrameIndex != depthFrame.FrameIndex)
            {
                _lastDepthFrameIndex = depthFrame.FrameIndex;

                NewDepthFrameEvent.Invoke(depthFrame);
            }
        }

        ColorFrame colorFrame = e.Frame.GetFrame<ColorFrame>();

        if (colorFrame != null)
        {
            if (_lastColorFrameIndex != colorFrame.FrameIndex)
            {
                _lastColorFrameIndex = colorFrame.FrameIndex;

                NewColorFrameEvent.Invoke(colorFrame);
            }
        }

        BodyFrame bodyFrame = e.Frame.GetFrame<BodyFrame>();

        if(bodyFrame != null)
        {
            if (_lastBodyFrameIndex != bodyFrame.FrameIndex)
            {
                _lastBodyFrameIndex = bodyFrame.FrameIndex;

                NewBodyFrameEvent.Invoke(_bodyStream, bodyFrame);
                NewBodyMaskEvent.Invoke(bodyFrame.BodyMask);
            }
        }

        MaskedColorFrame maskedColorFrame = e.Frame.GetFrame<MaskedColorFrame>();

        if (maskedColorFrame != null)
        {
            if (_lastMaskedColorFrameIndex != maskedColorFrame.FrameIndex)
            {
                _lastMaskedColorFrameIndex = maskedColorFrame.FrameIndex;

                NewMaskedColorFrameEvent.Invoke(maskedColorFrame);
            }
        }

        ColorizedBodyFrame colorizedBodyFrame = e.Frame.GetFrame<ColorizedBodyFrame>();

        if (colorizedBodyFrame != null)
        {
            if (_lastColorizedBodyFrameIndex != colorizedBodyFrame.FrameIndex)
            {
                _lastColorizedBodyFrameIndex = colorizedBodyFrame.FrameIndex;

                NewColorizedBodyFrameEvent.Invoke(colorizedBodyFrame);
            }
        }

        _frameCount++;
        _frameReadyDirty = true;
        frameReadyTime.Stop();
    }

    void PrintBody(Astra.BodyFrame bodyFrame)
    {
        if (bodyFrame != null)
        {
            Body[] bodies = { };
            bodyFrame.CopyBodyData(ref bodies);
            foreach (Body body in bodies)
            {
                Astra.Joint headJoint = body.Joints[(int)JointType.Head];

                Debug.Log("Body " + body.Id + " COM " + body.CenterOfMass +
                    " Head Depth: " + headJoint.DepthPosition.X + "," + headJoint.DepthPosition.Y +
                    " World: " + headJoint.WorldPosition.X + "," + headJoint.WorldPosition.Y + "," + headJoint.WorldPosition.Z +
                    " Status: " + headJoint.Status.ToString());
            }
        }
    }

    void PrintDepth(Astra.DepthFrame depthFrame,
                    Astra.CoordinateMapper mapper)
    {
        if (depthFrame != null)
        {
            int width = depthFrame.Width;
            int height = depthFrame.Height;
            long frameIndex = depthFrame.FrameIndex;

            //determine if buffer needs to be reallocated
            if (width != _lastWidth || height != _lastHeight)
            {
                _buffer = new short[width * height];
                _lastWidth = width;
                _lastHeight = height;
            }
            depthFrame.CopyData(ref _buffer);

            int index = (int)((width * (height / 2.0f)) + (width / 2.0f));
            short middleDepth = _buffer[index];

            Vector3D worldPoint = mapper.MapDepthPointToWorldSpace(new Vector3D(width / 2.0f, height / 2.0f, middleDepth));
            Vector3D depthPoint = mapper.MapWorldPointToDepthSpace(worldPoint);

            Debug.Log("depth frameIndex: " + frameIndex
                      + " width: " + width
                      + " height: " + height
                      + " middleDepth: " + middleDepth
                      + " wX: " + worldPoint.X
                      + " wY: " + worldPoint.Y
                      + " wZ: " + worldPoint.Z
                      + " dX: " + depthPoint.X
                      + " dY: " + depthPoint.Y
                      + " dZ: " + depthPoint.Z + " frameCount: " + _frameCount);
        }
    }

    private void UpdateStreamStartStop()
    {
        if (ToggleDepth == null || ToggleDepth.isOn)
        {
            _depthStream.Start();
        }
        else
        {
            _depthStream.Stop();
        }

        if (ToggleColor == null || ToggleColor.isOn)
        {
            _colorStream.Start();
        }
        else
        {
            _colorStream.Stop();
        }

        if (ToggleBody == null || ToggleBody.isOn)
        {
            _bodyStream.Start();
        }
        else
        {
            _bodyStream.Stop();
        }

        if (ToggleMaskedColor == null || ToggleMaskedColor.isOn)
        {
            _maskedColorStream.Start();
        }
        else
        {
            _maskedColorStream.Stop();
        }

        if (ToggleColorizedBody == null || ToggleColorizedBody.isOn)
        {
            _colorizedBodyStream.Start();
        }
        else
        {
            _colorizedBodyStream.Stop();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (!_areStreamsInitialized)
        {
            InitializeStreams();
        }

        UpdateStreamStartStop();

        totalFrameTime.Stop();
        totalFrameTime.Start();

        astraUpdateTime.Start();
        AstraUnityContext.Instance.Update();

        if (_frameReadyDirty)
        {
            astraUpdateTime.Stop();
            _frameReadyDirty = false;
        }
        else
        {
            astraUpdateTime.Pause();
        }

        if (TimeText != null)
        {
            float frameReadyMs = frameReadyTime.AverageMilliseconds;
            float astraUpdateMs = astraUpdateTime.AverageMilliseconds;
            float totalFrameMs = totalFrameTime.AverageMilliseconds;
            float astraUpdateInternalMs = astraUpdateMs - frameReadyMs;
            TimeText.text = "Tot: " + totalFrameMs.ToString("0.0") + " ms\n" +
                            "AU: " + astraUpdateMs.ToString("0.0") + " ms\n" +
                            "FR: " + frameReadyMs.ToString("0.0") + " ms\n" +
                            "AUI: " + astraUpdateInternalMs.ToString("0.0") + " ms\n";
        }
    }

    void OnDestroy()
    {
        Debug.Log("AstraController.OnDestroy");

        UninitializeStreams();

        AstraUnityContext.Instance.Initializing -= OnAstraInitializing;
        AstraUnityContext.Instance.Terminating -= OnAstraTerminating;
    }

    private void OnApplicationPause(bool isPaused)
    {
        // if (isPaused)
        // {
        //     Debug.Log("Application paused.");
        //     AstraUnityContext.Instance.Terminate();
        // }
        // else
        // {
        //     Debug.Log("Application resumed.");
        //     AstraUnityContext.Instance.Initialize();
        // }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("AstraController handling OnApplicationQuit");
        AstraUnityContext.Instance.Terminate();
    }
}
