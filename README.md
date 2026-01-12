# WebServiceVentas

API REST para gestión de ventas construida con ASP.NET Core (.NET 8), Entity Framework Core y PostgreSQL. Incluye autenticación con ASP.NET Identity y JWT, y documentación interactiva con Swagger.

## Descripción

WebServiceVentas centraliza operaciones típicas de un punto de venta:
- Administración de clientes y productos.
- Registro de ventas con descuento de stock y cálculo de total.
- Autenticación de usuarios y roles con emisión de tokens JWT.
- Exposición de endpoints REST con documentación OpenAPI/Swagger.

## Tecnologías

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core + Npgsql (PostgreSQL)
- ASP.NET Identity (usuarios, roles)
- JWT Bearer Authentication
- Swagger/OpenAPI
- CORS (política abierta por defecto para pruebas)

## Estructura del proyecto

- Controllers
  - `AuthController`: registro/login y emisión de JWT.
  - `ClientesController`: CRUD de clientes.
  - `ProductosController`: CRUD de productos.
  - `VentasController`: creación y consulta de ventas.
- Data
  - `VentasDbContext`: DbContext y modelado EF Core.
  - `VentasDbContextFactory`: factory de diseño para migraciones.
- Models: entidades de dominio e identidad.
- `Program.cs`: configuración de servicios y middleware.

## Modelos principales

- Usuario: hereda de `IdentityUser<int>`, añade `Nombre` y `Apellido`.
- Producto: `Id`, `Nombre`, `Descripcion`, `Precio` (decimal 18,2), `Cantidad`.
- Cliente: `Id`, `Nombre`, `NIT`, `Direccion`.
- Venta: `Id`, `ClienteId`, `Fecha`, `Total`, `ProductosVendidos` (lista de `VentaProducto`).
- VentaProducto: `ProductoId`, `Cantidad`, `PrecioUnitario` (+ navegación a `Producto`).

## Autenticación y Autorización

- ASP.NET Identity con roles (`IdentityRole<int>`).
- JWT Bearer configurado en `Program.cs`.
- Swagger incluye esquema `Bearer` para probar endpoints protegidos.

Flujo básico:
1. Registrar usuario (con o sin rol).
2. Login para obtener JWT.
3. Enviar `Authorization: Bearer {token}` en endpoints que requieran autenticación.

## Endpoints principales

- Clientes
  - `POST /api/clientes` — Crea cliente.
  - `GET /api/clientes` — Lista todos.
  - `GET /api/clientes/{id}` — Detalle por Id.
  - `GET /api/clientes/nombre/{nombre}` — Filtro por nombre.
  - `GET /api/clientes/nit/{nit}` — Búsqueda por NIT.

- Productos
  - `GET /api/productos` — Lista de productos.
  - `GET /api/productos/{id}` — Detalle por Id.
  - `GET /api/productos/nombre/{nombre}` — Búsqueda por nombre.
  - `POST /api/productos` — Crea producto.
  - `PUT /api/productos/{id}` — Actualiza por Id.
  - `PUT /api/productos/nombre/{nombre}` — Actualiza por nombre.
  - `DELETE /api/productos/{id}` / `DELETE /api/productos/nombre/{nombre}` — Elimina.

- Ventas
  - `POST /api/ventas` — Crea una venta, descuenta stock y calcula total.
    - Errores comunes: “Cliente no encontrado.”, “Producto X no existe.”, “Stock insuficiente para el producto Y.”
  - `GET /api/ventas` — Lista ventas (incluye cliente y productos).
  - `GET /api/ventas/{id}` — Detalle por Id.
  - `GET /api/ventas/cliente/{clienteId}` — Ventas por cliente.

- Autenticación
  - `POST /api/auth/register` — Registro de usuario y, opcionalmente, creación/asignación de rol.
  - `POST /api/auth/login` — Emite JWT y retorna datos del usuario.

- Utilidad
  - `GET /` — Redirige a `/swagger` (documentación).
  - `GET /healthz` — Health check (“Healthy”).

## Ejemplos de payload

- Crear Cliente
```json
{ "nombre": "Juan", "nit": "1234567", "direccion": "Zona 1" }
```

- Crear Producto
```json
{ "nombre": "Lapicero", "precio": 2.50, "cantidad": 100, "descripcion": "Azul" }
```

- Crear Venta
```json
{
  "clienteId": 1,
  "productos": [
    { "productoId": 1, "cantidad": 2 },
    { "productoId": 3, "cantidad": 1 }
  ]
}
```

- Login
```json
{ "userName": "admin1", "password": "P4ssw0rd!" }
```

Respuesta esperada:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "userName": "admin1",
    "email": "admin@demo.com",
    "nombre": "Ada",
    "apellido": "Lovelace",
    "roles": ["Admin"]
  }
}
```

