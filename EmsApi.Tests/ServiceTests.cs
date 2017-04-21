using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using EmsApi.Client.V2;

namespace EmsApi.Tests
{
    public class ServiceTests : TestBase
    {
        [Fact( DisplayName = "Invalid configuration should throw an exception" ) ]
        public void Invalid_configuration_should_throw_exception()
        {
            var service = new EmsApiService();
            var badConfig = new EmsApiServiceConfiguration()
            {
                UserName = string.Empty,
                Password = null
            };

            Action setConfig = () => service.ServiceConfig = badConfig;
            setConfig.ShouldThrowExactly<EmsApiConfigurationException>();
        }

        [Fact( DisplayName = "Valid configuration should change service state" )]
        public void Valid_configuration_should_change_service_state()
        {
            using( var service = NewService() )
            {
                // Authenticate the first time.
                service.Authenticate().Should().BeTrue();
                service.Authenticated.Should().BeTrue();

                // Change the configuration.
                var newConfig = m_config.Clone();
                newConfig.Password = "somethingElse";

                Action setConfig = () => service.ServiceConfig = newConfig;
                setConfig.ShouldNotThrow();

                // Make sure we are no longer authenticated.
                service.Authenticated.Should().BeFalse();
            }
        }

        [Fact( DisplayName = "Service should shut down gracefully" )]
        public void Service_should_shut_down_gracefully()
        {
            var service = NewService();
            service.Dispose();
            service = null;
        }

        [Fact( DisplayName = "Service should support anonymous proxies" )]
        public void Service_should_support_anonymous_proxies()
        {
            const int port = 8124;

            EmsApiServiceConfiguration config = m_config.Clone();
            config.ProxyServer = string.Format( "http://localhost:{0}", port );
            TestProxyCallbacks( port, config );

            config = m_config.Clone();
            config.ProxyServer = "localhost";
            config.ProxyPort = port;
            TestProxyCallbacks( port, config );
        }

        private void TestProxyCallbacks( int port, EmsApiServiceConfiguration config )
        {
            int proxyHits = 0;
            using( var proxy = new TestProxy( port, _ => proxyHits++ ) )
            {
                using( var api = new EmsApiService( config ) )
                {
                    api.Authenticate();
                    var emsSystems = api.EmsSystems.GetAll().ToArray();
                }
            }
            proxyHits.Should().BeGreaterThan( 0 );
        }

        /// <summary>
        /// Creates an in-memory https proxy server at "localhost:port".
        /// </summary>
        private class TestProxy : IDisposable
        {
            public TestProxy( int port, Action<SessionEventArgs> callback )
            {
                m_proxy = new ProxyServer();
                m_proxy.AddEndPoint( new ExplicitProxyEndPoint( System.Net.IPAddress.Any, port, true ) );

                m_requestCallback = callback;
                m_proxy.BeforeRequest += RunCallback;

                m_proxy.Start();
            }

            private Task RunCallback( object sender, SessionEventArgs e )
            {
                if( m_requestCallback == null )
                    return Task.Run( () => { } );

                return Task.Run( () => m_requestCallback( e ) );
            }

            private ProxyServer m_proxy;
            private Action<SessionEventArgs> m_requestCallback;

            public void Dispose()
            {
                m_proxy.BeforeRequest -= RunCallback;
                m_proxy.Stop();
                m_proxy.Dispose();
            }
        }
    }
}
