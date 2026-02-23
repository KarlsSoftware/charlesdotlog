# Test-Dokumentation — Blog API

> Du bist Student, ich bin Testingenieur. Dieses Dokument erklärt **was** getestet wird,
> **warum** es getestet wird, und welche Muster dabei verwendet werden.

---

## Warum schreiben wir überhaupt Tests?

In einer Firma passiert folgendes ohne Tests:

1. Du änderst Zeile 180 in `Program.cs` — jetzt sind alle Admin-Endpunkte plötzlich öffentlich.
2. Du merkst es nicht. Du committest. Die Pipeline läuft durch. Du deployest.
3. Irgendjemand scraped alle Drafts und du merkst es erst Tage später.

**Automatisierte Tests verhindern das.** Jedes Mal, wenn du Code änderst, laufen alle 26 Tests in ~4 Sekunden. Wenn du aus Versehen `.RequireAuthorization()` entfernst, schlägt der Test sofort an — nicht dein Nutzer in Production.

---

## Test-Strategie: Integration Tests

Wir verwenden **Integration Tests**, keine Unit Tests.

| | Unit Test | Integration Test (was wir machen) |
|---|---|---|
| Testet | Eine einzelne Methode isoliert | Den gesamten HTTP-Request-Pipeline |
| Datenbank | Gemockt | Echte EF Core (In-Memory) |
| Middleware | Nicht vorhanden | Auth, CORS, Routing — alles dabei |
| Geschwindigkeit | Sehr schnell | Schnell (In-Memory) |
| Vertrauen | Niedrig | Hoch |

Für eine API sind Integration Tests wertvoller, weil sie das echte Verhalten aus Nutzersicht testen. Ein Unit Test hätte nicht gefunden, wenn `RequireAuthorization()` fehlt — ein Integration Test schon.

---

## Test-Stack

| Paket | Wozu |
|---|---|
| `xUnit` | Test-Framework (Klassen, `[Fact]`, `Assert`) |
| `Microsoft.AspNetCore.Mvc.Testing` | Startet die echte App im Speicher — kein echter Port nötig |
| `Microsoft.EntityFrameworkCore.InMemory` | Ersetzt SQLite durch eine In-Memory-Datenbank für Tests |

---

## Dateistruktur

```
Backend.Tests/
├── Backend.Tests.csproj       ← Projekt-Datei mit Paket-Referenzen
├── .env                       ← Test-Secrets (JWT_SECRET, ADMIN_PASSWORD)
├── BlogApiFactory.cs          ← Gemeinsame Test-Infrastruktur
├── AuthTests.cs               ← Tests für Login-Endpunkt (3 Tests)
├── AdminAuthorizationTests.cs ← Tests: Alle geschützten Routen → 401 ohne Token (5 Tests)
├── PostsPublicTests.cs        ← Tests für öffentliche Endpunkte (6 Tests)
└── AdminCrudTests.cs          ← Tests für Admin-CRUD mit gültigem JWT (10 Tests)
                                                               Gesamt: 26 Tests
```

---

## BlogApiFactory.cs — Die Basis aller Tests

Dies ist das Herzstück unserer Test-Infrastruktur. Verstehe diese Datei, und alles andere macht Sinn.

```
WebApplicationFactory<Program>
      │
      │  startet die echte App (Program.cs) im Speicher
      │  tauscht SQLite → In-Memory-Datenbank aus
      │  stellt Helfer-Methoden bereit (SeedPost, ClearPosts)
      └─→ jede Test-Klasse bekommt eine eigene Factory-Instanz
```

**Warum tauschen wir die Datenbank aus?**

- Die echte `blog.db` liegt auf der Entwicklermaschine. Tests dürfen echte Daten nie anfassen.
- In-Memory ist schneller, braucht keine Datei und wird nach dem Test automatisch weggeworfen.

**EF Core 9 Besonderheit:**
In EF Core 9 registriert `AddDbContext()` die Optionen unter `IDbContextOptionsConfiguration<T>`, nicht mehr unter `DbContextOptions<T>`. Wenn wir einfach `AddDbContext` ein zweites Mal aufrufen, sind sowohl SQLite als auch InMemory registriert → EF Core wirft einen Fehler. Deshalb entfernen wir die bestehende Konfiguration **vor** dem Hinzufügen der neuen.

---

## AuthTests.cs — Login-Tests (3 Tests)

**Was wird getestet?** Der `POST /api/auth/login` Endpunkt.

**Warum?** Authentifizierung ist die Grundlage der Sicherheit. Wenn jemand mit falschem Passwort trotzdem einen Token bekommt, ist alles andere wertlos.

| Test | Eingabe | Erwartetes Ergebnis | Warum wichtig |
|---|---|---|---|
| `Login_WithCorrectPassword_Returns200AndToken` | Richtiges Passwort | 200 + JWT-Token | Happy Path — der normale Anwendungsfall muss funktionieren |
| `Login_WithWrongPassword_Returns401` | Falsches Passwort | 401 Unauthorized | Falsche Credentials dürfen NIE einen Token liefern |
| `Login_WithEmptyPassword_Returns401` | Leeres Passwort | 401 Unauthorized | Ein leerer String ist kein gültiges Passwort |

**Muster in diesem Test:**
```csharp
// Arrange — Testdaten vorbereiten
var body = new { Password = CorrectPassword };

// Act — HTTP-Anfrage senden
var response = await _client.PostAsJsonAsync("/api/auth/login", body);

// Assert — Ergebnis prüfen
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
```
Das ist das **AAA-Muster** (Arrange-Act-Assert) — der Standard in der Industrie.

---

## AdminAuthorizationTests.cs — Autorisierungs-Tests (5 Tests)

**Was wird getestet?** Alle geschützten Endpunkte ohne Authorization-Header.

**Warum haben diese Tests eine eigene Datei?**
Authorization-Bugs sind *still*: Die App läuft weiter, aber sensible Daten sind öffentlich. Wenn jemand versehentlich `.RequireAuthorization()` entfernt, merkt man es beim normalen Entwickeln nicht. Diese Tests fangen genau das ab.

| Test | Endpunkt | Erwartet |
|---|---|---|
| `GetAdminPosts_WithoutToken_Returns401` | GET /api/posts/admin | 401 |
| `GetAdminPostById_WithoutToken_Returns401` | GET /api/posts/admin/1 | 401 |
| `CreatePost_WithoutToken_Returns401` | POST /api/posts | 401 |
| `UpdatePost_WithoutToken_Returns401` | PUT /api/posts/1 | 401 |
| `DeletePost_WithoutToken_Returns401` | DELETE /api/posts/1 | 401 |

**Wichtige Beobachtung:**
Wir testen `/api/posts/admin/1` — eine ID die vielleicht nicht existiert. Das ist Absicht: Die Authentifizierung muss geprüft werden, **bevor** die Datenbank abgefragt wird. 401 muss kommen, egal ob der Post existiert oder nicht.

---

## PostsPublicTests.cs — Öffentliche Endpunkte (6 Tests)

**Was wird getestet?** Was ein anonymer Blog-Leser sieht.

**Wichtiges Muster:** Dieser Test verwendet `IAsyncLifetime`:
```csharp
public Task InitializeAsync()
{
    _factory.ClearPosts(); // Datenbank vor jedem Test leeren
    return Task.CompletedTask;
}
```
So startet jeder Test mit einer sauberen, leeren Datenbank. Kein Test beeinflusst den nächsten.

| Test | Prüft |
|---|---|
| `GetPosts_ReturnsOnlyPublishedPosts` | Drafts sind für Öffentlichkeit unsichtbar |
| `GetPosts_ReturnsPostsOrderedByNewestFirst` | Sortierung: Neueste zuerst |
| `GetPostById_WithPublishedPost_Returns200AndPost` | Happy Path: Post laden |
| `GetPostById_WithDraft_Returns404` | Draft-URL gibt 404 (nicht 403!) |
| `SearchPosts_ByTitle_ReturnsMatchingPosts` | Suche nach Titel filtert korrekt |
| `SearchPosts_ByAuthor_ReturnsMatchingPosts` | Suche nach Autor filtert korrekt |

**Warum 404 statt 403 für Drafts?**
403 ("Forbidden") würde bestätigen, dass der Post *existiert*, nur nicht zugänglich ist. 404 ("Not Found") verrät gar nichts — das ist besser für die Sicherheit.

---

## AdminCrudTests.cs — Admin-CRUD mit JWT (10 Tests)

**Was wird getestet?** Alle CRUD-Operationen als authentifizierter Admin.

**Muster für Authentication in Tests:**
```csharp
// In InitializeAsync (läuft vor jedem Test):

// 1. Login → JWT holen
var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", ...);
var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

// 2. Token an alle weiteren Requests hängen
_client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", loginResult!.Token);
```

| Test | Prüft |
|---|---|
| `GetAdminPosts_ReturnsAllPosts_IncludingDrafts` | Admin sieht Drafts, öffentliche API nicht |
| `GetAdminPostById_WithDraft_Returns200` | Admin kann Draft per ID laden (für Edit-Formular) |
| `GetAdminPostById_WithNonExistentId_Returns404` | Nicht-existente ID → 404 |
| `CreatePost_WithValidData_Returns201AndCreatedPost` | Happy Path: Post erstellen → 201 Created |
| `CreatePost_WithMissingTitle_Returns400` | Validierung: Titel fehlt → 400 |
| `CreatePost_WithMissingContent_Returns400` | Validierung: Content fehlt → 400 |
| `CreatePost_WithMissingAuthor_Returns400` | Validierung: Autor fehlt → 400 |
| `UpdatePost_WithValidData_Returns200AndUpdatedPost` | Happy Path: Post updaten |
| `UpdatePost_WithNonExistentId_Returns404` | Update auf nicht-existente ID → 404 |
| `DeletePost_WithExistingId_Returns204AndPostIsGone` | Delete → 204 + danach wirklich weg |
| `DeletePost_WithNonExistentId_Returns404` | Delete auf nicht-existente ID → 404 |

**Warum testen wir alle drei Validierungsfehler (Title, Content, Author) separat?**
In einer Firma hat jemand schonmal nur `if (title == null)` geprüft, aber nicht `if (content == null)`. Separate Tests für jedes Pflichtfeld stellen sicher, dass jede Validierung unabhängig funktioniert.

---

## Tests ausführen

```bash
# Aus dem Root-Verzeichnis:
dotnet test Backend.Tests/

# Mit Details (sieht jeden einzelnen Test):
dotnet test Backend.Tests/ --logger "console;verbosity=detailed"

# Mit Code-Coverage (zeigt welche Zeilen abgedeckt sind):
dotnet test Backend.Tests/ --collect:"XPlat Code Coverage"
```

**Erwartete Ausgabe:**
```
Gesamtzahl Tests: 26
     Bestanden: 26
 Gesamtzeit: ~4 Sekunden
```

---

## Was ist NICHT getestet (und warum)?

| Bereich | Warum nicht getestet |
|---|---|
| Frontend (Angular) | Wäre ein separates Test-Projekt mit Jest/Cypress — außerhalb dieses Scopes |
| JWT-Ablauf nach 8h | Würde echtes Warten erfordern; besser mit `DateTime`-Abstraktion mockbar |
| Simultane Requests | Performance-/Load-Tests — anderes Werkzeug (k6, Artillery) |
| CORS-Header | Korrekt zu testen braucht Browser-Kontext; Smoke-Test in E2E-Tests besser |
| Sehr langer Title (>200 Zeichen) | Wäre ein guter nächster Schritt! EF Core Validierung via `[StringLength(200)]` |

---

## Nächste Schritte für dich als Lernender

1. **Füge einen Test für zu langen Titel hinzu** — `StringLength(200)` in `BlogPost.cs` — wird das enforced?
2. **Schau dir den Code-Coverage-Report an** — welche Zeilen in `Program.cs` sind nicht abgedeckt?
3. **Breakpoint im Test setzen** — debugge einen Test in Visual Studio/Rider und sieh, wie der Request durch die Pipeline geht.
4. **Einen Test absichtlich kaputt machen** — entferne `.RequireAuthorization()` aus einer Route und schau welcher Test rot wird.
