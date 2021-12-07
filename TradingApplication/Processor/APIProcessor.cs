using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Threading;using System.Threading.Tasks;using System.Windows.Threading;using AliceBlueWrapper;using AliceBlueWrapper.Models;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Trading_App.Model;namespace Trading_App.Processor{    public delegate void OnLogHandler(string logMessage);    public class APIProcessor : IDisposable    {        #region Private Variables        private List<Order> pendingOrders;        private List<Order> completedOrder;        private AliceBlue aliceBlue;        private TradeSetting tradeSetting;        private List<Instrument> masterContact;        private Helper helper;        #endregion        public bool IsStrangleChecked = false;        public int OTMDiff = 0;        public int Lots = 1;        public int ExitOrderRetryCount = 3;        public event OnLogHandler LogAdded;        public int Strike { get; set; }        public CashDetail Cash { get; set; }        public bool IsCEChecked = false;        public bool IsPEChecked = false;        public string StopLossForOrder;        public bool IsStopLossInPercent = true;        public string TransactionOrderType;        public string TakeProfitForOrder;        public List<OrderResponse> ExecutedOrders { get; set; }        public APIProcessor(TradeSetting tradeSetting,Helper helper)        {            aliceBlue = new AliceBlue();            this.tradeSetting = tradeSetting;            this.helper = helper;            masterContact = new List<Instrument>();            ExecutedOrders = new List<OrderResponse>();            StopLossForOrder = tradeSetting.StopLossPercentage;            TransactionOrderType = TransactionType.Buy;        }        public void LoadMasterContract()        {            try            {                string masterContractJson =                System.IO.File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\Data\\contract.json");                JToken jToken = JToken.Parse(masterContractJson).SelectToken("$.NSE-OPT");                masterContact = JsonConvert.DeserializeObject<List<Instrument>>(jToken.ToString());            }            catch (Exception ex)            {                LogAdded("Error while loading master contract.");                LogAdded(ex.Message);            }        }        public async Task GetMasterContract()        {            try            {                string masterContractJson = string.Empty;                masterContractJson = await aliceBlue.GetMasterContract();                System.IO.File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\Data\\contract.json", masterContractJson);                if (!string.IsNullOrEmpty(masterContractJson))                {                    LogAdded("Master contract downloaded successfully.");                    LoadMasterContract();                }                else                    LogAdded("Error Master contract download failed.");            }            catch (Exception ex)            {                LogAdded(ex.Message);            }        }        public async Task Login()        {            try            {                LoginDetail loginDetail = new LoginDetail                {                                        client_id = tradeSetting.ClientId,                    client_secret = tradeSetting.ClientSecret,                    userId = tradeSetting.UserId,                    password = tradeSetting.Password,                    answer1 = tradeSetting.Answer1,                    answer2 = tradeSetting.Answer2                };                Token token = await aliceBlue.LoginAndGetToken(loginDetail);                if (token.AccessToken != string.Empty)                {                    tradeSetting.Token = token.AccessToken;                    helper.UpdateConfigKey("Token", token.AccessToken);                    helper.UpdateConfigKey("TokenCreatedOn", DateTime.Now.ToString());                    LogAdded("Token created successfully");                }                else                    LogAdded("Access token is empty.");            }            catch (Exception ex)            {                LogAdded(ex.Message);            }        }        public async Task PlaceEntryOrder(string symbol)        {            try            {                OrderResponse response = null;                int ceStrike = Strike;                int peStrike = Strike;                int qty = 0;                if (IsStrangleChecked)                {                    ceStrike = Strike + OTMDiff;                    peStrike = Strike - OTMDiff;                }                if (!string.IsNullOrEmpty(symbol) && symbol.ToLower().StartsWith("banknifty"))
                    qty = 25;
                else
                    qty = 50;
                //place CE order
                if (IsCEChecked)                {                    response = await aliceBlue.PlaceOrder(tradeSetting.Token,                        new Order                        {                            order_type = OrderType.Market,
                            //instrument_token = GetInstrumentTokenForFNO($"NIFTY {tradeSetting.ExpiryWeek} {ceStrike}.0 CE"),
                            instrument_token = GetInstrumentTokenForFNO($"{symbol} {tradeSetting.ExpiryWeek} {ceStrike}.0 CE"),                            quantity = (qty * Lots).ToString(),                            transaction_type = TransactionOrderType,                            product = ProductType.Intraday                        });                    ExecutedOrders.Add(response);
                    //LogAdded($"NIFTY {tradeSetting.ExpiryWeek} {ceStrike} CE  : {response.message} status: {response.status}");
                    LogAdded($"{symbol} {tradeSetting.ExpiryWeek} {ceStrike} CE  : {response.message} status: {response.status}");                }                //place PE order                if (IsPEChecked)                {                    response = await aliceBlue.PlaceOrder(tradeSetting.Token,                     new Order                     {                         order_type = OrderType.Market,                         //instrument_token = GetInstrumentTokenForFNO($"NIFTY {tradeSetting.ExpiryWeek} {peStrike}.0 PE"),                         instrument_token = GetInstrumentTokenForFNO($"{symbol} {tradeSetting.ExpiryWeek} {peStrike}.0 PE"),                         quantity = (qty * Lots).ToString(),                         transaction_type = TransactionOrderType,                         product = ProductType.Intraday                     });                    ExecutedOrders.Add(response);                    //LogAdded($"NIFTY {tradeSetting.ExpiryWeek} {peStrike} PE  : {response.message} status: {response.status}");                    LogAdded($"{symbol} {tradeSetting.ExpiryWeek} {peStrike} PE  : {response.message} status: {response.status}");                }            }            catch (Exception ex)            {                LogAdded("PlaceEntryOrder failed");                LogAdded(ex.Message);            }        }        public async Task PlaceEntryBracketOrder(string orderPrice,string stoplossPrice,string targetPrice)        {            try            {                OrderResponse response = null;                int ceStrike = Strike;                int peStrike = Strike;                if (IsStrangleChecked)                {                    ceStrike = Strike + OTMDiff;                    peStrike = Strike - OTMDiff;                }

                //place CE order
                if (IsCEChecked)                {                    response = await aliceBlue.PlaceBracketOrder(tradeSetting.Token,                        new Order                        {                            order_type = OrderType.Limit,
                            //instrument_token = GetInstrumentTokenForFNO($"NIFTY {tradeSetting.ExpiryWeek} {ceStrike}.0 CE"),
                            instrument_token = GetInstrumentTokenForFNO($"BANKNIFTY {tradeSetting.ExpiryWeek} {ceStrike}.0 CE"),                            quantity = (Convert.ToInt32(tradeSetting.Quantity) * Lots).ToString(),                            transaction_type = TransactionOrderType,                            product = ProductType.Intraday,                            price = orderPrice,                            square_off_value = targetPrice,                            stop_loss_value = stoplossPrice,                        });                    ExecutedOrders.Add(response);
                    //LogAdded($"NIFTY {tradeSetting.ExpiryWeek} {ceStrike} CE  : {response.message} status: {response.status}");
                    LogAdded($"BANKNIFTY {tradeSetting.ExpiryWeek} {ceStrike} CE  : {response.message} status: {response.status}");                }
                //place PE order
                if (IsPEChecked)                {                    response = await aliceBlue.PlaceBracketOrder(tradeSetting.Token,                     new Order                     {                         order_type = OrderType.Limit,
                         //instrument_token = GetInstrumentTokenForFNO($"NIFTY {tradeSetting.ExpiryWeek} {peStrike}.0 PE"),
                         instrument_token = GetInstrumentTokenForFNO($"BANKNIFTY {tradeSetting.ExpiryWeek} {peStrike}.0 PE"),                         quantity = (Convert.ToInt32(tradeSetting.Quantity) * Lots).ToString(),                         transaction_type = TransactionOrderType,                         product = ProductType.Intraday,                         price = orderPrice,
                         square_off_value = targetPrice,
                         stop_loss_value = stoplossPrice,                     });                    ExecutedOrders.Add(response);
                    //LogAdded($"NIFTY {tradeSetting.ExpiryWeek} {peStrike} PE  : {response.message} status: {response.status}");
                    LogAdded($"BANKNIFTY {tradeSetting.ExpiryWeek} {peStrike} PE  : {response.message} status: {response.status}");                }            }            catch (Exception ex)            {                LogAdded("PlaceEntryOrder failed");                LogAdded(ex.Message);            }        }        public async Task PlaceStopLossOrder(List<OrderResponse> executedOrders,string symbol = "NIFTY")
        {
            try
            {
                OrderResponse response;

                foreach (OrderResponse orderResponse in executedOrders)
                {
                    Order order = completedOrder.SingleOrDefault(x => x.oms_order_id == orderResponse.data.oms_order_id
                                                                    && x.order_status == "complete");
                    if (order != null && order.trading_symbol.ToLower().StartsWith(symbol.ToLower()))
                    {
                        double.TryParse(order.average_price, out double orderPrice);

                        if (orderPrice > 0 && (order.order_type == OrderType.Market || order.order_type == OrderType.Limit))
                        {
                            string transactionType;
                            double.TryParse(StopLossForOrder, out double stopLoss);
                            if (order.transaction_type == TransactionType.Buy)
                            {
                                transactionType = TransactionType.Sell;
                                if (IsStopLossInPercent)
                                    orderPrice -= (orderPrice * (stopLoss / 100));
                                else
                                    orderPrice -= stopLoss;
                            }
                            else
                            {
                                transactionType = TransactionType.Buy;
                                if (IsStopLossInPercent)
                                    orderPrice += (orderPrice * (stopLoss / 100));
                                else
                                    orderPrice += stopLoss;
                            }
                            response = await aliceBlue.PlaceOrder(tradeSetting.Token,
                                new Order
                                {
                                    order_type = OrderType.StopLossLimit,
                                    instrument_token = order.instrument_token,
                                    quantity = order.quantity,
                                    transaction_type = transactionType,
                                    product = order.product,
                                    trigger_price = ((int)orderPrice).ToString(),
                                    price = ((int)orderPrice).ToString()
                                });
                        }
                        LogAdded($"StopLoss order for { order.trading_symbol} placed.");
                    }
                }
                executedOrders.Clear();
            }
            catch (Exception ex)
            {
                LogAdded("PlaceStopLossOrder failed");
                LogAdded(ex.Message);
            }
        }        public async Task PlaceStopLossForSELLOrder(List<OrderResponse> executedOrders, string symbol = "NIFTY")
        {
            try
            {
                OrderResponse response;

                foreach (OrderResponse orderResponse in executedOrders)
                {
                    Order order = completedOrder.SingleOrDefault(x => x.oms_order_id == orderResponse.data.oms_order_id
                                                                    && x.order_status == "complete");
                    if (order != null && order.trading_symbol.ToLower().StartsWith(symbol.ToLower()))
                    {
                        double.TryParse(order.average_price, out double orderPrice);

                        if (orderPrice > 0 && (order.order_type == OrderType.Market || order.order_type == OrderType.Limit))
                        {
                            string transactionType;
                            double.TryParse(StopLossForOrder, out double stopLoss);
                            if (order.transaction_type == TransactionType.Buy)
                            {
                                //transactionType = TransactionType.Sell;
                                //if (IsStopLossInPercent)
                                //    orderPrice -= (orderPrice * (stopLoss / 100));
                                //else
                                //    orderPrice -= stopLoss;
                                continue;
                            }
                            else
                            {
                                transactionType = TransactionType.Buy;
                                if (IsStopLossInPercent)
                                    orderPrice += (orderPrice * (stopLoss / 100));
                                else
                                    orderPrice += stopLoss;
                            }
                            response = await aliceBlue.PlaceOrder(tradeSetting.Token,
                                new Order
                                {
                                    order_type = OrderType.StopLossLimit,
                                    instrument_token = order.instrument_token,
                                    quantity = order.quantity,
                                    transaction_type = transactionType,
                                    product = order.product,
                                    trigger_price = ((int)orderPrice).ToString(),
                                    price = ((int)orderPrice).ToString()
                                });
                        }
                        LogAdded($"StopLoss order for { order.trading_symbol} placed.");
                    }
                }
                executedOrders.Clear();
            }
            catch (Exception ex)
            {
                LogAdded("PlaceStopLossOrder failed");
                LogAdded(ex.Message);
            }
        }        public async Task PlaceTargetOrder(List<OrderResponse> executedOrders)
        {
            try
            {
                OrderResponse response;

                foreach (OrderResponse orderResponse in executedOrders)
                {
                    Order order = completedOrder.SingleOrDefault(x => x.oms_order_id == orderResponse.data.oms_order_id
                                                                    && x.order_status == "complete");
                    if (order != null)
                    {
                        double.TryParse(order.average_price, out double orderPrice);

                        if (orderPrice > 0 && (order.order_type == OrderType.Market || order.order_type == OrderType.Limit))
                        {
                            string transactionType;
                            double.TryParse(TakeProfitForOrder, out double takeProfit);
                            if (order.transaction_type == TransactionType.Buy)
                            {
                                transactionType = TransactionType.Sell;
                                orderPrice += takeProfit;
                            }
                            else
                            {
                                transactionType = TransactionType.Buy;
                                orderPrice -= takeProfit;
                            }
                            response = await aliceBlue.PlaceOrder(tradeSetting.Token,
                                new Order
                                {
                                    order_type = OrderType.StopLossLimit,
                                    instrument_token = order.instrument_token,
                                    quantity = order.quantity,
                                    transaction_type = transactionType,
                                    product = order.product,
                                    trigger_price = ((int)orderPrice).ToString(),
                                    price = ((int)orderPrice).ToString()
                                });
                        }
                        LogAdded($"Target order for { order.trading_symbol} placed.");
                    }
                }
                executedOrders.Clear();
            }
            catch (Exception ex)
            {
                LogAdded("PlaceTargetOrder failed");
                LogAdded(ex.Message);
            }
        }        public async Task GetOrderHistory()        {            try            {                OrderHistory orderHistory = await aliceBlue.GetOrderHistory(tradeSetting.Token);                pendingOrders = orderHistory.data.pending_orders;                completedOrder = orderHistory.data.completed_orders;            }            catch (Exception ex)            {                LogAdded("GetOrderHistory Failed");                LogAdded(ex.Message);            }        }        public async Task<DayPosition> GetDayPosition()        {            try            {                return await aliceBlue.GetDayPosition(tradeSetting.Token);            }            catch (Exception ex)            {                LogAdded("GetDayPostion failed");                throw ex;            }        }        //public async Task GetTradeBook()        //{        //    string tradeBook = await aliceBlue.GetTradeBook(tradeSetting.Token);        //}        //public async Task GetCashDetails()        //{        //    Cash = await aliceBlue.GetCash(tradeSetting.Token);        //}        public async Task CancelAllPendingOrder()        {            foreach (var order in pendingOrders)            {                await CancelOrder(order.oms_order_id);            }        }                public async Task ExitAllOrders(string symbol)        {            try            {                await ExitAllSELLOrders();                System.Threading.Thread.Sleep(10000);                await GetOrderHistory();                foreach (var order in pendingOrders)                {                    if(order.trading_symbol.ToLower().StartsWith(symbol.ToLower()))                        await CancelOrder(order.oms_order_id);                }                DayPosition dayPosition = await aliceBlue.GetDayPosition(tradeSetting.Token);                string transactionType = string.Empty;                foreach (Position position in dayPosition.data.positions)                {                    if (position.net_quantity != 0 && position.trading_symbol.ToLower().StartsWith(symbol.ToLower()))                    {                        if (position.net_quantity > 0)                            transactionType = TransactionType.Sell;                        else                            transactionType = TransactionType.Buy;                        Order squareoffOrder = new Order                        {                            order_type = OrderType.Market,                            instrument_token = position.instrument_token,                            transaction_type = transactionType,                            quantity = Math.Abs(position.net_quantity).ToString(),                            product = position.product,                        };                        await aliceBlue.PlaceOrder(tradeSetting.Token, squareoffOrder);                    }                }                LogAdded("Exited all pending order for " + symbol + ".");            }            catch (Exception ex)            {                LogAdded("ExitAllOrders failed");                if(ExitOrderRetryCount != 0)                {                    ExitOrderRetryCount--;                    Thread.Sleep(500);                    LogAdded("ExitAllOrders Retry");                    await ExitAllOrders(symbol);                }                LogAdded(ex.Message);            }        }        public async Task ExitAllSELLOrders()        {            try            {                await GetOrderHistory();                foreach (var order in pendingOrders)                {                    await CancelOrder(order.oms_order_id);                }                DayPosition dayPosition = await aliceBlue.GetDayPosition(tradeSetting.Token);                string transactionType = string.Empty;                foreach (Position position in dayPosition.data.positions)                {

                    if (position.net_quantity < 0)
                    {
                        transactionType = TransactionType.Buy;

                        Order squareoffOrder = new Order
                        {
                            order_type = OrderType.Market,
                            instrument_token = position.instrument_token,
                            transaction_type = transactionType,
                            quantity = Math.Abs(position.net_quantity).ToString(),
                            product = position.product,
                        };

                        await aliceBlue.PlaceOrder(tradeSetting.Token, squareoffOrder);
                    }

                }                LogAdded("Exited all SELL orders.");            }            catch (Exception ex)            {                LogAdded("ExitAllOrders failed");                //if (ExitOrderRetryCount != 0)                //{                //    ExitOrderRetryCount--;                //    Thread.Sleep(500);                //    LogAdded("ExitAllOrders Retry");                //    await ExitAllOrders();                //}                LogAdded(ex.Message);            }        }

        public async Task ExitCEOrders()        {            IEnumerable<Order> ceOrders = pendingOrders.Where(x => x.trading_symbol.ToLower().Contains("ce"));            foreach (var order in ceOrders)            {                //await CancelAndPlaceMarketOrder(order);            }        }                public async Task ExitPEOrders()        {            IEnumerable<Order> ceOrders = pendingOrders.Where(x => x.trading_symbol.ToLower().Contains("pe"));            foreach (var order in ceOrders)            {                // await CancelAndPlaceMarketOrder(order);            }        }        private async Task CancelOrder(string orderId)        {            try            {                await aliceBlue.CancelOrder(tradeSetting.Token, orderId);            }            catch (Exception ex)            {                LogAdded("CancelOrder failed");                LogAdded(ex.Message);            }        }                private string GetInstrumentTokenForFNO(string symbol)        {
            //NIFTY 06 May21 14500.0 CE
            Instrument ins = masterContact.Single(x => x.symbol.StartsWith(symbol));            return ins.code;        }        public Tick GetInstrumentTick(string symbol)        {
            //NIFTY 06 May21 14500.0 CE
            Instrument ins = masterContact.Single(x => x.symbol.StartsWith(symbol));            Tick tick = new Tick();            tick.InstrumentToken = uint.Parse(ins.code);            tick.Mode = Convert.ToString(ins.exchange_code);            tick.Symbol = ins.symbol;            return tick;        }

        public void Dispose()        {            aliceBlue.Dispose();        }            }}