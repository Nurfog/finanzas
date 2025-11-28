# Financial Analytics Application

Sistema de análisis financiero con capacidades de IA que analiza datos históricos de clientes, ingresos, uso de salas y rendimiento de estudiantes, generando predicciones e informes automáticos.

## 🚀 Características

- **Análisis de Ingresos**: Análisis detallado de ingresos por sede, método de pago y período
- **Predicciones con IA**: Predicción de ingresos futuros usando ML.NET
- **Segmentación de Clientes**: Clustering automático de clientes por comportamiento
- **Análisis de Uso de Salas**: Optimización y predicción de utilización de salas
- **Análisis de Estudiantes**: Seguimiento de rendimiento académico y predicciones
- **Generación de Informes**: Informes automáticos en JSON
- **Entrenamiento Automático**: Los modelos de IA se re-entrenan automáticamente cada 24 horas

## 🛠️ Stack Tecnológico

### Backend
- **ASP.NET Core 8.0** - Framework web
- **Entity Framework Core** - ORM
- **MySQL** - Base de datos
- **ML.NET** - Machine Learning
- **Pomelo.EntityFrameworkCore.MySql** - Proveedor MySQL

### Frontend (próximamente)
- **React** - UI Framework
- **Vite** - Build tool
- **Chart.js** - Visualización de datos
- **TailwindCSS** - Estilos

## 📋 Requisitos

- .NET 8.0 SDK
- MySQL 8.0+
- IIS (para deployment en Windows)
- Node.js 18+ (para frontend)

## 🔧 Configuración

### 1. Configurar Base de Datos

Edita `FinancialAnalytics.API/appsettings.json` y actualiza la cadena de conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=financial_analytics;User=tu_usuario;Password=tu_password;"
  }
}
```

### 2. Crear Base de Datos

La aplicación creará automáticamente la base de datos y las tablas con datos de ejemplo en el primer arranque (modo Development).

Si prefieres usar migraciones manualmente:

```bash
cd FinancialAnalytics.API
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Ejecutar la Aplicación

```bash
cd FinancialAnalytics.API
dotnet run
```

La API estará disponible en:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`

## 📡 Endpoints de la API

### Analytics
- `GET /api/analytics/revenue` - Análisis de ingresos
- `GET /api/analytics/revenue/by-location` - Ingresos por sede
- `GET /api/analytics/revenue/predictions?locationId={id}&monthsAhead={n}` - Predicciones de ingresos
- `GET /api/analytics/customers/segments` - Segmentación de clientes
- `GET /api/analytics/rooms/usage` - Análisis de uso de salas
- `GET /api/analytics/students/performance` - Análisis de estudiantes

### Customers
- `GET /api/customers` - Listar clientes
- `GET /api/customers/{id}` - Detalle de cliente
- `POST /api/customers` - Crear cliente
- `PUT /api/customers/{id}` - Actualizar cliente

### Reports
- `GET /api/reports` - Listar informes
- `GET /api/reports/{id}` - Obtener informe
- `POST /api/reports/generate/revenue` - Generar informe de ingresos
- `POST /api/reports/generate/students` - Generar informe de estudiantes
- `POST /api/reports/generate/rooms` - Generar informe de salas
- `POST /api/reports/generate/customers` - Generar informe de clientes

## 🤖 Modelos de Machine Learning

La aplicación incluye 4 modelos de ML que se entrenan automáticamente:

1. **Revenue Predictor** (Regresión)
   - Predice ingresos futuros basándose en histórico
   - Algoritmo: FastTree

2. **Customer Segmentation** (Clustering)
   - Agrupa clientes en 3 segmentos por comportamiento
   - Algoritmo: K-Means

3. **Room Usage Predictor** (Regresión)
   - Predice tasa de utilización de salas
   - Algoritmo: FastTree

4. **Student Performance** (Regresión)
   - Predice nivel de rendimiento de estudiantes
   - Algoritmo: FastTree

Los modelos se guardan en `FinancialAnalytics.API/MLModels/Trained/`

## 🚀 Deployment en IIS

### Requisitos
- Windows Server con IIS instalado
- ASP.NET Core Hosting Bundle

### Pasos

1. Ejecutar el script de deployment (como Administrador):

```powershell
.\deploy-iis.ps1
```

2. Configurar la cadena de conexión en el servidor

3. Asegurar que el Application Pool tenga permisos para:
   - Leer/escribir en el directorio de la aplicación
   - Conectarse a MySQL

## 📊 Datos de Ejemplo

La aplicación incluye datos de ejemplo:
- 3 sedes (Santiago Central, Norte, Sur)
- 6 salas
- 5 clientes
- 5 estudiantes
- 120 transacciones (últimos 6 meses)

## 🔐 Seguridad

> [!WARNING]
> Antes de deployment en producción:
> - Cambiar la cadena de conexión
> - Configurar HTTPS
> - Implementar autenticación/autorización
> - Configurar CORS apropiadamente
> - Revisar logs y manejo de errores

## 📝 Próximos Pasos

- [ ] Implementar frontend React
- [ ] Agregar autenticación JWT
- [ ] Exportar informes a PDF/Excel
- [ ] Dashboard en tiempo real
- [ ] Notificaciones por email
- [ ] API de integración con sistemas externos

## 🐛 Troubleshooting

### Error de conexión a MySQL
- Verificar que MySQL esté corriendo
- Verificar credenciales en appsettings.json
- Verificar firewall y permisos de red

### Modelos ML no se entrenan
- Verificar que haya suficientes datos (mínimo 10 registros)
- Revisar logs en consola
- Verificar permisos de escritura en directorio MLModels

### IIS no inicia la aplicación
- Verificar que ASP.NET Core Hosting Bundle esté instalado
- Revisar logs en `logs/stdout`
- Verificar permisos del Application Pool

## 📄 Licencia

Este proyecto es de código abierto.

## 👥 Autor

Desarrollado para análisis financiero empresarial.
