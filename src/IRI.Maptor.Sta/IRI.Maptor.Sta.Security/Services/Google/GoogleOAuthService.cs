using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Contracts.Google;
using IRI.Maptor.Sta.Common.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Sta.Common.Services;

namespace IRI.Maptor.Sta.Spatial.Services.Google
{
    public static class GoogleOAuthService
    {

        const string _clientID = "525009601749-75ds9jtdar3pl2e5qn7hc58ooe8vqu6n.apps.googleusercontent.com";
        const string _clientSecret = "Ucjud5t8GxbHZyL2HBrofSS_";


        const string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        const string tokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";
        const string userInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";

        const string userinfoRequestURI = "https://www.googleapis.com/oauth2/v3/userinfo";
        const string tokenRequestURI = "https://www.googleapis.com/oauth2/v4/token";

        public static async Task<Response<GoogleOAuthUserInfoResult>> RunOAuth2()
        {
            // Generates state and PKCE values.
            string state = CryptographyHelper.GetRandomDataBase64url(32);

            string code_verifier = CryptographyHelper.GetRandomDataBase64url(32);

            string code_challenge = CryptographyHelper.GetBase64urlEncodeNoPadding(HashAlgorithmHelper.GetSha256(code_verifier));

            const string code_challenge_method = "S256";

            // Creates a redirect URI using an available port on the loopback address.
            string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, NetworkUtilities.GetRandomUnusedPort());
            //output("redirect URI: " + redirectURI);

            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectURI);
            //output("Listening..");
            http.Start();

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format("{0}?response_type=code&scope=email%20openid%20profile&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                authorizationEndpoint,
                System.Uri.EscapeDataString(redirectURI),
                _clientID,
                state,
                code_challenge,
                code_challenge_method);

            // Opens request in the browser.
            System.Diagnostics.Process.Start(authorizationRequest);

            // Waits for the OAuth authorization response.
            var context = await http.GetContextAsync();

            // Brings this app back to the foreground.
            //this.Activate();

            // Sends an HTTP response to the browser.
            var response = context.Response;
            string responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>");
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            {
                responseOutput.Close();
                http.Stop();
                //Console.WriteLine("HTTP server stopped.");
            });

            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                //output(String.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
                return ResponseFactory.CreateError<GoogleOAuthUserInfoResult>($"OAuth authorization error: {context.Request.QueryString.Get("error")}.");
            }
            if (context.Request.QueryString.Get("code") == null
                || context.Request.QueryString.Get("state") == null)
            {
                //output("Malformed authorization response. " + context.Request.QueryString);
                return ResponseFactory.CreateError<GoogleOAuthUserInfoResult>("Malformed authorization response. " + context.Request.QueryString);
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incoming_state = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incoming_state != state)
            {
                //output(String.Format("Received request with invalid state ({0})", incoming_state));
                return ResponseFactory.CreateError<GoogleOAuthUserInfoResult>($"Received request with invalid state ({incoming_state})");
            }
            //output("Authorization code: " + code);

            // Starts the code exchange at the Token Endpoint.
            return await PerformCodeExchange(code, code_verifier, redirectURI);
        }

        private static async Task<Response<GoogleOAuthUserInfoResult>> PerformCodeExchange(string code, string code_verifier, string redirectURI)
        {
            //("Exchanging code for tokens...");

            // builds the  request
            //string tokenRequestURI = "https://www.googleapis.com/oauth2/v4/token";
            string tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code",
                code,
                System.Uri.EscapeDataString(redirectURI),
                _clientID,
                code_verifier,
                _clientSecret
                );

            // sends the request
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(tokenRequestURI);
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = _byteVersion.Length;
            Stream stream = tokenRequest.GetRequestStream();
            await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            try
            {
                // gets the response
                WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    //output(responseText);

                    // converts to dictionary
                    Dictionary<string, string> tokenEndpointDecoded = JsonHelper.Deserialize<Dictionary<string, string>>(responseText);

                    string access_token = tokenEndpointDecoded["access_token"];

                    return await GetUserInfo(access_token);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        //output("HTTP: " + response.StatusCode);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = reader.ReadToEnd();

                            return ResponseFactory.CreateError<GoogleOAuthUserInfoResult>(responseText);
                            //output(responseText);
                        }
                    }

                }
            }

            return ResponseFactory.CreateError<GoogleOAuthUserInfoResult>(string.Empty);
        }
         
        private static async Task<Response<GoogleOAuthUserInfoResult>> GetUserInfo(string access_token)
        {
            //("Making API Call to Userinfo...");

            // builds the  request

            // sends the request
            HttpWebRequest userinfoRequest = (HttpWebRequest)WebRequest.Create(userinfoRequestURI);
            userinfoRequest.Method = "GET";
            userinfoRequest.Headers.Add(string.Format("Authorization: Bearer {0}", access_token));
            userinfoRequest.ContentType = "application/x-www-form-urlencoded";
            userinfoRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            // gets the response
            WebResponse userinfoResponse = await userinfoRequest.GetResponseAsync();

            string resultString = null;

            using (StreamReader userinfoResponseReader = new StreamReader(userinfoResponse.GetResponseStream()))
            {
                // reads response body
                //string userinfoResponseText = await userinfoResponseReader.ReadToEndAsync();
                //output(userinfoResponseText);

                resultString = await userinfoResponseReader.ReadToEndAsync();
            }

            try
            {
                return ResponseFactory.Create(JsonHelper.Deserialize<GoogleOAuthUserInfoResult>(resultString));
            }
            catch (Exception ex)
            {
                return ResponseFactory.CreateError<GoogleOAuthUserInfoResult>(ex.Message);
            }

        }

    }
}
