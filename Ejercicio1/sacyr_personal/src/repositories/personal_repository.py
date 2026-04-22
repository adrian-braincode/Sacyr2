from sacyr_personal.src.models.operario import Operario
from typing import List

class PersonalRepository:
    """Gestiona la base de datos de empleados de Sacyr."""
    
    def __init__(self):
        # Datos simulados: Algunos operarios tienen máquinas asignadas
        self._datos_empleados = [
            {
                "id": "EMP-001", "nombre": "Carlos Gómez", 
                "cargo": "Operador Tuneladora", "costo": 25.0, 
                "obra": "OBRA-VAL-01", "maquina": "TUN-005"
            },
            {
                "id": "EMP-002", "nombre": "Ana Belén", 
                "cargo": "Conductor Pesado", "costo": 18.5, 
                "obra": "OBRA-VAL-01", "maquina": "EXC-001"
            },
            {
                "id": "EMP-003", "nombre": "Luis Paez", 
                "cargo": "Conductor Pesado", "costo": 18.5, 
                "obra": "OBRA-CHL-05", "maquina": "GRU-022"
            }
        ]

    def obtener_personal_por_obra(self, id_obra: str) -> List[Operario]:
        """Filtra los empleados que están trabajando en un proyecto específico."""
        filtrados = [e for e in self._datos_empleados if e["obra"] == id_obra]
        return [self._mapear_a_modelo(f) for f in filtrados]

    def _mapear_a_modelo(self, data: dict) -> Operario:
        return Operario(
            id_empleado=data["id"],
            nombre=data["nombre"],
            especialidad=data["cargo"],
            costo_hora=data["costo"],
            id_obra=data["obra"],
            id_maquina_asignada=data["maquina"]
        )