namespace StockAlert.models
{
    public class Alert
    {
        public string Topic = "";
        public string Message = "";

        public Alert(string topic, string message) 
        {
            Topic = topic;
            Message = message;
        }
    }
}
