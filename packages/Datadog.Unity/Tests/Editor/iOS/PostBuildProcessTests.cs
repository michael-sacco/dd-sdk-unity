// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2023-Present Datadog, Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

// Disable "Scriptable Objects should not be instantiated directly"
#pragma warning disable UNT0011

namespace Datadog.Unity.Editor.iOS
{
    public class PostBuildProcessTests
    {
        private static readonly string _cleanMainfile = "main.txt";

        private string _tempDirectory;
        private string _initializationFilePath;
        private string _mainFilePath;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine("tmp", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _initializationFilePath = Path.Combine(_tempDirectory, "DatadogInitialization.swift");
            _mainFilePath = Path.Combine(_tempDirectory, _cleanMainfile);
            File.Copy(_cleanMainfile, _mainFilePath);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete("tmp", true);
        }

        [Test]
        public void GenerateOptionsFileCreatesFile()
        {
            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, new DatadogConfigurationOptions(),
                null);

            File.Exists(_initializationFilePath);
        }

        [Test]
        public void GenerateOptionsFileWritesAutoGenerationWarning()
        {
            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, new DatadogConfigurationOptions(),
                null);

            string fileContents = File.ReadAllText(_initializationFilePath);
            Assert.IsTrue(fileContents.Contains("THIS FILE IS AUTO GENERATED"));
        }

        [TestCase(BatchSize.Small, ".small")]
        [TestCase(BatchSize.Medium, ".medium")]
        [TestCase(BatchSize.Large, ".large")]
        public void GenerationOptionsFileWritesBatchSize(BatchSize batchSize, string expectedBatchSizeString)
        {
            var options = new DatadogConfigurationOptions()
            {
                Enabled = true,
                BatchSize = batchSize,
            };
            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, options, null);

            var lines = File.ReadAllLines(_initializationFilePath);
            var batchSizeLines = lines.Where(l => l.Contains("batchSize:")).ToArray();
            Assert.AreEqual(1, batchSizeLines.Length);
            Assert.AreEqual($"batchSize: {expectedBatchSizeString},", batchSizeLines.First().Trim());
        }

        [TestCase(UploadFrequency.Rare, ".rare")]
        [TestCase(UploadFrequency.Average, ".average")]
        [TestCase(UploadFrequency.Frequent, ".frequent")]
        public void GenerationOptionsFileWritesUploadFrequency(UploadFrequency uploadFrequency,
            string expectedUploadFrequency)
        {
            var options = new DatadogConfigurationOptions()
            {
                Enabled = true,
                UploadFrequency = uploadFrequency,
            };
            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, options, null);

            var lines = File.ReadAllLines(_initializationFilePath);
            var uploadFrequencyLines = lines.Where(l => l.Contains("uploadFrequency:")).ToArray();
            Assert.AreEqual(1, uploadFrequencyLines.Length);
            Assert.AreEqual($"uploadFrequency: {expectedUploadFrequency}", uploadFrequencyLines.First().Trim());
        }

        [TestCase(0.0f)]
        [TestCase(12.0f)]
        [TestCase(100.0f)]
        public void GenerateOptionsFileWritesTelemetrySampleRate(float sampleRate)
        {
            var options = new DatadogConfigurationOptions()
            {
                Enabled = true,
                RumEnabled = true,
                TelemetrySampleRate = sampleRate,
            };
            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, options, null);

            var lines = File.ReadAllLines(_initializationFilePath);
            var sampleTelemetryLines = lines.Where(l => l.Contains("telemetrySampleRate ="));
            var telemetryLines = sampleTelemetryLines as string[] ?? sampleTelemetryLines.ToArray();
            Assert.AreEqual(1, telemetryLines.Length);
            Assert.AreEqual($"rumConfig.telemetrySampleRate = {sampleRate}", telemetryLines.First().Trim());
        }

        [Test]
        public void MissingBuildIdDoesNotWriteBuildId()
        {
            var options = new DatadogConfigurationOptions();
            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, options, null);

            var lines = File.ReadAllLines(_initializationFilePath);
            var buildIdLines = lines.Where(l => l.Contains("\"_dd.build_id:\""));
            Assert.AreEqual(0, buildIdLines.Count());
        }

        [Test]
        public void GeneratedBuildIdWritesBuildId()
        {
            var uuid = Guid.NewGuid().ToString();

            var options = new DatadogConfigurationOptions()
            {
                Enabled = true
            };

            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, options, uuid);

			var lines = File.ReadAllLines(_initializationFilePath);
            var buildIdLines = lines.Where(l => l.Contains("\"_dd.build_id\":"));
            Assert.AreEqual(1, buildIdLines.Count());
            Assert.AreEqual($"\"_dd.build_id\": \"{uuid}\"", buildIdLines.First().Trim());
		}

		[Test]
        public void GenerateOptionsFileWritesDefaultEnv()
        {
            var options = new DatadogConfigurationOptions();
            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, options, null);

            var lines = File.ReadAllLines(_initializationFilePath);
            var envLines = lines.Where(l => l.Contains("env: ")).ToArray();
            Assert.AreEqual(1, envLines.Length);
            Assert.AreEqual($"env: \"prod\",", envLines.First().Trim());
        }

        [Test]
        public void GenerateOptionsFileWritesEnvFromOptions()
        {
            var options = new DatadogConfigurationOptions()
            {
                Env = "env-from-options",
            };
            PostBuildProcess.GenerateInitializationFile(_initializationFilePath, options, null);

            var lines = File.ReadAllLines(_initializationFilePath);
            var envLines = lines.Where(l => l.Contains("env: ")).ToArray();
            Assert.AreEqual(1, envLines.Length);
            Assert.AreEqual($"env: \"env-from-options\",", envLines.First().Trim());
        }

        [Test]
        public void AddInitializationToMainAddsDatadogBlocks()
        {
            var options = new DatadogConfigurationOptions()
            {
                Enabled = true
            };

            PostBuildProcess.AddInitializationToMain(_mainFilePath, options);

            string fileContents = File.ReadAllText(_mainFilePath);

            var includeBlock = @"// > Datadog Generated Block
#import ""DatadogOptions.h""
// < End Datadog Generated Block";

            var initializationBlock = @"        // > Datadog Generated Block
        initializeDatadog();
        // < End Datadog Generated Block";

            Assert.IsTrue(fileContents.Contains(includeBlock));
            Assert.IsTrue(fileContents.Contains(initializationBlock));
        }

        [Test]
        public void RemoveDatadogBlocksRemovesDatadogBlocks()
        {
            var options = new DatadogConfigurationOptions();
            PostBuildProcess.AddInitializationToMain(_mainFilePath, options);

            var fileContents = File.ReadAllLines(_mainFilePath);
            var cleanContents = PostBuildProcess.RemoveDatadogBlocks(new List<string>(fileContents));

            Assert.IsNull(cleanContents.FirstOrDefault(l => l.Contains("Datadog")));
        }
    }
}
