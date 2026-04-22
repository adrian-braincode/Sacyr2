def formatear_moneda(valor: float, moneda: str = "€") -> str:
    """Devuelve un string formateado para informes financieros de Sacyr."""
    if valor >= 1_000_000:
        return f"{valor / 1_000_000:.2f}M {moneda}"
    return f"{valor:,.2f} {moneda}"

def generar_separador(titulo: str = "") -> str:
    return f"\n{'='*20} {titulo} {'='*20}"