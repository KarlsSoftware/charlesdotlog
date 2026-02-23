# TODO

## Projekt aus OneDrive verschieben

**Problem:** Das Projekt liegt unter `OneDrive\Desktop\` — Windows Defender sperrt
neu erstellte `.exe`-Dateien beim Build-Vorgang (apphost.exe).

**Workaround aktiv:** `<UseAppHost>false</UseAppHost>` in `Backend/MinimalApiDemo.csproj`

**Aufgabe:** Projekt nach `C:\Dev\netminimalapi` (oder ähnlich) verschieben

Schritte:
1. Neuen Ordner erstellen: `C:\Dev\`
2. Projekt dorthin kopieren/verschieben
3. `<UseAppHost>false</UseAppHost>` aus dem csproj wieder entfernen
4. Build testen: `dotnet build`
