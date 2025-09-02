# WebServiceVentas

API REST para gestión de ventas construida con ASP.NET Core (.NET 8), Entity Framework Core y PostgreSQL. Incluye autenticación con ASP.NET Identity y JWT, documentación con Swagger y migraciones automáticas al inicio.

- URL de Swagger (local): http://localhost:5248/swagger (el puerto puede variar)
- Health check: GET /healthz

## Tecnologías

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core + Npgsql (PostgreSQL)
- ASP.NET Identity (usuarios, roles)
- JWT Bearer Authentication
- Swagger/OpenAPI
- CORS (política abierta por defecto para pruebas)

## Estructura del proyecto

- WebServiceVentas/
  - Controllers/
    - AuthController.cs (registro/login, emisión de JWT)
    - ClientesController.cs (CRUD básico de clientes)
    - ProductosController.cs (CRUD básico de productos)
    - VentasController.cs (creación y consultas de ventas)
  - Data/
    - VentasDbContext.cs (DbContext + modelado EF Core)
    - VentasDbContextFactory.cs (factory en tiempo de diseño para migraciones)
  - Models/ (entidades de dominio e Identity)
  - Program.cs (configuración de servicios y middleware)
  - appsettings.json (JWT y connection string)

## Modelos principales

- Usuario: hereda de IdentityUser<int>, agrega Nombre y Apellido.
- Producto: Id, Nombre, Descripcion, Precio (decimal 18,2), Cantidad.
- Cliente: Id, Nombre, NIT, Direccion.
- Venta: Id, ClienteId, Fecha, Total, ProductosVendidos (lista de VentaProducto).
- VentaProducto: ProductoId, Cantidad, PrecioUnitario (+ navegación a Producto).

Nota: La precisión de Precio está configurada como 18,2 en OnModelCreating.

## Configuración

1) Requisitos
- .NET SDK 8.0+
- PostgreSQL 13+ accesible (local o remoto)

2) Variables/Configuración (appsettings.json)
- Jwt:
  - Key: clave simétrica para firmar JWT
  - Issuer y Audience: deben coincidir con la validación configurada
- ConnectionStrings:PostgresConnection: cadena de conexión usada por la app en tiempo de ejecución.

Ejemplo (local):
```json
{
  "Jwt": {
    "Key": "cambia-esta-clave",
    "Issuer": "WebServiceVentas",
    "Audience": "WebServiceVentas"
  },
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Port=5432;Database=ventasdb;Username=postgres;Password=postgres123"
  }
}
```

Importante:
- VentasDbContextFactory usa una cadena de conexión propia (hardcodeada a localhost). Úsala para migraciones locales o actualízala según tu entorno para evitar inconsistencias.
- En producción, usa variables de entorno/user-secrets y NO confirmes secretos en el repositorio.

3) Puerto de ejecución
- Si existe la variable de entorno PORT, la app escucha en http://*:{PORT} (útil en Render/PAAS).
- Si no, usa el puerto por defecto de Kestrel/lanzador.

## Ejecutar localmente

```bash
# 1) Restaurar dependencias
dotnet restore

# 2) (Opcional) Crear DB si no existe y aplicar migraciones manualmente
# dotnet ef migrations add InitialCreate
# dotnet ef database update

# 3) Ejecutar (aplicará db.Database.Migrate() al inicio)
dotnet run --project WebServiceVentas/WebServiceVentas.csproj
```

Swagger quedará disponible en /swagger.

## Migraciones de base de datos

La app ejecuta migraciones automáticamente al iniciar:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VentasDbContext>();
    db.Database.Migrate();
}
```

Comandos EF útiles:
```bash
# Agregar migración
dotnet ef migrations add NombreMigracion --project WebServiceVentas --startup-project WebServiceVentas

# Aplicar migraciones
dotnet ef database update --project WebServiceVentas --startup-project WebServiceVentas
```

Si EF no encuentra la cadena en tiempo de diseño, actualizar VentasDbContextFactory.

## Autenticación y Autorización

- ASP.NET Identity con roles (IdentityRole<int>).
- JWT Bearer configurado en Program.cs. Swagger incluye el esquema Bearer.

Flujo básico:
1) Registrar usuario (opcionalmente crear/asignar rol si no existe).
2) Hacer login para obtener JWT.
3) Enviar Authorization: Bearer {token} en las requests que requieran autenticación.

Nota: Actualmente, la mayoría de endpoints no están decorados con [Authorize] y, por tanto, son públicos. Ciertas acciones tienen [AllowAnonymous] explícito. Recomendado: agregar [Authorize] donde corresponda.

### Registro

POST /api/auth/register
```json
{
  "userName": "admin1",
  "email": "admin@demo.com",
  "nombre": "Ada",
  "apellido": "Lovelace",
  "password": "P4ssw0rd!",
  "role": "Admin"
}
```

Respuesta (200):
```json
{
  "id": 1,
  "userName": "admin1",
  "email": "admin@demo.com",
  "nombre": "Ada",
  "apellido": "Lovelace",
  "role": "Admin"
}
```

- Si el rol no existe, se crea y se asigna.

### Login

POST /api/auth/login
```json
{
  "userName": "admin1",
  "password": "P4ssw0rd!"
}
```

Respuesta (200):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "userName": "admin1",
    "email": "admin@demo.com",
    "nombre": "Ada",
    "apellido": "Lovelace",
    "roles": [ "Admin" ]
  }
}
```

En Swagger: botón “Authorize”, ingresar “Bearer {token}”.

## Endpoints

Convención de respuesta:
- Muchas acciones devuelven `{ "data": ... }`. 
- Observación: En ProductosController, los endpoints GET/POST/PUT/DELETE individuales devuelven el objeto directo en algunos casos (no envuelto). Recomendación: unificar a `{ data: ... }`.

### Clientes

- POST /api/clientes
  - Crea cliente.
  - Body:
    ```json
    { "nombre": "Juan", "nit": "1234567", "direccion": "Zona 1" }
    ```
  - Respuesta 201:
    ```json
    { "data": { "id": 1, "nombre": "Juan", "nit": "1234567", "direccion": "Zona 1" } }
    ```

- GET /api/clientes
  - Lista todos.
  - Respuesta 200:
    ```json
    { "data": [ { "id": 1, "nombre": "Juan", "nit": "1234567", "direccion": "Zona 1" } ] }
    ```

- GET /api/clientes/{id}
  - Respuesta 200:
    ```json
    { "data": { "id": 1, "nombre": "Juan", "nit": "1234567", "direccion": "Zona 1" } }
    ```

- GET /api/clientes/nombre/{nombre}
  - Filtro exacto por nombre.
  - Respuesta 200:
    ```json
    { "data": [ ... ] }
    ```

- GET /api/clientes/nit/{nit}
  - Búsqueda por NIT.
  - Respuesta 200:
    ```json
    { "data": { ... } }
    ```

### Productos

- GET /api/productos
  - Lista de productos.
  - Respuesta 200:
    ```json
    { "data": [ { "id": 1, "nombre": "Lapicero", "precio": 2.50, "cantidad": 100, "descripcion": "Azul" } ] }
    ```

- GET /api/productos/{id}
  - Producto por Id.
  - Respuesta 200:
    ```json
    { "id": 1, "nombre": "Lapicero", "precio": 2.50, "cantidad": 100, "descripcion": "Azul" }
    ```

- GET /api/productos/nombre/{nombre}
  - Producto por nombre.
  - Respuesta 200: objeto directo (no envuelto).

- POST /api/productos
  - Crea producto.
  - Body:
    ```json
    { "nombre": "Lapicero", "precio": 2.50, "cantidad": 100, "descripcion": "Azul" }
    ```
  - Respuesta 201: objeto directo.

- PUT /api/productos/{id}
  - Actualiza por Id (requiere que id=producto.Id).
  - Respuesta 204 NoContent.

- PUT /api/productos/nombre/{nombre}
  - Actualiza por nombre (requiere que nombre coincida con body).
  - Respuesta 204 NoContent.

- DELETE /api/productos/{id} y /api/productos/nombre/{nombre}
  - Respuesta 204 NoContent.

### Ventas

- POST /api/ventas
  - Crea una venta, descuenta stock y calcula total.
  - Body:
    ```json
    {
      "clienteId": 1,
      "productos": [
        { "productoId": 1, "cantidad": 2 },
        { "productoId": 3, "cantidad": 1 }
      ]
    }
    ```
  - Respuesta 200:
    ```json
    {
      "data": {
        "id": 10,
        "fecha": "2025-01-31",
        "cliente": { "id": 1, "nombre": "Juan", "nit": "1234567", "direccion": "Zona 1" },
        "total": 7.50,
        "productos": [
          { "productoId": 1, "cantidad": 2, "precioUnitario": 2.50 },
          { "productoId": 3, "cantidad": 1, "precioUnitario": 2.50 }
        ]
      }
    }
    ```
  - Errores comunes: “Cliente no encontrado.”, “Producto X no existe.”, “Stock insuficiente para el producto Y.”

- GET /api/ventas
  - Lista ventas (incluye cliente y productos).
  - Respuesta 200:
    ```json
    { "data": [ { "id": 10, "fecha": "2025-01-31", "cliente": { ... }, "total": 7.50, "productos": [ ... ] } ] }
    ```

- GET /api/ventas/{id}
  - Venta por Id.
  - Respuesta 200:
    ```json
    { "data": { ... } }
    ```

- GET /api/ventas/cliente/{clienteId}
  - Ventas por cliente.
  - Respuesta 200:
    ```json
    { "data": [ ... ] }
    ```

## CORS

- Política por defecto abierta (AllowAnyOrigin/AllowAnyHeader/AllowAnyMethod) aplicada globalmente.
- Recomendación: restringir en producción a orígenes de confianza.

## Notas y recomendaciones

- Unificar la forma de respuesta a `{ data: ... }` para todos los endpoints (ProductosController tiene casos no envueltos).
- Añadir [Authorize] a controladores/acciones que deban requerir autenticación y usar roles via `[Authorize(Roles = "Admin")]` según necesidad.
- Mover secretos (JWT Key, connection strings) a variables de entorno o al sistema de secretos de .NET (User Secrets).
- Revisar VentasDbContextFactory para que coincida con el origen de datos real usado en producción/desarrollo.
- Validaciones adicionales sugeridas:
  - Prevent duplicate `Nombre` de producto si se requiere unicidad.
  - Normalizar búsquedas por nombre (case-insensitive/contains) según UX esperada.

## Endpoints de utilidad

- GET / -> redirige a /swagger
- GET /healthz -> “Healthy”

## Ejemplos con curl

Login y usar token:
```bash
# Login
TOKEN=$(curl -s -X POST http://localhost:5248/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{ "userName": "admin1", "password": "P4ssw0rd!" }' | jq -r .token)

# Llamar endpoint protegido (si se agrega [Authorize] en el futuro)
curl -H "Authorization: Bearer $TOKEN" http://localhost:5248/api/clientes
