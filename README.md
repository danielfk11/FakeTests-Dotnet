# TestDrive.Fakes

[![CI](https://github.com/danielfk11/FakeTests-Dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/danielfk11/FakeTests-Dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/TestDrive.Fakes.svg)](https://www.nuget.org/packages/TestDrive.Fakes/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**TestDrive.Fakes** é uma biblioteca leve e poderosa que fornece test doubles (fakes/mocks/stubs) para integrações comuns, permitindo testes rápidos, determinísticos e offline. Ideal para testar funcionalidades que dependem de e-mail, armazenamento de blobs e clientes HTTP sem depender de serviços externos.

## ✨ Características

- **🎯 Foco em Testes**: Criada especificamente para facilitar testes determinísticos e rápidos
- **📧 E-mail Fake**: Simula envio de e-mails com inbox in-memory e métodos de busca
- **💾 Storage Fake**: Armazenamento de blobs em memória com operações completas CRUD
- **🌐 HTTP Fake**: Handler HTTP configurável com sistema de regras flexível
- **⏰ Clock Determinístico**: Controle total sobre tempo em testes
- **🆔 ID Generator**: Gerador de IDs sequenciais e previsíveis
- **🚨 Fault Injection**: Políticas de falha configuráveis para testar resiliência
- **🛠️ TestKit**: Extensões de assertions para facilitar verificações em testes

## 🚀 Cenários de Uso

### Cenário 1: Testando Envio de E-mails
Você tem um serviço que envia e-mails de boas-vindas, confirmações ou notificações. Com TestDrive.Fakes, você pode:
- ✅ Verificar se os e-mails foram enviados
- ✅ Validar destinatários, assuntos e conteúdo
- ✅ Testar sem configurar servidores SMTP
- ✅ Garantir timestamps determinísticos

### Cenário 2: Testando Upload/Download de Arquivos
Seu sistema salva relatórios, imagens ou documentos em storage de blobs. Com TestDrive.Fakes:
- ✅ Simule operações de upload/download instantâneas
- ✅ Teste sem dependências de AWS S3, Azure Blob ou outros serviços
- ✅ Valide conteúdo, metadados e estrutura de diretórios
- ✅ Injete falhas para testar recuperação de erros

### Cenário 3: Testando Integrações HTTP
Sua aplicação consome APIs REST ou serviços HTTP. Com TestDrive.Fakes:
- ✅ Simule respostas de APIs externas
- ✅ Configure cenários de sucesso, erro e latência
- ✅ Teste sem conectividade de rede
- ✅ Crie testes reproduzíveis e rápidos

## 📦 Instalação

```bash
# Via NuGet Package Manager
Install-Package TestDrive.Fakes

# Via .NET CLI
dotnet add package TestDrive.Fakes

# Via PackageReference
<PackageReference Include="TestDrive.Fakes" Version="1.0.0" />
```

## 🎯 Uso Básico

### 📧 Fake Email Sender

```csharp
using TestDrive.Fakes.Email;
using TestDrive.Fakes.Core;

// Configurar
var clock = new FixedClock(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
var emailSender = new FakeEmailSender(clock);

// Usar no código
await emailSender.SendAsync("noreply@app.com", "user@example.com", "Bem-vindo!", "Olá! Bem-vindo ao nosso serviço.");

// Verificar em testes
emailSender.ShouldHaveSent(1);
emailSender.ShouldHaveSentEmailTo("user@example.com", "Bem-vindo!");

var email = emailSender.GetEmailTo("user@example.com");
Assert.Equal("Olá! Bem-vindo ao nosso serviço.", email.Body);
Assert.Equal(clock.UtcNow, email.UtcSentAt);
```

### 💾 Fake Blob Storage

```csharp
using TestDrive.Fakes.Storage;
using System.Text;

// Configurar
var storage = new InMemoryBlobStorage();

// Upload
var content = Encoding.UTF8.GetBytes("Conteúdo do relatório...");
using var stream = new MemoryStream(content);
await storage.UploadAsync("relatorios", "2023/janeiro/vendas.txt", stream, "text/plain");

// Download
using var downloadStream = await storage.DownloadAsync("relatorios", "2023/janeiro/vendas.txt");
using var reader = new StreamReader(downloadStream);
var conteudo = await reader.ReadToEndAsync();

// Verificar em testes
storage.ShouldContainBlob("relatorios", "2023/janeiro/vendas.txt");
storage.ShouldHaveContentType("relatorios", "2023/janeiro/vendas.txt", "text/plain");
storage.ShouldHaveTextContent("relatorios", "2023/janeiro/vendas.txt", "Conteúdo do relatório...");
```

### 🌐 Fake HTTP Handler

```csharp
using TestDrive.Fakes.Http;
using System.Net;

// Configurar regras
var httpHandler = new FakeHttpHandler();
httpHandler
    .WhenGet("https://api.externa.com/usuarios/123", """{"id": 123, "nome": "João"}""")
    .WhenPost("https://api.externa.com/usuarios", """{"id": 456, "status": "criado"}""", HttpStatusCode.Created)
    .When(req => req.RequestUri.ToString().Contains("erro"), 
          req => new HttpResponseMessage(HttpStatusCode.InternalServerError));

// Usar com HttpClient
using var httpClient = new HttpClient(httpHandler);

var response = await httpClient.GetAsync("https://api.externa.com/usuarios/123");
var json = await response.Content.ReadAsStringAsync();
// json será: {"id": 123, "nome": "João"}
```

### ⏰ Clock Determinístico

```csharp
using TestDrive.Fakes.Core;

// Configurar tempo fixo
var clock = new FixedClock(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));

// Usar no código (injeção de dependência)
var agora = clock.UtcNow; // Sempre retorna 2023-01-15 10:30:00

// Avançar tempo em testes
clock.Advance(TimeSpan.FromHours(2));
var depois = clock.UtcNow; // 2023-01-15 12:30:00
```

### 🆔 Gerador de IDs Determinístico

```csharp
using TestDrive.Fakes.Core;

var idGenerator = new DeterministicIdGenerator();

var id1 = idGenerator.GenerateId(); // "000001"
var id2 = idGenerator.GenerateId(); // "000002"
var id3 = idGenerator.GenerateId(); // "000003"

// Reiniciar sequência
idGenerator.Reset();
var id4 = idGenerator.GenerateId(); // "000001" novamente
```

## 🏗️ Estrutura

```
TestDrive.Fakes/
├── Core/           # Clock, IdGenerator, FaultPolicy
├── Email/          # IEmailSender, FakeEmailSender
├── Storage/        # IBlobStorage, InMemoryBlobStorage
├── Http/           # FakeHttpHandler
└── TestKit/        # Assertions e helpers
```

## 📋 Funcionalidades Avançadas

### Fault Injection (Injeção de Falhas)

```csharp
// Sempre falhar
var alwaysFailPolicy = FaultPolicy.AlwaysFail(() => new TimeoutException("Timeout simulado"));

// Latência fixa
var latencyPolicy = FaultPolicy.WithLatency(TimeSpan.FromMilliseconds(500));

// Falha probabilística (30% de chance)
var randomFailPolicy = FaultPolicy.WithFailureProbability(0.3, () => new HttpRequestException("Rede instável"));

// Usar com qualquer fake
var emailSender = new FakeEmailSender(faultPolicy: latencyPolicy);
var storage = new InMemoryBlobStorage(faultPolicy: randomFailPolicy);
var httpHandler = new FakeHttpHandler(alwaysFailPolicy);
```

### TestKit - Assertions Fluentes

```csharp
using TestDrive.Fakes.TestKit;

// Email assertions
emailSender.ShouldHaveSent(3);
emailSender.ShouldHaveSentEmailTo("admin@app.com");
emailSender.ShouldHaveSentEmailWithSubject("Relatório Mensal");
emailSender.ShouldNotHaveSentAnyEmails();

// Storage assertions
storage.ShouldContainBlob("bucket", "arquivo.txt");
storage.ShouldHaveObjectCount("bucket", 5);
storage.ShouldHaveContentType("bucket", "imagem.jpg", "image/jpeg");
storage.ShouldHaveTextContent("bucket", "config.json", """{"debug": true}""");
```

## 🏆 Por que TestDrive.Fakes?

**Motivação**: Testes de integração são essenciais, mas frequentemente são lentos, flaky e dependem de recursos externos. TestDrive.Fakes resolve isso fornecendo substitutos determinísticos e rápidos para integrações comuns, mantendo a semântica e o comportamento esperado dos serviços reais.

**Filosofia**: 
- ✅ **Simplicidade**: API intuitiva e fácil de usar
- ✅ **Determinismo**: Resultados previsíveis e reproduzíveis
- ✅ **Performance**: Testes rápidos que executam em milissegundos
- ✅ **Isolamento**: Zero dependências externas durante testes
- ✅ **Flexibilidade**: Configuração granular de comportamentos

---

**Happy Testing!** 🚀