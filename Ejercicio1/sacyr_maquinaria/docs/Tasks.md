# 📋 TASK_ROADMAP.md – MÓDULO MAQUINARIA
## Roadmap Determinístico: Interoperabilidad de IDs y Consolidación

---

## 📅 CRONOGRAMA GENERAL

```
SEMANA 1: VALIDACIÓN DE IDENTIDADES
├─ T1.1 - Auditoría de IDs en base de datos (1 día)
├─ T1.2 - Crear script de sincronización (1 día)
├─ T1.3 - Validar coherencia id_activo = id_maquina_asignada (1 día)
└─ T1.4 - Tests unitarios de modelos (1 día)

SEMANA 2: CONSOLIDACIÓN DE COSTES
├─ T2.1 - Tests CostService con datos mock (2 días)
├─ T2.2 - Integración DI: FlotaRepository → CostService (1 día)
├─ T2.3 - Tests flujo completo: Obra → Maquinaria → Costes (2 días)
└─ T2.4 - Validación contrato de salida Dict (1 día)

SEMANA 3: INTERCONEXIÓN CON PERSONAL
├─ T3.1 - Asegurar que Operario.id_maquina_asignada existe en Flota (2 días)
├─ T3.2 - Tests de ID matching entre módulos (2 días)
├─ T3.3 - Crear fixture de datos coherentes (1 día)
└─ T3.4 - Documentación de ID governance (1 día)

SEMANA 4: VALIDACIÓN Y DEPLOYMENT
├─ T4.1 - Tests E2E: main.py consolida correctamente (2 días)
├─ T4.2 - Validación cobertura >= 90% en CostService (1 día)
├─ T4.3 - Review de anti-patterns (imports cruzados) (1 día)
└─ T4.4 - Setup CI/CD, documentación final (1 día)
```

**Ruta Crítica:** T1.3 → T2.4 → T3.2 → T4.1  
**Duración Total:** 20 días de trabajo (4 semanas a 5 días/semana)

---

# 🎯 NIVEL 1: VALIDACIÓN DE IDENTIDADES (SEMANA 1)

## T1.1: Auditoría de IDs en Base de Datos

**Duración:** 1 día | **Prioridad:** CRÍTICO | **Bloqueador:** Sí

### Descripción
Inspeccionar la integridad de los identificadores en la fuente de datos para asegurar que no hay:
- IDs duplicados en `Maquinaria.id_activo`
- IDs huérfanos (máquinas sin obra asignada)
- Valores nulos o malformados

### Artefactos a Crear

```
audit_ids_maquinaria.sql (si usa BD)
o
audit_ids_maquinaria.py (si usa CSV/memoria)
```

### Queries de Auditoría

```sql
-- Query 1: IDs duplicados
SELECT id_activo, COUNT(*) as cant
FROM maquinaria
GROUP BY id_activo
HAVING COUNT(*) > 1;
-- RESULTADO ESPERADO: Empty (0 rows)

-- Query 2: Obras no existen
SELECT m.id_activo, m.id_obra
FROM maquinaria m
LEFT JOIN obra o ON m.id_obra = o.id_obra
WHERE o.id_obra IS NULL;
-- RESULTADO ESPERADO: Empty (0 rows)

-- Query 3: IDs nulos
SELECT id_activo, id_obra
FROM maquinaria
WHERE id_activo IS NULL OR id_obra IS NULL;
-- RESULTADO ESPERADO: Empty (0 rows)
```

### Definition of Done ✓

- [ ] 0 IDs duplicados en maquinaria.id_activo
- [ ] 0 máquinas sin obra_fk válido
- [ ] 0 valores nulos en id_activo o id_obra
- [ ] Reporte generado: `audit_report_ids_T1.1.txt`
- [ ] Si hay errores, lista de correcciones manual
- [ ] Documento `ID_GOVERNANCE.md` iniciado

---

## T1.2: Crear Script de Sincronización

**Duración:** 1 día | **Prioridad:** ALTO | **Depende de:** T1.1

### Descripción
Generar un script que valide y corrija automáticamente inconsistencias menores entre los IDs de maquinaria y los IDs asignados en personal.

### Script Pseudocódigo

```python
# sync_ids_maquinaria_personal.py

def sincronizar_ids():
    """
    1. Carga lista de id_activo válidos de FlotaRepository
    2. Carga lista de id_maquina_asignada de PersonalRepository
    3. Identifica:
       - IDs en personal que NO existen en maquinaria → ERROR
       - IDs en maquinaria no asignados a nadie → INFO
       - IDs inconsistentes → WARN
    4. Genera reporte de sincronización
    """
    
    flota = FlotaRepository().obtener_todas()
    personal = PersonalRepository().obtener_todos()
    
    ids_maquinaria = {m.id_activo for m in flota}
    ids_personal_asignadas = {p.id_maquina_asignada for p in personal if p.id_maquina_asignada}
    
    # IDs huérfanos en personal
    huerfanos = ids_personal_asignadas - ids_maquinaria
    if huerfanos:
        log_error(f"⚠️ Operarios asignados a máquinas inexistentes: {huerfanos}")
    
    # IDs sin operarios (máquinas disponibles)
    disponibles = ids_maquinaria - ids_personal_asignadas
    log_info(f"ℹ️ Máquinas disponibles (sin operarios): {len(disponibles)}")
    
    return {
        "sincronizado": len(huerfanos) == 0,
        "errores": huerfanos,
        "disponibles": disponibles
    }
```

### Artefactos a Crear

```
sacyr_maquinaria/utils/sync_ids_maquinaria_personal.py
└─ Función: sincronizar_ids() → Dict
   └─ Retorna: {"sincronizado": bool, "errores": set, "disponibles": set}
```

### Definition of Done ✓

- [ ] Script ejecutable sin errores
- [ ] Importa FlotaRepository y PersonalRepository correctamente
- [ ] Log de ERROR si encuentra IDs huérfanos
- [ ] Reporte guardado en `sync_report_T1.2.log`
- [ ] Tests: Sin error cuando datos son coherentes
- [ ] Tests: Detecta un ID huérfano si lo hay

---

## T1.3: Validar Coherencia id_activo = id_maquina_asignada

**Duración:** 1 día | **Prioridad:** CRÍTICO | **Depende de:** T1.1, T1.2

### Descripción
Crear validador que asegure que cada operario asignado a una máquina existe realmente en el catálogo de maquinaria.

### Test Cases

```python
def test_operario_con_maquina_valida():
    """
    Operario está asignado a máquina que SÍ existe en FlotaRepository
    RESULTADO: ✓ Válido
    """
    repo_maq = MockFlotaRepository([
        Maquinaria("MAQ-001", ..., id_obra="OBRA-01")
    ])
    repo_per = MockPersonalRepository([
        Operario("OP-001", ..., id_maquina_asignada="MAQ-001")
    ])
    
    validator = IdCoherenceValidator(repo_maq, repo_per)
    assert validator.validar() == True

def test_operario_con_maquina_invalida():
    """
    Operario está asignado a máquina que NO existe
    RESULTADO: ✗ Inválido, excepción
    """
    repo_maq = MockFlotaRepository([])  # Sin máquinas
    repo_per = MockPersonalRepository([
        Operario("OP-001", ..., id_maquina_asignada="MAQ-INEXISTENTE")
    ])
    
    validator = IdCoherenceValidator(repo_maq, repo_per)
    with pytest.raises(InvalidOperarioException):
        validator.validar()

def test_operario_sin_maquina_asignada():
    """
    Operario sin máquina asignada (None o "")
    RESULTADO: ✓ Válido (puede estar en laboratorio, disponible, etc.)
    """
    repo_per = MockPersonalRepository([
        Operario("OP-002", ..., id_maquina_asignada=None)
    ])
    
    validator = IdCoherenceValidator(repo_maq, repo_per)
    assert validator.validar_individual("OP-002") == True
```

### Clase Validadora

```python
class IdCoherenceValidator:
    """
    Valida que todos los id_maquina_asignada de Operarios
    correspondan a máquinas reales en FlotaRepository
    """
    
    def __init__(self, flota_repo, personal_repo):
        self.flota_repo = flota_repo
        self.personal_repo = personal_repo
    
    def validar(self) -> bool:
        """Retorna True si TODOS los operarios son coherentes"""
        pass
    
    def get_errores(self) -> List[str]:
        """Retorna lista de problemas encontrados"""
        pass
```

### Artefactos a Crear

```
sacyr_maquinaria/src/validators/id_coherence_validator.py
sacyr_maquinaria/tests/test_id_coherence_validator.py
```

### Definition of Done ✓

- [ ] Clase IdCoherenceValidator implementada
- [ ] Todos los tests pasan (4+ casos)
- [ ] Cobertura >= 95%
- [ ] Lanza InvalidOperarioException si hay mismatch
- [ ] Retorna lista detallada de errores
- [ ] Documentación inline con ejemplos

---

## T1.4: Tests Unitarios de Modelos

**Duración:** 1 día | **Prioridad:** ALTO | **Depende de:** T1.1

### Descripción
Validar que los modelos Maquinaria y Obra validan correctamente sus invariantes.

### Test Cases

```python
def test_maquinaria_precio_negativo():
    """Rechaza precio_compra < 0"""
    with pytest.raises(ValueError):
        Maquinaria(
            id_activo="MAQ-001",
            precio_compra=-1000,  # ❌ Inválido
            ...
        )

def test_maquinaria_horas_negativas_reset():
    """Si horas_operativas < 0, las resetea a 0"""
    maq = Maquinaria(
        id_activo="MAQ-001",
        horas_operativas=-50,  # ← Inválido
        ...
    )
    assert maq.horas_operativas == 0

def test_obra_presupuesto_nulo():
    """Rechaza presupuesto <= 0"""
    with pytest.raises(ValueError):
        Obra(
            id_obra="OBRA-01",
            presupuesto_maximo=0,  # ❌ Inválido
            ...
        )

def test_obra_factor_logistico_bajo():
    """Si factor_logistico < 1.0, lo resetea a 1.0"""
    obra = Obra(
        id_obra="OBRA-01",
        factor_logistico=0.5,  # ← Por debajo del mínimo
        ...
    )
    assert obra.factor_logistico == 1.0
```

### Definition of Done ✓

- [ ] 4+ tests para Maquinaria
- [ ] 4+ tests para Obra
- [ ] Cobertura >= 90% en models/
- [ ] Todos los tests pasan
- [ ] Documentado: qué valida cada test
- [ ] Archivo: `tests/test_models_validation.py`

---

# 🎯 NIVEL 2: CONSOLIDACIÓN DE COSTES (SEMANA 2)

## T2.1: Tests CostService con Datos Mock

**Duración:** 2 días | **Prioridad:** CRÍTICO | **Depende de:** T1.4

### Descripción
Crear suite de tests exhaustiva para CostService usando mocks de FlotaRepository, sin tocar BD real.

### Test Cases

```python
class TestCostService:
    
    def setup_method(self):
        """Preparar fixtures reutilizables"""
        self.repo_mock = MockFlotaRepository()
        self.service = CostService(self.repo_mock)
        self.obra_test = Obra("OBRA-01", "Test", "España", 2000000, 1.10)
    
    def test_servicio_recibe_repo_inyectado(self):
        """✓ DI correcta: CostService recibe repo en __init__"""
        assert self.service.repo is self.repo_mock
    
    def test_analizar_rentabilidad_sin_maquinas(self):
        """Obra sin máquinas asignadas retorna costes = 0"""
        self.repo_mock.maquinas = []  # Sin maquinaria
        
        resultado = self.service.analizar_rentabilidad_obra(
            self.obra_test, tasa_iva=0.21, tasa_diesel=15.5
        )
        
        assert resultado['gasto_total_iva'] == 0
        assert resultado['margen'] == self.obra_test.presupuesto_maximo
        assert resultado['estado'] == "SALUDABLE"
    
    def test_analizar_rentabilidad_con_una_maquina(self):
        """Cálculo correcto con una máquina"""
        maq = Maquinaria(
            id_activo="MAQ-001",
            nombre="Excavadora",
            tipo="pesada",
            precio_compra=100000,
            costo_mantenimiento_hora=50,
            horas_operativas=500,
            pais="España",
            id_obra="OBRA-01"
        )
        self.repo_mock.maquinas = [maq]
        
        resultado = self.service.analizar_rentabilidad_obra(
            self.obra_test, tasa_iva=0.21, tasa_diesel=15.5
        )
        
        # Esperado:
        # coste_manto = 500 * 50 * 1.10 = 27,500
        # coste_diesel = 500 * 15.5 = 7,750
        # neto = 35,250
        # con_iva = 35,250 * 1.21 = 42,652.5
        
        assert resultado['gasto_total_iva'] == pytest.approx(42652.5)
        assert resultado['margen'] == pytest.approx(1957347.5)
        assert resultado['estado'] == "SALUDABLE"
    
    def test_analizar_rentabilidad_multiples_maquinas(self):
        """Suma correcta de múltiples máquinas"""
        maq1 = Maquinaria(..., horas_operativas=500, id_obra="OBRA-01")
        maq2 = Maquinaria(..., horas_operativas=300, id_obra="OBRA-01")
        self.repo_mock.maquinas = [maq1, maq2]
        
        resultado = self.service.analizar_rentabilidad_obra(...)
        
        # Debe sumar costes de ambas
        assert len(resultado['activos_vinculados']) == 2
        assert sum(a['neto'] for a in resultado['activos_vinculados']) > 0
    
    def test_estado_alerta_margen_bajo(self):
        """Si margen < 10% presupuesto → estado ALERTA"""
        # Crear máquina con coste muy alto
        maq = Maquinaria(
            ...,
            horas_operativas=10000,  # Muchas horas
            costo_mantenimiento_hora=150,  # Costo alto
            id_obra="OBRA-01"
        )
        self.repo_mock.maquinas = [maq]
        
        resultado = self.service.analizar_rentabilidad_obra(...)
        
        # Margen debería ser bajo
        if resultado['margen'] < self.obra_test.presupuesto_maximo * 0.1:
            assert resultado['estado'] == "ALERTA"
    
    def test_estado_deficit_margen_negativo(self):
        """Si margen < 0 → estado DÉFICIT CRÍTICO"""
        # Máquina extremadamente cara
        maq = Maquinaria(
            ...,
            horas_operativas=50000,
            costo_mantenimiento_hora=200,
            id_obra="OBRA-01"
        )
        self.repo_mock.maquinas = [maq]
        
        resultado = self.service.analizar_rentabilidad_obra(...)
        
        if resultado['margen'] < 0:
            assert resultado['estado'] == "DÉFICIT CRÍTICO"
    
    def test_retorna_dict_con_estructura_correcta(self):
        """✓ Contrato: Retorna Dict con claves exactas"""
        resultado = self.service.analizar_rentabilidad_obra(...)
        
        claves_esperadas = {
            'obra_nombre', 'presupuesto', 'gasto_total_iva',
            'margen', 'estado', 'activos_vinculados'
        }
        assert set(resultado.keys()) == claves_esperadas
```

### Definition of Done ✓

- [ ] 10+ tests en TestCostService
- [ ] Todos los tests pasan
- [ ] Cobertura >= 95% en CostService
- [ ] Usa MockFlotaRepository sin BD real
- [ ] Archivo: `tests/test_cost_service.py`
- [ ] Documentado: qué valida cada test

---

## T2.2: Integración DI: FlotaRepository → CostService

**Duración:** 1 día | **Prioridad:** ALTO | **Depende de:** T2.1

### Descripción
Verificar que la inyección de dependencias funciona correctamente en la integración real (sin mocks).

### Checklist de Integración

```python
def test_di_integracion_real():
    """
    ✓ FlotaRepository (real, sin mock) se inyecta en CostService
    ✓ CostService invoca métodos del repo correctamente
    ✓ No hay acoplamiento directo
    """
    
    repo_real = FlotaRepository()  # Implementación real
    service = CostService(repo_real)  # Inyección
    
    # Usar datos de prueba (fixture)
    obra = Obra("OBRA-TEST", ...)
    resultado = service.analizar_rentabilidad_obra(obra, 0.21, 15.5)
    
    assert resultado is not None
    assert 'gasto_total_iva' in resultado
```

### Definition of Done ✓

- [ ] FlotaRepository se inyecta sin error
- [ ] CostService invoca repo.obtener_maquinaria_por_obra()
- [ ] Retorna Dict válido
- [ ] Archivo: `tests/test_di_integration.py`

---

## T2.3: Tests Flujo Completo: Obra → Maquinaria → Costes

**Duración:** 2 días | **Prioridad:** CRÍTICO | **Depende de:** T2.2

### Descripción
Tests E2E que validen todo el pipeline de cálculo de costes paso a paso.

### Test Cases Pipeline

```python
def test_pipeline_obra_unica_maquina():
    """
    E2E: Obra → Maquinaria → Cálculo Costes → Resultado
    """
    
    # Paso 1: Cargar Obra
    obra = Obra(
        id_obra="OBRA-VAL-01",
        nombre="Puerto Valencia",
        ubicacion="España",
        presupuesto_maximo=2000000,
        factor_logistico=1.10
    )
    
    # Paso 2: Cargar Maquinaria para esa Obra
    repo = MockFlotaRepository()
    repo.maquinas = [
        Maquinaria(
            id_activo="EXCAV-001",
            nombre="Excavadora CAT 320",
            tipo="pesada",
            precio_compra=150000,
            costo_mantenimiento_hora=50,
            horas_operativas=1000,
            pais="España",
            id_obra="OBRA-VAL-01"
        )
    ]
    
    # Paso 3: Calcular Costes
    service = CostService(repo)
    resultado = service.analizar_rentabilidad_obra(
        obra=obra,
        tasa_iva=0.21,
        tasa_diesel=15.5
    )
    
    # Paso 4: Validar Resultado
    assert resultado['obra_nombre'] == "Puerto Valencia"
    assert resultado['presupuesto'] == 2000000
    assert resultado['gasto_total_iva'] > 0
    assert resultado['margen'] > 0
    assert len(resultado['activos_vinculados']) == 1
    
    # Validar cálculo manual:
    # coste_manto = 1000 * 50 * 1.10 = 55,000
    # coste_diesel = 1000 * 15.5 = 15,500
    # neto = 70,500
    # con_iva = 70,500 * 1.21 = 85,305
    assert resultado['gasto_total_iva'] == pytest.approx(85305)

def test_pipeline_multiples_maquinas():
    """E2E con varias máquinas, todas en misma obra"""
    # [Similar estructura, múltiples máquinas]
    pass

def test_pipeline_diferentes_obras():
    """
    ✓ Mismo servicio puede procesar diferentes obras
    ✓ Filtra máquinas por id_obra correctamente
    """
    repo = MockFlotaRepository()
    repo.maquinas = [
        Maquinaria(..., id_obra="OBRA-01"),
        Maquinaria(..., id_obra="OBRA-02"),
    ]
    
    service = CostService(repo)
    
    obra1 = Obra(id_obra="OBRA-01", ...)
    obra2 = Obra(id_obra="OBRA-02", ...)
    
    resultado1 = service.analizar_rentabilidad_obra(obra1, 0.21, 15.5)
    resultado2 = service.analizar_rentabilidad_obra(obra2, 0.21, 15.5)
    
    # Ambos deben retornar resultados válidos pero diferentes
    assert resultado1['obra_nombre'] != resultado2['obra_nombre']
```

### Definition of Done ✓

- [ ] 5+ tests E2E pipeline
- [ ] Todos los tests pasan
- [ ] Cobertura >= 95%
- [ ] Validación de cálculos manuales
- [ ] Archivo: `tests/test_cost_pipeline_e2e.py`

---

## T2.4: Validación Contrato de Salida Dict

**Duración:** 1 día | **Prioridad:** ALTO | **Depende de:** T2.3

### Descripción
Asegurar que el contrato del Dict retornado es inmutable y consistente.

### Test Cases de Contrato

```python
def test_contrato_dict_tiene_todas_claves():
    """✓ Retorna EXACTAMENTE estas 6 claves"""
    resultado = service.analizar_rentabilidad_obra(...)
    
    assert set(resultado.keys()) == {
        'obra_nombre', 'presupuesto', 'gasto_total_iva',
        'margen', 'estado', 'activos_vinculados'
    }

def test_contrato_tipos_datos():
    """✓ Cada clave tiene tipo correcto"""
    resultado = service.analizar_rentabilidad_obra(...)
    
    assert isinstance(resultado['obra_nombre'], str)
    assert isinstance(resultado['presupuesto'], float)
    assert isinstance(resultado['gasto_total_iva'], float)
    assert isinstance(resultado['margen'], float)
    assert isinstance(resultado['estado'], str)
    assert isinstance(resultado['activos_vinculados'], list)

def test_contrato_valores_minimos():
    """✓ Valores en rangos válidos"""
    resultado = service.analizar_rentabilidad_obra(...)
    
    assert resultado['presupuesto'] > 0
    assert resultado['gasto_total_iva'] >= 0
    assert resultado['estado'] in ['SALUDABLE', 'ALERTA', 'DÉFICIT CRÍTICO']

def test_contrato_activos_vinculados_estructura():
    """✓ Cada elemento de activos_vinculados tiene estructura correcta"""
    resultado = service.analizar_rentabilidad_obra(...)
    
    for activo in resultado['activos_vinculados']:
        assert 'id' in activo
        assert 'nombre' in activo
        assert 'neto' in activo
        assert isinstance(activo['neto'], float)
```

### Definition of Done ✓

- [ ] 4+ tests de contrato
- [ ] Todos pasan
- [ ] Documentado: qué es cada clave
- [ ] Archivo: `tests/test_cost_service_contract.py`
- [ ] Especificación guardada en `docs/API_CONTRACT.md`

---

# 🎯 NIVEL 3: INTERCONEXIÓN CON PERSONAL (SEMANA 3)

## T3.1: Asegurar que Operario.id_maquina_asignada Existe en Flota

**Duración:** 2 días | **Prioridad:** CRÍTICO | **Depende de:** T1.3

### Descripción
Crear validador que garantice que todo operario asignado a una máquina tiene una máquina válida en el catálogo.

### Test Case

```python
def test_operario_id_maquina_existe():
    """
    Operario OP-001 asignado a MAQ-001
    MAQ-001 EXISTE en FlotaRepository
    RESULTADO: ✓ Válido
    """
    repo_maq = MockFlotaRepository([
        Maquinaria(id_activo="MAQ-001", id_obra="OBRA-01", ...)
    ])
    repo_per = MockPersonalRepository([
        Operario(..., id_maquina_asignada="MAQ-001")
    ])
    
    validator = OperarioMaquinaValidator(repo_maq, repo_per)
    assert validator.es_valido() == True

def test_operario_id_maquina_no_existe():
    """
    Operario OP-001 asignado a MAQ-INEXISTENTE
    RESULTADO: ✗ Error, excepción
    """
    repo_maq = MockFlotaRepository([])  # Catálogo vacío
    repo_per = MockPersonalRepository([
        Operario(..., id_maquina_asignada="MAQ-INEXISTENTE")
    ])
    
    validator = OperarioMaquinaValidator(repo_maq, repo_per)
    with pytest.raises(InvalidOperarioAssignmentException):
        validator.validar_todos()
```

### Definition of Done ✓

- [ ] OperarioMaquinaValidator implementado
- [ ] Tests: operario válido, inválido, sin asignación
- [ ] Cobertura >= 90%
- [ ] Lanza excepción con mensaje claro si hay error
- [ ] Archivo: `tests/test_operario_maquina_validator.py`

---

## T3.2: Tests de ID Matching Entre Módulos

**Duración:** 2 días | **Prioridad:** CRÍTICO | **Depende de:** T3.1

### Descripción
Crear suite de tests que valide la sincronización de IDs entre sacyr_maquinaria y sacyr_personal.

### Test Cases

```python
def test_id_matching_completo():
    """
    Todos los id_maquina_asignada en personal
    coinc con id_activo en maquinaria
    """
    # Setup: Datos coherentes
    flota_valida = [
        Maquinaria(id_activo="MAQ-001", id_obra="OBRA-01", ...),
        Maquinaria(id_activo="MAQ-002", id_obra="OBRA-01", ...),
    ]
    personal_valido = [
        Operario(..., id_maquina_asignada="MAQ-001"),
        Operario(..., id_maquina_asignada="MAQ-002"),
    ]
    
    matcher = IdMatcher(flota_valida, personal_valido)
    report = matcher.generar_reporte()
    
    assert report['inconsistencias'] == []
    assert report['estado'] == "OK"

def test_id_matching_detecta_huerfanos():
    """
    Algunos id_maquina_asignada NO existen en maquinaria
    RESULTADO: Reporte de error
    """
    flota = [
        Maquinaria(id_activo="MAQ-001", id_obra="OBRA-01", ...)
    ]
    personal = [
        Operario(..., id_maquina_asignada="MAQ-001"),  # OK
        Operario(..., id_maquina_asignada="MAQ-999"),  # ❌ No existe
    ]
    
    matcher = IdMatcher(flota, personal)
    report = matcher.generar_reporte()
    
    assert len(report['inconsistencias']) == 1
    assert "MAQ-999" in str(report['inconsistencias'][0])

def test_id_matching_multiples_obras():
    """
    IDs se sincronizan correctamente incluso con múltiples obras
    """
    flota = [
        Maquinaria(id_activo="MAQ-001", id_obra="OBRA-01", ...),
        Maquinaria(id_activo="MAQ-002", id_obra="OBRA-02", ...),
    ]
    personal = [
        Operario(..., id_obra="OBRA-01", id_maquina_asignada="MAQ-001"),
        Operario(..., id_obra="OBRA-02", id_maquina_asignada="MAQ-002"),
    ]
    
    # Debe validar que:
    # - Operario de OBRA-01 solo usa máquinas de OBRA-01
    # - Operario de OBRA-02 solo usa máquinas de OBRA-02
    
    matcher = IdMatcher(flota, personal)
    report = matcher.generar_reporte()
    
    assert report['estado'] == "OK"
```

### Definition of Done ✓

- [ ] Clase IdMatcher implementada
- [ ] 5+ tests de matching
- [ ] Todos pasan
- [ ] Reporte detallado de inconsistencias
- [ ] Archivo: `tests/test_id_matching.py`

---

## T3.3: Crear Fixture de Datos Coherentes

**Duración:** 1 día | **Prioridad:** ALTO | **Depende de:** T3.2

### Descripción
Generar datos de prueba consistentes que se reutilicen en todos los tests.

### Fixture `conftest.py`

```python
# tests/conftest.py

import pytest
from sacyr_maquinaria.src.models import Maquinaria, Obra
from sacyr_personal.src.models import Operario

@pytest.fixture
def obra_test():
    return Obra(
        id_obra="OBRA-VAL-01",
        nombre="Puerto Valencia",
        ubicacion="España",
        presupuesto_maximo=2000000,
        factor_logistico=1.10
    )

@pytest.fixture
def maquinaria_test():
    return [
        Maquinaria(
            id_activo="EXCAV-001",
            nombre="Excavadora CAT 320",
            tipo="pesada",
            precio_compra=150000,
            costo_mantenimiento_hora=50,
            horas_operativas=500,
            pais="España",
            id_obra="OBRA-VAL-01"
        ),
        Maquinaria(
            id_activo="DUMPER-001",
            nombre="Volquete CAT 725",
            tipo="transporte",
            precio_compra=80000,
            costo_mantenimiento_hora=30,
            horas_operativas=1000,
            pais="España",
            id_obra="OBRA-VAL-01"
        ),
    ]

@pytest.fixture
def operarios_test():
    return [
        Operario(
            id_empleado="OP-001",
            nombre="Juan López",
            especialidad="Operador Excavadora",
            costo_hora=25,
            id_obra="OBRA-VAL-01",
            id_maquina_asignada="EXCAV-001"  # ✓ Existe en maquinaria_test
        ),
        Operario(
            id_empleado="OP-002",
            nombre="María García",
            especialidad="Operador Volquete",
            costo_hora=22,
            id_obra="OBRA-VAL-01",
            id_maquina_asignada="DUMPER-001"  # ✓ Existe en maquinaria_test
        ),
    ]

@pytest.fixture
def flota_repository_mock(maquinaria_test):
    repo = MockFlotaRepository()
    repo.maquinas = maquinaria_test
    return repo

@pytest.fixture
def personal_repository_mock(operarios_test):
    repo = MockPersonalRepository()
    repo.operarios = operarios_test
    return repo
```

### Definition of Done ✓

- [ ] `conftest.py` con fixtures reutilizables
- [ ] Datos coherentes entre maquinaria y operarios
- [ ] Todos los tests pueden usar las fixtures
- [ ] Documentado: qué es cada fixture
- [ ] Archivo: `tests/conftest.py`

---

## T3.4: Documentación de ID Governance

**Duración:** 1 día | **Prioridad:** MEDIO | **Depende de:** T3.3

### Descripción
Documento que define reglas de nomenclatura, unicidad y sincronización de IDs.

### Contenido de ID_GOVERNANCE.md

```markdown
# ID Governance: Sacyr Maquinaria + Personal

## 1. Nomenclatura de IDs

### Maquinaria
- **id_activo:** Prefijo + número (ej: EXCAV-001, DUMPER-001)
- **Patrón:** [TIPO]-[NÚMERO]
- **Longitud:** 1-20 caracteres, alfanumérico sin espacios

### Operario
- **id_empleado:** Prefijo + número (ej: OP-001, OP-002)
- **Patrón:** [PREFIJO]-[NÚMERO]
- **Longitud:** 1-20 caracteres, alfanumérico sin espacios

### Obra
- **id_obra:** Prefijo + ubicación + número (ej: OBRA-VAL-01)
- **Patrón:** OBRA-[CÓDIGO]-[NÚMERO]
- **Longitud:** 1-20 caracteres, alfanumérico sin espacios

## 2. Reglas de Unicidad

| ID | Ámbito | Validación |
|----|--------|-----------|
| id_activo | Global | Único en toda la flota |
| id_empleado | Global | Único en toda la empresa |
| id_obra | Global | Único en toda la empresa |
| id_maquina_asignada | Por Obra | Debe existir en Maquinaria.id_activo |

## 3. Sincronización

**Frecuencia:** Diaria al inicio del día
**Responsable:** Script `sync_ids_maquinaria_personal.py`
**Acción:** Detecta desajustes y genera alerta

## 4. Auditoría

**Trimestral:** Revisar para evitar IDs duplicados
**Antes de producción:** Ejecutar validadores
```

### Definition of Done ✓

- [ ] Documento `ID_GOVERNANCE.md` creado
- [ ] Reglas de nomenclatura claras
- [ ] Tabla de unicidad por ámbito
- [ ] Procedimiento de sincronización
- [ ] Archivo: `docs/ID_GOVERNANCE.md`

---

# 🎯 NIVEL 4: VALIDACIÓN Y DEPLOYMENT (SEMANA 4)

## T4.1: Tests E2E: main.py Consolida Correctamente

**Duración:** 2 días | **Prioridad:** CRÍTICO | **Depende de:** T3.3

### Descripción
Test de extremo a extremo que valida todo el flujo desde main.py hasta informe consolidado.

### Test E2E Completo

```python
def test_e2e_main_consolida_costes():
    """
    ✓ main.py carga ambos módulos
    ✓ Calcula costes maquinaria
    ✓ Calcula costes personal
    ✓ Suma ambos correctamente
    ✓ Genera informe final
    """
    
    # Setup: Preparar datos coherentes
    obra = Obra("OBRA-VAL-01", "Puerto Valencia", "España", 2000000, 1.10)
    
    repo_maq = MockFlotaRepository([
        Maquinaria("EXCAV-001", ..., horas_operativas=500, id_obra="OBRA-VAL-01"),
    ])
    repo_per = MockPersonalRepository([
        Operario("OP-001", ..., id_maquina_asignada="EXCAV-001", id_obra="OBRA-VAL-01"),
    ])
    
    # Paso 1: Crear servicios (como en main.py)
    serv_maq = CostService(repo_maq)
    serv_per = NominaService(repo_per)
    
    # Paso 2: Obtener análisis maquinaria
    analisis_maq = serv_maq.analizar_rentabilidad_obra(
        obra, tasa_iva=0.21, tasa_diesel=15.5
    )
    # Resultado: {"gasto_total_iva": X, ...}
    
    # Paso 3: Obtener análisis personal
    analisis_per = serv_per.calcular_coste_personal_obra(
        obra.id_obra, horas_mes=160
    )
    # Resultado: {"total_personal_neto": Y, ...}
    
    # Paso 4: Consolidar (como en main.py)
    coste_total = analisis_maq['gasto_total_iva'] + analisis_per['total_personal_neto']
    margen_final = obra.presupuesto_maximo - coste_total
    
    # Validaciones
    assert coste_total > 0
    assert margen_final > 0
    assert margen_final < obra.presupuesto_maximo
    
    # Paso 5: Estado final
    if margen_final > 0:
        estado = "✓ SALUDABLE" if margen_final > obra.presupuesto_maximo * 0.1 else "⚠️ ALERTA"
    else:
        estado = "🔴 DÉFICIT CRÍTICO"
    
    assert estado in ["✓ SALUDABLE", "⚠️ ALERTA", "🔴 DÉFICIT CRÍTICO"]
```

### Definition of Done ✓

- [ ] Test E2E completo
- [ ] Todos los pasos ejecutan correctamente
- [ ] Consolida maquinaria + personal
- [ ] Calcula margen final
- [ ] Cobertura >= 95%
- [ ] Archivo: `tests/test_e2e_main_consolidation.py`

---

## T4.2: Validación Cobertura >= 90% en CostService

**Duración:** 1 día | **Prioridad:** ALTO | **Depende de:** T2.3, T4.1

### Descripción
Ejecutar coverage y asegurar que todos los branches están testados.

### Comando Coverage

```bash
pytest --cov=sacyr_maquinaria.src.services.cost_service --cov-report=html tests/
```

### Líneas Críticas a Cubrir

```python
# Línea 1: Inicio método
def analizar_rentabilidad_obra(self, obra, tasa_iva, tasa_diesel):
    maquinas = self.repo.obtener_maquinaria_por_obra(obra.id_obra)  # ← CUBRIR
    
    # Línea 2: Loop maquinas
    for m in maquinas:  # ← CUBRIR (con 0 y >0 máquinas)
        coste_mantenimiento = m.horas_operativas * m.costo_mantenimiento_hora * obra.factor_logistico
        # ← CUBRIR
        
    # Línea 3: Cálculo IVA
    total_con_iva = gasto_total_neto * (1 + tasa_iva)  # ← CUBRIR
    
    # Línea 4: Evaluación estado
    estado = "SALUDABLE" if margen_restante > (obra.presupuesto_maximo * 0.1) else "ALERTA"
    # ← CUBRIR ambas ramas
    
    if margen_restante < 0:
        estado = "DÉFICIT CRÍTICO"  # ← CUBRIR
```

### Definition of Done ✓

- [ ] Coverage report generado
- [ ] CostService: >= 95%
- [ ] Todas las líneas cubiertas
- [ ] Todos los branches cubiertos
- [ ] Informe guardado: `coverage_report_T4.2.html`

---

## T4.3: Review de Anti-Patterns (Imports Cruzados)

**Duración:** 1 día | **Prioridad:** CRÍTICO | **Depende de:** T4.2

### Descripción
Auditoría de código para asegurar que NO hay imports cruzados entre módulos.

### Script de Auditoría

```python
# audit_imports.py

import ast
import os

PROHIBIDO = [
    ("sacyr_maquinaria", "sacyr_personal"),
    ("sacyr_personal", "sacyr_maquinaria"),
]

def auditar_imports(carpeta_src):
    """Encuentra imports cruzados prohibidos"""
    for root, dirs, files in os.walk(carpeta_src):
        for archivo in files:
            if archivo.endswith('.py'):
                ruta = os.path.join(root, archivo)
                with open(ruta, 'r') as f:
                    tree = ast.parse(f.read())
                
                for node in ast.walk(tree):
                    if isinstance(node, ast.ImportFrom):
                        if node.module:
                            for prohibida in PROHIBIDO:
                                if prohibida[0] in ruta and prohibida[1] in node.module:
                                    print(f"❌ ERROR: {ruta} importa {node.module}")
                                    return False
    return True

# Ejecutar
print("Auditando imports en sacyr_maquinaria...")
if auditar_imports("sacyr_maquinaria/src"):
    print("✓ OK: Sin imports cruzados")
else:
    print("✗ ERROR: Imports cruzados detectados")
```

### Checklist de Anti-Patterns

```python
# ❌ PROHIBIDO: En cualquier archivo de sacyr_maquinaria
from sacyr_personal.src.repositories import PersonalRepository
from sacyr_personal.src.services import NominaService

# ❌ PROHIBIDO: En CostService
from sacyr_personal import *

# ✓ PERMITIDO: Solo en main.py
from sacyr_maquinaria.src.services import CostService
from sacyr_personal.src.services import NominaService
```

### Definition of Done ✓

- [ ] Script audit_imports.py ejecuta sin errores
- [ ] 0 imports cruzados encontrados
- [ ] Reporte limpio: `audit_imports_report_T4.3.txt`
- [ ] main.py es único punto de cruce

---

## T4.4: Setup CI/CD y Documentación Final

**Duración:** 1 día | **Prioridad:** MEDIO | **Depende de:** T4.3

### Descripción
Configurar pipeline CI/CD y generar documentación final de arquitectura.

### Archivos de Configuración

```yaml
# .github/workflows/test_maquinaria.yml
name: Test Maquinaria

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Set up Python
        uses: actions/setup-python@v2
        with:
          python-version: 3.9
      - name: Install dependencies
        run: |
          pip install -r requirements.txt
          pip install pytest pytest-cov
      - name: Run tests
        run: |
          pytest tests/ --cov=sacyr_maquinaria \
            --cov-report=xml --cov-report=term
      - name: Check coverage
        run: |
          pytest tests/ --cov=sacyr_maquinaria --cov-fail-under=90
      - name: Audit imports
        run: |
          python audit_imports.py
```

### Documentación Final

**Archivo:** `docs/ARQUITECTURA_FINAL.md`

Incluir:
- Resumen ejecutivo
- Diagramas de arquitectura
- Matriz de responsabilidades
- ADRs (Architecture Decision Records)
- Guía de mantenimiento
- Links a todos los documentos generados

### Definition of Done ✓

- [ ] Pipeline CI/CD configurado
- [ ] Tests ejecutan automáticamente en push
- [ ] Coverage >= 90% enforced
- [ ] Documentación final completada
- [ ] README actualizado con instrucciones

---

## 📌 RUTA CRÍTICA

```
T1.3 (1d)
    ↓ bloquea
T2.4 (1d)
    ↓ bloquea
T3.2 (2d)
    ↓ bloquea
T4.1 (2d)
    ↓ bloquea
T4.3 (1d)

Duración Total Crítica: 7 días
Duración Total Proyecto: 20 días (incluye tasks paralelas)
```

---

## 🎯 DEFINICIÓN DE ÉXITO

✓ **Interoperabilidad Validada:**
- Todos los operarios asignados a máquinas existentes
- IDs sincronizados entre módulos

✓ **Consolidación Funcional:**
- Costes maquinaria + personal se suman correctamente
- Informe final imprime margen proyecto

✓ **Arquitectura Limpia:**
- 0 imports cruzados entre módulos
- DI implementada correctamente

✓ **Calidad Garantizada:**
- Cobertura >= 90%
- Todos los tests pasan
- CI/CD configurado

