# DPP API - Specifikation

## Oversikt

DPP (Digital Product Passport) API for textil- och modeindustrin.
Multi-tenant REST API med JSON request/response.

- **Bas-URL:** `https://din-doman.se/api/...`
- **Format:** JSON (Content-Type: application/json)
- **CORS:** Tillater alla origins

---

## Autentisering

### Tenant API (Brand / Supplier)
Alla vanliga endpoints kraver `X-API-Key` header.

```
X-API-Key: <api_key_fran_brands_eller_suppliers_tabellen>
```

API-nyckeln avgor om du ar autentiserad som **brand** eller **supplier**.

**Atkomstkontroll:**
- **Brand:** Ager produkter, POs, produktdata. Laser batcher/items/material via PO-koppling.
- **Supplier:** Ager fabriksmaterial. Skapar batcher/items under POs riktade till sig. Laser POs riktade till sig.

### Admin API
Admin-endpoints (`/api/admin/*`) kraver `X-Admin-Key` header.

```
X-Admin-Key: dpp_admin_master_key_2024_secure
```

### Publika endpoints (ingen auth)
- `GET /api/tenants/brands` - Lista aktiva brands med API-nycklar
- `GET /api/tenants/suppliers` - Lista aktiva suppliers med API-nycklar

---

## Response-format

### Lyckat svar
```json
{
  "success": true,
  "data": { ... }
}
```

### Felsvar
```json
{
  "success": false,
  "error": "Felbeskrivning"
}
```

### HTTP-statuskoder
| Kod | Betydelse |
|-----|-----------|
| 200 | OK |
| 400 | Valideringsfel / Bad request |
| 401 | Saknad eller ogiltig API-nyckel |
| 403 | Ej behorig (fel roll, inaktivt konto) |
| 404 | Resursen hittades inte |
| 405 | Metoden ej tillaten |
| 500 | Serverfel |

---

## Endpoints

### 1. BRANDS

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/brands` | Lista brands (filtrerade) | X-API-Key |
| GET | `/api/brands/all` | Lista alla brands (for dropdowns) | X-API-Key |
| GET | `/api/brands/{id}` | Hamta ett brand | X-API-Key |
| PUT | `/api/brands/{id}` | Uppdatera brand (bara eget) | X-API-Key (brand) |

**Brand-objekt:**
```json
{
  "id": 1,
  "brand_name": "VardTex",
  "logo_url": "https://...",
  "sub_brand": null,
  "parent_company": "Parent Corp",
  "trader": "Trader AB",
  "trader_location": "Stockholm, Sweden",
  "lei": "ABCDEFGHIJ1234567890",
  "gs1_company_prefix": "7350001",
  "api_key": "...",
  "_is_active": true
}
```

---

### 2. SUPPLIERS

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/suppliers` | Lista suppliers | X-API-Key |
| POST | `/api/suppliers` | Skapa supplier | X-API-Key |
| GET | `/api/suppliers/{id}` | Hamta en supplier | X-API-Key |
| PUT | `/api/suppliers/{id}` | Uppdatera supplier | X-API-Key |
| DELETE | `/api/suppliers/{id}` | Ta bort supplier | X-API-Key |

**Supplier-objekt:**
```json
{
  "id": 1,
  "supplier_name": "Porto Textil Lda",
  "supplier_location": "Porto, Portugal",
  "facility_registry": "GLN",
  "facility_identifier": "7350001000001",
  "country_of_origin_confection": "PT",
  "country_of_origin_dyeing": "PT",
  "country_of_origin_weaving": "PT",
  "api_key": "...",
  "_is_active": true
}
```

---

### 3. BRAND-SUPPLIER RELATIONER

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/brand-suppliers` | Lista relationer | X-API-Key |
| GET | `/api/brand-suppliers/available` | Lista tillgangliga suppliers (brand) | X-API-Key (brand) |
| GET | `/api/brand-suppliers/{id}` | Hamta relation | X-API-Key |
| POST | `/api/brand-suppliers` | Skapa relation (brand) | X-API-Key (brand) |
| PUT | `/api/brand-suppliers/{id}/activate` | Aktivera relation | X-API-Key (brand) |
| PUT | `/api/brand-suppliers/{id}/deactivate` | Inaktivera relation | X-API-Key (brand) |
| DELETE | `/api/brand-suppliers/{id}` | Ta bort relation | X-API-Key (brand) |

---

### 4. PRODUCTS

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/products` | Lista produkter | X-API-Key |
| GET | `/api/brands/{brandId}/products` | Lista produkter for ett brand | X-API-Key |
| POST | `/api/brands/{brandId}/products` | Skapa produkt | X-API-Key (brand) |
| GET | `/api/products/{id}` | Hamta produkt (inkl. care, compliance, etc.) | X-API-Key |
| PUT | `/api/products/{id}` | Uppdatera produkt | X-API-Key (brand) |
| DELETE | `/api/products/{id}` | Ta bort produkt | X-API-Key (brand) |
| GET | `/api/products/{id}/dpp` | Hamta komplett DPP-export | X-API-Key |

**Product-objekt (POST/PUT body):**
```json
{
  "product_name": "Scrubs Tunika",
  "gtin_type": "GTIN",
  "gtin": "7350001000001",
  "description": "Antimikrobiell tunika for vardpersonal",
  "category": "clothing",
  "product_group": "Top",
  "type_line_concept": "Healthcare",
  "type_item": "Tunic",
  "gender": "Unisex",
  "market_segment": "mid-price",
  "water_properties": "Water Repellent",
  "net_weight": 0.250,
  "data_carrier_type": "RFID"
}
```

**Obligatoriskt falt:** `product_name`

---

### 5. PRODUCT COMPONENTS (Materialsammansattning)

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/products/{productId}/components` | Lista komponenter | X-API-Key |
| POST | `/api/products/{productId}/components` | Skapa komponent | X-API-Key (brand) |
| GET | `/api/components/{id}` | Hamta komponent | X-API-Key |
| PUT | `/api/components/{id}` | Uppdatera komponent | X-API-Key (brand) |
| DELETE | `/api/components/{id}` | Ta bort komponent | X-API-Key (brand) |

**Component-objekt:**
```json
{
  "component": "Body fabric",
  "material": "Textile",
  "content_name": "Polyester",
  "content_value": 65.00,
  "content_source": "Recycled",
  "recycled": true,
  "recycled_percentage": 100.00,
  "recycled_input_source": "Post-consumer"
}
```

---

### 6. PRODUCT VARIANTS (Storlek/Farg)

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/products/{productId}/variants` | Lista varianter | X-API-Key |
| POST | `/api/products/{productId}/variants` | Skapa variant | X-API-Key (brand) |
| GET | `/api/variants/{id}` | Hamta variant | X-API-Key |
| PUT | `/api/variants/{id}` | Uppdatera variant | X-API-Key (brand) |
| DELETE | `/api/variants/{id}` | Ta bort variant | X-API-Key (brand) |

**Variant-objekt:**
```json
{
  "item_number": "SCR-TUN-WHT-S",
  "size": "S",
  "size_country_code": "EU",
  "color_brand": "Clinical White",
  "color_general": "white",
  "gtin": "73500010000012"
}
```

---

### 7. CARE INFORMATION

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/products/{productId}/care` | Hamta skotselinfo | X-API-Key |
| PUT | `/api/products/{productId}/care` | Skapa/uppdatera | X-API-Key (brand) |
| DELETE | `/api/products/{productId}/care` | Ta bort | X-API-Key (brand) |

---

### 8. COMPLIANCE INFORMATION

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/products/{productId}/compliance` | Hamta compliance | X-API-Key |
| PUT | `/api/products/{productId}/compliance` | Skapa/uppdatera | X-API-Key (brand) |
| DELETE | `/api/products/{productId}/compliance` | Ta bort | X-API-Key (brand) |

---

### 9. CIRCULARITY INFORMATION

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/products/{productId}/circularity` | Hamta cirkularitet | X-API-Key |
| PUT | `/api/products/{productId}/circularity` | Skapa/uppdatera | X-API-Key (brand) |
| DELETE | `/api/products/{productId}/circularity` | Ta bort | X-API-Key (brand) |

---

### 10. SUSTAINABILITY INFORMATION

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/products/{productId}/sustainability` | Hamta hallbarhet | X-API-Key |
| PUT | `/api/products/{productId}/sustainability` | Skapa/uppdatera | X-API-Key (brand) |
| DELETE | `/api/products/{productId}/sustainability` | Ta bort | X-API-Key (brand) |

---

### 11. PURCHASE ORDERS (Inkopsorder)

Brand skapar POs riktade till en supplier for en specifik produkt. Supplier kan acceptera/avvisa.

**PO-statusflode:**
```
draft --> sent --> accepted --> fulfilled
                     |
                  cancelled (reject)
draft --> cancelled (brand cancel)
sent  --> cancelled (brand cancel)
```

#### Brand-endpoints

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/purchase-orders` | Lista brand:s POs | X-API-Key (brand) |
| POST | `/api/purchase-orders` | Skapa PO (status: draft) | X-API-Key (brand) |
| GET | `/api/purchase-orders/{id}` | Visa PO (inkl. lines + batcher) | X-API-Key |
| PUT | `/api/purchase-orders/{id}` | Uppdatera PO (bara draft) | X-API-Key (brand) |
| DELETE | `/api/purchase-orders/{id}` | Ta bort PO (bara draft, inga batcher) | X-API-Key (brand) |
| PUT | `/api/purchase-orders/{id}/send` | Skicka till supplier (draft -> sent) | X-API-Key (brand) |
| PUT | `/api/purchase-orders/{id}/cancel` | Avbryt (draft/sent -> cancelled) | X-API-Key (brand) |
| GET | `/api/suppliers/{supplierId}/purchase-orders` | Lista POs for supplier | X-API-Key (brand) |
| GET | `/api/products/{productId}/purchase-orders` | Lista POs for produkt | X-API-Key |

#### Supplier-endpoints

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/purchase-orders` | Lista inkommande POs | X-API-Key (supplier) |
| GET | `/api/purchase-orders/{id}` | Visa PO (inkl. lines + batcher) | X-API-Key (supplier) |
| PUT | `/api/purchase-orders/{id}/accept` | Acceptera (sent -> accepted) | X-API-Key (supplier) |
| PUT | `/api/purchase-orders/{id}/reject` | Avvisa (sent -> cancelled) | X-API-Key (supplier) |

**POST /api/purchase-orders - Body:**
```json
{
  "supplier_id": 1,
  "product_id": 1,
  "po_number": "PO-VT-2025-001",
  "quantity": 500,
  "requested_delivery_date": "2025-03-15"
}
```

**Obligatoriska falt:** `supplier_id`, `product_id`, `po_number`

**PUT /api/purchase-orders/{id}/send** kraver att minst 1 PO line finns.

**GET /api/purchase-orders/{id}** returnerar PO med `lines` och `batches` inkluderade.

---

### 12. PURCHASE ORDER LINES (Orderrader)

En PO line specificerar antal per produktvariant (storlek/farg). Brand only, bara pa draft-POs.

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/purchase-orders/{poId}/lines` | Lista orderrader | X-API-Key |
| POST | `/api/purchase-orders/{poId}/lines` | Lagg till rad (bara draft) | X-API-Key (brand) |
| GET | `/api/purchase-order-lines/{id}` | Hamta rad | X-API-Key |
| PUT | `/api/purchase-order-lines/{id}` | Uppdatera rad (bara draft) | X-API-Key (brand) |
| DELETE | `/api/purchase-order-lines/{id}` | Ta bort rad (bara draft) | X-API-Key (brand) |

**POST body:**
```json
{
  "product_variant_id": 1,
  "quantity": 100
}
```

**Obligatoriska falt:** `product_variant_id`, `quantity`

Supplier har read-only-atkomst till PO lines.

---

### 13. BATCHES (Produktionsbatcher)

En batch = en produktionsomgang med specifika material-inputs. Supplier skapar batcher under accepted POs. Flera batcher per PO om fabriken byter tygrulle.

**Batch-statusflode:**
```
in_production --> completed
```

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/batches` | Lista alla batcher (filtrerat per tenant) | X-API-Key |
| GET | `/api/batches?status=in_production` | Filtrera pa status | X-API-Key |
| GET | `/api/purchase-orders/{poId}/batches` | Lista batcher for PO | X-API-Key |
| POST | `/api/purchase-orders/{poId}/batches` | Skapa batch (PO maste vara accepted) | X-API-Key (supplier) |
| GET | `/api/batches/{id}` | Hamta batch (inkl. materials) | X-API-Key |
| PUT | `/api/batches/{id}` | Uppdatera batch (bara in_production) | X-API-Key (supplier) |
| PUT | `/api/batches/{id}/complete` | Slutfor batch (in_production -> completed) | X-API-Key (supplier) |
| DELETE | `/api/batches/{id}` | Ta bort batch (bara in_production, inga items) | X-API-Key (supplier) |

**POST /api/purchase-orders/{poId}/batches - Body:**
```json
{
  "batch_number": "PT-2025-001",
  "production_date": "2025-01-15",
  "quantity": 350,
  "facility_name": "Override Factory Name",
  "facility_location": "Override Location",
  "facility_registry": "GLN",
  "facility_identifier": "1234567890",
  "country_of_origin_confection": "PT",
  "country_of_origin_dyeing": "PT",
  "country_of_origin_weaving": "PT"
}
```

**Obligatoriskt falt:** `batch_number`

Facility-falt (7 st) ar overrides — NULL innebar att supplier-defaults fran PO:ns supplier anvands.

Brand har read-only-atkomst till batcher.

**GET /api/batches** returnerar aven `po_number`, `product_name`, `supplier_name`/`brand_name` och `item_count`.

---

### 14. BATCH MATERIALS (Koppling batch <-> fabriksmaterial)

Supplier valjer vilka tygleveranser som anvands i en batch. Brand har read-only.

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/batches/{batchId}/materials` | Lista material i batch | X-API-Key |
| POST | `/api/batches/{batchId}/materials` | Koppla material till batch | X-API-Key (supplier) |
| GET | `/api/batch-materials/{id}` | Hamta koppling | X-API-Key |
| PUT | `/api/batch-materials/{id}` | Uppdatera koppling | X-API-Key (supplier) |
| DELETE | `/api/batch-materials/{id}` | Ta bort koppling | X-API-Key (supplier) |

**POST body:**
```json
{
  "factory_material_id": 1,
  "component": "Body fabric"
}
```

**Obligatoriskt falt:** `factory_material_id`

---

### 15. ITEMS (Individuella serialiserade produkter)

Supplier skapar items under batcher. Brand har read-only.

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/batches/{batchId}/items` | Lista items i batch | X-API-Key |
| POST | `/api/batches/{batchId}/items` | Skapa item | X-API-Key (supplier) |
| POST | `/api/batches/{batchId}/items/bulk` | Skapa flera items | X-API-Key (supplier) |
| GET | `/api/items/{id}` | Hamta item via ID | X-API-Key |
| GET | `/api/items/sgtin/{sgtin}` | Hamta item via SGTIN | X-API-Key |
| DELETE | `/api/items/{id}` | Ta bort item | X-API-Key (supplier) |

**POST body (singel):**
```json
{
  "product_variant_id": 1,
  "unique_product_id": "UPD-001",
  "tid": "E28011700000020ABC",
  "sgtin": "7350001.000001",
  "serial_number": "SN-12345"
}
```

**POST body (bulk):**
```json
{
  "product_variant_id": 1,
  "quantity": 100,
  "serial_prefix": "SN-"
}
```

**Item-status:** `produced`, `shipped`, `sold`, `returned`, `recycled`

---

### 16. FACTORY MATERIALS (Fabriksmaterial)

Supplier ager sina material. Brand laser via brand_suppliers-relation.

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/materials` | Lista alla material | X-API-Key |
| GET | `/api/suppliers/{supplierId}/materials` | Lista material for supplier | X-API-Key |
| POST | `/api/suppliers/{supplierId}/materials` | Skapa material | X-API-Key (supplier) |
| GET | `/api/materials/{id}` | Hamta material | X-API-Key |
| PUT | `/api/materials/{id}` | Uppdatera material | X-API-Key (supplier) |
| DELETE | `/api/materials/{id}` | Ta bort material | X-API-Key (supplier) |
| GET | `/api/materials/{materialId}/batches` | Lista batcher som anvander materialet | X-API-Key |

**FactoryMaterial-objekt:**
```json
{
  "material_name": "Polyester-Cotton Blend 180gsm",
  "material_type": "Textile",
  "description": "65/35 poly-cotton, antimikrobiell finish"
}
```

---

### 17. MATERIAL COMPOSITIONS

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/materials/{materialId}/compositions` | Lista sammansattning | X-API-Key |
| POST | `/api/materials/{materialId}/compositions` | Lagg till | X-API-Key (supplier) |
| GET | `/api/compositions/{id}` | Hamta | X-API-Key |
| PUT | `/api/compositions/{id}` | Uppdatera | X-API-Key (supplier) |
| DELETE | `/api/compositions/{id}` | Ta bort | X-API-Key (supplier) |

**Composition-objekt:**
```json
{
  "content_name": "Polyester",
  "content_value": 65.00,
  "content_source": "Recycled",
  "recycled": true,
  "recycled_percentage": 100.00,
  "recycled_input_source": "Post-consumer"
}
```

---

### 18. MATERIAL CERTIFICATIONS

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/materials/{materialId}/certifications` | Lista certifieringar | X-API-Key |
| POST | `/api/materials/{materialId}/certifications` | Lagg till | X-API-Key (supplier) |
| GET | `/api/material-certifications/{id}` | Hamta | X-API-Key |
| PUT | `/api/material-certifications/{id}` | Uppdatera | X-API-Key (supplier) |
| DELETE | `/api/material-certifications/{id}` | Ta bort | X-API-Key (supplier) |

**Certification-objekt:**
```json
{
  "certification": "GOTS",
  "certification_id": "CU-GOTS-12345",
  "valid_until": "2026-12-31"
}
```

---

### 19. MATERIAL SUPPLY CHAIN

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/materials/{materialId}/supply-chain` | Lista supply chain | X-API-Key |
| POST | `/api/materials/{materialId}/supply-chain` | Lagg till steg | X-API-Key (supplier) |
| GET | `/api/supply-chain/{id}` | Hamta steg | X-API-Key |
| PUT | `/api/supply-chain/{id}` | Uppdatera steg | X-API-Key (supplier) |
| DELETE | `/api/supply-chain/{id}` | Ta bort steg | X-API-Key (supplier) |

**SupplyChain-objekt:**
```json
{
  "sequence": 1,
  "process_step": "spinning",
  "country": "PT",
  "facility_name": "Porto Spinning Mill",
  "facility_identifier": "GLN-123456"
}
```

---

### 20. DPP EXPORT

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/products/{id}/dpp/preview` | Forhandsvisning av DPP | X-API-Key |
| GET | `/api/products/{id}/dpp/validate` | Validera DPP-data | X-API-Key |
| GET | `/api/products/{id}/dpp/export` | Exportera DPP | X-API-Key |

---

### 21. DASHBOARD SUMMARY

Rollbaserad sammanfattning av systemstatus.

| Metod | Endpoint | Beskrivning | Auth |
|-------|----------|-------------|------|
| GET | `/api/dashboard/summary` | Hamta dashboard-data | X-API-Key |

**Brand-response:**
```json
{
  "pending_orders": 2,
  "completed_batches": 3,
  "incomplete_products": 4,
  "export_ready": 5,
  "expiring_certifications": 2
}
```

- `pending_orders` — POs med status `sent` eller `rejected`
- `completed_batches` — Batcher med status `completed` for brand:s POs
- `incomplete_products` — Produkter som saknar care, compliance, components eller variants
- `export_ready` — Kompletta produkter utan befintlig DPP-export
- `expiring_certifications` — Materialcertifieringar som gar ut inom 30 dagar (via brand_suppliers)

**Supplier-response:**
```json
{
  "pending_orders": 3,
  "batches_without_materials": 1,
  "batches_without_items": 2,
  "incomplete_materials": 1,
  "expiring_certifications": 2
}
```

- `pending_orders` — POs med status `sent`
- `batches_without_materials` — Batcher i `in_production` utan kopplade material
- `batches_without_items` — Batcher i `in_production` med 0 items
- `incomplete_materials` — Factory materials utan compositions
- `expiring_certifications` — Certifieringar som gar ut inom 30 dagar eller redan utgangna

---

### 22. ADMIN API

Alla admin-endpoints kraver `X-Admin-Key` header.

#### Brands
| Metod | Endpoint | Beskrivning |
|-------|----------|-------------|
| GET | `/api/admin/brands` | Lista alla brands |
| POST | `/api/admin/brands` | Skapa brand |
| GET | `/api/admin/brands/{id}` | Hamta brand |
| PUT | `/api/admin/brands/{id}` | Uppdatera brand |
| DELETE | `/api/admin/brands/{id}` | Ta bort brand |
| POST | `/api/admin/brands/{id}/regenerate-key` | Ny API-nyckel |

#### Suppliers
| Metod | Endpoint | Beskrivning |
|-------|----------|-------------|
| GET | `/api/admin/suppliers` | Lista alla suppliers |
| POST | `/api/admin/suppliers` | Skapa supplier |
| GET | `/api/admin/suppliers/{id}` | Hamta supplier |
| PUT | `/api/admin/suppliers/{id}` | Uppdatera supplier |
| DELETE | `/api/admin/suppliers/{id}` | Ta bort supplier |
| POST | `/api/admin/suppliers/{id}/regenerate-key` | Ny API-nyckel |

#### Relationer
| Metod | Endpoint | Beskrivning |
|-------|----------|-------------|
| GET | `/api/admin/relations` | Lista relationer |
| POST | `/api/admin/relations` | Skapa relation |
| GET | `/api/admin/relations/{id}` | Hamta relation |
| PUT | `/api/admin/relations/{id}` | Uppdatera relation |
| DELETE | `/api/admin/relations/{id}` | Ta bort relation |

#### Statistik
| Metod | Endpoint | Beskrivning |
|-------|----------|-------------|
| GET | `/api/admin/stats` | Hamta systemstatistik |

---

## Datamodell

```
brands (1) --------< brand_suppliers >-------- (1) suppliers
  |                                                   |
  v                                                   v
products (1) --< purchase_orders >-- (1) suppliers    factory_materials
  |                   |                                  |
  |-- components      |-- purchase_order_lines           |-- compositions
  |-- variants        |-- batches                        |-- certifications
  |-- care (1:1)           |                             |-- supply_chain
  |-- compliance           |-- batch_materials --< factory_materials
  |-- circularity (1:1)    |-- items
  |-- sustainability (1:1)
  |-- dpp_exports
```

**Nyckelrelationer:**
- `purchase_orders` har `brand_id`, `supplier_id`, `product_id`
- `batches` har `purchase_order_id` (arver brand/supplier/product via PO)
- `items` har `batch_id` (arver allt via batch -> PO)
- Atkomstkontroll gar alltid via `purchase_orders`-kedjan

---

## Atkomstkontroll — Sammanfattning

| Resurs | Brand | Supplier |
|--------|-------|----------|
| Products | CRUD (egna) | Read (via PO) |
| Purchase Orders | CRUD + send/cancel | Read + accept/reject |
| PO Lines | CRUD (bara draft) | Read-only |
| Batches | Read-only | CRUD + complete |
| Batch Materials | Read-only | CRUD |
| Items | Read-only | Create + Read + Delete |
| Factory Materials | Read (via relation) | CRUD (egna) |
| Dashboard | Brand-metriker | Supplier-metriker |


## Testdata API-nycklar
**- VårdTex (brand): `brand_vardtex_test_key_000`
- MediWear (brand): `brand_mediwear_test_key_001`
- HealthTex (brand): `brand_healthtex_test_key_002`
**- Têxtil Hospitalar Porto (supplier): `supplier_porto_test_key_000`
- Baltic Textile Factory (supplier): `supplier_baltic_test_key_001`
- Aegean Healthcare Fabrics (supplier): `supplier_aegean_test_key_002`
**- Admin: `dpp_admin_master_key_2024_secure`

## API bas-URL
`https://dpp.petersjostedt.se`