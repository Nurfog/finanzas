import { useState, useEffect } from 'react';
import { AnalyticsService } from '../services/api';

import InsightBadge from '../components/InsightBadge';

export default function Students() {
    const [data, setData] = useState(null);

    useEffect(() => {
        AnalyticsService.getStudentPerformance().then(res => setData(res.data));
    }, []);

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-bold">Rendimiento Estudiantil</h2>

            <InsightBadge type="positive">
                El <strong>{Math.round((data?.byPerformance?.find(p => p.performanceLevel === 'Excellent')?.studentCount || 0) / (data?.totalStudents || 1) * 100)}%</strong> de los estudiantes mantiene un nivel de excelencia. Se sugiere implementar programas de mentoría liderados por estos alumnos.
            </InsightBadge>

            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
                {data?.byPerformance?.map((perf, index) => (
                    <div key={index} className="card text-center">
                        <h3 className="text-lg font-semibold text-slate-300">{perf.performanceLevel}</h3>
                        <p className="text-3xl font-bold my-2">{perf.studentCount}</p>
                        <p className="text-sm text-slate-500">Estudiantes</p>
                        <div className="mt-2 text-xs text-accent">
                            Promedio: {Math.round(perf.averageScore)}
                        </div>
                    </div>
                ))}
            </div>

            <div className="card">
                <h3 className="text-lg font-semibold mb-4">Listado de Estudiantes</h3>
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="text-xs text-slate-400 uppercase bg-slate-800/50">
                            <tr>
                                <th className="px-4 py-3">Estudiante</th>
                                <th className="px-4 py-3">Programa</th>
                                <th className="px-4 py-3">Rendimiento</th>
                                <th className="px-4 py-3">Última Nota</th>
                                <th className="px-4 py-3">Promedio</th>
                            </tr>
                        </thead>
                        <tbody>
                            {data?.students?.map((student, index) => (
                                <tr key={index} className="border-b border-slate-700 hover:bg-slate-800/50">
                                    <td className="px-4 py-3 font-medium">{student.studentName}</td>
                                    <td className="px-4 py-3 text-slate-400">{student.program}</td>
                                    <td className="px-4 py-3">
                                        <span className={`px-2 py-1 rounded text-xs font-bold ${student.latestPerformance === 'Excellent' ? 'bg-green-500/20 text-green-400' :
                                            student.latestPerformance === 'Good' ? 'bg-blue-500/20 text-blue-400' :
                                                student.latestPerformance === 'Average' ? 'bg-yellow-500/20 text-yellow-400' :
                                                    'bg-red-500/20 text-red-400'
                                            }`}>
                                            {student.latestPerformance}
                                        </span>
                                    </td>
                                    <td className="px-4 py-3 font-bold">{student.latestScore}</td>
                                    <td className="px-4 py-3">{student.averageScore}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}
