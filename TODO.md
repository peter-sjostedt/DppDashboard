# DPP Dashboard - TODO

## Inaktiva tyger (is_active)
Tabellen `factory_materials` har redan kolumnen `is_active BOOLEAN DEFAULT TRUE`.

**Scenario:** En supplier slutar erbjuda ett visst tyg (utgått ur sortimentet), men historisk data ska finnas kvar för redan producerade batchar.

**Plan:**
- [ ] Sätt `is_active = FALSE` när en supplier avvecklar ett tyg
- [ ] Dölj inaktiva tyger vid val av tyg för nya batchar
- [ ] Behåll historiska `batch_materials`-kopplingar intakta för DPP-export
- [ ] Visa inaktiva tyger gråtonade eller med "(inaktivt)" i SuppliersView
- [ ] Implementera toggle i MaterialEditDialog eller SuppliersView


### Admin API – inkludera räknevärden
- GET `/api/admin/brands` → lägg till `product_count` per brand (COUNT från products-tabellen)
- GET `/api/admin/suppliers` → lägg till `batch_count` per supplier (COUNT från batches-tabellen)
- Visa product_count i Varumärken-fliken och batch_count i Leverantörer-fliken i admin-vyn



### Det är inte ett utvecklings-/testverktyg utan ett som ska användas av våra slutkunder.
Då måste vi validera att de två nycklarna tillhör samma företag. Befintligt schema har LEI (Legal Entity Identifier) på båda tabellerna – det är unikt per juridisk person. NordicCare har 549300NORDICCARE04SE i båda.
Validering vid login:

Verifiera brand-nyckeln → hämta LEI
Verifiera supplier-nyckeln → hämta LEI
Om båda angivna: LEI måste matcha, annars avvisa med "API-nycklarna tillhör inte samma företag"