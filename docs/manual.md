# Manual del Sistema: PasswordManager (.NET 8, C#)

## 1. Descripción General
PasswordManager es una aplicación de consola para gestionar contraseñas con cifrado de extremo a extremo. El vault (almacén) se guarda como un archivo cifrado con AES-GCM, cuya clave se deriva de una contraseña maestra mediante PBKDF2. Incluye exportación/importación de entradas con RSA, generador de contraseñas seguras, menú interactivo y modo no interactivo por variables de entorno/flags.

## 2. Arquitectura y Componentes
- Capa CLI (interfaz de línea de comandos): `Program.cs`
- Servicios de negocio: `Services/`
- Capa de almacenamiento y persistencia: `Storage/`
- Criptografía: `Crypto/`
- Configuración: `Config/`
- Documentación: `README.md`, `CONVENTIONAL_COMMITS.md`, `docs/`

Flujo principal:
1) El usuario define la contraseña maestra (interactiva o por env `PM_PASSWORD`).
2) Se deriva la clave con PBKDF2 a partir del salt del vault.
3) El contenido del vault (entradas) se guarda/lee cifrado con AES-GCM.
4) Exportación/Importación opcional de entradas con RSA.

## 3. Mapeo de Archivos (Qué hace cada archivo)
- `PasswordManager.sln`: Solución .NET de la aplicación.
- `PasswordManager.Cli/PasswordManager.Cli.csproj`: Proyecto de consola (target `net8.0`).
- `PasswordManager.Cli/Program.cs`: Punto de entrada; implementa:
  - Inicio de la app, lectura de `PM_VAULT`/`pmconfig.json`/`PM_PASSWORD`.
  - Parser sencillo de comandos y flags.
  - Menú interactivo (listar, agregar, eliminar, generar contraseña, RSA genkeys/export/import).
- `PasswordManager.Cli/Config/AppConfig.cs`: Carga configuración desde `pmconfig.json` y variables de entorno.
- `PasswordManager.Cli/Services/VaultService.cs`: Lógica de negocio del vault:
  - `Initialize`, `Load`, `ListEntries`, `AddEntry`, `RemoveEntry`, `Persist`.
- `PasswordManager.Cli/Services/PasswordGenerator.cs`: Generación de contraseñas seguras (control de longitud y clases de caracteres).
- `PasswordManager.Cli/Services/ExportImportService.cs`: Exportación/Importación de entradas:
  - `GenerateKeyPair`, `ExportEntry`, `ImportEntry` usando RSA.
- `PasswordManager.Cli/Storage/Vault.cs`: Modelo del vault (versión, salt, verificador, lista de entradas).
- `PasswordManager.Cli/Storage/VaultRepository.cs`: Persistencia:
  - Inicialización si falta el vault.
  - Carga/Guardado con formato `magic(4)+version(1)+len-json(4)+metadata-json+blob-cifrado`.
- `PasswordManager.Cli/Models/PasswordEntry.cs`: Modelo de una entrada (Id, Service, Username, Password, Notes, timestamps).
- `PasswordManager.Cli/Crypto/KeyDerivation.cs`: PBKDF2 (derivación), generación de salt, verificación de contraseña (HMAC-SHA256).
- `PasswordManager.Cli/Crypto/AesGcmCipher.cs`: Cifrado/descifrado AES-GCM empaquetando `nonce|tag|ciphertext`.
- `PasswordManager.Cli/Crypto/RsaToolbox.cs`: Generación de par de claves, cifrado/descifrado OAEP-SHA256, firmas/verificación.
- `README.md`: Guía rápida de uso, configuración y estructura.
- `CONVENTIONAL_COMMITS.md`: Guía de mensajes de commit.
- `docs/report.md`: Informe para PDF (tecnologías, URL, capturas, decisiones).
- `docs/report.pdf`: PDF generado a partir del reporte.
- `docs/manual.md`: Este manual.

## 4. Criptografía (Qué y por qué)
- Derivación de clave: PBKDF2-HMACSHA256 con 210.000 iteraciones y salt aleatorio (16 bytes). Motivo: endurecer ataques de fuerza bruta y asegurar claves únicas por usuario.
- Cifrado simétrico: AES-GCM (AEAD) con nonce de 12 bytes y tag de 16 bytes. Motivo: proporciona confidencialidad e integridad autenticada.
- Criptografía asimétrica: RSA 2048 con OAEP-SHA256. Motivo: exportar/importar entradas de forma segura entre usuarios/instancias.
- Verificación de contraseña maestra: HMAC-SHA256 sobre el salt usando clave derivada. Motivo: validar contraseña sin descifrar contenido.

## 5. Formato del Vault y Persistencia
Archivo binario:
- Magic: `PMV1` (4 bytes)
- Versión: `0x01` (1 byte)
- Longitud JSON de metadata: `int32 little-endian` (4 bytes)
- Metadata JSON: `Vault` sin las entradas (incluye `Salt` y `PasswordVerifier`)
- Blob cifrado: contenido de `Entries` serializado a JSON y cifrado con AES-GCM

La clave simétrica proviene de PBKDF2(master, salt). Para leer:
1) Cargar metadata y derivar clave.
2) Verificar contraseña con `PasswordVerifier` (comparación en tiempo constante).
3) Descifrar blob y deserializar entradas.

## 6. Uso de la CLI
- Interactivo (menú): ejecutar sin argumentos.
- Comandos directos:
  - `add` (interactivo o con flags/env)
  - `list`
  - `remove <id>`
  - `genpass [longitud]`
  - `genkeys <ruta_pub> <ruta_priv>`
  - `export <id> <ruta_pub> <salida>`
  - `import <ruta_priv> <entrada>`

### 6.1 Flags y variables de entorno
- `PM_VAULT`: ruta del archivo vault.
- `PM_PASSWORD`: contraseña maestra no interactiva.
- `PM_SERVICE`, `PM_USERNAME`, `PM_ENTRY_PASSWORD`, `PM_NOTES`: para `add` no interactivo.
- Flags de `add`: `--service`, `--username`, `--password`, `--notes` (o `--flag=valor`).
- Config archivo: `pmconfig.json` en el directorio de trabajo con `{"VaultPath": "/ruta"}`.

### 6.2 Ejemplos
- Listar:
```
PM_VAULT=/ruta/.vault dotnet run --project PasswordManager.Cli -- list
```
- Agregar (no interactivo):
```
PM_VAULT=/ruta/.vault PM_PASSWORD='Secreta' dotnet run --project PasswordManager.Cli -- add --service Github --username usuario --password P@ssw0rd --notes "Cuenta personal"
```
- Generar contraseña:
```
dotnet run --project PasswordManager.Cli -- genpass 20
```
- RSA (claves y export/import):
```
dotnet run --project PasswordManager.Cli -- genkeys pub.key priv.key
dotnet run --project PasswordManager.Cli -- export <id> pub.key salida.enc
dotnet run --project PasswordManager.Cli -- import priv.key salida.enc
```

## 7. Buenas Prácticas Implementadas
- Mensajes de commit según Conventional Commits (`feat:`, `fix:`, `docs:`, `chore:`...).
- Separación de responsabilidades (Crypto/Storage/Services/CLI/Config).
- Uso de APIs criptográficas modernas del framework.
- Comparaciones en tiempo constante para verificación de contraseña.

## 8. Decisiones Técnicas Relevantes
- AES-GCM vs. CBC: GCM evita necesidad de MAC separado y mitiga ataques de padding.
- PBKDF2 iteraciones elevadas (210k) balanceando seguridad y rendimiento en CPU.
- Formato binario con metadata separada: permite validar contraseña antes de descifrar.
- Re-generación de `Id` al importar entradas: evita colisiones en el vault destino.

## 9. Construcción y Ejecución
- Requisitos: .NET SDK 8.
- Compilar: `dotnet build`
- Ejecutar menú: `dotnet run --project PasswordManager.Cli --`
- Configuración vía `PM_VAULT` y/o `pmconfig.json`.

## 10. Seguridad y Limitaciones
- El campo `Password` se almacena en texto claro dentro del vault cifrado; si se desea, puede sustituirse por cifrado por entrada o integrarse con un clipboard seguro.
- Custodia de la contraseña maestra: no recuperable; sin ella no es posible descifrar el vault.
- Exportación con RSA: protege la entrada en tránsito/almacenamiento; el receptor debe proteger su clave privada.

## 11. Extensiones Futuras (Sugerencias)
- Doble factor para desbloqueo (por ejemplo TOTP).
- Soporte de hardware keys (YubiKey) o DPAPI/Linux Keyring.
- Edición y búsqueda de entradas avanzadas.
- Integración con gestores de portapapeles y expiración.

## 12. Licencia y Créditos
- Licencia: MIT
- Autoría: Estructura y desarrollo del ejemplo dentro del repositorio.
