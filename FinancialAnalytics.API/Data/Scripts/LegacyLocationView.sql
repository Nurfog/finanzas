USE financial_analytics;

CREATE OR REPLACE VIEW vw_legacy_cursos_sedes AS
WITH CourseStats AS (
    SELECT 
        dc.idCursoAbierto, 
        COUNT(DISTINCT dc.idAlumno) as AlumnosInscritos
    FROM sige_sam_v3.detallecontrato dc
    INNER JOIN sige_sam_v3.contrato c ON dc.idContrato = c.idContrato
    WHERE dc.Activo = 1 
      AND c.Activo = 1
      AND dc.idtiporegistroacademico NOT IN (2,3,4,17)
    GROUP BY dc.idCursoAbierto
)
SELECT 
    curabi.idCursoAbierto as 'IdCursoAbierto',
    sed.nombre as 'Sede',
    cur.NombreCurso as 'NombreCurso',
    sal.Nombre as 'Sala',
    sal.Cupo as 'Capacidad',
    COALESCE(stats.AlumnosInscritos, 0) as 'AlumnosInscritos',
    (sal.Cupo - COALESCE(stats.AlumnosInscritos, 0)) as 'CuposDisponibles',
    combid.nombrecorto as 'DiasClases',
    curabi.FechaInicio as 'FechaInicio',
    curabi.FechaFin as 'FechaFin',
    bh.HoraInicio as 'HoraInicio',
    bh.HoraFin as 'HoraFin',
    alu.idAlumno,
    concat(alu.nombres,' ',alu.ap_paterno,' ', alu.ap_materno) as 'Nombre Alumno',
    CONCAT(apo.Nombres, ' ', apo.AP_Paterno, ' ', apo.AP_Materno) as 'Nombre Apoderado',
    apo.Email as 'EmailApoderado'
FROM 
    sige_sam_v3.cursosabiertos as curabi 
    INNER JOIN sige_sam_v3.detallecontrato as detcon on detcon.idCursoAbierto = curabi.idCursoAbierto
    INNER JOIN sige_sam_v3.contrato as cont on detcon.idContrato = cont.idContrato
    INNER JOIN sige_sam_v3.tipocombinacion as tc on curabi.idTipoCombinacion = tc.idTipoCombinacion
    INNER JOIN sige_sam_v3.curso as cur on curabi.idCursos = cur.idCursos
    INNER JOIN sige_sam_v3.plananual as pa on curabi.idPlanAnual = pa.idPlanAnual
    INNER JOIN sige_sam_v3.salas as sal on curabi.idSalas = sal.idSalas
    INNER JOIN sige_sam_v3.combinaciondias as combid on curabi.idCombinacionDias = combid.idCombinacionDias
    INNER JOIN sige_sam_v3.jornada as jor on curabi.idJornada = jor.idJornada
    INNER JOIN sige_sam_v3.bloqueshorario as bh on bh.idBloquesHorario = curabi.idBloquesHorario
    INNER JOIN sige_sam_v3.sede as sed on curabi.idSede = sed.idSede
    INNER JOIN sige_sam_v3.alumnos as alu on detcon.idAlumno = alu.idAlumno
    LEFT JOIN sige_sam_v3.apoderado as apo on alu.idAlumno = apo.idAlumno
    LEFT JOIN CourseStats stats ON curabi.idCursoAbierto = stats.idCursoAbierto
WHERE 
    cont.Activo = 1 and
    detcon.Activo = 1 and
    detcon.idtiporegistroacademico not in(2,3,4,17) and
    curabi.Activo = 1;
