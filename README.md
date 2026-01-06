# FCG-Users

## 📋 Introdução

**FCG-Users** é um microserviço responsável pelo gerenciamento de usuários, autenticação e autorização da plataforma FCG. Este serviço fornece endpoints para cadastro, login, validação de tokens JWT e gerenciamento de perfis de usuários, servindo como base de segurança para toda a plataforma.

## 🎯 Objetivos

- Gerenciar cadastro e dados de usuários
- Implementar autenticação segura com JWT
- Fornecer autorização baseada em roles/claims
- Validar tokens para outros microserviços
- Gerenciar perfis e preferências de usuários
- Processar eventos relacionados a usuários

## 🏗️ Arquitetura

### Padrão Clean Architecture

O projeto segue a arquitetura em camadas com separação clara de responsabilidades:

```
FCG-Users/
├── FCG-Users.Api/              # Camada de Apresentação (Controllers, Endpoints)
├── FCG-Users.Application/       # Camada de Aplicação (UseCases, DTOs, Services)
├── FCG-Users.Domain/            # Camada de Domínio (Entidades, Interfaces)
├── FCG-Users.Infrastructure/    # Camada de Infraestrutura (BD, Externos)
└── FCG-Users.Consumer/          # Processador de Mensagens (Worker Service)
```

### Fluxo de Dados - Autenticação

```
Cliente HTTP
    ↓
LoginController (POST /auth/login)
    ↓
AuthenticationService
    ├─ Valida credenciais
    ├─ Busca usuário em MongoDB
    └─ Gera JWT Token
    ↓
Retorna Token JWT
    ↓
Cliente armazena token
    ↓
Requisições subsequentes
    ├─ Header: Authorization: Bearer {token}
    └─ ApimAuthenticationHandler valida JWT
```

### Fluxo de Dados - Autorização

```
Microserviço Recebe Requisição
    ↓
ApimAuthenticationHandler
    ├─ Extrai token do header
    ├─ Valida assinatura JWT
    ├─ Valida expiração
    └─ Extrai claims (userId, roles, etc.)
    ↓
Acesso garantido/negado
```

## 🔧 Stack Tecnológico

- **Framework**: ASP.NET Core 8.0
- **Autenticação**: JWT Bearer Token
- **Hash**: BCrypt ou PBKDF2
- **Banco de Dados**: MongoDB 5.0+
- **Persistência**: Entity Framework Core
- **Mensageria**: Azure Service Bus
- **API Documentation**: Swagger/OpenAPI
- **Docker**: Containerização
- **CI/CD**: Azure Pipelines

## 📨 Microserviços e Mensageria

### Integração com Outros Serviços

**FCG-Users** é consultado por:
- **FCG-Games**: Valida JWT e obtém dados do usuário
- **FCG-Libraries**: Valida JWT e obtém dados do usuário
- **FCG-Payments**: Valida JWT e obtém dados do usuário

### Azure Service Bus - Mensageria Assíncrona

O projeto usa **Azure Service Bus** para comunicação assíncrona baseada em eventos:

#### Consumer Service (FCG-Users.Consumer)
- **Tipo**: Worker Service (Host Service)
- **Responsabilidade**: Processa mensagens relacionadas a usuários
- **Padrão**: Listen & Process
- **Eventos Consumidos**: 
  - `PaymentCompletedEvent`: Atualiza histórico de compras
  - `GameAddedToLibraryEvent`: Rastreia preferências
  - `AccountSuspendedEvent`: De outros serviços

#### Publisher Service
- **Localização**: `FCG.Shared.EventService.Publisher`
- **Função**: Publica eventos para outros microserviços
- **Eventos Publicados**:
  - `UserCreatedEvent`: Novo usuário registrado
  - `UserUpdatedEvent`: Perfil atualizado
  - `UserDeletedEvent`: Conta deletada
  - `UserLoggedInEvent`: Usuário fez login
  - `PasswordChangedEvent`: Senha alterada

### Fluxo de Mensageria

```
1. POST /api/auth/register
   └─ Novo usuário criado

2. UserService
   ├─ Valida dados
   ├─ Persiste em MongoDB
   └─ Publica UserCreatedEvent

3. FCG.Shared.EventService.Publisher
   └─ Envia para Azure Service Bus

4. Outros Consumers recebem
   ├─ FCG-Games.Consumer
   ├─ FCG-Libraries.Consumer
   └─ FCG-Payments.Consumer

5. Cada um processa evento
   ├─ Games: Cria registro de usuário
   ├─ Libraries: Inicializa biblioteca
   └─ Payments: Cria dados de faturamento
```

## 🔐 Sistema de Autenticação e Autorização

### JWT Token

#### Estrutura Base
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-id-uuid",
    "email": "user@example.com",
    "name": "User Name",
    "roles": ["User", "Admin"],
    "iat": 1234567890,
    "exp": 1234571490
  },
  "signature": "..."
}
```

#### Claims Padrão
```csharp
public class ClaimConstants
{
    public const string UserId = "sub";              // Subject (ID do usuário)
    public const string Email = "email";            // Email
    public const string Name = "name";              // Nome completo
    public const string Role = "role";              // Roles/Perfis
    public const string IssuedAt = "iat";          // Emitido em
    public const string ExpiresAt = "exp";         // Expira em
    public const string Issuer = "fcg-users";      // Emissor
}
```

### Roles e Permissions

#### Roles Padrão
```csharp
public enum UserRole
{
    User = 1,           // Usuário comum
    Premium = 2,        // Usuário premium
    Admin = 3,          // Administrador
    Developer = 4,      // Desenvolvedor
    Support = 5         // Suporte
}
```

#### Permissions
```csharp
[Authorize(Roles = "Admin")]
[HttpDelete("/api/users/{userId}")]
public async Task<IActionResult> DeleteUser(Guid userId)
{
    // Apenas admins podem deletar usuários
}

[Authorize(Roles = "Premium,Admin")]
[HttpGet("/api/users/{userId}/advanced-stats")]
public async Task<IActionResult> GetAdvancedStats(Guid userId)
{
    // Apenas usuários premium e admins
}
```

### Fluxo Completo de Login

```
1. POST /api/auth/login
   {
     "email": "user@example.com",
     "password": "password123"
   }

2. AuthenticationService
   ├─ Valida email/senha
   ├─ Compara hash de senha
   └─ Gera JWT

3. Retorno
   {
     "token": "eyJhbGc...",
     "expiresIn": 3600,
     "user": {
       "id": "uuid",
       "email": "user@example.com",
       "name": "User Name"
     }
   }

4. Cliente armazena token

5. Requisição com JWT
   GET /api/games
   Authorization: Bearer eyJhbGc...

6. ApimAuthenticationHandler
   ├─ Extrai token
   ├─ Valida
   └─ Extrai claims

7. Acesso concedido
```

## 📁 Estrutura do Projeto

### FCG-Users.Api
- **Program.cs**: Configuração do host e injeção de dependências
- **Controllers/**: Endpoints HTTP
  - `AuthController.cs`: Login, registro, refresh token
  - `UserController.cs`: Gerenciamento de perfil
  - `AdminController.cs`: Operações administrativas (Roles: Admin)
- **ApimAuthenticationHandler.cs**: Middleware de validação JWT

### FCG-Users.Application
- **Services/**: Lógica de negócios
  - `AuthenticationService.cs`: Login, registro, JWT
  - `UserService.cs`: Gerenciamento de usuário
  - `TokenService.cs`: Geração e validação de tokens
  - `PasswordService.cs`: Hash e validação de senha
- **DTOs/**: Data Transfer Objects
  - `LoginRequest.cs`, `LoginResponse.cs`
  - `RegisterRequest.cs`
  - `UserProfileDto.cs`
- **Validators/**: Validação de dados
- **Interfaces/**: Contratos de serviços
- **Shared/**: Helpers e utilitários

### FCG-Users.Domain
- **Entities/**: Modelos de domínio
  - `User.cs`: Entidade de usuário
  - `UserProfile.cs`: Perfil adicional
  - `RefreshToken.cs`: Tokens refresh
- **Interfaces/**: Contratos de repositório
  - `IUserRepository.cs`
  - `IAuthenticationRepository.cs`
- **Enums/**: Enumerações
  - `UserRole.cs`: Roles de usuário
  - `UserStatus.cs`: Ativo, suspenso, deletado

### FCG-Users.Infrastructure
- **Context/**: DbContext do Entity Framework
- **Repositories/**: Implementação de acesso a dados
- **Services/**: Serviços de infraestrutura
  - `PasswordHashingService.cs`: BCrypt/PBKDF2
  - `JwtTokenGenerator.cs`: Geração de JWT
  - `EmailService.cs`: Envio de emails (opcional)

### FCG-Users.Consumer
- **Program.cs**: Configuração do Worker Service
- **Worker.cs**: Lógica principal
- **EventHandlers/**: Processadores de eventos
- **DependencyInjection.cs**: Setup de DI

## 🚀 Como Executar

### Pré-requisitos
- .NET 8.0 SDK
- MongoDB rodando (local ou cloud)
- Azure Service Bus configurado
- Docker (opcional)

### Desenvolvimento Local

1. **Clonar o repositório**
   ```bash
   git clone https://github.com/seu-repo/FCG-Users.git
   cd FCG-Users
   ```

2. **Configurar appsettings.json**
   ```json
   {
     "ConnectionStrings": {
       "MongoDB": "mongodb://localhost:27017/fcg-users"
     },
     "JwtSettings": {
       "Secret": "your-super-secret-key-min-32-chars",
       "Issuer": "fcg-users",
       "Audience": "fcg-platform",
       "ExpiresInMinutes": 60,
       "RefreshTokenExpiresInDays": 7
     },
     "AzureServiceBus": {
       "ConnectionString": "your-service-bus-connection-string"
     }
   }
   ```

3. **Restaurar dependências e executar**
   ```bash
   dotnet restore
   dotnet run --project FCG-Users.Api
   ```

4. **Executar Consumer**
   ```bash
   dotnet run --project FCG-Users.Consumer
   ```

### Docker

```bash
docker-compose up --build
```

## 🔐 Segurança

### Boas Práticas Implementadas

1. **Hash de Senha**
   ```csharp
   // Usar BCrypt com salt
   var hash = BCrypt.Net.BCrypt.HashPassword(password);
   // Verificar
   var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
   ```

2. **JWT Seguro**
   - Assinado com chave secreta forte
   - Tempo de expiração curto (1 hora)
   - Refresh token com expiração mais longa (7 dias)
   - HTTPS obrigatório em produção

3. **Rate Limiting**
   - Limitar tentativas de login
   - Proteger contra brute force

4. **CORS**
   - Configurar domínios permitidos
   - Validar origem das requisições

### Endpoints de Autenticação

#### Registro
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "name": "User Name"
}

Response: 201 Created
{
  "id": "uuid",
  "email": "user@example.com",
  "name": "User Name"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}

Response: 200 OK
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token-value",
  "expiresIn": 3600,
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "name": "User Name",
    "roles": ["User"]
  }
}
```

#### Refresh Token
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "refresh-token-value"
}

Response: 200 OK
{
  "token": "new-jwt-token",
  "expiresIn": 3600
}
```

#### Validar Token (usado por outros microserviços)
```http
POST /api/auth/validate
Authorization: Bearer {token}

Response: 200 OK
{
  "isValid": true,
  "userId": "uuid",
  "email": "user@example.com",
  "roles": ["User"]
}
```

## 📚 Documentação de API

Acesse o Swagger em: `http://localhost/swagger/index.html`

### Principais Endpoints

**Autenticação**
- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Fazer login
- `POST /api/auth/refresh` - Renovar token
- `POST /api/auth/logout` - Fazer logout
- `POST /api/auth/validate` - Validar token

**Usuário**
- `GET /api/users/me` - Obter perfil do usuário atual
- `PUT /api/users/me` - Atualizar perfil
- `POST /api/users/me/change-password` - Alterar senha
- `DELETE /api/users/me` - Deletar conta

**Admin**
- `GET /api/admin/users` - Listar todos os usuários
- `GET /api/admin/users/{userId}` - Obter detalhes do usuário
- `POST /api/admin/users/{userId}/suspend` - Suspender usuário
- `PUT /api/admin/users/{userId}/role` - Alterar role
