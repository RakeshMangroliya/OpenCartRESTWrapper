using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using OpenCartNET.Base;

namespace OpenCartNET
{
    public class RestAPI
    {
        private string wc_url = string.Empty;
        private string wc_authkey = "";
        private string wc_authvalue = "";
        //private bool wc_Proxy = false;

        private bool AuthorizedHeader { get; set; }

        private Func<string, string> jsonSeFilter;
        private Func<string, string> jsonDeseFilter;
        private Action<HttpWebRequest> webRequestFilter;

        /// <summary>
        /// Initialize the RestAPI object
        /// </summary>
        /// <param name="url">WooCommerce REST API URL, e.g.: http://yourstore/wp-json/wc/v1/ </param>
        /// <param name="key">WooCommerce REST API Key</param>
        /// <param name="secret">WooCommerce REST API Secret</param>
        /// <param name="authorizedHeader">WHEN using HTTPS, do you prefer to send the Credentials in HTTP HEADER?</param>
        /// <param name="jsonSerializeFilter">Provide a function to modify the json string after serilizing.</param>
        /// <param name="jsonDeserializeFilter">Provide a function to modify the json string before deserilizing.</param>
        /// <param name="requestFilter">Provide a function to modify the HttpWebRequest object.</param>
        public RestAPI(string url, string authKey,string authkeyValue, bool authorizedHeader = true, 
                            Func<string, string> jsonSerializeFilter = null, 
                            Func<string, string> jsonDeserializeFilter = null, 
                            Action<HttpWebRequest> requestFilter = null)//, bool useProxy = false)
        {
            wc_url = url;
            wc_authkey = authKey;
            wc_authvalue = authkeyValue;
            AuthorizedHeader = authorizedHeader;
            
            //if ((url.ToLower().Contains("wc-api/v3") || !IsLegacy) && !wc_url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            //    wc_secret = secret + "&";
            //else
            //    wc_secret = secret;

            jsonSeFilter = jsonSerializeFilter;
            jsonDeseFilter = jsonDeserializeFilter;
            webRequestFilter = requestFilter;

            //wc_Proxy = useProxy;
        }

        

        public bool IsLegacy
        {
            get
            {
                return false;
            }
        }

        public string Url { get { return wc_url; } }

        /// <summary>
        /// Make Restful calls
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="method">HEAD, GET, POST, PUT, PATCH, DELETE</param>
        /// <param name="requestBody">If your call doesn't have a body, please pass string.Empty, not null.</param>
        /// <param name="parms"></param>
        /// <returns>json string</returns>
        public async Task<string> SendHttpClientRequest<T>(string endpoint, RequestMethod method, T requestBody, Dictionary<string, string> parms = null)
        {
            HttpWebRequest httpWebRequest = null;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                //if (wc_url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                //{
                    if (AuthorizedHeader)
                    {
                        httpWebRequest = (HttpWebRequest)WebRequest.Create(wc_url + GetOAuthEndPoint(method.ToString(), endpoint, parms));
                        httpWebRequest.Headers.Add(wc_authkey, wc_authvalue);
                        //httpWebRequest.Headers.Add(HttpRequestHeader.Accept, "application/json");
                       //httpWebRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(wc_key + ":" + wc_secret));

                    }
                    //else
                    //{
                    //    //if (parms == null) parms = new Dictionary<string, string>();
                    //    //parms.Add("consumer_key", wc_key);
                    //    //parms.Add("consumer_secret", wc_secret);

                    //    //httpWebRequest = (HttpWebRequest)WebRequest.Create(wc_url + GetOAuthEndPoint(method.ToString(), endpoint, parms));
                    //}
                //}
                //else
                //    httpWebRequest = (HttpWebRequest)WebRequest.Create(wc_url + GetOAuthEndPoint(method.ToString(), endpoint, parms));

                // start the stream immediately
                httpWebRequest.Method = method.ToString();
                httpWebRequest.AllowReadStreamBuffering = false;
                httpWebRequest.UserAgent = "OrdoriteApplication/1.0 (Living Space Furniture)";

                if (webRequestFilter != null)
                    webRequestFilter.Invoke(httpWebRequest);

                //if (wc_Proxy)
                //    httpWebRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;
                //else
                //    httpWebRequest.Proxy = null;

                if (requestBody.GetType() != typeof(string))
                {
                    httpWebRequest.ContentType = "application/json";
                    var buffer = Encoding.UTF8.GetBytes(SerializeJSon(requestBody));
                    Stream dataStream = await httpWebRequest.GetRequestStreamAsync();
                    dataStream.Write(buffer, 0, buffer.Length);
                }
                
                // asynchronously get a response
                WebResponse wr = await httpWebRequest.GetResponseAsync();
                return await GetStreamContent(wr.GetResponseStream(), wr.ContentType.Split('=')[1]);
            }
            catch (WebException we)
            {
                if (httpWebRequest != null && httpWebRequest.HaveResponse)
                    if (we.Response != null)
                        throw new Exception(await GetStreamContent(we.Response.GetResponseStream(), we.Response.ContentType.Split('=')[1]));
                    else
                        throw we;
                else
                    throw we;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public async Task<string> GetRestful(string endpoint, Dictionary<string, string> parms = null)
        {
            return await SendHttpClientRequest(endpoint, RequestMethod.GET, string.Empty, parms);
        }

        public async Task<string> PostRestful(string endpoint, object jsonObject, Dictionary<string, string> parms = null)
        {
            return await SendHttpClientRequest(endpoint, RequestMethod.POST, jsonObject, parms);
        }

        public async Task<string> PutRestful(string endpoint, object jsonObject, Dictionary<string, string> parms = null)
        {
            return await SendHttpClientRequest(endpoint, RequestMethod.PUT, jsonObject, parms);
        }

        public async Task<string> DeleteRestful(string endpoint, Dictionary<string, string> parms = null)
        {
            return await SendHttpClientRequest(endpoint, RequestMethod.DELETE, string.Empty, parms);
        }

        private string GetOAuthEndPoint(string method, string endpoint, Dictionary<string, string> parms = null)
        {
            if (parms == null)
                return "?" + endpoint;
            
            Dictionary<string, string> dic = new Dictionary<string, string>();

            if (parms != null)
                foreach (var p in parms)
                    dic.Add(p.Key, p.Value);

            string parmstr = string.Empty;
            foreach (var parm in dic)
                parmstr += parm.Key + "=" + Uri.EscapeDataString(parm.Value) + "&";

            return "?" + endpoint + "&" + parmstr.TrimEnd('&');
        }
        
        private async Task<string> GetStreamContent(Stream s, string charset)
        {
            StringBuilder sb = new StringBuilder();
            byte[] Buffer = new byte[512];
            int count = 0;
            count = await s.ReadAsync(Buffer, 0, Buffer.Length);
            while (count > 0)
            {
                sb.Append(Encoding.GetEncoding(charset).GetString(Buffer, 0, count));
                count = await s.ReadAsync(Buffer, 0, Buffer.Length);
            }

            return sb.ToString();
        }

        public string SerializeJSon<T>(T t)
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings()
            {
                DateTimeFormat = new DateTimeFormat(DateTimeFormat),
                UseSimpleDictionaryFormat = true
            };
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(T), settings);
            ds.WriteObject(stream, t);
            byte[] data = stream.ToArray();
            string jsonString = Encoding.UTF8.GetString(data, 0, data.Length);

            if (IsLegacy)
                if (typeof(T).IsArray)
                    jsonString = "{\"" + typeof(T).Name.ToLower().Replace("[]", "s") + "\":" + jsonString + "}";
                else
                    jsonString = "{\"" + typeof(T).Name.ToLower() + "\":" + jsonString + "}";

            stream.Dispose();

            if (jsonSeFilter != null)
                jsonString = jsonSeFilter.Invoke(jsonString);

            return jsonString;
        }

        public T DeserializeJSon<T>(string jsonString)
        {
            if (jsonDeseFilter != null)
                jsonString = jsonDeseFilter.Invoke(jsonString);

            Type dT = typeof(T);
            
            if (dT.Name.EndsWith("List"))
                dT = dT.GetTypeInfo().DeclaredProperties.First().PropertyType.GenericTypeArguments[0];

            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings()
            {
                DateTimeFormat = new DateTimeFormat(DateTimeFormat),
                UseSimpleDictionaryFormat = true
            };
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T), settings);
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            T obj = (T)ser.ReadObject(stream);
            stream.Dispose();

            return obj;
        }

        public string DateTimeFormat
        {
            get
            {
                return IsLegacy ? "yyyy-MM-ddTHH:mm:ssZ" : "yyyy-MM-ddTHH:mm:ss";
            }
        }
    }

    public enum RequestMethod
    {
        HEAD = 1,
        GET = 2,
        POST = 3,
        PUT = 4,
        PATCH = 5,
        DELETE = 6
    }
}
