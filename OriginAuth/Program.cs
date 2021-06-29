using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OriginAuth
{
    class Program
    {
        static public CookieContainer cc = new CookieContainer();
        public static Random rnd = new Random();
        private struct User_data
        {
            public string Login;
            public string Password;
        }
        private struct Request_headers
        {
            public string UserAgent;
            public string ContentType;
        }
        static void Main(string[] args)
        {
            /* YOUR LOGIN AND PASSWORD */
            User_data user_Data = new User_data { Login = "", Password = "" };
            Request_headers request_Headers = new Request_headers { UserAgent = "Mozilla/5.0 (Zeny; EA-Origin-Auth/1.0)", ContentType = "application/x-www-form-urlencoded" };

            // 1 - get selflocation //
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://accounts.ea.com/connect/auth?response_type=code" +
                "&client_id=ORIGIN_SPA_ID" +
                "&display=originXWeb%2Flogin" +
                "&locale=en_US" +
                "&release_type=prod" +
                "&redirect_uri=https%3A%2F%2Fwww.origin.com%2Fviews%2Flogin.html");
            req.Method = "GET";
            req.UserAgent = request_Headers.UserAgent;
            req.ContentType = request_Headers.ContentType;
            req.Headers.Add("Accept-Encoding: gzip, deflate");
            req.Headers.Add("Accept-Language: en-US");
            req.AllowAutoRedirect = true;

            req.CookieContainer = cc;
            HttpWebResponse response = (HttpWebResponse)req.GetResponse();

            string selflocation = null;
            foreach (string s in response.Headers.GetValues("selflocation"))
            {
                selflocation = s;
            }
            response.Close();

            // 2 - window.location //
            req = (HttpWebRequest)WebRequest.Create(selflocation);
            req.Method = "POST";
            req.UserAgent = request_Headers.UserAgent;
            req.ContentType = request_Headers.ContentType;
            req.AllowAutoRedirect = false;
            req.CookieContainer = cc;
            string FormParams = $"email={user_Data.Login}" +
                                $"&password={user_Data.Password}" +
                                "&_eventId=submit" +
                               $"&cid={New_cid()}" +
                                "&showAgeUp=true" +
                                "&googleCaptchaResponse=" +
                                "&_rememberMe=on";
            byte[] SomeBytes = Encoding.GetEncoding(1251).GetBytes(FormParams);
            req.ContentLength = SomeBytes.Length;
            Stream newStream = req.GetRequestStream();
            newStream.Write(SomeBytes, 0, SomeBytes.Length);
            newStream.Close();
            response = (HttpWebResponse)req.GetResponse();

            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string temp_page = reader.ReadToEnd();
            Match m1 = Regex.Match(temp_page,"window.location = \"([\\s\\S]+?)\"");
            stream.Close();
            reader.Close();

            // 3 - code //
            req = (HttpWebRequest)WebRequest.Create(m1.Groups[1].Value);
            req.Method = "GET";
            req.UserAgent = request_Headers.UserAgent;
            req.ContentType = request_Headers.ContentType;
            req.AllowAutoRedirect = false;
            req.CookieContainer = cc;
            response = (HttpWebResponse)req.GetResponse();
            response.Close();

            //string code = null;
            //foreach (string s in response.Headers.GetValues("location"))
            //{
            //    code = s;
            //}
            //m1 = Regex.Match(code,"\\?code=([\\s\\S]+)");
            //code = m1.Groups[1].Value;

            // 4 - Authorization //
            req = (HttpWebRequest)WebRequest.Create("https://accounts.ea.com/connect/auth?client_id=ORIGIN_JS_SDK&response_type=token&redirect_uri=nucleus%3Arest&prompt=none&release_type=prod");
            req.Method = "GET";
            req.UserAgent = request_Headers.UserAgent;
            req.ContentType = request_Headers.ContentType;
            req.AllowAutoRedirect = false;
            req.CookieContainer = cc;
            response = (HttpWebResponse)req.GetResponse();

            stream = response.GetResponseStream();
            reader = new StreamReader(stream);
            temp_page = reader.ReadToEnd();
            var define_token = new { access_token = "", token_type = "", expires_in = "" };
            var token_info = JsonConvert.DeserializeAnonymousType(temp_page, define_token);
            response.Close();
            reader.Close();
            stream.Close();
            Console.WriteLine($"Authorization: {token_info.token_type} {token_info.access_token}");
            Console.ReadLine();

            bool tests = false;
            if(tests)
            {
                // pids/me //
                req = (HttpWebRequest)WebRequest.Create("https://gateway.ea.com/proxy/identity/pids/me");
                req.Method = "GET";
                req.UserAgent = request_Headers.UserAgent;
                req.ContentType = request_Headers.ContentType;
                req.AllowAutoRedirect = true;
                req.CookieContainer = cc;
                req.Headers.Add($"Authorization: {token_info.token_type} {token_info.access_token}");
                response = (HttpWebResponse)req.GetResponse();

                stream = response.GetResponseStream();
                reader = new StreamReader(stream);
                temp_page = reader.ReadToEnd();

                var define_pid = new
                {
                    pid = new
                    {
                        externalRefType = "",
                        externalRefValue = "",
                        pidId = "",
                        email = "",
                        emailStatus = "",
                        strength = "",
                        dob = "",
                        country = "",
                        language = "",
                        locale = "",
                        status = "",
                        reasonCode = "",
                        tosVersion = "",
                        parentalEmail = "",
                        thirdPartyOptin = "",
                        globalOptin = "",
                        dateCreated = "",
                        dateModified = "",
                        lastAuthDate = "",
                        registrationSource = "",
                        authenticationSource = "",
                        showEmail = "",
                        discoverableEmail = "",
                        anonymousPid = "",
                        underagePid = "",
                        defaultBillingAddressUri = "",
                        defaultShippingAddressUri = "",
                        passwordSignature = ""
                    }
                };
                var pid_info = JsonConvert.DeserializeAnonymousType(temp_page, define_pid);
                Console.WriteLine(response.Headers);
                stream.Close();
                reader.Close();
                response.Close();
                Console.ReadLine();
            }
        }
        static string New_cid()
        {
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            string output = null;
            for(int i = 0; i < 32; i++)
            {
                output += chars[rnd.Next(0, 32)];
            }
            return output;
        }
    }
}
