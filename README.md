# Hardware Checker

Aplicacion de diagnostico de hardware para Windows creada con .NET 8, C# y WPF.

## Ejecutar

```powershell
cd HardwareChecker
dotnet build HardwareChecker.sln
dotnet run --project HardwareChecker/HardwareChecker.csproj
```

## MVP incluido

- Arquitectura MVVM con carpetas `Models`, `ViewModels`, `Views`, `Services`, `Helpers` y `Resources`.
- Diagnostico async de CPU, RAM, discos, GPU, bateria, red y dispositivos.
- Lectura real mediante WMI, `PerformanceCounter`, `DriveInfo` y `NetworkInterface`.
- Fallbacks cuando Windows no expone temperatura, SMART, uso de GPU o salud de bateria.
- Resumen visual con estados verde, amarillo, rojo y desconocido.
- Panel profesional con puntuacion de salud, estado general, tarjetas seleccionables y detalle por categoria.
- Informe en pantalla y exportacion a TXT o JSON.
- Deteccion de dispositivos con errores y aviso limitado de drivers potencialmente antiguos.
- Alertas con sugerencias de solucion y acceso rapido a Windows Update, Configuracion de red, Almacenamiento o Administrador de dispositivos.
- Ventana inicial maximizada para aprovechar mejor pantallas de escritorio.
- Icono de aplicacion propio integrado en el ejecutable y en la ventana principal.
- Historial de diagnosticos con comparacion de puntuacion entre ejecuciones.
- Filtros de alertas por severidad y categoria.
- Copia rapida del informe al portapapeles.
- Exportacion de informe en TXT, JSON o HTML.
- Categoria Sistema con BIOS/UEFI, TPM, Secure Boot, fabricante y modelo.
- Monitor en vivo opcional para refrescar CPU/RAM cada pocos segundos tras ejecutar un diagnostico.

Algunos datos dependen del fabricante, drivers y permisos del equipo. En esos casos la aplicacion muestra `No disponible` en vez de bloquear la interfaz.
