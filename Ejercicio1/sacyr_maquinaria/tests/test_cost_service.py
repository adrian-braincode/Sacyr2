import pytest
from unittest.mock import MagicMock
from src.models.obra import Obra
from src.models.maquinaria import Maquinaria
from src.services.cost_service import CostService

def test_calculo_rentabilidad_obra_correcta():
    # 1. Setup: Creamos mocks y datos de prueba
    mock_repo = MagicMock()
    # Simulamos que el repo devuelve una sola máquina
    mock_repo.obtener_maquinaria_por_obra.return_value = [
        Maquinaria("M1", "Test", "P", 100, 10, 10, "ES", "OBRA1")
    ]
    
    servicio = CostService(mock_repo)
    obra_test = Obra("OBRA1", "Test", "Loc", 500.0, 1.0) # Presupuesto 500

    # 2. Execution
    # Coste: (10h * 10€) + (10h * 1€ diesel) = 110€ + 21% IVA = 133.1€
    resultado = servicio.analizar_rentabilidad_obra(obra_test, 0.21, 1.0)

    # 3. Validation
    assert resultado["estado"] == "SALUDABLE"
    assert resultado["gasto_total_iva"] == 133.1
    assert resultado["margen"] > 0