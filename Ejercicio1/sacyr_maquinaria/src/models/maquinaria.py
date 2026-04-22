from dataclasses import dataclass

@dataclass
class Maquinaria:
    """
    Entidad que representa una unidad de maquinaria pesada.
    Ahora incluye la vinculación obligatoria a una Obra específica.
    """
    id_activo: str
    nombre: str
    tipo: str
    precio_compra: float
    costo_mantenimiento_hora: float
    horas_operativas: int
    pais: str
    id_obra: str

    def __post_init__(self):
        if self.precio_compra < 0:
            raise ValueError(f"El precio de compra de {self.id_activo} no puede ser negativo.")
        if self.horas_operativas < 0:
            self.horas_operativas = 0