# Publicar Dudiver Music en la Microsoft Store

Todo lo empaquetable ya está en el repo (proyecto de empaquetado, manifiesto, íconos y el
MSIX se construye con un comando). Lo que **solo podés hacer vos** es lo que necesita tu
cuenta de desarrollador. Esta guía cubre ambas partes.

## Qué ya está listo (en el repo)
- `DudiverMusic.Package/` — proyecto de empaquetado (`.wapproj`) + `Package.appxmanifest`.
- `DudiverMusic.Package/Images/` — los ~36 íconos/mosaicos en todos los tamaños.
- `build-msix.ps1` — construye el `.msix` con un comando.
- MSIX verificado: se genera un paquete **x64 autocontenido (~77 MB)**; el usuario final
  **no necesita instalar .NET**.
- Política de privacidad y términos publicados (obligatorio para la Store):
  https://app.music.dudiver.net/privacy · https://app.music.dudiver.net/terms
- Texto de la ficha (ES + EN) en `docs/store-listing.md`.

---

## Paso 1 — Cuenta de desarrollador (una sola vez)  ·  *solo vos*
1. Entrá a **https://partner.microsoft.com/dashboard** e iniciá sesión con tu cuenta Microsoft.
2. Registrate como desarrollador individual (**pago único ~US$19**).

## Paso 2 — Reservar el nombre  ·  *solo vos*
1. En Partner Center: **Apps and games → New product → MSIX or PWA app**.
2. Reservá el nombre: **Dudiver Music** (si está libre).
3. Anotá, en **Product → Product identity**, estos tres valores:
   - **Package/Identity/Name** (ej. `1234RoberthDudiver.DudiverMusic`)
   - **Package/Identity/Publisher** (ej. `CN=ABCD1234-...`)
   - **Publisher display name**

## Paso 3 — Asociar el paquete con esos valores
Tenés dos formas. La **A (Visual Studio)** es la más simple y recomendada.

### Opción A — Visual Studio (recomendada)
1. Abrí `dudiver-music.sln`… (o abrí la carpeta) y agregá/abrí el proyecto
   **DudiverMusic.Package**.
2. Clic derecho en **DudiverMusic.Package → Publicar → Asociar app con la Store**.
3. Iniciá sesión con tu cuenta y elegí **Dudiver Music**. VS escribe automáticamente el
   `Name` y `Publisher` correctos en `Package.appxmanifest`.
4. Clic derecho → **Publicar → Crear paquetes de aplicación → Microsoft Store**.
   - Arquitecturas: marcá **x64** (y **ARM64** si querés; el `.wapproj` ya lo soporta).
   - Genera un `.msixupload` firmado y listo para subir.

### Opción B — Editar el manifiesto a mano + `build-msix.ps1`
1. En `DudiverMusic.Package/Package.appxmanifest`, reemplazá:
   - `Name="RoberthDudiver.DudiverMusic"` → tu **Package/Identity/Name** del Paso 2.
   - `Publisher="CN=Roberth Dudiver"` → tu **Package/Identity/Publisher** del Paso 2.
   - `<PublisherDisplayName>` → tu nombre de editor.
2. Construí el paquete:
   ```powershell
   .\build-msix.ps1                 # x64
   .\build-msix.ps1 -Platform arm64 # (opcional) ARM64
   ```
   El `.msix` queda en `_msix\`. **No lo firmes** para la Store: Microsoft lo firma al publicar.

## Paso 4 — Probar la app localmente (opcional, antes de subir)
Para instalarla en tu PC hay que firmarla con un certificado de prueba:
```powershell
.\build-msix.ps1 -Sign
```
Luego confiá en el certificado de prueba (una vez, como admin) e instalá haciendo doble
clic en el `.msix`. Este cert es **solo para probar**, no sirve para la Store.

## Paso 5 — Completar la ficha y enviar  ·  *solo vos*
En Partner Center, dentro del producto:
1. **Packages**: subí el `.msixupload` (VS) o el `.msix` (build manual).
2. **Store listings → Español (es)** y **Inglés (en-us)**: pegá los textos de
   `docs/store-listing.md`.
3. **Screenshots**: subí 1–8 capturas (ver "Capturas" en `docs/store-listing.md`).
4. **Properties**: Categoría **Música**; **Política de privacidad**:
   `https://app.music.dudiver.net/privacy`; contacto de soporte: `rdudiver@gmail.com`.
5. **Age ratings**: completá el cuestionario IARC (queda apta para todos).
6. **Pricing**: Gratis. Mercados: todos.
7. **Submit for certification**. La revisión suele tardar de unas horas a ~1–2 días.

---

## Notas técnicas
- La versión de la app sale de `Package.appxmanifest → Identity/Version` (hoy `1.0.0.0`).
  Para actualizar, subí ese número y volvé a generar/subir.
- El paquete es **x64 autocontenido** (trae el runtime .NET 10), así que corre en cualquier
  Windows 10 1809+ / 11 sin dependencias.
- Requisitos de build: Visual Studio 2022/2026 con **"Desarrollo de escritorio con .NET"**
  y **"Herramientas de empaquetado de aplicaciones de Windows (MSIX)"** + Windows SDK.
- Si `build-msix.ps1` no encuentra MSBuild, instalá esos componentes desde el
  Visual Studio Installer.
