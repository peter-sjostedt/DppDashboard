# Brand-vy — Funktionsbeskrivning

## Översikt
Brand-vyn är tillgänglig efter inloggning med en Brand API-nyckel. Den innehåller tre flikar: **Produkter**, **Batcher** och **Leverantörer**, samt en statusrad med produktantal, batchantal och språkväljare (SV/EN).

---

## Flik 1: Produkter

### Lista
- DataGrid med kolumner: **Produktnamn + Artikelnr**, **Kategori**, **Status** (aktiv/inaktiv badge), **Varianter** (antal, högerjusterat), **Uppdaterad**, **Åtgärdsmeny (⋮)**
- Sökfält (filtrerar på produktnamn)
- Statusfilter: Aktiv / Inaktiv (multi-select toggle-knappar)
- Tom-vy visas om inga produkter finns

### Skapa / Redigera produkt (drawer, 600px)
Sidopanel med expanderbara sektioner:

**1. Produktinfo** (alltid redigerbar)
27 fält: produktnamn, GTIN-typ/värde, beskrivning, foto-URL, artikelnummer, varunummer (HS/CN), försäljningsår/säsong, pris, kategori, produktgrupp, typ/linje/koncept, artikeltyp, åldersgrupp, kön, marknadssegment, vattenbeständighet, nettovikt/enhet, databärare, aktiv-checkbox (bara vid redigering)

**2. Varianter** (bara vid redigering)
Inline DataGrid: Artnr, Storlek, Färg (brand), Färg (generell), GTIN. Lägga till / ta bort varianter.

**3. Komponenter** (bara vid redigering)
Inline DataGrid: Komponent, Material, Innehåll, %, Källa. Lägga till / ta bort komponenter.

**4. Skötselinfo** (bara vid redigering)
Skötsel-bild-URL, skötseltext (fritext), säkerhetsinformation. Separat Spara-knapp.

**5. Compliance** (bara vid redigering)
9 fält: farliga ämnen (+ info), certifieringar (+ validering), kemisk compliance (standard + validering + länk), mikrofibrer, spårbarhetsleverantör. Separat Spara-knapp.

**6. Cirkularitet** (bara vid redigering)
9 fält: prestanda, återvinningsbarhet, insamlingsinstruktioner, återvinningsinstruktioner, demontering (sorterare & användare), cirkulär design (strategi & beskrivning), reparationsinstruktioner. Separat Spara-knapp.

**7. Hållbarhet** (bara vid redigering)
Varumärkesuttalande, länk, miljöavtryck. Separat Spara-knapp.

### Åtgärder
- **Ny produkt**: öppnar skapa-drawer
- **Redigera**: dubbelklick eller ⋮-meny → öppnar drawer med alla sektioner
- **Ta bort**: ⋮-meny → bekräftelsedialog
- **Växla aktiv/inaktiv**: ⋮-meny → direkt API-anrop

---

## Flik 2: Batcher

### Lista
- Grupperad per produkt (expanderbar rubrik med produktnamn + batchantal)
- DataGrid per produktgrupp: **Batchnummer**, **PO-nummer**, **Leverantör**, **Antal**, **Status** (badge), **Åtgärdsmeny (⋮)**
- Sökfält (filtrerar på batchnummer, produktnamn eller leverantörsnamn)
- Statusfilter: Planerad / I produktion / Klar (multi-select toggle-knappar)

### Skapa / Redigera batch (drawer)
Fält: Batchnummer, Produkt (dropdown, bara vid ny), Leverantör (dropdown), PO-nummer, Antal, Status, Produktionsdatum.

### Hantera material (separat drawer-läge)
- Lista kopplade material med ta bort-knapp
- Dropdown för att lägga till nytt material från leverantörens materialbibliotek

### Visa artiklar (separat drawer-läge)
- Generera artiklar: ange antal + klicka "Generera artiklar"
- DataGrid: Unikt produkt-ID, Serienummer, SGTIN, TID, Status, ta bort-knapp

### Åtgärder
- **Ny batch**: öppnar skapa-drawer med produktval
- **Redigera**: dubbelklick eller ⋮-meny
- **Ta bort**: ⋮-meny → bekräftelsedialog
- **Hantera material**: ⋮-meny → material-drawer
- **Visa artiklar**: ⋮-meny → artikel-drawer

---

## Flik 3: Leverantörer

### Lista (skrivskyddad)
- Grupperad per leverantör (expanderbar rubrik med leverantörsnamn + antal tyger + plats)
- Material visas som klickbara kort: materialnamn, materialtyp, status-badge
- Sökfält (filtrerar på leverantörsnamn eller materialnamn)
- Inga skapa/redigera/ta bort-funktioner

### Materialdetalj (drawer, skrivskyddad)
Klicka på ett materialkort öppnar en drawer med:

**1. Materialinfo**: Materialnamn, Materialtyp, Beskrivning

**2. Komposition**: DataGrid med Material, Andel, Källa, Återvunnet, Återv.%

**3. Certifieringar**: DataGrid med Certifiering, ID, Giltig t.o.m.

**4. Leveranskedja**: DataGrid med Steg, Anläggning, Land

---

## Gemensamma mönster

| Mönster | Beskrivning |
|---------|-------------|
| Drawer-panel | All redigering sker i sidopanel (600px), inga modala dialoger |
| Multi-select filter | Toggle-knappar med OR-logik, sparas mellan sessioner |
| Statusrad | Visar brandnamn, produktantal, batchantal |
| Språkväxling | SV/EN-knappar i statusraden |
| Tom-vy | EmptyState-kontroll med beskrivande text |
| DynamicResource | Alla UI-texter via ResourceDictionary (sv/en) |
| Expanderbara sektioner | Produktsektioner och grupperingar med Expander |
| Högerjusterade numeriska kolumner | Antal, Varianter, % — konsekvent i alla DataGrids |
