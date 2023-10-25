// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Datadog.Unity.Logs;
using Datadog.Unity.Rum;
using Datadog.Unity.Worker;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: UnityEngine.Scripting.Preserve]
[assembly: UnityEngine.Scripting.AlwaysLinkAssembly]
[assembly: InternalsVisibleTo("com.datadoghq.unity.tests")]

namespace Datadog.Unity.Android
{
    // These are mappings to android.util.Log
    internal enum AndroidLogLevel
    {
        Verbose = 2,
        Debug = 3,
        Info = 4,
        Warn = 5,
        Error = 6,
        Assert = 7,
    }

    [Preserve]
    public static class DatadogInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void InitializeDatadog()
        {
            var options = DatadogConfigurationOptions.Load();
            if (options.Enabled)
            {
                var datadogPlatform = new DatadogAndroidPlatform();
                datadogPlatform.Init(options);
                DatadogSdk.InitWithPlatform(datadogPlatform, options);
            }
        }
    }

    internal class DatadogAndroidPlatform : IDatadogPlatform
    {
        private AndroidJavaClass _datadogClass;

        public DatadogAndroidPlatform()
        {
            _datadogClass = new AndroidJavaClass("com.datadog.android.Datadog");
        }

        public void Init(DatadogConfigurationOptions options)
        {
            var applicationId = options.RumApplicationId == string.Empty ? null : options.RumApplicationId;
            _datadogClass.CallStatic("setVerbosity", (int)AndroidLogLevel.Verbose);

            using var configBuilder = new AndroidJavaObject(
                "com.datadog.android.core.configuration.Configuration$Builder",
                options.ClientToken,
                "prod",
                string.Empty, // Variant Name
                null // Service Name
            );
            configBuilder.Call<AndroidJavaObject>("useSite", DatadogConfigurationHelpers.GetSite(options.Site));
            configBuilder.Call<AndroidJavaObject>("setBatchSize", DatadogConfigurationHelpers.GetBatchSize(options.BatchSize));
            configBuilder.Call<AndroidJavaObject>("setUploadFrequency", DatadogConfigurationHelpers.GetUploadFrequency(options.UploadFrequency));

#if DEBUG
            using var internalProxyClass = new AndroidJavaClass("com.datadog.android._InternalProxy");
            using var proxyInstance = internalProxyClass.GetStatic<AndroidJavaObject>("Companion");
            proxyInstance.Call<AndroidJavaObject>("allowClearTextHttp", configBuilder);
#endif

            using var configuration = configBuilder.Call<AndroidJavaObject>("build");
            _datadogClass.CallStatic<AndroidJavaObject>(
                "initialize",
                GetApplicationContext(),
                configuration,
                DatadogConfigurationHelpers.GetTrackingConsent(TrackingConsent.Pending));

            // Configure logging
            using var logsConfigBuilder = new AndroidJavaObject("com.datadog.android.log.LogsConfiguration$Builder");
            if (options.CustomEndpoint != string.Empty)
            {
                logsConfigBuilder.Call<AndroidJavaObject>("useCustomEndpoint", options.CustomEndpoint + "/logs");
            }

            using var logsConfig = logsConfigBuilder.Call<AndroidJavaObject>("build");
            using var logsClass = new AndroidJavaClass("com.datadog.android.log.Logs");
            logsClass.CallStatic("enable", logsConfig);

            if (options.RumEnabled)
            {
                using var rumConfigBuilder = new AndroidJavaObject("com.datadog.android.rum.RumConfiguration$Builder", options.RumApplicationId);
                rumConfigBuilder.Call<AndroidJavaObject>("disableUserInteractionTracking");
                if (options.CustomEndpoint != string.Empty)
                {
                    rumConfigBuilder.Call<AndroidJavaObject>("useCustomEndpoint", options.CustomEndpoint + "/rum");
                }

                rumConfigBuilder.Call<AndroidJavaObject>("useViewTrackingStrategy", new object[] { null });
                rumConfigBuilder.Call<AndroidJavaObject>("setTelemetrySampleRate", options.TelemetrySampleRate);

                using var rumConfig = rumConfigBuilder.Call<AndroidJavaObject>("build");
                using var rumClass = new AndroidJavaClass("com.datadog.android.rum.Rum");
                rumClass.CallStatic("enable", rumConfig);
            }

            using var crashPlugin = new AndroidJavaClass("com.datadog.android.ndk.NdkCrashReports");
            crashPlugin.CallStatic("enable");
        }

        public void SetTrackingConsent(TrackingConsent trackingConsent)
        {
            _datadogClass.CallStatic("setTrackingConsent", DatadogConfigurationHelpers.GetTrackingConsent(trackingConsent));
        }

        public DdLogger CreateLogger(DatadogLoggingOptions options, DatadogWorker worker)
        {
            using var loggerBuilder = new AndroidJavaObject("com.datadog.android.log.Logger$Builder");
            if (options.Service != null)
            {
                loggerBuilder.Call<AndroidJavaObject>("setService", options.Service);
            }

            if (options.Name != null)
            {
                loggerBuilder.Call<AndroidJavaObject>("setName", options.Name);
            }

            loggerBuilder.Call<AndroidJavaObject>("setNetworkInfoEnabled", options.NetworkInfoEnabled);
            loggerBuilder.Call<AndroidJavaObject>("setBundleWithRumEnabled", options.BundleWithRumEnabled);
            var androidLogger = loggerBuilder.Call<AndroidJavaObject>("build");

            var innerLogger = new DatadogAndroidLogger(options.RemoteLogThreshold, options.RemoteSampleRate, androidLogger);
            return new DdWorkerProxyLogger(worker, innerLogger);
        }

        public IDdRum InitRum(DatadogConfigurationOptions options)
        {
            using var globalRumMonitorClass = new AndroidJavaClass("com.datadog.android.rum.GlobalRumMonitor");
            var rum = globalRumMonitorClass.CallStatic<AndroidJavaObject>("get");

            return new DatadogAndroidRum(rum);
        }

        public void SendDebugTelemetry(string message)
        {
            using var proxy = GetInternalProxy();
            using AndroidJavaObject telemetry = proxy.Call<AndroidJavaObject>("get_telemetry");
            telemetry.Call("debug", message);
        }

        public void SendErrorTelemetry(string message, string stack, string kind)
        {
            using var proxy = GetInternalProxy();
            using AndroidJavaObject telemetry = proxy.Call<AndroidJavaObject>("get_telemetry");
            telemetry.Call("error", message, stack, kind);
        }

        private AndroidJavaObject GetApplicationContext()
        {
            using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        }

        private AndroidJavaObject GetInternalProxy()
        {
            using AndroidJavaObject datadogInstance = _datadogClass.GetStatic<AndroidJavaObject>("INSTANCE");
            AndroidJavaObject internalProxy = datadogInstance.Call<AndroidJavaObject>("_internalProxy", new object[] { null });

            return internalProxy;
        }
    }
}
