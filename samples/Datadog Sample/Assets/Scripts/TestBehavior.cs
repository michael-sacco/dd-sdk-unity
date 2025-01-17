// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System.Collections.Generic;
using Datadog.Unity;
using Datadog.Unity.Logs;
using Datadog.Unity.Rum;
using UnityEngine;

public class TestBehavior : MonoBehaviour
{
    // Start is called before the first frame update
    public void Start()
    {
        DatadogSdk.Instance.SetTrackingConsent(TrackingConsent.Granted);

        DatadogSdk.Instance.SetSdkVerbosity(CoreLoggerLevel.Debug);

        DatadogSdk.Instance.Rum.StartView("Test View", attributes: new()
        {
            { "view_attribute", "active" },
        });

        DatadogSdk.Instance.SetUserInfo("1234", "John Fakename");

        var logger = DatadogSdk.Instance.CreateLogger(new DatadogLoggingOptions()
        {
            NetworkInfoEnabled = true,
            RemoteLogThreshold = DdLogLevel.Debug,
        });
        logger.Info("Hello from Unity!");

        logger.Debug("Hello with attributes", new()
        {
            { "my_attribute", 122 },
            { "second_attribute", "with_value" },
            { "bool_attribute", true },
            {
                "nested_attribute", new Dictionary<string, object>()
                {
                    { "internal_attribute", 1.234 },
                }
            },
        });

        DatadogSdk.Instance.Rum.StopResource("key", RumResourceType.Native);
    }
}
