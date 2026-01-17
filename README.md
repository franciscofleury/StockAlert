# StockAlert

StockAlert é um framework leve de monitoramento e alerta originalmente desenvolvido para monitorar cotações da B3, mas projetado para funcionar com qualquer fonte de dados que forneça payloads no formato &IStockInfo&. O sistema é modular: providers fornecem dados, serviços de monitoramento aplicam regras e geradores de alertas (alert services) enviam notificações por diferentes canais (SMTP, MQTT, console, etc.). A aplicação é construída com .NET 10 e usa injeção de dependência, padrão &IOptions& e &BackgroundService& para workers.

---

## Guia de Uso Rápido

Para utilizar a aplicação, é necessário configurar o ambiente e executar a versão do binário adequada para o seu cenário. Siga os passos abaixo:

### 1. Configuração (appsettings.json)

Antes de executar qualquer binário, você **deve** preencher o arquivo &appsettings.json& localizado na mesma pasta do executável. O sistema valida essas configurações na inicialização e falhará se estiverem incorretas.

Consulte a seção **"Exemplos de seções do appsettings.json"** no final deste documento para copiar os modelos de configuração.

### 2. Execução

Navegue até a pasta &executaveis&. Lá você encontrará diferentes arquivos &.exe&, onde cada um representa uma **Versão Final** (combinação específica de Provider + Worker).

Abra o terminal (CMD ou PowerShell) e execute o programa utilizando o seguinte formato:

```bash
nome_do_programa.exe [SIMBOLO] [LIMIAR DE COMPRA] [LIMIAR DE VENDA]
```

**Argumentos:**

* `[SIMBOLO]` : O ticker do ativo a ser monitorado (ex: `PETR4`, `VALE3`).
* `[LIMIAR DE COMPRA]` : Se o preço cair abaixo deste valor, um alerta de recomendação de compra será gerado.
* `[LIMIAR DE VENDA]` : Se o preço subir acima deste valor, um alerta de recomendação de venda será gerado.

**Exemplo prático:**
Para rodar a versão que usa a API da Brapi com monitoramento e alerta integrados:

```bash
.\executaveis\Brapi_AllInOne.exe PETR4 22.50 35.00
```

---

## Arquitetura de alto nível

* **Providers:** implementam `IStockProvider` e retornam `IStockInfo`. Exemplos: `BrapiStockProvider`, `RandomStockProvider`, `MQTTProvider`.
* **Workers:** `BackgroundService` que orquestram polling e despacho de alertas. Exemplos: `MonitorWorker`, `AlertWorker`, `AllInOneWorker`.
* **Monitor Services:** implementam `IMonitorService` e contêm a lógica de geração de alertas a partir dos dados do provider. Ex.: `B3StockMonitorService`, `VecnaMonitorService`.
* **Alert Services:** implementam `IAlertService` e entregam alertas (email, MQTT, console). Exemplos: `SMTPMailService`, `MQTTAlertService`, `ConsoleMailService`.
* **Serviços Compartilhados:** utilitários como um wrapper cliente MQTT compartilhado (`IMqttClientWrapper` / `MqttClientWrapper`) usado por providers e alert services.
* **Models:** DTOs e objetos como `Parameters`, `Alert`, `IStockInfo`, `AlertQueue`.
* **Configs:** classes de configuração em `models/configs` que mapeiam seções do `appsettings.json` (consumidas via `IOptions<T>`).

---

## Padrões de código utilizados

* **Injeção de Dependência (DI):** serviços registrados em `HostApplicationBuilder` e resolvidos por construtor.
* **Options pattern:** configurações via `IOptions<T>` com `builder.Services.Configure<T>(...)`.
* **Background workers:** `BackgroundService` para loops assíncronos e lifecycle.
* **Interfaces e responsabilidade única:** cada subsistema possui uma interface (`IStockProvider`, `IMonitorService`, `IAlertService`) para facilitar extensão e testes.
* **Providers pattern:** abstração de fontes de dados externas através de interfaces padronizadas (`IStockProvider`), permitindo a troca fácil entre diferentes APIs ou mocks sem alterar a lógica de consumo.
* **Wrapper pattern:** encapsulamento de bibliotecas de infraestrutura (como clientes MQTT) em classes próprias (`MqttClientWrapper`) para simplificar o uso, gerenciar conexões e facilitar testes unitários.

---

## Sumário Adicional: Serviços e Configurações

Abaixo descrevo os principais serviços disponíveis no framework, as configurações que esperam no `appsettings.json` e observações operacionais importantes.

### Providers (Fornecedores de Dados)

* **BrapiStockProvider**
  * **Propósito:** Busca cotações na API Brapi (`https://brapi.dev/api/quote/{symbol}`).
  * **Configuração:** Este provider não requer nenhuma configuração no `appsettings.json`.
  * **Obs:** Desserialização tolerante a campos nulos ou strings.

* **RandomStockProvider**
  * **Propósito:** Gera preços sintéticos para testes e demos (não requer internet).
  * **Configuração (`RandomStockConfig`):**
    * `StartPrice`: Preço inicial do ativo simulado.
    * `DayHigh`: O valor máximo absoluto que a ação pode atingir durante o dia de simulação.
    * `DayLow`: O valor mínimo absoluto que a ação pode atingir durante o dia de simulação.
    * `StdStock`: Desvio padrão utilizado para calcular a volatilidade do preço a cada tick.
    * `AverageLatency`: Latência média simulada na resposta (em ms).
    * `StdLatency`: Desvio padrão da latência (em ms).
    * `FailureRate`: Taxa de falha simulada para testar resiliência (0.0 a 1.0).

* **MqttStockProvider**
  * **Propósito:** Consome mensagens MQTT e converte em `IStockInfo`.
  * **Configuração:**
    * `MqttConfig`: Parâmetros de conexão (`Broker`, `Port`, `ClientId`).
    * `MqttStockConfig`: Define o tópico de onde ler os dados (`StockTopic`).
  * **Obs:** Usa o cliente compartilhado `MqttClientWrapper`.

* **MqttVecnaProvider**
  * **Propósito:** Provider especializado que fornece mensagens recebidas através de um canal MQTT específico (Vecna).
  * **Configuração:**
    * `MqttConfig`: Parâmetros de conexão.
    * `MqttVecnaConfig`: Define o tópico específico de origem das mensagens (`VecnaTopic`).
  * **Obs:** Utilizado em conjunto com o `VecnaMonitorService`.

### Serviços de Monitoramento (Monitor Services)

* **B3StockMonitorService**
  * **Propósito:** Aplica regras de negócio para ações (Compra se < X, Venda se > Y).
  * **Configuração (`StockMonitorConfig`):**
    * Flags: `StockRecommendation`, `DayHighAlert`, `DayLowAlert` (ativam/desativam tipos de alerta).

* **VecnaMonitorService**
  * **Propósito:** Serviço que monitora as mensagens fornecidas pelo provider (`MqttVecnaProvider`) e as repassa diretamente como alertas, sem lógica de compra/venda complexa.
  * **Configuração:** Utiliza indiretamente o `MqttVecnaConfig` (via provider) para saber a origem dos dados.

### Serviços de Alerta (Alert Services)

* **SMTPMailService**
  * **Propósito:** Enviar alertas por e-mail via protocolo SMTP.
  * **Configuração (`SmtpConfig`):**
    * `Server`: Endereço do servidor SMTP (ex: `smtp.gmail.com`).
    * `Port`: Porta de conexão (ex: `587`).
    * `SenderAddress` / `SenderPassword`: Credenciais de envio.
    * `TargetAddress`: E-mail de destino.
    * `MaxTries`: Número máximo de tentativas de reenvio em caso de falha.
  * **Obs:** Recomendado uso de "App Passwords" para Gmail.

* **MQTTAlertService**
  * **Propósito:** Publicar alertas em tópicos MQTT.
  * **Configuração:**
    * `MqttConfig`: Parâmetros de conexão.
    * `MqttAlertConfig`: Define o tópico de publicação (`AlertTopic`).

* **ConsoleAlertService**
  * **Propósito:** Simula o envio imprimindo no console (Debug/Testes).
  * **Configuração (`ConsoleAlertConfig`):**
    * `AverageLatency`: Latência média simulada no envio.
    * `StdLatency`: Desvio padrão da latência.
    * `FailureRate`: Taxa de falha simulada.

### Workers (Processamento em Background)

* **Workers (MonitorWorker / AlertWorker)**
  * **Propósito:** Orquestram o fluxo de execução.
  * **Configuração (`WorkerConfig`):**
    * `MonitorInterval`: Tempo de espera entre ciclos de monitoramento (em ms).
    * `AllowMonitoringFailure`: Se `true`, não encerra o app em caso de erro na obtenção de dados.
    * `AllowAlertFailure`: Se `true`, não encerra o app em caso de erro no envio de alertas.

---

## Exemplos de seções do appsettings.json

Copie e cole as seções abaixo no seu arquivo `appsettings.json` conforme os serviços que você pretende utilizar.

### Configuração Completa de Exemplo

```json
{
  "WorkerConfig": {
    "MonitorInterval": 5000,
    "AllowMonitoringFailure": true,
    "AllowAlertFailure": false
  },
  "StockMonitorConfig": {
    "StockRecommendation": true,
    "DayHighAlert": true,
    "DayLowAlert": true
  },
  "RandomStockConfig": {
    "StartPrice": 25,
    "DayHigh": 26,
    "DayLow": 24,
    "StdStock": 0.2,
    "AverageLatency": 0,
    "StdLatency": 0.0,
    "FailureRate": 0.0
  },
  "SmtpConfig": {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "SenderAddress": "seu-email@gmail.com",
    "SenderPassword": "senha-de-app",
    "TargetAddress": "destino@exemplo.com",
    "MaxTries": 3
  },
  "ConsoleAlertConfig": {
    "AverageLatency": 0,
    "StdLatency": 0.0,
    "FailureRate": 0.0
  },
  "MqttConfig": {
    "Broker": "test.mosquitto.org",
    "Port": 1883,
    "ClientId": "stock-alert-client"
  },
  "MqttAlertConfig": {
    "AlertTopic": "stock/alert"
  },
  "MqttStockConfig": {
    "StockTopic": "stock/data"
  },
  "MqttVecnaConfig": {
    "VecnaTopic": "vecna/data"
  }
}
```

---

## Versões Finais Disponíveis

Na pasta `executaveis` você encontrará as seguintes builds prontas:

1. **Brapi_AllInOne:**
   * Usa `BrapiStockProvider` (API Real).
   * Worker único para monitorar e alertar.
   * Ideal para uso em produção simples.

2. **Brapi_TwoWorkers:**
   * Usa `BrapiStockProvider` (API Real).
   * Workers separados (Produtor/Consumidor) para maior resiliência.

3. **Random_AllInOne:**
   * Usa `RandomStockProvider` (Dados Fictícios).
   * Ideal para testar a aplicação offline ou validar regras de alerta sem depender da bolsa.

4. **Random_TwoWorkers:**
   * Usa `RandomStockProvider` (Dados Fictícios).
   * Workers separados (Produtor/Consumidor) para maior resiliência.
