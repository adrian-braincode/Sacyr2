# 📋 SPECIFICATION.md – MÓDULO SACYR_MAQUINARIA
## Radiografía de Fronteras: Módulo de Flota y Costes de Maquinaria

---

## 1. ANÁLISIS DE CONTRATO: Frontera del Sistema

### 1.1 Responsabilidad del Módulo
El módulo **sacyr_maquinaria** es responsable de:
- **Gestionar el inventario de maquinaria pesada** vinculado a obras específicas
- **Calcular costes operacionales** (mantenimiento, combustible, depreciación)
- **Proporcionar análisis de rentabilidad** por obra
- **Exportar datos consolidados** hacia el orquestador (`main.py`)

### 1.2 Inputs al Módulo

| Campo | Ubicación | Tipo | Rango Válido | Criticidad |
|-------|-----------|------|--------------|-----------|
| `id_obra` | Obra | String | 1-20 chars, alfanumérico | ✓ CRÍTICO |
| `presupuesto_maximo` | Obra | Float | > 0 | ✓ CRÍTICO |
| `factor_logistico` | Obra | Float | ≥ 1.0 | ✓ CRÍTICO |
| `id_activo` | Maquinaria | String | 1-20 chars, único | ✓ CRÍTICO |
| `precio_compra` | Maquinaria | Float | ≥ 0 | ✓ CRÍTICO |
| `horas_operativas` | Maquinaria | Integer | ≥ 0 | ✓ CRÍTICO |
| `tasa_iva` | Config Externa | Float | [0.0, 1.0] | ✓ CRÍTICO |
| `tasa_diesel_hora` | Config Externa | Float | > 0 | ✓ CRÍTICO |

### 1.3 Outputs del Módulo

| Evento | Estructura | Responsable | Naturaleza |
|--------|-----------|------------|-----------|
| `AnalisisRentabilidad` | Dict con gasto_total_iva, margen, estado | CostService | Respuesta |
| `ListaMaquinaria` | List[Maquinaria] | FlotaRepository | Consulta |
| `ErrorPrecioNegativo` | ValueError | Maquinaria.__post_init__() | Excepción |
| `ErrorPresupuestoNulo` | ValueError | Obra.__post_init__() | Excepción |

---

## 2. REGLA DE DEPENDENCIA: Capas de Desacoplamiento

### 2.1 Diagrama de Capas (Pirámide de Dependencia)

```
┌─────────────────────────────────────────────────────────┐
│  ORQUESTADOR GLOBAL: main.py (ÚNICO PUNTO DE UNIÓN)    │
│  ├─ Importa: CostService, NominaService                 │
│  ├─ Importa: FlotaRepository, PersonalRepository        │
│  ├─ Importa: Obra (modelo compartido)                   │
│  └─ ❌ PROHIBIDO: Importar sacyr_personal directamente  │
└─────────────────────────────────────────────────────────┘
         ▲                                      ▲
         │                                      │
    [LÍMITE]                                [LÍMITE]
         │                                      │
┌────────┴──────────────────┐    ┌─────────────┴─────────┐
│  SACYR_MAQUINARIA         │    │  SACYR_PERSONAL       │
│  (Módulo Independiente)   │    │  (Módulo Independiente)
├──────────────────────────┤    ├─────────────────────┤
│ CostService              │    │ NominaService       │
│  ├─ inyecta: repo        │    │  ├─ inyecta: repo   │
│  ├─ método público:      │    │  ├─ método público: │
│  │  analizar_rentabi...  │    │  │  calcular_coste_ │
│  └─ ❌ sin imports de    │    │  └─ ❌ sin imports de
│     sacyr_personal       │    │     sacyr_maquinaria│
└──────────────┬───────────┘    └──────────────┬──────┘
               │                               │
        ┌──────▼────────┐            ┌────────▼───────┐
        │ FlotaRepository       │            │ PersonalRepository  │
        │  ├─ obtener_maq...   │            │  ├─ obtener_pers... │
        │  └─ por_obra         │            │  └─ por_obra        │
        └──────┬────────┘            └────────┬───────┘
               │                               │
        ┌──────▼────────────────────────────────▼───────┐
        │  MODELOS COMPARTIDOS (bajo src/)              │
        │  ├─ Obra (id_obra, presupuesto, factor)      │
        │  ├─ Maquinaria (id_activo, id_obra, ...)     │
        │  ├─ Operario (id_empleado, id_maquina_...)   │
        │  └─ ❌ NUNCA deben hacer imports cruzados    │
        └─────────────────────────────────────────────┘
```

### 2.2 Límites Explícitos por Carpeta

#### 📁 `sacyr_maquinaria/src/models/`
**Permite:** Importar de `dataclasses`, tipos estándar  
**Prohibe:** ❌ `from sacyr_personal import ...`  
**Responsabilidad:** Definir estructuras `Obra`, `Maquinaria`

#### 📁 `sacyr_maquinaria/src/repositories/`
**Permite:** `from .models import Maquinaria, Obra`  
**Prohibe:** ❌ `from sacyr_personal.src.repositories import ...`  
**Responsabilidad:** Obtener datos de fuente (DB, CSV, etc.)

#### 📁 `sacyr_maquinaria/src/services/`
**Permite:** `from .repositories import FlotaRepository`  
**Prohibe:** ❌ `from sacyr_personal.src.services import NominaService` (solo en `main.py`)  
**Responsabilidad:** Lógica de costes, inyección de dependencias

---

## 3. LÍMITES FÍSICOS Y DE NEGOCIO

### 3.1 Restricciones de Validación Obligatorias

| Restricción | Ubicación | Acción | Impacto |
|-------------|-----------|--------|---------|
| `precio_compra ≥ 0` | Maquinaria.__post_init__() | ValueError | CRÍTICO |
| `horas_operativas ≥ 0` | Maquinaria.__post_init__() | Reset a 0 | ADVERTENCIA |
| `presupuesto > 0` | Obra.__post_init__() | ValueError | CRÍTICO |
| `factor_logistico ≥ 1.0` | Obra.__post_init__() | Reset a 1.0 | ADVERTENCIA |
| `tasa_iva ∈ [0.0, 1.0]` | CostService.analizar_rentabilidad_obra() | Validar antes de cálculo | CRÍTICO |
| `id_obra no nulo` | Maquinaria, Operario | Constraint | CRÍTICO |

### 3.2 Protocolo de Error

```python
# EJEMPLO: CostService debe capturar excepciones sin propagarlas a sacyr_personal
try:
    resultado = servicio_maquinaria.analizar_rentabilidad_obra(...)
except ValueError as e:
    # Manejo LOCAL, sin exponer implementación interna
    log_error(f"Maquinaria: {e}")
    raise ValueError("Error en análisis de maquinaria") from None
```

---

## 4. CONTRATO DE DATOS: Interfaz con main.py

### 4.1 Método Público de sacyr_maquinaria

**Firma:**
```python
def analizar_rentabilidad_obra(self, 
                                 obra: Obra, 
                                 tasa_iva: float, 
                                 tasa_diesel: float) -> Dict
```

**Retorna:**
```python
{
    "obra_nombre": str,                    # Nombre de la obra
    "presupuesto": float,                  # Presupuesto máximo
    "gasto_total_iva": float,              # Costo TOTAL incluyendo IVA
    "margen": float,                       # Presupuesto - Gasto
    "estado": str,                         # "SALUDABLE" | "ALERTA" | "DÉFICIT CRÍTICO"
    "activos_vinculados": List[Dict]       # [{"id": ..., "nombre": ..., "neto": ...}]
}
```

**Garantías:**
- ✓ Retorna siempre un Dict válido
- ✓ Las claves son idénticas siempre
- ✓ Los valores numéricos son ≥ 0
- ✓ No modifica el estado de `Obra` o `Maquinaria`

---

## 5. CONFIGURACIÓN: Aislamiento de Parámetros

### 5.1 Archivo settings.yaml

**Ubicación:** `sacyr_maquinaria/config/settings.yaml`

**Estructura OBLIGATORIA:**
```yaml
fiscalidad:
  iva: 0.21                    # No hardcodear: leer de config
  
costes:
  diesel_hora: 15.5            # €/hora (dinámico por obra)
  mantenimiento_rata: 0.05     # 5% del precio de compra anual
```

**Regla:** Si `main.py` necesita parámetro de otro módulo:
```python
# ❌ INCORRECTO:
from sacyr_personal.config import settings
tasa_iva = settings['iva']

# ✓ CORRECTO:
# main.py carga ambos settings independientemente:
conf_maq = cargar_config('sacyr_maquinaria/config/settings.yaml')
conf_per = cargar_config('sacyr_personal/config/settings.yaml')
```

---

## 6. RESUMEN: Matriz de Responsabilidades

| Componente | Entrada | Procesamiento | Salida | Dependencias |
|------------|---------|---------------|--------|--------------|
| **Maquinaria** | id_activo, costos | Validación | Entidad | dataclasses |
| **Obra** | id_obra, presupuesto | Validación | Entidad | dataclasses |
| **FlotaRepository** | id_obra | Query filtro | List[Maquinaria] | .models |
| **CostService** | Obra, tasas | Cálculo + consolidación | Dict análisis | FlotaRepository |
| **main.py** | Config ambos módulos | Orquestación | Informe TOTAL | CostService, NominaService |

---

## 7. PUNTOS DE NO RETORNO (Anti-Patterns Prohibidos)

❌ **PROHIBIDO 1:** `from sacyr_personal import *` dentro de CostService  
❌ **PROHIBIDO 2:** Hardcodear `id_obra = "OBRA-VAL-01"` en modelos  
❌ **PROHIBIDO 3:** Modificar estado de Maquinaria en CostService sin transacción  
❌ **PROHIBIDO 4:** Retornar objetos internos; siempre Dict o DTO  
❌ **PROHIBIDO 5:** Diferentes formatos de `id_obra` entre módulos  

✓ **OBLIGATORIO:** El contrato (firmas, retornos) es **inmutable** entre sprints.

---

## 📌 Conclusión

Este módulo **actúa en aislamiento total**, proporcionando un contrato bien definido solo a `main.py`. La dependencia es **unidireccional descendente** (main.py → CostService → Repository → Models). No hay conocimiento horizontal entre módulos.

