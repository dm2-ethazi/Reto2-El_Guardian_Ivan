# Contenedor `.hgw` HOG2 — especificacion de interoperabilidad

Esta es la especificacion compartida por la aplicación de JavaFX del reto (AesFileCipher).
Android implementa este mismo formato en `HgwDecryptor.kt`.

| Parametro | Valor |
|---|---|
| Cifrado | AES-256-GCM (`AES/GCM/NoPadding`) |
| Derivacion | PBKDF2-HMAC-SHA-256 (`PBKDF2WithHmacSHA256`) |
| Iteraciones | 200 000 |
| Clave | 256 bits |
| Contraseña | UTF-8 mediante `PBEKeySpec` |
| Sal | 16 bytes aleatorios |
| Nonce base | 12 bytes aleatorios |
| Etiqueta | 16 bytes por trozo |
| Trozo | Tamaño uint32 BE en cabecera (entre 1 y 64 MiB) |

## Cabecera (37 bytes)

```
0..3      "HOG2" (4 bytes)
4         Version 0x01
5..20     Sal PBKDF2 (16 bytes)
21..32    Nonce base (12 bytes)
33..36    Tamano trozo uint32 big-endian (4 bytes)
```

## Cada trozo AES-GCM

- Nonce del trozo `i`: nonce base con sus 4 ultimos bytes XOR `i` uint32 big-endian.
- AAD del trozo `i`: cabecera completa (37 bytes) + `i` uint64 big-endian (8 bytes) + indicador de ultimo (1 byte: `01` si es el ultimo, `00` si no).
- Cada trozo contiene datos cifrados + 16 bytes de etiqueta GCM.
- Los trozos no finales ocupan `tamano trozo + 16` bytes.
- El ultimo es siempre mas corto. Cuando el contenido es multiplo exacto de un trozo, se anade un trozo vacio final (solo etiqueta).

## Contenido descifrado

```
1 byte   Longitud de extension N
N bytes  Extension ASCII/UTF8 sin punto: [A-Za-z0-9_-], max. 32
Resto    Bytes exactos del fichero original
```

Android valida el prefijo, escribe el resultado en un fichero temporal y rechaza
claves invalidas, alteraciones de la cabecera, truncamientos, trozos modificados
y extensiones incompatibles con la visualizacion de imagenes.

La contraseña y el nombre original no se guardan en `.hgw`.
