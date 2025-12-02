import { useState, useEffect } from 'react';
import { ReportsService } from '../services/api';
import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    BarElement,
    Title,
    Tooltip,
    Legend,
    ArcElement
} from 'chart.js';
import { Bar, Doughnut } from 'react-chartjs-2';

ChartJS.register(
    CategoryScale,
    LinearScale,
    BarElement,
    Title,
    Tooltip,
    Legend,
    ArcElement
);

function ReportViewer({ report }) {
    if (!report || !report.content) return <div className="text-slate-400">Sin contenido disponible</div>;

    let data;
    try {
        data = typeof report.content === 'string' ? JSON.parse(report.content) : report.content;
    } catch (e) {
        return <div className="text-red-400">Error al procesar los datos del reporte.</div>;
    }

    const formatCurrency = (val) => new Intl.NumberFormat('es-CL', { style: 'currency', currency: 'CLP' }).format(val);
    const formatPercent = (val) => `${Math.round(val)}%`;

    // --- Executive Summary Components ---
    const InsightBadge = ({ type, children }) => {
        const colors = {
            positive: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
            warning: 'bg-amber-500/10 text-amber-400 border-amber-500/20',
            info: 'bg-blue-500/10 text-blue-400 border-blue-500/20'
        };
        return (
            <div className={`px-4 py-3 rounded-lg border flex items-start gap-3 ${colors[type] || colors.info}`}>
                <span className="text-xl mt-0.5">{type === 'positive' ? '🚀' : type === 'warning' ? '⚠️' : '💡'}</span>
                <span className="text-sm font-medium">{children}</span>
            </div>
        );
    };

    if (report.reportType === 'Ingresos') {
        const chartData = {
            labels: data.Summary.ByPaymentMethod.map(pm => pm.PaymentMethod),
            datasets: [{
                data: data.Summary.ByPaymentMethod.map(pm => pm.Revenue),
                backgroundColor: ['#3b82f6', '#10b981', '#f59e0b', '#ef4444'],
                borderWidth: 0,
            }]
        };

        return (
            <div className="space-y-8">
                {/* Executive Summary */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <InsightBadge type="positive">
                        Los ingresos totales son sólidos, con un ticket promedio de <strong>{formatCurrency(data.Summary.AverageTransaction)}</strong>.
                    </InsightBadge>
                    <InsightBadge type="info">
                        La <strong>{data.ByLocation.sort((a, b) => b.Revenue - a.Revenue)[0]?.LocationName}</strong> lidera la facturación este periodo.
                    </InsightBadge>
                </div>

                {/* KPIs */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="bg-slate-800/50 p-5 rounded-xl border border-slate-700">
                        <p className="text-slate-400 text-sm font-medium uppercase tracking-wider">Ingresos Totales</p>
                        <p className="text-3xl font-bold text-emerald-400 mt-2">{formatCurrency(data.Summary.TotalRevenue)}</p>
                    </div>
                    <div className="bg-slate-800/50 p-5 rounded-xl border border-slate-700">
                        <p className="text-slate-400 text-sm font-medium uppercase tracking-wider">Transacciones</p>
                        <p className="text-3xl font-bold text-white mt-2">{data.Summary.TransactionCount}</p>
                    </div>
                    <div className="bg-slate-800/50 p-5 rounded-xl border border-slate-700">
                        <p className="text-slate-400 text-sm font-medium uppercase tracking-wider">Ticket Promedio</p>
                        <p className="text-3xl font-bold text-blue-400 mt-2">{formatCurrency(data.Summary.AverageTransaction)}</p>
                    </div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                    {/* Table */}
                    <div className="lg:col-span-2">
                        <h4 className="text-lg font-semibold text-white mb-4 flex items-center">
                            <span className="w-1.5 h-5 bg-blue-500 rounded-full mr-2"></span>
                            Desglose por Sede
                        </h4>
                        <div className="overflow-hidden rounded-xl border border-slate-700">
                            <table className="w-full text-sm text-left text-slate-300">
                                <thead className="text-xs uppercase bg-slate-800 text-slate-400 font-semibold">
                                    <tr>
                                        <th className="px-6 py-3">Sede</th>
                                        <th className="px-6 py-3 text-right">Ingresos</th>
                                        <th className="px-6 py-3 text-right">Tx</th>
                                        <th className="px-6 py-3 text-right">Promedio</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-700/50 bg-slate-800/30">
                                    {data.ByLocation.map((loc, i) => (
                                        <tr key={i} className="hover:bg-slate-700/30 transition-colors">
                                            <td className="px-6 py-4 font-medium text-white">{loc.LocationName}</td>
                                            <td className="px-6 py-4 text-right font-mono text-emerald-400">{formatCurrency(loc.Revenue)}</td>
                                            <td className="px-6 py-4 text-right">{loc.TransactionCount}</td>
                                            <td className="px-6 py-4 text-right font-mono">{formatCurrency(loc.AverageTransaction)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    {/* Chart */}
                    <div>
                        <h4 className="text-lg font-semibold text-white mb-4 flex items-center">
                            <span className="w-1.5 h-5 bg-purple-500 rounded-full mr-2"></span>
                            Métodos de Pago
                        </h4>
                        <div className="bg-slate-800/30 p-4 rounded-xl border border-slate-700/50 h-64 flex items-center justify-center">
                            <Doughnut data={chartData} options={{
                                plugins: { legend: { position: 'bottom', labels: { color: '#94a3b8' } } },
                                maintainAspectRatio: false
                            }} />
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    if (report.reportType === 'Estudiantes') {
        const chartData = {
            labels: data.Analytics.ByPerformance.map(p => p.PerformanceLevel),
            datasets: [{
                label: 'Estudiantes',
                data: data.Analytics.ByPerformance.map(p => p.StudentCount),
                backgroundColor: ['#10b981', '#3b82f6', '#f59e0b'],
                borderRadius: 6,
            }]
        };

        return (
            <div className="space-y-8">
                {/* Executive Summary */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <InsightBadge type="positive">
                        El <strong>{formatPercent((data.Analytics.ByPerformance.find(p => p.PerformanceLevel === 'Good')?.StudentCount || 0) / data.Analytics.TotalStudents * 100)}</strong> de los estudiantes mantiene un rendimiento "Bueno" o superior.
                    </InsightBadge>
                    <InsightBadge type="warning">
                        Se detectaron <strong>{data.Analytics.ByPerformance.find(p => p.PerformanceLevel === 'Average')?.StudentCount || 0}</strong> estudiantes en nivel promedio que podrían requerir apoyo.
                    </InsightBadge>
                </div>

                <div className="bg-slate-800/50 p-6 rounded-xl border border-slate-700 flex items-center justify-between">
                    <div>
                        <p className="text-slate-400 text-sm font-medium uppercase tracking-wider">Total Estudiantes Analizados</p>
                        <p className="text-4xl font-bold text-white mt-2">{data.Analytics.TotalStudents}</p>
                    </div>
                    <div className="text-5xl">🎓</div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                    <div>
                        <h4 className="text-lg font-semibold text-white mb-4 flex items-center">
                            <span className="w-1.5 h-5 bg-amber-500 rounded-full mr-2"></span>
                            Distribución de Rendimiento
                        </h4>
                        <div className="bg-slate-800/30 p-4 rounded-xl border border-slate-700/50 h-64">
                            <Bar data={chartData} options={{
                                plugins: { legend: { display: false } },
                                scales: {
                                    y: { grid: { color: '#334155' }, ticks: { color: '#94a3b8' } },
                                    x: { grid: { display: false }, ticks: { color: '#94a3b8' } }
                                },
                                maintainAspectRatio: false
                            }} />
                        </div>
                    </div>

                    <div>
                        <h4 className="text-lg font-semibold text-white mb-4 flex items-center">
                            <span className="w-1.5 h-5 bg-slate-500 rounded-full mr-2"></span>
                            Top Estudiantes
                        </h4>
                        <div className="overflow-hidden rounded-xl border border-slate-700">
                            <table className="w-full text-sm text-left text-slate-300">
                                <thead className="text-xs uppercase bg-slate-800 text-slate-400 font-semibold">
                                    <tr>
                                        <th className="px-4 py-3">Estudiante</th>
                                        <th className="px-4 py-3 text-right">Promedio</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-700/50 bg-slate-800/30">
                                    {data.Analytics.Students.slice(0, 5).map((student, i) => (
                                        <tr key={i} className="hover:bg-slate-700/30 transition-colors">
                                            <td className="px-4 py-3">
                                                <div className="font-medium text-white">{student.StudentName}</div>
                                                <div className="text-xs text-slate-500">{student.Program}</div>
                                            </td>
                                            <td className="px-4 py-3 text-right font-bold text-emerald-400">{student.AverageScore.toFixed(1)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    if (report.reportType === 'Salas') {
        const topRooms = data.Analytics.ByRoom.sort((a, b) => b.AverageUtilization - a.AverageUtilization).slice(0, 5);
        const chartData = {
            labels: topRooms.map(r => r.RoomName),
            datasets: [{
                label: 'Utilización %',
                data: topRooms.map(r => r.AverageUtilization),
                backgroundColor: '#8b5cf6',
                borderRadius: 4,
            }]
        };

        return (
            <div className="space-y-8">
                {/* Executive Summary */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <InsightBadge type={data.Analytics.OverallUtilization > 70 ? "positive" : "warning"}>
                        La utilización global es del <strong>{formatPercent(data.Analytics.OverallUtilization)}</strong>, {data.Analytics.OverallUtilization > 70 ? "lo que indica un uso eficiente de recursos." : "indicando capacidad ociosa disponible."}
                    </InsightBadge>
                    <InsightBadge type="info">
                        La sala más solicitada es <strong>{topRooms[0]?.RoomName}</strong> con {formatPercent(topRooms[0]?.AverageUtilization)} de ocupación.
                    </InsightBadge>
                </div>

                <div className="bg-slate-800/50 p-6 rounded-xl border border-slate-700 flex items-center justify-between">
                    <div>
                        <p className="text-slate-400 text-sm font-medium uppercase tracking-wider">Utilización Global</p>
                        <p className="text-4xl font-bold text-purple-400 mt-2">{formatPercent(data.Analytics.OverallUtilization)}</p>
                    </div>
                    <div className="text-5xl">🏢</div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                    <div className="lg:col-span-2">
                        <h4 className="text-lg font-semibold text-white mb-4 flex items-center">
                            <span className="w-1.5 h-5 bg-purple-500 rounded-full mr-2"></span>
                            Detalle por Sala
                        </h4>
                        <div className="overflow-hidden rounded-xl border border-slate-700">
                            <table className="w-full text-sm text-left text-slate-300">
                                <thead className="text-xs uppercase bg-slate-800 text-slate-400 font-semibold">
                                    <tr>
                                        <th className="px-6 py-3">Sala</th>
                                        <th className="px-6 py-3 text-right">Uso</th>
                                        <th className="px-6 py-3 text-right">Asistentes</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-700/50 bg-slate-800/30">
                                    {data.Analytics.ByRoom.slice(0, 6).map((room, i) => (
                                        <tr key={i} className="hover:bg-slate-700/30 transition-colors">
                                            <td className="px-6 py-4 font-medium text-white">
                                                {room.RoomName}
                                                <span className="block text-xs text-slate-500">{room.LocationName}</span>
                                            </td>
                                            <td className="px-6 py-4 text-right">
                                                <div className="flex items-center justify-end">
                                                    <span className="mr-2 font-bold">{formatPercent(room.AverageUtilization)}</span>
                                                    <div className="w-16 bg-slate-700 rounded-full h-1.5">
                                                        <div className="bg-purple-500 h-1.5 rounded-full" style={{ width: `${room.AverageUtilization}%` }}></div>
                                                    </div>
                                                </div>
                                            </td>
                                            <td className="px-6 py-4 text-right text-slate-400">{room.TotalAttendees}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <div>
                        <h4 className="text-lg font-semibold text-white mb-4 flex items-center">
                            <span className="w-1.5 h-5 bg-purple-400 rounded-full mr-2"></span>
                            Top 5 Salas
                        </h4>
                        <div className="bg-slate-800/30 p-4 rounded-xl border border-slate-700/50 h-64">
                            <Bar data={chartData} options={{
                                indexAxis: 'y',
                                plugins: { legend: { display: false } },
                                scales: {
                                    x: { grid: { color: '#334155' }, ticks: { color: '#94a3b8' } },
                                    y: { grid: { display: false }, ticks: { color: '#94a3b8' } }
                                },
                                maintainAspectRatio: false
                            }} />
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    if (report.reportType === 'Clientes' || report.reportType === 'Customers') {
        const customerData = data.Analytics || data; // Handle potential structure differences
        const segments = customerData.BySegment || [];

        const chartData = {
            labels: segments.map(s => s.Segment),
            datasets: [{
                data: segments.map(s => s.Count),
                backgroundColor: ['#f59e0b', '#3b82f6', '#8b5cf6'],
                borderWidth: 0,
            }]
        };

        return (
            <div className="space-y-8">
                {/* Executive Summary */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <InsightBadge type="positive">
                        Se han registrado <strong>{customerData.NewCustomersLastMonth || 0}</strong> nuevos clientes en el último mes, mostrando un crecimiento activo.
                    </InsightBadge>
                    <InsightBadge type="info">
                        El segmento <strong>{segments.sort((a, b) => b.Count - a.Count)[0]?.Segment}</strong> es el más representativo, con el {formatPercent((segments.sort((a, b) => b.Count - a.Count)[0]?.Count || 0) / customerData.TotalCustomers * 100)} del total.
                    </InsightBadge>
                </div>

                <div className="bg-slate-800/50 p-6 rounded-xl border border-slate-700 flex items-center justify-between">
                    <div>
                        <p className="text-slate-400 text-sm font-medium uppercase tracking-wider">Total Clientes</p>
                        <p className="text-4xl font-bold text-amber-400 mt-2">{customerData.TotalCustomers}</p>
                    </div>
                    <div className="text-5xl">👥</div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                    <div>
                        <h4 className="text-lg font-semibold text-white mb-4 flex items-center">
                            <span className="w-1.5 h-5 bg-amber-500 rounded-full mr-2"></span>
                            Distribución por Segmento
                        </h4>
                        <div className="bg-slate-800/30 p-4 rounded-xl border border-slate-700/50 h-64 flex items-center justify-center">
                            <Doughnut data={chartData} options={{
                                plugins: { legend: { position: 'right', labels: { color: '#94a3b8' } } },
                                maintainAspectRatio: false
                            }} />
                        </div>
                    </div>

                    <div>
                        <h4 className="text-lg font-semibold text-white mb-4 flex items-center">
                            <span className="w-1.5 h-5 bg-blue-500 rounded-full mr-2"></span>
                            Últimos Clientes Registrados
                        </h4>
                        <div className="overflow-hidden rounded-xl border border-slate-700">
                            <table className="w-full text-sm text-left text-slate-300">
                                <thead className="text-xs uppercase bg-slate-800 text-slate-400 font-semibold">
                                    <tr>
                                        <th className="px-4 py-3">Nombre</th>
                                        <th className="px-4 py-3">Tipo</th>
                                        <th className="px-4 py-3 text-right">Fecha</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-700/50 bg-slate-800/30">
                                    {customerData.RecentCustomers?.slice(0, 5).map((customer, i) => (
                                        <tr key={i} className="hover:bg-slate-700/30 transition-colors">
                                            <td className="px-4 py-3 font-medium text-white">{customer.Name}</td>
                                            <td className="px-4 py-3">
                                                <span className={`px-2 py-0.5 rounded-full text-xs ${customer.CustomerType === 'Premium' ? 'bg-amber-500/20 text-amber-400' :
                                                    customer.CustomerType === 'VIP' ? 'bg-purple-500/20 text-purple-400' : 'bg-blue-500/20 text-blue-400'
                                                    }`}>
                                                    {customer.CustomerType}
                                                </span>
                                            </td>
                                            <td className="px-4 py-3 text-right text-slate-400">
                                                {new Date(customer.RegistrationDate).toLocaleDateString()}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        );
    }
    return (
        <div className="space-y-4">
            <div className="bg-yellow-500/10 border border-yellow-500/20 p-4 rounded-lg text-yellow-200 text-sm">
                Vista ejecutiva no disponible para este tipo de reporte. Mostrando datos crudos.
            </div>
            <pre className="bg-slate-900/50 p-4 rounded-lg overflow-x-auto text-sm font-mono text-slate-300 border border-slate-700">
                {JSON.stringify(data, null, 2)}
            </pre>
        </div>
    );
}

export default function Reports() {
    const [reports, setReports] = useState([]);
    const [generating, setGenerating] = useState(false);
    const [selectedReport, setSelectedReport] = useState(null);
    const [isModalOpen, setIsModalOpen] = useState(false);

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

    const downloadExcelReport = async () => {
        setGenerating(true);
        try {
            // Calculate date range (last 6 months by default)
            const endDate = new Date();
            const startDate = new Date();
            startDate.setMonth(startDate.getMonth() - 6);

            const response = await ReportsService.downloadExcel(
                startDate.toISOString().split('T')[0],
                endDate.toISOString().split('T')[0]
            );

            // Create blob from response
            const blob = new Blob([response.data], {
                type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
            });

            // Create download link
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `Reporte_Ingresos_${startDate.toISOString().split('T')[0]}_${endDate.toISOString().split('T')[0]}.xlsx`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
        } catch (error) {
            console.error("Error downloading Excel report", error);
            alert("Error al descargar el reporte Excel. Por favor intenta nuevamente.");
        } finally {
            setGenerating(false);
        }
    };

    const viewReport = async (id) => {
        console.log("Viewing report:", id);
        try {
            const res = await ReportsService.getById(id);
            console.log("Report details:", res.data);
            setSelectedReport(res.data);
            setIsModalOpen(true);
        } catch (error) {
            console.error("Error fetching report details", error);
            alert("Error al cargar el reporte. Revisa la consola.");
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-3xl font-bold text-white">Informes</h2>
                    <p className="text-slate-400 mt-1">Genera y visualiza informes detallados del sistema</p>
                </div>
                <div className="flex space-x-3">
                    <button
                        onClick={() => generateReport('revenue')}
                        disabled={generating}
                        className="btn bg-gradient-to-r from-blue-600 to-blue-500 hover:from-blue-500 hover:to-blue-400 text-white shadow-lg shadow-blue-500/20"
                    >
                        + Ingresos
                    </button>
                    <button
                        onClick={() => generateReport('students')}
                        disabled={generating}
                        className="btn bg-gradient-to-r from-green-600 to-green-500 hover:from-green-500 hover:to-green-400 text-white shadow-lg shadow-green-500/20"
                    >
                        + Estudiantes
                    </button>
                    <button
                        onClick={() => generateReport('rooms')}
                        disabled={generating}
                        className="btn bg-gradient-to-r from-purple-600 to-purple-500 hover:from-purple-500 hover:to-purple-400 text-white shadow-lg shadow-purple-500/20"
                    >
                        + Salas
                    </button>
                    <button
                        onClick={downloadExcelReport}
                        disabled={generating}
                        className="btn bg-gradient-to-r from-emerald-600 to-emerald-500 hover:from-emerald-500 hover:to-emerald-400 text-white shadow-lg shadow-emerald-500/20 flex items-center gap-2"
                    >
                        <span>📊</span>
                        <span>Descargar Excel</span>
                    </button>
                </div>
            </div>

            <div className="card bg-secondary/50 backdrop-blur-xl border-slate-700/50">
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="text-xs text-slate-400 uppercase bg-slate-800/50 border-b border-slate-700">
                            <tr>
                                <th className="px-6 py-4 font-semibold">ID</th>
                                <th className="px-6 py-4 font-semibold">Título</th>
                                <th className="px-6 py-4 font-semibold">Tipo</th>
                                <th className="px-6 py-4 font-semibold">Fecha Generación</th>
                                <th className="px-6 py-4 font-semibold">Estado</th>
                                <th className="px-6 py-4 font-semibold">Acciones</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-700/50">
                            {reports.map((report) => (
                                <tr key={report.id} className="hover:bg-slate-700/30 transition-colors">
                                    <td className="px-6 py-4 text-slate-400">#{report.id}</td>
                                    <td className="px-6 py-4 font-medium text-white">{report.title}</td>
                                    <td className="px-6 py-4">
                                        <span className="px-2.5 py-1 rounded-full bg-slate-700/50 text-slate-300 text-xs border border-slate-600">
                                            {report.reportType}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 text-slate-300">
                                        {new Date(report.generatedDate).toLocaleString()}
                                    </td>
                                    <td className="px-6 py-4">
                                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${report.status === 'Completed' || report.status === 'Generado' ? 'bg-green-500/10 text-green-400' : 'bg-yellow-500/10 text-yellow-400'
                                            }`}>
                                            {report.status === 'Completed' || report.status === 'Generado' ? '● Completado' : '● Pendiente'}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4">
                                        <button
                                            onClick={() => viewReport(report.id)}
                                            className="text-accent hover:text-blue-400 font-medium transition-colors"
                                        >
                                            Ver Detalles
                                        </button>
                                    </td>
                                </tr>
                            ))}
                            {reports.length === 0 && (
                                <tr>
                                    <td colSpan="6" className="px-6 py-12 text-center text-slate-500">
                                        <div className="flex flex-col items-center justify-center">
                                            <span className="text-4xl mb-3">📄</span>
                                            <p>No hay informes generados aún.</p>
                                        </div>
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Report Details Modal */}
            {isModalOpen && selectedReport && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
                    <div className="bg-secondary border border-slate-600 rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh] flex flex-col">
                        <div className="p-6 border-b border-slate-700 flex justify-between items-center bg-slate-800/50 rounded-t-2xl">
                            <div>
                                <h3 className="text-xl font-bold text-white">{selectedReport.title}</h3>
                                <p className="text-sm text-slate-400 mt-1">
                                    Generado el {new Date(selectedReport.generatedDate).toLocaleString()}
                                </p>
                            </div>
                            <button
                                onClick={() => setIsModalOpen(false)}
                                className="text-slate-400 hover:text-white transition-colors"
                            >
                                ✕
                            </button>
                        </div>
                        <div className="p-8 overflow-y-auto bg-primary/50">
                            <ReportViewer report={selectedReport} />
                        </div>
                        <div className="p-4 border-t border-slate-700 bg-slate-800/50 rounded-b-2xl flex justify-end space-x-3">
                            <button className="btn bg-slate-700 hover:bg-slate-600 text-white flex items-center">
                                🖨️ <span className="ml-2">Imprimir</span>
                            </button>
                            <button
                                onClick={() => setIsModalOpen(false)}
                                className="btn bg-accent hover:bg-blue-600 text-white"
                            >
                                Cerrar
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
