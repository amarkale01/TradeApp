using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Trading_App;
using Trading_App.Common;
using Trading_App.Model;
using Trading_App.Processor;
using System.Configuration;
using TradingApplication.Strategy;
using AliceBlueWrapper.Models;

namespace TradingApplication
{
    public partial class MainWindow : Window
    {
        #region Private Variables
        private TradeSetting tradeSetting;
        //private GmailConnect gmailConnect;
        private MTMConnect mtmConnect;
        private TickerConnect tickerConnect;
        StrategyConnect strategyConnect = null;
        BankNiftyTelegramStrategy bankNiftyTelegramStrategy = null;
        BankNiftyShortStraddleStrategy bankNiftyShortStraddleStrategy = null;
        APIProcessor apiProcessor;
        Helper helper;
        List<Tick> tickerTicks = null;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            helper = new Helper();
            tradeSetting = helper.ReadSetting();
            //SetGmailSetting();
            apiProcessor = new APIProcessor(tradeSetting, helper);
            apiProcessor.LoadMasterContract();
            apiProcessor.LogAdded += LogAdded;

            mtmConnect = new MTMConnect(apiProcessor);
            mtmConnect.OnMTMChanged += UpdateMTM;
            mtmConnect.OnMTMTargetHit += MTMTargetHit;
            mtmConnect.OnError += MTMConnectError;
            mtmConnect.OnBreakevenHit += BreakevenHit;

            tickerConnect = new TickerConnect(apiProcessor);
            tickerConnect.OnTickerChanged += TickerConnect_OnTickerChanged;
            tickerConnect.OnError += MTMConnectError;
            tickerConnect.OnTickerTickChanged += TickerConnect_OnTickerTickChanged;

            strategyConnect = new StrategyConnect(apiProcessor);
            strategyConnect.LogMessage += MTMConnectError;

            CheckToken();
            InitializeSetting();
            SubscribeTicker();
            StartMTM();
        }

       

        private void InitializeSetting()
        {
            txtMaxProfit.Text = tradeSetting.MTMProfit;
            txtMaxLoss.Text = tradeSetting.MTMLoss;
            tblExpiryWeek.Text = tradeSetting.ExpiryWeek;
            txtSL.Text = tradeSetting.StopLossPercentage;
            txtOTMDiff.Text = "0";
            txtOTMDiff.IsEnabled = false;
            txtLots.Text = tradeSetting.Lots;
        }

        private void LogAdded(string logMessage)
        {
            if (!Thread.CurrentThread.IsBackground)
                AddLogs(logMessage);
            else
                Dispatcher.BeginInvoke(new OnLogHandler(AddLogs), new object[] { logMessage });
        }

        private void AddLogs(string log)
        {
            txtLogs.Text += DateTime.Now.ToString() + ":  " + log + Environment.NewLine;
            FileLogger.Write(log, FileLogger.MsgType.Info);
        }

        private void CheckToken()
        {
            if (!string.IsNullOrEmpty(tradeSetting.TokenCreatedOn) && !string.IsNullOrEmpty(tradeSetting.Token))
            {
                DateTime saveDate = DateTime.Parse(tradeSetting.TokenCreatedOn);
                if (DateTime.Now.Day != saveDate.Day)
                    chkTokenGenerated.IsChecked = false;
            }
            else
            {
                chkTokenGenerated.IsChecked = false;
            }
        }

        #region gmail setting
        private void SetGmailSetting()
        {
            //gmailConnect = new GmailConnect();
            //gmailConnect.OnUniversalCondition += GmailConnect_OnUniversalCondition;
        }

        private async void GmailConnect_OnUniversalCondition(object sender, EventArgs e)
        {
            await apiProcessor.GetOrderHistory();
            await apiProcessor.CancelAllPendingOrder();
        }
        #endregion
        private async void btnEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnEntry.IsEnabled = false;

                if (txtStrike.Text == string.Empty)
                {
                    MessageBox.Show("Enter strike");
                    btnEntry.IsEnabled = true;
                    return;
                }

                if (chkCallChecked.IsChecked == false && chkPutChecked.IsChecked == false)
                {
                    MessageBox.Show("Please select CE / PE option.");
                    btnEntry.IsEnabled = true;
                    return;
                }

                apiProcessor.IsCEChecked = Convert.ToBoolean(chkCallChecked.IsChecked);
                apiProcessor.IsPEChecked = Convert.ToBoolean(chkPutChecked.IsChecked);

                apiProcessor.IsStrangleChecked = Convert.ToBoolean(chkStrangleChecked.IsChecked);
                apiProcessor.Strike = Convert.ToInt32(txtStrike.Text);

                if (apiProcessor.IsStrangleChecked)
                {
                    if (!string.IsNullOrEmpty(txtOTMDiff.Text))
                        apiProcessor.OTMDiff = Convert.ToInt32(txtOTMDiff.Text);
                    else
                        apiProcessor.OTMDiff = 0;
                }

                if (string.IsNullOrEmpty(txtSL.Text))
                {
                    MessageBox.Show("Please provide stop loss.");
                    btnEntry.IsEnabled = true;
                    return;
                }

                apiProcessor.StopLossForOrder = txtSL.Text;
                if (cmbSL.Text.Contains('%'))
                    apiProcessor.IsStopLossInPercent = true;
                else
                    apiProcessor.IsStopLossInPercent = false;

                apiProcessor.Lots = Convert.ToInt32(txtLots.Text);

                apiProcessor.TakeProfitForOrder = "35";
                apiProcessor.TransactionOrderType = cmbOrderType.Text;

                await apiProcessor.PlaceEntryOrder(cmbSymbol.Text);
                System.Threading.Thread.Sleep(10000);
                await apiProcessor.GetOrderHistory();
                //await apiProcessor.PlaceStopLossOrder(apiProcessor.ExecutedOrders);
                //await apiProcessor.PlaceTargetOrder(apiProcessor.ExecutedOrders);
                btnEntry.IsEnabled = true;

                //BankNiftyTelegramStrategy_OnBuyExecuted("Manual entry done");
            }
            catch (Exception ex)
            {
                LogAdded(ex.Message);
            }
        }

        private async void btnToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddLogs("Generating token...");
                btnToken.IsEnabled = false;
                await apiProcessor.Login();
                chkTokenGenerated.IsChecked = true;
                btnToken.IsEnabled = true;
                SubscribeTicker();

            }
            catch (Exception ex)
            {
                AddLogs(ex.Message);
            }
        }

        private async void btnExit_Click(object sender, RoutedEventArgs e)
        {
            btnExit.IsEnabled = false;
            apiProcessor.ExitOrderRetryCount = 3;
            await apiProcessor.ExitAllOrders("Nifty");

            btnExit.IsEnabled = true;
        }

        private async void btnExitBankNifty_Click(object sender, RoutedEventArgs e)
        {
            btnExitBankNifty.IsEnabled = false;
            apiProcessor.ExitOrderRetryCount = 3;
            await apiProcessor.ExitAllOrders("BankNifty");

            btnExitBankNifty.IsEnabled = true;
        }

        private void btnMTMExit_Click(object sender, RoutedEventArgs e)
        {
            // await apiProcessor.GetDayPosition();
            if (btnMTMExit.Content.ToString().Contains("Start"))
                StartMTMExit();
            else
                StopMTMExit();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (apiProcessor != null)
                apiProcessor.Dispose();
            //if (gmailConnect != null)
            //    gmailConnect.Dispose();
            if (mtmConnect != null)
                mtmConnect.Dispose();
            if (tickerConnect != null)
                tickerConnect.Dispose();
            if (strategyConnect != null)
                strategyConnect.Dispose();
        }

        private void chkStrangleChecked_Checked(object sender, RoutedEventArgs e)
        {
            if (chkStrangleChecked.IsChecked == true)
            {
                txtOTMDiff.IsEnabled = true;
                txtOTMDiff.Text = tradeSetting.OTMDiff;
            }
            else
            {
                txtOTMDiff.IsEnabled = false;
                txtOTMDiff.Text = "0";
            }
        }

        #region Commented Code
        //private async void btnPEExit_Click(object sender, RoutedEventArgs e)
        //{
        //    btnPEExit.IsEnabled = false;
        //    await apiProcessor.GetOrderHistory();
        //    await apiProcessor.ExitPEOrders();
        //    btnPEExit.IsEnabled = true;
        //}

        //private async void btnCEExit_Click(object sender, RoutedEventArgs e)
        //{
        //    btnCEExit.IsEnabled = false;
        //    await apiProcessor.GetOrderHistory();
        //    await apiProcessor.ExitCEOrders();
        //    btnCEExit.IsEnabled = true;
        //}

        //private void btnReadEmail_Click(object sender, RoutedEventArgs e)
        //{
        //    if (btnReadEmail.Content.ToString() == "Read Email")
        //    {
        //        btnReadEmail.Content = "Stop Email";
        //        SetGmailSetting();
        //    }
        //    else
        //    {
        //        btnReadEmail.Content = "Read Email";
        //        gmailConnect.Dispose();
        //    }
        //} 
        #endregion      

        #region MTM 
        private void StartMTM()
        {
            //if (txtMaxProfit.Text.Trim() == string.Empty)
            //{
            //    MessageBox.Show("Please enter target MTM value.");
            //    return;
            //}

            //double.TryParse(txtMaxProfit.Text, out mtmConnect.TargerMTM);
            //double.TryParse(txtMaxLoss.Text, out mtmConnect.MaxLossMTM);
            //int.TryParse(txtSL.Text, out mtmConnect.StopLossForCoverOrder);

            mtmConnect.TimerStart();
            //btnMTMExit.Content = "Stop MTM";
            AddLogs("MTM timer started.");
        }

        private void StopMTMExit()
        {
            btnMTMExit.Content = "Start MTM Exit";
            //tblMTM.Text = "MTM value";
            //mtmConnect.TimerStop();
           // mtmConnect.IsMTMExitEnabled = false;

            if (bankNiftyShortStraddleStrategy != null)
                bankNiftyShortStraddleStrategy.IsMTMExitEnabled = false;

            AddLogs("MTM Exit ended.");
        }

        private void StartMTMExit()
        {
            if (txtMaxProfit.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Please enter target MTM value.");
                return;
            }

            double.TryParse(txtMaxProfit.Text, out mtmConnect.TargerMTM);
            double.TryParse(txtMaxLoss.Text, out mtmConnect.MaxLossMTM);
            int.TryParse(txtSL.Text, out mtmConnect.StopLossForCoverOrder);

            //mtmConnect.IsMTMExitEnabled = true;

            if (bankNiftyShortStraddleStrategy != null)
                bankNiftyShortStraddleStrategy.IsMTMExitEnabled = true;

            //mtmConnect.TimerStart();
            btnMTMExit.Content = "Stop MTM Exit";
            AddLogs("MTM Exit timer started.");
        }

        private void MTMTargetHit(string message)
        {
            try
            {
                LogAdded(message);
                string finalMTM = mtmConnect.GetFinalMTM().ToString();
                UpdateMTM(finalMTM);
                Dispatcher.BeginInvoke(new Action(StopMTMExit), null);
            }
            catch (Exception ex)
            {
                LogAdded(ex.Message);
            }
        }

        private void BreakevenHit(string message)
        {
            try
            {
                LogAdded(message);

                Dispatcher.BeginInvoke(new MTMHandler(BUYCoverOrder), new object[] { message });
            }
            catch (Exception ex)
            {
                LogAdded(ex.Message);
            }
        }

        private void SetMTM(string mtmVal)
        {
            if (double.TryParse(mtmVal, out double m2m))
                tblMTM.Text = mtmVal;

            if(bankNiftyShortStraddleStrategy != null)
            {
                //txtBankNiftyStrategyMTM.Text = bankNiftyShortStraddleStrategy.DayM2m.ToString();
            }
        }

        private void UpdateMTM(string mtmVal)
        {
            Dispatcher.BeginInvoke(new MTMHandler(SetMTM), new object[] { mtmVal });
        }

        private async void BUYCoverOrder(string message)
        {
            if (!string.IsNullOrEmpty(message) && message.IndexOf("CE") >= 0)
            {
                apiProcessor.IsCEChecked = true;
                apiProcessor.IsPEChecked = false;
                apiProcessor.IsStrangleChecked = false;
                int currentBankNifty = Convert.ToInt32(strategyConnect.Strike);
                apiProcessor.Strike = Round(currentBankNifty);
                apiProcessor.OTMDiff = 0;
                apiProcessor.StopLossForOrder = "24";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.TransactionOrderType = "BUY";

                LogAdded("Executing BUY CE cover order .. strike - " + apiProcessor.Strike);

                await apiProcessor.PlaceEntryOrder("NIFTY");
            }
            else if (!string.IsNullOrEmpty(message) && message.IndexOf("PE") >= 0)
            {
                apiProcessor.IsCEChecked = false;
                apiProcessor.IsPEChecked = true;
                apiProcessor.IsStrangleChecked = false;
                int currentBankNifty = Convert.ToInt32(strategyConnect.Strike);
                apiProcessor.Strike = Round(currentBankNifty);
                apiProcessor.OTMDiff = 0;
                apiProcessor.StopLossForOrder = "24";
                apiProcessor.IsStopLossInPercent = true;
                apiProcessor.TransactionOrderType = "BUY";

                LogAdded("Executing BUY PE cover order .. strike - " + apiProcessor.Strike);

                await apiProcessor.PlaceEntryOrder("NIFTY");
            }
        }

        private int Round(int number)
        {
            int nearest = 100;
            return (number + 5 * nearest / 10) / nearest * nearest;
        }

        private void MTMConnectError(string errorMessage)
        {
            LogAdded(errorMessage);
        }
        #endregion

        private void txtLogs_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            return;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tradeSetting.MTMProfit = txtMaxProfit.Text;
                tradeSetting.MTMLoss = txtMaxLoss.Text;

                mtmConnect.TargerMTM = double.Parse(txtMaxProfit.Text);
                mtmConnect.MaxLossMTM = double.Parse(txtMaxLoss.Text);

                AddLogs("MTM values updated.");
            }
            catch (Exception ex)
            {
                AddLogs("Error while updating MTM values.");
            }
        }

        private async void btnMasterContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddLogs("Downloading master contract...");
                await apiProcessor.GetMasterContract();
            }
            catch (Exception ex)
            {
                AddLogs("Error while downloading master contract.");
            }
        }

        private void btnStrategy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //strategyConnect.IsBreakout930Checked = true;
                //strategyConnect.Start();

                if (txtTelegramCode.Text == string.Empty)
                {
                    MessageBox.Show("Enter Telegram code");
                    return;
                }

                if (btnStrategy.Content.ToString().Contains("Start"))
                {
                    btnStrategy.Content = "Stop Strategy";
                    bankNiftyTelegramStrategy = new BankNiftyTelegramStrategy();
                    bankNiftyTelegramStrategy.OnBuyExecuted += BankNiftyTelegramStrategy_OnBuyExecuted;
                    bankNiftyTelegramStrategy.OnMessage += MTMConnectError;
                    bankNiftyTelegramStrategy.OnError += MTMConnectError;
                    bankNiftyTelegramStrategy.TelegramCode = txtTelegramCode.Text;
                    bankNiftyTelegramStrategy.Subscribe(apiProcessor);
                }
                else
                {
                    btnStrategy.Content = "Start Strategy";

                    if (bankNiftyTelegramStrategy != null)
                        bankNiftyTelegramStrategy.Unsubscribe();
                }
            }
            catch (Exception ex)
            {
                AddLogs("Error while starting strategy.");
            }
        }

        private void BankNiftyTelegramStrategy_OnBuyExecuted(string value)
        {
            Dispatcher.BeginInvoke(new MTMHandler(SetBuyOrder), new object[] { value });
        }

        private void btnBankNiftyStrategy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                if (btnBankNiftyStrategy.Content.ToString().Contains("Start"))
                {
                    btnBankNiftyStrategy.Content = "Stop BN Strategy";
                    bankNiftyShortStraddleStrategy = new BankNiftyShortStraddleStrategy();
                    bankNiftyShortStraddleStrategy.LogMessage += MTMConnectError;
                    bankNiftyShortStraddleStrategy.OnStraddleTickMonitoringStart += BankNiftyShortStraddleStrategy_OnStraddleTickMonitoringStart;
                    bankNiftyShortStraddleStrategy.OnStraddleTickStopLossHit += BankNiftyShortStraddleStrategy_OnStraddleTickStopLossHit;
                    bankNiftyShortStraddleStrategy.OnStraddleTickMonitoringStop += BankNiftyShortStraddleStrategy_OnStraddleTickMonitoringStop;
                    bankNiftyShortStraddleStrategy.Is945StraddleEnabled = true;
                    bankNiftyShortStraddleStrategy.ExpiryWeek = tradeSetting.ExpiryWeek;
                    bankNiftyShortStraddleStrategy.Subscribe(apiProcessor);
                }
                else
                {
                    btnBankNiftyStrategy.Content = "Start BN Strategy";

                    if (bankNiftyShortStraddleStrategy != null)
                        bankNiftyShortStraddleStrategy.Unsubscribe();

                    tickerTicks = new List<Tick>();
                }
            }
            catch (Exception ex)
            {
                AddLogs("Error while starting strategy.");
            }
        }

        private void BankNiftyShortStraddleStrategy_OnStraddleTickMonitoringStop()
        {
            Dispatcher.BeginInvoke(new StraddleTickMonitoringStop(StopStraddleMonitoring));
        }

        private void BankNiftyShortStraddleStrategy_OnStraddleTickStopLossHit(Tick tick)
        {
            Dispatcher.BeginInvoke(new StraddleTickStopLossHit(OnStraddleTickStopLossHit), tick);
        }

        private void BankNiftyShortStraddleStrategy_OnStraddleTickMonitoringStart(List<Tick> ticks)
        {
            Dispatcher.BeginInvoke(new StraddleTickMonitoringStart(SubscribeStraddleTicker), ticks);
        }

        private void SubscribeStraddleTicker(List<Tick> ticks)
        {
            tickerTicks = null;

            if (tickerTicks == null)
            {
                tickerTicks = new List<Tick>();
                tickerTicks.Add(new Tick() { InstrumentToken = tickerConnect.BANKNIFTY_INSTRUMENT_TOKEN, Mode = "1" });
            }

            tickerTicks.AddRange(ticks);

            if (chkTokenGenerated.IsChecked == true)
            {
                tickerConnect.ReSubscribeTicker(tradeSetting.Token, tickerTicks);
            }
        }

        private void OnStraddleTickStopLossHit(Tick tick)
        {
            if (tickerTicks == null)
            {
                tickerTicks = new List<Tick>();
                tickerTicks.Add(new Tick() { InstrumentToken = tickerConnect.BANKNIFTY_INSTRUMENT_TOKEN, Mode = "1" });
            }

            Tick removeTick = tickerTicks.Where(s => s.InstrumentToken == tick.InstrumentToken).FirstOrDefault();

            if (removeTick != null)
                tickerTicks.Remove(removeTick);

            if (chkTokenGenerated.IsChecked == true)
            {
                tickerConnect.ReSubscribeTicker(tradeSetting.Token, tickerTicks);
            }
        }

        private void StopStraddleMonitoring()
        {
            tickerTicks = null;

            if (tickerTicks == null)
            {
                tickerTicks = new List<Tick>();
                tickerTicks.Add(new Tick() { InstrumentToken = tickerConnect.BANKNIFTY_INSTRUMENT_TOKEN, Mode = "1" });
            }
            
            if (chkTokenGenerated.IsChecked == true)
            {
                tickerConnect.ReSubscribeTicker(tradeSetting.Token, tickerTicks);
            }
        }

        private void SetBuyOrder(string value)
        {
            double currentMTM = 0;
           
            double.TryParse(tblMTM.Text, out currentMTM);

            mtmConnect.TargerMTM = currentMTM + 2000;
            mtmConnect.MaxLossMTM = currentMTM - 2000;

            txtMaxProfit.Text = mtmConnect.TargerMTM.ToString();
            txtMaxLoss.Text = mtmConnect.MaxLossMTM.ToString();
            //AddLogs("Buy order executed using BankNiftyTelegramStrategy ." + value);
            AddLogs(value);

            //start the MTM
            StartMTM();
        }

        private void btnTickerConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SubscribeTicker();

            }
            catch (Exception ex)
            {
                AddLogs("Error while starting ticker.");
            }
        }

        private void TickerConnect_OnTickerChanged(string value)
        {
            Dispatcher.BeginInvoke(new TickerHandler(SetTiker), new object[] { value });
        }

        private void TickerConnect_OnTickerTickChanged(Tick tick)
        {
            Dispatcher.BeginInvoke(new TickerTickHandler(SetTikerTick), tick);
        }
        

        private void SetTiker(string value)
        {
            lblBanknifty.Text = value;
            strategyConnect.Strike = value;

            if(bankNiftyTelegramStrategy != null)
            {
                bankNiftyTelegramStrategy.CurrentBankNfity = value;
            }

            if (bankNiftyShortStraddleStrategy != null)
            {
                bankNiftyShortStraddleStrategy.CurrentBankNifty = value;
                bankNiftyShortStraddleStrategy.CurrentStrategyPosition = mtmConnect.dayPosition;
            }

        }

        private void SetBankNiftyStraddleTiker()
        {
            if (tickerTicks != null)
            {

                txtBankNiftyCE.Text = string.Empty;
                txtBankNiftyCEValue.Text = string.Empty;
                txtBankNiftyCEInitial.Text = string.Empty;
                txtBankNiftyCESL.Text = string.Empty;

                txtBankNiftyPE.Text = string.Empty;
                txtBankNiftyPEValue.Text = string.Empty;
                txtBankNiftyPEInitial.Text = string.Empty;
                txtBankNiftyPESL.Text = string.Empty;

                foreach (Tick item in tickerTicks)
                {
                    if(!string.IsNullOrEmpty(item.Symbol) && item.Symbol.ToLower().IndexOf("ce") > 0)
                    {
                        txtBankNiftyCE.Text = item.Symbol;
                        txtBankNiftyCEValue.Text = item.LastPrice.ToString();
                        txtBankNiftyCEInitial.Text = item.InitialPrice.ToString();
                        txtBankNiftyCESL.Text = item.StopLoss.ToString();
                    }
                    else if (!string.IsNullOrEmpty(item.Symbol) && item.Symbol.ToLower().IndexOf("pe") > 0)
                    {
                        txtBankNiftyPE.Text = item.Symbol;
                        txtBankNiftyPEValue.Text = item.LastPrice.ToString();
                        txtBankNiftyPEInitial.Text = item.InitialPrice.ToString();
                        txtBankNiftyPESL.Text = item.StopLoss.ToString();
                    }
                }
            }

        }

        private void SetTikerTick(Tick tick)
        {
            if (tick != null)
            {
                if (tick.InstrumentToken == tickerConnect.BANKNIFTY_INSTRUMENT_TOKEN)
                {
                    lblBanknifty.Text = tick.LastPrice.ToString();
                    strategyConnect.Strike = tick.LastPrice.ToString();
                }
                else
                {
                    Tick selectedTick = tickerTicks.Where(s => s.InstrumentToken == tick.InstrumentToken).FirstOrDefault();
                    if(selectedTick != null)
                    {
                        selectedTick.LastPrice = tick.LastPrice;
                    }
                }
            }

            if (bankNiftyTelegramStrategy != null)
            {
                bankNiftyTelegramStrategy.CurrentBankNfity = tick.LastPrice.ToString();
            }

            if (bankNiftyShortStraddleStrategy != null)
            {
                if (tick.InstrumentToken == tickerConnect.BANKNIFTY_INSTRUMENT_TOKEN)
                    bankNiftyShortStraddleStrategy.CurrentBankNifty = tick.LastPrice.ToString();

                bankNiftyShortStraddleStrategy.CurrentStrategyPosition = mtmConnect.dayPosition;

                if (bankNiftyShortStraddleStrategy.straddleTicks != null)
                {
                    Tick selectedTick = bankNiftyShortStraddleStrategy.straddleTicks.Where(s => s.InstrumentToken == tick.InstrumentToken).FirstOrDefault();
                    if (selectedTick != null)
                    {
                        selectedTick.LastPrice = tick.LastPrice;
                    }
                }
            }

            SetBankNiftyStraddleTiker();
        }

        public void SubscribeTicker()
        {
            if (tickerTicks == null)
            {
                tickerTicks = new List<Tick>();
                tickerTicks.Add(new Tick() { InstrumentToken = tickerConnect.BANKNIFTY_INSTRUMENT_TOKEN, Mode="1"});//[4, 219002]
            }

            if (chkTokenGenerated.IsChecked == true)
            {
                tickerConnect.SubscribeTicker(tradeSetting.Token, tickerTicks);
            }
        }


    }
}
