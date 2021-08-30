using AliceBlueWrapper.Models;
using HtmlAgilityPack;
//using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AliceBlueWrapper
{
    public class AliceBlue : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly string baseUrl = "https://ant.aliceblueonline.com";

        List<Instrument> masterContact = new List<Instrument>();
        IDictionary<string, string> urls = new Dictionary<string, string>
        {
            ["authorize"] = "/oauth2/auth",
            ["token"] = "/oauth2/token",
            ["profile"] = "/api/v2/profile",
            ["balance"] = "/api/v2/cashposition",
            ["day_position"]= "/api/v2/positions?type=daywise",
            ["master_contract"] = "/api/v2/contracts.json?exchanges=NFO",
            ["place_order"] = "/api/v2/order",
            ["get_orders"] = "/api/v2/order",
            ["get_order_info"] = "/api/v2/order/{order_id}",
            ["modify_order"] = "/api/v2/order",
            ["cancel_order"] = "/api/v2/order?oms_order_id={0}&order_status=open",
            ["trade_book"] = "/api/v2/trade"
        };

        public AliceBlue()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(baseUrl);
        }
        public async Task<Token> LoginAndGetToken(LoginDetail login)//string client_id, string client_secret, string userId, string password)
        {
            try
            {
                string callback = "https://ant.aliceblueonline.com/plugin/callback";
                string authUrl = baseUrl + urls["authorize"] + $"?response_type=code&state=test12345&client_id={login.client_id}" +
                    $"&redirect_uri={callback}";

                var response = await Get(authUrl);

                string result = await response.Content.ReadAsStringAsync();

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(result);
                string _csrf_token = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='_csrf_token']").Attributes["value"].Value;
                string login_challenge = htmlDocument.DocumentNode.SelectSingleNode("//input[@name='login_challenge']").Attributes["value"].Value;


                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>
                {
                    ["login_challenge"] = login_challenge,
                    ["client_id"] = login.userId,
                    ["password"] = login.password,
                    ["_csrf_token"] = _csrf_token
                };
                response = await Post(response.RequestMessage.RequestUri.AbsoluteUri, keyValuePairs);

                keyValuePairs = new Dictionary<string, string>
                {
                    ["login_challenge"] = login_challenge,
                    ["question_id1"] = "11,6",
                    //["answer1"] = "a",
                    //["answer2"] = "a",
                    ["answer1"] = login.answer1,
                    ["answer2"] = login.answer2,
                    ["_csrf_token"] = _csrf_token
                };
                response = await Post(response.RequestMessage.RequestUri.AbsoluteUri, keyValuePairs);

                keyValuePairs = new Dictionary<string, string>
                {
                    ["scopes"] = "",
                    ["consent"] = "Authorize",
                    ["_csrf_token"] = _csrf_token
                };
                response = await Post(response.RequestMessage.RequestUri.AbsoluteUri, keyValuePairs);

                string code = response.RequestMessage.RequestUri.OriginalString;
                code = code.Substring(code.IndexOf('=') + 1);
                code = code.Substring(0, code.IndexOf('&'));

                //get token
                keyValuePairs = new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["redirect_uri"] = callback,
                    ["grant_type"] = "authorization_code",
                    ["cliend_id"] = login.client_id,
                    ["client_secret"] = login.client_secret,
                    ["_csrf_token"] = _csrf_token,
                    ["auth_scheme"] = "basic_auth"
                };
                string requestUrl = baseUrl + urls["token"] +
                    $"?client_id={login.client_id}&client_secret={login.client_secret}&grant_type=authorization_code&code={code}&redirect_uri={callback}&authorization_response={response.RequestMessage.RequestUri.AbsoluteUri}";
                var authValue = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login.client_id}:{login.client_secret}")));

                httpClient.DefaultRequestHeaders.Authorization = authValue;
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                response = await Post(requestUrl, keyValuePairs);
                result = await response.Content.ReadAsStringAsync();
                Token token = JsonConvert.DeserializeObject<Token>(result);
                return token;
            }
            catch (Exception)
            {
                throw new Exception("Login failed. Please check your credential.");
            }
        }

        public async Task<string> GetMasterContract()
        {
            string url = baseUrl + urls["master_contract"];
            var response = await Get(url);
            return await response.Content.ReadAsStringAsync();
        }
  
        public async Task<dynamic> GetProfile(string token)
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["profile"];
            var response = await Get(url);
            return await response.Content.ReadAsStringAsync();
        }

      

        public async Task<OrderResponse> PlaceOrder(string token, Order order)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["place_order"];
            IEnumerable<KeyValuePair<string, string>> keyvaluePairs = GetOrderData(order);
            var response = await Post(url, keyvaluePairs);
            string result = await response.Content.ReadAsStringAsync();
            OrderResponse orderRespose = JsonConvert.DeserializeObject<OrderResponse>(result);
            return orderRespose;
        }

        public async Task<OrderResponse> PlaceBracketOrder(string token, Order order)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["place_order"];
            IEnumerable<KeyValuePair<string, string>> keyvaluePairs = GetBracketOrderData(order);
            var response = await Post(url, keyvaluePairs);
            string result = await response.Content.ReadAsStringAsync();
            OrderResponse orderRespose = JsonConvert.DeserializeObject<OrderResponse>(result);
            return orderRespose;
        }


        private IEnumerable<KeyValuePair<string, string>> GetOrderData(Order order)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>
            {
                ["exchange"] = order.exchange,
                ["order_type"] = order.order_type,
                ["instrument_token"] = order.instrument_token,
                ["quantity"] = order.quantity,

                ["disclosed_quantity"] = "0",
                ["price"] = order.price,
                ["transaction_type"] = order.transaction_type,
                ["trigger_price"] = order.trigger_price,

                ["validity"] = "DAY",
                ["product"] = order.product,
                ["source"] = "web",
                ["order_tag"] = order.order_tag,

            };
            if(!string.IsNullOrEmpty( order.oms_order_id))
            {
                keyValuePairs.Add("oms_order_id", order.oms_order_id);
            }
            return keyValuePairs;
        }

        private IEnumerable<KeyValuePair<string, string>> GetBracketOrderData(Order order)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>
            {
                ["exchange"] = order.exchange,
                ["order_type"] = order.order_type,
                ["instrument_token"] = order.instrument_token,
                ["quantity"] = order.quantity,

                ["disclosed_quantity"] = "0",
                ["price"] = order.price,
                ["square_off_value"] = order.square_off_value,
                ["stop_loss_value"] = order.stop_loss_value,
                ["transaction_type"] = order.transaction_type,
                ["trigger_price"] = order.trigger_price,

                ["validity"] = "DAY",
                ["product"] = order.product,
                ["source"] = "web",
                ["order_tag"] = order.order_tag,
                

            };
            if (!string.IsNullOrEmpty(order.oms_order_id))
            {
                keyValuePairs.Add("oms_order_id", order.oms_order_id);
            }
            return keyValuePairs;
        }

        public async Task<CashDetail> GetCash(string token)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["balance"];
            var response = await Get(url);
            string result = await response.Content.ReadAsStringAsync();
            CashDetail cashDetail = JsonConvert.DeserializeObject<CashDetail>(result);
            return cashDetail;
        }
        public async Task<OrderHistory> GetOrderHistory(string token)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["get_orders"];
            var response = await Get(url);
             string result = await response.Content.ReadAsStringAsync();
            OrderHistory orderHistory = JsonConvert.DeserializeObject<OrderHistory>(result);
            return orderHistory;

        }
        public async Task<DayPosition> GetDayPosition(string token)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["day_position"];
            var response = await Get(url);
            string result = await response.Content.ReadAsStringAsync();
            DayPosition dayPosition = JsonConvert.DeserializeObject<DayPosition>(result);
            return dayPosition;           
        }

        public async Task<string> GetTradeBook(string token)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["trade_book"];
            var response = await Get(url);
            string result = await response.Content.ReadAsStringAsync();
            return result;

        }
        public async Task<dynamic> CancelOrder(string token,string orderId)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["cancel_order"];
            url = string.Format(url, orderId);
            var response = await Delete(url);
            return await response.Content.ReadAsStringAsync();
        }


        public async Task<string> ModifyOrder(string token,Order order)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string url = baseUrl + urls["place_order"];
            IEnumerable<KeyValuePair<string, string>> keyvaluePairs = GetOrderData(order);
            var response = await Put(url, keyvaluePairs);
            string result = await response.Content.ReadAsStringAsync();
            return result;
        }
        /// <summary>
        /// Get call
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Get(string url)
        {
            return await Request(HttpMethod.Get, url);
        }
        /// <summary>
        /// Get call
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Delete(string url)
        {
            return await Request(HttpMethod.Delete, url);
        }

        public Task<HttpResponseMessage> Put(string url, IEnumerable<KeyValuePair<string, string>> json)
        {
            FormUrlEncodedContent data = null;
            if (json != null)
                data = new FormUrlEncodedContent(json);
            return Request(HttpMethod.Put, url, data);
        }
        /// <summary>
        /// Post Call
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> Post(string url, IEnumerable<KeyValuePair<string, string>> json)
        {
            FormUrlEncodedContent data = null;
            if (json != null)
                data =  new FormUrlEncodedContent(json);
            return Request(HttpMethod.Post, url, data);
        }

        public async Task<HttpResponseMessage> Request(HttpMethod httpMethod, string url, FormUrlEncodedContent data = null)
        {
            try
            {

                var httpRequestMessage = new HttpRequestMessage(httpMethod, new Uri(url));
                httpRequestMessage.Content = data;
                var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false); ;
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
