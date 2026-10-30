# Formato del contenedor `.hgw`

Este documento describe el formato de los ficheros cifrados que genera la aplicación JavaFX
(`AesFileCipher`) y los parámetros necesarios para descifrarlos desde Android u otra plataforma.

## Parámetros criptográficos

| Parámetro | Valor |
|---|---|
| Algoritmo | AES-256-GCM (`AES/GCM/NoPadding`) |
| Derivación de clave | PBKDF2-HMAC-SHA-256 (`PBKDF2WithHmacSHA256`) |
| Iteraciones PBKDF2 | 200 000 |
| Longitud de clave | 256 bits (32 bytes) |
| Codificación de la contraseña | UTF-8 (es lo que hace `PBEKeySpec` en Java) |
| Sal | 16 bytes aleatorios, nueva en cada cifrado |
| Nonce base | 12 bytes aleatorios, nuevo en cada cifrado |
| Etiqueta GCM | 16 bytes (128 bits) por trozo |
| Tamaño de trozo | 1 MiB (1 048 576 bytes) por defecto; se lee siempre de la cabecera |

La contraseña no se guarda en ningún sitio: ni en el fichero ni en la aplicación.

## Estructura del fichero

Todos los enteros son big-endian.

```
Cabecera (37 bytes)
  0   4  "HOG2" (48 4F 47 32)       identificador del formato
  4   1  0x01                       versión
  5  16  sal                        para PBKDF2
 21  12  nonce base                 para AES-GCM
 33   4  tamaño de trozo (uint32)   bytes de contenido por trozo

Trozos (uno o más, seguidos hasta el final del fichero)
  trozo 0:  min(tamaño de trozo, lo que quede) bytes cifrados + 16 bytes de etiqueta
  trozo 1:  ...
  último:   SIEMPRE con menos bytes de contenido que el tamaño de trozo (puede tener 0)
```

Cada trozo se cifra por separado con AES-GCM:

- **Nonce del trozo i:** el nonce base con sus 4 últimos bytes (12 al 11) XOR `i` como uint32 big-endian.
- **AAD del trozo i:** `cabecera (37 bytes) || i (uint64 big-endian, 8 bytes) || último (1 byte: 0x01 si es el último, 0x00 si no)`.

Así se detecta si alguien modifica la cabecera, cambia trozos de orden, quita trozos del final o añade trozos.

Cómo saber si un trozo es el último al descifrar: se intentan leer `tamaño de trozo + 16` bytes; si se
leen menos (fin de fichero), ese es el último. Como el último trozo siempre tiene menos contenido que el
tamaño de trozo, un trozo leído completo nunca es el último. Si el contenido mide justo un múltiplo del
tamaño de trozo, se añade al final un trozo vacío (solo los 16 bytes de etiqueta).

## Contenido cifrado

Lo que se cifra (la concatenación del contenido de todos los trozos) no es solo el fichero original:
delante va su extensión, para que viaje cifrada y no se vea desde fuera.

```
N (1 byte)          longitud de la extensión en bytes (0 si el fichero no tenía)
extensión (N bytes) en UTF-8, sin punto, solo [A-Za-z0-9_-], máximo 32 caracteres. Ej.: "png"
bytes del fichero   los bytes originales, exactos
```

Al descifrar hay que quitar ese prefijo. La extensión debe validarse con la misma regla antes de usarla
en un nombre de fichero.

## Descifrar paso a paso

1. Leer 37 bytes. Comprobar `HOG2`, versión `0x01` y que el tamaño de trozo esté entre 1 y 64 MiB.
   Si no, rechazar: "no es un contenedor .hgw válido".
2. Derivar la clave: `PBKDF2-HMAC-SHA-256(contraseña en UTF-8, sal, 200000, 32 bytes)`.
3. Para `i = 0, 1, 2...`: leer `tamaño de trozo + 16` bytes, calcular nonce y AAD como arriba y
   descifrar con AES-GCM. Si falla la etiqueta (`AEADBadTagException` en Java/Android), la contraseña
   es incorrecta o el fichero está alterado: borrar la salida provisional y avisar al usuario.
4. Escribir el contenido de cada trozo en un fichero temporal. Al terminar el último trozo sin errores,
   quitar el prefijo de la extensión y publicar el resultado.

## Limitaciones conocidas

- Máximo 2^32 trozos por fichero (4 PiB con trozos de 1 MiB).
- El nombre original no se guarda, solo la extensión. El usuario elige el nombre de salida.
- PBKDF2 con 200 000 iteraciones tarda una fracción de segundo; es intencionado, para frenar ataques
  de fuerza bruta.
