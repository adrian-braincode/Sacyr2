from sacyr_maquinaria.src.models.maquinaria import Maquinaria
from typing import List

class FlotaRepository:
    """
    Repositorio encargado de la gestión de persistencia de la maquinaria.
    Simula una base de datos con una lista interna.
    """
    
    def __init__(self):
        # Datos simulados con asignación a diferentes IDs de obra
        self._datos_maquinaria = [
            {"id": "EXC-001", "name": "Excavadora Volvo", "type": "Pesada", "price": 250000.0, "maint": 45.0, "hours": 1200, "country": "España", "obra": "OBRA-VAL-01"},
            {"id": "TUN-005", "name": "Tuneladora Robbins", "type": "Crítica", "price": 12000000.0, "maint": 1500.0, "hours": 450, "country": "España", "obra": "OBRA-VAL-01"},
            {"id": "GRU-022", "name": "Grúa Liebherr", "type": "Elevación", "price": 850000.0, "maint": 120.0, "hours": 890, "country": "Chile", "obra": "OBRA-CHL-05"},
            {"id": "CAM-010", "name": "Camión Caterpillar", "type": "Transporte", "price": 150000.0, "maint": 30.0, "hours": 2100, "country": "España", "obra": "OBRA-VAL-01"}
        ]

    def obtener_toda_la_flota(self) -> List[Maquinaria]:
        """Devuelve todos los activos convertidos en objetos de modelo."""
        return [self._mapear_a_modelo(d) for d in self._datos_maquinaria]

    def obtener_maquinaria_por_obra(self, id_obra: str) -> List[Maquinaria]:
        """Filtra y devuelve solo las máquinas asignadas a una obra específica."""
        filtrados = [d for d in self._datos_maquinaria if d["obra"] == id_obra]
        return [self._mapear_a_modelo(f) for f in filtrados]

    def obtener_tipo_maquina(self, id_activo: str) -> str:
        """Devuelve el tipo de una máquina por su ID."""
        for data in self._datos_maquinaria:
            if data["id"] == id_activo:
                return data["type"]
        raise ValueError(f"Máquina con ID {id_activo} no encontrada.")

    def _mapear_a_modelo(self, data: dict) -> Maquinaria:
        """Helper privado para transformar diccionario a Dataclass."""
        return Maquinaria(
            id_activo=data["id"],
            nombre=data["name"],
            tipo=data["type"],
            precio_compra=data["price"],
            costo_mantenimiento_hora=data["maint"],
            horas_operativas=data["hours"],
            pais=data["country"],
            id_obra=data["obra"]
        )