Mein Claude Code Workflow: Plan zuerst, dann ausführen

Claude Code ist kein Chat-Interface im Browser — es ist ein CLI-Tool. Man öffnet ein Terminal, tippt claude, und arbeitet direkt im eigenen Projekt. Das ist der entscheidende Unterschied: Claude hat Zugriff auf die echten Dateien, kann Code lesen, schreiben und ausführen — alles lokal, alles im Kontext des Projekts.

AI-Chats mit Coding-Assistenten fühlen sich trotzdem oft chaotisch an: Man tippt drauf los, der Kontext wird riesig, und irgendwann liefert das Modell Unsinn. Dabei braucht es kein Chaos — sondern ein System. In diesem Post zeige ich meinen konkreten Workflow mit Claude Code, den ich sofort beim nächsten Projekt anwenden kann.

---

1. Die wichtigste Regel: Plan → Execute → Clear

Der wichtigste erste Schritt: /plan eingeben. Das aktiviert den Plan Mode — Claude erkundet die Codebasis, denkt die Lösung durch und präsentiert einen Plan, bevor eine einzige Zeile Code geändert wird. Erst nach Zustimmung wird ausgeführt.

Mit @ + m (oder Shift+Enter nach dem /terminal-setup) kann ich mehrzeilige Eingaben tippen. Das erlaubt es, einen klaren, strukturierten Prompt zu formulieren, bevor Claude irgendetwas macht.

Das think-Keyword ist dabei besonders nützlich. Je nach Komplexität der Aufgabe nutze ich think für etwas mehr Überlegung, think harder für bessere Ergebnisse bei schwierigeren Problemen, oder ultrathink für maximale Denktiefe bei komplexen Fragen. Claude liefert bei gutem Planning bessere Ergebnisse mit weniger Nacharbeit — ein klarer Plan spart mehr Zeit als er kostet.

Wenn eine Aufgabe erledigt ist, lösche ich den Chat-Verlauf mit /clear. Der Kontext wird damit neu gestartet — das reduziert Kosten und hält den Chat übersichtlich.

---

2. Setup: Der erste Start

Bevor es losgeht, drei wichtige Befehle: claude startet Claude Code, /terminal-setup aktiviert Shift+Enter in VS Code für mehrzeilige Eingaben, und /init erstellt die CLAUDE.md — die Projekt-Bibel für Claude.

Ein guter erster Check ist /doctor. Der Befehl prüft ob alles korrekt konfiguriert ist — API-Key vorhanden, Verbindung funktioniert, Abhängigkeiten stimmen. Wer Probleme beim Setup hat, fängt hier an.

/init erstellt eine CLAUDE.md-Datei im Projektverzeichnis. Claude liest diese Datei automatisch bei jedem Chat. Hier speichere ich projektspezifische Anweisungen, zum Beispiel welche Library-Docs zu verwenden sind, ob TypeScript oder JavaScript, welches Test-Framework. Diese Datei manuell erweitern lohnt sich — sie ist der Kern für konsistente Ergebnisse über Sessions hinweg.

---

3. Kosten im Griff behalten

Claude Code kostet pro Token — jede Nachricht, jede Antwort, jeder gelesene Dateiinhalt zählt. Das klingt erstmal abschreckend, ist aber gut steuerbar. /clear nach jeder abgeschlossenen Aufgabe hält den Kontext klein. /compact fasst lange Sessions zusammen statt sie komplett zu verwerfen. Und mit /model lässt sich zwischen den verfügbaren Modellen wechseln — Opus ist das mächtigste, Haiku das günstigste. Für einfache Aufgaben reicht Haiku völlig aus, für komplexe Architekturentscheidungen lohnt sich Opus.

---

4. Kontext gezielt steuern

Anstatt Code in den Chat zu kopieren, referenziere ich Dateien direkt mit @pfad/zur/datei. Claude liest dann genau die angegebene Datei oder den Ordner — kein unnötiger Kontext, kein Raten.

Mit dem #-Prefix wird eine Anweisung dauerhaft in die CLAUDE.md gespeichert und gilt damit für alle zukünftigen Sessions in diesem Projekt. Einfach "#Immer Fehler auf Deutsch erklären" eingeben — und das ist gespeichert. Das ist der schnellste Weg, Präferenzen persistent zu machen.

---

5. Das Permission-System verstehen

Bevor Claude etwas Riskantes tut — eine Datei löschen, einen Shell-Befehl ausführen, ins Dateisystem schreiben — fragt es nach. Das ist kein Zufall, sondern ein bewusstes Design. Claude Code unterscheidet zwischen lesen (immer erlaubt) und handeln (Zustimmung erforderlich). Wer das einmal verstanden hat, arbeitet viel ruhiger damit — man muss nicht jeden Output überprüfen, sondern nur die Aktionen bestätigen.

---

6. Wichtige Befehle im Überblick

Die wichtigsten Slash-Befehle: /plan aktiviert den Plan Mode — Claude plant zuerst und führt erst nach Bestätigung aus. /clear löscht den Chat-Verlauf für einen frischen Kontext. /compact reduziert den Chat auf eine kompakte Zusammenfassung, wenn er lang wird, aber der Kontext nicht verloren gehen soll. /resume zeigt alle bisherigen Chats an und lässt mich eine Session fortsetzen. /model wechselt das Modell. /doctor prüft das Setup. Und ESC ESC spult zu einem früheren Punkt in der Session zurück.

---

7. Power-Features

MCP (Model Context Protocol) erlaubt es, externe Dienste direkt in Claude Code zu integrieren. Ein Beispiel mit Context7 für aktuelle Library-Dokumentation: "claude mcp add --transport http context7 https://mcp.context7.com/mcp --scope project". --scope project bedeutet, dieser Server ist nur für dieses Projekt aktiv. In CLAUDE.md eingetragen: "Nutze Context7 für aktuelle Docs von Libraries". Mit /mcp sehe ich alle aktiven MCP Server.

Mit /agents lassen sich spezialisierte Sub-Agenten erstellen, die bestimmte Aufgaben übernehmen — z.B. "immer Code reviewen bevor committen" oder "Testfälle schreiben". Die Agent-Definition wird in .claude/agents/ gespeichert und im interaktiven Menü konfiguriert.

Ich kann auch eigene Slash-Befehle erstellen. Dafür lege ich eine Datei unter .claude/commands/meincommand.md an, schreibe den Inhalt mit Platzhaltern, starte Claude Code neu — und nutze den Befehl dann so: /meincommand Carousel | H1-Titel und Animationen. Argumente werden per | getrennt und die Platzhalter werden der Reihe nach ersetzt.

---

Fazit

Claude Code ist mächtiger als es auf den ersten Blick wirkt — aber nur wenn man es mit System benutzt. Plan first mit /plan. Kontext smart halten mit @datei, #anweisung und CLAUDE.md. Kosten im Griff behalten mit /clear, /compact und dem richtigen Modell. Das Permission-System nicht als Hindernis sehen, sondern als Sicherheitsnetz. Und bei Bedarf erweitern mit MCP Servern, Agents und Custom Commands.

Der Unterschied zwischen chaotischem AI-Chat und einem soliden Workflow ist kein Zufall — es ist ein System.
