import pytest
from unittest.mock import MagicMock
from sacyr_personal.src.models.operario import Operario
from sacyr_personal.src.services.nomina_service import NominaService

def test_calculo_nomina_mes_completo():
    # Setup
    mock_repo = MagicMock()
    # Creamos un operario de prueba (20€/hora)
    mock_repo.obtener_personal_por_obra.return_value = [
        Operario("E1", "Test", "Especialista", 20.0, "OBRA1", "M1")
    ]
    
    servicio = NominaService(mock_repo)
    
    # Execution: 100 horas a 20€ = 2000€
    resultado = servicio.calcular_coste_personal_obra("OBRA1", 100)
    
    # Validation
    assert resultado["total_personal_neto"] == 2000.0
    assert resultado["num_empleados"] == 1