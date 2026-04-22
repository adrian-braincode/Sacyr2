# 📐 ARCHITECTURE_PLAN.md – DISEÑO DE MAQUINARIA
## Plan Arquitectónico: Flujo de Costes y Consolidación con DI

---

## 1. ARQUITECTURA DE INYECCIÓN DE DEPENDENCIAS (DI)

### 1.1 Patrón Estratégico: Inversión de Control

**Objetivo:** Desacoplar `CostService` de la implementación concreta de datos, permitiendo que cambiar de repositorio (CSV → DB → API) no afecte la lógica.

#### Flujo de Inyección:

```
┌────────────────────────────────────────────────────┐
│  main.py (Orquestador Central)                      │
│  ├─ instancia: repo_maq = FlotaRepository()         │
│  ├─ instancia: serv_maq = CostService(repo_maq) ← DI
│  └─ invoca: serv_maq.analizar_rentabilidad_obra()  │
└────────────────────┬───────────────────────────────┘
                     │ [Inyecta repo]
                     ▼
        ┌─────────────────────────────────┐
        │  CostService                     │
        │  ├─ __init__(flota_repo)         │
        │  │   self.repo = flota_repo     │
        │  ├─ analizar_rentabilidad_obra() │
        │  │   maquinas = self.repo       │
        │  │             .obtener_...()   │
        │  └─ [No sabe SI es CSV/DB]      │
        └─────────────┬───────────────────┘
                      │ [Delega consulta]
                      ▼
        ┌─────────────────────────────────┐
        │  FlotaRepository                 │
        │  ├─ obtener_maquinaria_por_obra()│
        │  ├─ [Implementación concreta:   │
        │  │   CSV, DB, hardcoded, etc.]  │
        │  └─ Retorna: List[Maquinaria]   │
        └─────────────────────────────────┘
```

### 1.2 Ventajas de DI en Sacyr

| Ventaja | Aplicación Real |
|---------|-----------------|
| **Testeable** | Mock FlotaRepository con datos falsificados sin tocar BD |
| **Mantenible** | Cambiar CSV → PostgreSQL solo en FlotaRepository |
| **Flexible** | Múltiples orígenes de datos simultáneamente |
| **Escalable** | Agregar caché sin tocar CostService |

---

## 2. PATRÓN DE FLUJO DE COSTES: Desglose Granular

### 2.1 Pipeline de Cálculo (Paso a Paso)

```python
FASE 1: Cargar Contexto
────────────────────────────
Entrada: id_obra = "OBRA-VAL-01"
         factor_logistico = 1.10 (peaje, clima, distancia)
         
Operación: repo.obtener_maquinaria_por_obra("OBRA-VAL-01")
Salida: [Maquinaria1, Maquinaria2, ...]

FASE 2: Iterar Máquinas (Por cada máquina)
────────────────────────────────────────
Entrada: Maquinaria(horas=500, costo_manto=50, id_obra="OBRA-VAL-01")
         factor_logistico=1.10
         
Cálculos:
  coste_mantenimiento = horas_oper × costo_manto_hora × factor_logistico
                      = 500 × 50 × 1.10
                      = 27,500 €
  
  coste_combustible = horas_oper × tasa_diesel_hora
                    = 500 × 15.5
                    = 7,750 €
  
  neto_activo = coste_manto + coste_combustible
              = 27,500 + 7,750
              = 35,250 €
  
Acumulador: gasto_total_neto += 35,250 €

FASE 3: Consolidación (Después de iterar todas)
──────────────────────────────────────────────
Entrada: gasto_total_neto (suma de todas máquinas)
         tasa_iva = 0.21
         
Cálculo:
  total_con_iva = gasto_total_neto × (1 + tasa_iva)
                = X × 1.21
  
  margen_restante = presupuesto_maximo - total_con_iva
  
Evaluación de Estado:
  si margen_restante > presupuesto × 0.10  → "SALUDABLE"
  si margen_restante ∈ [0, presupuesto × 0.10] → "ALERTA"
  si margen_restante < 0 → "DÉFICIT CRÍTICO"

FASE 4: Retorno
──────────────
Salida: {
  "obra_nombre": "Puerto Valencia",
  "presupuesto": 2,000,000,
  "gasto_total_iva": 1,352,000,
  "margen": 648,000,
  "estado": "SALUDABLE",
  "activos_vinculados": [...]
}
```

### 2.2 Algoritmo Pseudocódigo

```
Función: analizar_rentabilidad_obra(obra, tasa_iva, tasa_diesel)
  maquinas ← repo.obtener_maquinaria_por_obra(obra.id_obra)
  
  gasto_total_neto ← 0
  detalles ← []
  
  Para cada máquina en maquinas:
    coste_manto ← máquina.horas_op × máquina.costo_manto_hora × obra.factor_logistico
    coste_diesel ← máquina.horas_op × tasa_diesel
    neto ← coste_manto + coste_diesel
    
    gasto_total_neto ← gasto_total_neto + neto
    detalles.append({id, nombre, neto})
  
  total_iva ← gasto_total_neto × (1 + tasa_iva)
  margen ← obra.presupuesto - total_iva
  
  estado ← evaluar_estado(margen, obra.presupuesto)
  
  Retorna {
    obra_nombre, presupuesto, gasto_total_iva,
    margen, estado, activos_vinculados: detalles
  }
```

---

## 3. INTEGRACIÓN CON SACYR_PERSONAL: Consolidación de Costes

### 3.1 Flujo en main.py (Orquestación)

```
main.py
├─ Paso 1: Instancia CostService (DI)
│          repo_maq = FlotaRepository()
│          serv_maq = CostService(repo_maq)
│
├─ Paso 2: Instancia NominaService (DI)
│          repo_per = PersonalRepository()
│          serv_per = NominaService(repo_per)
│
├─ Paso 3: Obtiene Contexto (Obra)
│          proyecto = Obra("OBRA-VAL-01", ...)
│
├─ Paso 4: Ejecuta Análisis Maquinaria
│          analisis_maq = serv_maq.analizar_rentabilidad_obra(
│                           proyecto, iva=0.21, diesel=15.5)
│          → Retorna: Dict con gasto_total_iva
│
├─ Paso 5: Ejecuta Cálculo Personal
│          analisis_per = serv_per.calcular_coste_personal_obra(
│                           proyecto.id_obra, horas_mes=160)
│          → Retorna: Dict con total_personal_neto
│
└─ Paso 6: CONSOLIDACIÓN (La Magia)
           coste_total_obra = (
             analisis_maq['gasto_total_iva'] +
             analisis_per['total_personal_neto']
           )
           
           margen_final = proyecto.presupuesto_maximo - coste_total_obra
           
           Imprime:
           - Gasto Maquinaria: X €
           - Gasto Personal: Y €
           - COSTE TOTAL: X + Y €
           - MARGEN PROYECTO: Z €
```

### 3.2 Garantía de Sincronización: Identificadores Únicos

**Problema:** Los operarios están asignados a máquinas (campo `id_maquina_asignada`), pero los módulos operan independientemente. ¿Qué si un `id_activo` de maquinaria no existe en FlotaRepository?

**Solución (Nivel Arquitectónico):**

```
INVARIANTE: Los IDs de máquina en sacyr_personal.Operario
            DEBEN coincidir con IDs en sacyr_maquinaria.Maquinaria

Garantizado en:
  1. Validación en carga de datos (ver Tasks.md)
  2. Script de sincronización mensual
  3. Alertas si hay desajuste
```

---

## 4. PATRONES SOLID APLICADOS

### 4.1 Single Responsibility Principle (SRP)

| Clase | Responsabilidad Única |
|-------|----------------------|
| **Maquinaria** | Representar máquina + validar atributos |
| **Obra** | Representar contexto obra + validar presupuesto |
| **FlotaRepository** | Obtener datos de maquinaria (agnóstico a fuente) |
| **CostService** | Calcular costes y rentabilidad |
| **main.py** | Orquestar módulos y consolidar |

### 4.2 Dependency Inversion Principle (DIP)

❌ **INCORRECTO (Acoplamiento Alto):**
```python
class CostService:
    def __init__(self):
        self.repo = FlotaRepository()  # Acoplado a implementación
```

✓ **CORRECTO (DI Pattern):**
```python
class CostService:
    def __init__(self, flota_repo):  # Recibe abstracción
        self.repo = flota_repo
# En main.py:
repo = FlotaRepository()  # Instancia concreta
serv = CostService(repo)  # Inyecta
```

### 4.3 Open/Closed Principle (OCP)

**Abierto a extensión, cerrado a modificación:**

- ✓ Cambiar FlotaRepository → PostgreSQLRepository: ¿Afecta CostService? **NO**
- ✓ Agregar nuevo método en CostService: ¿Afecta FlotaRepository? **NO**
- ✓ Cambiar formato de Obra: ¿Afecta cálculo de costes? **SÍ** → Eso es correcto (depende de contratos)

---

## 5. FLUJO DE DATOS: Diagramas de Secuencia

### 5.1 Secuencia: Análisis Completo Obra

```
Secuencia Temporal
──────────────────

T=0ms   main.py
        ├─ Lee settings.yaml
        ├─ Instancia servicios
        └─ Invoca: serv_maq.analizar_rentabilidad_obra()

T=10ms  CostService.analizar_rentabilidad_obra()
        └─ Invoca: repo.obtener_maquinaria_por_obra("OBRA-VAL-01")

T=15ms  FlotaRepository.obtener_maquinaria_por_obra()
        └─ Consulta fuente (BD/CSV/API)
        ┌─ Retorna: [Maquinaria_1, Maquinaria_2, ...]
        
T=20ms  CostService (iteración, cálculos)
        └─ Suma costes, calcula IVA, evalúa margen

T=30ms  CostService
        └─ Retorna Dict análisis a main.py

T=35ms  NominaService (Paralelo conceptual)
        └─ Calcula costes personal

T=40ms  main.py (Consolidación)
        ├─ Suma: analisis_maq + analisis_per
        ├─ Evalúa margen final
        └─ Imprime informe
```

### 5.2 Matriz de Responsabilidades en Consolidación

| Paso | Quién | Qué | Resultado |
|------|-------|-----|-----------|
| 1 | CostService | Calcula gasto_maq_iva | Float |
| 2 | NominaService | Calcula gasto_personal | Float |
| 3 | main.py | Suma ambos gastos | coste_total |
| 4 | main.py | Resta presupuesto | margen_final |
| 5 | main.py | Evalúa viabilidad | Print alerta |

---

## 6. GESTIÓN DE ERRORES Y RESILIENCIA

### 6.1 Estrategia de Captura

```python
# En main.py (punto de consolidación)
try:
    analisis_maq = serv_maq.analizar_rentabilidad_obra(...)
    analisis_per = serv_per.calcular_coste_personal_obra(...)
except ValueError as e:
    log_critical(f"Error en datos maestros: {e}")
    raise
except Exception as e:
    log_error(f"Error inesperado: {e}")
    raise RuntimeError("Fallo en consolidación") from e
```

### 6.2 Timeouts y Circuit Breaker (Futura)

```python
# Para cuando se integre con BD remota:
@retry(max_attempts=3, backoff=1.5)
def obtener_maquinaria_por_obra(self, id_obra):
    # Intenta 3 veces con espera exponencial
    return self._fetch_from_db(id_obra)
```

---

## 7. CONSOLIDACIÓN: El Informe Único Sacyr

### 7.1 Estructura del Informe Final

```
╔════════════════════════════════════════════════════════╗
║      INFORME CONSOLIDADO SACYR: Puerto Valencia       ║
╠════════════════════════════════════════════════════════╣
║ Módulo            │ Concepto            │ Importe      ║
╠═══════════════════╪═════════════════════╪══════════════╣
║ MAQUINARIA        │ Costes (IVA incl.)  │ 1,352,000 €  ║
║ PERSONAL          │ Nóminas (neto)      │   480,000 €  ║
╠═══════════════════╪═════════════════════╪══════════════╣
║ TOTAL PROYECTO    │ Coste Total         │ 1,832,000 €  ║
║                   │ Presupuesto Máximo  │ 2,000,000 €  ║
║                   │ Margen Disponible   │   168,000 €  ║
╠═══════════════════╪═════════════════════╪══════════════╣
║ ESTADO GENERAL    │                     │ ✓ SALUDABLE  ║
╚════════════════════════════════════════════════════════╝
```

### 7.2 Lógica de Estado

```python
Pseudocódigo: Evaluación Estado Final

def evaluar_estado_proyecto(presupuesto, coste_total):
    margen_pct = (presupuesto - coste_total) / presupuesto
    
    if margen_pct >= 0.15:
        return "✓ SALUDABLE"
    elif margen_pct >= 0.0:
        return "⚠️ ALERTA"
    else:
        return "🔴 DÉFICIT CRÍTICO"
```

---

## 8. RESUMEN: ADR (Architecture Decision Records)

### ADR-001: Inyección de Dependencias en CostService
**Decisión:** Pasar FlotaRepository como parámetro, no instanciarlo dentro.  
**Justificación:** Permite testear sin BD real, cambiar fuente datos sin refactorizar.  
**Impacto:** +2 líneas en main.py, -10 líneas en tests.

### ADR-002: Modelo Compartido "Obra"
**Decisión:** La entidad Obra vive en sacyr_maquinaria, ambos módulos la importan.  
**Justificación:** Único punto de verdad para contexto obra, evita duplicación.  
**Impacto:** Ambos módulos dependen de sacyr_maquinaria.src.models.obra.

### ADR-003: Consolidación en main.py
**Decisión:** No crear servicio "ConsolidacionService", hacerlo en orquestador.  
**Justificación:** Lógica trivial (suma), evita acoplamiento transversal.  
**Impacto:** main.py es el único lugar donde se suman gastos.

---

## 📌 Conclusión

El diseño garantiza que:
✓ CostService es testeable sin BD  
✓ FlotaRepository puede cambiar de CSV a PostgreSQL transparentemente  
✓ Costes de maquinaria y personal se consolidan en un único informe  
✓ Los dos módulos nunca se importan directamente  
✓ main.py es el ÚNICO orquestador

