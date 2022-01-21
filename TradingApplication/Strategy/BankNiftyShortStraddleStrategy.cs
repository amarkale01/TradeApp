using AliceBlueWrapper;
using AliceBlueWrapper.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Trading_App.Processor;

namespace TradingApplication.Strategy
{
    public delegate void StraddleTickMonitoringStart(List<Tick> ticks);
    public delegate void StraddleTickStopLossHit(Tick tick);
    public delegate void StraddleTickMonitoringStop();

    public class BankNiftyShortStraddleStrategy
    {
        Timer timer;
        APIProcessor apiProcessor;

        public event OnErrorHandler LogMessage;
        public event StraddleTickMonitoringStart OnStraddleTickMonitoringStart;
        public event StraddleTickStopLossHit OnStraddleTickStopLossHit;
        public event StraddleTickMonitoringStop OnStraddleTickMonitoringStop;

        public bool Is945StraddleEnabled = false;
        private bool Is945StraddleExecuted = false;

        private bool IsStraddleUnderMonitoring = false;
        private bool IsStopLossHit = false;
        private bool IsWaitingFor20Points = false;

        public bool IsMTMExitEnabled = false;

        private decimal OptionWaitForPrice = 0;
        private decimal OptionLastStopLossPrice = 0;
        public string CurrentBankNifty = string.Empty;
        public DayPosition CurrentStrategyPosition = null;
        public double DayM2m = 0;
        public double TargerMTM = 1500;
        public double MaxLossMTM = -1500;
        public string TradeAutoExecutionTime = string.Empty;
        public double StopLossPercentage = 1.20;
        public List<Tick> straddleTicks = null;
        public string ExpiryWeek = string.Empty;
        public bool IsExecuteTrades = false;

        public BankNiftyShortStraddleStrategy()
        {
            timer = new Timer
            {
                Interval = 5000
            };

            TradeAutoExecutionTime = ConfigurationManager.AppSettings["TradeAutoExecutionTime"];
            string strStopLossPercentage = ConfigurationManager.AppSettings["StopLossPercentage"];
            if(!string.IsNullOrEmpty(strStopLossPercentage))
            {
                StopLossPercentage = Convert.ToDouble("1." + strStopLossPercentage);
            }

        }

        public void Subscribe(APIProcessor apiProcessor)
        {
            this.apiProcessor = apiProcessor;

            TimerStart();

            LogMessage?.Invoke("BankNifty Short Straddle Strategy started...");
            LogMessage?.Invoke("BankNifty Short Straddle will be executed at - " + TradeAutoExecutionTime);
        }

        public void Unsubscribe()
        {
            if (timer != null)
                timer.Stop();

            LogMessage?.Invoke("BankNifty Short Straddle Strategy stopped...");
        }

        public void TimerStart()
        {
            timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            timer.Start();
        }

        private async void Timer_Elapsed_old(object sender, ElapsedEventArgs e)
        {
            try
            {
                string currentTime = DateTime.Now.ToString("hh:mm");
                
                if (Is945StraddleEnabled && !Is945StraddleExecuted && !string.IsNullOrEmpty(TradeAutoExecutionTime) && string.Compare(currentTime, TradeAutoExecutionTime, true) == 0)
                {
                    Is945StraddleExecuted = true;
                    int currentBankNifty = Convert.ToInt32(CurrentBankNifty);
                    //timer.Stop();
                    LogMessage?.Invoke("Executing 9:50 AM Short Straddle...");

                    if (currentBankNifty > 0)
                        Straddle_950AM(currentBankNifty);
                    else
                        LogMessage?.Invoke("CurrentBankNifty is 0.");

                    //timer.Start();
                }


                if(CurrentStrategyPosition != null)
                {
                    DayM2m = GetMTMValue(CurrentStrategyPosition);

                    if ((DayM2m >= TargerMTM || DayM2m <= MaxLossMTM) && IsMTMExitEnabled)
                    {
                        IsMTMExitEnabled = false;
                        //apiProcessor.ExitOrderRetryCount = 3;
                        //await apiProcessor.ExitAllOrders("BANKNIFTY");
                        LogMessage?.Invoke("BankNifty Short Straddle strategy target hit");
                    }
                }

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                string currentTime = DateTime.Now.ToString("hh:mm");
                int currentBankNifty = 0;

                if(!string.IsNullOrEmpty(CurrentBankNifty))
                    currentBankNifty = Convert.ToInt32(CurrentBankNifty);

                if (Is945StraddleEnabled && !Is945StraddleExecuted && !string.IsNullOrEmpty(TradeAutoExecutionTime) && string.Compare(currentTime, TradeAutoExecutionTime, true) == 0 && !IsStraddleUnderMonitoring) //
                {
                    StartStraddleMonitoring(currentBankNifty);
                }
                else if (IsStraddleUnderMonitoring)
                {
                    ExecuteStraddle();
                }
                else if (Is945StraddleExecuted)
                {
                    CheckDayPosition();
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void Straddle_945AM(int strike)
        {
            try
            {
                if (strike == 0)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                Is945StraddleExecuted = true;

                apiProcessor.IsCEChecked = true;
                apiProcessor.IsPEChecked = true;
                apiProcessor.IsStrangleChecked = true;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 300;
                apiProcessor.StopLossForOrder = "50";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.TransactionOrderType = "SELL";
                
                await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                System.Threading.Thread.Sleep(10000);
                await apiProcessor.GetOrderHistory();
                await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders, "BANKNIFTY");

                LogMessage?.Invoke("Executed 9:45 AM Short Straddle...");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void Straddle_950AM(int strike)
        {
            try
            {
                if (strike == 0)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                Is945StraddleExecuted = true;

                apiProcessor.IsCEChecked = true;
                apiProcessor.IsPEChecked = true;
                apiProcessor.IsStrangleChecked = true;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 500;
                apiProcessor.StopLossForOrder = "60";
                apiProcessor.IsStopLossInPercent = false;
                apiProcessor.TransactionOrderType = "BUY";

                await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                System.Threading.Thread.Sleep(10000);
                //await apiProcessor.GetOrderHistory();
                //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders, "BANKNIFTY");

                apiProcessor.IsCEChecked = true;
                apiProcessor.IsPEChecked = true;
                apiProcessor.IsStrangleChecked = true;
                apiProcessor.Strike = Round(strike);
                apiProcessor.OTMDiff = 100;
                apiProcessor.StopLossForOrder = "60";
                apiProcessor.IsStopLossInPercent = false;
                apiProcessor.TransactionOrderType = "SELL";

                await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                System.Threading.Thread.Sleep(10000);
                await apiProcessor.GetOrderHistory();
                await apiProcessor.PlaceStopLossForSELLOrder(apiProcessor.ExecutedOrders, "BANKNIFTY");

                LogMessage?.Invoke("Executed 9:45 AM Short Straddle...");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void ExecuteStraddle()
        {
            try
            {
                if (straddleTicks == null || straddleTicks.Count() < 1)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                if (!IsStopLossHit)
                {
                    uint code = 0;
                    foreach (Tick tick in straddleTicks)
                    {
                        if (tick.LastPrice != 0)
                        {
                            if (tick.StopLoss == 0)
                            {
                                tick.InitialPrice = tick.LastPrice;
                                tick.StopLoss = Convert.ToDecimal((double)tick.LastPrice * StopLossPercentage);
                                LogMessage?.Invoke("Monitoring " + tick.Symbol);
                                LogMessage?.Invoke("InitialPrice Price " + tick.InitialPrice);
                                LogMessage?.Invoke("StopLoss Price " + tick.StopLoss);

                            }

                            if (tick.LastPrice > tick.StopLoss)
                            {
                                code = tick.InstrumentToken;
                                break;
                            }
                        }
                    }

                    if (code != 0)
                    {
                        IsStopLossHit = true;
                        Tick removeTick = straddleTicks.Where(s => s.InstrumentToken == code).FirstOrDefault();
                        if (removeTick != null)
                        {
                            straddleTicks.Remove(removeTick);
                            IsWaitingFor20Points = true;

                            if(straddleTicks != null && straddleTicks.Count > 0)
                            {
                                straddleTicks[0].InitialPrice = straddleTicks[0].LastPrice;
                                OptionWaitForPrice = straddleTicks[0].LastPrice;
                                OptionWaitForPrice += GetWaitPrice();
                                OptionLastStopLossPrice = straddleTicks[0].StopLoss;
                                LogMessage?.Invoke("Wait for  " + straddleTicks[0].Symbol + " to hit " + OptionWaitForPrice);
                            }
                            
                            LogMessage?.Invoke("Stop Loss hit for " + removeTick.Symbol);
                            //ExecuteBUYForStraddle(strike);//
                            //ExecuteSELLForStraddle(strike);
                            OnStraddleTickStopLossHit?.Invoke(removeTick);
                        }
                    }
                }
                else if(IsWaitingFor20Points)
                {
                    uint code = 0;
                    int iStrike = 0;
                    foreach (Tick tick in straddleTicks)
                    {
                        if (tick.LastPrice > OptionWaitForPrice)
                        {
                            code = tick.InstrumentToken;
                            iStrike = tick.Strike;
                            break;
                        }
                    }

                    if (code != 0)
                    {
                        IsWaitingFor20Points = false;
                        ExecuteSELLForStraddle(iStrike);
                    }
                }

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void StartStraddleMonitoring(int strike)
        {
            try
            {
                if (strike == 0)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }

                //Is945StraddleExecuted = true;
                IsStraddleUnderMonitoring = true;
                int iStrike = Round(strike);
                int ceStrike = Round(strike);
                int peStrike = Round(strike);
                
                if (straddleTicks == null)
                    straddleTicks = new List<Tick>();

                straddleTicks.Add(apiProcessor.GetInstrumentTick($"BANKNIFTY {ExpiryWeek} {ceStrike}.0 CE"));
                straddleTicks.Add(apiProcessor.GetInstrumentTick($"BANKNIFTY {ExpiryWeek} {peStrike}.0 PE"));

                foreach(Tick tick in straddleTicks)
                {
                    tick.Strike = iStrike;
                }

                LogMessage?.Invoke($"Monitoring {TradeAutoExecutionTime} Short Straddle for strike " + iStrike);

                OnStraddleTickMonitoringStart?.Invoke(straddleTicks);

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void ExecuteSELLForStraddle(int strike)
        {
            try
            {
                if (straddleTicks == null || straddleTicks.Count() < 1)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }
                Tick tick = straddleTicks[0];

                if (tick != null)
                {
                    straddleTicks.Clear();
                    int currentBankNifty = Convert.ToInt32(CurrentBankNifty);
                    

                    if (straddleTicks == null)
                        straddleTicks = new List<Tick>();

                    int iStrike = Round(strike);

                    if (!string.IsNullOrEmpty(tick.Symbol) && tick.Symbol.ToLower().IndexOf("ce") > 0)
                        straddleTicks.Add(apiProcessor.GetInstrumentTick($"BANKNIFTY {ExpiryWeek} {iStrike}.0 CE"));
                    else if (!string.IsNullOrEmpty(tick.Symbol) && tick.Symbol.ToLower().IndexOf("pe") > 0)
                        straddleTicks.Add(apiProcessor.GetInstrumentTick($"BANKNIFTY {ExpiryWeek} {iStrike}.0 PE"));

                    //if (straddleTicks != null && straddleTicks.Count > 0)
                    //    OnStraddleTickMonitoringStart?.Invoke(straddleTicks);

                    LogMessage?.Invoke("Executed BANKNIFTY SELL strike " + iStrike);

                    if (!string.IsNullOrEmpty(tick.Symbol) && tick.Symbol.ToLower().IndexOf("ce") > 0)
                    {
                        apiProcessor.IsCEChecked = true;
                        apiProcessor.IsPEChecked = false;
                    }
                    else if (!string.IsNullOrEmpty(tick.Symbol) && tick.Symbol.ToLower().IndexOf("pe") > 0)
                    {
                        apiProcessor.IsCEChecked = false;
                        apiProcessor.IsPEChecked = true;
                    }

                    apiProcessor.IsStrangleChecked = true;
                    apiProcessor.Strike = Round(strike);
                    apiProcessor.OTMDiff = 800;
                    apiProcessor.StopLossForOrder = "60";
                    apiProcessor.IsStopLossInPercent = false;
                    apiProcessor.TransactionOrderType = "BUY";

                    if (IsExecuteTrades)
                    {
                        await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                        System.Threading.Thread.Sleep(20000);
                    }


                    apiProcessor.IsStrangleChecked = false;
                    apiProcessor.OTMDiff = 0;
                    apiProcessor.Strike = Round(strike);
                    apiProcessor.IsStopLossInPercent = false;
                    apiProcessor.TransactionOrderType = "SELL";

                    if (IsExecuteTrades)
                    {
                        await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                    }

                    Is945StraddleExecuted = true;
                    IsStraddleUnderMonitoring = false;
                    // string logStrike = string.Format("{0} {1}", apiProcessor.Strike, apiProcessor.IsCEChecked ? "CE" : "PE");
                    // LogMessage?.Invoke("Executed BANKNIFTY SELL strike " + logStrike);

                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private async void ExecuteBUYForStraddle(int strike)
        {
            try
            {
                if (straddleTicks == null || straddleTicks.Count() < 1)
                {
                    LogMessage?.Invoke("Empty Strike");
                    return;
                }
                Tick tick = straddleTicks[0];

                if (tick != null)
                {
                    straddleTicks.Clear();
                    int currentBankNifty = Convert.ToInt32(CurrentBankNifty);
                    Is945StraddleExecuted = true;

                    if (straddleTicks == null)
                        straddleTicks = new List<Tick>();

                    int iStrike = Round(strike);

                    if (!string.IsNullOrEmpty(tick.Symbol) && tick.Symbol.ToLower().IndexOf("ce") > 0)
                        straddleTicks.Add(apiProcessor.GetInstrumentTick($"BANKNIFTY {ExpiryWeek} {iStrike}.0 PE"));
                    else if (!string.IsNullOrEmpty(tick.Symbol) && tick.Symbol.ToLower().IndexOf("pe") > 0)
                        straddleTicks.Add(apiProcessor.GetInstrumentTick($"BANKNIFTY {ExpiryWeek} {iStrike}.0 CE"));

                    if (straddleTicks != null && straddleTicks.Count > 0)
                        OnStraddleTickMonitoringStart?.Invoke(straddleTicks);

                    LogMessage?.Invoke("Executed BANKNIFTY BUY strike " + iStrike);
                    if (!string.IsNullOrEmpty(tick.Symbol) && tick.Symbol.ToLower().IndexOf("ce") > 0)
                    {
                        apiProcessor.IsCEChecked = false;
                        apiProcessor.IsPEChecked = true;
                    }
                    else if (!string.IsNullOrEmpty(tick.Symbol) && tick.Symbol.ToLower().IndexOf("pe") > 0)
                    {
                        apiProcessor.IsCEChecked = true;
                        apiProcessor.IsPEChecked = false;
                    }

                    apiProcessor.IsStrangleChecked = true;
                    apiProcessor.Strike = Round(strike);
                    apiProcessor.OTMDiff = 200;
                    apiProcessor.StopLossForOrder = "60";
                    apiProcessor.IsStopLossInPercent = false;
                    apiProcessor.TransactionOrderType = "BUY";

                    // await apiProcessor.PlaceEntryOrder("BANKNIFTY");
                    //System.Threading.Thread.Sleep(10000);

                    //apiProcessor.IsStrangleChecked = false;
                    //apiProcessor.OTMDiff = 0;
                    //apiProcessor.Strike = Round(strike);
                    //apiProcessor.IsStopLossInPercent = false;
                    //apiProcessor.TransactionOrderType = "SELL";

                    //await apiProcessor.PlaceEntryOrder("BANKNIFTY");

                    Is945StraddleExecuted = true;
                    //string logStrike = string.Format("{0} {1}", apiProcessor.Strike, apiProcessor.IsCEChecked ? "CE" : "PE");
                    LogMessage?.Invoke("Executed BANKNIFTY BUY option ");

                }
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

        public double GetMTMValue(DayPosition dayPosition)
        {
            double mtmval = 0;
            foreach (Position position in dayPosition.data.positions)
            {
                if (position.trading_symbol.ToLower().StartsWith("banknifty"))
                    mtmval += double.Parse(position.m2m);
            }
            return mtmval;
        }

        public async void CheckDayPosition()
        {
            try
            {
                if (CurrentStrategyPosition != null)
                {
                    DayM2m = GetMTMValue(CurrentStrategyPosition);

                    if ((DayM2m >= TargerMTM || DayM2m <= MaxLossMTM) && IsMTMExitEnabled)
                    {
                        //IsMTMExitEnabled = false;
                        //apiProcessor.ExitOrderRetryCount = 3;
                        //await apiProcessor.ExitAllSELLOrders();
                        //System.Threading.Thread.Sleep(10000);
                        //await apiProcessor.ExitAllSELLOrders();
                        //System.Threading.Thread.Sleep(10000);
                        //await apiProcessor.ExitAllOrders("BANKNIFTY");
                        //LogMessage?.Invoke("BankNifty Short Straddle strategy target hit");
                    }
                }

                if (straddleTicks != null && straddleTicks.Count() > 0)
                {
                    Tick executedTick = null;
                    foreach (Tick tick in straddleTicks)
                    {
                        if (tick.LastPrice != 0)
                        {
                            if (tick.StopLoss == 0)
                            {
                                tick.InitialPrice = tick.LastPrice;
                                tick.StopLoss = OptionLastStopLossPrice;
                                tick.Target = tick.LastPrice - 100;

                                LogMessage?.Invoke("Executed " + tick.Symbol);
                                LogMessage?.Invoke("SELL Price " + tick.LastPrice);
                                LogMessage?.Invoke("StopLoss Price " + tick.StopLoss);
                                LogMessage?.Invoke("Target Price " + tick.Target);
                            }

                            if ((tick.LastPrice >= tick.StopLoss || tick.LastPrice <= tick.Target))
                            {
                                executedTick = tick;
                                Is945StraddleExecuted = false;
                                await apiProcessor.ExitAllSELLOrders();
                                System.Threading.Thread.Sleep(10000);
                                await apiProcessor.ExitAllOrders("BANKNIFTY");

                                LogMessage?.Invoke("BankNifty Short Straddle strategy target hit");
                                
                                //OnStraddleTickMonitoringStop?.Invoke();
                            }
                        }
                    }

                    if (executedTick != null)
                    {
                        straddleTicks.Remove(executedTick);
                        OnStraddleTickStopLossHit?.Invoke(executedTick);
                    }
                }
            }
            catch(Exception ex)
            {
                LogMessage?.Invoke(ex.Message);
            }
        }

        private int GetWaitPrice()
        {
            int waitPrice = 0;

            DayOfWeek today = DateTime.Now.DayOfWeek;

            switch (today)
            {
                case DayOfWeek.Friday:
                    waitPrice = 10;
                    break;
                case DayOfWeek.Monday:
                    waitPrice = 10;
                    break;
                case DayOfWeek.Tuesday:
                    waitPrice = 10;
                    break;
                case DayOfWeek.Wednesday:
                    waitPrice = 5;
                    break;
                case DayOfWeek.Thursday:
                    waitPrice = 5;
                    break;
                

            }

            return waitPrice;
        }
    }
}
