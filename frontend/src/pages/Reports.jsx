import { useState, useEffect } from 'react';
import { ReportsService } from '../services/api';

export default function Reports() {
    const [reports, setReports] = useState([]);
    const [generating, setGenerating] = useState(false);

    useEffect(() => {
        loadReports();
    }, []);

    const loadReports = () => {
        ReportsService.getAll().then(res => setReports(res.data));
    };

    const generateReport = async (type) => {
        setGenerating(true);
        try {
            if (type === 'revenue') await ReportsService.generateRevenue();
            else if (type === 'students') await ReportsService.generateStudent();
            else if (type === 'rooms') await ReportsService.generateRoom();
            else if (type === 'customers') await ReportsService.generateCustomer();

            loadReports();
        } catch (error) {
            console.error("Error generating report", error);
        } finally {
            setGenerating(false);
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <h2 className="text-2xl font-bold">Informes Generados</h2>
                <div className="space-x-2">
                    <button
                        onClick={() => generateReport('revenue')}
                        disabled={generating}
                        className="btn btn-primary bg-blue-600 hover:bg-blue-700"
                    >
                        + Ingresos
                    </button>
                    <button
                        onClick={() => generateReport('students')}
                        disabled={generating}
                        className="btn btn-primary bg-green-600 hover:bg-green-700"
                    >
                        + Estudiantes
                    </button>
                    <button
                        onClick={() => generateReport('rooms')}
                        disabled={generating}
                        className="btn btn-primary bg-purple-600 hover:bg-purple-700"
                    >
                        + Salas
                    </button>
                </div>
            </div>

            <div className="card">
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="text-xs text-slate-400 uppercase bg-slate-800/50">
                            <tr>
                                <th className="px-4 py-3">ID</th>
                                <th className="px-4 py-3">Título</th>
                                <th className="px-4 py-3">Tipo</th>
                                <th className="px-4 py-3">Fecha Generación</th>
                                <th className="px-4 py-3">Estado</th>
                                <th className="px-4 py-3">Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                            {reports.map((report) => (
                                <tr key={report.id} className="border-b border-slate-700 hover:bg-slate-800/50">
                                    <td className="px-4 py-3 text-slate-400">#{report.id}</td>
                                    <td className="px-4 py-3 font-medium">{report.title}</td>
                                    <td className="px-4 py-3">
                                        <span className="px-2 py-1 rounded bg-slate-700 text-xs">
                                            {report.reportType}
                                        </span>
                                    </td>
                                    <td className="px-4 py-3">
                                        {new Date(report.generatedDate).toLocaleString()}
                                    </td>
                                    <td className="px-4 py-3">
                                        <span className="text-success">● {report.status}</span>
                                    </td>
                                    <td className="px-4 py-3">
                                        <button className="text-accent hover:underline">Ver</button>
                                    </td>
                                </tr>
                            ))}
                            {reports.length === 0 && (
                                <tr>
                                    <td colSpan="6" className="px-4 py-8 text-center text-slate-500">
                                        No hay informes generados.
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}
