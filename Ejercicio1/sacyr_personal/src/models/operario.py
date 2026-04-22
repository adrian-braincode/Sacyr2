from dataclasses import dataclass

@dataclass
class Operario:
    id_empleado: str
    nombre: str
    especialidad: str
    costo_hora: float
    id_obra: str
    id_maquina_asignada: str