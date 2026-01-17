using System;
using System.Collections.Generic;
using System.Text;

namespace StockAlert.utils
{
    public class BetterRandom
    {
        public static double GetGaussianGeneratedNumber(double std_variation, double mean = 0.0)
        {
            // Gerando numeros randomicos uniformes entre 0 e 1
            double u1 = 1.0 - Random.Shared.NextDouble();
            double u2 = 1.0 - Random.Shared.NextDouble();

            // Aplicando a transformação de Box-Muller que gera uma distribuição normal padrão (média 0, desvio padrão 1) a partir de duas variáveis uniformes
            double standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            return mean + standardNormal * std_variation;
        }
    }
}
