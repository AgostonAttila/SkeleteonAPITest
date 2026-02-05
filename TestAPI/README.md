# Student API - Production Ready .NET 10 Minimal API

## Funkciók

? **CRUD mûveletek** Student entitásra  
? **JWT Authentikáció** Bearer token alapú  
? **PostgreSQL adatbázis** Entity Framework Core-ral  
? **Serilog logging** konzolra és fájlba  
? **Health Checks** adatbázis és alkalmazás állapot ellenõrzéshez  
? **Globális hibakezelés** központi exception middleware-rel  
? **Carter Minimal API** modul-alapú endpoint szervezés  
? **Security Headers** XSS, CSRF, Clickjacking védelem  
? **CORS konfiguráció**  
? **OpenAPI/Swagger** dokumentáció  

---

## Elõfeltételek

- .NET 10 SDK
- PostgreSQL szerver (localhost:5432)
- PostgreSQL felhasználó: `postgres` / jelszó: `postgres`

---

## Telepítés és futtatás

### 1. PostgreSQL adatbázis beállítása

```bash
# PostgreSQL kapcsolódás (psql vagy pgAdmin)
# Hozz létre egy új adatbázist:
CREATE DATABASE studentdb_dev;
```

### 2. Connection string frissítése (opcionális)

Szerkeszd az `appsettings.Development.json` fájlt, ha más adatbázis beállításokat használsz.

### 3. NuGet csomagok visszaállítása

```bash
dotnet restore
```

### 4. Adatbázis migráció létrehozása és futtatása

```bash
# EF Core Tools telepítése (ha még nincs)
dotnet tool install --global dotnet-ef

# Migráció létrehozása
dotnet ef migrations add InitialCreate --project TestAPI

# Adatbázis frissítése
dotnet ef database update --project TestAPI
```

**Megjegyzés:** Development módban az alkalmazás automatikusan futtatja a migrációkat indításkor.

### 5. Alkalmazás futtatása

```bash
dotnet run --project TestAPI
```

Az API elérhetõ lesz:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

---

## API Végpontok

### ?? Authentikáció (nincs védelem)

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin123!"
}
```

**Válasz:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-01-20T12:00:00Z"
  },
  "message": "Login successful"
}
```

---

### ????? Student mûveletek (JWT védett)

**Fontos:** Minden Student endpoint-hoz szükséges a JWT token a `Authorization` header-ben:
```
Authorization: Bearer {token}
```

#### 1. Összes diák lekérdezése
```http
GET /api/students
Authorization: Bearer {token}
```

#### 2. Diák lekérdezése ID alapján
```http
GET /api/students/{id}
Authorization: Bearer {token}
```

#### 3. Új diák létrehozása
```http
POST /api/students
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "János",
  "lastName": "Kovács",
  "email": "janos.kovacs@example.com",
  "dateOfBirth": "2000-05-15",
  "phoneNumber": "+36301234567",
  "address": "Budapest, Fõ utca 1."
}
```

#### 4. Diák frissítése
```http
PUT /api/students/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "János",
  "lastName": "Kovács",
  "email": "janos.kovacs@example.com",
  "dateOfBirth": "2000-05-15",
  "phoneNumber": "+36301234567",
  "address": "Budapest, Kossuth utca 10.",
  "isActive": true
}
```

#### 5. Diák törlése
```http
DELETE /api/students/{id}
Authorization: Bearer {token}
```

---

## ?? Health Checks

### Általános health check
```http
GET /health
```

### Készenlét ellenõrzés
```http
GET /health/ready
```

### Élõ állapot ellenõrzés
```http
GET /health/live
```

---

## ?? Logok

A logok a következõ helyekre íródnak:
- **Konzol:** Valós idejû naplózás
- **Fájl:** `logs/log-YYYYMMDD.txt` (napi rotáció)

---

## ?? Biztonsági funkciók

1. **JWT Authentikáció**
   - HS256 aláírás
   - Token lejárat kezelés
   - Secure secret key

2. **Security Headers**
   - `X-Content-Type-Options: nosniff`
   - `X-Frame-Options: DENY`
   - `X-XSS-Protection: 1; mode=block`
   - `Referrer-Policy: strict-origin-when-cross-origin`
   - `Content-Security-Policy: default-src 'self'`

3. **CORS konfiguráció**
   - Csak engedélyezett origin-ek
   - Credential támogatás

4. **HTTPS Redirection**
   - Automatikus HTTPS átirányítás

5. **Validáció**
   - Data Annotations
   - Email és telefonszám validáció
   - Egyedi email constraint

6. **Exception Handling**
   - Központi hibakezelés
   - Részletes logolás
   - Production-safe hibaüzenetek

---

## ?? Production Deployment

### 1. Környezeti változók beállítása

Production környezetben állítsd be a következõ környezeti változókat:

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Host=prod-server;Port=5432;Database=studentdb;Username=app_user;Password=strong_password"
JwtSettings__Secret="YourProductionSecretKeyMinimum32CharactersLong!"
```

### 2. appsettings.Production.json létrehozása

Hozz létre egy `appsettings.Production.json` fájlt:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-postgres;Port=5432;Database=studentdb;Username=prod_user;Password=CHANGE_ME"
  },
  "JwtSettings": {
    "Secret": "CHANGE_ME_TO_STRONG_SECRET_KEY_AT_LEAST_32_CHARS",
    "Issuer": "StudentAPI",
    "Audience": "StudentAPIClients",
    "ExpirationInMinutes": 30
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Microsoft": "Warning",
        "System": "Error"
      }
    }
  },
  "Cors": {
    "AllowedOrigins": [ "https://yourproductiondomain.com" ]
  }
}
```

### 3. Build és publish

```bash
dotnet publish -c Release -o ./publish
```

### 4. Migráció futtatása production-ben

```bash
dotnet ef database update --project TestAPI --configuration Release
```

---

## ?? Projekt struktúra

```
TestAPI/
??? Configuration/       # Konfigurációs osztályok
?   ??? JwtSettings.cs
??? Data/               # Adatbázis kontextus
?   ??? ApplicationDbContext.cs
??? Entities/           # Adatbázis entitások
?   ??? Student.cs
??? Middleware/         # Custom middleware-ek
?   ??? GlobalExceptionHandlerMiddleware.cs
??? Models/            # DTOs és request/response modellek
?   ??? Dtos.cs
??? Modules/           # Carter modul endpoint-ok
?   ??? AuthModule.cs
?   ??? StudentModule.cs
??? Services/          # Üzleti logika szolgáltatások
?   ??? JwtService.cs
??? logs/              # Log fájlok (automatikusan létrejön)
??? appsettings.json
??? appsettings.Development.json
??? Program.cs
```

---

## ?? Tesztelés Postman / curl használatával

### 1. Login és token megszerzése
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'
```

### 2. Diák létrehozása
```bash
curl -X POST https://localhost:5001/api/students \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {your-token}" \
  -d '{
    "firstName": "Anna",
    "lastName": "Nagy",
    "email": "anna.nagy@example.com",
    "dateOfBirth": "2001-03-20",
    "phoneNumber": "+36209876543",
    "address": "Debrecen, Piac utca 5."
  }'
```

### 3. Összes diák lekérdezése
```bash
curl -X GET https://localhost:5001/api/students \
  -H "Authorization: Bearer {your-token}"
```

---

## ?? Fontos megjegyzések

1. **Authentication:** A jelenlegi implementáció egyszerû username/password ellenõrzést használ. Production környezetben használj megfelelõ user management rendszert (pl. ASP.NET Core Identity, Azure AD).

2. **JWT Secret:** MINDIG változtasd meg a JWT secret-et production-ben! Használj legalább 32 karakter hosszú, véletlenszerû stringet.

3. **Database Migrations:** Production-ben ne használj automatikus migrációt! Futtasd manuálisan a migrációkat deployment elõtt.

4. **CORS:** Állítsd be az engedélyezett origin-eket a tényleges domain-ekre.

5. **HTTPS:** Production-ben mindig használj HTTPS-t megfelelõ SSL/TLS tanúsítvánnyal.

---

## ?? License

Ez egy demo projekt, szabadon használható és módosítható.
