// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using Datadog.Unity.Core;
using Datadog.Unity.Logs;
using Datadog.Unity.Rum;
using Datadog.Unity.Worker;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Datadog.Unity
{
    public class DatadogSdk
    {
        public static readonly DatadogSdk Instance = new();

        internal const string SourceConfigKey = "_dd.source";

        private IDatadogPlatform _platform = new DatadogNoOpPlatform();

        private DdUnityLogHandler _logHandler;
        private DatadogWorker _worker;
        private InternalLogger _internalLogger;
        private ResourceTrackingHelper _resourceTrackingHelper;

        private DatadogSdk()
        {
            DefaultLogger = new DdNoOpLogger();
        }

        public DdLogger DefaultLogger
        {
            get; private set;
        }

        public IDdRum Rum { get; private set; } = new DdNoOpRum();

        internal InternalLogger InternalLogger => _internalLogger;

        internal ResourceTrackingHelper ResourceTrackingHelper => _resourceTrackingHelper;

        public static void Shutdown()
        {
            Instance.ShutdownInstance();
        }

        public void SetTrackingConsent(TrackingConsent trackingConsent)
        {
            InternalHelpers.Wrap("SetTrackingConsent", () =>
            {
                _platform.SetTrackingConsent(trackingConsent);
            });
        }

        /// <summary>
        /// Sets information about the current user. User information will be added to logs, traces, and RUM events
        /// automatically.
        /// </summary>
        /// <param name="id">The ID for the user.</param>
        /// <param name="name">The name for the user.</param>
        /// <param name="email">The user's email.</param>
        /// <param name="extraInfo">A map of any extra information about the user.</param>
        public void SetUserInfo(
            string id = null,
            string name = null,
            string email = null,
            Dictionary<string, object> extraInfo = null)
        {
            InternalHelpers.Wrap("SetUserInfo", () =>
            {
                _worker.AddMessage(new DdSdkProcessor.SetUserInfoMessage(id, name, email, extraInfo));
            });
        }

        /// <summary>
        /// Add custom attributes to the current user information
        ///
        /// This extra info will be added to already existing extra info that is added to logs, traces, and RUM events
        /// automatically.
        ///
        /// Setting an existing attribute to `null` will remove that attribute from the user's extra info.
        /// </summary>
        /// <param name="extraInfo">Any additional extra info about a user.</param>
        public void AddUserExtraInfo(Dictionary<string, object> extraInfo)
        {
            InternalHelpers.Wrap("AddUserExtraInfo", () =>
            {
                _worker.AddMessage(new DdSdkProcessor.AddUserExtraInfoMessage(extraInfo));
            });
        }

        public DdLogger CreateLogger(DatadogLoggingOptions options)
        {
            try
            {
                return _platform?.CreateLogger(options, _worker);
            }
            catch (Exception e)
            {
                var internalLogger = DatadogSdk.Instance.InternalLogger;
                internalLogger?.Log(DdLogLevel.Warn, $"Error creating logger: {e}");
                internalLogger?.Log(DdLogLevel.Warn, "A NoOp logger will be used instead.");

                internalLogger?.TelemetryError("Error creating logger", e);

                return new DdNoOpLogger();
            }
        }

        public void ClearAllData()
        {
            InternalHelpers.Wrap("ClearAllData", () =>
            {
                _platform.ClearAllData();
            });
        }

        internal static void InitWithPlatform(IDatadogPlatform platform, DatadogConfigurationOptions options)
        {
            Instance.Init(platform, options);
        }

        private void Init(IDatadogPlatform platform, DatadogConfigurationOptions options)
        {
            try
            {
                _platform = platform;

                // Create our worker thread
                _worker = new ();
                _worker.AddProcessor(DdSdkProcessor.SdkTargetName, new DdSdkProcessor(_platform));

                _worker.AddProcessor(DdLogsProcessor.LogsTargetName, new DdLogsProcessor());
                _internalLogger = new InternalLogger(_worker, _platform);
                _resourceTrackingHelper = new ResourceTrackingHelper(options);

                var loggingOptions = new DatadogLoggingOptions()
                {
                    RemoteLogThreshold = DdLogHelpers.LogTypeToDdLogLevel(options.RemoteLogThreshold),
                };
                DefaultLogger = CreateLogger(loggingOptions);
                if (options.ForwardUnityLogs)
                {
                    _logHandler = new (DefaultLogger);
                    _logHandler.Attach();
                }

                if (options.RumEnabled)
                {
                    EnableRum(options);
                }

                _worker.Start();

                Application.quitting += OnQuitting;
            }
            catch (Exception e)
            {
                _internalLogger?.TelemetryError("Error initializing DatadogSdk", e);
            }
        }

        private void EnableRum(DatadogConfigurationOptions options)
        {
            if (options.RumApplicationId is null or "")
            {
                _internalLogger.Log(DdLogLevel.Error, "Datadog RUM is enabled but an Application ID is not set.");
                return;
            }

            var platformRum = _platform.InitRum(options);
            _worker.AddProcessor(DdRumProcessor.RumTargetName, new DdRumProcessor(platformRum));
            Rum = new DdWorkerProxyRum(_worker);

            if (options.AutomaticSceneTracking)
            {
                SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            }
        }

        private void SceneManagerOnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            Rum.StartView(nextScene.path, nextScene.name, new Dictionary<string, object>()
            {
                { "is_sub_scene", nextScene.isSubScene },
                { "is_loaded", nextScene.isLoaded },
            });
        }

        private void ShutdownInstance()
        {
            _platform = null;
            DefaultLogger = null;
            _logHandler?.Detach();
            _logHandler = null;
            _worker?.Stop();
        }

        private void OnQuitting()
        {
            ShutdownInstance();
        }

        internal class ConfigKeys
        {
            internal const string Source = "_dd.source";
            internal const string BuildId = "_dd.build_id";
            internal const string SdkVersion = "_dd.sdk_version";
        }
    }
}
