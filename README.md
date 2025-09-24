# PasswordManager (.NET 8, C#)

Gestor de contraseñas de consola con cifrado de extremo a extremo.

## Requisitos
- .NET SDK 8

## Configuración
- Variable de entorno: `PM_VAULT` (ruta al vault cifrado)
- Opcional: archivo `pmconfig.json` en el directorio de trabajo con:
```
{
  "VaultPath": "/ruta/al/vault"
}
```

## Uso rápido
```
# Listar
PM_VAULT=/ruta/.vault dotnet run --project PasswordManager.Cli -- list
# Agregar
PM_VAULT=/ruta/.vault dotnet run --project PasswordManager.Cli -- add
# Eliminar
PM_VAULT=/ruta/.vault dotnet run --project PasswordManager.Cli -- remove <id>
# Generar contraseña
dotnet run --project PasswordManager.Cli -- genpass 20
# Claves RSA
dotnet run --project PasswordManager.Cli -- genkeys pub.key priv.key
# Exportar / Importar
dotnet run --project PasswordManager.Cli -- export <id> pub.key salida.enc
dotnet run --project PasswordManager.Cli -- import priv.key salida.enc
```
## Uso Normal
```
# Ejecutar Aplicación
export PM_VAULT=/ruta/.vault && export PM_PASSWORD="ClaveMaestra123" && dotnet run --project /ruta/PasswordManager.Cli -- list | cat
```

## Seguridad
- Derivación de clave: PBKDF2-HMACSHA256 (210k iteraciones)
- Cifrado: AES-GCM (nonce 12B, tag 16B)
- Asimétrico: RSA 2048 OAEP-SHA256
- Verificación contraseña maestra: HMAC-SHA256 sobre salt derivado

## Estructura
- `PasswordManager.Cli/Crypto`: PBKDF2, AES-GCM, RSA
- `PasswordManager.Cli/Storage`: Vault, repo, formato
- `PasswordManager.Cli/Services`: CLI, generador, export/import

## Convenciones de commits
Ver `CONVENTIONAL_COMMITS.md`.

## Licencia
MIT

