using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Config;
using Newtonsoft.Json.Linq;
using uwpstoriesService.DataObjects;

namespace uwpstoriesService.Controllers
{
    [MobileAppController]
    public class UserInfoController : ApiController
    {
        const string TwitterConsumerKey = "<YOUR TWITTER CONSUMER KEY HERE>";
        const string TwitterConsumerSecret = "<YOUR TWITTER CONSUMER SECRET HERE>";

        public async Task<UserInfo> GetUserInfo()
        {

            string provider = "";
            string secret;
            string accessToken = GetAccessToken(out provider, out secret);

            UserInfo info = new UserInfo();
            switch (provider)
            {
                case "facebook":
                    using (HttpClient client = new HttpClient())
                    {
                        using (
                            HttpResponseMessage response =
                                await
                                    client.GetAsync("https://graph.facebook.com/me" + "?access_token=" +
                                                    accessToken))
                        {
                            var o = JObject.Parse(await response.Content.ReadAsStringAsync());
                            info.Name = o["name"].ToString();
                        }
                        using (
                            HttpResponseMessage response =
                                await
                                    client.GetAsync("https://graph.facebook.com/me" +
                                                    "/picture?redirect=false&access_token=" + accessToken))
                        {
                            var x = JObject.Parse(await response.Content.ReadAsStringAsync());
                            info.ImageUri = (x["data"]["url"].ToString());
                        }
                    }
                    break;
                case "google":
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Authorization =
                            AuthenticationHeaderValue.Parse("Bearer " + accessToken);
                        using (
                            HttpResponseMessage response =
                                await
                                    client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo"))
                        {
                            var o = JObject.Parse(await response.Content.ReadAsStringAsync());
                            info.Name = o["name"].ToString();
                            info.ImageUri = (o["picture"].ToString());
                        }
                    }
                    break;
                case "twitter":

                    //generating signature as of https://dev.twitter.com/oauth/overview/creating-signatures
                    string nonce = GenerateNonce();
                    string s = "oauth_consumer_key=" + TwitterConsumerKey + "&oauth_nonce=" +
                               nonce +
                               "&oauth_signature_method=HMAC-SHA1&oauth_timestamp=" +
                               DateTimeToUnixTimestamp(DateTime.Now) + "&oauth_token=" + accessToken +
                               "&oauth_version=1.0";
                    string sign = "GET" + "&" +
                                  Uri.EscapeDataString("https://api.twitter.com/1.1/account/verify_credentials.json") +
                                  "&" + Uri.EscapeDataString(s);
                    string sec = Uri.EscapeDataString(TwitterConsumerSecret) + "&" + Uri.EscapeDataString(secret);
                    byte[] key = Encoding.ASCII.GetBytes(sec);
                    string signature = Uri.EscapeDataString(Encode(sign, key));

                    using (HttpClient client = new HttpClient())
                    {

                        client.DefaultRequestHeaders.Authorization =
                            AuthenticationHeaderValue.Parse("OAuth oauth_consumer_key =\"" + TwitterConsumerKey +
                                                            "\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"" +
                                                            DateTimeToUnixTimestamp(DateTime.Now) + "\",oauth_nonce=\"" +
                                                            nonce +
                                                            "\",oauth_version=\"1.0\",oauth_token=\"" + accessToken +
                                                            "\",oauth_signature =\"" + signature + "\"");
                        using (
                            HttpResponseMessage response =
                                await
                                    client.GetAsync("https://api.twitter.com/1.1/account/verify_credentials.json"))
                        {
                            var o = JObject.Parse(await response.Content.ReadAsStringAsync());
                            info.Name = o["name"].ToString();
                            info.ImageUri = (o["profile_image_url"].ToString());
                        }
                    }
                    break;
                case "microsoftaccount":
                    using (HttpClient client = new HttpClient())
                    {
                        using (
                            HttpResponseMessage response =
                                await
                                    client.GetAsync("https://apis.live.net/v5.0/me" + "?access_token=" +
                                                    accessToken))
                        {
                            var o = JObject.Parse(await response.Content.ReadAsStringAsync());
                            info.Name = o["name"].ToString();
                        }
                    }
                    using (HttpClient client = new HttpClient())
                    {
                        using (
                            HttpResponseMessage response =
                                await
                                    client.GetAsync("https://apis.live.net/v5.0/me" +
                                                    "/picture?suppress_redirects=true&type=medium&access_token=" +
                                                    accessToken))
                        {
                            var o = JObject.Parse(await response.Content.ReadAsStringAsync());
                            info.ImageUri = o["location"].ToString();
                        }
                    }
                    break;
            }

            return info;
        }

        private string GetAccessToken(out string provider, out string secret)
        {
            var serviceUser = this.User as ClaimsPrincipal;
            var ident = serviceUser.FindFirst("http://schemas.microsoft.com/identity/claims/identityprovider").Value;
            string token = "";
            secret = "";
            provider = ident;
            switch (ident)
            {
                case "facebook":
                    token = Request.Headers.GetValues("X-MS-TOKEN-FACEBOOK-ACCESS-TOKEN").FirstOrDefault();
                    break;
                case "google":
                    token = Request.Headers.GetValues("X-MS-TOKEN-GOOGLE-ACCESS-TOKEN").FirstOrDefault();
                    break;
                case "microsoftaccount":
                    token = Request.Headers.GetValues("X-MS-TOKEN-MICROSOFTACCOUNT-ACCESS-TOKEN").FirstOrDefault();
                    break;
                case "twitter":
                    token = Request.Headers.GetValues("X-MS-TOKEN-TWITTER-ACCESS-TOKEN").FirstOrDefault();
                    secret = Request.Headers.GetValues("X-MS-TOKEN-TWITTER-ACCESS-TOKEN-SECRET").FirstOrDefault();
                    break;
            }
            return token;
        }

        public static string Encode(string input, byte[] key)
        {
            HMACSHA1 myhmacsha1 = new HMACSHA1(key);
            byte[] byteArray = Encoding.ASCII.GetBytes(input);
            MemoryStream stream = new MemoryStream(byteArray);
            return Convert.ToBase64String(myhmacsha1.ComputeHash(stream));
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long) (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                           new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        }

        public static string GenerateNonce()
        {
            return new Random()
                .Next(123400, int.MaxValue)
                .ToString("X");
        }

    }
}
