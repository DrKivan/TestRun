# Estimacion de Costos - EvaluaT

La estimacion se realiza considerando un escenario academico con un unico desarrollador. No se incluyen costos elevados de infraestructura porque el proyecto utiliza herramientas gratuitas o de uso local, como GitHub, Visual Studio Code, .NET, React y PostgreSQL mediante Docker.

## Formula de estimacion

Para este proyecto se usa una adaptacion simple basada en esfuerzo, similar al criterio de COCOMO basico:

```text
Persona-mes = Horas estimadas / Horas laborales del mes
Costo por hora = Costo mensual estimado del desarrollador / Horas laborales del mes
Subtotal = Horas estimadas * Costo por hora
Costo total = suma de subtotales + costos de herramientas o servicios externos
```

Valores usados:

```text
Horas laborales del mes = 160 h
Costo mensual academico estimado = Bs. 4.000
Costo por hora = 4.000 / 160 = Bs. 25
```

## Tabla de costos

| Recurso / Actividad | Horas Est. | Costo/Hora | Subtotal | Observaciones |
| --- | ---: | ---: | ---: | --- |
| Analisis y planificacion | 8 h | Bs. 25 | Bs. 200 | Unico desarrollador |
| Diseno de arquitectura y UML | 10 h | Bs. 25 | Bs. 250 | Diseno tecnico y diagramas |
| Desarrollo de modulos CRUD | 24 h | Bs. 25 | Bs. 600 | Backend, frontend y persistencia |
| Implementacion de patrones | 12 h | Bs. 25 | Bs. 300 | Strategy, Repository, Factory, Observer, DI |
| Refactorizacion documentada | 8 h | Bs. 25 | Bs. 200 | Mejoras de diseno y reduccion de bad smells |
| Pruebas unitarias e integracion | 10 h | Bs. 25 | Bs. 250 | xUnit, integracion API y cobertura |
| Documentacion y entrega | 6 h | Bs. 25 | Bs. 150 | Informe tecnico, anexos y evidencias |
| Herramientas (GitHub, IDE, CI/CD) | — | Bs. 0 | Bs. 0 | GitHub, VSCode y herramientas gratuitas |
| **TOTAL ESTIMADO** | **78 h** | **Academico** | **Bs. 1.950** | Ajustar si se usan servicios de pago |

## Consideracion COCOMO

En COCOMO basico, el esfuerzo se expresa en persona-mes. Para este proyecto:

```text
Persona-mes = 78 h / 160 h = 0,49 persona-mes
Costo total = 0,49 * Bs. 4.000 = Bs. 1.950 aproximado
```

Esto indica que el proyecto representa aproximadamente medio mes de trabajo de un desarrollador en contexto academico. Si se agregan servicios pagados, despliegue en la nube o consumo real de APIs de IA, el costo deberia actualizarse.
