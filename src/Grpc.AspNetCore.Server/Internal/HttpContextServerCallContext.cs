﻿#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Grpc.AspNetCore.Server.Internal
{
    internal class HttpContextServerCallContext : ServerCallContext
    {
        private string _peer;
        private Metadata _requestHeaders;
        private Metadata _responseTrailers;

        internal HttpContextServerCallContext(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        internal HttpContext HttpContext { get; }

        internal bool HasResponseTrailers => _responseTrailers != null;

        protected override string MethodCore => HttpContext.Request.Path.Value;

        protected override string HostCore => HttpContext.Request.Host.Value;

        protected override string PeerCore
        {
            get
            {
                if (_peer == null)
                {
                    var connection = HttpContext.Connection;
                    if (connection.RemoteIpAddress != null)
                    {
                        _peer = (connection.RemoteIpAddress.AddressFamily == AddressFamily.InterNetwork ? "ipv4:" : "ipv6:") + connection.RemoteIpAddress + ":" + connection.RemotePort;
                    }
                }

                return _peer;
            }
        }

        // TODO(JunTaoLuo, JamesNK): implement this
        protected override DateTime DeadlineCore => throw new NotImplementedException();

        protected override Metadata RequestHeadersCore
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = new Metadata();

                    foreach (var header in HttpContext.Request.Headers)
                    {
                        if (!header.Key.StartsWith(':'))
                        {
                            _requestHeaders.Add(header.Key, header.Value);
                        }
                    }
                }

                return _requestHeaders;
            }
        }

        // TODO(JunTaoLuo, JamesNK): implement this
        protected override CancellationToken CancellationTokenCore => throw new NotImplementedException();

        protected override Metadata ResponseTrailersCore
        {
            get
            {
                if (_responseTrailers == null)
                {
                    _responseTrailers = new Metadata();
                }

                return _responseTrailers;
            }
        }

        protected override Status StatusCore { get; set; }

        protected override WriteOptions WriteOptionsCore { get; set; }

        // TODO(JunTaoLuo, JamesNK): implement this
        protected override AuthContext AuthContextCore => throw new NotImplementedException();

        // TODO(JunTaoLuo, JamesNK): implement this
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions options)
        {
            throw new NotImplementedException();
        }

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        {
            if (responseHeaders != null)
            {
                foreach (var entry in responseHeaders)
                {
                    if (!entry.IsBinary)
                    {
                        // TODO(juntaoluo): what about binary headers?
                        HttpContext.Response.Headers[entry.Key] = entry.Value;
                    }
                }
            }

            return HttpContext.Response.Body.FlushAsync();
        }
    }
}
