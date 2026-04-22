# Especificación de Pruebas para Sistema de Geofencing

## Criterios de Aceptación Funcionales

### Alerta Positiva
**Given**: Un operario con dispositivo GPS activo se encuentra fuera de la zona de peligro circular de 50 metros centrada en las coordenadas (latitud, longitud) definidas para la obra.  
**When**: El operario se mueve y sus coordenadas GPS indican que ha entrado dentro del radio de 50 metros de la zona de peligro.  
**Then**: El sistema debe generar una alerta inmediata (sonora, visual o push notification) y registrar el evento en el log de seguridad.

### Falsa Alarma
**Given**: Un operario se encuentra dentro de la zona segura (más de 50 metros del centro de peligro) con coordenadas GPS válidas y estables.  
**When**: No hay cambios en la posición que indiquen entrada en zona de peligro, pero el sistema procesa datos erróneos o ruido de GPS.  
**Then**: El sistema NO debe generar alerta, y debe filtrar el ruido para evitar falsas alarmas.

### Recuperación de Señal
**Given**: La señal GPS se pierde temporalmente mientras el operario está en movimiento cerca del límite de la zona de peligro.  
**When**: La señal GPS se recupera y las coordenadas actualizadas confirman que el operario está dentro de los 50 metros.  
**Then**: El sistema debe evaluar la nueva posición y generar alerta solo si es necesario, sin acumular alertas retrasadas.

## Matriz de Casos de Borde

| Caso de Borde | Descripción | Comportamiento Esperado |
|---------------|-------------|-------------------------|
| Coordenadas Nulas | GPS devuelve valores null o indefinidos | Sistema ignora la lectura y espera la siguiente válida, sin alertar |
| Salto Brusco por Error GPS | Coordenadas saltan de 60m a 40m en una sola lectura | Sistema valida consistencia temporal y no alerta si el salto es irreal (>10m/segundo) |
| Coordenadas Fuera de Rango | Latitud >90° o < -90°, Longitud >180° o < -180° | Sistema rechaza coordenadas inválidas y registra error en log |
| Zona de Peligro con Radio Cero | Configuración accidental de radio=0 | Sistema trata como zona puntual y alerta solo en coordenadas exactas |
| Múltiples Operarios Simultáneos | 100 operarios enviando coordenadas al mismo tiempo | Sistema procesa todas las lecturas sin degradación de rendimiento (<1s por alerta) |
| Pérdida Prolongada de Señal | GPS offline por >5 minutos | Sistema marca operario como "fuera de cobertura" y alerta al supervisor |

Estos criterios aseguran que el sistema de geofencing sea robusto, minimizando riesgos de seguridad industrial en obras de Sacyr.