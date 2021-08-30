﻿using System;
                    qty = 25;
                else
                    qty = 50;
                //place CE order
                if (IsCEChecked)
                            //instrument_token = GetInstrumentTokenForFNO($"NIFTY {tradeSetting.ExpiryWeek} {ceStrike}.0 CE"),
                            instrument_token = GetInstrumentTokenForFNO($"{symbol} {tradeSetting.ExpiryWeek} {ceStrike}.0 CE"),
                    //LogAdded($"NIFTY {tradeSetting.ExpiryWeek} {ceStrike} CE  : {response.message} status: {response.status}");
                    LogAdded($"{symbol} {tradeSetting.ExpiryWeek} {ceStrike} CE  : {response.message} status: {response.status}");

                //place CE order
                if (IsCEChecked)
                            //instrument_token = GetInstrumentTokenForFNO($"NIFTY {tradeSetting.ExpiryWeek} {ceStrike}.0 CE"),
                            instrument_token = GetInstrumentTokenForFNO($"BANKNIFTY {tradeSetting.ExpiryWeek} {ceStrike}.0 CE"),
                    //LogAdded($"NIFTY {tradeSetting.ExpiryWeek} {ceStrike} CE  : {response.message} status: {response.status}");
                    LogAdded($"BANKNIFTY {tradeSetting.ExpiryWeek} {ceStrike} CE  : {response.message} status: {response.status}");
                //place PE order
                if (IsPEChecked)
                         //instrument_token = GetInstrumentTokenForFNO($"NIFTY {tradeSetting.ExpiryWeek} {peStrike}.0 PE"),
                         instrument_token = GetInstrumentTokenForFNO($"BANKNIFTY {tradeSetting.ExpiryWeek} {peStrike}.0 PE"),
                         square_off_value = targetPrice,
                         stop_loss_value = stoplossPrice,
                    //LogAdded($"NIFTY {tradeSetting.ExpiryWeek} {peStrike} PE  : {response.message} status: {response.status}");
                    LogAdded($"BANKNIFTY {tradeSetting.ExpiryWeek} {peStrike} PE  : {response.message} status: {response.status}");
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
        }
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
        }
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
        }

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

                }

        public async Task ExitCEOrders()
            //NIFTY 06 May21 14500.0 CE
            Instrument ins = masterContact.Single(x => x.symbol.StartsWith(symbol));
            //NIFTY 06 May21 14500.0 CE
            Instrument ins = masterContact.Single(x => x.symbol.StartsWith(symbol));

        public void Dispose()