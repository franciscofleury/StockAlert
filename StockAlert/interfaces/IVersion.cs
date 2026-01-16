using Microsoft.Extensions.Hosting;
using StockAlert.models;

namespace StockAlert.interfaces
{
    public interface IVersion
    {
        public static abstract HostApplicationBuilder GenerateApplicationBuilder(Parameters alertParameters);
    }
}
