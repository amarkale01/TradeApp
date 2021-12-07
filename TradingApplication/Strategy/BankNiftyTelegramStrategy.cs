using AliceBlueWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using TeleSharp.TL;
using TeleSharp.TL.Messages;
using TLSharp.Core;
using Trading_App.Processor;

namespace TradingApplication.Strategy
{
    public delegate void ExecuteHandler(string value);
    public class BankNiftyTelegramStrategy : StrategyBase, IDisposable
    {
        Timer timer;
        APIProcessor apiProcessor;

        public event MessasgeHandler OnBuyExecuted;


        TelegramClient _client = null;
        int apiId = 2393640;
        string apiHash = "984b4fd591fe5dc67c8b3d5db5742ba4";
        int previousMessageId = 0;
        public string TelegramCode = "";

        public string CurrentBankNifty = string.Empty;

        public async override void Subscribe(APIProcessor apiProcessor)
        {
            this.apiProcessor = apiProcessor;
            timer = new Timer
            {
                Interval = 5000
            };


            await ConnectToTelegram();

            if (_client.IsConnected && _client.IsUserAuthorized())
                TimerStart();
            else
                StrategyBase_OnMessage("Client not authorized...");

        }

        protected override void StrategyBase_OnMessage(string value)
        {
            base.StrategyBase_OnMessage(value);
        }

        private void TimerStart()
        {

            timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            timer.Start();
        }

        private async Task ConnectToTelegram()
        {
            try
            {
                StrategyBase_OnMessage("Connecting to telegram...");

                if (_client == null)
                    _client = new TelegramClient(apiId, apiHash);

                await _client.ConnectAsync();

                if (_client.IsConnected)
                {
                    if (!_client.IsUserAuthorized())
                    {
                        //var code = "62337"; // you can change code in debugger //code will send via telegram to you

                        TLUser user = null;
                        try
                        {
                            StrategyBase_OnMessage("Authorizing user in telegram...");


                            var hash = await _client.SendCodeRequestAsync("+919960227388");

                            user = await _client.MakeAuthAsync("+919960227388", hash, TelegramCode);

                            StrategyBase_OnMessage("Connected to telegram...");
                        }
                        catch (Exception ex)
                        {
                            //if u activate two step verification in telegram
                            //var password = await _client.GetPasswordSetting();
                            //var password_str = "yourPassword";

                            //user = await _client.MakeAuthWithPasswordAsync(password, password_str);
                            StrategyBase_OnMessage("Error connecting to telegram - " + ex.Message);
                        }
                    }
                    else
                    {
                        StrategyBase_OnMessage("Connected to telegram...");
                    }


                }
            }
            catch (Exception ex)
            {
                if (timer != null)
                    timer.Stop();
            }
        }

        public async Task ReadMessageFromChannel()
        {

            if (_client != null && _client.IsConnected && _client.IsUserAuthorized())
            {

                var dialogs = (TLDialogs)await _client.GetUserDialogsAsync();
                var chat = dialogs.Chats
                    .OfType<TLChannel>()
                    .FirstOrDefault(c => c.Title == "Intraday Banknifty Call");//.FirstOrDefault(c => c.Title == "Trade Easy Free Calls");.FirstOrDefault(c => c.Title == "Trading Hub");.FirstOrDefault(c => c.Title == "TradingView");


                TLAbsMessages messages = await _client.GetHistoryAsync(new TLInputPeerChannel() { ChannelId = chat.Id, AccessHash = chat.AccessHash.Value }, limit: 1);


                var tlMessages = (TLChannelMessages)messages;

                foreach (TLMessage message in tlMessages.Messages.ToList())
                {
                    if (message != null)
                    {
                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
                        DateTime dtMessageDate = epoch.AddSeconds(message.Date);
                        TimeSpan messageMinDiffTimeSpan = DateTime.Now - dtMessageDate;

                        if (message.Id != previousMessageId && messageMinDiffTimeSpan.TotalSeconds <= 60)
                        {
                            previousMessageId = message.Id;
                            StrategyBase_OnMessage("message from telegram - " + message.Message);
                            if (!string.IsNullOrEmpty(message.Message) && message.Message.ToLower().IndexOf("buy nifty ") >= 0)
                            {
                                //await BuyBankNiftyTradeEaseyCalls(message.Message);
                                //await BuyNifty(message.Message);
                            }
                            else if (!string.IsNullOrEmpty(message.Message) && message.Message.ToLower().IndexOf("buy banknifty ") >= 0)
                            {
                                //await BuyBankNiftyTradeEaseyCalls(message.Message);
                                //await BuyBankNifty(message.Message);
                            }
                        }
                    }
                }

            }
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                string currentTime = DateTime.Now.ToString("hh:mm:ss");
                await ReadMessageFromChannel();
            }
            catch (Exception ex)
            {
                StrategyBase_OnMessage(ex.Message);
            }
        }

        public override void Unsubscribe()
        {
            if (timer != null)
                timer.Stop();

            if (_client != null)
                _client.Dispose();

            StrategyBase_OnMessage("Telegram strategy unsubscribed..");
        }

        public async Task BuyBankNiftyTradeEaseyCalls(string message)
        {
            try
            {
                //sample messages as below
                //message = "buy banknifty 35600 PE above 360 SL 320";
                //string msg = "buy banknifty 35600 PE @300-295 SL 280";
                //message = "buy bank nifty 33900 PE above 360 SL 320";

                string strike = string.Empty;
                string option = string.Empty;
                string buyPrice = string.Empty;
                string slPrice = string.Empty;

                if (message.ToLower().IndexOf("above") > 0)
                {
                    strike = message.Substring(14, 5);

                    option = message.Substring(20, 2);
                    buyPrice = message.Substring(29, 3);
                    //slPrice = message.Substring(message.Length - 3, 3);
                }
                else if (message.ToLower().IndexOf("@") > 0)
                {
                    strike = message.Substring(14, 5);
                    option = message.Substring(20, 2);
                    buyPrice = message.Substring(24, 3);
                    //slPrice = message.Substring(message.Length - 3, 3);
                }
                else if (message.ToLower().IndexOf("at") > 0)
                {
                    strike = message.Substring(15, 5);
                    option = message.Substring(21, 2);
                    //buyPrice = message.Substring(24, 3);
                    //slPrice = message.Substring(message.Length - 3, 3);
                }

                //int iBuyPrice = 0;
                //int.TryParse(buyPrice,out iBuyPrice); // get the buy price from message
                //iBuyPrice = iBuyPrice - 5;//reduce buy price by 5 points
                //buyPrice = iBuyPrice.ToString();
                //string stopLossPrice = (iBuyPrice - 35).ToString();
                //string targetPrice = (iBuyPrice + 50).ToString();

                if (!string.IsNullOrEmpty(option) && option.ToLower().IndexOf("ce") >= 0)
                {
                    apiProcessor.IsCEChecked = true;
                    apiProcessor.IsPEChecked = false;
                }
                else if (!string.IsNullOrEmpty(option) && option.ToLower().IndexOf("pe") >= 0)
                {
                    apiProcessor.IsCEChecked = false;
                    apiProcessor.IsPEChecked = true;
                }

                if (!string.IsNullOrEmpty(strike))
                {
                    //exit all orders
                    apiProcessor.ExitOrderRetryCount = 3;
                    await apiProcessor.ExitAllOrders("BankNifty");
                    System.Threading.Thread.Sleep(5000);

                    apiProcessor.IsStrangleChecked = false;
                    apiProcessor.Strike = Convert.ToInt32(strike);
                    apiProcessor.OTMDiff = 0;
                    apiProcessor.StopLossForOrder = "35";
                    apiProcessor.IsStopLossInPercent = false;
                    apiProcessor.TakeProfitForOrder = "35";
                    apiProcessor.Lots = 1;
                    apiProcessor.TransactionOrderType = "BUY";

                    await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                    System.Threading.Thread.Sleep(10000);
                    await apiProcessor.GetOrderHistory();
                    //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);
                    await apiProcessor.PlaceTargetOrder(apiProcessor.ExecutedOrders);

                    //await apiProcessor.PlaceEntryBracketOrder(buyPrice, stopLossPrice, targetPrice);

                    Dispatcher.CurrentDispatcher.Invoke(new ExecuteHandler(CallBuyExecuted), new object[] { "Executed BUY " + strike + " " + option });
                }
            }
            catch (Exception ex)
            {
                StrategyBase_OnMessage("Error executing buy order " + ex.Message);
            }
        }

        public async Task BuyBankNifty(string message)
        {
            try
            {
                //sample messages as below
                //message = "BUY BankNifty CE @32842";
                int strike = 0;
                string option = string.Empty;
                string buyPrice = string.Empty;
                string slPrice = string.Empty;
                
                string sCurrentValue = message.Substring(message.IndexOf('@') + 1, 5);

                option = message.Substring(14, 2);
                //buyPrice = message.Substring(29, 3);
                //slPrice = message.Substring(message.Length - 3, 3);

                if (message.ToLower().IndexOf("ce") >= 0)
                {
                    apiProcessor.IsCEChecked = true;
                    apiProcessor.IsPEChecked = false;
                    strike = GetBankNiftyStrike(sCurrentValue,"CE");
                }
                else if (message.ToLower().IndexOf("pe") >= 0)
                {
                    apiProcessor.IsCEChecked = false;
                    apiProcessor.IsPEChecked = true;
                    strike = GetBankNiftyStrike(sCurrentValue,"PE");
                }


                if (strike != 0)
                {
                    //exit all orders
                    apiProcessor.ExitOrderRetryCount = 3;
                    await apiProcessor.ExitAllOrders("BankNifty");
                    System.Threading.Thread.Sleep(3000);

                    apiProcessor.IsStrangleChecked = false;
                    apiProcessor.Strike = strike;
                    apiProcessor.OTMDiff = 0;
                    apiProcessor.StopLossForOrder = "50";
                    apiProcessor.IsStopLossInPercent = false;
                    apiProcessor.TakeProfitForOrder = "35";
                    apiProcessor.Lots = 1;
                    apiProcessor.TransactionOrderType = "BUY";

                    await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                    //System.Threading.Thread.Sleep(10000);
                    //await apiProcessor.GetOrderHistory();
                    //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);
                    //await apiProcessor.PlaceTargetOrder(apiProcessor.ExecutedOrders);

                    //Dispatcher.CurrentDispatcher.Invoke(new ExecuteHandler(CallBuyExecuted), new object[] { "Executed BUY " + strike + " " + option });
                }
            }
            catch (Exception ex)
            {
                StrategyBase_OnMessage("Error executing buy order " + ex.Message);
            }
        }

        public async Task BuyNifty(string message)
        {
            try
            {
                //sample messages as below
                //message = "buy banknifty 35600 PE above 360 SL 320";
                //string msg = "buy banknifty 35600 PE @300-295 SL 280";

                int strike = 0;
                string option = string.Empty;
                string buyPrice = string.Empty;
                string slPrice = string.Empty;

                //message = "BankNifty BUY CE";
                //message = "BUY Nifty CE @14520.56";

                string sCurrentValue = message.Substring(message.IndexOf('@') + 1,5);

                option = message.Substring(10, 2);
                //buyPrice = message.Substring(29, 3);
                //slPrice = message.Substring(message.Length - 3, 3);

                if (message.ToLower().IndexOf("ce") >= 0)
                {
                    apiProcessor.IsCEChecked = true;
                    apiProcessor.IsPEChecked = false;
                    strike = GetNiftyStrike(sCurrentValue, option);
                }
                else if (message.ToLower().IndexOf("pe") >= 0)
                {
                    apiProcessor.IsCEChecked = false;
                    apiProcessor.IsPEChecked = true;
                    strike = GetNiftyStrike(sCurrentValue, option);
                }


                if (strike != 0)
                {
                    //exit all orders
                    apiProcessor.ExitOrderRetryCount = 3;
                    await apiProcessor.ExitAllOrders("Nifty");
                    System.Threading.Thread.Sleep(3000);

                    apiProcessor.IsStrangleChecked = false;
                    apiProcessor.Strike = strike;
                    apiProcessor.OTMDiff = 0;
                    apiProcessor.StopLossForOrder = "30";
                    apiProcessor.IsStopLossInPercent = false;
                    apiProcessor.TakeProfitForOrder = "20";
                    apiProcessor.Lots = 1;
                    apiProcessor.TransactionOrderType = "BUY";

                    await apiProcessor.PlaceEntryOrder("NIFTY");
                    //System.Threading.Thread.Sleep(10000);
                    //await apiProcessor.GetOrderHistory();
                    //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);
                    //await apiProcessor.PlaceTargetOrder(apiProcessor.ExecutedOrders);

                    //Dispatcher.CurrentDispatcher.Invoke(new ExecuteHandler(CallBuyExecuted), new object[] { "Executed BUY " + strike + " " + option });
                }
            }
            catch (Exception ex)
            {
                StrategyBase_OnMessage("Error executing buy order " + ex.Message);
            }
        }

        private void CallBuyExecuted(string message)
        {
            OnBuyExecuted?.Invoke(message);
        }

        private int GetSTrike(string option)
        {
            int diffValue = 0;
            int strike = 0;

            if (!string.IsNullOrEmpty(CurrentBankNfity))
            {
                int iBankNifty = Round(CurrentBankNfity);
                DayOfWeek today = DateTime.Now.DayOfWeek;

                switch (today)
                {
                    case DayOfWeek.Friday:
                        diffValue = 300;
                        break;
                    case DayOfWeek.Monday:
                        diffValue = 200;
                        break;
                    case DayOfWeek.Tuesday:
                        diffValue = 100;
                        break;
                    case DayOfWeek.Wednesday:
                        diffValue = -100;
                        break;
                    case DayOfWeek.Thursday:
                        diffValue = -200;
                        break;
                    case DayOfWeek.Saturday:
                        diffValue = 100;
                        break;

                }

                if (option.ToLower().IndexOf("pe") >= 0)
                    diffValue = diffValue * -1;

                strike = iBankNifty + diffValue;

            }

            return strike;
        }

        private int GetBankNiftyStrike(string currentBankNfity, string option)
        {
            int diffValue = 0;
            int strike = 0;

            if (!string.IsNullOrEmpty(currentBankNfity))
            {
                int iBankNifty = Round(currentBankNfity);
                DayOfWeek today = DateTime.Now.DayOfWeek;

                switch (today)
                {
                    case DayOfWeek.Friday:
                        diffValue = 0;
                        break;
                    case DayOfWeek.Monday:
                        diffValue = 0;
                        break;
                    case DayOfWeek.Tuesday:
                        diffValue = 0;
                        break;
                    case DayOfWeek.Wednesday:
                        diffValue = -100;
                        break;
                    case DayOfWeek.Thursday:
                        diffValue = -100;
                        break;
                }

                if (option.ToLower().IndexOf("pe") >= 0)
                    diffValue = diffValue * -1;

                strike = iBankNifty + diffValue;

            }

            return strike;
        }

        private int GetNiftyStrike(string currentNfity,string option)
        {
            int strike = 0;
            int diffValue = 0;

            if (!string.IsNullOrEmpty(currentNfity))
            {
                strike = RoundNifty(currentNfity);
                DayOfWeek today = DateTime.Now.DayOfWeek;

                switch (today)
                {
                    case DayOfWeek.Friday:
                        diffValue = 0;
                        break;
                    case DayOfWeek.Monday:
                        diffValue = 0;
                        break;
                    case DayOfWeek.Tuesday:
                        diffValue = 0;
                        break;
                    case DayOfWeek.Wednesday:
                        diffValue = -50;
                        break;
                    case DayOfWeek.Thursday:
                        diffValue = -50;
                        break;

                }

                if (option.ToLower().IndexOf("pe") >= 0)
                    diffValue = diffValue * -1;

                strike = strike + diffValue;
            }

            return strike;
        }

        private int Round(string number)
        {
            int currentStrike = Convert.ToInt32(number);
            int nearest = 100;
            return (currentStrike + 5 * nearest / 10) / nearest * nearest;
        }

        private int RoundNifty(string number)
        {
            int currentStrike = Convert.ToInt32(number);
            int nearest = 50;
            return (currentStrike + 5 * nearest / 10) / nearest * nearest;
        }

        public void Dispose()
        {
            if (_client != null)
                _client.Dispose();
        }
    }
}
