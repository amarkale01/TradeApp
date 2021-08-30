using AliceBlueWrapper;
using AliceBlueWrapper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Trading_App.Processor;

namespace Trading_App.Common
{
    public delegate void MTMHandler(string value);
    public class MTMConnect : IDisposable
    {


        public event MTMHandler OnMTMChanged;
        public event MTMHandler OnMTMTargetHit;
        public event OnErrorHandler OnError;
        public event MTMHandler OnBreakevenHit;

        Timer timer;
        APIProcessor apiProcessor;
        bool timerStop = false;
        public double TargerMTM = 0;
        public double MaxLossMTM = -2000;
        public int StopLossForCoverOrder = 22;
        public bool IsMTMExitEnabled = false;
        public DayPosition dayPosition;
        int breakEvenCount = 0;

        public MTMConnect(APIProcessor apiProcessor)
        {
            this.apiProcessor = apiProcessor;
            timer = new Timer
            {
                Interval = 1000
            };
        }
        public void TimerStart()
        {
            //double.TryParse(apiProcessor.TargetMTM, out targerMTM);
            //double.TryParse(apiProcessor.MaxMTMLoss, out maxLossMTM);
            timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            timer.Start();
            timerStop = false;
            breakEvenCount = 0;
        }
        public void TimerStop()
        {
            timer.Elapsed -= new ElapsedEventHandler(Timer_Elapsed);
            timer.Stop();
            timerStop = true;
        }
        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                dayPosition = await apiProcessor.GetDayPosition();

                if (dayPosition.data.positions.Any())
                {
                    double mtmVal = GetMTMValue(dayPosition);
                    OnMTMChanged?.Invoke((mtmVal.ToString()));
                    if ((mtmVal >= TargerMTM || mtmVal <= MaxLossMTM )&& !timerStop && IsMTMExitEnabled)
                    {
                        //TimerStop();
                        IsMTMExitEnabled = false;
                        apiProcessor.ExitOrderRetryCount = 3;
                        await apiProcessor.ExitAllOrders("BANKNIFTY");
                        //await apiProcessor.ExitAllOrders("NIFTY");
                        OnMTMTargetHit("Target hit");
                    }

                   
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public double GetMTMValue(DayPosition dayPosition)
        {
            double mtmval = 0;
            foreach (Position position in dayPosition.data.positions)
            {
                mtmval += double.Parse(position.m2m);
            }
            return mtmval;
        }

        public async Task<double> GetFinalMTM()
        {
            dayPosition = await apiProcessor.GetDayPosition();
            return GetMTMValue(dayPosition);
        }

        private bool IsBreakEvenHit(DayPosition dayPosition, ref string tradingSymbol)
        {
            bool retVal = false;

            //calculate breakeven for given breakeven % or stoploss %
            
            foreach (Position position in dayPosition.data.positions)
            {
                double sellPrice = double.Parse(position.average_sell_price);

                double ltp = double.Parse(position.ltp);

                if (sellPrice > 0 && ltp > 0)
                {
                    double breakEvenPrice = sellPrice + (sellPrice * (StopLossForCoverOrder / 100));

                    if (ltp >= breakEvenPrice)
                    {
                        tradingSymbol = position.trading_symbol;
                        retVal = true;
                        break;
                    }
                }
                
            }
           

            return retVal;
        }


        public void Dispose()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }
            OnMTMChanged = null;
        }
    }
}
