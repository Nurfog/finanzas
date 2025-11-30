# Financial Analytics Application

## Descripción

Aplicación de análisis financiero con capacidades de inteligencia artificial que procesa datos históricos de clientes, ingresos, uso de salas y rendimiento estudiantil, generando predicciones e informes automáticos.

## 🚀 Características

- **Análisis de ingresos**: Detalle por sede, método de pago y período.
- **Predicciones con IA**: Forecast de ingresos futuros usando ML.NET.
- **Segmentación de clientes**: Clustering automático basado en comportamiento.
- **Análisis de uso de salas**: Optimización y predicción de utilización.
- **Análisis de estudiantes**: Seguimiento y predicción de rendimiento académico.
- **Generación de informes**: Salida automática en JSON.
- **Entrenamiento continuo**: Modelos re‑entrenados cada 24 h.

## 🛠️ Stack Tecnológico

### Backend
- **ASP.NET Core 8.0** – Framework web.
- **Entity Framework Core** – ORM.
- **MySQL** – Base de datos relacional.
- **ML.NET** – Machine Learning.
- **Pomelo.EntityFrameworkCore.MySql** – Provider MySQL.

### Frontend (próximamente)
- **React** – UI framework.
- **Vite** – Build tool.
- **Chart.js** – Visualización de datos.
- **TailwindCSS** – Estilos modernos.

## 📋 Requisitos

- .NET 8.0 SDK
- MySQL 8.0+
- IIS (para despliegue en Windows)
- Node.js 18+ (para el frontend)

## 🔧 Configuración

### 1. Configurar la base de datos
Edita `FinancialAnalytics.API/appsettings.json` y actualiza la cadena de conexión:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=financial_analytics;User=root;Password=tu_password;"
  }
}
```

### 2. Crear la base de datos
La aplicación crea automáticamente la base y las tablas con datos de ejemplo en el primer arranque (modo Development). Si prefieres migraciones manuales:
```bash
cd FinancialAnalytics.API

dotnet ef migrations add InitialCreate

dotnet ef database update
```

### 3. Ejecutar la aplicación
```bash
cd FinancialAnalytics.API

dotnet run
```
La API estará disponible en:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `http://localhost:5000/swagger`

## 📡 Endpoints de la API
### Analytics
- `GET /api/analytics/revenue` – Análisis de ingresos.
- `GET /api/analytics/revenue/by-location` – Ingresos por sede.
- `GET /api/analytics/revenue/predictions?locationId={id}&monthsAhead={n}` – Predicciones de ingresos.
- `GET /api/analytics/customers/segments` – Segmentación de clientes.
- `GET /api/analytics/rooms/usage` – Uso de salas.
- `GET /api/analytics/students/performance` – Rendimiento estudiantil.

### Customers
- `GET /api/customers` – Listar clientes.
- `GET /api/customers/{id}` – Detalle de cliente.
- `POST /api/customers` – Crear cliente.
- `PUT /api/customers/{id}` – Actualizar cliente.

### Reports
- `GET /api/reports` – Listar informes.
- `GET /api/reports/{id}` – Obtener informe.
- `POST /api/reports/generate/revenue` – Generar informe de ingresos.
- `POST /api/reports/generate/students` – Generar informe de estudiantes.
- `POST /api/reports/generate/rooms` – Generar informe de salas.
- `POST /api/reports/generate/customers` – Generar informe de clientes.

## 🤖 Modelos de Machine Learning
La aplicación incluye cuatro modelos que se entrenan automáticamente:
1. **Revenue Predictor** (Regresión) – FastTree.
2. **Customer Segmentation** (Clustering) – K‑Means.
3. **Room Usage Predictor** (Regresión) – FastTree.
4. **Student Performance** (Regresión) – FastTree.
Los modelos se guardan en `FinancialAnalytics.API/MLModels/Trained/`.

## 🚀 Deployment en IIS
### Requisitos
- Windows Server con IIS.
- ASP.NET Core Hosting Bundle.
### Pasos
1. Ejecutar el script de despliegue como administrador:
```powershell
./deploy-iis.ps1
```
2. Configurar la cadena de conexión en el servidor.
3. Asegurar que el Application Pool tenga permisos para leer/escribir en el directorio de la aplicación y conectarse a MySQL.

## 📊 Datos de ejemplo
- 3 sedes (Santiago Central, Norte, Sur).
- 6 salas.
- 5 clientes.
- 5 estudiantes.
- 120 transacciones (últimos 6 meses).

## 🔐 Seguridad
> [!WARNING]
> Antes de pasar a producción:
> - Cambiar la cadena de conexión.
> - Configurar HTTPS.
> - Implementar autenticación y autorización.
> - Configurar CORS adecuadamente.
> - Revisar logs y manejo de errores.

## 📝 Próximos pasos
- [ ] Implementar frontend React.
- [ ] Añadir autenticación JWT.
- [ ] Exportar informes a PDF/Excel.
- [ ] Dashboard en tiempo real.
- [ ] Notificaciones por email.
- [ ] API de integración con sistemas externos.

## 🐛 Solución de problemas
### Conexión a MySQL fallida
- Verificar que MySQL esté activo.
- Comprobar credenciales en `appsettings.json`.
- Revisar firewall y permisos de red.
### Modelos ML no entrenan
- Asegurar al menos 10 registros de datos.
- Revisar logs de la consola.
- Verificar permisos de escritura en `MLModels`.
### IIS no inicia la aplicación
- Confirmar instalación del Hosting Bundle.
- Revisar `logs/stdout`.
- Verificar permisos del Application Pool.

## 📄 Licencia
Código abierto bajo licencia MIT.

## 👥 Autor
Desarrollado para análisis financiero empresarial.
