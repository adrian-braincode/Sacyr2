import yaml
import os
from sacyr_maquinaria.src.repositories.flota_repository import FlotaRepository
from sacyr_maquinaria.src.services.cost_service import CostService
from sacyr_personal.src.repositories.personal_repository import PersonalRepository
from sacyr_personal.src.services.nomina_service import NominaService
from sacyr_maquinaria.src.models.obra import Obra

def cargar_config(ruta):
    # Hacer la ruta relativa al directorio del script
    script_dir = os.path.dirname(os.path.abspath(__file__))
    ruta_completa = os.path.join(script_dir, ruta)
    with open(ruta_completa, 'r') as f:
        return yaml.safe_load(f)

def main():
    # 1. Cargar configuraciones de ambos módulos
    conf_maq = cargar_config('sacyr_maquinaria/config/settings.yaml')
    conf_per = cargar_config('sacyr_personal/config/settings.yaml')

    # 2. Definir contexto de Obra
    proyecto = Obra("OBRA-VAL-01", "Puerto Valencia", "España", 2000000.0, 1.10)

    # 3. Inicializar Repositorios
    repo_maq = FlotaRepository()
    repo_per = PersonalRepository()

    # Crear diccionario de tipos de máquinas para pluses
    tipos_maquinas = {}
    for maq in repo_maq.obtener_toda_la_flota():
        tipos_maquinas[maq.id_activo] = maq.tipo

    # 4. Inicializar Servicios
    serv_maq = CostService(repo_maq)
    serv_per = NominaService(repo_per)

    # 5. Cálculos de ambos departamentos
    analisis_maq = serv_maq.analizar_rentabilidad_obra(
        proyecto, 
        conf_maq['fiscalidad']['iva'], 
        conf_maq['costes']['diesel_hora']
    )
    
    analisis_per = serv_per.calcular_coste_personal_obra(
        proyecto.id_obra, 
        conf_per['nomina']['horas_estandar_mes'],
        tipos_maquinas
    )

    # 6. Gran Total (La Interconexión)
    coste_total_obra = analisis_maq['gasto_total_iva'] + analisis_per['total_personal_neto']
    margen_final = proyecto.presupuesto_maximo - coste_total_obra

    print(f"=== INFORME CONSOLIDADO SACYR: {proyecto.nombre} ===")
    print(f"Gasto Maquinaria: {analisis_maq['gasto_total_iva']:,.2f} €")
    print(f"Gasto Personal:   {analisis_per['total_personal_neto']:,.2f} €")
    print(f"--------------------------------------------------")
    print(f"COSTE TOTAL:      {coste_total_obra:,.2f} €")
    print(f"MARGEN PROYECTO:  {margen_final:,.2f} €")
    
    if margen_final < 0:
        print("⚠️ ALERTA: Proyecto en déficit financiero.")

if __name__ == "__main__":
    main()