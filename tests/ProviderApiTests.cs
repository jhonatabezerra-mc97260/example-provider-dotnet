using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using PactNet;
using PactNet.Infrastructure.Outputters;
using Xunit;
using Xunit.Abstractions;

namespace tests
{
    public class ProviderApiTests : IDisposable
    {
        private string _providerUri { get; }
        private string _pactServiceUri { get; }
        private IWebHost _webHost { get; }
        private ITestOutputHelper _outputHelper { get; }

        public ProviderApiTests(ITestOutputHelper output) {
            _outputHelper = output;
            _providerUri = "http://localhost:9000";
            _pactServiceUri = "http://localhost:9001";

            _webHost = WebHost.CreateDefaultBuilder()
                .UseUrls(_pactServiceUri)
                .UseStartup<TestStartup>()
                .Build();

            _webHost.Start();
        }

        [Fact]
        public void EnsureProviderApiHonoursPactWithConsumer() {
            // Arrange
            var pactBaseUrl = "https://rdisoftware.pactflow.io";
            var gitCommit = "bfc7d93b755ccf76a878ad99e3b6332aef067572";
            var pactBrokerToken = "s8V8Ol4fKqfq54cYvAEKZg";
            var config = new PactVerifierConfig
            {
                // NOTE: We default to using a ConsoleOutput, however xUnit 2 does not capture the
                // console output, so a custom outputter is required.
                Outputters = new List<IOutput> { new ConsoleOutput() },

                // Output verbose verification logs to the test output
                Verbose = true,
                PublishVerificationResults = true,
                ProviderVersion = Environment.GetEnvironmentVariable("GIT_COMMIT")
            };

            IPactVerifier pactVerifier = new PactVerifier(config);
            pactVerifier
                .ProviderState($"{_pactServiceUri}/provider-states")
                .ServiceProvider("provider", _providerUri)
                .HonoursPactWith("consumer")
                .PactBroker(Environment.GetEnvironmentVariable("PACT_BROKER_BASE_URL") ?? pactBaseUrl,
                    uriOptions: new PactUriOptions(Environment.GetEnvironmentVariable("PACT_BROKER_TOKEN") ?? pactBrokerToken),
                    consumerVersionTags: new List<string> { "master", "prod", "main" });

            // Act / Assert
            pactVerifier.Verify();
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    _webHost.StopAsync().GetAwaiter().GetResult();
                    _webHost.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}