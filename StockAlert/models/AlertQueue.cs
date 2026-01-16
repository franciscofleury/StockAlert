using System.Threading.Channels;

namespace StockAlert.models
{
    // O papel da AlertQueue é servir de canal para comunicação entre dois Workers, um produtor e um consumidor.
    public class AlertQueue
    {
        // Estrutura de canal para comunicação entre produtor e consumidor (nativa do .NET)
        private readonly Channel<Alert> _channel;

        public AlertQueue(int maxMessages)
        {
            // Cria um canal com capacidade limitada para evitar sobrecarga de memória
            _channel = Channel.CreateBounded<Alert>(maxMessages);
        }

        // Acesso do produtor ao canal
        public ChannelWriter<Alert> Writer => _channel.Writer;

        // Acesso do consumidor ao canal
        public ChannelReader<Alert> Reader => _channel.Reader;
    }

}