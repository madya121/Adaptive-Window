#if UNITY_ANDROID && !UNITY_EDITOR
#define ASTRA_UNITY_ANDROID_NATIVE
#endif

﻿using Astra;
using System;
using UnityEngine;
#if UNITY_ANDROID

#endif

public sealed class AstraUnityContext
{
    private bool _initialized = false;
    private bool _initializing = false;

#if ASTRA_UNITY_ANDROID_NATIVE
	private static AndroidJavaClass javaClass;
	private static AndroidJavaObject javaActivity;
#endif

    //private AstraUnityContext()
    //{
    //    Initialize();
    //}

    private EventHandler<AstraInitializingEventArgs> _initializingEvent;
    public event EventHandler<AstraInitializingEventArgs> Initializing
    {
        add
        {
            _initializingEvent += value;

            if (_initialized)
            {
                value(this, new AstraInitializingEventArgs());
            }
        }

        remove
        {
            _initializingEvent -= value;
        }
    }

    public event EventHandler<AstraTerminatingEventArgs> Terminating;

    public event EventHandler<PermissionRequestCompletedEventArgs> PermissionRequestCompleted;

    public static AstraUnityContext Instance { get { return Nested.Context; } }
    private class Nested
    {
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Nested() { }
        internal static readonly AstraUnityContext Context = new AstraUnityContext();
    }

    public void Initialize()
    {
        if (_initialized)
        {
            Debug.Log("Astra SDK previously initialized");
            return;
        }

        if (_initializing)
        {
            return;
        }

        _initializing = true;

        Debug.Log("Astra SDK initializing.");
        Context.Initialize();
        RaiseInitializing();

        _initialized = true;
        _initializing = false;
    }

    public void Terminate()
    {
        if (!_initialized)
        {
            return;
        }

        Debug.Log("Astra SDK terminating.");
        RaiseTerminating();

        Context.Terminate();
        _initialized = false;
    }

    public void Update()
    {
        if (!_initialized)
        {
            return;
        }

        Context.Update();
    }

    public void RequestUsbDeviceAccessFromAndroid()
    {
        Initialize();

#if ASTRA_UNITY_ANDROID_NATIVE
        EnsureJavaActivity();

        Debug.Log("AstraUnityContext.RequestUsbDeviceAccessFromAndroid() calling openAllDevices");

        //TODO use AndroidJavaProxy to do a callback
        System.Object[] args = new System.Object[0];
        javaActivity.Call("openAllDevices", args);

        Debug.Log("AstraUnityContext.RequestUsbDeviceAccessFromAndroid() called openAllDevices");

        //TODO: only call this in the callback with the success/fail results
        RaisePermissionRequestCompleted(true);
#else
        RaisePermissionRequestCompleted(true);
#endif
    }

    private void RaisePermissionRequestCompleted(bool granted)
    {
        var eventArgs = new PermissionRequestCompletedEventArgs(granted);
        var handler = PermissionRequestCompleted;
        if (handler != null) handler(this, eventArgs);
    }

    ~AstraUnityContext()
    {
        Debug.Log("Finalizer of AstraUnityContext");
        Terminate();
    }

#if ASTRA_UNITY_ANDROID_NATIVE
    private void EnsureJavaActivity()
    {
        if (javaActivity == null)
        {
            Debug.Log("AstraUnityContext.EnsureJavaActivity() Getting Java activity");
            javaClass = new AndroidJavaClass("com.orbbec.astra.android.unity3d.AstraUnityPlayerActivity");
            javaActivity = javaClass.GetStatic<AndroidJavaObject>("Instance");
            Debug.Log("AstraUnityContext.EnsureJavaActivity() Got Java activity");
        }
    }
#endif

    private void RaiseInitializing()
    {
        var handler = _initializingEvent;
        if (handler != null) handler(this, new AstraInitializingEventArgs());  
    }

    private void RaiseTerminating()
    {
        var handler = Terminating;
        if (handler != null) handler(this, new AstraTerminatingEventArgs());
    }
}