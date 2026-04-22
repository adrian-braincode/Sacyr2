from dataclasses import dataclass

@dataclass
class Obra:
    """
    Representa un proyecto de construcción en Sacyr.
    Contiene la información presupuestaria y logística del sitio.
    """
    id_obra: str
    nombre: str
    ubicacion: str
    presupuesto_maximo: float
    factor_logistico: float

    def __post_init__(self):
        if self.presupuesto_maximo <= 0:
            raise ValueError(f"Error en Obra {self.id_obra}: El presupuesto debe ser un valor positivo.")
        if self.factor_logistico < 1.0:
            self.factor_logistico = 1.0