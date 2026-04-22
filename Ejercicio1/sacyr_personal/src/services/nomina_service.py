from typing import List, Dict

class NominaService:
    def __init__(self, personal_repo):
        self.repo = personal_repo

    def calcular_coste_personal_obra(self, id_obra: str, horas_mes: int, tipos_maquinas: Dict[str, str] = None) -> Dict:
        """
        Calcula el coste total del personal para una obra, incluyendo pluses de peligrosidad
        si la máquina asignada es de tipo 'Crítica'.
        """
        empleados = self.repo.obtener_personal_por_obra(id_obra)
        total_nomina = 0.0
        plus_peligrosidad = 0.15  # 15% extra para máquinas críticas
        
        for empleado in empleados:
            costo_base = empleado.costo_hora * horas_mes
            costo_final = costo_base
            
            # Aplicar plus si la máquina es crítica
            if tipos_maquinas and empleado.id_maquina_asignada in tipos_maquinas:
                tipo_maquina = tipos_maquinas[empleado.id_maquina_asignada]
                if tipo_maquina == "Crítica":
                    costo_final = costo_base * (1 + plus_peligrosidad)
            
            total_nomina += costo_final
        
        return {
            "id_obra": id_obra,
            "num_empleados": len(empleados),
            "total_personal_neto": total_nomina
        }