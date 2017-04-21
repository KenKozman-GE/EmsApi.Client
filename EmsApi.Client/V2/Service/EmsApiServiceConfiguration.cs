using System;
using System.Linq;
using System.Net.Http.Headers;

namespace EmsApi.Client.V2
{
    /// <summary>
    /// The configuration to use when talking to the EMS API. This may be
    /// modified after the <seealso cref="EmsApiService"/> has been created
    /// by setting the <seealso cref="EmsApiService.ServiceConfig"/> property.
    /// </summary>
    public class EmsApiServiceConfiguration
    {
        /// <summary>
        /// Creates a new instance of the configuration with the given endpoint.
        /// </summary>
        /// <param name="endpoint">
        /// The API endpoint to connect to. If this is not specified, a default
        /// value will be used.
        /// </param>
        /// <param name="useEnvVars">
        /// When true, system environment variables will be used to substitute certain
        /// parameters when the configuration is first constructed.
        /// </param>
        /// <remarks>
        /// </remarks>
        public EmsApiServiceConfiguration( string endpoint = EmsApiEndpoints.Default, bool useEnvVars = true )
        {
            Endpoint = endpoint;
            UseCompression = true;
            ThrowExceptionOnAuthFailure = true;
            ThrowExceptionOnApiFailure = true;

            if( useEnvVars )
                LoadEnvironmentVariables();
        }

        /// <summary>
        /// The API endpoint to connect to. This may be substituted by the "EmsApiEndpoint"
        /// environment variable.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// The user name to use for authentication. This may be substituted by the "EmsApiUsername"
        /// environment variable.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password to use for authentication. This may be substituted by the "EmsApiPassword"
        /// environment variable, which should contain a base64 encoded version of the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The URL for an HTTP or HTTP proxy server to use. In most situations this is not required,
        /// but if your network doesn't have direct access to the internet then this will need to be
        /// used. Note that this does not include the port for the proxy server, that's a separate property.
        /// </summary>
        public string ProxyServer { get; set; }

        /// <summary>
        /// The port for the proxy server, if a proxy server is specified. If this is not specified, but
        /// a proxy server url is, this will default to port 80 for a HTTP address and port 443 for an HTTPS
        /// address.
        /// </summary>
        public int ProxyPort { get; set; }

        /// <summary>
        /// The username used to log into the proxy server. Leave blank for anonymous authentication.
        /// </summary>
        public string ProxyUserName { get; set; }

        /// <summary>
        /// The password used to log into the proxy server. Leave blank for anonymous authentication.
        /// </summary>
        public string ProxyPassword { get; set; }

        /// <summary>
        /// The application name to pass along to the EMS API. This is used for logging on the
        /// server side.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// The user agent header to pass along to the EMS API. 
        /// </summary>
        public string UserAgent { get { return "ems-api-sdk Dotnet v0.1"; } }

        /// <summary>
        /// When true, gzip compression will be used for responses on routes that support it.
        /// This is enabled by default. Responses are automatically decompressed by the library,
        /// so there's no advantage to disabling this unless you are running in a CPU constrained
        /// scenario.
        /// </summary>
        public bool UseCompression { get; set; }

        /// <summary>
        /// The trusted token to use for authentication.
        /// </summary>
        /// <remarks>
        /// This will always be overridden by the username / password.
        /// </remarks>
        public string TrustedToken { get; set; }

        /// <summary>
        /// When true, the <seealso cref="EmsApiService"/> will throw an exception for
        /// authentication failures. This is the default behavior, because opting out 
        /// of exceptions requires implementing additional callback functions. Callbacks
        /// are always executed regardless of this setting.
        /// </summary>
        public bool ThrowExceptionOnAuthFailure { get; set; }

        /// <summary>
        /// When true, the <seealso cref="EmsApiService"/> will throw an exception for
        /// any low level API failures. This is the default behavior, because opting out
        /// of exceptions requires implementing additional callback functions. Callbacks
        /// are always executed regardless of this setting.
        /// </summary>
        public bool ThrowExceptionOnApiFailure { get; set; }

        /// <summary>
        /// Returns true if authentication should use the trusted token, false otherwise.
        /// </summary>
        public bool UseTrustedToken()
        {
            return string.IsNullOrEmpty( UserName );
        }

        /// <summary>
        /// Adds the default headers into the given header collection.
        /// </summary>
        internal void AddDefaultRequestHeaders( HttpRequestHeaders headerCollection )
        {
            headerCollection.Add( HttpHeaderNames.UserAgent, UserAgent );

            // Optional application name.
            if( !string.IsNullOrEmpty( ApplicationName ) )
                headerCollection.Add( HttpHeaderNames.ApplicationName, ApplicationName );

            // Optional compression header.
            if( UseCompression )
                headerCollection.Add( HttpHeaderNames.AcceptEncoding, "gzip" );
        }

        /// <summary>
        /// Returns true if the configuration is valid, or false if not.
        /// </summary>
        /// <param name="error">
        /// The reason the configuration is invalid.
        /// </param>
        internal bool Validate( out string error )
        {
            error = null;

            // Make sure the user did not set the endpoint back to null.
            if( string.IsNullOrEmpty( Endpoint ) )
            {
                error = "The API endpoint is not set.";
                return false;
            }

            // Make sure we have a username / password OR token.
            if( !string.IsNullOrEmpty( UserName ) )
            {
                if( string.IsNullOrEmpty( Password ) )
                {
                    error = "A password was not provided for the given username.";
                    return false;
                }

                return true;
            }

            if( string.IsNullOrEmpty( TrustedToken ) )
            {
                error = "Either a username and password or a trusted token must be provided.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Compares this configuration to the given other configuration, and returns
        /// true if parameters affecting authentication have changed.
        /// </summary>
        internal bool AuthenticationChanged( EmsApiServiceConfiguration other )
        {
            return
                Endpoint != other.Endpoint ||
                UserName != other.UserName ||
                Password != other.Password;
        }

        /// <summary>
        /// Compares this configuration to the given other configuration, and returns
        /// true if parameters affecting the proxy have changed.
        /// </summary>
        internal bool ProxyChanged( EmsApiServiceConfiguration other )
        {
            return
                ProxyServer != other.ProxyServer ||
                ProxyPort != other.ProxyPort ||
                ProxyUserName != other.ProxyUserName ||
                ProxyPassword != other.ProxyPassword;
        }

        /// <summary>
        /// Retruns true if the proxy server url already contains the port.
        /// </summary>
        internal bool ProxyServerIncludesPort()
        {
            string[] parts = ProxyServer.TrimEnd( '/' ).Split( ':' );
            if( parts.Length == 0 )
                return false;

            int junk;
            if( !int.TryParse( parts.Last(), out junk ) )
                return false;

            return true;
        }

        /// <summary>
        /// Returns the correct proxy port to use, if it isn't already specified in the url.
        /// </summary>
        internal int ResolveProxyPort()
        {
            // Check the explicit port.
            if( ProxyPort != 0 )
                return ProxyPort;

            // Glean it from the uri.
            Uri endpointUri;
            if( !Uri.TryCreate( Endpoint, UriKind.Absolute, out endpointUri ) )
                return 443; // No way to figure it out, default to 443.

            return endpointUri.Scheme == "https" ? 443 : 80;
        }

        /// <summary>
        /// Loads some well-known environment variables into the current configuration.
        /// </summary>
        private void LoadEnvironmentVariables()
        {
            string endpoint = Environment.GetEnvironmentVariable( "EmsApiEndpoint" );
            if( !string.IsNullOrWhiteSpace( endpoint ) )
                Endpoint = endpoint.Trim();

            string user = Environment.GetEnvironmentVariable( "EmsApiUsername" );
            if( !string.IsNullOrWhiteSpace( user ) )
                UserName = user.Trim();

            string base64pass = Environment.GetEnvironmentVariable( "EmsApiPassword" );
            if( !string.IsNullOrWhiteSpace( base64pass ) )
            {
                byte[] passBytes = Convert.FromBase64String( base64pass.Trim() );
                Password = System.Text.Encoding.UTF8.GetString( passBytes );
            }

            string proxyServer = Environment.GetEnvironmentVariable( "EmsApiProxyServer" );
            if( !string.IsNullOrWhiteSpace( proxyServer ) )
                ProxyServer = proxyServer.Trim();

            string proxyPort = Environment.GetEnvironmentVariable( "EmsApiProxyPort" );
            if( !string.IsNullOrWhiteSpace( proxyPort ) )
            {
                int port;
                if( !int.TryParse( proxyPort, out port ) )
                {
                    throw new EmsApiConfigurationException( string.Format( 
                        "The EmsApiProxyPort environment variable '{0}' cannot be converted to an integer.", proxyPort ) );
                }

                ProxyPort = port;
            }

            string proxyUser = Environment.GetEnvironmentVariable( "EmsApiProxyUsername" );
            if( !string.IsNullOrWhiteSpace( proxyUser ) )
                ProxyUserName = proxyUser.Trim();

            string proxyPass = Environment.GetEnvironmentVariable( "EmsApiProxyPassword" );
            if( !string.IsNullOrWhiteSpace( proxyPass ) )
                ProxyPassword = proxyPass.Trim();
        }

        /// <summary>
        /// Retrun a copy of the configuration.
        /// </summary>
        public EmsApiServiceConfiguration Clone()
        {
            return new EmsApiServiceConfiguration
            {
                Endpoint = this.Endpoint,
                UserName = this.UserName,
                Password = this.Password,
                ApplicationName = this.ApplicationName,
                TrustedToken = this.TrustedToken,
                ThrowExceptionOnApiFailure = this.ThrowExceptionOnApiFailure,
                ThrowExceptionOnAuthFailure = this.ThrowExceptionOnAuthFailure
            };
        }

    }
}
