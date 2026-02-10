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
