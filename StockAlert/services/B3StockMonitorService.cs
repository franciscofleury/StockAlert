using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;

namespace StockAlert.services
{
    enum MonitorAction
    {
        NONE,
        BUY,
        SELL

    }
    public class B3StockMonitorService : IMonitorService
    {
        private readonly Parameters _params;
        private readonly IStockProvider _stock_provider;
        private readonly bool _enable_recommendation;
        private readonly bool _enable_day_high_alert;
        private readonly bool _enable_day_low_alert;

        private MonitorAction _last_action = MonitorAction.NONE;
        private decimal _day_high = -1;
        private decimal _day_low = -1;
        private CancellationToken _cancellation_token;
        public B3StockMonitorService(IOptions<StockMonitorConfig> options, Parameters alertParameters, IStockProvider stock_provider)
        {
            var config = options.Value;
            _enable_recommendation = config.StockRecommendation;
            _enable_day_high_alert = config.DayHighAlert;
            _enable_day_low_alert = config.DayLowAlert;
            _params = alertParameters;
            _stock_provider = stock_provider;
        }

        public string GetName()
        {
            return "B3 Stock";
        }

        public async Task Setup(CancellationToken cancellation_token)
        {
            _cancellation_token = cancellation_token;

            await _stock_provider.Setup(_cancellation_token);

            return;
        }
        private Alert? StockRecomendation(IStockInfo info)
        {
            Alert? recommendation = null;
            
            decimal current_stock_price = info.CurrentPrice;
            if (_last_action != MonitorAction.BUY && current_stock_price < _params.BuyThreshold)
            {
                _last_action = MonitorAction.BUY;
                recommendation = new Alert($"Monitoramento de {_params.StockSymbol} - COMPRA", $"A ação {_params.StockSymbol} atingiu o valor de {current_stock_price:F6} na B3 em {DateTime.Now}.\nA recomendação é COMPRAR");
            }
            else if (_last_action != MonitorAction.SELL && current_stock_price > _params.SellThreshold)
            {
                _last_action = MonitorAction.SELL;
                recommendation = new Alert($"Monitoramento de {_params.StockSymbol} - VENDA", $"A ação {_params.StockSymbol} atingiu o valor de {current_stock_price:F6} na B3 em {DateTime.Now}.\nA recomendação é VENDER");
            }

            return recommendation;
        }

        private void SetupMonitorInfo(IStockInfo info)
        {
            if (_day_high == -1 && _day_low == -1)
            {
                _day_high = info.DayHigh;
                _day_low = info.DayLow;
            }
        }

        private Alert? DayHighAlert (IStockInfo info)
        {
            Alert? alert = null;
            if (info.DayHigh > _day_high)
            {
                alert =  new Alert($"Monitoramento de {_params.StockSymbol} - RECORDE DE MAIOR VALOR DIÁRIO", $"A ação {_params.StockSymbol} atingiu um novo recorde de maior valor diário de {info.DayHigh:F6} na B3 em {DateTime.Now}.");
            }
            _day_high = info.DayHigh;

            return alert;
        }

        private Alert? DayLowAlert(IStockInfo info)
        {
            Alert? alert = null;
            if (info.DayLow < _day_low)
            {
                alert = new Alert($"Monitoramento de {_params.StockSymbol} - RECORDE DE MENOR VALOR DIÁRIO", $"A ação {_params.StockSymbol} atingiu um novo recorde de menor valor diário de {info.DayLow:F6} na B3 em {DateTime.Now}.");
            }
            _day_low = info.DayLow;

            return alert;
        }
        public async Task<List<Alert>?> Monitor()
        {
            var alertMessages = new List<Alert>();

            IStockInfo? stock_info = await _stock_provider.GetStockInfo(_params.StockSymbol);

            // Verificando se houve erro na obtenção das informações da ação
            if (stock_info == null)
                return null;

            Console.WriteLine($"[B3StockMonitorService] StockInfo: Symbol={stock_info.Symbol}, CurrentPrice={stock_info.CurrentPrice:F6}, DayHigh={stock_info.DayHigh:F6}, DayLow={stock_info.DayLow:F6}");

            // Define day_high e day_low pela primeira vez
            SetupMonitorInfo(stock_info);

            // Recomendação de operação
            if (_enable_recommendation)
            {
                Alert? recommendation = StockRecomendation(stock_info);
                if (recommendation != null)
                {
                    alertMessages.Add(recommendation);
                    Console.WriteLine("[B3StockMonitorService] Recomendação gerada.");
                }
            }

            // Alerta de recorde de maior valor diário
            if (_enable_day_high_alert)
            {
                Alert? dayHigh = DayHighAlert(stock_info);
                if (dayHigh != null)
                {
                    alertMessages.Add(dayHigh);
                    Console.WriteLine("[B3StockMonitorService] Alerta de maior valor diário gerado.");
                }
            }

            // Alerta de recorde de menor valor diário
            if (_enable_day_low_alert)
            {
                Alert? dayLow = DayLowAlert(stock_info);
                if (dayLow != null)
                {
                    alertMessages.Add(dayLow);
                    Console.WriteLine("[B3StockMonitorService] Alerta de menor valor diário gerado.");
                }
            }

            return alertMessages;
        }
    }
}
