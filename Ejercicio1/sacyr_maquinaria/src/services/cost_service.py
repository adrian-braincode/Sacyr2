from typing import List, Dict

class CostService:
    """
    Motor lógico de Sacyr para el análisis de rentabilidad y costes.
    Utiliza inyección de dependencias para acceder a los datos.
    """
    
    def __init__(self, flota_repo):
        self.repo = flota_repo

    def analizar_rentabilidad_obra(self, obra, tasa_iva: float, tasa_diesel: float) -> Dict:
        """
        Calcula el impacto económico de la maquinaria en una obra específica.
        Considera el factor logístico de la obra y los impuestos.
        """
        maquinas = self.repo.obtener_maquinaria_por_obra(obra.id_obra)
        
        detalles_maquinas = []
        gasto_total_neto = 0.0

        for m in maquinas:
            coste_mantenimiento = m.horas_operativas * m.costo_mantenimiento_hora * obra.factor_logistico
            coste_combustible = m.horas_operativas * tasa_diesel
            neto_activo = coste_mantenimiento + coste_combustible
            
            gasto_total_neto += neto_activo
            
            detalles_maquinas.append({
                "id": m.id_activo,
                "nombre": m.nombre,
                "neto": neto_activo
            })

        total_con_iva = gasto_total_neto * (1 + tasa_iva)
        margen_restante = obra.presupuesto_maximo - total_con_iva
        
        estado = "SALUDABLE" if margen_restante > (obra.presupuesto_maximo * 0.1) else "ALERTA"
        if margen_restante < 0:
            estado = "DÉFICIT CRÍTICO"

        return {
            "obra_nombre": obra.nombre,
            "presupuesto": obra.presupuesto_maximo,
            "gasto_total_iva": total_con_iva,
            "margen": margen_restante,
            "estado": estado,
            "activos_vinculados": detalles_maquinas
        }
