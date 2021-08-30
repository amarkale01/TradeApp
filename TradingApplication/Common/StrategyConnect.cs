using AliceBlueWrapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Trading_App.Processor;

namespace Trading_App.Common
{
    public class StrategyConnect : IDisposable
    {
        Timer timer;
        APIProcessor apiProcessor;
        
        public event OnErrorHandler LogMessage;

        public bool IsStraddle925Checked = false;
        private bool IsStraddle925Executed = false;

        public bool IsStraddle920Checked = false;
        private bool IsStraddle920Executed = false;

        public bool IsTwoStraddleChecked = false;
        private bool IsTwoStraddleExecuted = false;

        public string Strike = string.Empty;

        public bool IsBreakout930Checked = false;
        private bool IsBreakout930Executed = false;

        private int bankNifty930MinHigh = 0;
        private int bankNifty930MinLow = 0;

        public StrategyConnect(APIProcessor apiProcessor)
        {
            this.apiProcessor = apiProcessor;
            timer = new Timer
            {
                Interval = 5000
            };
        }

        public void Start()
        {
            TimerStart();
            int currentBankNifty = Convert.ToInt32(Strike);
            bankNifty930MinHigh = currentBankNifty;
            bankNifty930MinLow = currentBankNifty;

            IsBreakout930Checked = true;
            IsStraddle925Checked = true;
            LogMessage?.Invoke("Strategy connect started...");
        }

        public void TimerStart()
        {
            timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                string currentTime = DateTime.Now.ToString("hh:mm:ss");
                int currentBankNifty = Convert.ToInt32(Strike);

                if (DateTime.Now.Hour == 9 && DateTime.Now.Minute >= 15 && DateTime.Now.Minute <= 30)
                {
                    if (bankNifty930MinHigh < currentBankNifty)
                        bankNifty930MinHigh = currentBankNifty;

                    if (bankNifty930MinLow > currentBankNifty)
                        bankNifty930MinLow = currentBankNifty;
                }


                if (IsStraddle925Checked && !IsStraddle925Executed && string.Compare(currentTime, "09:42:00", true) == 0)
                {
                    // timer.Stop();

                    //Straddle_925AM(currentBankNifty);

                    //timer.Start();
                }

                if (IsStraddle920Checked && !IsStraddle920Executed && string.Compare(currentTime, "09:20:00", true) == 0)
                {
                    //IsStraddle920Executed = true;

                    // timer.Stop();

                    //Straddle_920AM(currentBankNifty);

                    //timer.Start();
                }

                if (IsTwoStraddleChecked && !IsTwoStraddleExecuted && string.Compare(currentTime, "09:45:00", true) == 0)
                {
                    IsTwoStraddleExecuted = true;

                     timer.Stop();

                    //TwoStraddle(currentBankNifty);

                    //timer.Start();
                }

                //if ((DateTime.Now.Hour >= 9 && DateTime.Now.Minute > 30) && IsBreakout930Checked && !IsBreakout930Executed && Is930Breakout(currentBankNifty))
                if ((DateTime.Now.Hour >= 9 && DateTime.Now.Minute > 30) && IsBreakout930Checked && !IsBreakout930Executed && Is930Breakout(currentBankNifty))
                {
                    //IsBreakout930Executed = true;
                    //timer.Stop();

                    //Breakout_930AM_SELL(currentBankNifty, IsHighBreak(currentBankNifty));

                    //Breakout_930AM_BUY(currentBankNifty, IsHighBreak(currentBankNifty));

                    //timer.Start();
                }


            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private bool Is930Breakout(int strike)
        {
            int buffer = 30;
            if (strike > (bankNifty930MinHigh + buffer))
                return true;

            if (strike < (bankNifty930MinLow - buffer))
                return true;

            return false;

        }

        private bool IsHighBreak(int strike)
        {
            int buffer = 30;
            if (strike > (bankNifty930MinHigh + buffer))
                return true;

            return false;

        }

        private async void Straddle_920AM(int strike)
        {
            try
            {
                if (strike == 0)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                IsStraddle920Executed = true;

                apiProcessor.IsCEChecked = true;
                apiProcessor.IsPEChecked = true;
                apiProcessor.IsStrangleChecked = false;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 0;
                apiProcessor.StopLossForOrder = "24";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.TransactionOrderType = "BUY";

                LogMessage?.Invoke("Executing 920 Two Straddle.. strike - " + apiProcessor.Strike);

                await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                //System.Threading.Thread.Sleep(10000);
                //await apiProcessor.GetOrderHistory();
                //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);

                //apiProcessor.IsCEChecked = true;
                //apiProcessor.IsPEChecked = true;
                //apiProcessor.IsStrangleChecked = true;
                //apiProcessor.Strike = Round(strike);
                //apiProcessor.OTMDiff = 200;
                //apiProcessor.StopLossForOrder = "24";
                //apiProcessor.IsStopLossInPercent = true;
                //apiProcessor.TransactionOrderType = "BUY";

                

                //await apiProcessor.PlaceEntryOrder();

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void TwoStraddle(int strike)
        {
            try
            {
                if (strike == 0)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                IsStraddle920Executed = true;

                apiProcessor.IsCEChecked = true;
                apiProcessor.IsPEChecked = true;
                apiProcessor.IsStrangleChecked = false;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 0;
                apiProcessor.StopLossForOrder = "24";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.TransactionOrderType = "SELL";

                LogMessage?.Invoke("Executing 920 Two Straddle.. strike - " + apiProcessor.Strike);

                //await apiProcessor.PlaceEntryOrder();
                //System.Threading.Thread.Sleep(10000);
                //await apiProcessor.GetOrderHistory();
                //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);

                apiProcessor.IsCEChecked = true;
                apiProcessor.IsPEChecked = true;
                apiProcessor.IsStrangleChecked = true;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 200;
                apiProcessor.StopLossForOrder = "24";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.TransactionOrderType = "SELL";

                //await apiProcessor.PlaceEntryOrder();

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void Straddle_925AM(int strike)
        {
            try
            {
                if (strike == 0)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                IsStraddle925Executed = true;

                apiProcessor.IsCEChecked = true;
                apiProcessor.IsPEChecked = true;
                apiProcessor.IsStrangleChecked = false;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 0;
                apiProcessor.StopLossForOrder = "24";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.TransactionOrderType = "SELL";

                LogMessage?.Invoke("Executing 925 Straddle.. strike - " + apiProcessor.Strike);

                //await apiProcessor.PlaceEntryOrder();
                //System.Threading.Thread.Sleep(10000);
                //await apiProcessor.GetOrderHistory();
                //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void Breakout_930AM_SELL(int strike,bool isHighBreak)
        {
            try
            {
                if (strike == 0)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                IsBreakout930Executed = true;

                if (isHighBreak)
                {
                    apiProcessor.IsCEChecked = false;
                    apiProcessor.IsPEChecked = true;
                }
                else
                {
                    apiProcessor.IsCEChecked = true;
                    apiProcessor.IsPEChecked = false;
                }

                apiProcessor.IsStrangleChecked = true;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 200;
                apiProcessor.StopLossForOrder = "24";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.TransactionOrderType = "SELL";

                LogMessage?.Invoke("Executing 930 breakout.. strike - " + apiProcessor.Strike + " Sell " + (apiProcessor.IsCEChecked?"CE":"PE"));

                //await apiProcessor.PlaceEntryOrder();
                //System.Threading.Thread.Sleep(10000);
                //await apiProcessor.GetOrderHistory();
                //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void Breakout_930AM_BUY(int strike, bool isHighBreak)
        {
            try
            {
                if (strike == 0)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                IsBreakout930Executed = true;

                if (isHighBreak)
                {
                    apiProcessor.IsCEChecked = true;
                    apiProcessor.IsPEChecked = false;
                }
                else
                {
                    apiProcessor.IsCEChecked = false;
                    apiProcessor.IsPEChecked = true;
                }

                apiProcessor.IsStrangleChecked = false;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 0;
                apiProcessor.StopLossForOrder = "24";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.Lots = 1;
                apiProcessor.TransactionOrderType = "BUY";

                LogMessage?.Invoke("Executing 930 breakout.. strike - " + apiProcessor.Strike + " BUY " + (apiProcessor.IsCEChecked ? "CE" : "PE"));

                await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                //System.Threading.Thread.Sleep(10000);
                //await apiProcessor.GetOrderHistory();
                //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private int Round(int number)
        {
            int nearest = 100;
            return (number + 5 * nearest / 10) / nearest * nearest;
        }

        public void Dispose()
        {
            if (timer != null)
                timer = null;
            apiProcessor.Dispose();
        }
    }
}
