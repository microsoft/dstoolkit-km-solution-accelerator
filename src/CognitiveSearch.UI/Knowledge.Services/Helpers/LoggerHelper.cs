// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Helpers
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Knowledge.Services;

    public class LoggerHelper
    {
        private bool enableLogger;

        private SeverityLevel minSevLevel;

        //Our single instance of the singleton
        public static LoggerHelper Instance { get { return lazy.Value; } }

        private static readonly Lazy<LoggerHelper> lazy = new Lazy<LoggerHelper>(() => new LoggerHelper());

        private static TelemetryClient telemetryClient = new TelemetryClient(TelemetryConfiguration.CreateDefault());

        //Private constructor to deny instance creation of this class from outside
        private LoggerHelper()
        {
        }


        /// <summary>
        /// Flushes the telemetry client.
        /// </summary>
        public static void FlushTelemetryClient()
        {
            telemetryClient.Flush();
        }

        /// <summary>
        /// Debug only method which is useful for tracing.
        /// </summary>
        /// <param name="message">&gt;Logs information message.</param>
        public void LogVerbose(string message)
        {
            if (!this.ShouldLog(SeverityLevel.Verbose))
            {
                return;
            }

            telemetryClient.TrackTrace(message, SeverityLevel.Verbose);
        }

        /// <summary>
        /// Logs warning messages
        /// </summary>
        /// <param name="message">Logs warning message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public void LogWarning(string message, string applicationName, string moduleName)
        {
            if (!this.ShouldLog(SeverityLevel.Warning))
            {
                return;
            }

            Dictionary<string, string> telemetricProps = new Dictionary<string, string>
            {
                [ServicesConstants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [ServicesConstants.ApplicationName] = applicationName,
                [ServicesConstants.ModuleName] = moduleName
            };

            telemetryClient.TrackTrace(message, SeverityLevel.Warning, telemetricProps);
            FlushTelemetryClient();
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="ex">Logs error with exception.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public void LogError(Exception ex, string applicationName, string moduleName)
        {
            if (!this.ShouldLog(SeverityLevel.Error))
            {
                return;
            }

            // Data to push into AI which can be searched on
            Dictionary<string, string> telemetricProps = new Dictionary<string, string>()
            {
                [ServicesConstants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [ServicesConstants.ApplicationName] = applicationName,
                [ServicesConstants.ModuleName] = moduleName
            };

            telemetryClient.TrackException(ex, telemetricProps);
            telemetryClient.TrackTrace(ex?.Message, SeverityLevel.Error, telemetricProps);
            FlushTelemetryClient();
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="ex">Logs error with exception.</param>
        /// <param name="message">Logs error with Message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public void LogError(Exception ex, string message, string applicationName, string moduleName)
        {
            if (!this.ShouldLog(SeverityLevel.Error))
            {
                return;
            }

            Dictionary<string, string> telemetricProps = new Dictionary<string, string>()
            {
                [ServicesConstants.AppInsightsMessage] = message,
                [ServicesConstants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [ServicesConstants.ApplicationName] = applicationName,
                [ServicesConstants.ModuleName] = moduleName
            };

            telemetryClient.TrackException(ex, telemetricProps);
            telemetryClient.TrackTrace(message, SeverityLevel.Error, telemetricProps);
        }

        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="message">Logs information message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        public void LogInformation(string message, string applicationName, string moduleName)
        {
            if (!this.ShouldLog(SeverityLevel.Information))
            {
                return;
            }

            Dictionary<string, string> telemetricProps = new Dictionary<string, string>()
            {
                [ServicesConstants.AppInsightsCategory] = $"{applicationName}-{moduleName}",
                [ServicesConstants.ApplicationName] = applicationName,
                [ServicesConstants.ModuleName] = moduleName
            };

            telemetryClient.TrackTrace(message, SeverityLevel.Information, telemetricProps);
            FlushTelemetryClient();
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="eventName">Logs error with exception.</param>
        /// <param name="serviceName">Logs error with Message.</param>
        /// <param name="searchId">Application name.</param>
        /// <param name="indexName">Application name.</param>
        /// <param name="queryText">Module name.</param>
        /// <param name="resultCount">Module name.</param>
        public void LogEvent(string eventName, string serviceName, string searchId, string indexName, string queryText, int resultCount)
        {
            var properties = new Dictionary<string, string>
            {
                {"SearchServiceName", serviceName},
                {"SearchId", searchId},
                {"IndexName", indexName},
                {"QueryTerms", queryText},
                {"ResultCount", resultCount.ToString()}
            };
            
            telemetryClient.TrackEvent(eventName, properties);
        }

        //Implement functionality as public instance methods
        public void ConfigureLoggerSettings(string key, bool enableLogger, SeverityLevel severityLevel)
        {
            telemetryClient.InstrumentationKey = key;
            this.enableLogger = enableLogger;
            this.minSevLevel = severityLevel;
        }

        /// <summary>
        /// Returns whether logging is enabled.
        /// </summary>
        /// <returns>
        /// returns true or false.
        /// </returns>
        public bool ShouldLog(SeverityLevel severityLevel)
        {
            return enableLogger && ((int)severityLevel >= (int)minSevLevel);
        }
    }
}
