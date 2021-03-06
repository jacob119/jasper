﻿using System;
using Baseline.Dates;
using Jasper.Messaging.Transports.Configuration;

namespace Jasper.Http
{
    public class HttpTransportSettings : IHttpTransportConfiguration
    {
        private readonly MessagingSettings _parent;

        public HttpTransportSettings(MessagingSettings parent)
        {
            _parent = parent;
        }

        public TimeSpan ConnectionTimeout { get; set; } = 10.Seconds();
        public string RelativeUrl { get; set; } = "messages";
        public bool IsEnabled => _parent.StateFor("http") == TransportState.Enabled;


        IHttpTransportConfiguration IHttpTransportConfiguration.EnableListening(bool enabled)
        {
            ListeningEnabled = enabled;

            return this;
        }

        public bool ListeningEnabled { get; set; }

        IHttpTransportConfiguration IHttpTransportConfiguration.RelativeUrl(string url)
        {
            RelativeUrl = url;
            return this;
        }

        IHttpTransportConfiguration IHttpTransportConfiguration.ConnectionTimeout(TimeSpan span)
        {
            ConnectionTimeout = span;
            return this;
        }
    }
}
